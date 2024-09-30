using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
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

  public CookingRecipeIngredient
  GetMatchingIngredient(ILogger logger, RecipeRegistrySystem system) {
    CookingRecipe recipe = system.CookingRecipes.Find(r => r.Code == Recipe);
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
        resolver.Resolver.Logger,
        resolver.Resolver.Api.ModLoader.GetModSystem<RecipeRegistrySystem>());
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
}
