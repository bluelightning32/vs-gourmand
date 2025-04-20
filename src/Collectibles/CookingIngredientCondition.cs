using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using Newtonsoft.Json.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
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
  /// <param name="sapi">server api for loading ACA, may be null</param>
  /// <returns>
  /// the list of recipes, or null if aculinaryartillery is not loaded
  /// </returns>
  public static List<CookingRecipe> GetAcaMixingRecipes(IModLoader loader,
                                                        ICoreServerAPI sapi) {
    Mod aca = loader.GetMod("aculinaryartillery");
    if (aca == null) {
      return null;
    }
    foreach (ModSystem system in aca.Systems) {
      // Use reflection to avoid having to add aculinaryartillery to the build
      // system.
      FieldInfo field = system.GetType().GetField("MixingRecipes");
      if (field == null) {
        continue;
      }
      return (List<CookingRecipe>)field.GetValue(system);
    }
    // This is the way to get the recipes before version 1.1.5.
    Type registryType = aca.Systems.First().GetType().Assembly.GetType(
        "ACulinaryArtillery.MixingRecipeRegistry");
    if (registryType == null) {
      loader.GetModSystem<GourmandSystem>()?.Mod.Logger.Error(
          "aculinaryartillery is present, but its mixing recipes cannot be " +
          "found.");
      return null;
    }
    FieldInfo registryFieldInfo = registryType.GetField(
        "registry", BindingFlags.NonPublic | BindingFlags.Static);
    FieldInfo mixingRecipesFieldInfo = registryType.GetField(
        "mixingRecipes", BindingFlags.NonPublic | BindingFlags.Instance);
    object registry = registryFieldInfo.GetValue(null);
    List<CookingRecipe> recipes =
        (List<CookingRecipe>)mixingRecipesFieldInfo.GetValue(registry);
    if (recipes.Count == 0) {
      // Old versions of ACA loaded the recipes in the AssetsFinalize stage
      // instead of the AssetsLoaded stage. Gourmand needs them loaded in the
      // AssetsLoaded stage. Workaround this by forcing ACA to load the recipes
      // now. This is a little inefficient, because ACA will load the recipes
      // again in the AssetsFinalized stage.
      foreach (ModSystem system in aca.Systems) {
        MethodInfo method = system.GetType().GetMethod(
            "LoadMixingRecipes", BindingFlags.Public | BindingFlags.Instance);
        if (method == null) {
          continue;
        }
        FieldInfo apiFieldInfo = system.GetType().GetField(
            "api", BindingFlags.Public | BindingFlags.Instance);
        if (apiFieldInfo.GetValue(system) == null) {
          // Set sapi before calling LoadMixingRecipes, to prevent a null
          // reference exception.
          apiFieldInfo.SetValue(system, sapi);
        }
        method.Invoke(system, Array.Empty<object>());
        recipes =
            (List<CookingRecipe>)mixingRecipesFieldInfo.GetValue(registry);
      }
    }
    return recipes;
  }

  public static IEnumerable<CookingRecipe> GetRecipes(IModLoader loader,
                                                      ICoreServerAPI sapi) {
    List<CookingRecipe> vanilla =
        loader.GetModSystem<RecipeRegistrySystem>().CookingRecipes;
    List<CookingRecipe> mixing = GetAcaMixingRecipes(loader, sapi);
    return mixing == null ? vanilla : vanilla.Concat(mixing);
  }

  public static Dictionary<string, List<CookingRecipe>>
  GetRecipeDict(IEnumerable<CookingRecipe> recipes, ICoreServerAPI sapi,
                ILogger logger) {
    Dictionary<string, List<CookingRecipe>> result = new();
    foreach (CookingRecipe recipe in recipes) {
      if (!result.TryGetValue(recipe.Code,
                              out List<CookingRecipe> recipesForCode)) {
        recipesForCode = new();
        result.Add(recipe.Code, recipesForCode);
      }
      recipesForCode.Add(recipe);
    }
    return result;
  }

  public static Dictionary<string, List<CookingRecipe>>
  GetRecipeDict(IModLoader loader, ICoreServerAPI sapi, ILogger logger) {
    return GetRecipeDict(GetRecipes(loader, sapi), sapi, logger);
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
        resolver.Logger, GetRecipes(resolver.Resolver.Api.ModLoader, null));
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
  GetCategories(IWorldAccessor resolver, IReadonlyCategoryDict catdict,
                CollectibleObject match) {
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
