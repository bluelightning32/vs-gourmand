using Microsoft.VisualStudio.TestTools.UnitTesting;

using PrefixClassName.MsTest;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.Common;

namespace Gourmand.Test.Collectibles;

using Real = Gourmand.Collectibles;

[PrefixTestClass]
public class NutritionPropsCondition {
  private readonly Real.MatchResolver _resolver;

  public NutritionPropsCondition() {
    _resolver = new(LoadAssets.Server.World, LoadAssets.Server.Api.Logger);
  }

  [TestMethod]
  public void JsonParseMissingOptionals() {
    string json = @"
    {
    }
    ";
    Real.NutritionPropsCondition cond =
        JsonObject.FromJson(json).AsObject<Real.NutritionPropsCondition>(
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
    Real.NutritionPropsCondition cond =
        JsonObject.FromJson(json).AsObject<Real.NutritionPropsCondition>(
            null, "gourmand");
    Assert.IsNull(cond.Category.Value);

    List<CollectibleObject> matches = null;
    cond.EnumerateMatches(_resolver, ref matches);

    CollectionAssert.Contains(matches,
                              LoadAssets.GetItem("game", "fruit-pineapple"));
    CollectionAssert.Contains(matches,
                              LoadAssets.GetItem("game", "fruit-blueberry"));
    CollectionAssert.Contains(matches,
                              LoadAssets.GetItem("game", "vegetable-onion"));
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
    Real.NutritionPropsCondition cond =
        JsonObject.FromJson(json).AsObject<Real.NutritionPropsCondition>(
            null, "gourmand");
    List<CollectibleObject> matches = null;
    cond.EnumerateMatches(_resolver, ref matches);

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
    Real.NutritionPropsCondition cond =
        JsonObject.FromJson(json).AsObject<Real.NutritionPropsCondition>(
            null, "gourmand");
    List<CollectibleObject> matches = null;
    cond.EnumerateMatches(_resolver, ref matches);

    CollectionAssert.Contains(matches,
                              LoadAssets.GetItem("game", "fruit-pineapple"));
    CollectionAssert.DoesNotContain(matches,
                                    LoadAssets.GetItem("game", "fish-raw"));
    CollectionAssert.DoesNotContain(matches,
                                    LoadAssets.GetItem("game", "firestarter"));
  }

  [TestMethod]
  public void EnumerateMatchesCookedSatiety() {
    Item fishRaw = LoadAssets.GetItem("game", "fish-raw");
    Item fishCooked = LoadAssets.GetItem("game", "fish-cooked");
    // fish-raw cooks into fish-cooked.
    Assert.AreEqual(EnumSmeltType.Cook, fishRaw.CombustibleProps.SmeltingType);
    Assert.AreEqual(fishCooked.Code,
                    fishRaw.CombustibleProps.SmeltedStack.Code);
    Assert.AreNotEqual(fishCooked.NutritionProps.Satiety,
                       fishRaw.NutritionProps.Satiety);
    int cookedSatiety =
        fishCooked.Attributes["nutritionPropsWhenInMeal"]["satiety"].AsInt();
    Assert.AreNotEqual(fishCooked.NutritionProps.Satiety, cookedSatiety);
    string json = $@"
    {{
      satiety: {{
        cooked: true,
        min: {cookedSatiety},
        max: {cookedSatiety}
      }}
    }}";
    Real.NutritionPropsCondition cond =
        JsonObject.FromJson(json).AsObject<Real.NutritionPropsCondition>(
            null, "gourmand");
    List<CollectibleObject> matches = null;
    cond.EnumerateMatches(_resolver, ref matches);

    CollectionAssert.Contains(matches, LoadAssets.GetItem("game", "fish-raw"));
    CollectionAssert.Contains(matches,
                              LoadAssets.GetItem("game", "fish-cooked"));
    CollectionAssert.DoesNotContain(
        matches, LoadAssets.GetItem("game", "fruit-pineapple"));
    CollectionAssert.DoesNotContain(matches,
                                    LoadAssets.GetItem("game", "firestarter"));
  }

  [TestMethod]
  public void EnumerateMatchesHealth() {
    string json = @"
    {
      health: {
        max: -1
      }
    }";
    Real.NutritionPropsCondition cond =
        JsonObject.FromJson(json).AsObject<Real.NutritionPropsCondition>(
            null, "gourmand");
    List<CollectibleObject> matches = null;
    cond.EnumerateMatches(_resolver, ref matches);

    CollectionAssert.Contains(
        matches, LoadAssets.GetBlock("game", "mushroom-flyagaric-normal"));
    CollectionAssert.DoesNotContain(
        matches, LoadAssets.GetItem("game", "fruit-pineapple"));
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
    Real.NutritionPropsCondition cond =
        JsonObject.FromJson(json).AsObject<Real.NutritionPropsCondition>(
            null, "gourmand");
    List<CollectibleObject> matches =
        new() { LoadAssets.GetItem("game", "fruit-pineapple"),
                LoadAssets.GetItem("game", "fish-raw"),
                LoadAssets.GetItem("game", "firestarter") };
    cond.EnumerateMatches(_resolver, ref matches);

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
  public void CategoriesBoth() {
    Item pineapple = LoadAssets.GetItem("game", "fruit-pineapple");
    string json = @"
    {
      category: {
        value: ""Fruit"",
        outputs: [ ""category1"", ""category2"" ]
      },
      satiety: {
        min: 1,
        max: 2,
        outputs: [ ""category1"", ""category3"" ]
      }
    }";
    Real.NutritionPropsCondition cond =
        JsonObject.FromJson(json).AsObject<Real.NutritionPropsCondition>(
            null, "gourmand");
    CollectionAssert.AreEquivalent(
        new List<AssetLocation>() {
          new("gourmand", "category1"),
          new("gourmand", "category2"),
          new("gourmand", "category3"),
        },
        cond.Categories.Distinct().ToList());
  }

  [TestMethod]
  public void CategoriesCategoryOnly() {
    Item pineapple = LoadAssets.GetItem("game", "fruit-pineapple");
    string json = @"
    {
      category: {
        value: ""Fruit"",
        outputs: [ ""category1"", ""category2"" ]
      }
    }";
    Real.NutritionPropsCondition cond =
        JsonObject.FromJson(json).AsObject<Real.NutritionPropsCondition>(
            null, "gourmand");
    CollectionAssert.AreEquivalent(
        new List<AssetLocation>() { new("gourmand", "category1"),
                                    new("gourmand", "category2") },
        cond.Categories.Distinct().ToList());
  }

  [TestMethod]
  public void GetCategoriesOutput2() {
    Item pineapple = LoadAssets.GetItem("game", "fruit-pineapple");
    string json = $@"
    {{
      category: {{
        value: ""Fruit"",
        outputs: [ ""category1"", ""category2"" ]
      }},
      satiety: {{
        min: {pineapple.NutritionProps.Satiety},
        max: {pineapple.NutritionProps.Satiety},
        outputs: [ ""category1"", ""category2"" ]
      }}
    }}";
    Real.NutritionPropsCondition cond =
        JsonObject.FromJson(json).AsObject<Real.NutritionPropsCondition>(
            null, "gourmand");
    IEnumerable<KeyValuePair<AssetLocation, IAttribute[]>> categories =
        cond.GetCategories(_resolver.Resolver, _resolver.CatDict,
                           LoadAssets.GetItem("game", "fruit-pineapple"));
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

  [TestMethod]
  public void CategoriesEmpty() {
    Item pineapple = LoadAssets.GetItem("game", "fruit-pineapple");
    string json = $@"
    {{
      category: {{
      }},
      satiety: {{
        min: {pineapple.NutritionProps.Satiety},
        max: {pineapple.NutritionProps.Satiety},
      }}
    }}";
    Real.NutritionPropsCondition cond =
        JsonObject.FromJson(json).AsObject<Real.NutritionPropsCondition>(
            null, "gourmand");
    CollectionAssert.AreEquivalent(Array.Empty<AssetLocation>(),
                                   cond.Categories.ToList());
  }
}
