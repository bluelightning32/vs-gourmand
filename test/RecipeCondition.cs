using Gourmand.Collectibles;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using PrefixClassName.MsTest;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Gourmand.Test;

using Real = Gourmand;

[PrefixTestClass]
public class RecipeCondition {
  private static readonly MatchResolver Resolver;

  static RecipeCondition() {
    Resolver = new(LoadAssets.Server.World, LoadAssets.Server.Api.Logger);
  }

  [TestMethod]
  public void Resolve() {
    Real.RecipeCondition condition = new("meatystew");
    Dictionary<string, List<CookingRecipe>> cooking =
        CookingIngredientCondition.GetRecipeDict(
            LoadAssets.Server.Api.ModLoader,
            LoadAssets.Server.Api as ICoreServerAPI,
            LoadAssets.Server.Api.Logger);
    List<string> implicits =
        condition.Resolve(cooking, LoadAssets.Server.Api.Logger);

    CollectionAssert.Contains(implicits, "protein-base");

    Assert.AreEqual(1, condition.ContentConditions.Count);
    ContentsCondition ccond = condition.ContentConditions[0];
    Real.SlotCondition protein_base =
        ccond.Slots.First(s => s.Code == "protein-base");
    Assert.AreEqual(1, protein_base.Categories.Length);
    Assert.AreEqual(Real.Collectibles.CategoryDict.ImplictIngredientCategory(
                        "meatystew", "protein-base"),
                    protein_base.Categories[0].Input);
    Assert.AreEqual(2, protein_base.Min);
    Assert.AreEqual(2, protein_base.Max);
    Assert.IsTrue(protein_base.Categories[0].EnumeratePerDistinct > 0);

    Real.SlotCondition fruit_extra =
        ccond.Slots.First(s => s.Code == "fruit-extra");
    Assert.AreEqual(1, fruit_extra.Categories.Length);
    Assert.AreEqual(Real.Collectibles.CategoryDict.ImplictIngredientCategory(
                        "meatystew", "fruit-extra"),
                    fruit_extra.Categories[0].Input);
    Assert.AreEqual(1, fruit_extra.Max);

    Real.SlotCondition meal_egg_extra =
        ccond.Slots.First(s => s.Code == "egg-extra");
    Assert.AreEqual(1, meal_egg_extra.Categories.Length);
    Assert.AreEqual(
        new AssetLocation("gourmandimportrecipe", "meatystew.egg-extra"),
        meal_egg_extra.Categories[0].Input);
  }

  [TestMethod]
  public void ResolveMissingRecipe() {
    Real.RecipeCondition condition = new("nonexistent");

    Dictionary<string, List<CookingRecipe>> cooking =
        CookingIngredientCondition.GetRecipeDict(
            LoadAssets.Server.Api.ModLoader,
            LoadAssets.Server.Api as ICoreServerAPI,
            LoadAssets.Server.Api.Logger);
    List<string> implicits =
        condition.Resolve(cooking, LoadAssets.Server.Api.Logger);
    Assert.AreEqual(0, implicits.Count);

    Block bowl = LoadAssets.GetBlock("game", "bowl-blue-meal");
    Assert.IsNotNull(bowl);
    ItemStack meal = new(bowl);
    meal.Attributes["recipeCode"] = new StringAttribute("meatystew");
    // The rule should not match anything, because its recipe is missing.
    Assert.IsFalse(condition.IsMatch(Resolver.Resolver, Resolver.CatDict,
                                     new ItemStack(bowl)));

    Assert.AreEqual(
        0, condition.EnumerateMatches(Resolver.Resolver, Resolver.CatDict, null)
               .Count());
  }

  [TestMethod]
  public void ResolveDuplicateRecipe() {
    Real.RecipeCondition condition = new("leather-sturdy-plain");

    Dictionary<string, List<CookingRecipe>> cooking =
        CookingIngredientCondition.GetRecipeDict(
            LoadAssets.Server.Api.ModLoader,
            LoadAssets.Server.Api as ICoreServerAPI,
            LoadAssets.Server.Api.Logger);
    List<string> implicits =
        condition.Resolve(cooking, LoadAssets.Server.Api.Logger);
    Assert.IsTrue(implicits.Count > 0);

    Assert.IsTrue(condition.ContentConditions.Count > 1);
  }
}
