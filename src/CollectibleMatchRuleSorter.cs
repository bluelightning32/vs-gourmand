using System;
using System.Collections.Generic;
using System.Text;

using Vintagestory.API.Common;

namespace Gourmand;

public struct RuleOrCategory {
  public CollectibleMatchRule Rule = null;
  public AssetLocation Category = null;

  public RuleOrCategory(CollectibleMatchRule rule) { Rule = rule; }

  public RuleOrCategory(AssetLocation category) { Category = category; }
}

public class CollectibleMatchRuleSorter {
  public readonly List<RuleOrCategory> Result = new();

  // Tracks all categories and which rules generate those categories.
  private readonly Dictionary<AssetLocation, List<CollectibleMatchRule>>
      _categoryGenerators = new();
  private readonly int _totalRules = 0;
  private readonly HashSet<CollectibleMatchRule> _visitedRules = new();
  private readonly HashSet<AssetLocation> _visitedCategories = new();
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
      VisitCategory(kv.Key);
    }
  }

  private void VisitCategory(AssetLocation category) {
    if (_visitedCategories.Contains(category)) {
      return;
    }
    _path.Add(category);
    CheckForCycle();
    foreach (CollectibleMatchRule rule in _categoryGenerators[category]) {
      VisitRule(rule);
    }
    Result.Add(new RuleOrCategory(category));
    _visitedCategories.Add(category);
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

  private void VisitRule(CollectibleMatchRule rule) {
    if (_visitedRules.Contains(rule)) {
      return;
    }
    foreach (AssetLocation dependency in rule.Dependencies) {
      VisitCategory(dependency);
    }
    _visitedRules.Add(rule);
    Result.Add(new RuleOrCategory(rule));
  }
}
