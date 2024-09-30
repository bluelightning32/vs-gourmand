using Microsoft.VisualStudio.TestTools.UnitTesting;

using PrefixClassName.MsTest;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand.Test.Collectibles;

using Real = Gourmand.Collectibles;

[PrefixTestClass]
public class CodeCondition {
  private readonly Real.MatchResolver _resolver;

  public CodeCondition() {
    _resolver = new(LoadAssets.Server.World, LoadAssets.Server.Api.Logger);
  }

  [TestMethod]
  public void JsonParseOutputOptional() {
    string json = @"
    {
      match: ""fruit"",
      type: ""item""
    }
    ";
    Real.CodeCondition cond =
        JsonObject.FromJson(json).AsObject<Real.CodeCondition>(null,
                                                               "gourmand");
    Assert.AreEqual(Array.Empty<AssetLocation>(), cond.Outputs);
    Assert.AreEqual(new AssetLocation("gourmand", "fruit"), cond.Match);
  }

  [TestMethod]
  [ExpectedException(typeof(Newtonsoft.Json.JsonSerializationException),
                     "Required property 'Match' not found in JSON")]
  public void JsonParseMatchRequired() {
    string json = @"
    {
    }
    ";
    JsonObject.FromJson(json).AsObject<Real.CodeCondition>(null, "gourmand");
  }

  [TestMethod]
  public void EnumerateMatchesExistingNull() {
    string json = @"
    {
      match: ""game:fruit-*"", type: ""item""
    }
    ";
    Real.CodeCondition cond =
        JsonObject.FromJson(json).AsObject<Real.CodeCondition>(null,
                                                               "gourmand");
    List<CollectibleObject> matches = null;
    cond.EnumerateMatches(_resolver, ref matches);

    CollectionAssert.Contains(matches,
                              LoadAssets.GetItem("game", "fruit-pineapple"));
    CollectionAssert.Contains(matches,
                              LoadAssets.GetItem("game", "fruit-blueberry"));
    CollectionAssert.DoesNotContain(matches,
                                    LoadAssets.GetItem("game", "firestarter"));
  }

  [TestMethod]
  public void EnumerateMatchesExisting() {
    string json = @"
    {
      match: ""game:fruit-*"", type: ""item""
    }
    ";
    Real.CodeCondition cond =
        JsonObject.FromJson(json).AsObject<Real.CodeCondition>(null,
                                                               "gourmand");
    List<CollectibleObject> matches =
        new() { LoadAssets.GetItem("game", "fruit-pineapple"),
                LoadAssets.GetItem("game", "firestarter") };
    cond.EnumerateMatches(_resolver, ref matches);

    CollectionAssert.Contains(matches,
                              LoadAssets.GetItem("game", "fruit-pineapple"));
    CollectionAssert.DoesNotContain(
        matches, LoadAssets.GetItem("game", "fruit-blueberry"));
    CollectionAssert.DoesNotContain(matches,
                                    LoadAssets.GetItem("game", "firestarter"));
  }

  [TestMethod]
  public void GetCategoriesOutputEmpty() {
    string json = @"
    {
      match: ""game:fruit-*"", type: ""item""
    }
    ";
    Real.CodeCondition cond =
        JsonObject.FromJson(json).AsObject<Real.CodeCondition>(null,
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
      match: ""game:fruit-*"", type: ""item"",
      outputs: [""category1"", ""category2""]
    }
    ";
    Real.CodeCondition cond =
        JsonObject.FromJson(json).AsObject<Real.CodeCondition>(null,
                                                               "gourmand");
    Dictionary<AssetLocation, IAttribute[]> categories = new(cond.GetCategories(
        _resolver.CatDict, LoadAssets.GetItem("game", "fruit-pineapple")));
    LoadAssets.AssertCategoriesEqual(
        new Dictionary<AssetLocation, IAttribute> {
          { new AssetLocation("gourmand", "category1"),
            new StringAttribute("game:fruit-pineapple") },
          { new AssetLocation("gourmand", "category2"),
            new StringAttribute("game:fruit-pineapple") }
        },
        categories);
  }

  [TestMethod]
  public void Categories2() {
    string json = @"
    {
      match: ""game:fruit-*"", type: ""item"",
      outputs: [""category1"", ""category2""]
    }
    ";
    Real.CodeCondition cond =
        JsonObject.FromJson(json).AsObject<Real.CodeCondition>(null,
                                                               "gourmand");
    CollectionAssert.AreEquivalent(
        new List<AssetLocation>() {
          new("gourmand", "category1"),
          new("gourmand", "category2"),
        },
        cond.Categories.ToList());
  }
}
