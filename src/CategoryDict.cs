using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Gourmand.Collectibles;

using Newtonsoft.Json;

using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Gourmand;

public class CategoryDict : RecipeRegistryBase, IByteSerializable {
  private readonly Collectibles.CategoryDict _collectibleDict;
  private readonly Dictionary<AssetLocation, List<MatchRule>> _rules;
  public CategoryDict(IWorldAccessor resolver, ILogger logger,
                      IEnumerable<Collectibles.MatchRule> collectibleRules,
                      IEnumerable<MatchRule> stackRules) {
    MatchResolver matchResolver = new(resolver, logger);
    _rules = new();
    IEnumerable<Collectibles.MatchRule> implicitRules =
        LoadStackRules(resolver, logger, stackRules);
    _collectibleDict =
        matchResolver.Load(collectibleRules.Concat(implicitRules));
    Validate(resolver, logger);
  }

  public CategoryDict() {
    _collectibleDict = new();
    _rules = new();
  }

  public void Set(IWorldAccessor resolver, ILogger logger,
                  IEnumerable<Collectibles.MatchRule> collectibleRules,
                  List<MatchRule> stackRules) {
    MatchResolver matchResolver = new(resolver, logger);
    _rules.Clear();
    IEnumerable<Collectibles.MatchRule> implicitRules =
        LoadStackRules(resolver, logger, stackRules);
    _collectibleDict.Copy(
        matchResolver.Load(collectibleRules.Concat(implicitRules)));
    Validate(resolver, logger);
  }

  private IEnumerable<Collectibles.MatchRule>
  LoadStackRules(IWorldAccessor resolver, ILogger logger,
                 IEnumerable<MatchRule> stackRules) {
    Dictionary<string, List<CookingRecipe>> cooking =
        CookingIngredientCondition.GetRecipeDict(
            resolver.Api.ModLoader, resolver.Api as ICoreServerAPI, logger);
    HashSet<Tuple<string, string>> implicits = new();

    Dictionary<AssetLocation, HashSet<MatchRule>> rules = new();
    foreach (MatchRule rule in stackRules) {
      if (!rule.DependsOnSatisified(resolver.Api.ModLoader)) {
        continue;
      }
      foreach (string ingred in rule.ResolveImports(cooking, logger)) {
        implicits.Add(new(rule.ImportRecipe, ingred));
      }
      foreach (AssetLocation category in rule.OutputCategories) {
        if (!rules.TryGetValue(category,
                               out HashSet<MatchRule> categoryRules)) {
          categoryRules = new();
          rules.Add(category, categoryRules);
        }
        categoryRules.Add(rule);
      }
    }
    foreach (KeyValuePair<AssetLocation, HashSet<MatchRule>> categoryRules in
                 rules) {
      List<MatchRule> sorted = categoryRules.Value.ToList();
      sorted.Sort((a, b) =>
                      Comparer<float>.Default.Compare(b.Priority, a.Priority));
      _rules.Add(categoryRules.Key, sorted);
    }

    foreach (var imp in implicits) {
      yield return CookingIngredientCondition.CreateImplicitRule(imp.Item1,
                                                                 imp.Item2);
    }
  }

  /// <summary>
  /// Look up the value of a category for the given item stack.
  /// </summary>
  /// <param name="resolver">resolver</param>
  /// <param name="category">the category to look up</param>
  /// <param name="stack">the object to look up the category value for</param>
  /// <returns>the value, or null if the stack does not match the
  /// category</returns>
  public CategoryValue GetValue(IWorldAccessor resolver, AssetLocation category,
                                ItemStack stack) {
    CategoryValue current =
        _collectibleDict.GetValue(category, stack.Collectible);
    bool modifiable = false;
    if (!_rules.TryGetValue(category, out List<MatchRule> categoryRules)) {
      return current;
    }
    foreach (MatchRule m in categoryRules) {
      if (current != null && m.Priority < current.Priority) {
        return current;
      }
      if (m.IsMatch(resolver, _collectibleDict, stack)) {
        List<IAttribute> value =
            m.GetValue(resolver, _collectibleDict, category, stack);
        if (!modifiable) {
          current = new(m.Priority, value);
          modifiable = true;
        } else {
          current.Priority = m.Priority;
          current.Value = value;
        }
      }
    }
    return current;
  }

  /// <summary>
  /// Check whether the stack is in a category
  /// </summary>
  /// <param name="resolver">resolver</param>
  /// <param name="category">the category to check</param>
  /// <param name="stack">the stack that may be in the category</param>
  /// <returns>true if it is in the category</returns>
  public bool InCategory(IWorldAccessor resolver, AssetLocation category,
                         ItemStack stack) {
    return GetValue(resolver, category, stack)?.Value != null;
  }

  /// <summary>
  /// Find all collectible objects that match any value in a category.
  /// </summary>
  /// <param name="resolver">resolver</param>
  /// <param name="category">the category to search</param>
  /// <returns>an enumeration of the matching collectibles</returns>
  public IEnumerable<ItemStack> EnumerateMatches(IWorldAccessor resolver,
                                                 AssetLocation category) {

    HashSet<ItemStack> stacks = new(new ItemStackComparer(
        resolver, GlobalConstants.IgnoredStackAttributes));
    foreach (CollectibleObject c in _collectibleDict.EnumerateMatches(category,
                                                                      false)) {
      ItemStack s = new(c);
      if (stacks.Add(s) && InCategory(resolver, category, s)) {
        yield return s;
      }
    }
    if (!_rules.TryGetValue(category, out List<MatchRule> categoryRules)) {
      yield break;
    }
    foreach (MatchRule m in categoryRules) {
      foreach (ItemStack s in m.EnumerateMatches(resolver, _collectibleDict)) {
        if (stacks.Add(s) && InCategory(resolver, category, s)) {
          yield return s;
        }
      }
    }
  }

  public void ToBytes(BinaryWriter writer) {
    _collectibleDict.ToBytes(writer);
    HashSet<MatchRule> rules = new();
    foreach (List<MatchRule> newRules in _rules.Values) {
      rules.UnionWith(newRules);
    }
    writer.Write(rules.Count);
    foreach (MatchRule rule in rules) {
      writer.Write(JsonConvert.SerializeObject(rule));
    }
  }

  public void FromBytes(BinaryReader reader, IWorldAccessor resolver) {
    _collectibleDict.FromBytes(reader, resolver);
    int count = reader.ReadInt32();
    List<MatchRule> stackRules = new(count);
    for (int i = 0; i < count; ++i) {
      stackRules.Add(
          JsonConvert.DeserializeObject<MatchRule>(reader.ReadString()));
    }
    _rules.Clear();
    IEnumerable<Collectibles.MatchRule> newCollectibleRules =
        LoadStackRules(resolver, resolver.Logger, stackRules);
    ValidateRulesAlreadyLoaded(resolver, newCollectibleRules);
    Validate(resolver, resolver.Logger);
  }

  private void ValidateRulesAlreadyLoaded(
      IWorldAccessor resolver,
      IEnumerable<Collectibles.MatchRule> newCollectibleRules) {
    MatchResolver matchResolver = new(resolver, resolver.Logger);
    HashSet<AssetLocation> emitted = new();
    Collectibles.CategoryDict accum = new();
    foreach (Collectibles.MatchRule rule in newCollectibleRules) {
      List<CollectibleObject> objs = rule.EnumerateMatches(matchResolver);
      foreach (CollectibleObject obj in objs) {
        rule.UpdateCategories(resolver, obj, _collectibleDict, accum, emitted);
      }
    }
    accum.ValidateSubsetOf(_collectibleDict);
  }

  public override void ToBytes(IWorldAccessor resolver, out byte[] data,
                               out int quantity) {
    using MemoryStream ms = new();
    using BinaryWriter writer = new(ms);

    _collectibleDict.ToBytes(writer);
    HashSet<MatchRule> rules = new();
    foreach (List<MatchRule> newRules in _rules.Values) {
      rules.UnionWith(newRules);
    }
    quantity = rules.Count;
    foreach (MatchRule rule in rules) {
      writer.Write(JsonConvert.SerializeObject(rule));
    }

    data = ms.ToArray();
  }

  public override void FromBytes(IWorldAccessor resolver, int quantity,
                                 byte[] data) {
    using MemoryStream ms = new(data);
    using BinaryReader reader = new(ms);

    _collectibleDict.FromBytes(reader, resolver);
    List<MatchRule> stackRules = new(quantity);
    for (int i = 0; i < quantity; ++i) {
      stackRules.Add(
          JsonConvert.DeserializeObject<MatchRule>(reader.ReadString()));
    }
    _rules.Clear();
    IEnumerable<Collectibles.MatchRule> newCollectibleRules =
        LoadStackRules(resolver, resolver.Logger, stackRules);
    ValidateRulesAlreadyLoaded(resolver, newCollectibleRules);
    Validate(resolver, resolver.Logger);
  }

  public bool Validate(IWorldAccessor resolver, ILogger logger) {
    bool result = true;
    foreach (List<MatchRule> categoryRules in _rules.Values) {
      foreach (MatchRule rule in categoryRules) {
        result &= rule.Validate(resolver, logger, _collectibleDict);
      }
    }
    return result;
  }

  public string ExplainMatch(IWorldAccessor resolver, AssetLocation category,
                             ItemStack stack) {
    CategoryValue current =
        _collectibleDict.GetValue(category, stack.Collectible);
    bool modifiable = false;
    StringBuilder explanation = new();
    if (current == null) {
      if (_collectibleDict.IsRegistered(category)) {
        explanation.AppendLine(
            "No collectible matchers registered for category.");
      } else {
        explanation.AppendLine("Collectible matcher does not match.");
      }
    } else {
      explanation.AppendLine(
          $"Collectible matcher matched at priority {current.Priority}");
    }
    if (!_rules.TryGetValue(category, out List<MatchRule> categoryRules)) {
      explanation.AppendLine("No registered item stack matchers for category");
      return explanation.ToString();
    }
    foreach (MatchRule m in categoryRules) {
      if (current != null && m.Priority < current.Priority) {
        explanation.AppendLine(
            $"Match priority {current.Priority} is greater than the max of the remaining item stack matcher priorities of {m.Priority}");
        return explanation.ToString();
      }
      string mismatch = m.ExplainMismatch(resolver, _collectibleDict, stack);
      if (mismatch == null) {
        List<IAttribute> value =
            m.GetValue(resolver, _collectibleDict, category, stack);
        if (!modifiable) {
          current = new(m.Priority, value);
          modifiable = true;
        } else {
          current.Priority = m.Priority;
          current.Value = value;
        }
        explanation.AppendLine($"Matched item stack rule: {current}.");
      } else {
        explanation.AppendLine("Failed item stack matcher: " + mismatch);
      }
    }
    if (current != null && current.Value == null) {
      explanation.AppendLine("The last matching rule was a delete rule.");
    }
    return explanation.ToString();
  }
}
