using Microsoft.VisualStudio.TestTools.UnitTesting;

using PrefixClassName.MsTest;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand.Tests;

[PrefixTestClass]
public class AttributeCondition {
  private readonly Gourmand.MatchResolver _resolver;

  public AttributeCondition() { _resolver = new(LoadAssets.Server.World); }

  [TestMethod]
  public void JsonParseMissingOptionals() {
    string json = @"
    {
      path: [],
    }
    ";
    Gourmand.AttributeCondition cond =
        JsonObject.FromJson(json).AsObject<Gourmand.AttributeCondition>(
            null, "gourmand");
    CollectionAssert.AreEqual(Array.Empty<AssetLocation>(), cond.Path);
    Assert.AreEqual(null, cond.Value);
    CollectionAssert.AreEqual(Array.Empty<AssetLocation>(), cond.Outputs);
  }

  [TestMethod]
  [ExpectedException(typeof(Newtonsoft.Json.JsonSerializationException),
                     "Required property 'Path' not found in JSON")]
  public void JsonParsePathRequired() {
    string json = @"
    {
    }
    ";
    JsonObject.FromJson(json).AsObject<Gourmand.AttributeCondition>(null,
                                                                    "gourmand");
  }

  [TestMethod]
  public void EnumerateMatchesEmptyValue() {
    string json = @"
    {
      path: [""nutritionPropsWhenInMeal"", ""foodcategory""]
    }
    ";
    Gourmand.AttributeCondition cond =
        JsonObject.FromJson(json).AsObject<Gourmand.AttributeCondition>(
            null, "gourmand");
    List<CollectibleObject> matches = null;
    cond.EnumerateMatches(_resolver, EnumItemClass.Item, ref matches);

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
    Gourmand.AttributeCondition cond =
        JsonObject.FromJson(json).AsObject<Gourmand.AttributeCondition>(
            null, "gourmand");
    List<CollectibleObject> matches = null;
    cond.EnumerateMatches(_resolver, EnumItemClass.Item, ref matches);

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
    Gourmand.AttributeCondition cond =
        JsonObject.FromJson(json).AsObject<Gourmand.AttributeCondition>(
            null, "gourmand");
    List<CollectibleObject> matches =
        new() { LoadAssets.GetItem("game", "fruit-pineapple"),
                LoadAssets.GetItem("game", "firestarter") };
    cond.EnumerateMatches(_resolver, EnumItemClass.Item, ref matches);

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
    Gourmand.AttributeCondition cond =
        JsonObject.FromJson(json).AsObject<Gourmand.AttributeCondition>(
            null, "gourmand");
    Dictionary<AssetLocation, IAttribute[]> categories =
        new(cond.GetCategories(LoadAssets.GetItem("game", "fruit-pineapple")));
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
