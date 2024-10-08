using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand.Collectibles;

abstract public class NutritionConditionBase : ICondition {
  [JsonProperty]
  public readonly AssetLocation[] Outputs;

  [JsonIgnore]
  public IEnumerable<AssetLocation> Categories => Outputs;

  public NutritionConditionBase(AssetLocation[] outputs) {
    Outputs = outputs ?? Array.Empty<AssetLocation>();
  }

  public void EnumerateMatches(MatchResolver resolver,
                               ref List<CollectibleObject> matches) {
    matches ??= resolver.Resolver.Blocks
                    .Concat<CollectibleObject>(resolver.Resolver.Items)
                    .ToList();
    matches.RemoveAll((c) => !IsMatch(c.NutritionProps));
  }

  abstract public bool IsMatch(FoodNutritionProperties nutrition);

  abstract public IAttribute
  GetCategoryValue(FoodNutritionProperties nutrition);

  public IEnumerable<KeyValuePair<AssetLocation, IAttribute[]>>
  GetCategories(IReadonlyCategoryDict catdict, CollectibleObject match) {
    foreach (AssetLocation category in Outputs) {
      yield return new KeyValuePair<AssetLocation, IAttribute[]>(
          category,
          new IAttribute[] { GetCategoryValue(match.NutritionProps) });
    }
  }
}

public class NutritionCategoryCondition : NutritionConditionBase {
  [JsonProperty]
  public readonly EnumFoodCategory? Value;

  public NutritionCategoryCondition(EnumFoodCategory? value,
                                    AssetLocation[] outputs)
      : base(outputs) {
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

  public NutritionSatietyCondition(float min, float max,
                                   AssetLocation[] outputs)
      : base(outputs) {
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

  public NutritionHealthCondition(float min, float max, AssetLocation[] outputs)
      : base(outputs) {
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
