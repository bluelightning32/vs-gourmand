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
    // The first rule does not depend on anything. So it should be processed
    // first.
    Assert.AreEqual(rules[0], sorter.Result[0]);
    Assert.AreEqual(rules[1], sorter.Result[1]);
    sorter = new(rules.AsEnumerable().Reverse());
    Assert.AreEqual(rules[0], sorter.Result[0]);
    Assert.AreEqual(rules[1], sorter.Result[1]);
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
    // The final rule does not depend on anything. So it should be processed
    // first.
    Assert.AreEqual(rules[3], sorter.Result[0]);
    Assert.IsTrue(sorter.Result[1] == rules[1] || sorter.Result[1] == rules[2]);
    Assert.IsTrue(sorter.Result[2] == rules[1] || sorter.Result[2] == rules[2]);
    Assert.AreNotEqual(sorter.Result[1], sorter.Result[2]);
    // The start rule depends on everything. So it should be processed last.
    Assert.AreEqual(rules[0], sorter.Result[3]);

    sorter = new(rules);
    Assert.AreEqual(rules[3], sorter.Result[0]);
    Assert.IsTrue(sorter.Result[1] == rules[1] || sorter.Result[1] == rules[2]);
    Assert.IsTrue(sorter.Result[2] == rules[1] || sorter.Result[2] == rules[2]);
    Assert.AreNotEqual(sorter.Result[1], sorter.Result[2]);
    Assert.AreEqual(rules[0], sorter.Result[3]);
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
