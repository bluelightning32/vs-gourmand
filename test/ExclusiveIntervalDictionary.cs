using Microsoft.VisualStudio.TestTools.UnitTesting;

using PrefixClassName.MsTest;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand.Test;

using Real = Gourmand;

[PrefixTestClass]
public class ExclusiveIntervalDictionary {
  [TestMethod]
  public void GetIntersectingEmpty() {
    ExclusiveIntervalDictionary<int, int> dict = new();
    CollectionAssert.AreEqual(Array.Empty<Interval<int, int>>(),
                              dict.GetIntersecting(0, 100).ToArray());
  }

  [TestMethod]
  public void GetIntersecting() {
    ExclusiveIntervalDictionary<int, int> dict = new();
    Interval<int, int> int1 = new(2, 10, 0);
    Interval<int, int> int2 = new(10, 20, 1);
    dict.Add(int1);
    dict.Add(int2);
    // Don't return [2, 10), when the end is 2.
    CollectionAssert.AreEqual(Array.Empty<Interval<int, int>>(),
                              dict.GetIntersecting(0, 2).ToArray());

    CollectionAssert.AreEqual(new Interval<int, int>[] { int1 },
                              dict.GetIntersecting(0, 3).ToArray());
    var a = dict.GetIntersecting(0, 30).ToArray();
    CollectionAssert.AreEqual(new Interval<int, int>[] { int1, int2 },
                              dict.GetIntersecting(0, 30).ToArray());
    CollectionAssert.AreEqual(new Interval<int, int>[] { int1 },
                              dict.GetIntersecting(9, 10).ToArray());
    CollectionAssert.AreEqual(new Interval<int, int>[] { int1, int2 },
                              dict.GetIntersecting(9, 11).ToArray());
    CollectionAssert.AreEqual(new Interval<int, int>[] { int2 },
                              dict.GetIntersecting(10, 11).ToArray());
    CollectionAssert.AreEqual(new Interval<int, int>[] { int2 },
                              dict.GetIntersecting(11, 20).ToArray());
    CollectionAssert.AreEqual(Array.Empty<Interval<int, int>>(),
                              dict.GetIntersecting(20, 30).ToArray());
    CollectionAssert.AreEqual(Array.Empty<Interval<int, int>>(),
                              dict.GetIntersecting(21, 30).ToArray());
  }

  [TestMethod]
  public void RemoveIntersecting() {
    Interval<int, int> int1 = new(2, 10, 0);
    Interval<int, int> int2 = new(10, 20, 1);
    List<Tuple<int, int, Interval<int, int>[]>> cases = new() {
      new(0, 2, new[] { int1, int2 }),
      new(0, 3, new[] { new(3, 10, 0), int2 }),
      new(0, 30, Array.Empty<Interval<int, int>>()),
      new(9, 10, new[] { new(2, 9, 0), int2 }),
      new(9, 11, new Interval<int, int>[] { new(2, 9, 0), new(11, 20, 1) }),
      new(10, 11, new[] { int1, new(11, 20, 1) }),
      new(10, 20, new[] { int1 }),
      new(11, 20, new[] { int1, new(10, 11, 1) }),
      new(20, 30, new[] { int1, int2 }),
      new(21, 30, new[] { int1, int2 }),
    };
    for (int i = 0; i < cases.Count; ++i) {
      ExclusiveIntervalDictionary<int, int> dict = new() { int1, int2 };
      dict.RemoveIntersecting(cases[i].Item1, cases[i].Item2);
      CollectionAssert.AreEqual(cases[i].Item3, dict, $"case {i} failed ");
    }
  }

  [TestMethod]
  [ExpectedException(typeof(ArgumentException),
                     "intersects with existing intervals")]
  public void AddIntersectsPrevious() {
    ExclusiveIntervalDictionary<int, int> dict = new() { new(2, 10, 0) };
    dict.Add(9, 10, 1);
  }

  [TestMethod]
  [ExpectedException(typeof(ArgumentException),
                     "intersects with existing intervals")]
  public void AddIntersectsNext() {
    ExclusiveIntervalDictionary<int, int> dict = new() { new(2, 10, 0) };
    dict.Add(0, 3, 1);
  }

  [TestMethod]
  [ExpectedException(typeof(ArgumentException),
                     "intersects with existing intervals")]
  public void AddIntersectsMiddle() {
    ExclusiveIntervalDictionary<int, int> dict = new() { new(2, 10, 0) };
    dict.Add(0, 12, 1);
  }

  [TestMethod]
  public void AddTouchesAdjacent() {
    ExclusiveIntervalDictionary<int, int> dict =
        new() { new(2, 10, 0), new(20, 30, 2) };
    dict.Add(10, 20, 1);
  }
}
