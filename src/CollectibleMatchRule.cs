using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Gourmand;

public class CollectibleMatchRuleJson {
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
  public readonly CollectibleCategoryCondition[] Categories;
  [JsonProperty]
  public readonly NutritionPropsCondition NutritionProp;
  [JsonProperty]
  public readonly AttributeCondition[] Attributes;

  [JsonConstructor]
  public CollectibleMatchRuleJson(
      float priority, IReadOnlyDictionary<string, JToken[]> rawOutputs,
      AssetLocation[] deletes, CodeCondition code,
      CollectibleCategoryCondition[] categories,
      NutritionPropsCondition nutritionProp, AttributeCondition[] attributes) {
    Priority = priority;
    RawOutputs = rawOutputs ?? new Dictionary<string, JToken[]>();
    Deletes = deletes ?? Array.Empty<AssetLocation>();
    Code = code;
    Categories = categories ?? Array.Empty<CollectibleCategoryCondition>();
    NutritionProp = nutritionProp;
    Attributes = attributes;
  }

  public CollectibleMatchRuleJson(CollectibleMatchRuleJson copy) {
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
public class CollectibleMatchRuleConverter
    : JsonConverter<CollectibleMatchRule> {
  private EnumItemClass _itemClass;

  public static JsonSerializerSettings
  AddConverter(EnumItemClass itemClass,
               JsonSerializerSettings settings = null) {
    settings ??= new();
    foreach (JsonConverter converter in settings.Converters) {
      if (converter is CollectibleMatchRuleConverter cconverter) {
        cconverter._itemClass = itemClass;
        return settings;
      }
    }
    settings.Converters.Add(new CollectibleMatchRuleConverter(itemClass));
    return settings;
  }

  public CollectibleMatchRuleConverter(EnumItemClass itemClass) {
    _itemClass = itemClass;
  }

  public override void WriteJson(JsonWriter writer, CollectibleMatchRule value,
                                 JsonSerializer serializer) {
    // This shouldn't be called because CanWrite returns false.
    throw new NotImplementedException();
  }

  public override CollectibleMatchRule ReadJson(
      JsonReader reader, Type objectType, CollectibleMatchRule existingValue,
      bool hasExistingValue, JsonSerializer serializer) {
    CollectibleMatchRuleJson json =
        serializer.Deserialize<CollectibleMatchRuleJson>(reader);
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
    return new CollectibleMatchRule(_itemClass, domain, json);
  }

  public override bool CanWrite => false;
}

public class CollectibleMatchRule : CollectibleMatchRuleJson {
  readonly public IReadOnlyDictionary<AssetLocation, IAttribute[]> Outputs;

  public readonly IReadOnlyList<ICollectibleCondition> Conditions;
  public readonly EnumItemClass ItemClass;

  /// <summary>
  /// Construct a CollectibleMatchRule. To create this from json, call
  /// CollectibleMatchRuleConverter.AddConverter first.
  /// </summary>
  /// <param name="itemClass">whether this rule matches blocks or items</param>
  /// <param name="domain">the default domain for assets</param>
  /// <param name="json">the unresolved json data</param>
  public CollectibleMatchRule(EnumItemClass itemClass, string domain,
                              CollectibleMatchRuleJson json)
      : base(json) {
    ItemClass = itemClass;
    Dictionary<AssetLocation, IAttribute[]> outputs = new(RawOutputs.Count);
    foreach (KeyValuePair<string, JToken[]> p in RawOutputs) {
      outputs.Add(
          AssetLocation.Create(p.Key, domain),
          p.Value.Select((a) => new JsonObject(a).ToAttribute()).ToArray());
    }
    Outputs = outputs;
    List<ICollectibleCondition> conditions = new() {
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
    foreach (ICollectibleCondition condition in Conditions) {
      condition.EnumerateMatches(resolver, ItemClass, ref matches);
    }
    return matches;
  }

  /// <summary>
  /// Update the categories dictionary for a matched collectible.
  /// </summary>
  /// <param name="collectible">the collectible that matches this rule</param>
  /// <param name="categories">the collectible's category dictionary</param>
  /// <param name="emitted">the set of categories modified by this rule (on
  /// output)</param>
  /// <exception cref="FormatException">a delete category
  /// conflicts with an output category</exception>
  public void
  UpdateCategories(CollectibleObject collectible,
                   IReadonlyCategoryDict existingCategories,
                   Dictionary<AssetLocation, CategoryValue> categories,
                   HashSet<AssetLocation> emitted) {
    emitted.Clear();
    foreach (KeyValuePair<AssetLocation, IAttribute[]> p in Outputs) {
      UpdateCategory(categories, p.Key, p.Value, emitted);
    }
    foreach (ICollectibleCondition condition in Conditions) {
      foreach (KeyValuePair<AssetLocation, IAttribute[]> p in condition
                   .GetCategories(existingCategories, collectible)) {
        UpdateCategory(categories, p.Key, p.Value, emitted);
      }
    }
    foreach (AssetLocation c in Deletes) {
      if (emitted.Contains(c)) {
        throw new FormatException(
            $"{c.ToString()} is listed to delete but already present in the same rule.");
      }
      if (categories.TryGetValue(c, out CategoryValue value)) {
        if (value.Priority < Priority) {
          value.Priority = Priority;
          value.Value = null;
          emitted.Add(c);
        }
      } else {
        categories.Add(c, new CategoryValue(Priority, null));
      }
    }
  }

  private void
  UpdateCategory(Dictionary<AssetLocation, CategoryValue> categories,
                 AssetLocation key, IAttribute[] value,
                 HashSet<AssetLocation> emitted) {
    if (categories.TryGetValue(key, out CategoryValue categoryValue)) {
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
      categories.Add(key, new CategoryValue(Priority, value.ToList()));
      emitted.Add(key);
    }
  }
}
