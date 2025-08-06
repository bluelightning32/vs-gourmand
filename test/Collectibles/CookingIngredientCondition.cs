using Microsoft.VisualStudio.TestTools.UnitTesting;

using PrefixClassName.MsTest;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand.Test.Collectibles;

using Real = Gourmand.Collectibles;

[PrefixTestClass]
public class CookingIngredientCondition {
  private readonly Real.MatchResolver _resolver;

  public CookingIngredientCondition() {
    _resolver = new(LoadAssets.Server.World, LoadAssets.Server.Api.Logger);
  }

  [TestMethod]
  public void JsonParseOutputOptional() {
    string json = @"
    {
      recipe: ""recipe"",
      ingredient: ""ingredient""
    }
    ";
    Real.CookingIngredientCondition cond =
        JsonObject.FromJson(json).AsObject<Real.CookingIngredientCondition>(
            null, "gourmand");
    Assert.AreEqual(Array.Empty<AssetLocation>(), cond.Outputs);
    Assert.AreEqual("recipe", cond.Recipe);
    Assert.AreEqual("ingredient", cond.Ingredient);
  }

  [TestMethod]
  public void JsonParseRecipeRequired() {
    string json = @"
    {
      ingredient: ""ingredient""
    }
    ";
    var ex = Assert.Throws<Newtonsoft.Json.JsonSerializationException>(
        () =>
            JsonObject.FromJson(json).AsObject<Real.CookingIngredientCondition>(
                null, "gourmand"));
    Assert.Contains("Required property 'Recipe' not found in JSON", ex.Message);
  }

  [TestMethod]
  public void JsonParseIngredientRequired() {
    string json = @"
    {
      recipe: ""recipe""
    }
    ";
    var ex = Assert.Throws<Newtonsoft.Json.JsonSerializationException>(
        () =>
            JsonObject.FromJson(json).AsObject<Real.CookingIngredientCondition>(
                null, "gourmand"));
    Assert.Contains("Required property 'Ingredient' not found in JSON",
                    ex.Message);
  }

  [TestMethod]
  public void EnumerateMatchesExistingNull() {
    string json = @"
    {
      recipe: ""vegetablestew"",
      ingredient: ""vegetable-base""
    }
    ";
    Real.CookingIngredientCondition cond =
        JsonObject.FromJson(json).AsObject<Real.CookingIngredientCondition>(
            null, "gourmand");
    List<CollectibleObject> matches = null;
    cond.EnumerateMatches(_resolver, ref matches);

    CollectionAssert.Contains(matches,
                              LoadAssets.GetItem("game", "vegetable-cabbage"));
    CollectionAssert.Contains(matches,
                              LoadAssets.GetItem("game", "vegetable-carrot"));
    CollectionAssert.DoesNotContain(matches,
                                    LoadAssets.GetItem("game", "firestarter"));
  }

  [TestMethod]
  public void EnumerateMatchesWildcard() {
    string json = @"
    {
      recipe: ""meatystew"",
      ingredient: ""fruit-extra""
    }
    ";
    Real.CookingIngredientCondition cond =
        JsonObject.FromJson(json).AsObject<Real.CookingIngredientCondition>(
            null, "gourmand");
    List<CollectibleObject> matches = null;
    cond.EnumerateMatches(_resolver, ref matches);

    CollectionAssert.Contains(matches,
                              LoadAssets.GetItem("game", "fruit-pineapple"));
    CollectionAssert.DoesNotContain(matches,
                                    LoadAssets.GetItem("game", "firestarter"));
  }

  [TestMethod]
  public void EnumerateMatchesExisting() {
    string json = @"
    {
      recipe: ""vegetablestew"",
      ingredient: ""vegetable-base""
    }
    ";
    Real.CookingIngredientCondition cond =
        JsonObject.FromJson(json).AsObject<Real.CookingIngredientCondition>(
            null, "gourmand");
    List<CollectibleObject> matches =
        new() { LoadAssets.GetItem("game", "vegetable-cabbage"),
                LoadAssets.GetItem("game", "firestarter") };
    cond.EnumerateMatches(_resolver, ref matches);

    CollectionAssert.Contains(matches,
                              LoadAssets.GetItem("game", "vegetable-cabbage"));
    CollectionAssert.DoesNotContain(
        matches, LoadAssets.GetItem("game", "vegetable-carrot"));
    CollectionAssert.DoesNotContain(matches,
                                    LoadAssets.GetItem("game", "firestarter"));
  }

  [TestMethod]
  public void GetCategoriesOutput2() {
    string json = @"
    {
      recipe: ""vegetablestew"",
      ingredient: ""vegetable-base"",
      outputs: [""category1"", ""category2""]
    }
    ";
    Real.CookingIngredientCondition cond =
        JsonObject.FromJson(json).AsObject<Real.CookingIngredientCondition>(
            null, "gourmand");
    Dictionary<AssetLocation, IAttribute[]> categories = new(
        cond.GetCategories(_resolver.Resolver, _resolver.CatDict,
                           LoadAssets.GetItem("game", "vegetable-cabbage")));
    LoadAssets.AssertCategoriesEqual(
        new Dictionary<AssetLocation, IAttribute> {
          { new AssetLocation("gourmand", "category1"),
            new StringAttribute("game:vegetable-cabbage") },
          { new AssetLocation("gourmand", "category2"),
            new StringAttribute("game:vegetable-cabbage") }
        },
        categories);
  }

  [TestMethod]
  public void Categories2() {
    string json = @"
    {
      recipe: ""vegetablestew"",
      ingredient: ""vegetable-base"",
      outputs: [""category1"", ""category2""]
    }
    ";
    Real.CookingIngredientCondition cond =
        JsonObject.FromJson(json).AsObject<Real.CookingIngredientCondition>(
            null, "gourmand");
    CollectionAssert.AreEquivalent(
        new List<AssetLocation>() {
          new("gourmand", "category1"),
          new("gourmand", "category2"),
        },
        cond.Categories.ToList());
  }
}
