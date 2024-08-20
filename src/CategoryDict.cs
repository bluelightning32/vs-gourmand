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
    if (Value == null) {
      return other.Value == null;
    }
    if (other.Value == null) {
      return false;
    }
    if (Value.Count != other.Value.Count) {
      return false;
    }
    for (int i = 0; i < Value.Count; ++i) {
      // The attribute values have to be specially compared to avoid the
      // StringAttribute Equals method, because it is broken.
      if (!Value[i].GetValue().Equals(other.Value[i].GetValue())) {
        return false;
      }
    }
    return true;
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
