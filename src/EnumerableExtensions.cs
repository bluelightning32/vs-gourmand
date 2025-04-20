using System.Collections.Generic;

namespace Gourmand;

public static class EnumerableExtensions {
  public static IEnumerable<T>
  Interleave<T>(this IEnumerable<IEnumerable<T>> source) {
    List<IEnumerator<T>> available = new();
    foreach (IEnumerable<T> found in source) {
      IEnumerator<T> iter = found.GetEnumerator();
      if (iter.MoveNext()) {
        yield return iter.Current;
        if (iter.MoveNext()) {
          available.Add(iter);
        }
      }
    }
    while (available.Count > 0) {
      for (int i = 0; i < available.Count;) {
        IEnumerator<T> iter = available[i];
        yield return iter.Current;
        if (iter.MoveNext()) {
          ++i;
        } else {
          available.RemoveAt(i);
        }
      }
    }
  }
}
