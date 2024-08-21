using Microsoft.VisualStudio.TestTools.UnitTesting;

using PrefixClassName.MsTest;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand.Test;

using Real = Gourmand;

[PrefixTestClass]
public class AttributeCondition {
  private readonly Real.Collectibles.MatchResolver _resolver;

  public AttributeCondition() { _resolver = new(LoadAssets.Server.World); }

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
    Assert.IsFalse(cond.IsMatch(_resolver.CatDict, stack));

    stack.Attributes.SetInt("pieSize", 1);
    Assert.IsTrue(cond.IsMatch(_resolver.CatDict, stack));

    stack.Attributes.SetLong("pieSize", 1);
    Assert.IsTrue(cond.IsMatch(_resolver.CatDict, stack));

    stack.Attributes.SetString("pieSize", "1");
    Assert.IsTrue(cond.IsMatch(_resolver.CatDict, stack));

    stack.Attributes.SetLong("pieSize", 2);
    Assert.IsFalse(cond.IsMatch(_resolver.CatDict, stack));

    stack.Attributes["pieSize"] = new IntArrayAttribute(new int[] { 1 });
    Assert.IsFalse(cond.IsMatch(_resolver.CatDict, stack));
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
    Assert.IsFalse(cond.IsMatch(_resolver.CatDict, stack));

    stack.Attributes.SetInt("pieSize", 1);
    Assert.IsFalse(cond.IsMatch(_resolver.CatDict, stack));

    stack.Attributes.SetLong("pieSize", 1);
    Assert.IsTrue(cond.IsMatch(_resolver.CatDict, stack));

    stack.Attributes.SetString("pieSize", "1");
    Assert.IsFalse(cond.IsMatch(_resolver.CatDict, stack));

    stack.Attributes.SetLong("pieSize", 2);
    Assert.IsFalse(cond.IsMatch(_resolver.CatDict, stack));

    stack.Attributes["pieSize"] = new IntArrayAttribute(new int[] { 1 });
    Assert.IsFalse(cond.IsMatch(_resolver.CatDict, stack));
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

    List<ItemStack> matches = null;
    cond.EnumerateMatches(_resolver.CatDict, ref matches);
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
    cond.EnumerateMatches(_resolver.CatDict, ref matches);

    CollectionAssert.AreEqual(
        originalMatches.Select(c => c.Collectible).ToList(),
        matches.Select(c => c.Collectible).ToList());
    Assert.IsTrue(matches.All(i => i.Attributes.GetAsInt("pieSize") == 1));
  }

  [TestMethod]
  public void Categories2() {
    string json = @"
    {
      path: [""pieSize""],
      value: 1,
      output: [ ""output1"", ""output2"" ]
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
  public void GetValue() {
    string json = @"
    {
      path: [""pieSize""],
      output: [ ""output1"", ""output2"" ]
    }
    ";
    Real.AttributeCondition cond =
        JsonObject.FromJson(json).AsObject<Real.AttributeCondition>(null,
                                                                    "gourmand");

    ItemStack stack = new(LoadAssets.GetItem("game", "fruit-pineapple"));
    stack.Attributes.SetInt("pieSize", 5);
    IAttribute[] expected = { new IntAttribute(5) };
    List<IAttribute> actual =
        cond.GetValue(_resolver.CatDict, new("gourmand", "output1"), stack);
    Assert.IsTrue(CategoryValue.ValuesEqual(actual, expected));
    actual =
        cond.GetValue(_resolver.CatDict, new("gourmand", "output2"), stack);
    Assert.IsTrue(CategoryValue.ValuesEqual(actual, expected));
  }

  [TestMethod]
  public void GetValueNullCat() {
    string json = @"
    {
      path: [""pieSize""]
    }
    ";
    Real.AttributeCondition cond =
        JsonObject.FromJson(json).AsObject<Real.AttributeCondition>(null,
                                                                    "gourmand");

    ItemStack stack = new(LoadAssets.GetItem("game", "fruit-pineapple"));
    stack.Attributes.SetInt("pieSize", 5);
    IAttribute[] expected = { new IntAttribute(5) };
    List<IAttribute> actual = cond.GetValue(_resolver.CatDict, null, stack);
    Assert.IsTrue(CategoryValue.ValuesEqual(actual, expected));
  }
}
