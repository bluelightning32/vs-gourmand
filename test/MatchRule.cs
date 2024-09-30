using Microsoft.VisualStudio.TestTools.UnitTesting;

using PrefixClassName.MsTest;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace Gourmand.Test;

using Real = Gourmand;

[PrefixTestClass]
public class MatchRule {
  private static readonly Real.Collectibles.MatchResolver Resolver;

  static MatchRule() {
    Resolver = new(LoadAssets.Server.World, LoadAssets.Server.Api.Logger);

    string rulesJson = @"
    [
      {
        code : {
          match: ""game:bowl-meal"",
          type: ""block"",
          outputs: [ ""edible-meal-container"" ]
        }
      },
      {
        code : {
          match: ""game:fish-raw"",
          type: ""item"",
          outputs: [ ""meal-protein-base"" ]
        }
      },
      {
        code : {
          match: ""game:fish-cured"",
          type: ""item"",
          outputs: [ ""meal-protein-base"" ]
        }
      },
      {
        code : {
          match: ""game:fruit-*"",
          type: ""item"",
          outputs: [ ""meal-fruit"" ]
        }
      },
    ]";
    List<Real.Collectibles.MatchRule> rules =
        JsonUtil.ToObject<List<Real.Collectibles.MatchRule>>(rulesJson,
                                                             "gourmand");

    Resolver.Load(rules);
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
    Assert.IsTrue(
        rule.IsMatch(Resolver.Resolver, Resolver.CatDict, new ItemStack(bowl)));

    Item pineapple = LoadAssets.GetItem("game", "fruit-pineapple");
    Assert.IsFalse(rule.IsMatch(Resolver.Resolver, Resolver.CatDict,
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

    Assert.IsFalse(
        rule.IsMatch(Resolver.Resolver, Resolver.CatDict, new ItemStack(bowl)));

    ItemStack meal = new(bowl);
    meal.Attributes["recipeCode"] = new StringAttribute("meatystew");
    Assert.IsTrue(rule.IsMatch(Resolver.Resolver, Resolver.CatDict, meal));
  }

  [TestMethod]
  public void IsMatchContents() {
    string json = @"
    {
      category: {
        input: ""edible-meal-container""
      },
      attributes: [
        {
          path: [ ""recipeCode"" ],
        },
      ],
      contents: [
        {
          min: 2,
          max: 2,
          categories: [
            {
              input: ""meal-protein-base"",
            },
          ]
        },
        {
          min: 0,
          max: 1,
          categories: [
            {
              input: ""meal-fruit"",
            },
          ]
        },
      ]
    }
    ";
    Real.MatchRule rule =
        JsonObject.FromJson(json).AsObject<Real.MatchRule>(null, "gourmand");
    Block bowl = LoadAssets.GetBlock("game", "bowl-meal");
    ItemStack meal = new(bowl);
    meal.Attributes["recipeCode"] = new StringAttribute("meatystew");
    Item fish_raw = LoadAssets.GetItem("game", "fish-raw");
    Item pineapple = LoadAssets.GetItem("game", "fruit-pineapple");

    // Not a match without contents
    Assert.IsFalse(rule.IsMatch(Resolver.Resolver, Resolver.CatDict, meal));

    Real.ContentBuilder.SetContents(Resolver.Resolver, meal,
                                    new ItemStack[] {
                                      new(fish_raw),
                                      new(fish_raw),
                                      new(pineapple),
                                    });
    Assert.IsTrue(rule.IsMatch(Resolver.Resolver, Resolver.CatDict, meal));

    // Not a match with too much fruit.
    Real.ContentBuilder.SetContents(Resolver.Resolver, meal,
                                    new ItemStack[] {
                                      new(fish_raw),
                                      new(fish_raw),
                                      new(pineapple),
                                      new(pineapple),
                                    });
    Assert.IsFalse(rule.IsMatch(Resolver.Resolver, Resolver.CatDict, meal));
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
        rule.EnumerateMatches(Resolver.Resolver, Resolver.CatDict);
    CollectionAssert.AreEquivalent(new CollectibleObject[] { bowl, bowl },
                                   matches.Select(s => s.Collectible).ToList());
    CollectionAssert.AreEquivalent(
        new string[] { "meatystew", "vegetablestew" },
        matches.Select(s => s.Attributes["recipeCode"].GetValue()).ToList());
  }

  [TestMethod]
  public void EnumerateMatchesContents() {
    string json = @"
    {
      category: {
        input: ""edible-meal-container""
      },
      attributes: [
        {
          path: [ ""recipeCode"" ],
          enumerateValues: [ ""meatystew"" ]
        },
      ],
      contents: [
        {
          min: 2,
          max: 2,
          categories: [
            {
              input: ""meal-protein-base"",
            },
          ],
          enumerateMax: 1
        },
        {
          min: 0,
          max: 1,
          categories: [
            {
              input: ""meal-fruit"",
            },
          ],
          enumerateMax: 20
        },
      ]
    }
    ";
    Real.MatchRule rule =
        JsonObject.FromJson(json).AsObject<Real.MatchRule>(null, "gourmand");
    Block bowl = LoadAssets.GetBlock("game", "bowl-meal");
    Item pineapple = LoadAssets.GetItem("game", "fruit-pineapple");

    List<ItemStack> matches =
        rule.EnumerateMatches(Resolver.Resolver, Resolver.CatDict);
    Assert.IsTrue(matches.Select(s => s.Collectible).All(c => c == bowl));
    Assert.IsTrue(
        matches.Select(s => (string)s.Attributes["recipeCode"].GetValue())
            .All(r => r == "meatystew"));
    List<List<CollectibleObject>> matchContents =
        matches
            .Select(s => Real.ContentBuilder.GetContents(Resolver.Resolver, s)
                             .Select(s => s.Collectible)
                             .ToList())
            .ToList();
    Assert.IsTrue(matchContents.Any(c => c.Contains(pineapple)));
    Assert.IsTrue(matchContents.All(c => c.Count >= 2 && c.Count <= 3));
    Assert.IsTrue(matchContents.Any(c => c.Count == 2));
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
      contents: [
        {
          min: 2,
          max: 2,
          categories: [
            {
              input: ""meal-protein-base"",
              outputs: [ ""content-output1"" ]
            },
          ],
          enumerateMax: 1,
          countOutputs: [ ""content-count-output1"" ]
        },
        {
          min: 0,
          max: 1,
          categories: [
            {
              input: ""meal-fruit"",
              outputs: [ ""content-output2"" ]
            },
          ],
          enumerateMax: 20,
          countOutputs: [ ""content-count-output2"" ]
        },
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
          new("gourmand", "content-output1"),
          new("gourmand", "content-output2"),
          new("gourmand", "content-count-output1"),
          new("gourmand", "content-count-output2"),
          new("gourmand", "rule-delete"),
          new("gourmand", "rule-output"),
        },
        rule.OutputCategories.ToList());
  }

  [TestMethod]
  public void GetValueContents() {
    string json = @"
    {
      category: {
        input: ""edible-meal-container"",
      },
      attributes: [
        {
          path: [ ""recipeCode"" ],
          enumerateValues: [ ""meatystew"" ]
        },
      ],
      contents: [
        {
          min: 2,
          max: 2,
          categories: [
            {
              input: ""meal-protein-base"",
              distinctMax: 1,
              distinctOutputs: [ ""contains-meal-protein-base"" ]
            },
          ],
          enumerateMax: 1,
        },
        {
          min: 0,
          max: 1,
          categories: [
            {
              input: ""meal-fruit"",
              outputs: [ ""contains-meal-fruit"" ]
            },
          ],
          enumerateMax: 20,
        },
      ],
    }
    ";
    Real.MatchRule rule =
        JsonObject.FromJson(json).AsObject<Real.MatchRule>(null, "gourmand");
    Block bowl = LoadAssets.GetBlock("game", "bowl-meal");
    ItemStack meal = new(bowl);
    meal.Attributes["recipeCode"] = new StringAttribute("meatystew");
    Item fish_raw = LoadAssets.GetItem("game", "fish-raw");
    Item pineapple = LoadAssets.GetItem("game", "fruit-pineapple");

    Real.ContentBuilder.SetContents(Resolver.Resolver, meal,
                                    new ItemStack[] {
                                      new(fish_raw),
                                      new(fish_raw),
                                    });
    Assert.IsTrue(Real.CategoryValue.ValuesEqual(
        new IAttribute[] { new StringAttribute(fish_raw.Code.ToString()) },
        rule.GetValue(
            Resolver.Resolver, Resolver.CatDict,
            new AssetLocation("gourmand", "contains-meal-protein-base"),
            meal)));
    Assert.IsTrue(Real.CategoryValue.ValuesEqual(
        new IAttribute[] {},
        rule.GetValue(Resolver.Resolver, Resolver.CatDict,
                      new AssetLocation("gourmand", "contains-meal-fruit"),
                      meal)));

    Real.ContentBuilder.SetContents(Resolver.Resolver, meal,
                                    new ItemStack[] {
                                      new(fish_raw),
                                      new(fish_raw),
                                      new(pineapple),
                                    });
    Assert.IsTrue(Real.CategoryValue.ValuesEqual(
        new IAttribute[] { new StringAttribute(fish_raw.Code.ToString()) },
        rule.GetValue(
            Resolver.Resolver, Resolver.CatDict,
            new AssetLocation("gourmand", "contains-meal-protein-base"),
            meal)));
    Assert.IsTrue(Real.CategoryValue.ValuesEqual(
        new IAttribute[] { new StringAttribute(pineapple.Code.ToString()) },
        rule.GetValue(Resolver.Resolver, Resolver.CatDict,
                      new AssetLocation("gourmand", "contains-meal-fruit"),
                      meal)));
  }

  [TestMethod]
  public void GetValueOutputs() {
    string json = @"
    {
      category: {
        input: ""edible-meal-container"",
      },
      outputs: {
        ""edible"": [ 1 ]
      }
    }
    ";
    Real.MatchRule rule =
        JsonObject.FromJson(json).AsObject<Real.MatchRule>(null, "gourmand");
    Block bowl = LoadAssets.GetBlock("game", "bowl-meal");
    ItemStack meal = new(bowl);

    Assert.IsTrue(Real.CategoryValue.ValuesEqual(
        new IAttribute[] { new LongAttribute(1) },
        rule.GetValue(Resolver.Resolver, Resolver.CatDict,
                      new AssetLocation("gourmand", "edible"), meal)));
  }

  [TestMethod]
  public void ResolveImports() {
    string json = @"
    {
      category: {
        input: ""edible-meal-container"",
      },
      importRecipe: ""meatystew"",
      contents: [
        {
          code: ""fruit-extra"",
          min: 1,
          max: 100,
          categories: [
            {
              input: ""gourmandimportrecipe:meatystew.fruit-extra"",
            },
          ],
        },
        {
          min: 0,
          max: 101,
          categories: [
            {
              input: ""meal-fruit"",
              outputs: [ ""contains-meal-fruit"" ]
            },
          ],
          enumerateMax: 20,
        },
      ],
    }
    ";
    Real.MatchRule rule =
        JsonObject.FromJson(json).AsObject<Real.MatchRule>(null, "gourmand");
    RecipeRegistrySystem cooking =
        LoadAssets.Server.Api.ModLoader.GetModSystem<RecipeRegistrySystem>();
    List<string> implicits =
        rule.ResolveImports(cooking, LoadAssets.Server.Api.Logger);

    CollectionAssert.Contains(implicits, "protein-base");

    Real.SlotCondition protein_base =
        rule.Slots.First(s => s.Code == "protein-base");
    Assert.AreEqual(1, protein_base.Categories.Length);
    Assert.AreEqual(Real.Collectibles.CategoryDict.ImplictIngredientCategory(
                        "meatystew", "protein-base"),
                    protein_base.Categories[0].Input);
    Assert.AreEqual(2, protein_base.Min);
    Assert.AreEqual(2, protein_base.Max);
    Assert.IsTrue(protein_base.Categories[0].EnumeratePerDistinct > 0);

    Real.SlotCondition fruit_extra =
        rule.Slots.First(s => s.Code == "fruit-extra");
    Assert.AreEqual(1, fruit_extra.Categories.Length);
    Assert.AreEqual(Real.Collectibles.CategoryDict.ImplictIngredientCategory(
                        "meatystew", "fruit-extra"),
                    fruit_extra.Categories[0].Input);
    Assert.AreEqual(100, fruit_extra.Max);

    Real.SlotCondition meal_fruit = rule.Slots.First(s => s.Max == 101);
    Assert.AreEqual(1, meal_fruit.Categories.Length);
    Assert.AreEqual(new AssetLocation("gourmand", "meal-fruit"),
                    meal_fruit.Categories[0].Input);
    Assert.IsNull(meal_fruit.Code);
  }

  [TestMethod]
  public void ResolveImportsNoContents() {
    string json = @"
    {
      category: {
        input: ""edible-meal-container"",
      },
      importRecipe: ""meatystew""
    }
    ";
    Real.MatchRule rule =
        JsonObject.FromJson(json).AsObject<Real.MatchRule>(null, "gourmand");
    RecipeRegistrySystem cooking =
        LoadAssets.Server.Api.ModLoader.GetModSystem<RecipeRegistrySystem>();
    List<string> implicits =
        rule.ResolveImports(cooking, LoadAssets.Server.Api.Logger);

    CollectionAssert.Contains(implicits, "protein-base");

    Real.SlotCondition protein_base =
        rule.Slots.First(s => s.Code == "protein-base");
    Assert.AreEqual(1, protein_base.Categories.Length);
    Assert.AreEqual(Real.Collectibles.CategoryDict.ImplictIngredientCategory(
                        "meatystew", "protein-base"),
                    protein_base.Categories[0].Input);
    Assert.AreEqual(2, protein_base.Min);
    Assert.AreEqual(2, protein_base.Max);
  }
}
