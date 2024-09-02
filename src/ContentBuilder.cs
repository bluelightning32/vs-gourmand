using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Gourmand;

public class ContentBuilder {
  public ContentBuilder() : this(Array.Empty<ItemStack>()) {}

  public ContentBuilder(ItemStack[] contents) {
    _contents = contents.ToList();
    ResetUsed();
  }

  public static ItemStack[] GetContents(IWorldAccessor resolver,
                                        ItemStack stack) {
    if (stack.Collectible is IBlockMealContainer mealContainer) {
      return mealContainer.GetContents(resolver, stack);
    } else if (stack.Collectible is BlockContainer container) {
      return container.GetContents(resolver, stack);
    } else {
      return Array.Empty<ItemStack>();
    }
  }

  public static void SetContents(IWorldAccessor resolver, ItemStack stack,
                                 ItemStack[] newContents) {
    if (stack.Collectible is IBlockMealContainer mealContainer) {
      mealContainer.SetContents(
          mealContainer.GetRecipeCode(resolver, stack), stack, newContents,
          mealContainer.GetQuantityServings(resolver, stack));
    } else if (stack.Collectible is BlockContainer container) {
      container.SetContents(stack, newContents);
    }
  }

  public static void SetContents(IWorldAccessor resolver, ItemStack stack,
                                 IEnumerable<ItemStack> contents, int slots) {
    SetContents(resolver, stack, contents.Take(slots).ToArray());
  }

  public bool PushValue(ItemStack value, int slotBegin, int slotEnd) {
    int i = int.Max(slotBegin, _firstUnused);
    while (true) {
      if (i >= slotEnd) {
        return false;
      }
      if (i >= _contents.Count) {
        while (_contents.Count < i) {
          _contents.Add(null);
        }
        _contents.Add(value);
        _used.Add(i);
        if (_firstUnused == i) {
          _firstUnused = i + 1;
        }
        HighestUsed = int.Max(HighestUsed, i);
        return true;
      }
      if (_contents[i] == null) {
        _used.Add(i);
        _contents[i] = value;
        if (_firstUnused == i) {
          _firstUnused = i + 1;
        }
        HighestUsed = int.Max(HighestUsed, i);
        return true;
      } else if (i == _firstUnused) {
        _firstUnused = i + 1;
      }
      ++i;
    }
  }

  public ItemStack PopValue() {
    int i = _used[_used.Count - 1];
    ItemStack value = _contents[i];
    _contents[i] = null;
    if (i == HighestUsed) {
      --HighestUsed;
      while (HighestUsed >= 0 && _contents[HighestUsed] == null) {
        --HighestUsed;
      }
    }
    _firstUnused = int.Min(_firstUnused, i);
    _used.RemoveAt(_used.Count - 1);
    return value;
  }

  public void Set(IWorldAccessor resolver, ItemStack s) {
    _base = s;
    _contents.Clear();
    _contents.AddRange(GetContents(resolver, s));
    _minOutput = _contents.Count;
    _baseUsed = false;
    ResetUsed();
  }

  public ItemStack GetItemStack(IWorldAccessor resolver) {
    ItemStack s = _base;
    if (_baseUsed) {
      s = s.Clone();
    }
    _baseUsed = true;
    SetContents(resolver, s, _contents, int.Max(_minOutput, HighestUsed + 1));
    return s;
  }

  public IReadOnlyList<ItemStack> Contents => _contents;
  public int HighestUsed { get; private set; }

  private void ResetUsed() {
    Debug.Assert(_used.Count == 0);
    HighestUsed = -1;
    for (int i = 0;; ++i) {
      if (i == _contents.Count) {
        _firstUnused = i;
        break;
      }
      if (_contents[i] == null) {
        _firstUnused = i;
        break;
      } else {
        HighestUsed = i;
      }
    }
    for (int i = _firstUnused + 1; i < _contents.Count; ++i) {
      if (_contents[i] != null) {
        HighestUsed = i;
      }
    }
  }

  private readonly List<ItemStack> _contents;
  private readonly List<int> _used = new();
  private int _firstUnused;
  private ItemStack _base = null;
  private bool _baseUsed = false;
  private int _minOutput = 0;
}
