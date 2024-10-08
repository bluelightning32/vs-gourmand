using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json;

using PrefixClassName.MsTest;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand.Test;

using Real = Gourmand;

[PrefixTestClass]
public class AttributeCondition {
  private readonly Real.Collectibles.MatchResolver _resolver;

  public AttributeCondition() {
    _resolver = new(LoadAssets.Server.World, LoadAssets.Server.Api.Logger);
  }

  [TestMethod]
  public void IsMatch() {
    string json = @"
    {
      path: [""pieSize""],
      value: 1
    }
    ";
    Real.AttributeCondition cond =
        JsonObject.FromJson(json).AsObject<Real.AttributeCondition>(null,
                                                                    "gourmand");

    ItemStack stack = new(LoadAssets.GetBlock("game", "pie-perfect"));
    Assert.IsFalse(cond.IsMatch(_resolver.Resolver, _resolver.CatDict, stack));

    stack.Attributes.SetInt("pieSize", 1);
    Assert.IsTrue(cond.IsMatch(_resolver.Resolver, _resolver.CatDict, stack));

    stack.Attributes.SetLong("pieSize", 1);
    Assert.IsTrue(cond.IsMatch(_resolver.Resolver, _resolver.CatDict, stack));

    stack.Attributes.SetString("pieSize", "1");
    Assert.IsTrue(cond.IsMatch(_resolver.Resolver, _resolver.CatDict, stack));

    stack.Attributes.SetLong("pieSize", 2);
    Assert.IsFalse(cond.IsMatch(_resolver.Resolver, _resolver.CatDict, stack));

    stack.Attributes["pieSize"] = new IntArrayAttribute(new int[] { 1 });
    Assert.IsFalse(cond.IsMatch(_resolver.Resolver, _resolver.CatDict, stack));
  }

  [TestMethod]
  public void IsMatchNoCast() {
    string json = @"
    {
      path: [""pieSize""],
      value: 1,
      allowcast: false
    }
    ";
    Real.AttributeCondition cond =
        JsonObject.FromJson(json).AsObject<Real.AttributeCondition>(null,
                                                                    "gourmand");

    ItemStack stack = new(LoadAssets.GetBlock("game", "pie-perfect"));
    Assert.IsFalse(cond.IsMatch(_resolver.Resolver, _resolver.CatDict, stack));

    stack.Attributes.SetInt("pieSize", 1);
    Assert.IsFalse(cond.IsMatch(_resolver.Resolver, _resolver.CatDict, stack));

    stack.Attributes.SetLong("pieSize", 1);
    Assert.IsTrue(cond.IsMatch(_resolver.Resolver, _resolver.CatDict, stack));

    stack.Attributes.SetString("pieSize", "1");
    Assert.IsFalse(cond.IsMatch(_resolver.Resolver, _resolver.CatDict, stack));

    stack.Attributes.SetLong("pieSize", 2);
    Assert.IsFalse(cond.IsMatch(_resolver.Resolver, _resolver.CatDict, stack));

    stack.Attributes["pieSize"] = new IntArrayAttribute(new int[] { 1 });
    Assert.IsFalse(cond.IsMatch(_resolver.Resolver, _resolver.CatDict, stack));
  }

  [TestMethod]
  public void EnumerateMatchesExistingNull() {
    string json = @"
    {
      path: [""pieSize""],
      value: 1
    }
    ";
    Real.AttributeCondition cond =
        JsonObject.FromJson(json).AsObject<Real.AttributeCondition>(null,
                                                                    "gourmand");

    List<ItemStack> matches =
        cond.EnumerateMatches(_resolver.Resolver, _resolver.CatDict, null)
            .ToList();
    CollectionAssert.AreEqual(Array.Empty<ItemStack>(), matches);
  }

  [TestMethod]
  public void EnumerateMatchesSetAttribute() {
    string json = @"
    {
      path: [""pieSize""],
      value: 1
    }
    ";
    Real.AttributeCondition cond =
        JsonObject.FromJson(json).AsObject<Real.AttributeCondition>(null,
                                                                    "gourmand");

    List<ItemStack> originalMatches =
        new() { new ItemStack(LoadAssets.GetBlock("game", "pie-perfect"), 2),
                new ItemStack(LoadAssets.GetBlock("game", "pie-charred"), 2) };
    originalMatches[1].Attributes.SetInt("pieSize", 5);
    List<ItemStack> matches = originalMatches.ToList();
    matches =
        cond.EnumerateMatches(_resolver.Resolver, _resolver.CatDict, matches)
            .ToList();

    CollectionAssert.AreEqual(
        originalMatches.Select(c => c.Collectible).ToList(),
        matches.Select(c => c.Collectible).ToList());
    Assert.IsTrue(matches.All(i => i.Attributes.GetAsInt("pieSize") == 1));
  }

  [TestMethod]
  public void EnumerateMatchesEnumerateValues() {
    string json = @"
    {
      path: [""pieSize""],
      enumerateValues: [1, 2]
    }
    ";
    Real.AttributeCondition cond =
        JsonObject.FromJson(json).AsObject<Real.AttributeCondition>(null,
                                                                    "gourmand");

    List<ItemStack> originalMatches =
        new() { new ItemStack(LoadAssets.GetBlock("game", "pie-perfect"), 2),
                new ItemStack(LoadAssets.GetBlock("game", "pie-charred"), 2) };
    originalMatches[1].Attributes.SetInt("pieSize", 5);
    List<ItemStack> matches =
        cond.EnumerateMatches(_resolver.Resolver, _resolver.CatDict,
                              originalMatches)
            .ToList();

    CollectionAssert.AreEqual(
        originalMatches.Select(c => c.Collectible).ToHashSet().ToList(),
        matches.Select(c => c.Collectible).ToHashSet().ToList());
    Assert.AreEqual(2,
                    matches.Count(i => i.Attributes.GetAsInt("pieSize") == 1));
    Assert.AreEqual(2,
                    matches.Count(i => i.Attributes.GetAsInt("pieSize") == 2));
    Assert.AreEqual(originalMatches.Count * 2, matches.Count);
  }

  [TestMethod]
  public void SaveRestore() {
    string json = @"
    {
      path: [""pieSize""],
      outputs: [ ""output1"" ],
      enumerateValues: [2]
    }
    ";
    Real.AttributeCondition cond =
        JsonObject.FromJson(json).AsObject<Real.AttributeCondition>(null,
                                                                    "gourmand");
    string json2 = JsonConvert.SerializeObject(cond);
    Real.AttributeCondition cond2 =
        JsonObject.FromJson(json2).AsObject<Real.AttributeCondition>(
            null, "gourmand");
    Assert.AreEqual("pieSize", cond2.Path[0]);
    Assert.AreEqual("gourmand:output1", cond2.Outputs[0].ToString());
    Assert.AreEqual(cond.EnumerateValues[0].GetValue(),
                    cond2.EnumerateValues[0].GetValue());
  }

  [TestMethod]
  public void Categories2() {
    string json = @"
    {
      path: [""pieSize""],
      value: 1,
      outputs: [ ""output1"", ""output2"" ]
    }
    ";
    Real.AttributeCondition cond =
        JsonObject.FromJson(json).AsObject<Real.AttributeCondition>(null,
                                                                    "gourmand");

    CollectionAssert.AreEquivalent(
        new List<AssetLocation>() {
          new("gourmand", "output1"),
          new("gourmand", "output2"),
        },
        cond.Categories.ToList());
  }

  [TestMethod]
  public void AppendValue() {
    string json = @"
    {
      path: [""pieSize""],
      outputs: [ ""output1"", ""output2"" ]
    }
    ";
    Real.AttributeCondition cond =
        JsonObject.FromJson(json).AsObject<Real.AttributeCondition>(null,
                                                                    "gourmand");

    ItemStack stack = new(LoadAssets.GetItem("game", "fruit-pineapple"));
    stack.Attributes.SetInt("pieSize", 5);
    IAttribute[] expected = { new IntAttribute(5) };
    List<IAttribute> actual = new();
    cond.AppendValue(_resolver.Resolver, _resolver.CatDict,
                     new("gourmand", "output1"), stack, actual);
    Assert.IsTrue(Real.CategoryValue.ValuesEqual(actual, expected));
    actual.Clear();
    cond.AppendValue(_resolver.Resolver, _resolver.CatDict,
                     new("gourmand", "output2"), stack, actual);
    Assert.IsTrue(Real.CategoryValue.ValuesEqual(actual, expected));
  }
}
