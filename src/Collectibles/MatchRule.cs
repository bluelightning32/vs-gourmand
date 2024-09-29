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
  /// <summary>
  /// This is a list of modids that the rule depends on. Ignore this match rule
  /// if one of the mods in this list is not installed.
  /// </summary>
  [JsonProperty("dependsOn")]
  public readonly string[] DependsOn;

  [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
  [DefaultValue(1.0)]
  public readonly float Priority;

  [JsonProperty("outputs")]
  public readonly IReadOnlyDictionary<string, JToken[]> RawOutputs;

  [JsonProperty]
  public readonly AssetLocation[] Deletes;

  [JsonProperty]
  public readonly CategoryCondition[] Categories;
  [JsonProperty]
  public readonly CodeCondition Code;
  [JsonProperty]
  public readonly NutritionPropsCondition NutritionProps;
  [JsonProperty]
  public readonly AttributeCondition[] Attributes;
  [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
  [DefaultValue(false)]
  public readonly bool IgnoreNoMatches;

  [JsonConstructor]
  public MatchRuleJson(string[] dependsOn, float priority,
                       IReadOnlyDictionary<string, JToken[]> rawOutputs,
                       AssetLocation[] deletes, CategoryCondition[] categories,
                       CodeCondition code,
                       NutritionPropsCondition nutritionProp,
                       AttributeCondition[] attributes, bool ignoreNoMatches) {
    DependsOn = dependsOn ?? Array.Empty<string>();
    Priority = priority;
    RawOutputs = rawOutputs ?? new Dictionary<string, JToken[]>();
    Deletes = deletes ?? Array.Empty<AssetLocation>();
    Categories = categories ?? Array.Empty<CategoryCondition>();
    Code = code;
    NutritionProps = nutritionProp;
    Attributes = attributes ?? Array.Empty<AttributeCondition>();
    IgnoreNoMatches = ignoreNoMatches;
  }

  public MatchRuleJson(MatchRuleJson copy) {
    DependsOn = copy.DependsOn;
    Priority = copy.Priority;
    RawOutputs = copy.RawOutputs;
    Deletes = copy.Deletes;
    Categories = copy.Categories;
    Code = copy.Code;
    NutritionProps = copy.NutritionProps;
    Attributes = copy.Attributes;
    IgnoreNoMatches = copy.IgnoreNoMatches;
  }

  public bool DependsOnSatisified(IModLoader modLoader) {
    return DependsOn.All(modLoader.IsModEnabled);
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
    string domain = GetDomain(serializer);
    return new MatchRule(domain, json);
  }

  public static string GetDomain(JsonSerializer serializer) {
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
    return domain;
  }

  public override bool CanWrite => false;
}

[JsonConverter(typeof(MatchRuleConverter))]
[JsonObject(MemberSerialization.OptIn)]
public class MatchRule : MatchRuleJson {
  readonly public IReadOnlyDictionary<AssetLocation, IAttribute[]> Outputs;

  [JsonIgnore]
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
    List<ICondition> conditions = new(Categories) { Code, NutritionProps };
    conditions.AddRange(Attributes);
    conditions.RemoveAll(c => c == null);
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
    if (matches.Count == 0 && !IgnoreNoMatches) {
      Console.WriteLine(
          "None of the collectibles succeeded for match rule: {0}",
          JsonConvert.SerializeObject(this));
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
