using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand;

public class CategoryValue : IEquatable<CategoryValue>, IByteSerializable {
  public float Priority;
  public List<IAttribute> Value;

  public CategoryValue(float priority, List<IAttribute> value) {
    Priority = priority;
    Value = value;
  }

  public CategoryValue(BinaryReader reader) {
    Value = new();
    FromBytes(reader, null);
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
    builder.Append($"{{ priority: {Priority}, value:");
    if (Value == null) {
      builder.Append(" null");
    } else {
      builder.Append("[");
      foreach (IAttribute value in Value) {
        builder.Append(value.ToString());
        builder.Append(", ");
      }
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

  /// <summary>
  /// Comparison function for attributes that handles StringAttributes and
  /// TreeAttributes correctly
  /// </summary>
  /// <param name="a1">first attribute to compare</param>
  /// <param name="a2">second attribute to compare</param>
  /// <returns>true if they are equal</returns>
  public static bool Equals(IAttribute a1, IAttribute a2) {
    if (a1 == null) {
      return a2 == null;
    }
    if (a2 == null) {
      return false;
    }
    if (a1 is TreeAttribute t1) {
      if (a2 is not TreeAttribute t2) {
        return false;
      }
      lock (t1.attributesLock) {
        lock (t2.attributesLock) {
          using var enum1 = t1.GetEnumerator();
          using var enum2 = t2.GetEnumerator();
          // Before the first call to enum.MoveNext, enum1.Current points before
          // the first element and is invalid.
          while (enum1.MoveNext()) {
            if (!enum2.MoveNext()) {
              // enum1 has more elements than enum2
              return false;
            }
            if (enum1.Current.Key != enum2.Current.Key) {
              return false;
            }
            if (!Equals(enum1.Current.Value, enum2.Current.Value)) {
              return false;
            }
          }
          if (enum2.MoveNext()) {
            // enum2 has more elements than enum1
            return false;
          }
        }
      }
      return true;
    }
    if (a1 is StringAttribute s1) {
      if (a2 is not StringAttribute s2) {
        return false;
      }
      // The attribute values have to be specially compared to avoid the
      // StringAttribute Equals method, because it is broken.
      if (s1.value == null) {
        return s2.value == null;
      }
      if (s2.value == null) {
        return false;
      }
      return s1.value == s2.value;
    }
    if (a1 is FloatArrayAttribute f1) {
      if (a2 is not FloatArrayAttribute f2) {
        return false;
      }
      return f1.value.SequenceEqual(f2.value);
    }
    return a1.GetValue().Equals(a2.GetValue());
  }

  public static bool ValuesEqual(IEnumerable<IAttribute> value1,
                                 IEnumerable<IAttribute> value2) {
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
      if (!Equals(enum1.Current, enum2.Current)) {
        return false;
      }
    }
    if (enum2.MoveNext()) {
      // enum2 has more elements than enum1
      return false;
    }
    return true;
  }

  public static bool
  ValuesEqualEitherOrder(IReadOnlyCollection<IAttribute> expected1,
                         IReadOnlyCollection<IAttribute> expected2,
                         IReadOnlyCollection<IAttribute> actual) {
    return ValuesEqual(expected1.Concat(expected2), actual) ||
           ValuesEqual(expected2.Concat(expected1), actual);
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

  public static void IAttributeListToBytes(BinaryWriter writer,
                                           List<IAttribute> save) {
    writer.Write(save.Count);
    foreach (IAttribute a in save) {
      writer.Write(a.GetAttributeId());
      a.ToBytes(writer);
    }
  }

  public static void IAttributeListFromBytes(BinaryReader reader,
                                             List<IAttribute> load) {
    load.Clear();
    int count = reader.ReadInt32();
    load.EnsureCapacity(count);
    for (int i = 0; i < count; ++i) {
      int id = reader.ReadInt32();
      Type type = TreeAttribute.AttributeIdMapping[id];
      IAttribute attr = (IAttribute)Activator.CreateInstance(type);
      attr.FromBytes(reader);
      load.Add(attr);
    }
  }

  public void ToBytes(BinaryWriter writer) {
    writer.Write(Priority);
    if (Value == null) {
      writer.Write(false);
    } else {
      writer.Write(true);
      IAttributeListToBytes(writer, Value);
    }
  }

  public void FromBytes(BinaryReader reader, IWorldAccessor resolver) {
    Priority = reader.ReadSingle();
    if (reader.ReadBoolean()) {
      IAttributeListFromBytes(reader, Value);
    } else {
      Value = null;
    }
  }
}

public class ListIAttributeComparer : IComparer<List<IAttribute>>,
                                      IEqualityComparer<List<IAttribute>> {
  public int Compare(List<IAttribute> x, List<IAttribute> y) {
    return CategoryValue.CompareAttributeCollections(x, y);
  }

  public bool Equals(List<IAttribute> x, List<IAttribute> y) {
    return CategoryValue.ValuesEqual(x, y);
  }

  public int GetHashCode([DisallowNull] List<IAttribute> obj) {
    return CategoryValue.ValuesGetHashCode(obj);
  }
}
