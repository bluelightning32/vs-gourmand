using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Gourmand.Collectibles;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Gourmand;

public class RecipeCondition : ICondition {
  public readonly List<ContentsCondition> ContentConditions = new();
  private readonly string _recipeCode;

  public RecipeCondition(string recipeCode) { _recipeCode = recipeCode; }

  /// <summary>
  /// Sets the <see cref="ContentConditions"/> field based on the recipe.
  /// </summary>
  /// <param name="sapi">api to lookup the cooking recipe</param>
  /// <returns>a list of implict recipe categories that this rule depends
  /// on. The list may contain duplicates.</returns>
  public List<string> Resolve(Dictionary<string, List<CookingRecipe>> recipes,
                              ILogger logger) {
    ContentConditions.Clear();
    List<string> implictCategories = new();
    if (!recipes.TryGetValue(_recipeCode,
                             out List<CookingRecipe> recipesForCode)) {
      logger.Warning("Could not find recipe {0}", _recipeCode);
      return implictCategories;
    }
    foreach (CookingRecipe recipe in recipesForCode) {
      List<SlotCondition> slots = new();
      int requiredEnumerateMax = 10;
      foreach (CookingRecipeIngredient ingred in recipe.Ingredients) {
        implictCategories.Add(ingred.Code);
        slots.Add(new SlotCondition(recipe.Code, ingred, requiredEnumerateMax));
        if (slots[^1].Min > 0) {
          requiredEnumerateMax = 3;
        }
      }
      ContentConditions.Add(new ContentsCondition(slots.ToArray(), -1));
    }
    return implictCategories;
  }

  public IEnumerable<AssetLocation> Categories =>
      ContentConditions.SelectMany(c => c.Categories);

  public void AppendValue(IWorldAccessor resolver,
                          IReadonlyCategoryDict catdict, AssetLocation category,
                          ItemStack stack, List<IAttribute> result) {
    foreach (ContentsCondition c in ContentConditions) {
      if (c.Categories.Contains(category) &&
          c.IsMatch(resolver, catdict, stack)) {
        c.AppendValue(resolver, catdict, category, stack, result);
      }
    }
  }

  public IEnumerable<ItemStack> EnumerateMatches(IWorldAccessor resolver,
                                                 IReadonlyCategoryDict catdict,
                                                 IEnumerable<ItemStack> input) {
    return ContentConditions
        .Select(c => c.EnumerateMatches(resolver, catdict, input))
        .Interleave();
  }

  public bool IsMatch(IWorldAccessor resolver, IReadonlyCategoryDict catdict,
                      ItemStack stack) {
    return ContentConditions.Any(c => c.IsMatch(resolver, catdict, stack));
  }

  public bool Validate(IWorldAccessor resolver, ILogger logger,
                       IReadonlyCategoryDict catdict) {
    return ContentConditions.All(c => c.Validate(resolver, logger, catdict));
  }

  public string ExplainMismatch(IWorldAccessor resolver,
                                IReadonlyCategoryDict catdict,
                                ItemStack stack) {
    for (int i = 0; i < ContentConditions.Count; ++i) {
      string mismatch =
          ContentConditions[i].ExplainMismatch(resolver, catdict, stack);
      if (mismatch != null) {
        return $"Content condition {i} failed: {mismatch}";
      }
    }
    return null;
  }
}
