using Microsoft.VisualStudio.TestTools.UnitTesting;

using PrefixClassName.MsTest;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand.Test;

using Real = Gourmand;

[PrefixTestClass]
public class CategoryCondition {
  private readonly Real.Collectibles.MatchResolver _resolver;

  public CategoryCondition() {
    _resolver = new(LoadAssets.Server.World, LoadAssets.Server.Api.Logger);
    string rulesJson = @"
    [
      {
        code: { match: ""game:fruit-*"", type: ""item"" },
        outputs: {
          ""cat1"": [ 11 ],
          ""itemCat"": [ ""food"" ],
        }
      },
      {
        code: { match: ""game:firestarter"", type: ""item"" },
        outputs: {
          ""itemCat"": [ ""tool"" ],
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
    List<Real.Collectibles.MatchRule> rules = ParseItemRules(rulesJson);
    _resolver.Load(rules);
  }

  private static List<Real.Collectibles.MatchRule> ParseItemRules(string json) {
    return JsonUtil.ToObject<List<Real.Collectibles.MatchRule>>(json,
                                                                "gourmand");
  }

  [TestMethod]
  public void IsMatch() {
    string json = @"
    {
      input: ""cat1"",
    }
    ";
    Real.CategoryCondition cond =
        JsonObject.FromJson(json).AsObject<Real.CategoryCondition>(null,
                                                                   "gourmand");

    Assert.IsTrue(cond.IsMatch(
        _resolver.Resolver, _resolver.CatDict,
        new ItemStack(LoadAssets.GetItem("game", "fruit-pineapple"))));
    Assert.IsFalse(cond.IsMatch(
        _resolver.Resolver, _resolver.CatDict,
        new ItemStack(LoadAssets.GetItem("game", "fruit-cranberry"))));
    Assert.IsFalse(
        cond.IsMatch(_resolver.Resolver, _resolver.CatDict,
                     new ItemStack(LoadAssets.GetItem("game", "firestarter"))));
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

    List<ItemStack> matches =
        cond.EnumerateMatches(_resolver.Resolver, _resolver.CatDict, null)
            .ToList();

    List<CollectibleObject> collectibles =
        matches.Select(c => c.Collectible).ToList();
    CollectionAssert.Contains(collectibles,
                              LoadAssets.GetItem("game", "fruit-pineapple"));
    CollectionAssert.Contains(collectibles,
                              LoadAssets.GetItem("game", "fruit-blueberry"));
    CollectionAssert.DoesNotContain(
        collectibles, LoadAssets.GetItem("game", "fruit-cranberry"));
    CollectionAssert.DoesNotContain(collectibles,
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

    List<ItemStack> matches =
        new() { new ItemStack(LoadAssets.GetItem("game", "fruit-pineapple"), 2),
                new ItemStack(LoadAssets.GetItem("game", "fruit-cranberry"), 2),
                new ItemStack(LoadAssets.GetItem("game", "firestarter"), 2) };
    matches =
        cond.EnumerateMatches(_resolver.Resolver, _resolver.CatDict, matches)
            .ToList();

    List<CollectibleObject> collectibles =
        matches.Select(c => c.Collectible).ToList();
    CollectionAssert.Contains(collectibles,
                              LoadAssets.GetItem("game", "fruit-pineapple"));
    CollectionAssert.DoesNotContain(
        collectibles, LoadAssets.GetItem("game", "fruit-blueberry"));
    CollectionAssert.DoesNotContain(
        collectibles, LoadAssets.GetItem("game", "fruit-cranberry"));
    CollectionAssert.DoesNotContain(collectibles,
                                    LoadAssets.GetItem("game", "firestarter"));

    Assert.IsTrue(matches.All(i => i.StackSize == 2));
  }

  [TestMethod]
  public void EnumeratePerDistinctExistingNull() {
    string json = @"
    {
      input: ""itemCat"",
      enumeratePerDistinct: 2
    }
    ";
    Real.CategoryCondition cond =
        JsonObject.FromJson(json).AsObject<Real.CategoryCondition>(null,
                                                                   "gourmand");

    List<ItemStack> matches =
        cond.EnumerateMatches(_resolver.Resolver, _resolver.CatDict, null)
            .ToList();

    List<CollectibleObject> collectibles =
        matches.Select(c => c.Collectible).ToList();
    int foods = 0;
    int tools = 0;
    foreach (CollectibleObject c in collectibles) {
      Real.CategoryValue value = _resolver.CatDict.GetValue(
          new AssetLocation("gourmand", "itemcat"), c);
      switch (value.Value[0].ToString()) {
      case "tool":
        tools++;
        break;
      case "food":
        foods++;
        break;
      default:
        Assert.Fail("Unexpected category value {0}", value.Value[0].ToString());
        break;
      }
    }
    Assert.AreEqual(2, foods);
    Assert.AreEqual(1, tools);
    CollectionAssert.Contains(collectibles,
                              LoadAssets.GetItem("game", "firestarter"));
  }

  [TestMethod]
  public void EnumeratePerDistinctExisting() {
    string json = @"
    {
      input: ""itemCat"",
      enumeratePerDistinct: 2
    }
    ";
    Real.CategoryCondition cond =
        JsonObject.FromJson(json).AsObject<Real.CategoryCondition>(null,
                                                                   "gourmand");

    // fruit-cranberry will be filtered out, because there were 2 fruits before
    // it. firestarter will be kept, because it is the only tool.
    List<ItemStack> matches =
        new() { new ItemStack(LoadAssets.GetItem("game", "fruit-pineapple"), 2),
                new ItemStack(LoadAssets.GetItem("game", "fruit-blueberry"), 2),
                new ItemStack(LoadAssets.GetItem("game", "fruit-cranberry"), 2),
                new ItemStack(LoadAssets.GetItem("game", "firestarter"), 2) };
    matches =
        cond.EnumerateMatches(_resolver.Resolver, _resolver.CatDict, matches)
            .ToList();

    List<CollectibleObject> collectibles =
        matches.Select(c => c.Collectible).ToList();
    CollectionAssert.Contains(collectibles,
                              LoadAssets.GetItem("game", "fruit-pineapple"));
    CollectionAssert.Contains(collectibles,
                              LoadAssets.GetItem("game", "fruit-blueberry"));
    CollectionAssert.DoesNotContain(
        collectibles, LoadAssets.GetItem("game", "fruit-cranberry"));
    CollectionAssert.Contains(collectibles,
                              LoadAssets.GetItem("game", "firestarter"));

    Assert.IsTrue(matches.All(i => i.StackSize == 2));
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

  [TestMethod]
  public void AppendValue() {
    string json = @"
    {
      input: ""cat1"",
      outputs: [ ""output1"", ""output2"" ]
    }
    ";
    Real.CategoryCondition cond =
        JsonObject.FromJson(json).AsObject<Real.CategoryCondition>(null,
                                                                   "gourmand");

    ItemStack stack = new(LoadAssets.GetItem("game", "fruit-pineapple"));
    IAttribute[] expected = { new LongAttribute(11) };
    List<IAttribute> actual = new();
    cond.AppendValue(_resolver.Resolver, _resolver.CatDict,
                     new("gourmand", "output1"), stack, actual);
    Assert.IsTrue(Real.CategoryValue.ValuesEqual(actual, expected));
    actual.Clear();
    cond.AppendValue(_resolver.Resolver, _resolver.CatDict,
                     new("gourmand", "output2"), stack, actual);
    Assert.IsTrue(Real.CategoryValue.ValuesEqual(actual, expected));
  }
}
