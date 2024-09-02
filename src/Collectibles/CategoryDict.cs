using System.Collections.Generic;
using System.IO;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Gourmand.Collectibles;

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
  IEnumerable<CollectibleObject> EnumerateMatches(AssetLocation category,
                                                  int enumeratePerDistinct);
}

public class CategoryDict : IReadonlyCategoryDict, IByteSerializable {
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

  public IEnumerable<CollectibleObject>
  EnumerateMatches(AssetLocation category, int enumeratePerDistinct) {
    Dictionary<List<IAttribute>, int> distinctCounts =
        new(new ListIAttributeComparer());
    if (!_byCat.TryGetValue(
            category,
            out Dictionary<CollectibleObject, CategoryValue> collectibles)) {
      yield break;
    }
    foreach (KeyValuePair<CollectibleObject, CategoryValue> kv in
                 collectibles) {
      // Deleted categories have a null value.
      if (kv.Value.Value == null) {
        continue;
      }
      int count = distinctCounts.GetValueOrDefault(kv.Value.Value, 0);
      if (count < enumeratePerDistinct) {
        ++count;
        distinctCounts[kv.Value.Value] = count;
        yield return kv.Key;
      }
    }
  }

  public void ToBytes(BinaryWriter writer) {
    writer.Write(_byCat.Count);
    foreach (var entry in _byCat) {
      writer.Write(entry.Key.Domain);
      writer.Write(entry.Key.Path);
      writer.Write(entry.Value.Count);
      foreach (var catEntry in entry.Value) {
        writer.Write((int)catEntry.Key.ItemClass);
        writer.Write(catEntry.Key.Id);
        if (catEntry.Value == null) {
          writer.Write(false);
        } else {
          writer.Write(true);
          catEntry.Value.ToBytes(writer);
        }
      }
    }
  }

  public void FromBytes(BinaryReader reader, IWorldAccessor resolver) {
    _byCat.Clear();
    int count1 = reader.ReadInt32();
    for (int i = 0; i < count1; ++i) {
      AssetLocation category =
          new AssetLocation(reader.ReadString(), reader.ReadString());
      Dictionary<CollectibleObject, CategoryValue> collectibles = new();
      _byCat.Add(category, collectibles);
      int categoryCount = reader.ReadInt32();
      for (int j = 0; j < categoryCount; ++j) {
        EnumItemClass itemClass = (EnumItemClass)reader.ReadInt32();
        int id = reader.ReadInt32();
        CollectibleObject collectible =
            itemClass switch { EnumItemClass.Block => resolver.GetBlock(id),
                               _ => resolver.GetItem(id) };
        CategoryValue value = null;
        if (reader.ReadBoolean()) {
          value = new(reader);
        }
        collectibles.Add(collectible, value);
      }
    }
  }
}
