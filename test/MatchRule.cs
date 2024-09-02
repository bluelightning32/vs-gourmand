using Microsoft.VisualStudio.TestTools.UnitTesting;

using PrefixClassName.MsTest;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand.Test;

using Real = Gourmand;

[PrefixTestClass]
public class MatchRule {
  private static readonly Real.Collectibles.MatchResolver _resolver;

  static MatchRule() {
    _resolver = new(LoadAssets.Server.World);

    string rulesJson = @"
    [
      {
        code : {
          match: ""game:bowl-meal"",
          type: ""block"",
          outputs: [ ""edible-meal-container"" ]
        }
      }
    ]";
    List<Real.Collectibles.MatchRule> rules =
        JsonUtil.ToObject<List<Real.Collectibles.MatchRule>>(rulesJson,
                                                             "gourmand");

    _resolver.Load(rules);
  }

  [TestMethod]
  public void IsMatchCategory() {
    string json = @"
    {
      category: {
        input: ""edible-meal-container""
      },
    }
    ";
    Real.MatchRule rule =
        JsonObject.FromJson(json).AsObject<Real.MatchRule>(null, "gourmand");
    Block bowl = LoadAssets.GetBlock("game", "bowl-meal");
    Assert.IsTrue(rule.IsMatch(_resolver.Resolver, _resolver.CatDict,
                               new ItemStack(bowl)));

    Item pineapple = LoadAssets.GetItem("game", "fruit-pineapple");
    Assert.IsFalse(rule.IsMatch(_resolver.Resolver, _resolver.CatDict,
                                new ItemStack(pineapple)));
  }

  [TestMethod]
  public void IsMatchAttribute() {
    string json = @"
    {
      attributes: [
        {
          path: [ ""recipeCode"" ],
        },
      ]
    }
    ";
    Real.MatchRule rule =
        JsonObject.FromJson(json).AsObject<Real.MatchRule>(null, "gourmand");
    Block bowl = LoadAssets.GetBlock("game", "bowl-meal");

    Assert.IsFalse(rule.IsMatch(_resolver.Resolver, _resolver.CatDict,
                                new ItemStack(bowl)));

    ItemStack meal = new(bowl);
    meal.Attributes["recipeCode"] = new StringAttribute("meatystew");
    Assert.IsTrue(rule.IsMatch(_resolver.Resolver, _resolver.CatDict, meal));
  }

  [TestMethod]
  public void EnumerateMatches() {
    string json = @"
    {
      category: {
        input: ""edible-meal-container""
      },
      attributes: [
        {
          path: [ ""recipeCode"" ],
          enumerateValues: [ ""meatystew"", ""vegetablestew"" ]
        },
      ]
    }
    ";
    Real.MatchRule rule =
        JsonObject.FromJson(json).AsObject<Real.MatchRule>(null, "gourmand");
    Block bowl = LoadAssets.GetBlock("game", "bowl-meal");
    List<ItemStack> matches =
        rule.EnumerateMatches(_resolver.Resolver, _resolver.CatDict);
    CollectionAssert.AreEquivalent(new CollectibleObject[] { bowl, bowl },
                                   matches.Select(s => s.Collectible).ToList());
    CollectionAssert.AreEquivalent(
        new string[] { "meatystew", "vegetablestew" },
        matches.Select(s => s.Attributes["recipeCode"].GetValue()).ToList());
  }

  [TestMethod]
  public void OutputCategories() {
    string json = @"
    {
      category: {
        input: ""edible-meal-container"",
        outputs: [ ""category-output"" ]
      },
      attributes: [
        {
          path: [ ""recipeCode"" ],
          outputs: [ ""attribute-output1"" ]
        },
        {
          path: [ ""recipeCode"" ],
          outputs: [ ""attribute-output2"" ]
        }
      ],
      deletes: [ ""rule-delete"" ],
      outputs: {
        ""rule-output"": [1]
      }
    }
    ";
    Real.MatchRule rule =
        JsonObject.FromJson(json).AsObject<Real.MatchRule>(null, "gourmand");
    CollectionAssert.AreEquivalent(
        new AssetLocation[] {
          new("gourmand", "category-output"),
          new("gourmand", "attribute-output1"),
          new("gourmand", "attribute-output2"),
          new("gourmand", "rule-delete"),
          new("gourmand", "rule-output"),
        },
        rule.OutputCategories.ToList());
  }
}
