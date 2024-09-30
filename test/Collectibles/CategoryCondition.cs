using Microsoft.VisualStudio.TestTools.UnitTesting;

using PrefixClassName.MsTest;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand.Test.Collectibles;

using Real = Gourmand.Collectibles;

[PrefixTestClass]
public class CategoryCondition {
  private readonly Real.MatchResolver _resolver;

  public CategoryCondition() {
    _resolver = new(LoadAssets.Server.World, LoadAssets.Server.Api.Logger);
    string rulesJson = @"
    [
      {
        code: { match: ""game:fruit-*"", type: ""item"" },
        outputs: {
          ""cat1"": [ 11 ]
        }
      },
      {
        code: { match: ""game:fruit-cranberry"", type: ""item"" },
        priority: 2,
        deletes: [
          ""cat1""
        ]
      }
    ]";
    List<Real.MatchRule> rules = ParseItemRules(rulesJson);
    _resolver.Load(rules);
  }

  private static List<Real.MatchRule> ParseItemRules(string json) {
    return JsonUtil.ToObject<List<Real.MatchRule>>(json, "gourmand");
  }

  [TestMethod]
  public void EnumerateMatchesExistingNull() {
    string json = @"
    {
      input: ""cat1"",
    }
    ";
    Real.CategoryCondition cond =
        JsonObject.FromJson(json).AsObject<Real.CategoryCondition>(null,
                                                                   "gourmand");

    List<CollectibleObject> matches = null;
    cond.EnumerateMatches(_resolver, ref matches);

    CollectionAssert.Contains(matches,
                              LoadAssets.GetItem("game", "fruit-pineapple"));
    CollectionAssert.Contains(matches,
                              LoadAssets.GetItem("game", "fruit-blueberry"));
    CollectionAssert.DoesNotContain(
        matches, LoadAssets.GetItem("game", "fruit-cranberry"));
    CollectionAssert.DoesNotContain(matches,
                                    LoadAssets.GetItem("game", "firestarter"));
  }

  [TestMethod]
  public void EnumerateMatchesExisting() {
    string json = @"
    {
      input: ""cat1"",
    }
    ";
    Real.CategoryCondition cond =
        JsonObject.FromJson(json).AsObject<Real.CategoryCondition>(null,
                                                                   "gourmand");

    List<CollectibleObject> matches =
        new() { LoadAssets.GetItem("game", "fruit-pineapple"),
                LoadAssets.GetItem("game", "fruit-cranberry"),
                LoadAssets.GetItem("game", "firestarter") };
    cond.EnumerateMatches(_resolver, ref matches);

    CollectionAssert.Contains(matches,
                              LoadAssets.GetItem("game", "fruit-pineapple"));
    CollectionAssert.DoesNotContain(
        matches, LoadAssets.GetItem("game", "fruit-blueberry"));
    CollectionAssert.DoesNotContain(
        matches, LoadAssets.GetItem("game", "fruit-cranberry"));
    CollectionAssert.DoesNotContain(matches,
                                    LoadAssets.GetItem("game", "firestarter"));
  }

  [TestMethod]
  public void GetCategoriesOutputEmpty() {
    string json = @"
    {
      input: ""cat1"",
    }
    ";
    Real.CategoryCondition cond =
        JsonObject.FromJson(json).AsObject<Real.CategoryCondition>(null,
                                                                   "gourmand");

    Dictionary<AssetLocation, IAttribute[]> categories = new(cond.GetCategories(
        _resolver.CatDict, LoadAssets.GetItem("game", "fruit-pineapple")));
    CollectionAssert.AreEqual(
        categories, Array.Empty<KeyValuePair<AssetLocation, IAttribute[]>>());
  }

  [TestMethod]
  public void GetCategoriesOutput2() {
    string json = @"
    {
      input: ""cat1"",
      outputs: [ ""output1"", ""output2"" ]
    }
    ";
    Real.CategoryCondition cond =
        JsonObject.FromJson(json).AsObject<Real.CategoryCondition>(null,
                                                                   "gourmand");

    Dictionary<AssetLocation, IAttribute[]> categories = new(cond.GetCategories(
        _resolver.CatDict, LoadAssets.GetItem("game", "fruit-pineapple")));
    LoadAssets.AssertCategoriesEqual(
        new Dictionary<AssetLocation, IAttribute> {
          { new AssetLocation("gourmand", "output1"), new LongAttribute(11) },
          { new AssetLocation("gourmand", "output2"), new LongAttribute(11) }
        },
        categories);
  }

  [TestMethod]
  public void Categories2() {
    string json = @"
    {
      input: ""cat1"",
      outputs: [ ""output1"", ""output2"" ]
    }
    ";
    Real.CategoryCondition cond =
        JsonObject.FromJson(json).AsObject<Real.CategoryCondition>(null,
                                                                   "gourmand");

    CollectionAssert.AreEquivalent(
        new List<AssetLocation>() {
          new("gourmand", "output1"),
          new("gourmand", "output2"),
        },
        cond.Categories.ToList());
  }
}
