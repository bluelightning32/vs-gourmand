using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Vintagestory.API.Common;

namespace Gourmand;

public class ItemStackComparer : IEqualityComparer<ItemStack> {
  private readonly IWorldAccessor _resolver;
  private readonly string[] _ignorePaths;

  public ItemStackComparer(IWorldAccessor resolver, string[] ignorePaths) { _resolver = resolver; _ignorePaths = ignorePaths; }

  public ItemStackComparer(IWorldAccessor resolver) { _resolver = resolver; _ignorePaths = null; }

  public bool Equals(ItemStack x, ItemStack y) {
    if (x == null) {
      return y == null;
    }
    if (y == null) {
      return false;
    }
    if (x.Collectible != y.Collectible) {
      return false;
    }
    if (x.StackSize != y.StackSize) {
      return false;
    }
    if (_ignorePaths == null) {
      return x.Attributes.Equals(_resolver, y.Attributes);
    } else {
      return x.Attributes.Equals(_resolver, y.Attributes, _ignorePaths);
    }
  }

  public int GetHashCode([DisallowNull] ItemStack obj) {
    int attrHash = _ignorePaths == null ? obj.Attributes.GetHashCode() : obj.Attributes.GetHashCode(_ignorePaths);
    return HashCode.Combine(obj.Collectible, obj.StackSize,
                            attrHash);
  }
}
