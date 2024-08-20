using System;
using System.Collections.Generic;
using System.Text;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

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
    return Priority.GetHashCode() ^ (Value?.GetHashCode() ?? 0);
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
}

public interface IReadonlyCategoryDict {
  /// <summary>
  /// Look up the value of a category for the given collectible.
  /// </summary>
  /// <param name="category">the category to look up</param>
  /// <param name="c">the item/block to look up the category value for</param>
  /// <returns>the value, or null if the collectible does not match the
  /// category</returns>
  CategoryValue GetValue(AssetLocation category, CollectibleObject c);

  /// <summary>
  /// Check whether the collectible is in a category
  /// </summary>
  /// <param name="category">the category to check</param>
  /// <param name="c">the collectible that may be in the category</param>
  /// <returns>true if it is in the category</returns>
  bool InCategory(AssetLocation category, CollectibleObject c) {
    return GetValue(category, c)?.Value != null;
  }

  /// <summary>
  /// Find all collectible objects that match any value in a category.
  /// </summary>
  /// <param name="category">the category to search</param>
  /// <returns>an enumeration of the matching collectibles</returns>
  IEnumerable<CollectibleObject> EnumerateMatches(AssetLocation category);
}

public class CategoryDict : IReadonlyCategoryDict {
  readonly Dictionary<AssetLocation,
                      Dictionary<CollectibleObject, CategoryValue>> _byCat =
      new();

  public CategoryDict() {}

  public CategoryValue GetValue(AssetLocation category, CollectibleObject c) {
    if (!_byCat.TryGetValue(
            category,
            out Dictionary<CollectibleObject, CategoryValue> collectibles)) {
      return null;
    }
    return collectibles.Get(c);
  }

  public IEnumerable<CollectibleObject>
  EnumerateMatches(AssetLocation category) {
    if (!_byCat.TryGetValue(
            category,
            out Dictionary<CollectibleObject, CategoryValue> collectibles)) {
      yield break;
    }
    foreach (KeyValuePair<CollectibleObject, CategoryValue> kv in
                 collectibles) {
      // Deleted categories have a null value.
      if (kv.Value.Value != null) {
        yield return kv.Key;
      }
    }
  }

  /// <summary>
  /// Add new entries to the dictionary. These category-collectible combinations
  /// must not already exist in the dictionary.
  /// </summary>
  /// <param name="category">
  ///   the category that all of the collectibles belong to
  /// </param>
  /// <param name="collectibles">
  ///   the collectible in the category along with its category value
  /// </param>
  public void Add(AssetLocation category,
                  Dictionary<CollectibleObject, CategoryValue> collectibles) {
    _byCat.Add(category, collectibles);
  }

  public void Add(AssetLocation category, CollectibleObject collectible,
                  CategoryValue value) {
    if (!_byCat.TryGetValue(
            category,
            out Dictionary<CollectibleObject, CategoryValue> catDict)) {
      catDict = new();
      _byCat.Add(category, catDict);
    }
    catDict.Add(collectible, value);
  }

  public void Set(AssetLocation category, CollectibleObject collectible,
                  CategoryValue value) {
    if (!_byCat.TryGetValue(
            category,
            out Dictionary<CollectibleObject, CategoryValue> catDict)) {
      catDict = new();
      _byCat.Add(category, catDict);
    }
    catDict[collectible] = value;
  }

  public void Transfer(AssetLocation category, CategoryDict to) {
    if (_byCat.Remove(
            category,
            out Dictionary<CollectibleObject, CategoryValue> collectibles)) {
      to.Add(category, collectibles);
    }
  }
}
