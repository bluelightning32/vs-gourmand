using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

using Vintagestory.API.Common;
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

  public float GetHealthForPoints(int points) {
    HealthFunctionPiece upto = new(points, 0);
    HealthFunctionPiece starting = new(points + 1, 0);
    if (points < _healthFunc.Min.Points) {
      return 0;
    }
    SortedSet<HealthFunctionPiece> beforeView =
        _healthFunc.GetViewBetween(_healthFunc.Min, upto);
    HealthFunctionPiece before = beforeView.Max;

    float healthOverPoints = 0;
    if (points > _healthFunc.Max.Points) {
      if (beforeView.Count > 1) {
        HealthFunctionPiece secondToLast = beforeView.Reverse().Skip(1).First();
        healthOverPoints = (before.Health - secondToLast.Health) /
                           (before.Points - secondToLast.Points);
      }
    } else {
      SortedSet<HealthFunctionPiece> afterView =
          _healthFunc.GetViewBetween(upto, _healthFunc.Max);
      HealthFunctionPiece after = afterView.Min;
      if (after.Points != before.Points) {
        healthOverPoints =
            (after.Health - before.Health) / (after.Points - before.Points);
      }
    }
    return before.Health + (points - before.Points) * healthOverPoints;
  }

  public int GetPointsForAchievements(ILogger logger,
                                      ITreeAttribute achievements) {
    int points = 0;
    foreach (var achievement in achievements) {
      if (!_achievements.TryGetValue(new AssetLocation(achievement.Key),
                                     out AchievementPoints rating)) {
        logger?.Warning($"Category {achievement.Key} not found");
        continue;
      }
      int eaten = ((ITreeAttribute)achievement.Value).Count;
      points += rating.Points * eaten;
      if (eaten >= rating.BonusAt) {
        points += rating.Bonus;
      }
    }
    return points;
  }

  public bool UpdateAchievements(IWorldAccessor resolver, CategoryDict catdict,
                                 ITreeAttribute achievements, ItemStack eaten) {
    bool modified = false;
    foreach (var entry in _achievements) {
      CategoryValue value = catdict.GetValue(resolver, entry.Key, eaten);
      if (value == null || value.Value == null) {
        continue;
      }
      // This is indexed by the category value as a string, and the value is an
      // item stack.
      ITreeAttribute eatenValues =
          achievements.GetOrAddTreeAttribute(entry.Key.ToString());
      string categoryValue =
          string.Join(",", value.Value.Select(a => a.ToString()));
      if (eatenValues.HasAttribute(categoryValue)) {
        continue;
      }
      eatenValues.SetItemstack(categoryValue, eaten);
      modified = true;
    }
    return modified;
  }
}
