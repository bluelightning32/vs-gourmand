using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand;

public class ItemStackCategoryCondition : IItemStackCondition {
  [JsonProperty(Required = Required.Always)]
  readonly public AssetLocation Input;
  [JsonProperty]
  public readonly AssetLocation[] Outputs;

  public IEnumerable<AssetLocation> Categories => Outputs;

  public ItemStackCategoryCondition(AssetLocation input,
                                    AssetLocation[] output) {
    Input = input;
    Outputs = output ?? Array.Empty<AssetLocation>();
  }

  public bool IsMatch(IReadonlyCategoryDict catdict, ItemStack stack) {
    return catdict.InCategory(Input, stack.Collectible);
  }

  public List<IAttribute> GetValue(IReadonlyCategoryDict catdict,
                                   AssetLocation category, ItemStack stack) {
    // All of the output categories have the same value for any given ItemStack.
    // The behavior is undefined if stack is not in the category. So skip
    // checking whether the stack is in the Input category.
    return catdict.GetValue(Input, stack.Collectible).Value;
  }

  public void EnumerateMatches(IReadonlyCategoryDict catdict,
                               ref List<ItemStack> matches) {
    if (matches == null) {
      matches = catdict.EnumerateMatches(Input)
                    .Select(c => new ItemStack(c))
                    .ToList();
    } else {
      matches.RemoveAll((c) => !IsMatch(catdict, c));
    }
  }
}
