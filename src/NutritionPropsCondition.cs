using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand;

abstract public class NutritionConditionBase : ICollectibleCondition {
  [JsonProperty]
  public readonly AssetLocation[] Outputs;

  public IEnumerable<AssetLocation> Categories => Outputs;

  public NutritionConditionBase(AssetLocation[] output) { Outputs = output; }

  public void EnumerateMatches(MatchResolver resolver, EnumItemClass itemClass,
                               ref List<CollectibleObject> matches) {
    matches ??= itemClass switch {
      EnumItemClass.Block =>
          resolver.Resolver.Blocks.ToList<CollectibleObject>(),
      EnumItemClass.Item => resolver.Resolver.Items.ToList<CollectibleObject>(),
      _ => throw new ArgumentException("Invalid enum value", nameof(itemClass)),
    };
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

  public NutritionCategoryCondition(EnumFoodCategory value,
                                    AssetLocation[] output)
      : base(output) {
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
  [JsonProperty]
  [DefaultValue(float.NegativeInfinity)]
  public readonly float Min;
  [JsonProperty]
  [DefaultValue(float.PositiveInfinity)]
  public readonly float Max;

  public NutritionSatietyCondition(float min, float max, AssetLocation[] output)
      : base(output) {
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

public class NutritionPropsCondition : AggregateCondition {
  [JsonProperty]
  readonly public NutritionCategoryCondition Category;

  [JsonProperty]
  readonly public NutritionSatietyCondition Satiety;

  public NutritionPropsCondition(NutritionCategoryCondition category,
                                 NutritionSatietyCondition satiety) {
    Category = category;
    Satiety = satiety;
  }

  public override IEnumerable<ICollectibleCondition> Conditions {
    get {
      yield return Category;
      yield return Satiety;
    }
  }
}
