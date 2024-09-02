using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using PrefixClassName.MsTest;

using Vintagestory.API.Common;

namespace Gourmand.Test.Collectibles;

using Real = Gourmand.Collectibles;

[PrefixTestClass]
public class CategoryDict {
  private static readonly Real.CategoryDict CatDict;

  static CategoryDict() {
    Real.MatchResolver resolver = new(LoadAssets.Server.World);

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
          match: ""game:fruit-*"",
          type: ""item"",
          outputs: [ ""meal-fruit"" ]
        }
      },
      {
        priority: 2,
        code : {
          match: ""game:fruit-cranberry"",
          type: ""item""
        },
        deletes: [ ""meal-fruit"" ]
      }
    ]";
    List<Real.MatchRule> rules =
        JsonUtil.ToObject<List<Real.MatchRule>>(rulesJson, "gourmand");

    CatDict = resolver.Load(rules);
  }

  [TestMethod]
  public void Serialize() {
    Real.CategoryDict restored = new();
    using (MemoryStream ms = new()) {
      using (BinaryWriter writer = new(ms, Encoding.UTF8, true)) {
        CatDict.ToBytes(writer);
      }
      ms.Position = 0;
      using (BinaryReader reader = new(ms)) {
        restored.FromBytes(reader, LoadAssets.Server.World);
      }
    }

    Block bowl = LoadAssets.GetBlock("game", "bowl-meal");
    Assert.AreEqual(
        CatDict.GetValue(new AssetLocation("gourmand", "edible-meal-container"),
                         bowl),
        restored.GetValue(
            new AssetLocation("gourmand", "edible-meal-container"), bowl));

    Item pineapple = LoadAssets.GetItem("game", "fruit-pineapple");
    Assert.AreEqual(
        CatDict.GetValue(new AssetLocation("gourmand", "meal-fruit"),
                         pineapple),
        restored.GetValue(new AssetLocation("gourmand", "meal-fruit"),
                          pineapple));

    Item cranberry = LoadAssets.GetItem("game", "fruit-cranberry");
    Assert.AreEqual(
        CatDict.GetValue(new AssetLocation("gourmand", "meal-fruit"),
                         cranberry),
        restored.GetValue(new AssetLocation("gourmand", "meal-fruit"),
                          cranberry));
  }
}
