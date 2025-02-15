using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Gourmand.Collectibles;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
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

  [JsonProperty]
  public readonly CategoryCondition Category;
  [JsonProperty]
  public readonly AttributeCondition[] Attributes;
  [JsonProperty]
  public string ImportRecipe { get; protected set; }
  [JsonProperty("contents")]
  public SlotCondition[] Slots { get; protected set; }

  [JsonConstructor]
  public MatchRuleJson(
      ModDependency[] dependsOn, float priority,
      [JsonProperty("outputs")] Dictionary<string, JToken[]> rawOutputs,
      AssetLocation[] deletes, CategoryCondition category,
      AttributeCondition[] attributes, string importRecipe,
      SlotCondition[] slots) {
    DependsOn = dependsOn ?? Array.Empty<ModDependency>();
    Priority = priority;
    _rawOutputs = rawOutputs ?? new Dictionary<string, JToken[]>();
    Deletes = deletes ?? Array.Empty<AssetLocation>();
    Category = category;
    Attributes = attributes ?? Array.Empty<AttributeCondition>();
    ImportRecipe = importRecipe;
    Slots = slots;
  }

  public MatchRuleJson(MatchRuleJson copy) {
    DependsOn = copy.DependsOn;
    Priority = copy.Priority;
    _rawOutputs = copy._rawOutputs;
    Deletes = copy.Deletes;
    Category = copy.Category;
    Attributes = copy.Attributes;
    ImportRecipe = copy.ImportRecipe;
    Slots = copy.Slots;
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

  public IReadOnlyList<ICondition> Conditions { set; private get; }

  private Dictionary<AssetLocation, List<ICondition>> _conditionsByCategory;
  private ContentsCondition _contentsCondition;

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
    List<ICondition> conditions = new();
    if (Category != null) {
      conditions.Add(Category);
    }
    conditions.AddRange(Attributes);
    if (Slots != null) {
      _contentsCondition = new ContentsCondition(Slots);
      conditions.Add(_contentsCondition);
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
            Conditions.Where(c => c.Categories.Contains(category)).ToList());
      }
    }
  }

  public static Dictionary<string, List<CookingRecipe>>
  GetRecipeDict(IEnumerable<CookingRecipe> recipes, ICoreServerAPI sapi,
                ILogger logger) {
    Dictionary<string, List<CookingRecipe>> result = new();
    foreach (CookingRecipe recipe in recipes) {
      if (result.TryGetValue(recipe.Code,
                             out List<CookingRecipe> recipesForCode)) {
        HashSet<string> expectedDuplicates = new() {
          // The base game has two recipes for sturdy leather.
          "leather-sturdy-plain",
          // Expanded Matter adds a recipe for glueportion-pitch-hot, and leaves
          // the base game recipe.
          "glueportion-pitch-hot",
          // The allclasses mod adds several recipes for boilingwaterportion. The base game does not have any recipes for it.
          "boilingwaterportion",
        };
        if (!expectedDuplicates.Contains(recipe.Code)) {
          StringBuilder sb = new();
          sb.AppendLine(
              $"Two recipes with the same code name of {recipe.Code} were found. Maybe you copied files within the game's asset folder? All recipe codes:");
          foreach (CookingRecipe entry in recipes) {
            sb.AppendLine(entry.Code);
          }
          if (sapi != null) {
            sb.AppendLine("All recipe files:");
            foreach (KeyValuePair<AssetLocation, JToken> file in sapi.Assets
                         .GetMany<JToken>(logger, "recipes/cooking")) {
              sb.AppendLine(file.Key.ToString());
            }
          }
          logger.Warning(sb.ToString());
        }
      } else {
        recipesForCode = new();
        result.Add(recipe.Code, recipesForCode);
      }
      recipesForCode.Add(recipe);
    }
    return result;
  }

  public static Dictionary<string, List<CookingRecipe>>
  GetRecipeDict(IModLoader loader, ICoreServerAPI sapi, ILogger logger) {
    return GetRecipeDict(CookingIngredientCondition.GetRecipes(loader, sapi),
                         sapi, logger);
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
    List<string> implictCategories = new();
    if (ImportRecipe == null) {
      return implictCategories;
    }
    if (!recipes.TryGetValue(ImportRecipe,
                             out List<CookingRecipe> recipesForCode)) {
      logger.Warning("Could not find recipe {0}", ImportRecipe);
      return implictCategories;
    }
    if (recipesForCode.Count > 1) {
      logger.Error("There are {recipesForCode.Count} recipes for " +
                   "{ImportRecipe}. Only the first will be used.");
    }
    CookingRecipe recipe = recipesForCode[0];

    List<SlotCondition> slots = new(Slots ?? Array.Empty<SlotCondition>());
    HashSet<string> initialSlots = new(slots.Select(s => s.Code));
    int requiredEnumerateMax = 10;
    foreach (CookingRecipeIngredient ingred in recipe.Ingredients) {
      implictCategories.Add(ingred.Code);
      if (!initialSlots.Contains(ingred.Code)) {
        slots.Add(new SlotCondition(recipe.Code, ingred, requiredEnumerateMax));
        if (slots[^1].Min > 0) {
          requiredEnumerateMax = 3;
        }
      }
    }
    Slots = slots.ToArray();
    ImportRecipe = null;
    SetConditions();
    return implictCategories;
  }

  /// <summary>
  /// All categories that this match rule produces. The output may contain
  /// duplicates.
  /// </summary>
  public IEnumerable<AssetLocation> OutputCategories =>
      Outputs.Select(p => p.Key)
          .Concat(Deletes)
          .Concat(Conditions.SelectMany(c => c.Categories));

  public IEnumerable<ItemStack>
  EnumerateMatches(IWorldAccessor resolver,
                   Collectibles.IReadonlyCategoryDict catdict) {
    if (ImportRecipe != null) {
      return Array.Empty<ItemStack>();
    }
    IEnumerable<ItemStack> matches = null;
    foreach (ICondition condition in Conditions) {
      matches = condition.EnumerateMatches(resolver, catdict, matches);
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
    if (ImportRecipe != null) {
      return false;
    }
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
    foreach (ICondition condition in Conditions) {
      result &= condition.Validate(resolver, logger, catdict);
    }
    return result;
  }
}
