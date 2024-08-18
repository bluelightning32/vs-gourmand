using Microsoft.VisualStudio.TestTools.UnitTesting;

using PrefixClassName.MsTest;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand.Tests;

[PrefixTestClass]
public class CodeCondition {
  private readonly Gourmand.MatchResolver _resolver;

  public CodeCondition() { _resolver = new(LoadAssets.Server.World); }

  [TestMethod]
  public void JsonParseOutputOptional() {
    string json = @"
    {
      match: ""fruit"",
    }
    ";
    Gourmand.CodeCondition cond =
        JsonObject.FromJson(json).AsObject<Gourmand.CodeCondition>(null,
                                                                   "gourmand");
    Assert.IsNull(cond.Output);
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
    JsonObject.FromJson(json).AsObject<Gourmand.CodeCondition>(null,
                                                               "gourmand");
  }

  [TestMethod]
  public void EnumerateMatchesExistingNull() {
    string json = @"
    {
      match: ""game:fruit-*"",
    }
    ";
    Gourmand.CodeCondition cond =
        JsonObject.FromJson(json).AsObject<Gourmand.CodeCondition>(null,
                                                                   "gourmand");
    List<CollectibleObject> matches = null;
    cond.EnumerateMatches(_resolver, EnumItemClass.Item, ref matches);

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
      match: ""game:fruit-*"",
    }
    ";
    Gourmand.CodeCondition cond =
        JsonObject.FromJson(json).AsObject<Gourmand.CodeCondition>(null,
                                                                   "gourmand");
    List<CollectibleObject> matches =
        new() { LoadAssets.GetItem("game", "fruit-pineapple"),
                LoadAssets.GetItem("game", "firestarter") };
    cond.EnumerateMatches(_resolver, EnumItemClass.Item, ref matches);

    CollectionAssert.Contains(matches,
                              LoadAssets.GetItem("game", "fruit-pineapple"));
    CollectionAssert.DoesNotContain(
        matches, LoadAssets.GetItem("game", "fruit-blueberry"));
    CollectionAssert.DoesNotContain(matches,
                                    LoadAssets.GetItem("game", "firestarter"));
  }

  [TestMethod]
  public void GetMatchValue() {
    string json = @"
    {
      match: ""game:fruit-*"",
    }
    ";
    Gourmand.CodeCondition cond =
        JsonObject.FromJson(json).AsObject<Gourmand.CodeCondition>(null,
                                                                   "gourmand");
    Assert.AreEqual(
        new StringAttribute("game:fruit-pineapple").ToString(),
        cond.GetMatchValue(LoadAssets.GetItem("game", "fruit-pineapple"))
            .ToString());
  }
}
