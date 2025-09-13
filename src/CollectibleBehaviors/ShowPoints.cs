using System.Collections.Generic;
using System.Text;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Gourmand.CollectibleBehaviors;

/// <summary>
/// Show the available Gourmand points for the item in the item info. This
/// behavior should only be installed on the client side.
/// </summary>
public class ShowPoints : CollectibleBehavior {
  public ShowPoints(CollectibleObject collObj) : base(collObj) {}

  public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc,
                                       IWorldAccessor world,
                                       bool withDebugInfo) {
    if (world.Api is not ICoreClientAPI capi) {
      // This should only be called on the client side, but return here in case
      // someone accidentally added this behavior on the server side.
      return;
    }
    ITreeAttribute modData =
        FoodAchievements.GetModData(capi.World.Player.Entity);
    GourmandSystem gourmand = capi.ModLoader.GetModSystem<GourmandSystem>();

    int addPoints = gourmand.FoodAchievements.GetAvailableFoodPoints(
        world, gourmand.CatDict, modData, inSlot.Itemstack);
    if (addPoints == -1) {
      // It didn't match any achievements.
      return;
    }
    if (gourmand.FoodAchievements.HideExactFoodPoints) {
      if (addPoints > 0) {
        dsc.AppendLine(Lang.Get("gourmand:available-points-vague"));
      } else {
        dsc.AppendLine(Lang.Get("gourmand:already-eaten"));
      }
    } else {
      dsc.AppendLine(Lang.Get("gourmand:available-points", addPoints));
    }
    List<string> hints = gourmand.FoodAchievements.GetHints(
        world, gourmand.CatDict, inSlot.Itemstack);
    foreach (string hint in hints) {
      dsc.AppendLine(Lang.Get(hint));
    }
  }
}
