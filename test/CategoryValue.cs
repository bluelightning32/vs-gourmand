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
}
