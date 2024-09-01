using System.Collections;
using System.Collections.Generic;

using Vintagestory.API.Common;

namespace Gourmand;

public class AllStacksList : IReadOnlyList<ItemStack> {
  private readonly IWorldAccessor _resolver;

  public AllStacksList(IWorldAccessor resolver) { _resolver = resolver; }

  public ItemStack this[int index] {
    get {
      if (index < _resolver.Items.Count) {
        return new ItemStack(_resolver.Items[index]);
      }
      return new ItemStack(_resolver.Blocks[index - _resolver.Items.Count]);
    }
  }

  public int Count => _resolver.Items.Count + _resolver.Blocks.Count;

  public IEnumerator<ItemStack> GetEnumerator() {
    foreach (Item i in _resolver.Items) {
      yield return new ItemStack(i);
    }
    foreach (Block b in _resolver.Blocks) {
      yield return new ItemStack(b);
    }
  }

  IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
}
