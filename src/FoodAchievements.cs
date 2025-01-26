using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Newtonsoft.Json;

using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Gourmand;

public class AchievementPoints {
  /// <summary>
  /// This is a list of modids that the achievement depends on. Ignore this
  /// achievement if one of the mods in this list is not installed.
  /// </summary>
  [JsonProperty("dependsOn")]
  public ModDependency[] DependsOn { get; private set; }

  /// <summary>
  /// Only include this achievement if at least one of the mods in the list is
  /// installed. DependsOnAny is ignored when it is null.
  /// </summary>
  [JsonProperty("dependsOnAny")]
  public ModDependency[] DependsOnAny { get; private set; }

  /// <summary>
  /// Ignore this achievement if the total BonusAs is 0. This is useful for
  /// setting the default BonusAs to 0, then adding to the bonus if any of
  /// several mods are installed. If none of the mods are installed, the bonus
  /// remains at zero, and the achivement is hidden. If any of the mods are
  /// installed, then the achievement is shown.
  /// </summary>
  [JsonProperty("hideIfNoBonusAt")]
  [DefaultValue(false)]
  public bool HideIfNoBonusAt { get; private set; }

  /// <summary>
  /// The points gained by eating each collectible in the category.
  /// </summary>
  [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
  [DefaultValue(1)]
  public int Points { get; private set; }

  [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
  [DefaultValue(0)]
  public int BonusAt { get; private set; }

  [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
  [DefaultValue(0)]
  public int Bonus { get; private set; }
  [JsonProperty("description")]
  public string Description { get; private set; }

  /// <summary>
  /// The Points, BonusAt, and Bonus values will get the sum of all the
  /// corresponding fields of the entries in this list. The intention is that
  /// the elements in the list only exist when certain mods are installed.
  /// </summary>
  [JsonProperty("add")]
  public AchievementPoints[] Add { get; private set; }

  public AchievementPoints(ModDependency[] dependsOn,
                           ModDependency[] dependsOnAny, int points,
                           int bonusAt, int bonus, AchievementPoints[] add) {
    DependsOn = dependsOn ?? Array.Empty<ModDependency>();
    DependsOnAny = dependsOnAny;
    Points = points;
    BonusAt = bonusAt;
    Bonus = bonus;
    Add = add;
  }

  public int GetPoints(int eaten) {
    int points = Points * eaten;
    if (eaten >= BonusAt) {
      points += Bonus;
    }
    return points;
  }

  public bool DependsOnSatisified(IModLoader loader) {
    if (DependsOnAny != null && !DependsOn.Any(d => d.IsSatisified(loader))) {
      return false;
    }
    return DependsOn.All(d => d.IsSatisified(loader));
  }

  /// <summary>
  /// Accumulates and clears all entries in <see cref="Add"/>.
  /// </summary>
  /// <param name="loader"></param>
  /// <returns>true if the achievement should be shown, or returns false if the
  /// mod should be hidden due to the top level DependsOn or
  /// HideIfNoBonusAt.</returns>
  public bool Resolve(IModLoader loader) {
    if (!DependsOnSatisified(loader)) {
      Points = 0;
      BonusAt = 0;
      Bonus = 0;
      Add = null;
      return false;
    }
    foreach (AchievementPoints add in Add ?? Array.Empty<AchievementPoints>()) {
      if (!add.Resolve(loader)) {
        continue;
      }
      Points += add.Points;
      BonusAt += add.BonusAt;
      Bonus += add.Bonus;
      if (add.Description != null) {
        Description = add.Description;
      }
    }
    Add = null;
    if (BonusAt == 0 && HideIfNoBonusAt) {
      Points = 0;
      Bonus = 0;
      return false;
    }
    return true;
  }
}

public class HealthFunctionPiece : IComparable<HealthFunctionPiece> {
  /// <summary>
  /// This is a list of modids that the achievement depends on. Ignore this
  /// achievement if one of the mods in this list is not installed.
  /// </summary>
  [JsonProperty("dependsOn")]
  public ModDependency[] DependsOn { get; private set; }

  public int Points { get; private set; }
  public float Health { get; private set; }

  /// <summary>
  /// The Points and Health values will get the sum of all the corresponding
  /// fields of the entries in this list. The intention is that the elements in
  /// the list only exist when certain mods are installed.
  /// </summary>
  [JsonProperty("add")]
  public HealthFunctionPiece[] Add {
    get; private set;
  }

  public HealthFunctionPiece(ModDependency[] dependsOn, int points, int health,
                             HealthFunctionPiece[] add) {
    DependsOn = dependsOn ?? Array.Empty<ModDependency>();
    Points = points;
    Health = health;
    Add = add;
  }

  public int CompareTo(HealthFunctionPiece other) {
    return Points - other.Points;
  }

  public bool DependsOnSatisified(IModLoader loader) {
    return DependsOn.All(d => d.IsSatisified(loader));
  }

  /// <summary>
  /// Accumulates and clears all entries in <see cref="Add"/>.
  /// </summary>
  /// <param name="loader"></param>
  /// <returns>true if the top level DependsOn was satisfied</returns>
  public bool Resolve(IModLoader loader) {
    if (!DependsOnSatisified(loader)) {
      Points = 0;
      Health = 0;
      Add = null;
      return false;
    }
    foreach (HealthFunctionPiece add in Add ??
             Array.Empty<HealthFunctionPiece>()) {
      if (!add.Resolve(loader)) {
        continue;
      }
      Points += add.Points;
      Health += add.Health;
    }
    Add = null;
    return true;
  }
}

public class PointBreakdown {
  public Dictionary<string, int> GrantedPoints = new();
  public Dictionary<string, int> AvailablePoints = new();
  public Dictionary<string, int> AvailableBonus = new();
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

    _healthFunc = new(healthPoints);
  }

  public void Resolve(string domain, IModLoader loader) {
    _achievements.Clear();
    foreach (var entry in RawAchievements) {
      if (!entry.Value.Resolve(loader)) {
        continue;
      }
      _achievements.Add(AssetLocation.Create(entry.Key, domain), entry.Value);
    }

    // _healthFunc needs to be cleared and rebuilt, because resolving the
    // entries can change their order.
    _healthFunc.Clear();
    foreach (var entry in HealthPoints) {
      if (!entry.Resolve(loader)) {
        continue;
      }
      _healthFunc.Add(entry);
    }
  }

  public float GetHealthFunctionPiece(int points, out float gainRate,
                                      out int untilPoints) {
    HealthFunctionPiece upto = new(null, points, 0, null);
    HealthFunctionPiece starting = new(null, points + 1, 0, null);
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

  /// <summary>
  /// Returns the points that can be earned by eating the food
  /// </summary>
  /// <param name="resolver">resolver</param>
  /// <param name="catdict">catdict</param>
  /// <param name="moddata">the player's food data</param>
  /// <param name="food">the food to get the stats for</param>
  /// <returns>0 if the food is already eaten, a positive value if it has not
  /// been eaten yet, or -1 if it does not match any achievements</returns>
  public int GetAvailableFoodPoints(IWorldAccessor resolver,
                                    CategoryDict catdict,
                                    ITreeAttribute moddata, ItemStack food) {
    int newPoints = 0;
    bool isFood = false;
    ITreeAttribute achieved = moddata.GetOrAddTreeAttribute("achieved");
    foreach (var entry in _achievements) {
      CategoryValue value = catdict.GetValue(resolver, entry.Key, food);
      if (value == null || value.Value == null) {
        continue;
      }
      isFood = true;
      // This is indexed by the category value as a string, and the value is an
      // item stack.
      ITreeAttribute eatenValues =
          achieved.GetOrAddTreeAttribute(entry.Key.ToString());
      string categoryValue =
          string.Join(",", value.Value.Select(a => a.ToString()));
      if (eatenValues.HasAttribute(categoryValue)) {
        continue;
      }
      newPoints += entry.Value.GetPoints(eatenValues.Count + 1) -
                   entry.Value.GetPoints(eatenValues.Count);
    }
    return isFood ? newPoints : -1;
  }

  /// <summary>
  /// Returns the points that can be earned by eating the food
  /// </summary>
  /// <param name="resolver">resolver</param>
  /// <param name="catdict">catdict</param>
  /// <param name="moddata">the player's food data</param>
  /// <param name="food">the food to get the stats for</param>
  /// <returns>0 if the food is already eaten, a positive value if it has not
  /// been eaten yet, or -1 if it does not match any achievements</returns>
  public PointBreakdown GetFoodPoints(IWorldAccessor resolver,
                                      CategoryDict catdict,
                                      ITreeAttribute moddata, ItemStack food) {
    PointBreakdown result = new();
    ITreeAttribute achieved = moddata.GetOrAddTreeAttribute("achieved");
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
      if (eatenValues.HasAttribute(categoryValue)) {
        result.GrantedPoints.Add(entry.Key.ToString(), entry.Value.Points);
      } else {
        result.AvailablePoints.Add(entry.Key.ToString(), entry.Value.Points);
        if (eatenValues.Count + 1 == entry.Value.BonusAt) {
          result.AvailableBonus.Add(entry.Key.ToString(), entry.Value.Bonus);
        }
      }
    }
    return result;
  }

  public int RemoveAchievements(IWorldAccessor resolver, CategoryDict catdict,
                                ITreeAttribute moddata, ItemStack food) {
    ITreeAttribute achieved = moddata.GetOrAddTreeAttribute("achieved");
    ITreeAttribute lost = moddata.GetOrAddTreeAttribute("lost");
    return RemoveAchievements(resolver, catdict, achieved, lost, food);
  }

  private int RemoveAchievements(IWorldAccessor resolver, CategoryDict catdict,
                                 ITreeAttribute achieved, ITreeAttribute lost,
                                 ItemStack food) {
    int lostPoints = 0;
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

  public static void ClearFoodCreatedTime(ItemStack stack) {
    ITreeAttribute transition =
        stack.Attributes.GetTreeAttribute("transitionstate");
    transition?.RemoveAttribute("createdTotalHours");
  }

  public static IEnumerable<ItemStack> GetLost(IWorldAccessor resolver,
                                               ITreeAttribute moddata) {
    ITreeAttribute lost = moddata.GetOrAddTreeAttribute("lost");
    foreach (KeyValuePair<string, IAttribute> entry in lost) {
      ITreeAttribute lostValues = (ITreeAttribute)entry.Value;
      foreach (KeyValuePair<string, IAttribute> lostEntry in lostValues) {
        ItemStack stack = ((ItemstackAttribute)lostEntry.Value).value;
        // Prevent foods that were added in legacy versions of the mod from
        // showing up as rotten. Current versions of the mod clear this
        // attribute before adding it to the eaten or lost food lists.
        ClearFoodCreatedTime(stack);
        stack.ResolveBlockOrItem(resolver);
        yield return stack;
      }
    }
  }

  public static IEnumerable<ItemStack> GetEaten(IWorldAccessor resolver,
                                                ITreeAttribute moddata) {
    ITreeAttribute achieved = moddata.GetOrAddTreeAttribute("achieved");
    foreach (KeyValuePair<string, IAttribute> entry in achieved) {
      ITreeAttribute eatenValues = (ITreeAttribute)entry.Value;
      foreach (KeyValuePair<string, IAttribute> eatenEntry in eatenValues) {
        ItemStack stack = ((ItemstackAttribute)eatenEntry.Value).value;
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

  /// <summary>
  /// Get a multimap of foods the player has not eaten yet, indexed by the food
  /// category value.
  /// </summary>
  /// <param name="resolver"></param>
  /// <param name="catdict"></param>
  /// <param name="category">the category to find items for</param>
  /// <param name="moddata">data about what the player has already eaten</param>
  /// <param name="maxEntries">
  /// The maximum number of keys in the dictionary. Once the limit is reached,
  /// no more entries are added, even if they would belong to existing keys.
  /// Aside from that, this only affects the number of keys, not the total
  /// number of values in the dictionary value lists.
  /// </param>
  /// <param name="missing">the output multimap</param>
  /// <returns>true if more entries are available but not returned due to
  /// maxEntries</returns>
  public bool GetMissingDict(IWorldAccessor resolver, CategoryDict catdict,
                             AssetLocation category, ITreeAttribute moddata,
                             int maxEntries,
                             out Dictionary<string, List<ItemStack>> missing) {
    missing = new();
    if (!_achievements.ContainsKey(category)) {
      return false;
    }
    ITreeAttribute achieved = moddata.GetOrAddTreeAttribute("achieved");
    ITreeAttribute eatenValues =
        achieved.GetOrAddTreeAttribute(category.ToString());
    foreach (ItemStack stack in catdict.EnumerateMatches(resolver, category)) {
      CategoryValue value = catdict.GetValue(resolver, category, stack);
      string categoryValue =
          string.Join(",", value.Value.Select(a => a.ToString()));
      if (eatenValues.HasAttribute(categoryValue)) {
        continue;
      }

      if (!missing.TryGetValue(categoryValue, out List<ItemStack> stacks)) {
        if (missing.Count >= maxEntries) {
          return true;
        }
        stacks = new();
        missing.Add(categoryValue, stacks);
      }
      stacks.Add(stack);
    }
    return false;
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

  public int ApplyDeath(IWorldAccessor resolver, CategoryDict catDict,
                        ITreeAttribute moddata, float deathPenalty) {
    Dictionary<ItemStack, bool> foodDecision = new(new ItemStackComparer(
        resolver, GlobalConstants.IgnoredStackAttributes));
    foreach (ItemStack stack in GetEaten(resolver, moddata)) {
      if (!foodDecision.ContainsKey(stack)) {
        bool lose = Random.Shared.NextSingle() < deathPenalty;
        foodDecision.Add(stack, lose);
      }
    }
    ITreeAttribute achieved = moddata.GetOrAddTreeAttribute("achieved");
    ITreeAttribute lost = moddata.GetOrAddTreeAttribute("lost");
    int lostPoints = 0;
    foreach (var entry in foodDecision) {
      if (entry.Value) {
        lostPoints +=
            RemoveAchievements(resolver, catDict, achieved, lost, entry.Key);
      }
    }
    return lostPoints;
  }

  public static implicit operator FoodAchievements(AchievementPoints v) {
    throw new NotImplementedException();
  }
}
