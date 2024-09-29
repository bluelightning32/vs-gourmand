using Microsoft.VisualStudio.TestTools.UnitTesting;

using PrefixClassName.MsTest;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand.Test;

using Real = Gourmand;

[PrefixTestClass]
public class FoodAchievements {
  private static readonly Real.CategoryDict CatDict;
  private static readonly Real.FoodAchievements LoadedAchievements;

  static FoodAchievements() {
    string collectibleJson = @"
    [
      {
        code : {
          match: ""*:*"",
          type: ""item"",
          outputs: [""edible-code""]
        },
        nutritionProps: {
          category: {
            outputs: [""edible""]
          }
        },
        outputs: {
          ""edible-code"": [ ""item"" ]
        }
      },
      {
        categories: [
          {
            input: ""edible"",
          },
          {
            input: ""edible-code"",
            outputs: [ ""achievement-fruit"" ]
          },
        ],
        nutritionProps: {
          category: {
            value: ""Fruit""
          }
        },
      },
      {
        categories: [
          {
            input: ""edible"",
          },
          {
            input: ""edible-code"",
            outputs: [ ""achievement-protein"" ]
          },
        ],
        nutritionProps: {
          category: {
            value: ""Protein""
          }
        },
      },
    ]";
    string stackJson = @"
    [
    ]";
    List<Real.Collectibles.MatchRule> collectibleRules =
        JsonUtil.ToObject<List<Real.Collectibles.MatchRule>>(collectibleJson,
                                                             "gourmand");
    List<Real.MatchRule> stackRules =
        JsonUtil.ToObject<List<Real.MatchRule>>(stackJson, "gourmand");

    CatDict = new(LoadAssets.Server.World, collectibleRules, stackRules);
    string achievementsJson = @"
    {
      achievements: {
        ""achievement-fruit"": {
          points: 2,
          bonusAt: 1,
          bonus: 90,
          add: [
            {
              dependsOn: [ ""survival"" ],
              points: 1,
              bonusAt: 1,
              bonus: 10,
            },
            {
              dependsOn: [ ""notinstalled"" ],
              points: 1000,
              bonusAt: 1000,
              bonus: 1000,
            }
          ]
        },
        ""achievement-protein"": {
          points: 1,
        },
        ""achievement-not-installed"": {
          dependsOn: [ ""notinstalled"" ],
          points: 99,
        }
      },
      healthPoints: [
        {
          points: 4,
          health: 10,
        },
        {
          points: 5,
          health: 20,
        },
        {
          points: 10,
          health: 20,
          add: [
            {
              dependsOn: [ ""survival"" ],
              points: 1,
              health: 10,
            }
          ]
        },
        {
          dependsOn: [ ""not-installed"" ],
          points: 99,
          health: 99,
        }
      ]
    }";
    LoadedAchievements =
        JsonUtil.ToObject<Real.FoodAchievements>(achievementsJson, "gourmand");
    LoadedAchievements.Resolve("gourmand", LoadAssets.Server.Api.ModLoader);
  }

  [TestMethod]
  public void GetPointsForAchievements() {
    Assert.AreEqual(0, LoadedAchievements.GetHealthForPoints(1));
    Assert.AreEqual(10, LoadedAchievements.GetHealthForPoints(4));
    Assert.AreEqual(20, LoadedAchievements.GetHealthForPoints(5));
    // Interpolate between 20 and 30
    Assert.AreEqual(25, LoadedAchievements.GetHealthForPoints(8));
    Assert.AreEqual(30, LoadedAchievements.GetHealthForPoints(11));
    // Extrapolate after the last piece
    Assert.AreEqual(35, LoadedAchievements.GetHealthForPoints(14));
  }

  [TestMethod]
  public void UpdateAchievements() {
    Item pineapple = LoadAssets.GetItem("game", "fruit-pineapple");
    Item cranberry = LoadAssets.GetItem("game", "fruit-cranberry");
    Item blueberry = LoadAssets.GetItem("game", "fruit-blueberry");
    Item fish_raw = LoadAssets.GetItem("game", "fish-raw");
    Item fish_cooked = LoadAssets.GetItem("game", "fish-cooked");
    TreeAttribute achieved = new();
    int points = 0;

    Assert.AreNotEqual(0, LoadedAchievements.AddAchievements(
                              LoadAssets.Server.World, CatDict, achieved,
                              new ItemStack(pineapple)));
    points += 3;
    Assert.AreEqual(
        points, LoadedAchievements.GetPointsForAchievements(null, achieved));

    // No new points for eating the same item again
    Assert.AreEqual(0, LoadedAchievements.AddAchievements(
                           LoadAssets.Server.World, CatDict, achieved,
                           new ItemStack(pineapple)));

    Assert.AreNotEqual(0, LoadedAchievements.AddAchievements(
                              LoadAssets.Server.World, CatDict, achieved,
                              new ItemStack(cranberry)));
    points += 3;
    // Completion bonus
    points += 100;
    Assert.AreEqual(
        points, LoadedAchievements.GetPointsForAchievements(null, achieved));

    Assert.AreNotEqual(0, LoadedAchievements.AddAchievements(
                              LoadAssets.Server.World, CatDict, achieved,
                              new ItemStack(blueberry)));
    points += 3;
    Assert.AreEqual(
        points, LoadedAchievements.GetPointsForAchievements(null, achieved));

    Assert.AreNotEqual(0, LoadedAchievements.AddAchievements(
                              LoadAssets.Server.World, CatDict, achieved,
                              new ItemStack(fish_raw)));
    points += 1;
    Assert.AreEqual(
        points, LoadedAchievements.GetPointsForAchievements(null, achieved));

    Assert.AreNotEqual(0, LoadedAchievements.AddAchievements(
                              LoadAssets.Server.World, CatDict, achieved,
                              new ItemStack(fish_cooked)));
    points += 1;
    Assert.AreEqual(
        points, LoadedAchievements.GetPointsForAchievements(null, achieved));
  }

  [TestMethod]
  public void DependsOn() {
    TreeAttribute moddata = new();
    Dictionary<AssetLocation, Tuple<int, AchievementPoints>> achievements =
        LoadedAchievements.GetAchievementStats(moddata);
    Assert.IsFalse(achievements.ContainsKey(
        new AssetLocation("gourmand", "achievement-not-installed")));
    Assert.IsTrue(achievements.TryGetValue(
        new AssetLocation("gourmand", "achievement-fruit"),
        out Tuple<int, AchievementPoints> fruit));
    Assert.AreEqual(3, fruit.Item2.Points);
    Assert.AreEqual(2, fruit.Item2.BonusAt);
    Assert.AreEqual(100, fruit.Item2.Bonus);
  }
}
