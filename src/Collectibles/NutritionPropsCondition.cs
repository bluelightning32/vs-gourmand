using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand.Collectibles;

abstract public class NutritionConditionBase : ICondition {
  /// <summary>
  /// When true, try to get the nutritional properties of the item when it is
  /// cooked, and fallback to the uncooked nutritional properties if there are
  /// no cooked properties.
  ///
  /// When false, get the nutritional properties of the raw item.
  /// </summary>
  [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
  [DefaultValue(false)]
  public readonly bool Cooked;

  [JsonProperty]
  public readonly AssetLocation[] Outputs;

  [JsonIgnore]
  public IEnumerable<AssetLocation> Categories => Outputs;

  public NutritionConditionBase(bool cooked, AssetLocation[] outputs) {
    Cooked = cooked;
    Outputs = outputs ?? Array.Empty<AssetLocation>();
  }

  public FoodNutritionProperties GetProps(IWorldAccessor resolver,
                                          CollectibleObject c) {
    if (Cooked) {
      if (c.CombustibleProps?.SmeltedStack != null) {
        c.CombustibleProps.SmeltedStack.Resolve(resolver, "gourmand");
        var resolved = c.CombustibleProps.SmeltedStack.ResolvedItemstack;
        if (resolved != null) {
          c = resolved.Collectible;
        } else {
          GourmandSystem.Logger.Warning(
              $"Failed to resolve {c.CombustibleProps.SmeltedStack.Code}, " +
              $"which is the cooked form of {c.Code}. Treating it as an " +
              "uncookable food instead.");
        }
      }
      JsonObject inMeal = c.Attributes?["nutritionPropsWhenInMeal"];
      if (inMeal?.Exists == true) {
        return inMeal.AsObject<FoodNutritionProperties>();
      }
    }
    return c.NutritionProps;
  }

  public void EnumerateMatches(MatchResolver resolver,
                               ref List<CollectibleObject> matches) {
    matches ??= resolver.Resolver.Blocks
                    .Concat<CollectibleObject>(resolver.Resolver.Items)
                    .ToList();
    matches.RemoveAll((c) => !IsMatch(GetProps(resolver.Resolver, c)));
  }

  abstract public bool IsMatch(FoodNutritionProperties nutrition);

  abstract public IAttribute
  GetCategoryValue(FoodNutritionProperties nutrition);

  public IEnumerable<KeyValuePair<AssetLocation, IAttribute[]>>
  GetCategories(IWorldAccessor resolver, IReadonlyCategoryDict catdict,
                CollectibleObject match) {
    foreach (AssetLocation category in Outputs) {
      yield return new KeyValuePair<AssetLocation, IAttribute[]>(
          category,
          new IAttribute[] { GetCategoryValue(GetProps(resolver, match)) });
    }
  }
}

public class NutritionCategoryCondition : NutritionConditionBase {
  [JsonProperty]
  public readonly EnumFoodCategory? Value;

  public NutritionCategoryCondition(bool cooked, EnumFoodCategory? value,
                                    AssetLocation[] outputs)
      : base(cooked, outputs) {
    Value = value;
  }

  public override bool IsMatch(FoodNutritionProperties nutrition) {
    if (nutrition == null) {
      return false;
    }
    if (Value != null) {
      return nutrition.FoodCategory == Value;
    }
    return true;
  }

  public override
      IAttribute GetCategoryValue(FoodNutritionProperties nutrition) {
    return new StringAttribute(nutrition.FoodCategory.ToString());
  }
}

public class NutritionSatietyCondition : NutritionConditionBase {
  [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
  [DefaultValue(float.NegativeInfinity)]
  public readonly float Min;
  [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
  [DefaultValue(float.PositiveInfinity)]
  public readonly float Max;

  public NutritionSatietyCondition(bool cooked, float min, float max,
                                   AssetLocation[] outputs)
      : base(cooked, outputs) {
    Min = min;
    Max = max;
  }

  public override bool IsMatch(FoodNutritionProperties nutrition) {
    if (nutrition == null) {
      return false;
    }
    return Min <= nutrition.Satiety && nutrition.Satiety <= Max;
  }

  public override
      IAttribute GetCategoryValue(FoodNutritionProperties nutrition) {
    return new FloatAttribute(nutrition.Satiety);
  }
}

public class NutritionHealthCondition : NutritionConditionBase {
  [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
  [DefaultValue(float.NegativeInfinity)]
  public readonly float Min;
  [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
  [DefaultValue(float.PositiveInfinity)]
  public readonly float Max;

  public NutritionHealthCondition(bool cooked, float min, float max,
                                  AssetLocation[] outputs)
      : base(cooked, outputs) {
    Min = min;
    Max = max;
  }

  public override bool IsMatch(FoodNutritionProperties nutrition) {
    if (nutrition == null) {
      return false;
    }
    return Min <= nutrition.Health && nutrition.Health <= Max;
  }

  public override
      IAttribute GetCategoryValue(FoodNutritionProperties nutrition) {
    return new FloatAttribute(nutrition.Health);
  }
}

public class NutritionPropsCondition : AggregateCondition {
  [JsonProperty]
  readonly public NutritionCategoryCondition Category;

  [JsonProperty]
  readonly public NutritionSatietyCondition Satiety;

  [JsonProperty]
  readonly public NutritionHealthCondition Health;

  public NutritionPropsCondition(NutritionCategoryCondition category,
                                 NutritionSatietyCondition satiety,
                                 NutritionHealthCondition health) {
    Category = category;
    Satiety = satiety;
    Health = health;
  }

  [JsonIgnore]
  public override IEnumerable<ICondition> Conditions {
    get {
      if (Category != null) {
        yield return Category;
      }
      if (Satiety != null) {
        yield return Satiety;
      }
      if (Health != null) {
        yield return Health;
      }
    }
  }
}
