using Gourmand.EntityBehaviors;

using Vintagestory.API.Common;

namespace Gourmand.CollectibleBehaviors;

public class FoodEaten : CollectibleBehavior {
  private ICoreAPI _api;

  public FoodEaten(CollectibleObject collObj) : base(collObj) {
  }

  public override void OnLoaded(ICoreAPI api) {
    base.OnLoaded(api);
    _api = api;
  }

  public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling) {
    base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
    if (secondsUsed >= 0.95f) {
      byEntity.GetBehavior<UpdateFoodAchievements>()?.SetCurrentFood(_api, slot?.Itemstack);
    }
  }
}
