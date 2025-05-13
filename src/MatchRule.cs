using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace Gourmand;

public class MatchRuleJson {
  /// <summary>
  /// This is a list of modids that the rule depends on. Ignore this match rule
  /// if one of the mods in this list is not installed.
  /// </summary>
  [JsonProperty("dependsOn")]
  public readonly ModDependency[] DependsOn;

  [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
  [DefaultValue(1.0)]
  public readonly float Priority;

  protected readonly Dictionary<string, JToken[]> _rawOutputs;

  [JsonProperty("outputs")]
  public IReadOnlyDictionary<string, JToken[]> RawOutputs => _rawOutputs;

  [JsonProperty]
  public readonly AssetLocation[] Deletes;

  /// <summary>
  /// Enumerate up to this many matches total.
  /// </summary>
  [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
  [DefaultValue(int.MaxValue)]
  public readonly int EnumerateMax;

  [JsonProperty]
  public readonly CategoryCondition Category;
  [JsonProperty]
  public readonly AttributeCondition[] Attributes;
  [JsonProperty]
  public string ImportRecipe { get; protected set; }
  [JsonProperty("contents")]
  public SlotCondition[] Slots { get; protected set; }
  [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
  [DefaultValue(-1)]
  public int ContentsMinSlots { get; protected set; }

  [JsonConstructor]
  public MatchRuleJson(
      ModDependency[] dependsOn, float priority,
      [JsonProperty("outputs")] Dictionary<string, JToken[]> rawOutputs,
      AssetLocation[] deletes, int enumerateMax, CategoryCondition category,
      AttributeCondition[] attributes, string importRecipe,
      SlotCondition[] slots, int contentsMinSlots) {
    DependsOn = dependsOn ?? Array.Empty<ModDependency>();
    Priority = priority;
    _rawOutputs = rawOutputs ?? new Dictionary<string, JToken[]>();
    Deletes = deletes ?? Array.Empty<AssetLocation>();
    EnumerateMax = enumerateMax;
    Category = category;
    Attributes = attributes ?? Array.Empty<AttributeCondition>();
    ImportRecipe = importRecipe;
    Slots = slots;
    ContentsMinSlots = contentsMinSlots;
  }

  public MatchRuleJson(MatchRuleJson copy) {
    DependsOn = copy.DependsOn;
    Priority = copy.Priority;
    _rawOutputs = copy._rawOutputs;
    Deletes = copy.Deletes;
    EnumerateMax = copy.EnumerateMax;
    Category = copy.Category;
    Attributes = copy.Attributes;
    ImportRecipe = copy.ImportRecipe;
    Slots = copy.Slots;
    ContentsMinSlots = copy.ContentsMinSlots;
  }

  public bool DependsOnSatisified(IModLoader modLoader) {
    return DependsOn.All(d => d.IsSatisified(modLoader));
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
  public readonly IReadOnlyDictionary<AssetLocation, List<IAttribute>> Outputs;

  public IReadOnlyList<Tuple<string, ICondition>> Conditions {
    set; private get;
  }

  private Dictionary<AssetLocation, List<ICondition>> _conditionsByCategory;
  private ContentsCondition _contentsCondition;
  private RecipeCondition _recipeCondition;

  /// <summary>
  /// Construct a CollectibleMatchRule. To create this from json, call
  /// CollectibleMatchRuleConverter.AddConverter first.
  /// </summary>
  /// <param name="itemClass">whether this rule matches blocks or items</param>
  /// <param name="domain">the default domain for assets</param>
  /// <param name="json">the unresolved json data</param>
  public MatchRule(string domain, MatchRuleJson json) : base(json) {
    Dictionary<AssetLocation, List<IAttribute>> outputs = new(RawOutputs.Count);
    // Parse the keys from RawOutputs into AssetLocations for outputs. Also fix
    // the strings in _rawOutputs to include the domain. Fixing the domain is
    // important in case this MatchRule is serialized back to JSON.
    KeyValuePair<string, JToken[]>[] rawOutputs = RawOutputs.ToArray();
    _rawOutputs.Clear();
    foreach (KeyValuePair<string, JToken[]> p in rawOutputs) {
      AssetLocation category = AssetLocation.Create(p.Key, domain);
      _rawOutputs.Add(category.ToString(), p.Value);
      outputs.Add(
          category,
          p.Value.Select((a) => new JsonObject(a).ToAttribute()).ToList());
    }
    Outputs = outputs;
    SetConditions();
  }

  private void SetConditions() {
    List<Tuple<string, ICondition>> conditions = new();
    if (Category != null) {
      conditions.Add(new("category", Category));
    }
    if (_recipeCondition != null) {
      conditions.Add(new("recipe", _recipeCondition));
    }
    foreach (AttributeCondition attribute in Attributes) {
      conditions.Add(new("attribute", attribute));
    }
    if (Slots != null || ContentsMinSlots >= 0) {
      _contentsCondition = new ContentsCondition(
          Slots ?? Array.Empty<SlotCondition>(), ContentsMinSlots);
      conditions.Add(new("contents", _contentsCondition));
    } else {
      _contentsCondition = null;
    }
    Conditions = conditions;
    _conditionsByCategory = new();
    foreach (AssetLocation category in OutputCategories) {
      if (_conditionsByCategory.ContainsKey(category)) {
        // This duplicate key was already processed.
        continue;
      }
      if (Deletes.Contains(category) || Outputs.Keys.Contains(category)) {
        _conditionsByCategory.Add(category, null);
      } else {
        _conditionsByCategory.Add(
            category,
            Conditions.Where(c => c.Item2.Categories.Contains(category))
                .Select(c => c.Item2)
                .ToList());
      }
    }
  }

  /// <summary>
  /// Processes the <see cref="ImportRecipe"/> field and merges any slots it
  /// creates with the existing slots.
  /// </summary>
  /// <param name="sapi">api to lookup the cooking recipe</param>
  /// <returns>a list of implict recipe categories that this rule depends
  /// on</returns>
  public List<string>
  ResolveImports(Dictionary<string, List<CookingRecipe>> recipes,
                 ILogger logger) {
    if (ImportRecipe == null) {
      return new();
    }
    _recipeCondition = new(ImportRecipe);
    List<string> result = _recipeCondition.Resolve(recipes, logger);
    SetConditions();
    return result;
  }

  /// <summary>
  /// All categories that this match rule produces. The output may contain
  /// duplicates.
  /// </summary>
  public IEnumerable<AssetLocation> OutputCategories =>
      Outputs.Select(p => p.Key)
          .Concat(Deletes)
          .Concat(Conditions.SelectMany(c => c.Item2.Categories));

  public IEnumerable<ItemStack>
  EnumerateMatches(IWorldAccessor resolver,
                   Collectibles.IReadonlyCategoryDict catdict) {
    IEnumerable<ItemStack> matches = null;
    foreach (Tuple<string, ICondition> condition in Conditions) {
      matches = condition.Item2.EnumerateMatches(resolver, catdict, matches);
    }
    if (EnumerateMax != int.MaxValue) {
      matches = matches.Take(EnumerateMax);
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
    return Conditions.All(c => c.Item2.IsMatch(resolver, catdict, stack));
  }

  /// <summary>
  /// Get the value of a category for a match.
  /// </summary>
  /// <param name="resolver">resolver</param>
  /// <param name="catdict">
  /// a precomputed dictionary of categories for collectible objects</param>
  /// <param name="category">
  /// The category to look up. The result is undefined if this category is not
  /// an output category.</param>
  /// <param name="stack">The stack to look up. The
  /// result is undefined if this stack is not a match.</param>
  /// <returns>the
  /// category value, or null if this match deletes the category match</returns>
  public List<IAttribute> GetValue(IWorldAccessor resolver,
                                   Collectibles.IReadonlyCategoryDict catdict,
                                   AssetLocation category, ItemStack stack) {

    List<ICondition> conditions = _conditionsByCategory[category];
    if (conditions == null) {
      if (Outputs.TryGetValue(category, out List<IAttribute> value)) {
        return value;
      }
      return null;
    }
    List<IAttribute> result = new();
    foreach (ICondition c in conditions) {
      c.AppendValue(resolver, catdict, category, stack, result);
    }
    return result;
  }

  public bool Validate(IWorldAccessor resolver, ILogger logger,
                       Collectibles.IReadonlyCategoryDict catdict) {
    bool result = true;
    foreach (Tuple<string, ICondition> condition in Conditions) {
      result &= condition.Item2.Validate(resolver, logger, catdict);
    }
    return result;
  }

  public string ExplainMismatch(IWorldAccessor resolver,
                                Collectibles.IReadonlyCategoryDict catdict,
                                ItemStack stack) {
    foreach (Tuple<string, ICondition> c in Conditions) {
      string mismatch = c.Item2.ExplainMismatch(resolver, catdict, stack);
      if (mismatch != null) {
        return "Mismatch condition " + c.Item1 + ": " + mismatch;
      }
    }
    return null;
  }
}
