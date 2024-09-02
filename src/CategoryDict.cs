using System.Collections.Generic;
using System.Linq;

using Gourmand.Collectibles;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand;

public class CategoryDict {
  private readonly IReadonlyCategoryDict _collectibleDict;
  private readonly IWorldAccessor _resolver;
  private readonly Dictionary<AssetLocation, List<MatchRule>> _rules;
  public CategoryDict(IWorldAccessor resolver,
                      IEnumerable<Collectibles.MatchRule> collectibleRules,
                      IEnumerable<MatchRule> stackRules) {
    MatchResolver matchResolver = new(resolver);
    _resolver = resolver;
    matchResolver.Load(collectibleRules);
    _collectibleDict = matchResolver.CatDict;
    Dictionary<AssetLocation, HashSet<MatchRule>> rules = new();
    foreach (MatchRule rule in stackRules) {
      foreach (AssetLocation category in rule.OutputCategories) {
        if (!rules.TryGetValue(category,
                               out HashSet<MatchRule> categoryRules)) {
          categoryRules = new();
          rules.Add(category, categoryRules);
        }
        categoryRules.Add(rule);
      }
    }
    _rules = new();
    foreach (KeyValuePair<AssetLocation, HashSet<MatchRule>> categoryRules in
                 rules) {
      List<MatchRule> sorted = categoryRules.Value.ToList();
      sorted.Sort((a, b) =>
                      Comparer<float>.Default.Compare(b.Priority, a.Priority));
      _rules.Add(categoryRules.Key, sorted);
    }
  }

  /// <summary>
  /// Look up the value of a category for the given item stack.
  /// </summary>
  /// <param name="category">the category to look up</param>
  /// <param name="stack">the object to look up the category value for</param>
  /// <returns>the value, or null if the stack does not match the
  /// category</returns>
  public CategoryValue GetValue(AssetLocation category, ItemStack stack) {
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
      if (m.IsMatch(_resolver, _collectibleDict, stack)) {
        List<IAttribute> value =
            m.GetValue(_resolver, _collectibleDict, category, stack);
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
  /// <param name="category">the category to check</param>
  /// <param name="stack">the stack that may be in the category</param>
  /// <returns>true if it is in the category</returns>
  public bool InCategory(AssetLocation category, ItemStack stack) {
    return GetValue(category, stack)?.Value != null;
  }

  /// <summary>
  /// Find all collectible objects that match any value in a category.
  /// </summary>
  /// <param name="category">the category to search</param>
  /// <returns>an enumeration of the matching collectibles</returns>
  public List<ItemStack> EnumerateMatches(AssetLocation category) {
    HashSet<ItemStack> stacks =
        new(_collectibleDict.EnumerateMatches(category).Select(
                c => new ItemStack(c)),
            new ItemStackComparer(_resolver));
    if (!_rules.TryGetValue(category, out List<MatchRule> categoryRules)) {
      return stacks.ToList();
    }
    foreach (MatchRule m in categoryRules) {
      stacks.UnionWith(m.EnumerateMatches(_resolver, _collectibleDict));
    }
    stacks.RemoveWhere(s => !InCategory(category, s));
    return stacks.ToList();
  }
}
