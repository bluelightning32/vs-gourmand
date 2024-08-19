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
  /// <param name="c">the item/block to look up the category value for</param>
  /// <param name="category">the category to look up</param>
  /// <returns>the value, or null if the collectible does not match the
  /// category</returns>
  CategoryValue GetValue(CollectibleObject c, AssetLocation category);
}

public class CategoryDict : IReadonlyCategoryDict {
  readonly Dictionary<ValueTuple<CollectibleObject, AssetLocation>,
                      CategoryValue> _byObj = new();

  public CategoryDict() {}

  public CategoryValue GetValue(CollectibleObject c, AssetLocation category) {
    return _byObj.Get(ValueTuple.Create(c, category));
  }
}
