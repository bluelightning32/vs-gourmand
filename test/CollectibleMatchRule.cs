using Microsoft.VisualStudio.TestTools.UnitTesting;

using PrefixClassName.MsTest;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.Common;

namespace Gourmand.Tests;

[PrefixTestClass]
public class CollectibleMatchRule {
  private readonly Gourmand.MatchResolver _resolver;

  public CollectibleMatchRule() { _resolver = new(LoadAssets.Server.World); }

  [TestMethod]
  public void JsonParseMissingOptionals() {
    string json = @"
    {
    }
    ";
    Gourmand.CollectibleMatchRule rule =
        JsonObject.FromJson(json).AsObject<Gourmand.CollectibleMatchRule>(
            null, "gourmand");
    Assert.AreEqual(1.0, rule.Priority);
    CollectionAssert.AreEqual(
        Array.Empty<KeyValuePair<AssetLocation, IAttribute[]>>(),
        rule.Outputs.ToList());
    CollectionAssert.AreEqual(Array.Empty<AssetLocation>(), rule.Deletes);
    CollectionAssert.AreEqual(Array.Empty<ICollectibleCondition>(),
                              rule.Conditions.ToList());
  }

  [TestMethod]
  public void JsonParseOutputs() {
    string json = @"
    {
      outputs: {
        ""category"" : [ ""value"" ]
      }
    }
    ";
    Gourmand.CollectibleMatchRule rule =
        JsonObject.FromJson(json).AsObject<Gourmand.CollectibleMatchRule>(
            null, "gourmand");
    LoadAssets.AssertCategoriesEqual(
        new Dictionary<AssetLocation, IAttribute> {
          { new ("gourmand", "category"),
            new StringAttribute("value") }
        },
        rule.Outputs);
  }

  [TestMethod]
  public void EnumerateMatches() {
    string json = @"
    {
      code: { match: ""game:fruit-*"", outputs: [ ""category"" ] },
      outputs: {
        ""category"" : [ ""value"" ]
      }
    }
    ";
    Gourmand.CollectibleMatchRule rule =
        JsonObject.FromJson(json).AsObject<Gourmand.CollectibleMatchRule>(
            null, "gourmand");
    List<CollectibleObject> matches =
        rule.EnumerateMatches(_resolver, EnumItemClass.Item);

    CollectionAssert.Contains(matches,
                              LoadAssets.GetItem("game", "fruit-pineapple"));
    CollectionAssert.Contains(matches,
                              LoadAssets.GetItem("game", "fruit-blueberry"));
    CollectionAssert.DoesNotContain(matches,
                                    LoadAssets.GetItem("game", "firestarter"));
  }

  [TestMethod]
  public void UpdateCategoriesLowPriority() {
    Item pineapple = LoadAssets.GetItem("game", "fruit-pineapple");
    string json = @"
    {
      code: { match: ""game:fruit-*"", outputs: [ ""category"" ] },
      outputs: {
        ""category"" : [ ""value"" ]
      }
    }
    ";
    Gourmand.CollectibleMatchRule rule =
        JsonObject.FromJson(json).AsObject<Gourmand.CollectibleMatchRule>(
            null, "gourmand");
    Dictionary<AssetLocation, CategoryValue> categories =
        new() { { new AssetLocation("gourmand", "category"),
                  new CategoryValue(2, null) } };
    HashSet<AssetLocation> emitted = new();
    rule.UpdateCategories(pineapple, categories, emitted);
    CollectionAssert.DoesNotContain(emitted.ToList(),
                                    new AssetLocation("gourmand", "category"));
    Assert.AreEqual(categories[new AssetLocation("gourmand", "category")],
                    new CategoryValue(2, null));
  }

  [TestMethod]
  public void UpdateCategoriesHighPriority() {
    Item pineapple = LoadAssets.GetItem("game", "fruit-pineapple");
    string json = @"
    {
      priority: 1,
      outputs: {
        ""category"" : [ ""value"" ],
        ""category2"" : [ ""value2"" ]
      },
      code: { match: ""game:fruit-*"", outputs: [ ""category"" ] }
    }
    ";
    Gourmand.CollectibleMatchRule rule =
        JsonObject.FromJson(json).AsObject<Gourmand.CollectibleMatchRule>(
            null, "gourmand");
    Dictionary<AssetLocation, CategoryValue> categories =
        new() { { new AssetLocation("gourmand", "category"),
                  new CategoryValue(0, null) } };
    HashSet<AssetLocation> emitted = new();
    rule.UpdateCategories(pineapple, categories, emitted);
    CollectionAssert.AreEquivalent(
        new AssetLocation[] { new("gourmand", "category"),
                              new("gourmand", "category2") },
        emitted.ToList());
    Assert.AreEqual(new CategoryValue(1, new List<IAttribute>() {
                      new StringAttribute("value"),
                      new StringAttribute("game:fruit-pineapple")
                    }),
                    categories[new AssetLocation("gourmand", "category")]);
    Assert.AreEqual(
        new CategoryValue(
            1, new List<IAttribute>() { new StringAttribute("value2") }),
            categories[new AssetLocation("gourmand", "category2")]);
  }

  [TestMethod]
  public void UpdateCategoriesDelete() {
    Item pineapple = LoadAssets.GetItem("game", "fruit-pineapple");
    string json = @"
    {
      priority: 1,
      deletes: [
        ""category2""
      ],
      code: { match: ""game:fruit-*"", outputs: [ ""category"" ] }
    }
    ";
    Gourmand.CollectibleMatchRule rule =
        JsonObject.FromJson(json).AsObject<Gourmand.CollectibleMatchRule>(
            null, "gourmand");
    Dictionary<AssetLocation, CategoryValue> categories =
        new() { { new AssetLocation("gourmand", "category2"),
                  new CategoryValue(0, new List<IAttribute>() {
                      new StringAttribute("value")
                    }) } };
    HashSet<AssetLocation> emitted = new();
    rule.UpdateCategories(pineapple, categories, emitted);
    CollectionAssert.AreEquivalent(
        new AssetLocation[] { new("gourmand", "category"),
                              new("gourmand", "category2") },
        emitted.ToList());
    Assert.AreEqual(new CategoryValue(1, new List<IAttribute>() {
                      new StringAttribute("game:fruit-pineapple")
                    }),
                    categories[new AssetLocation("gourmand", "category")]);
    Assert.AreEqual(new CategoryValue(1, null),
                    categories[new AssetLocation("gourmand", "category2")]);
  }
}
