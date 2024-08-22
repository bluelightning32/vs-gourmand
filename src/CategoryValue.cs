using System;
using System.Collections.Generic;
using System.Text;

using Vintagestory.API.Datastructures;

namespace Gourmand;

public class CategoryValue : IEquatable<CategoryValue> {
  public float Priority;
  public List<IAttribute> Value;

  public CategoryValue(float priority, List<IAttribute> value) {
    Priority = priority;
    Value = value;
  }

  public bool Equals(CategoryValue other) {
    if (other == null) {
      return false;
    }
    if (Priority != other.Priority) {
      return false;
    }
    return ValuesEqual(Value, other.Value);
  }

  public override bool Equals(object obj) {
    return Equals(obj as CategoryValue);
  }

  public override int GetHashCode() {
    return Priority.GetHashCode() ^ ValuesGetHashCode(Value);
  }

  public override string ToString() {
    StringBuilder builder = new();
    builder.Append($"{{ priority: {Priority}, value: [");
    foreach (IAttribute value in Value) {
      builder.Append(value.ToString());
      builder.Append(", ");
    }
    builder.Append("]}");
    return builder.ToString();
  }

  public static int ValuesGetHashCode(IReadOnlyCollection<IAttribute> value) {
    int total = 0;
    if (value != null) {
      foreach (IAttribute a in value) {
        total += 0xE;
        // Rotate total to the left 13 bits.
        total = (total << 13) | (total >> (32 - 13));
        total ^= a.GetHashCode();
      }
    }
    return total;
  }

  public static bool ValuesEqual(IReadOnlyCollection<IAttribute> value1,
                                 IReadOnlyCollection<IAttribute> value2) {
    if (value1 == null) {
      return value2 == null;
    }
    if (value2 == null) {
      return false;
    }

    using IEnumerator<IAttribute> enum1 = value1.GetEnumerator();
    using IEnumerator<IAttribute> enum2 = value2.GetEnumerator();
    // Before the first call to enum.MoveNext, enum1.Current points before the
    // first element and is invalid.
    while (enum1.MoveNext()) {
      if (!enum2.MoveNext()) {
        // enum1 has more elements than enum2
        return false;
      }
      // The attribute values have to be specially compared to avoid the
      // StringAttribute Equals method, because it is broken.
      if (!enum1.Current.GetValue().Equals(enum2.Current.GetValue())) {
        return false;
      }
    }
    if (enum2.MoveNext()) {
      // enum2 has more elements than enum1
      return false;
    }
    return true;
  }

  public static int CompareAttributes(IAttribute x, IAttribute y) {
    if (x == null) {
      if (y == null) {
        return 0;
      } else {
        return -1;
      }
    }
    if (y == null) {
      return 1;
    }
    int attrDiff = x.GetAttributeId() - y.GetAttributeId();
    if (attrDiff != 0) {
      return attrDiff;
    }
    object xValue = x.GetValue();
    object yValue = y.GetValue();
    if (xValue is IComparable comparable) {
      return comparable.CompareTo(yValue);
    }
    return xValue.GetHashCode() - yValue.GetHashCode();
  }

  /// <summary>
  /// Performs a lexicographical comparison between two arrays of attributes
  /// </summary>
  /// <param name="x">first attribute array</param>
  /// <param name="y">second attribute array</param>
  /// <returns>less than 0 if x is less than y, 0 if they are equal, or greater
  /// than 0 if x is greater than y</returns>
  public static int
  CompareAttributeCollections(IReadOnlyCollection<IAttribute> x,
                              IReadOnlyCollection<IAttribute> y) {
    if (x == null) {
      if (y == null) {
        return 0;
      } else {
        return -1;
      }
    }
    if (y == null) {
      return 1;
    }
    using IEnumerator<IAttribute> xe = x.GetEnumerator();
    using IEnumerator<IAttribute> ye = y.GetEnumerator();
    // Before the first call to xe.MoveNext, xe.Current points before the
    // first element and is invalid.
    while (xe.MoveNext()) {
      if (!ye.MoveNext()) {
        // xe has more elements than ye
        return 1;
      }
      int comparison = CompareAttributes(xe.Current, ye.Current);
      if (comparison != 0) {
        return comparison;
      }
    }
    if (ye.MoveNext()) {
      // enum2 has more elements than enum1
      return -1;
    }
    return 0;
  }
}

public class ListIAttributeComparer : IComparer<List<IAttribute>> {
  public int Compare(List<IAttribute> x, List<IAttribute> y) {
    return CategoryValue.CompareAttributeCollections(x, y);
  }
}
