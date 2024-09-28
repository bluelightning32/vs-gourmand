using System.Text;

using Gourmand.EntityBehaviors;

using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Gourmand.Blocks;

class NotifyingPie : BlockPie {
  public override float Consume(IWorldAccessor world, IPlayer eatingPlayer,
                                ItemSlot inSlot, ItemStack[] contentStacks,
                                float remainingServings,
                                bool mulwithStackSize) {
    UpdateFoodAchievements behavior =
        eatingPlayer.Entity.GetBehavior<UpdateFoodAchievements>();
    behavior?.SetCurrentFood(inSlot?.Itemstack);
    float remaining = base.Consume(world, eatingPlayer, inSlot, contentStacks,
                                   remainingServings, mulwithStackSize);
    behavior?.SetCurrentFood(null);
    return remaining;
  }

  public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc,
                                       IWorldAccessor world,
                                       bool withDebugInfo) {
    base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
    // The base class skips calling the behaviors. So call all the behaviors
    // after getting the base text, so that the ShowPoints behavior is called.
    foreach (var bh in CollectibleBehaviors) {
      bh.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
    }
  }
}
