using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using PrefixClassName.MsTest;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.Common;

namespace Gourmand.Tests;

[PrefixTestClass]
public class CollectibleMatchRuleSorter {
  public CollectibleMatchRuleSorter() {}

  private static List<Gourmand.CollectibleMatchRule> ParseRules(string json) {
    return JsonUtil.ToObject<List<Gourmand.CollectibleMatchRule>>(
        json, "gourmand",
        CollectibleMatchRuleConverter.AddConverter(EnumItemClass.Item));
  }

  [TestMethod]
  public void OrderTwo() {
    string json = @"
    [
      {
        outputs: {
          ""cat1"": [ 0 ]
        }
      },
      {
        categories: [ { input: ""cat1"" } ],
        outputs: {
          ""cat2"": [ 0 ]
        }
      }
    ]";
    List<Gourmand.CollectibleMatchRule> rules = ParseRules(json);
    Gourmand.CollectibleMatchRuleSorter sorter = new(rules);
    void Validate() {
      // The first rule does not depend on anything. So it should be processed
      // first.
      Assert.AreEqual(rules[0], sorter.Result[0].Rule);
      Assert.AreEqual(new AssetLocation("gourmand", "cat1"),
                      sorter.Result[1].Category);
      Assert.AreEqual(rules[1], sorter.Result[2].Rule);
      Assert.AreEqual(new AssetLocation("gourmand", "cat2"),
                      sorter.Result[3].Category);
    }
    Validate();

    sorter = new(rules.AsEnumerable().Reverse());
    Validate();
  }

  [TestMethod]
  public void OrderDiamond() {
    string json = @"
    [
      {
        categories: [ { input: ""A"" }, { input: ""B"" } ],
        outputs: {
          ""start"": [ 0 ]
        }
      },
      {
        categories: [ { input: ""final"" } ],
        outputs: {
          ""A"": [ 0 ]
        }
      },
      {
        categories: [ { input: ""final"" } ],
        outputs: {
          ""B"": [ 0 ]
        }
      },
      {
        outputs: {
          ""final"": [ 0 ]
        }
      },
    ]";
    List<Gourmand.CollectibleMatchRule> rules = ParseRules(json);
    Gourmand.CollectibleMatchRuleSorter sorter =
        new(rules.AsEnumerable().Reverse());
    void Validate() {
      // The final rule does not depend on anything. So it should be processed
      // first.
      Assert.AreEqual(rules[3], sorter.Result[0].Rule);
      Assert.AreEqual(new AssetLocation("gourmand", "final"),
                      sorter.Result[1].Category);
      Assert.IsTrue(sorter.Result[2].Rule == rules[1] ||
                    sorter.Result[2].Rule == rules[2]);
      Assert.IsTrue(
          sorter.Result[3].Category == new AssetLocation("gourmand", "a") ||
          sorter.Result[3].Category == new AssetLocation("gourmand", "b"));
      Assert.IsTrue(sorter.Result[4].Rule == rules[1] ||
                    sorter.Result[4].Rule == rules[2]);
      Assert.IsTrue(
          sorter.Result[5].Category == new AssetLocation("gourmand", "a") ||
          sorter.Result[5].Category == new AssetLocation("gourmand", "b"));
      Assert.AreNotEqual(sorter.Result[2].Rule, sorter.Result[4].Rule);
      Assert.AreNotEqual(sorter.Result[3].Category, sorter.Result[5].Category);
      // The start rule depends on everything. So it should be processed last.
      Assert.AreEqual(rules[0], sorter.Result[6].Rule);
      Assert.AreEqual(new AssetLocation("gourmand", "start"),
                      sorter.Result[7].Category);
    }
    Validate();

    sorter = new(rules);
    Validate();
  }

  [TestMethod]
  [ExpectedException(typeof(InvalidOperationException),
                     "Cycle in category dependencies")]
  public void DetectCycle() {
    string json = @"
    [
      {
        categories: [ { input: ""cat2"" } ],
        outputs: {
          ""cat1"": [ 0 ]
        }
      },
      {
        categories: [ { input: ""cat1"" } ],
        outputs: {
          ""cat2"": [ 0 ]
        }
      }
    ]";
    List<Gourmand.CollectibleMatchRule> rules = ParseRules(json);
    _ = new Gourmand.CollectibleMatchRuleSorter(rules.AsEnumerable().Reverse());
  }
}
