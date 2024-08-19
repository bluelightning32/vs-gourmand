using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Vintagestory.API.Common;

namespace Gourmand;

public class CollectibleMatchRuleSorter {
  public readonly List<CollectibleMatchRule> Result = new();

  // Tracks all categories and which rules generate those categories.
  private readonly Dictionary<AssetLocation, List<CollectibleMatchRule>>
      _categoryGenerators = new();
  private readonly int _totalRules = 0;
  private readonly HashSet<CollectibleMatchRule> _visited = new();
  private readonly List<AssetLocation> _path = new();

  public CollectibleMatchRuleSorter(IEnumerable<CollectibleMatchRule> rules) {
    foreach (CollectibleMatchRule rule in rules) {
      foreach (AssetLocation category in rule.OutputCategories) {
        if (!_categoryGenerators.TryGetValue(
                category, out List<CollectibleMatchRule> generators)) {
          generators = new();
          _categoryGenerators.Add(category, generators);
        }
        generators.Add(rule);
      }
      ++_totalRules;
    }

    foreach (KeyValuePair<AssetLocation, List<CollectibleMatchRule>> kv in
                 _categoryGenerators) {
      Visit(kv.Key);
    }
  }

  private void Visit(AssetLocation category) {
    _path.Add(category);
    CheckForCycle();
    foreach (CollectibleMatchRule rule in _categoryGenerators[category]) {
      if (_visited.Contains(rule)) {
        continue;
      }
      foreach (AssetLocation dependency in rule.Dependencies) {
        Visit(dependency);
      }
      _visited.Add(rule);
      Result.Add(rule);
    }
    _path.RemoveAt(_path.Count - 1);
  }

  private void CheckForCycle() {
    if (_path.Count <= _totalRules) {
      return;
    }
    AssetLocation last = _path[_path.Count - 1];
    int cycleStart = _path.FindLastIndex(_path.Count - 2, a => a == last) + 1;
    StringBuilder b = new("Cycle in category dependencies: ");
    for (int i = cycleStart; i < _path.Count; ++i) {
      if (i > cycleStart) {
        b.Append(", ");
      }
      b.Append(_path[i]);
    }
    throw new InvalidOperationException(b.ToString());
  }
}
