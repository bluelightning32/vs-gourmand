using System.Reflection;
using System.Text;

using HarmonyLib;

using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Gourmand.Blocks;

#pragma warning disable IDE1006

/// <summary>
/// Patches BlockPie to walk the behaviors on GetHeldItemInfo method.
/// </summary>
[HarmonyPatch(typeof(BlockPie))]
class BlockPiePatch {
  [HarmonyPrepare]
  public static bool Prepare(MethodBase original) {
    if (original != null) {
      // Prepare was already called for the class. Now it is getting called for
      // every method.
      return true;
    }
    if (Harmony.GetPatchInfo(typeof(BlockMeal).GetMethod("GetHeldItemInfo")) !=
        null) {
      GourmandSystem.Logger.Debug(
          "BlockPie.GetHeldItemInfo is already patched. " +
          "Skipping Gourmand's patch.");
      return false;
    }
    return true;
  }

  [HarmonyPostfix]
  [HarmonyPatch("GetHeldItemInfo")]
  public static void GetHeldItemInfo(BlockPie __instance, ItemSlot inSlot,
                                     StringBuilder dsc, IWorldAccessor world,
                                     bool withDebugInfo) {
    if (__instance.Class == "NotifyingPie") {
      // The NotifyingPie class from Novelty already forwards GetHeldItemInfo to
      // behaviors. So return here to avoid calling the behaviors twice.z`
      return;
    }
    // The base class skips calling the behaviors. So call all the behaviors
    // after getting the base text, so that the ShowPoints behavior is called.
    foreach (var bh in __instance.CollectibleBehaviors) {
      bh.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
    }
  }
}

#pragma warning restore IDE1006
