using System.Collections.Generic;
using System.Linq;

using Vintagestory.API.Common;

namespace Gourmand;

public class ContentBuilder {
  public ContentBuilder(ItemStack[] contents) {
    _contents = contents.ToList();
    _highestUsed = -1;
    for (int i = 0;; ++i) {
      if (i == contents.Length) {
        _firstUnused = i;
        break;
      }
      if (contents[i] == null) {
        _firstUnused = i;
        break;
      } else {
        _highestUsed = i;
      }
    }
    for (int i = _firstUnused + 1; i < contents.Length; ++i) {
      if (contents[i] != null) {
        _highestUsed = i;
      }
    }
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
        _highestUsed = int.Max(_highestUsed, i);
        return true;
      }
      if (_contents[i] == null) {
        _used.Add(i);
        _contents[i] = value;
        if (_firstUnused == i) {
          _firstUnused = i + 1;
        }
        _highestUsed = int.Max(_highestUsed, i);
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
    if (i == _highestUsed) {
      --_highestUsed;
      while (_highestUsed >= 0 && _contents[_highestUsed] == null) {
        --_highestUsed;
      }
    }
    _firstUnused = int.Min(_firstUnused, i);
    _used.RemoveAt(_used.Count - 1);
    return value;
  }

  public IReadOnlyList<ItemStack> Contents => _contents;
  public int HighestUsed => _highestUsed;

  private readonly List<ItemStack> _contents;
  private readonly List<int> _used = new();
  private int _firstUnused;
  private int _highestUsed;
}
