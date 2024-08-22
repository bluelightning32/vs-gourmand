using Microsoft.VisualStudio.TestTools.UnitTesting;

using PrefixClassName.MsTest;

using Vintagestory.API.Datastructures;

namespace Gourmand.Test;

using Real = Gourmand;

[PrefixTestClass]
public class CategoryValue {
  [TestMethod]
  public void EqualsGetHashCodeSameStrings() {
    Real.CategoryValue v1 =
        new(1, new List<IAttribute>() { new StringAttribute("ss") });
    Real.CategoryValue v2 =
        new(1, new List<IAttribute>() { new StringAttribute("ss") });
    Assert.IsTrue(v1.Equals(v2));
    Assert.IsTrue(v2.Equals(v1));
    Assert.AreEqual(v1.GetHashCode(), v2.GetHashCode());
  }

  [TestMethod]
  public void EqualsGetHashCodeSameInts() {
    Real.CategoryValue v1 =
        new(1, new List<IAttribute>() { new IntAttribute(15) });
    Real.CategoryValue v2 =
        new(1, new List<IAttribute>() { new IntAttribute(15) });
    Assert.IsTrue(v1.Equals(v2));
    Assert.IsTrue(v2.Equals(v1));
    Assert.AreEqual(v1.GetHashCode(), v2.GetHashCode());
  }

  [TestMethod]
  public void EqualsGetHashCodeDifferentStrings() {
    Real.CategoryValue v1 =
        new(1, new List<IAttribute>() { new StringAttribute("aa") });
    Real.CategoryValue v2 =
        new(1, new List<IAttribute>() { new StringAttribute("bb") });
    Assert.IsFalse(v1.Equals(v2));
    Assert.IsFalse(v2.Equals(v1));
    Assert.AreNotEqual(v1.GetHashCode(), v2.GetHashCode());
  }

  [TestMethod]
  public void EqualsGetHashCodeDifferentInts() {
    Real.CategoryValue v1 =
        new(1, new List<IAttribute>() { new IntAttribute(3) });
    Real.CategoryValue v2 =
        new(1, new List<IAttribute>() { new IntAttribute(32) });
    Assert.IsFalse(v1.Equals(v2));
    Assert.IsFalse(v2.Equals(v1));
    Assert.AreNotEqual(v1.GetHashCode(), v2.GetHashCode());
  }

  [TestMethod]
  public void CompareAttributesTypes() {
    IAttribute[] attrs = {
      new IntAttribute(1),
      new LongAttribute(1),
      new StringAttribute("1"),
    };
    for (int i = 0; i < attrs.Length; ++i) {
      for (int j = 0; j < attrs.Length; ++j) {
        if (i == j) {
          Assert.AreEqual(
              0, Real.CategoryValue.CompareAttributes(attrs[i], attrs[j]));
        } else {
          Assert.AreNotEqual(
              0, Real.CategoryValue.CompareAttributes(attrs[i], attrs[j]));
        }
      }
    }
  }

  [TestMethod]
  public void CompareAttributesInts() {
    IAttribute[] attrs = {
      new IntAttribute(-1),
      new IntAttribute(0),
      new IntAttribute(1),
      new IntAttribute(2),
    };
    for (int i = 0; i < attrs.Length; ++i) {
      for (int j = 0; j < attrs.Length; ++j) {
        Assert.AreEqual(i < j, Real.CategoryValue.CompareAttributes(
                                   attrs[i], attrs[j]) < 0);
        Assert.AreEqual(i > j, Real.CategoryValue.CompareAttributes(
                                   attrs[i], attrs[j]) > 0);
      }
    }
  }

  [TestMethod]
  public void CompareAttributesStrings() {
    IAttribute[] attrs = {
      new StringAttribute("a"),
      new StringAttribute("b"),
      new StringAttribute("c"),
    };
    for (int i = 0; i < attrs.Length; ++i) {
      for (int j = 0; j < attrs.Length; ++j) {
        Assert.AreEqual(i < j, Real.CategoryValue.CompareAttributes(
                                   attrs[i], attrs[j]) < 0);
        Assert.AreEqual(i > j, Real.CategoryValue.CompareAttributes(
                                   attrs[i], attrs[j]) > 0);
      }
    }
  }

  [TestMethod]
  public void CompareAttributeArraysInts() {
    IAttribute[][] attrs = {
      new IAttribute[] { new IntAttribute(1) },
      new IAttribute[] { new IntAttribute(1), new IntAttribute(2) },
      new IAttribute[] { new IntAttribute(2) },
      new IAttribute[] { new IntAttribute(2), new IntAttribute(0) },
    };
    for (int i = 0; i < attrs.Length; ++i) {
      for (int j = 0; j < attrs.Length; ++j) {
        Assert.AreEqual(i < j, Real.CategoryValue.CompareAttributeCollections(
                                   attrs[i], attrs[j]) < 0);
        Assert.AreEqual(i > j, Real.CategoryValue.CompareAttributeCollections(
                                   attrs[i], attrs[j]) > 0);
      }
    }
  }
}
