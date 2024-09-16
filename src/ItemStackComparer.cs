using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Vintagestory.API.Common;

namespace Gourmand;

public class ItemStackComparer : IEqualityComparer<ItemStack> {
  private readonly IWorldAccessor _resolver;

  public ItemStackComparer(IWorldAccessor resolver) { _resolver = resolver; }

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
    // Use this function to handle null values correctly
    return CategoryValue.Equals(x.Attributes, y.Attributes);
  }

  public int GetHashCode([DisallowNull] ItemStack obj) {
    return HashCode.Combine(obj.Collectible, obj.StackSize,
                            obj.Attributes.GetHashCode());
  }
}
