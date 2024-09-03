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
    behavior?.SetCurrentFood(api, inSlot?.Itemstack);
    float remaining = base.Consume(world, eatingPlayer, inSlot, contentStacks,
                                   remainingServings, mulwithStackSize);
    behavior?.SetCurrentFood(api, null);
    return remaining;
  }
}
