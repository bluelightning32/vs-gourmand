using Microsoft.VisualStudio.TestTools.UnitTesting;

using PrefixClassName.MsTest;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand.Tests;

[PrefixTestClass]
public class CollectibleCategoryCondition {
  private readonly Gourmand.MatchResolver _resolver;

  public CollectibleCategoryCondition() {
    _resolver = new(LoadAssets.Server.World);
    string rulesJson = @"
    [
      {
        code: { match: ""game:fruit-*"" },
        outputs: {
          ""cat1"": [ 11 ]
        }
      },
      {
        code: { match: ""game:fruit-cranberry"" },
        priority: 2,
        deletes: [
          ""cat1""
        ]
      }
    ]";
    List<Gourmand.CollectibleMatchRule> rules = ParseItemRules(rulesJson);
    _resolver.Load(rules);
  }

  private static List<Gourmand.CollectibleMatchRule>
  ParseItemRules(string json) {
    return JsonUtil.ToObject<List<Gourmand.CollectibleMatchRule>>(
        json, "gourmand",
        CollectibleMatchRuleConverter.AddConverter(EnumItemClass.Item));
  }

  [TestMethod]
  public void EnumerateMatchesExistingNull() {
    string json = @"
    {
      input: ""cat1"",
    }
    ";
    Gourmand.CollectibleCategoryCondition cond =
        JsonObject.FromJson(json)
            .AsObject<Gourmand.CollectibleCategoryCondition>(null, "gourmand");

    List<CollectibleObject> matches = null;
    cond.EnumerateMatches(_resolver, EnumItemClass.Item, ref matches);

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
    Gourmand.CollectibleCategoryCondition cond =
        JsonObject.FromJson(json)
            .AsObject<Gourmand.CollectibleCategoryCondition>(null, "gourmand");

    List<CollectibleObject> matches =
        new() { LoadAssets.GetItem("game", "fruit-pineapple"),
                LoadAssets.GetItem("game", "fruit-cranberry"),
                LoadAssets.GetItem("game", "firestarter") };
    cond.EnumerateMatches(_resolver, EnumItemClass.Item, ref matches);

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
    Gourmand.CollectibleCategoryCondition cond =
        JsonObject.FromJson(json)
            .AsObject<Gourmand.CollectibleCategoryCondition>(null, "gourmand");

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
      output: [ ""output1"", ""output2"" ]
    }
    ";
    Gourmand.CollectibleCategoryCondition cond =
        JsonObject.FromJson(json)
            .AsObject<Gourmand.CollectibleCategoryCondition>(null, "gourmand");

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
      output: [ ""output1"", ""output2"" ]
    }
    ";
    Gourmand.CollectibleCategoryCondition cond =
        JsonObject.FromJson(json)
            .AsObject<Gourmand.CollectibleCategoryCondition>(null, "gourmand");

    CollectionAssert.AreEquivalent(
        new List<AssetLocation>() {
          new("gourmand", "output1"),
          new("gourmand", "output2"),
        },
        cond.Categories.ToList());
  }
}
