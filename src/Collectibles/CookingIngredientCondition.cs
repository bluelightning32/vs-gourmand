using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace Gourmand.Collectibles;

public class CookingIngredientCondition : ICondition {
  [JsonProperty(Required = Required.Always)]
  readonly public string Recipe;
  [JsonProperty(Required = Required.Always)]
  readonly public string Ingredient;

  [JsonProperty]
  public readonly AssetLocation[] Outputs;

  public CookingIngredientCondition(string cookingRecipe, string ingredient,
                                    AssetLocation[] outputs) {
    Recipe = cookingRecipe;
    Ingredient = ingredient;
    Outputs = outputs ?? Array.Empty<AssetLocation>();
  }

  [JsonIgnore]
  public IEnumerable<AssetLocation> Categories => Outputs;

  /// <summary>
  /// Gets the mixing recipe list from aculinaryartillery, if it loaded.
  /// </summary>
  /// <param name="loader">all of the loaded mods</param>
  /// <returns>
  /// the list of recipes, or null if aculinaryartillery is not loaded
  /// </returns>
  public static List<CookingRecipe> GetAcaMixingRecipes(IModLoader loader) {
    Mod aca = loader.GetMod("aculinaryartillery");
    if (aca == null) {
      return null;
    }
    foreach (ModSystem system in aca.Systems) {
      // Use reflection to avoid having to add aculinaryartillery to the build system.
      FieldInfo field = system.GetType().GetField("MixingRecipes");
      if (field == null) {
        continue;
      }
      return (List<CookingRecipe>)field.GetValue(system);
    }
    return null;
  }

  public static IEnumerable<CookingRecipe> GetRecipes(IModLoader loader) {
    List<CookingRecipe> vanilla =
        loader.GetModSystem<RecipeRegistrySystem>().CookingRecipes;
    List<CookingRecipe> mixing = GetAcaMixingRecipes(loader);
    return mixing == null ? vanilla : vanilla.Concat(mixing);
  }

  public CookingRecipeIngredient
  GetMatchingIngredient(ILogger logger, IEnumerable<CookingRecipe> recipes) {
    CookingRecipe recipe = recipes.FirstOrDefault(r => r.Code == Recipe);
    if (recipe == null) {
      logger.Warning("Could not find recipe {0}", Recipe);
      return null;
    }
    CookingRecipeIngredient ingredient =
        Array.Find(recipe.Ingredients, i => i.Code == Ingredient);
    if (ingredient == null) {
      logger.Warning("Could not find recipe ingredient {0}.{1}", Recipe,
                     Ingredient);
      return null;
    }
    return ingredient;
  }

  IEnumerable<CollectibleObject> EnumerateCollectibles(MatchResolver resolver) {
    CookingRecipeIngredient ingredient = GetMatchingIngredient(
        resolver.Logger, GetRecipes(resolver.Resolver.Api.ModLoader));
    if (ingredient == null) {
      yield break;
    }
    foreach (CookingRecipeStack s in ingredient.ValidStacks) {
      if (s.CookedStack != null) {
        if (s.CookedStack.ResolvedItemstack?.Collectible != null) {
          yield return s.CookedStack.ResolvedItemstack.Collectible;
        }
        continue;
      }
      if (s.ResolvedItemstack?.Collectible != null) {
        yield return s.ResolvedItemstack.Collectible;
        continue;
      }
      if (s.Code.IsWildCard) {
        foreach (CollectibleObject c in resolver.GetMatchingCollectibles(
                     s.Code, s.Type)) {
          yield return c;
        }
      }
    }
  }

  public void EnumerateMatches(MatchResolver resolver,
                               ref List<CollectibleObject> matches) {
    var enumerable = EnumerateCollectibles(resolver);
    if (matches == null) {
      matches = enumerable?.ToList() ?? new();
      return;
    }
    HashSet<CollectibleObject> matchSet = enumerable?.ToHashSet() ?? new();
    matches.RemoveAll(c => !matchSet.Contains(c));
  }

  public IEnumerable<KeyValuePair<AssetLocation, IAttribute[]>>
  GetCategories(IReadonlyCategoryDict catdict, CollectibleObject match) {
    foreach (AssetLocation category in Outputs) {
      yield return new KeyValuePair<AssetLocation, IAttribute[]>(
          category,
          new IAttribute[1] { new StringAttribute(match.Code.ToString()) });
    }
  }

  public static MatchRule CreateImplicitRule(string recipe, string ingredient) {
    return new MatchRule(new CookingIngredientCondition(
        recipe, ingredient,
        new AssetLocation[] { CategoryDict.ImplictIngredientCategory(
            recipe, ingredient) }));
  }
}
