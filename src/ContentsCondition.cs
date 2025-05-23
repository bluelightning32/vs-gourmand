using System;
using System.Collections.Generic;
using System.Linq;

using Gourmand.Collectibles;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand;

public class ContentsCondition : ICondition {
  public readonly SlotCondition[] Slots;
  public readonly int MinSlots;

  public ContentsCondition(SlotCondition[] slots, int minSlots) {
    Slots = slots ?? Array.Empty<SlotCondition>();
    MinSlots = minSlots;
  }

  public IEnumerable<AssetLocation> Categories =>
      Slots.SelectMany(s => s.OutputCategories);

  public void AppendValue(IWorldAccessor resolver,
                          IReadonlyCategoryDict catdict, AssetLocation category,
                          ItemStack stack, List<IAttribute> result) {
    ItemStack[] contents = ContentBuilder.GetContents(resolver, stack);
    foreach (SlotCondition s in Slots) {
      if (s.OutputCategories.Contains(category)) {
        s.AppendValue(resolver, catdict, category, contents, result);
      }
    }
  }

  private IEnumerable<ValueTuple>
  GetInputEnumerable(IWorldAccessor resolver, ContentBuilder builder,
                     IEnumerable<ItemStack> matches) {
    matches ??= Array.Empty<ItemStack>();
    foreach (ItemStack s in matches) {
      builder.Set(resolver, s, MinSlots);
      yield return new();
    }
  }

  private IEnumerable<ValueTuple>
  GetAllSlotsEnumerable(IWorldAccessor resolver, IReadonlyCategoryDict catdict,
                        ContentBuilder builder, IEnumerable<ValueTuple> input) {
    IEnumerable<ValueTuple> result = input;
    foreach (SlotCondition s in Slots) {
      result = s.EnumerateMatchContents(resolver, catdict, builder, result);
    }
    return result;
  }

  public IEnumerable<ItemStack> EnumerateMatches(IWorldAccessor resolver,
                                                 IReadonlyCategoryDict catdict,
                                                 IEnumerable<ItemStack> input) {
    ContentBuilder builder = new();
    foreach (var _ in GetAllSlotsEnumerable(
                 resolver, catdict, builder,
                 GetInputEnumerable(resolver, builder, input))) {
      yield return builder.GetItemStack(resolver);
    }
  }

  public bool IsMatch(IWorldAccessor resolver, IReadonlyCategoryDict catdict,
                      ItemStack stack) {
    ItemStack[] contents = ContentBuilder.GetContents(resolver, stack);
    if (!Slots.All(s => s.IsMatch(resolver, catdict, contents))) {
      return false;
    }
    return contents.All(c => c == null);
  }

  public bool Validate(IWorldAccessor resolver, ILogger logger,
                       IReadonlyCategoryDict catdict) {
    bool result = true;
    foreach (SlotCondition slot in Slots) {
      result &= slot.Validate(resolver, logger, catdict);
    }
    return result;
  }

  public string ExplainMismatch(IWorldAccessor resolver,
                                IReadonlyCategoryDict catdict,
                                ItemStack stack) {
    ItemStack[] contents = ContentBuilder.GetContents(resolver, stack);
    for (int i = 0; i < Slots.Length; ++i) {
      SlotCondition s = Slots[i];
      string mismatch = s.ExplainMismatch(resolver, catdict, contents);
      if (mismatch != null) {
        return $"Slot condition {i} does not match: {mismatch}";
      }
    }
    for (int i = 0; i < contents.Length; ++i) {
      if (contents[i] != null) {
        return $"Content index {i} contents {contents[i].Collectible} was not matched.";
      }
    }
    return null;
  }
}
