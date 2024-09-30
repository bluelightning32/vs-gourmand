using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using PrefixClassName.MsTest;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand.Test;

using Real = Gourmand;

[PrefixTestClass]
public class CategoryDict {
  private static readonly Real.CategoryDict CatDict;

  static CategoryDict() {
    string collectibleJson = @"
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
      {
        code : {
          match: ""game:fruit-cranberry"",
          type: ""item"",
          outputs: [ ""fruit-cranberry"" ]
        }
      },
      {
        nutritionProps: {
          category: {
            outputs: [""edible""]
          }
        }
      }
    ]";
    string stackJson = @"
    [
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
                enumArrangement: ""repeated"",
                distinctOutputs: [ ""contains-meal-protein-base""]
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
                outputs: [ ""contains-meal-fruit""]
              },
            ],
            enumerateMax: 20,
          },
        ],
        outputs: {
          ""edible"": [ ""game:bowl-meal"" ]
        }
      },
      {
        priority: 2,
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
                enumArrangement: ""repeated"",
              },
            ],
            enumerateMax: 1,
          },
          {
            min: 1,
            max: 1,
            categories: [
              {
                input: ""fruit-cranberry"",
              },
            ],
            enumerateMax: 20,
          },
        ],
        deletes: [ ""edible"" ]
      }
    ]";
    List<Real.Collectibles.MatchRule> collectibleRules =
        JsonUtil.ToObject<List<Real.Collectibles.MatchRule>>(collectibleJson,
                                                             "gourmand");
    List<Real.MatchRule> stackRules =
        JsonUtil.ToObject<List<Real.MatchRule>>(stackJson, "gourmand");

    CatDict = new(LoadAssets.Server.World, LoadAssets.Server.Api.Logger,
                  collectibleRules, stackRules);
  }

  [TestMethod]
  public void InCategory() {
    Block bowl = LoadAssets.GetBlock("game", "bowl-meal");
    ItemStack meal = new(bowl);
    meal.Attributes["recipeCode"] = new StringAttribute("meatystew");
    Item fish_raw = LoadAssets.GetItem("game", "fish-raw");
    Item tongs = LoadAssets.GetItem("game", "tongs");
    Item pineapple = LoadAssets.GetItem("game", "fruit-pineapple");
    Item cranberry = LoadAssets.GetItem("game", "fruit-cranberry");
    AssetLocation edible = new("gourmand", "edible");

    // Test a collectible MatchRule
    Assert.IsTrue(CatDict.InCategory(LoadAssets.Server.World, edible,
                                     new ItemStack(pineapple)));
    Assert.IsTrue(CatDict.InCategory(LoadAssets.Server.World, edible,
                                     new ItemStack(fish_raw)));
    Assert.IsFalse(CatDict.InCategory(LoadAssets.Server.World, edible,
                                      new ItemStack(tongs)));

    // Test a stack MatchRule
    Real.ContentBuilder.SetContents(LoadAssets.Server.World, meal,
                                    new ItemStack[] {
                                      new(fish_raw),
                                      new(fish_raw),
                                    });
    Assert.IsTrue(CatDict.InCategory(LoadAssets.Server.World, edible, meal));

    // Test a stack MatchRule
    Real.ContentBuilder.SetContents(LoadAssets.Server.World, meal,
                                    new ItemStack[] {
                                      new(fish_raw),
                                      new(fish_raw),
                                      new(pineapple),
                                    });
    Assert.IsTrue(CatDict.InCategory(LoadAssets.Server.World, edible, meal));

    // Test a delete stack MatchRule
    Real.ContentBuilder.SetContents(LoadAssets.Server.World, meal,
                                    new ItemStack[] {
                                      new(fish_raw),
                                      new(fish_raw),
                                      new(cranberry),
                                    });
    Assert.IsFalse(CatDict.InCategory(LoadAssets.Server.World, edible, meal));
  }

  private static void TestGetValue(Real.CategoryDict catDict) {
    Block bowl = LoadAssets.GetBlock("game", "bowl-meal");
    ItemStack meal = new(bowl);
    meal.Attributes["recipeCode"] = new StringAttribute("meatystew");
    Item fish_raw = LoadAssets.GetItem("game", "fish-raw");
    Item tongs = LoadAssets.GetItem("game", "tongs");
    Item pineapple = LoadAssets.GetItem("game", "fruit-pineapple");
    Item cranberry = LoadAssets.GetItem("game", "fruit-cranberry");
    AssetLocation edible = new("gourmand", "edible");

    // Test a collectible MatchRule
    Assert.IsTrue(Real.CategoryValue.ValuesEqual(
        new IAttribute[] { new StringAttribute("Fruit") },
        catDict
            .GetValue(LoadAssets.Server.World, edible, new ItemStack(pineapple))
            .Value));
    // Test a collectible that nothing matches
    Assert.IsNull(catDict.GetValue(LoadAssets.Server.World, edible,
                                   new ItemStack(tongs)));

    // Test a stack MatchRule
    Real.ContentBuilder.SetContents(LoadAssets.Server.World, meal,
                                    new ItemStack[] {
                                      new(fish_raw),
                                      new(fish_raw),
                                    });
    Real.CategoryValue value =
        catDict.GetValue(LoadAssets.Server.World, edible, meal);
    Assert.IsTrue(Real.CategoryValue.ValuesEqual(
        new IAttribute[] { new StringAttribute("game:bowl-meal") },
        value.Value));

    // Test a delete stack MatchRule
    Real.ContentBuilder.SetContents(LoadAssets.Server.World, meal,
                                    new ItemStack[] {
                                      new(fish_raw),
                                      new(fish_raw),
                                      new(cranberry),
                                    });
    Assert.IsNull(
        catDict.GetValue(LoadAssets.Server.World, edible, meal).Value);
  }

  [TestMethod]
  public void GetValue() { TestGetValue(CatDict); }

  [TestMethod]
  public void EnumerateMatches() {
    AssetLocation edible = new("gourmand", "edible");
    List<ItemStack> matches =
        CatDict.EnumerateMatches(LoadAssets.Server.World, edible);
    Block bowl = LoadAssets.GetBlock("game", "bowl-meal");
    Item pineapple = LoadAssets.GetItem("game", "fruit-pineapple");
    Item cranberry = LoadAssets.GetItem("game", "fruit-cranberry");

    Assert.IsTrue(matches.Select(s => s.Collectible)
                      .All(c => c == bowl || c.NutritionProps != null));
    CollectionAssert.Contains(matches.Select(s => s.Collectible).ToList(),
                              pineapple);

    List<List<CollectibleObject>> matchContents =
        matches
            .Select(
                s => Real.ContentBuilder.GetContents(LoadAssets.Server.World, s)
                         .Select(s => s.Collectible)
                         .ToList())
            .Where(c => c.Count > 0)
            .ToList();
    Assert.IsTrue(matchContents.Any(c => c.Contains(pineapple)));
    Assert.IsFalse(matchContents.Any(c => c.Contains(cranberry)));
  }

  [TestMethod]
  public void Serialize() {

    using (MemoryStream ms = new()) {
      using (BinaryWriter writer = new(ms, Encoding.UTF8, true)) {
        CatDict.ToBytes(writer);
      }
      ms.Position = 0;
      using (BinaryReader reader = new(ms)) {
        Real.CategoryDict restored = new();
        restored.FromBytes(reader, LoadAssets.Server.World);
        TestGetValue(restored);
      }
    }
  }

  [TestMethod]
  public void SerializeBytes() {
    CatDict.ToBytes(LoadAssets.Server.World, out byte[] bytes,
                    out int quantity);
    Real.CategoryDict restored = new();
    restored.FromBytes(LoadAssets.Server.World, quantity, bytes);
    TestGetValue(restored);
  }
}
