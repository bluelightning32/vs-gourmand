using Gourmand.EntityBehaviors;

using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Gourmand.BlockBehaviors;

public class NotifyEaten : BlockBehavior, IContainedInteractable {
  private ICoreAPI _api;

  public NotifyEaten(Block block) : base(block) {}

  public override void OnLoaded(ICoreAPI api) {
    base.OnLoaded(api);
    _api = api;
  }

  public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot,
                                          EntityAgent byEntity,
                                          BlockSelection blockSel,
                                          EntitySelection entitySel,
                                          ref EnumHandling handling) {
    base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel,
                            ref handling);
    if (secondsUsed >= 0.95f) {
      byEntity.GetBehavior<UpdateFoodAchievements>()?.SetCurrentFood(
          slot?.Itemstack);
    }
  }

  public bool OnContainedInteractStart(BlockEntityContainer be, ItemSlot slot,
                                       IPlayer byPlayer,
                                       BlockSelection blockSel) {
    return false;
  }

  public bool OnContainedInteractStep(float secondsUsed,
                                      BlockEntityContainer be, ItemSlot slot,
                                      IPlayer byPlayer,
                                      BlockSelection blockSel) {
    return false;
  }

  public void OnContainedInteractStop(float secondsUsed,
                                      BlockEntityContainer be, ItemSlot slot,
                                      IPlayer byPlayer,
                                      BlockSelection blockSel) {
    if (secondsUsed >= 0.95f) {
      byPlayer.Entity.GetBehavior<UpdateFoodAchievements>()?.SetCurrentFood(
          slot?.Itemstack);
    }
  }

  public override void
  OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer,
                      BlockSelection blockSel, ref EnumHandling handling) {
    base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel,
                             ref handling);
    if (secondsUsed >= 0.95f) {
      ItemStack stack = block.OnPickBlock(world, blockSel.Position);
      byPlayer.Entity.GetBehavior<UpdateFoodAchievements>()?.SetCurrentFood(
          stack);
    }
  }
}
