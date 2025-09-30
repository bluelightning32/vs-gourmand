using System.Reflection;

using HarmonyLib;

using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Gourmand.Blocks;

#pragma warning disable IDE1006

/// <summary>
/// Patches BlockMeal to walk the behaviors on the OnHeldInteract,
/// OnBlockInteract, and OnContainedInteract methods.
/// </summary>
[HarmonyPatch(typeof(BlockMeal))]
class BlockMealPatch {
  private static MethodInfo _PlacedBlockEating;

  [HarmonyPrepare]
  public static bool Prepare(MethodBase original) {
    if (original != null) {
      // Prepare was already called for the class. Now it is getting called for
      // every method.
      return true;
    }
    if (Harmony.GetPatchInfo(
            typeof(BlockMeal).GetMethod("OnBlockInteractStop")) != null) {
      GourmandSystem.Logger.Debug(
          "BlockMeal.OnBlockInteractStop is already patched. " +
          "Skipping Gourmand's patch.");
      return false;
    }
    _PlacedBlockEating = typeof(BlockMeal).PropertyGetter("PlacedBlockEating");
    return true;
  }

  public static bool GetPlacedBlockEating(BlockMeal instance) {
    return (bool)_PlacedBlockEating.Invoke(instance, null);
  }

  private delegate void BlockBehaviorDelegate<T>(BlockBehavior behavior,
                                                 ref T result,
                                                 ref EnumHandling handling);
  private delegate void
  CollectibleBehaviorDelegate<T>(CollectibleBehavior behavior, ref T result,
                                 ref EnumHandling handling);

  public struct BehaviorResult<T> {
    public T Result;
    public EnumHandling Handled = EnumHandling.PassThrough;

    public BehaviorResult(T defaultResult) { Result = defaultResult; }
  }

  [HarmonyPrefix]
  [HarmonyPatch("OnBlockInteractStart")]
  public static bool
  OnBlockInteractStart(BlockMeal __instance, ref bool __result,
                       out BehaviorResult<bool> __state, IWorldAccessor world,
                       IPlayer byPlayer, BlockSelection blockSel) {
    if (!GetPlacedBlockEating(__instance)) {
      // When PlacedBlockEating is disabled, BlockMeal delegates to
      // BlockContainer, which calls the behaviors normally. So do not add an
      // extra level of calling behaviors in that case.
      __state = new(true);
      return true;
    }
    return WalkBlockBehaviors(
        __instance, true,
        (BlockBehavior behavior, ref bool result, ref EnumHandling handled) => {
          bool behaviorResult = behavior.OnBlockInteractStart(
              world, byPlayer, blockSel, ref handled);
          if (handled != EnumHandling.PassThrough) {
            result &= behaviorResult;
          }
        },
        out __state, out __result);
  }

  [HarmonyPostfix]
  [HarmonyPatch("OnBlockInteractStart")]
  public static void OnBlockInteractStartPostfix(
      BlockMeal __instance, ref bool __result, BehaviorResult<bool> __state,
      IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
    if (__state.Handled == EnumHandling.Handled) {
      __result &= __state.Result;
    }
  }

  [HarmonyPrefix]
  [HarmonyPatch("OnBlockInteractStep")]
  public static bool OnBlockInteractStep(BlockMeal __instance,
                                         ref bool __result,
                                         out BehaviorResult<bool> __state,
                                         float secondsUsed,
                                         IWorldAccessor world, IPlayer byPlayer,
                                         BlockSelection blockSel) {
    if (!GetPlacedBlockEating(__instance)) {
      // When PlacedBlockEating is disabled, BlockMeal delegates to
      // BlockContainer, which calls the behaviors normally. So do not add an
      // extra level of calling behaviors in that case.
      __state = new(true);
      return true;
    }
    return WalkBlockBehaviors(
        __instance, true,
        (BlockBehavior behavior, ref bool result, ref EnumHandling handled) => {
          bool behaviorResult = behavior.OnBlockInteractStep(
              secondsUsed, world, byPlayer, blockSel, ref handled);
          if (handled != EnumHandling.PassThrough) {
            result &= behaviorResult;
          }
        },
        out __state, out __result);
  }

  [HarmonyPostfix]
  [HarmonyPatch("OnBlockInteractStep")]
  public static void
  OnBlockInteractStepPostfix(BlockMeal __instance, ref bool __result,
                             BehaviorResult<bool> __state, float secondsUsed,
                             IWorldAccessor world, IPlayer byPlayer,
                             BlockSelection blockSel) {
    if (__state.Handled == EnumHandling.Handled) {
      __result &= __state.Result;
    }
  }

  [HarmonyPrefix]
  [HarmonyPatch("OnBlockInteractStop")]
  public static bool OnBlockInteractStop(BlockMeal __instance,
                                         float secondsUsed,
                                         IWorldAccessor world, IPlayer byPlayer,
                                         BlockSelection blockSel) {
    if (!GetPlacedBlockEating(__instance)) {
      // When PlacedBlockEating is disabled, BlockMeal delegates to
      // BlockContainer, which calls the behaviors normally. So do not add an
      // extra level of calling behaviors in that case.
      return true;
    }
    return WalkBlockBehaviors(
        __instance, true,
        (BlockBehavior behavior, ref bool result, ref EnumHandling handled) => {
          behavior.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel,
                                       ref handled);
        },
        out BehaviorResult<bool> unused, out bool unused2);
  }

  [HarmonyPrefix]
  [HarmonyPatch("OnContainedInteractStart")]
  public static bool OnContainedInteractStart(BlockMeal __instance,
                                              ref bool __result,
                                              out BehaviorResult<bool> __state,
                                              BlockEntityContainer be,
                                              ItemSlot slot, IPlayer byPlayer,
                                              BlockSelection blockSel) {
    return WalkCollectibleBehaviors(
        __instance, false,
        (CollectibleBehavior behavior, ref bool result,
         ref EnumHandling handled) => {
          if (behavior is IContainedInteractable interactable) {
            result |= interactable.OnContainedInteractStart(be, slot, byPlayer,
                                                            blockSel);
            handled = EnumHandling.Handled;
          }
        },
        out __state, out __result);
  }

  [HarmonyPostfix]
  [HarmonyPatch("OnContainedInteractStart")]
  public static void
  OnContainedInteractStartPostfix(BlockMeal __instance, ref bool __result,
                                  BehaviorResult<bool> __state,
                                  BlockEntityContainer be, ItemSlot slot,
                                  IPlayer byPlayer, BlockSelection blockSel) {
    if (__state.Handled == EnumHandling.Handled) {
      __result |= __state.Result;
    }
  }

  [HarmonyPrefix]
  [HarmonyPatch("OnContainedInteractStep")]
  public static bool
  OnContainedInteractStep(BlockMeal __instance, ref bool __result,
                          out BehaviorResult<bool> __state, float secondsUsed,
                          BlockEntityContainer be, ItemSlot slot,
                          IPlayer byPlayer, BlockSelection blockSel) {
    return WalkCollectibleBehaviors(
        __instance, false,
        (CollectibleBehavior behavior, ref bool result,
         ref EnumHandling handled) => {
          if (behavior is IContainedInteractable interactable) {
            result |= interactable.OnContainedInteractStep(
                secondsUsed, be, slot, byPlayer, blockSel);
            handled = EnumHandling.Handled;
          }
        },
        out __state, out __result);
  }

  [HarmonyPostfix]
  [HarmonyPatch("OnContainedInteractStep")]
  public static void OnContainedInteractStepPostfix(
      BlockMeal __instance, ref bool __result, BehaviorResult<bool> __state,
      float secondsUsed, BlockEntityContainer be, ItemSlot slot,
      IPlayer byPlayer, BlockSelection blockSel) {
    if (__state.Handled == EnumHandling.Handled) {
      __result |= __state.Result;
    }
  }

  [HarmonyPrefix]
  [HarmonyPatch("OnContainedInteractStop")]
  public static bool OnContainedInteractStop(BlockMeal __instance,
                                             float secondsUsed,
                                             BlockEntityContainer be,
                                             ItemSlot slot, IPlayer byPlayer,
                                             BlockSelection blockSel) {
    return WalkCollectibleBehaviors(
        __instance, false,
        (CollectibleBehavior behavior, ref bool result,
         ref EnumHandling handled) => {
          if (behavior is IContainedInteractable interactable) {
            interactable.OnContainedInteractStop(secondsUsed, be, slot,
                                                 byPlayer, blockSel);
            handled |= EnumHandling.Handled;
          }
        },
        out BehaviorResult<bool> unused, out bool unused2);
  }

  [HarmonyPrefix]
  [HarmonyPatch("OnHeldInteractStart")]
  public static bool
  OnHeldInteractStart(BlockMeal __instance,
                      out BehaviorResult<EnumHandHandling> __state,
                      ItemSlot slot, EntityAgent byEntity,
                      BlockSelection blockSel, EntitySelection entitySel,
                      bool firstEvent, ref EnumHandHandling handHandling) {
    return WalkCollectibleBehaviors(
        __instance, EnumHandHandling.NotHandled,
        (CollectibleBehavior behavior, ref EnumHandHandling result,
         ref EnumHandling handled) => {
          behavior.OnHeldInteractStart(slot, byEntity, blockSel, entitySel,
                                       firstEvent, ref result, ref handled);
        },
        out __state, out handHandling);
  }

  [HarmonyPostfix]
  [HarmonyPatch("OnHeldInteractStart")]
  public static void OnHeldInteractStartPostfix(
      BlockMeal __instance, BehaviorResult<EnumHandHandling> __state,
      ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel,
      EntitySelection entitySel, bool firstEvent,
      ref EnumHandHandling handHandling) {
    if (__state.Handled == EnumHandling.Handled) {
      handHandling = __state.Result;
    }
  }

  [HarmonyPrefix]
  [HarmonyPatch("OnHeldInteractStep")]
  public static bool OnHeldInteractStep(BlockMeal __instance, ref bool __result,
                                        out BehaviorResult<bool> __state,
                                        float secondsUsed, ItemSlot slot,
                                        EntityAgent byEntity,
                                        BlockSelection blockSel,
                                        EntitySelection entitySel) {
    return WalkCollectibleBehaviors(
        __instance, true,
        (CollectibleBehavior behavior, ref bool result,
         ref EnumHandling handled) => {
          bool behaviorResult = behavior.OnHeldInteractStep(
              secondsUsed, slot, byEntity, blockSel, entitySel, ref handled);
          if (handled != EnumHandling.PassThrough) {
            result &= behaviorResult;
          }
        },
        out __state, out __result);
  }

  [HarmonyPostfix]
  [HarmonyPatch("OnHeldInteractStep")]
  public static void OnHeldInteractStepPostfix(
      BlockMeal __instance, ref bool __result, BehaviorResult<bool> __state,
      float secondsUsed, ItemSlot slot, EntityAgent byEntity,
      BlockSelection blockSel, EntitySelection entitySel) {
    if (__state.Handled == EnumHandling.Handled) {
      __result = __state.Result;
    }
  }

  [HarmonyPrefix]
  [HarmonyPatch("OnHeldInteractStop")]
  public static bool OnHeldInteractStop(BlockMeal __instance, float secondsUsed,
                                        ItemSlot slot, EntityAgent byEntity,
                                        BlockSelection blockSel,
                                        EntitySelection entitySel) {
    return WalkCollectibleBehaviors(
        __instance, true,
        (CollectibleBehavior behavior, ref bool result,
         ref EnumHandling handled) => {
          behavior.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel,
                                      entitySel, ref handled);
        },
        out BehaviorResult<bool> unused, out bool unused2);
  }

  /// <summary>
  /// Walk the behaviors on the block, and return whether the default action
  /// should be run.
  /// </summary>
  /// <param name="block">the block to traverse</param>
  /// <param name="initialResult">the value to initialize the result in the
  /// state</param> <param name="callBehavior">delegate to run on every behavior
  /// on the block (unless handled says to end the traversal early)</param>
  /// <param name="state">the final result and handling</param>
  /// <returns>true if the default action should be run.</returns>
  private static bool
  WalkBlockBehaviors<T>(Block block, T initialResult,
                        BlockBehaviorDelegate<T> callBehavior,
                        out BehaviorResult<T> state, out T result) {
    state = new(initialResult);
    foreach (BlockBehavior behavior in block.BlockBehaviors) {
      EnumHandling behaviorHandled = EnumHandling.PassThrough;
      callBehavior(behavior, ref state.Result, ref behaviorHandled);
      if (behaviorHandled != EnumHandling.PassThrough) {
        state.Handled = behaviorHandled;
      }
      if (behaviorHandled == EnumHandling.PreventSubsequent) {
        result = state.Result;
        return false;
      }
    }
    result = state.Result;
    return state.Handled != EnumHandling.PreventDefault;
  }

  /// <summary>
  /// Walk the behaviors on the collectible, and return whether the default
  /// action should be run.
  /// </summary>
  /// <param name="collectible">the block to traverse</param>
  /// <param name="initialResult">the value to initialize the result in the
  /// state</param> <param name="callBehavior">delegate to run on every behavior
  /// on the block (unless handled says to end the traversal early)</param>
  /// <param name="state">the final result and handling</param>
  /// <returns>true if the default action should be run.</returns>
  private static bool
  WalkCollectibleBehaviors<T>(CollectibleObject collectible, T initialResult,
                              CollectibleBehaviorDelegate<T> callBehavior,
                              out BehaviorResult<T> state, out T result) {
    state = new(initialResult);
    foreach (CollectibleBehavior behavior in collectible.CollectibleBehaviors) {
      EnumHandling behaviorHandled = EnumHandling.PassThrough;
      callBehavior(behavior, ref state.Result, ref behaviorHandled);
      if (behaviorHandled != EnumHandling.PassThrough) {
        state.Handled = behaviorHandled;
      }
      if (behaviorHandled == EnumHandling.PreventSubsequent) {
        result = state.Result;
        return false;
      }
    }
    result = state.Result;
    return state.Handled != EnumHandling.PreventDefault;
  }
}

#pragma warning restore IDE1006
