using Microsoft.VisualStudio.TestTools.UnitTesting;

using PrefixClassName.MsTest;

namespace Gourmand.Test;

[PrefixTestClass]
public class EnumerableExtensions {
  [TestMethod]
  public void InterleaveEmpty() {
    IEnumerable<int>[] merge = Array.Empty<IEnumerable<int>>();

    CollectionAssert.AreEqual(Array.Empty<int>(), merge.Interleave().ToArray());
  }

  [TestMethod]
  public void InterleaveDiffLengths() {
    IEnumerable<int>[] merge = new IEnumerable<int>[] {
      new int[] { 1, 3 },
      new int[] { 2 },
      Array.Empty<int>(),
    };

    CollectionAssert.AreEqual(new int[] { 1, 2, 3 },
                              merge.Interleave().ToArray());
  }

  [TestMethod]
  public void InterleaveMany() {
    IEnumerable<int>[] merge = new IEnumerable<int>[] {
      new int[] { 1, 3, 5 },
      new int[] { 2, 4, 6 },
      Array.Empty<int>(),
    };

    CollectionAssert.AreEqual(new int[] { 1, 2, 3, 4, 5, 6 },
                              merge.Interleave().ToArray());
  }
}
