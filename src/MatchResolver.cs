using System;
using System.Collections.Generic;

using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Gourmand;

/// <summary>
/// Temporarily holds the state necessary to resolve the matchers. This is
/// destructed after the matchers are resolved.
/// </summary>
public class MatchResolver {
  private Dictionary<string, List<Item>> _itemVariantsByPrefix;
  private Dictionary<string, List<Block>> _blockVariantsByPrefix;
  public readonly IWorldAccessor Resolver;

  // An dictionary of all item variants, indexed by the first part of their code
  // (everything up to the first '-'). Only items that have a '-' in their code
  // are included in the dictionary.
  private IReadOnlyDictionary<string, List<Item>> ItemVariantsByPrefix {
    get {
      if (_itemVariantsByPrefix == null) {
        _itemVariantsByPrefix = new();
        foreach (Item item in Resolver.Items) {
          int variantStart = item.Code.Path.IndexOf('-');
          if (variantStart != -1) {
            string first = item.Code.Path[..variantStart];
            if (!_itemVariantsByPrefix.TryGetValue(first,
                                                   out List<Item> items)) {
              items = new();
              _itemVariantsByPrefix.Add(first, items);
            }
            items.Add(item);
          }
        }
      }
      return _itemVariantsByPrefix;
    }
  }

  // An dictionary of all block variants, indexed by the first part of their
  // code (everything up to the first '-'). Only items that have a '-' in their
  // code are included in the dictionary.
  private IReadOnlyDictionary<string, List<Block>> BlockVariantsByPrefix {
    get {
      if (_blockVariantsByPrefix == null) {
        _blockVariantsByPrefix = new();
        foreach (Block block in Resolver.Blocks) {
          int variantStart = block.Code.Path.IndexOf('-');
          if (variantStart != -1) {
            string first = block.Code.Path[..variantStart];
            if (!_blockVariantsByPrefix.TryGetValue(first,
                                                    out List<Block> blocks)) {
              blocks = new();
              _blockVariantsByPrefix.Add(first, blocks);
            }
            blocks.Add(block);
          }
        }
      }
      return _blockVariantsByPrefix;
    }
  }

  public MatchResolver(IWorldAccessor resolver) { Resolver = resolver; }

  private static IReadOnlyList<X> GetMatchingCollectibles<X>(
      AssetLocation wildcard,
      System.Func<string, AssetLocation, IReadOnlyList<X>> acceleratedSearch,
      System.Func<AssetLocation, IReadOnlyList<X>> search,
      System.Func<AssetLocation, IReadOnlyList<X>> getDirect)
      where X : CollectibleObject {
    if (wildcard.Path[0] == '@') {
      // Everything after the @ is a regular expression.
      for (int pos = 1; pos < wildcard.Path.Length; ++pos) {
        if (wildcard.Path[pos] == '-') {
          // Found the hyphen before any special characters. This can be
          // accelerated.
          return acceleratedSearch(wildcard.Path[1..pos], wildcard);
        }
        if (!char.IsAsciiLetterOrDigit(wildcard.Path[pos])) {
          // A special character was found before the hyphen. Use the
          // unaccelerated search.
          return search(wildcard);
        }
      }
      // No hyphen or special character was in the regex. The regex was
      // unnecessary.
      return getDirect(new AssetLocation(wildcard.Domain, wildcard.Path[1..]));
    }
    for (int pos = 0; pos < wildcard.Path.Length; ++pos) {
      if (wildcard.Path[pos] == '-') {
        // Found the hyphen before any special characters.
        for (int starPos = pos + 1; starPos < wildcard.Path.Length; ++starPos) {
          if (wildcard.Path[starPos] == '*') {
            // There is a star, and this can be accelerated.
            return acceleratedSearch(wildcard.Path[0..pos], wildcard);
          }
        }
        // The path is not a wildcard. Use the game's default dictionary.
        return getDirect(wildcard);
      }
      if (wildcard.Path[pos] == '*') {
        // The asterisk came before the hyphen. This cannot be accelerated.
        return search(wildcard);
      }
    }
    // The path was not a wildcard.
    return getDirect(wildcard);
  }

  private IReadOnlyList<Item>
  GetMatchingItemsWithoutWildcard(AssetLocation code) {
    Item item = Resolver.GetItem(code);
    if (item == null) {
      return Array.Empty<Item>();
    } else {
      return new Item[] { item };
    }
  }

  private IReadOnlyList<Item>
  AcceleratedGetMatchingItems(string firstPart, AssetLocation wildcard) {
    if (!ItemVariantsByPrefix.TryGetValue(firstPart, out List<Item> search)) {
      return Array.Empty<Item>();
    }
    List<Item> result = new();
    foreach (Item item in search) {
      if (WildcardUtil.Match(wildcard, item.Code)) {
        result.Add(item);
      }
    }
    return result;
  }

  public IReadOnlyList<Item> GetMatchingItems(AssetLocation wildcard) {
    return GetMatchingCollectibles(wildcard, AcceleratedGetMatchingItems,
                                   Resolver.SearchItems,
                                   GetMatchingItemsWithoutWildcard);
  }

  private IReadOnlyList<Block>
  GetMatchingBlocksWithoutWildcard(AssetLocation code) {
    Block block = Resolver.GetBlock(code);
    if (block == null) {
      return Array.Empty<Block>();
    } else {
      return new Block[] { block };
    }
  }

  private IReadOnlyList<Block>
  AcceleratedGetMatchingBlocks(string firstPart, AssetLocation wildcard) {
    if (!BlockVariantsByPrefix.TryGetValue(firstPart, out List<Block> search)) {
      return Array.Empty<Block>();
    }
    List<Block> result = new();
    foreach (Block block in search) {
      if (WildcardUtil.Match(wildcard, block.Code)) {
        result.Add(block);
      }
    }
    return result;
  }

  public IReadOnlyList<Block> GetMatchingBlocks(AssetLocation wildcard) {
    return GetMatchingCollectibles(wildcard, AcceleratedGetMatchingBlocks,
                                   Resolver.SearchBlocks,
                                   GetMatchingBlocksWithoutWildcard);
  }

  public IReadOnlyList<CollectibleObject>
  GetMatchingCollectibles(AssetLocation wildcard, EnumItemClass itemClass) {
    switch (itemClass) {
    case EnumItemClass.Block:
      return GetMatchingBlocks(wildcard);
    case EnumItemClass.Item:
      return GetMatchingItems(wildcard);
    default:
      throw new ArgumentException("Invalid enum value", "itemClass");
    }
  }
}
