using Gourmand.EntityBehaviors;

using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Gourmand.Blocks;

class NotifyingMeal : BlockMeal {
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
}
