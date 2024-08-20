using Microsoft.VisualStudio.TestTools.UnitTesting;

using PrefixClassName.MsTest;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand.Tests;

[PrefixTestClass]
public class ItemStackCategoryCondition {
  private readonly Gourmand.MatchResolver _resolver;

  public ItemStackCategoryCondition() {
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
  public void IsMatch() {
    string json = @"
    {
      input: ""cat1"",
    }
    ";
    Gourmand.ItemStackCategoryCondition cond =
        JsonObject.FromJson(json).AsObject<Gourmand.ItemStackCategoryCondition>(
            null, "gourmand");

    Assert.IsTrue(cond.IsMatch(
        _resolver.CatDict,
        new ItemStack(LoadAssets.GetItem("game", "fruit-pineapple"))));
    Assert.IsFalse(cond.IsMatch(
        _resolver.CatDict,
        new ItemStack(LoadAssets.GetItem("game", "fruit-cranberry"))));
    Assert.IsFalse(
        cond.IsMatch(_resolver.CatDict,
                     new ItemStack(LoadAssets.GetItem("game", "firestarter"))));
  }

  [TestMethod]
  public void EnumerateMatchesExistingNull() {
    string json = @"
    {
      input: ""cat1"",
    }
    ";
    Gourmand.ItemStackCategoryCondition cond =
        JsonObject.FromJson(json).AsObject<Gourmand.ItemStackCategoryCondition>(
            null, "gourmand");

    List<ItemStack> matches = null;
    cond.EnumerateMatches(_resolver.CatDict, ref matches);

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
    Gourmand.ItemStackCategoryCondition cond =
        JsonObject.FromJson(json).AsObject<Gourmand.ItemStackCategoryCondition>(
            null, "gourmand");

    List<ItemStack> matches =
        new() { new ItemStack(LoadAssets.GetItem("game", "fruit-pineapple"), 2),
                new ItemStack(LoadAssets.GetItem("game", "fruit-cranberry"), 2),
                new ItemStack(LoadAssets.GetItem("game", "firestarter"), 2) };
    cond.EnumerateMatches(_resolver.CatDict, ref matches);

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
  public void Categories2() {
    string json = @"
    {
      input: ""cat1"",
      output: [ ""output1"", ""output2"" ]
    }
    ";
    Gourmand.ItemStackCategoryCondition cond =
        JsonObject.FromJson(json).AsObject<Gourmand.ItemStackCategoryCondition>(
            null, "gourmand");

    CollectionAssert.AreEquivalent(
        new List<AssetLocation>() {
          new("gourmand", "output1"),
          new("gourmand", "output2"),
        },
        cond.Categories.ToList());
  }

  [TestMethod]
  public void GetValue() {
    string json = @"
    {
      input: ""cat1"",
      output: [ ""output1"", ""output2"" ]
    }
    ";
    Gourmand.ItemStackCategoryCondition cond =
        JsonObject.FromJson(json).AsObject<Gourmand.ItemStackCategoryCondition>(
            null, "gourmand");

    ItemStack stack = new(LoadAssets.GetItem("game", "fruit-pineapple"));
    IAttribute[] expected = { new LongAttribute(11) };
    List<IAttribute> actual =
        cond.GetValue(_resolver.CatDict, new("gourmand", "output1"), stack);
    Assert.IsTrue(CategoryValue.ValuesEqual(actual, expected));
    actual =
        cond.GetValue(_resolver.CatDict, new("gourmand", "output2"), stack);
    Assert.IsTrue(CategoryValue.ValuesEqual(actual, expected));
  }
}
