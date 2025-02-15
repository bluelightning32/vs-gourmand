using Newtonsoft.Json;

using Gourmand.Collectibles;

using System;
using System.Collections.Generic;
using System.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using System.ComponentModel;

namespace Gourmand;

public class CategoryCondition : ICondition {
  [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
  [DefaultValue(false)]
  public bool IgnoreMissing;
  [JsonProperty(Required = Required.Always)]
  readonly public AssetLocation Input;
  [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
  [DefaultValue(int.MaxValue)]
  public int EnumeratePerDistinct;
  [JsonProperty]
  public readonly AssetLocation[] Outputs;

  public IEnumerable<AssetLocation> Categories => Outputs;

  public CategoryCondition(AssetLocation input, AssetLocation[] outputs) {
    Input = input;
    Outputs = outputs ?? Array.Empty<AssetLocation>();
  }

  public bool IsMatch(IWorldAccessor resolver, IReadonlyCategoryDict catdict,
                      ItemStack stack) {
    return catdict.InCategory(Input, stack.Collectible);
  }

  public void AppendValue(IWorldAccessor resolver,
                          IReadonlyCategoryDict catdict, AssetLocation category,
                          ItemStack stack, List<IAttribute> result) {
    // All of the output categories have the same value for any given ItemStack.
    // The behavior is undefined if stack is not in the category. So skip
    // checking whether the stack is in the Input category.
    result.AddRange(catdict.GetValue(Input, stack.Collectible).Value);
  }

  public IEnumerable<ItemStack> EnumerateMatches(IWorldAccessor resolver,
                                                 IReadonlyCategoryDict catdict,
                                                 IEnumerable<ItemStack> input) {
    if (EnumeratePerDistinct == int.MaxValue) {
      if (input == null) {
        return catdict.EnumerateMatches(Input).Select(c => new ItemStack(c));
      } else {
        return input.Where((c) => IsMatch(resolver, catdict, c));
      }
    } else {
      if (input == null) {
        return catdict.EnumerateMatches(Input, EnumeratePerDistinct)
            .Select(c => new ItemStack(c));
      } else {
        Dictionary<List<IAttribute>, int> distinctCounts =
            new(new ListIAttributeComparer());
        return input.Where((c) => IsEnumerableDistinctMatch(resolver, catdict,
                                                            c, distinctCounts));
      }
    }
  }

  private bool
  IsEnumerableDistinctMatch(IWorldAccessor resolver,
                            IReadonlyCategoryDict catdict, ItemStack stack,
                            Dictionary<List<IAttribute>, int> distinctCounts) {
    List<IAttribute> value = catdict.GetValue(Input, stack.Collectible).Value;
    int count = distinctCounts.GetValueOrDefault(value, 0);
    if (count < EnumeratePerDistinct) {
      ++count;
      distinctCounts[value] = count;
      return true;
    } else {
      return false;
    }
  }

  public bool Validate(IWorldAccessor resolver, ILogger logger,
                       IReadonlyCategoryDict catdict) {
    if (!catdict.IsRegistered(Input) && !IgnoreMissing) {
      logger.Error($"Category {Input} is not registered.");
      return false;
    }
    return true;
  }
}
