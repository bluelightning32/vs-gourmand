using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand;

public class MatchRuleJson {
  [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
  [DefaultValue(1.0)]
  public readonly float Priority;

  [JsonProperty("outputs")]
  public readonly IReadOnlyDictionary<string, JToken[]> RawOutputs;

  [JsonProperty]
  public readonly AssetLocation[] Deletes;

  [JsonProperty]
  public readonly CategoryCondition Category;
  [JsonProperty]
  public readonly AttributeCondition[] Attributes;

  [JsonConstructor]
  public MatchRuleJson(float priority,
                       IReadOnlyDictionary<string, JToken[]> rawOutputs,
                       AssetLocation[] deletes, CategoryCondition category,
                       AttributeCondition[] attributes) {
    Priority = priority;
    RawOutputs = rawOutputs ?? new Dictionary<string, JToken[]>();
    Deletes = deletes ?? Array.Empty<AssetLocation>();
    Category = category;
    Attributes = attributes ?? Array.Empty<AttributeCondition>();
  }

  public MatchRuleJson(MatchRuleJson copy) {
    Priority = copy.Priority;
    RawOutputs = copy.RawOutputs;
    Deletes = copy.Deletes;
    Category = copy.Category;
    Attributes = copy.Attributes;
  }
}

/// <summary>
/// Json converter for <see cref="MatchRule"/>. This is necessary to convert the
/// category names from strings to AssetLocations, while respecting the
/// serializer's current domain.
/// </summary>
public class MatchRuleConverter : JsonConverter<MatchRule> {
  public MatchRuleConverter() {}

  public override void WriteJson(JsonWriter writer, MatchRule value,
                                 JsonSerializer serializer) {
    // This shouldn't be called because CanWrite returns false.
    throw new NotImplementedException();
  }

  public override MatchRule ReadJson(JsonReader reader, Type objectType,
                                     MatchRule existingValue,
                                     bool hasExistingValue,
                                     JsonSerializer serializer) {
    MatchRuleJson json = serializer.Deserialize<MatchRuleJson>(reader);
    string domain = Collectibles.MatchRuleConverter.GetDomain(serializer);
    return new MatchRule(domain, json);
  }

  public override bool CanWrite => false;
}

[JsonConverter(typeof(MatchRuleConverter))]
[JsonObject(MemberSerialization.OptIn)]
public class MatchRule : MatchRuleJson {
  public readonly IReadOnlyDictionary<AssetLocation, IAttribute[]> Outputs;

  public readonly IReadOnlyList<ICondition> Conditions;

  private readonly Dictionary<AssetLocation, List<ICondition>>
      _conditionsByCategory;

  /// <summary>
  /// Construct a CollectibleMatchRule. To create this from json, call
  /// CollectibleMatchRuleConverter.AddConverter first.
  /// </summary>
  /// <param name="itemClass">whether this rule matches blocks or items</param>
  /// <param name="domain">the default domain for assets</param>
  /// <param name="json">the unresolved json data</param>
  public MatchRule(string domain, MatchRuleJson json) : base(json) {
    Dictionary<AssetLocation, IAttribute[]> outputs = new(RawOutputs.Count);
    foreach (KeyValuePair<string, JToken[]> p in RawOutputs) {
      outputs.Add(
          AssetLocation.Create(p.Key, domain),
          p.Value.Select((a) => new JsonObject(a).ToAttribute()).ToArray());
    }
    Outputs = outputs;
    List<ICondition> conditions = new();
    if (Category != null) {
      conditions.Add(Category);
    }
    conditions.AddRange(Attributes);
    Conditions = conditions;
    _conditionsByCategory = new();
    foreach (AssetLocation category in OutputCategories) {
      if (_conditionsByCategory.ContainsKey(category)) {
        // This duplicate key was already processed.
        continue;
      }
      if (Deletes.Contains(category)) {
        _conditionsByCategory.Add(category, null);
      } else {
        _conditionsByCategory.Add(
            category,
            Conditions.Where(c => c.Categories.Contains(category)).ToList());
      }
    }
  }

  /// <summary>
  /// All categories that this match rule produces. The output may contain
  /// duplicates.
  /// </summary>
  public IEnumerable<AssetLocation> OutputCategories =>
      Outputs.Select(p => p.Key)
          .Concat(Deletes)
          .Concat(Conditions.SelectMany(c => c.Categories));

  public List<ItemStack>
  EnumerateMatches(IWorldAccessor resolver,
                   Collectibles.IReadonlyCategoryDict catdict) {
    List<ItemStack> matches = null;
    foreach (ICondition condition in Conditions) {
      condition.EnumerateMatches(resolver, catdict, ref matches);
    }
    return matches;
  }

  /// <summary>
  /// Determine whether the given ItemStack matches the condition
  /// </summary>
  /// <param name="catdict">a precomputed dictionary of categories for
  /// collectible objects</param>
  /// <param name="stack">the ItemStack to check</param>
  /// <returns>true, if it is a match</returns>
  public bool IsMatch(IWorldAccessor resolver,
                      Collectibles.IReadonlyCategoryDict catdict,
                      ItemStack stack) {
    return Conditions.All(c => c.IsMatch(resolver, catdict, stack));
  }

  /// <summary>
  /// Get the value of a category for a match.
  /// </summary>
  /// <param name="resolver">resolver</param>
  /// <param name="catdict">
  /// a precomputed dictionary of categories for collectible objects</param>
  /// <param name="category">
  /// The category to look up. The result is undefined if this category is not
  /// an output category.</param> <param name="stack">The stack to look up. The
  /// result is undefined if this stack is not a match.</param> <returns>the
  /// category value, or null if this match deletes the category match</returns>
  public List<IAttribute> GetValue(IWorldAccessor resolver,
                                   Collectibles.IReadonlyCategoryDict catdict,
                                   AssetLocation category, ItemStack stack) {

    List<ICondition> conditions = _conditionsByCategory[category];
    if (conditions == null) {
      return null;
    }
    List<IAttribute> result = new();
    foreach (ICondition c in conditions) {
      c.AppendValue(resolver, catdict, category, stack, result);
    }
    return result;
  }
}
