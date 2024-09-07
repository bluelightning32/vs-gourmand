using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Gourmand;

public class AchievementPoints {
  public readonly int Points;
  public readonly int BonusAt;
  public readonly int Bonus;

  public AchievementPoints(int points, int bonusAt, int bonus) {
    Points = points;
    BonusAt = bonusAt;
    Bonus = bonus;
  }

  public int GetPoints(int eaten) {
    int points = Points * eaten;
    if (eaten >= BonusAt) {
      points += Bonus;
    }
    return points;
  }
}

public class HealthFunctionPiece : IComparable<HealthFunctionPiece> {
  public readonly int Points;
  public readonly float Health;

  public HealthFunctionPiece(int points, int health) {
    Points = points;
    Health = health;
  }

  public int CompareTo(HealthFunctionPiece other) {
    return Points - other.Points;
  }
}

[JsonObject(MemberSerialization.OptIn)]
public class FoodAchievements {
  [JsonProperty("achievements")]
  public readonly IReadOnlyDictionary<string, AchievementPoints>
      RawAchievements;
  private readonly Dictionary<AssetLocation, AchievementPoints> _achievements;
  [JsonProperty]
  public readonly HealthFunctionPiece[] HealthPoints;
  private readonly SortedSet<HealthFunctionPiece> _healthFunc;

  public FoodAchievements(
      [
        JsonProperty("achievements")
      ] Dictionary<string, AchievementPoints> rawAchievements,
      [JsonProperty("healthPoints")] HealthFunctionPiece[] healthPoints) {
    RawAchievements = rawAchievements;
    _achievements = new();
    HealthPoints = healthPoints;

    _healthFunc = new();
    foreach (var entry in healthPoints) {
      _healthFunc.Add(entry);
    }
  }

  public void Resolve(string domain) {
    _achievements.Clear();
    foreach (var entry in RawAchievements) {
      _achievements.Add(AssetLocation.Create(entry.Key, domain), entry.Value);
    }
  }

  public float GetHealthFunctionPiece(int points, out float gainRate,
                                      out int untilPoints) {
    HealthFunctionPiece upto = new(points, 0);
    HealthFunctionPiece starting = new(points + 1, 0);
    if (points < _healthFunc.Min.Points) {
      gainRate = 0;
      untilPoints = _healthFunc.Min.Points;
      return 0;
    }
    SortedSet<HealthFunctionPiece> beforeView =
        _healthFunc.GetViewBetween(_healthFunc.Min, upto);
    HealthFunctionPiece before = beforeView.Max;

    if (points >= _healthFunc.Max.Points) {
      if (beforeView.Count > 1) {
        HealthFunctionPiece secondToLast = beforeView.Reverse().Skip(1).First();
        gainRate = (before.Health - secondToLast.Health) /
                   (before.Points - secondToLast.Points);
        untilPoints = int.MaxValue;
      } else {
        gainRate = 0;
        untilPoints = int.MaxValue;
      }
    } else {
      SortedSet<HealthFunctionPiece> afterView =
          _healthFunc.GetViewBetween(starting, _healthFunc.Max);
      HealthFunctionPiece after = afterView.Min;
      gainRate =
          (after.Health - before.Health) / (after.Points - before.Points);
      untilPoints = after.Points;
    }
    return before.Health + (points - before.Points) * gainRate;
  }

  public float GetHealthForPoints(int points) {
    return GetHealthFunctionPiece(points, out float gainRate,
                                  out int untilPoints);
  }

  public int GetPointsForAchievements(ILogger logger, ITreeAttribute moddata) {
    int points = 0;
    ITreeAttribute achieved = moddata.GetOrAddTreeAttribute("achieved");
    foreach (var achievement in achieved) {
      if (!_achievements.TryGetValue(new AssetLocation(achievement.Key),
                                     out AchievementPoints rating)) {
        logger?.Warning($"Category {achievement.Key} not found");
        continue;
      }
      int eaten = ((ITreeAttribute)achievement.Value).Count;
      points += rating.GetPoints(eaten);
    }
    return points;
  }

  public int AddAchievements(IWorldAccessor resolver, CategoryDict catdict,
                             ITreeAttribute moddata, ItemStack eaten) {
    int newPoints = 0;
    ITreeAttribute achieved = moddata.GetOrAddTreeAttribute("achieved");
    ITreeAttribute lost = moddata.GetOrAddTreeAttribute("lost");
    foreach (var entry in _achievements) {
      CategoryValue value = catdict.GetValue(resolver, entry.Key, eaten);
      if (value == null || value.Value == null) {
        continue;
      }
      // This is indexed by the category value as a string, and the value is an
      // item stack.
      ITreeAttribute eatenValues =
          achieved.GetOrAddTreeAttribute(entry.Key.ToString());
      string categoryValue =
          string.Join(",", value.Value.Select(a => a.ToString()));
      if (eatenValues.HasAttribute(categoryValue)) {
        continue;
      }
      eatenValues.SetItemstack(categoryValue, eaten);
      newPoints += entry.Value.GetPoints(eatenValues.Count) -
                   entry.Value.GetPoints(eatenValues.Count - 1);
      ITreeAttribute lostValues =
          lost.GetOrAddTreeAttribute(entry.Key.ToString());
      lostValues.RemoveAttribute(categoryValue);
    }
    return newPoints;
  }

  public int RemoveAchievements(IWorldAccessor resolver, CategoryDict catdict,
                                ITreeAttribute moddata, ItemStack food) {
    int lostPoints = 0;
    ITreeAttribute achieved = moddata.GetOrAddTreeAttribute("achieved");
    ITreeAttribute lost = moddata.GetOrAddTreeAttribute("lost");
    foreach (var entry in _achievements) {
      CategoryValue value = catdict.GetValue(resolver, entry.Key, food);
      if (value == null || value.Value == null) {
        continue;
      }
      // This is indexed by the category value as a string, and the value is an
      // item stack.
      ITreeAttribute eatenValues =
          achieved.GetOrAddTreeAttribute(entry.Key.ToString());
      string categoryValue =
          string.Join(",", value.Value.Select(a => a.ToString()));
      if (!eatenValues.HasAttribute(categoryValue)) {
        continue;
      }
      eatenValues.RemoveAttribute(categoryValue);
      ITreeAttribute lostValues =
          lost.GetOrAddTreeAttribute(entry.Key.ToString());
      lostValues.SetItemstack(categoryValue, food);
      lostPoints += entry.Value.GetPoints(eatenValues.Count + 1) -
                    entry.Value.GetPoints(eatenValues.Count);
    }
    return lostPoints;
  }

  public static void ClearAchievements(ITreeAttribute moddata) {
    moddata.RemoveAttribute("achieved");
    moddata.RemoveAttribute("lost");
  }

  public static IEnumerable<ItemStack> GetLost(IWorldAccessor resolver,
                                               ITreeAttribute moddata) {
    ITreeAttribute lost = moddata.GetOrAddTreeAttribute("lost");
    foreach (KeyValuePair<string, IAttribute> entry in lost) {
      ITreeAttribute lostValues = (ITreeAttribute)entry.Value;
      foreach (KeyValuePair<string, IAttribute> lostEntry in lostValues) {
        ItemStack stack = ((ItemstackAttribute)lostEntry.Value).value;
        stack.ResolveBlockOrItem(resolver);
        yield return stack;
      }
    }
  }

  public IEnumerable<ItemStack> GetMissing(IWorldAccessor resolver,
                                           CategoryDict catdict,
                                           AssetLocation category,
                                           ITreeAttribute moddata) {
    if (!_achievements.ContainsKey(category)) {
      yield break;
    }
    ITreeAttribute achieved = moddata.GetOrAddTreeAttribute("achieved");
    ITreeAttribute eatenValues =
        achieved.GetOrAddTreeAttribute(category.ToString());
    HashSet<string> given = new();
    foreach (ItemStack stack in catdict.EnumerateMatches(resolver, category)) {
      CategoryValue value = catdict.GetValue(resolver, category, stack);
      string categoryValue =
          string.Join(",", value.Value.Select(a => a.ToString()));
      if (eatenValues.HasAttribute(categoryValue)) {
        continue;
      }
      if (!given.Add(categoryValue)) {
        continue;
      }
      yield return stack;
    }
  }

  public Dictionary<AssetLocation, Tuple<int, AchievementPoints>>
  GetAchievementStats(ITreeAttribute moddata) {
    Dictionary<AssetLocation, Tuple<int, AchievementPoints>> result = new();
    ITreeAttribute achieved = moddata.GetOrAddTreeAttribute("achieved");
    foreach (var entry in _achievements) {
      // This is indexed by the category value as a string, and the value is an
      // item stack.
      ITreeAttribute eatenValues =
          achieved.GetOrAddTreeAttribute(entry.Key.ToString());
      result.Add(entry.Key, new Tuple<int, AchievementPoints>(eatenValues.Count,
                                                              entry.Value));
    }
    return result;
  }

  public static readonly string ModDataPath = "gourmand";

  public static ITreeAttribute GetModData(Entity entity) {
    return entity.WatchedAttributes.GetOrAddTreeAttribute(ModDataPath);
  }
}