using Microsoft.VisualStudio.TestTools.UnitTesting;

using PrefixClassName.MsTest;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.Server;

namespace Gourmand.Test.Collectibles;

using Real = Gourmand.Collectibles;

[PrefixTestClass]
public class AttributeCondition {
  private readonly Real.MatchResolver _resolver;

  public AttributeCondition() {
    _resolver = new(LoadAssets.Server.World, LoadAssets.Server.Api.Logger);
  }

  [TestMethod]
  public void JsonParseMissingOptionals() {
    string json = @"
    {
      path: [],
    }
    ";
    Real.AttributeCondition cond =
        JsonObject.FromJson(json).AsObject<Real.AttributeCondition>(null,
                                                                    "gourmand");
    CollectionAssert.AreEqual(Array.Empty<AssetLocation>(), cond.Path);
    Assert.AreEqual(null, cond.Value);
    CollectionAssert.AreEqual(Array.Empty<AssetLocation>(), cond.Outputs);
  }

  [TestMethod]
  public void JsonParsePathRequired() {
    string json = @"
    {
    }
    ";
    var ex = Assert.ThrowsExactly<Newtonsoft.Json.JsonSerializationException>(
        () => JsonObject.FromJson(json).AsObject<Real.AttributeCondition>(
            null, "gourmand"));
    Assert.Contains("Required property 'Path' not found in JSON", ex.Message);
  }

  [TestMethod]
  public void EnumerateMatchesEmptyValue() {
    string json = @"
    {
      path: [""nutritionPropsWhenInMeal"", ""foodcategory""]
    }
    ";
    Real.AttributeCondition cond =
        JsonObject.FromJson(json).AsObject<Real.AttributeCondition>(null,
                                                                    "gourmand");
    List<CollectibleObject> matches = null;
    cond.EnumerateMatches(_resolver, ref matches);

    CollectionAssert.Contains(matches,
                              LoadAssets.GetItem("game", "fruit-pineapple"));
    CollectionAssert.Contains(matches,
                              LoadAssets.GetItem("game", "fruit-blueberry"));
    CollectionAssert.DoesNotContain(matches,
                                    LoadAssets.GetItem("game", "firestarter"));
  }

  [TestMethod]
  public void EnumerateMatchesStringValue() {
    string json = @"
    {
      path: [""nutritionPropsWhenInMeal"", ""foodcategory""],
      value: ""Fruit""
    }
    ";
    Real.AttributeCondition cond =
        JsonObject.FromJson(json).AsObject<Real.AttributeCondition>(null,
                                                                    "gourmand");
    List<CollectibleObject> matches = null;
    cond.EnumerateMatches(_resolver, ref matches);

    CollectionAssert.Contains(matches,
                              LoadAssets.GetItem("game", "fruit-pineapple"));
    CollectionAssert.DoesNotContain(matches,
                                    LoadAssets.GetItem("game", "firestarter"));
  }

  [TestMethod]
  public void EnumerateMatchesRefine() {
    string json = @"
    {
      path: [""nutritionPropsWhenInMeal"", ""foodcategory""]
    }
    ";
    Real.AttributeCondition cond =
        JsonObject.FromJson(json).AsObject<Real.AttributeCondition>(null,
                                                                    "gourmand");
    List<CollectibleObject> matches =
        new() { LoadAssets.GetItem("game", "fruit-pineapple"),
                LoadAssets.GetItem("game", "firestarter") };
    cond.EnumerateMatches(_resolver, ref matches);

    CollectionAssert.Contains(matches,
                              LoadAssets.GetItem("game", "fruit-pineapple"));
    CollectionAssert.DoesNotContain(
        matches, LoadAssets.GetItem("game", "fruit-blueberry"));
    CollectionAssert.DoesNotContain(matches,
                                    LoadAssets.GetItem("game", "firestarter"));
  }

  [TestMethod]
  public void GetCategoriesOutput2() {
    string json = @"
    {
      path: [""nutritionPropsWhenInMeal"", ""foodcategory""],
      outputs: [""category1"", ""category2""]
    }
    ";
    Real.AttributeCondition cond =
        JsonObject.FromJson(json).AsObject<Real.AttributeCondition>(null,
                                                                    "gourmand");
    Dictionary<AssetLocation, IAttribute[]> categories =
        new(cond.GetCategories(_resolver.Resolver, _resolver.CatDict,
                               LoadAssets.GetItem("game", "fruit-pineapple")));
    LoadAssets.AssertCategoriesEqual(
        new Dictionary<AssetLocation, IAttribute> {
          { new AssetLocation("gourmand", "category1"),
            new StringAttribute("Fruit") },
          { new AssetLocation("gourmand", "category2"),
            new StringAttribute("Fruit") }
        },
        categories);
  }
}
