using Microsoft.VisualStudio.TestTools.UnitTesting;

using PrefixClassName.MsTest;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

using Gourmand.Collectibles;

namespace Gourmand.Test.Collectibles;

using Real = Gourmand.Collectibles;

[PrefixTestClass]
public class MatchRule {
  private readonly Real.MatchResolver _resolver;

  public MatchRule() { _resolver = new(LoadAssets.Server.World); }

  [TestMethod]
  public void JsonParseMissingOptionals() {
    string json = @"
    {
    }
    ";
    Real.MatchRule rule = JsonUtil.ToObject<Real.MatchRule>(json, "gourmand");
    Assert.AreEqual(1.0, rule.Priority);
    CollectionAssert.AreEqual(
        Array.Empty<KeyValuePair<AssetLocation, IAttribute[]>>(),
        rule.Outputs.ToList());
    CollectionAssert.AreEqual(Array.Empty<AssetLocation>(), rule.Deletes);
    CollectionAssert.AreEqual(Array.Empty<Real.ICondition>(),
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
    Real.MatchRule rule = JsonUtil.ToObject<Real.MatchRule>(json, "gourmand");
    LoadAssets.AssertCategoriesEqual(
        new Dictionary<AssetLocation, IAttribute> {
          { new("gourmand", "category"), new StringAttribute("value") }
        },
        rule.Outputs);
  }

  [TestMethod]
  public void EnumerateMatches() {
    string json = @"
    {
      code: { match: ""game:fruit-*"", type: ""item"", outputs: [ ""category"" ] },
      outputs: {
        ""category"" : [ ""value"" ]
      }
    }
    ";
    Real.MatchRule rule = JsonUtil.ToObject<Real.MatchRule>(json, "gourmand");
    List<CollectibleObject> matches = rule.EnumerateMatches(_resolver);

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
      code: { match: ""game:fruit-*"", type: ""item"", outputs: [ ""category"" ] },
      outputs: {
        ""category"" : [ ""value"" ]
      }
    }
    ";
    Real.MatchRule rule = JsonUtil.ToObject<Real.MatchRule>(json, "gourmand");
    Real.CategoryDict categories = new();
    categories.Set(new AssetLocation("gourmand", "category"), pineapple,
                   new CategoryValue(2, null));
    HashSet<AssetLocation> emitted = new();
    rule.UpdateCategories(pineapple, _resolver.CatDict, categories, emitted);
    CollectionAssert.DoesNotContain(emitted.ToList(),
                                    new AssetLocation("gourmand", "category"));
    Assert.AreEqual(categories.GetValue(
                        new AssetLocation("gourmand", "category"), pineapple),
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
      code: {
        match: ""game:fruit-*"", type: ""item"", outputs: [ ""category"" ]
      }
    }
    ";
    Real.MatchRule rule = JsonUtil.ToObject<Real.MatchRule>(json, "gourmand");
    Real.CategoryDict categories = new();
    categories.Set(new AssetLocation("gourmand", "category"), pineapple,
                   new CategoryValue(0, null));
    HashSet<AssetLocation> emitted = new();
    rule.UpdateCategories(pineapple, _resolver.CatDict, categories, emitted);
    CollectionAssert.AreEquivalent(
        new AssetLocation[] { new("gourmand", "category"),
                              new("gourmand", "category2") },
        emitted.ToList());
    Assert.AreEqual(
        new CategoryValue(
            1, new List<IAttribute>() { new StringAttribute("value"),
                                        new StringAttribute(
                                            "game:fruit-pineapple") }),
        categories.GetValue(new AssetLocation("gourmand", "category"),
                            pineapple));
    Assert.AreEqual(
        new CategoryValue(
            1, new List<IAttribute>() { new StringAttribute("value2") }),
        categories.GetValue(new AssetLocation("gourmand", "category2"),
                            pineapple));
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
      code: {
        match: ""game:fruit-*"", type: ""item"", outputs: [ ""category"" ]
      }
    }
    ";
    Real.MatchRule rule = JsonUtil.ToObject<Real.MatchRule>(json, "gourmand");
    Real.CategoryDict categories = new();
    categories.Set(
        new AssetLocation("gourmand", "category2"), pineapple,
        new CategoryValue(
            0, new List<IAttribute>() { new StringAttribute("value") }));
    HashSet<AssetLocation> emitted = new();
    rule.UpdateCategories(pineapple, _resolver.CatDict, categories, emitted);
    CollectionAssert.AreEquivalent(
        new AssetLocation[] { new("gourmand", "category"),
                              new("gourmand", "category2") },
        emitted.ToList());
    Assert.AreEqual(
        new CategoryValue(1, new List<IAttribute>() { new StringAttribute(
                                 "game:fruit-pineapple") }),
        categories.GetValue(new AssetLocation("gourmand", "category"),
                            pineapple));
    Assert.AreEqual(new CategoryValue(1, null),
                    categories.GetValue(
                        new AssetLocation("gourmand", "category2"), pineapple));
  }
}
