using Microsoft.VisualStudio.TestTools.UnitTesting;

using PrefixClassName.MsTest;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand.Tests;

[PrefixTestClass]
public class NutritionPropsCondition {
  private readonly Gourmand.MatchResolver _resolver;

  public NutritionPropsCondition() { _resolver = new(LoadAssets.Server.World); }

  [TestMethod]
  public void JsonParseMissingOptionals() {
    string json = @"
    {
    }
    ";
    Gourmand.NutritionPropsCondition cond =
        JsonObject.FromJson(json).AsObject<Gourmand.NutritionPropsCondition>(
            null, "gourmand");
    Assert.AreEqual(null, cond.Category);
    Assert.AreEqual(null, cond.Satiety);
  }

  [TestMethod]
  public void EnumerateMatchesEmptyCategory() {
    string json = @"
    {
      category: { }
    }
    ";
    Gourmand.NutritionPropsCondition cond =
        JsonObject.FromJson(json).AsObject<Gourmand.NutritionPropsCondition>(
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
  public void EnumerateMatchesFruitCategory() {
    string json = @"
    {
      category: { value: ""Fruit"" }
    }
    ";
    Gourmand.NutritionPropsCondition cond =
        JsonObject.FromJson(json).AsObject<Gourmand.NutritionPropsCondition>(
            null, "gourmand");
    List<CollectibleObject> matches = null;
    cond.EnumerateMatches(_resolver, EnumItemClass.Item, ref matches);

    CollectionAssert.Contains(matches,
                              LoadAssets.GetItem("game", "fruit-pineapple"));
    CollectionAssert.Contains(matches,
                              LoadAssets.GetItem("game", "fruit-blueberry"));
    CollectionAssert.DoesNotContain(matches,
                                    LoadAssets.GetItem("game", "fish-raw"));
    CollectionAssert.DoesNotContain(matches,
                                    LoadAssets.GetItem("game", "firestarter"));
  }

  [TestMethod]
  public void EnumerateMatchesSatiety() {
    Item pineapple = LoadAssets.GetItem("game", "fruit-pineapple");
    string json = $@"
    {{
      satiety: {{
        min: {pineapple.NutritionProps.Satiety},
        max: {pineapple.NutritionProps.Satiety}
      }}
    }}";
    Gourmand.NutritionPropsCondition cond =
        JsonObject.FromJson(json).AsObject<Gourmand.NutritionPropsCondition>(
            null, "gourmand");
    List<CollectibleObject> matches = null;
    cond.EnumerateMatches(_resolver, EnumItemClass.Item, ref matches);

    CollectionAssert.Contains(matches,
                              LoadAssets.GetItem("game", "fruit-pineapple"));
    CollectionAssert.DoesNotContain(matches,
                                    LoadAssets.GetItem("game", "fish-raw"));
    CollectionAssert.DoesNotContain(matches,
                                    LoadAssets.GetItem("game", "firestarter"));
  }

  [TestMethod]
  public void EnumerateMatchesRefine() {
    Item pineapple = LoadAssets.GetItem("game", "fruit-pineapple");
    string json = $@"
    {{
      category: {{ value: ""Fruit"" }},
      satiety: {{
        min: {pineapple.NutritionProps.Satiety},
        max: {pineapple.NutritionProps.Satiety}
      }}
    }}";
    Gourmand.NutritionPropsCondition cond =
        JsonObject.FromJson(json).AsObject<Gourmand.NutritionPropsCondition>(
            null, "gourmand");
    List<CollectibleObject> matches =
        new() { LoadAssets.GetItem("game", "fruit-pineapple"),
                LoadAssets.GetItem("game", "fish-raw"),
                LoadAssets.GetItem("game", "firestarter") };
    cond.EnumerateMatches(_resolver, EnumItemClass.Item, ref matches);

    CollectionAssert.Contains(matches,
                              LoadAssets.GetItem("game", "fruit-pineapple"));
    CollectionAssert.DoesNotContain(
        matches, LoadAssets.GetItem("game", "fruit-blueberry"));
    CollectionAssert.DoesNotContain(matches,
                                    LoadAssets.GetItem("game", "fish-raw"));
    CollectionAssert.DoesNotContain(matches,
                                    LoadAssets.GetItem("game", "firestarter"));
  }

  [TestMethod]
  public void GetCategoriesOutput2() {
    Item pineapple = LoadAssets.GetItem("game", "fruit-pineapple");
    string json = $@"
    {{
      category: {{
        value: ""Fruit"",
        output: [ ""category1"", ""category2"" ]
      }},
      satiety: {{
        min: {pineapple.NutritionProps.Satiety},
        max: {pineapple.NutritionProps.Satiety},
        output: [ ""category1"", ""category2"" ]
      }}
    }}";
    Gourmand.NutritionPropsCondition cond =
        JsonObject.FromJson(json).AsObject<Gourmand.NutritionPropsCondition>(
            null, "gourmand");
    IEnumerable<KeyValuePair<AssetLocation, IAttribute>> categories =
        cond.GetCategories(LoadAssets.GetItem("game", "fruit-pineapple"));
    LoadAssets.AssertCategoriesEqual(
        new List<KeyValuePair<AssetLocation, IAttribute>> {
          new(new AssetLocation("gourmand", "category1"),
              new StringAttribute("Fruit")),
          new(new AssetLocation("gourmand", "category2"),
              new StringAttribute("Fruit")),
          new(new AssetLocation("gourmand", "category1"),
              new FloatAttribute(pineapple.NutritionProps.Satiety)),
          new(new AssetLocation("gourmand", "category2"),
              new FloatAttribute(pineapple.NutritionProps.Satiety)),
        },
        categories);
  }
}
