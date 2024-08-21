using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Gourmand.Collectibles;

public class MatchRuleJson {
  [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
  [DefaultValue(1.0)]
  public readonly float Priority;

  [JsonProperty("outputs")]
  public readonly IReadOnlyDictionary<string, JToken[]> RawOutputs;

  [JsonProperty]
  public readonly AssetLocation[] Deletes;

  [JsonProperty]
  public readonly CodeCondition Code;
  [JsonProperty]
  public readonly CategoryCondition[] Categories;
  [JsonProperty]
  public readonly NutritionPropsCondition NutritionProp;
  [JsonProperty]
  public readonly AttributeCondition[] Attributes;

  [JsonConstructor]
  public MatchRuleJson(float priority,
                       IReadOnlyDictionary<string, JToken[]> rawOutputs,
                       AssetLocation[] deletes, CodeCondition code,
                       CategoryCondition[] categories,
                       NutritionPropsCondition nutritionProp,
                       AttributeCondition[] attributes) {
    Priority = priority;
    RawOutputs = rawOutputs ?? new Dictionary<string, JToken[]>();
    Deletes = deletes ?? Array.Empty<AssetLocation>();
    Code = code;
    Categories = categories ?? Array.Empty<CategoryCondition>();
    NutritionProp = nutritionProp;
    Attributes = attributes;
  }

  public MatchRuleJson(MatchRuleJson copy) {
    Priority = copy.Priority;
    RawOutputs = copy.RawOutputs;
    Deletes = copy.Deletes;
    Code = copy.Code;
    Categories = copy.Categories;
    NutritionProp = copy.NutritionProp;
    Attributes = copy.Attributes;
  }
}

/// <summary>
/// Json converter for CollectibleMatchRule. This is necessary to convert the
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
    string domain = GlobalConstants.DefaultDomain;
    foreach (JsonConverter converter in serializer.Converters) {
      if (converter is AssetLocationJsonParser parser) {
        FieldInfo domainField =
            typeof(AssetLocationJsonParser)
                .GetField("domain",
                          BindingFlags.Instance | BindingFlags.NonPublic);
        domain = (string)domainField.GetValue(converter);
      }
    }
    return new MatchRule(domain, json);
  }

  public override bool CanWrite => false;
}

[JsonConverter(typeof(MatchRuleConverter))]
[JsonObject(MemberSerialization.OptIn)]
public class MatchRule : MatchRuleJson {
  readonly public IReadOnlyDictionary<AssetLocation, IAttribute[]> Outputs;

  public readonly IReadOnlyList<ICondition> Conditions;

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
    List<ICondition> conditions = new() {
      Code,
    };
    conditions.AddRange(Categories);
    conditions.Add(NutritionProp);
    conditions.RemoveAll(c => c == null);
    if (Attributes != null) {
      conditions.AddRange(Attributes);
    }
    Conditions = conditions;
  }

  /// <summary>
  /// All categories that this match rule produces.
  /// </summary>
  public HashSet<AssetLocation> OutputCategories {
    get {
      List<IEnumerable<AssetLocation>> categoryLists =
          new() { Outputs.Select((p) => p.Key), Deletes,
                  Conditions.SelectMany((c) => c.Categories) };
      return categoryLists.SelectMany(a => a).ToHashSet();
    }
  }

  public HashSet<AssetLocation> Dependencies {
    get { return new(Categories.Select(c => c.Input)); }
  }

  public List<CollectibleObject> EnumerateMatches(MatchResolver resolver) {
    List<CollectibleObject> matches = null;
    foreach (ICondition condition in Conditions) {
      condition.EnumerateMatches(resolver, ref matches);
    }
    return matches;
  }

  /// <summary>
  /// Update the categories dictionary for a matched collectible.
  /// </summary>
  /// <param name="collectible">the collectible that matches this rule</param>
  /// <param name="existingCategories">the already processed, immutable
  /// categories</param> <param name="accum">the categories to update</param>
  /// <param name="emitted">the set of categories modified by this rule (on
  /// output)</param>
  /// <exception cref="FormatException">a delete category
  /// conflicts with an output category</exception>
  public void UpdateCategories(CollectibleObject collectible,
                               IReadonlyCategoryDict existingCategories,
                               CategoryDict accum,
                               HashSet<AssetLocation> emitted) {
    emitted.Clear();
    foreach (KeyValuePair<AssetLocation, IAttribute[]> p in Outputs) {
      UpdateCategory(accum, p.Key, collectible, p.Value, emitted);
    }
    foreach (ICondition condition in Conditions) {
      foreach (KeyValuePair<AssetLocation, IAttribute[]> p in condition
                   .GetCategories(existingCategories, collectible)) {
        UpdateCategory(accum, p.Key, collectible, p.Value, emitted);
      }
    }
    foreach (AssetLocation c in Deletes) {
      if (emitted.Contains(c)) {
        throw new FormatException(
            $"{c.ToString()} is listed to delete but already present in the same rule.");
      }
      CategoryValue value = accum.GetValue(c, collectible);
      if (value != null) {
        if (value.Priority < Priority) {
          value.Priority = Priority;
          value.Value = null;
          emitted.Add(c);
        }
      } else {
        accum.Add(c, collectible, new CategoryValue(Priority, null));
      }
    }
  }

  private void UpdateCategory(CategoryDict accum, AssetLocation key,
                              CollectibleObject collectible, IAttribute[] value,
                              HashSet<AssetLocation> emitted) {
    CategoryValue categoryValue = accum.GetValue(key, collectible);
    if (categoryValue != null) {
      if (!emitted.Contains(key)) {
        // This rule has not touched this category yet.
        if (categoryValue.Priority >= Priority) {
          return;
        }
        categoryValue.Value = value.ToList();
        categoryValue.Priority = Priority;
        emitted.Add(key);
      } else {
        // This is the second or greater time this rule has touched the
        // category. Append the new value to the old one.
        categoryValue.Value.AddRange(value);
      }
    } else {
      accum.Add(key, collectible, new CategoryValue(Priority, value.ToList()));
      emitted.Add(key);
    }
  }
}
