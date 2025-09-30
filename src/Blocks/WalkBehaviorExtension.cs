using Vintagestory.API.Common;

namespace Gourmand.Blocks;

public static class WalkBehaviorExtension {
  public delegate void BlockBehaviorDelegate<T>(BlockBehavior behavior,
                                                ref T result,
                                                ref EnumHandling handling);

  public delegate void
  CollectibleBehaviorDelegate<T>(CollectibleBehavior behavior, ref T result,
                                 ref EnumHandling handling);

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
  public static bool
  WalkBlockBehaviors<T>(this Block block, T initialResult,
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
  public static bool
  WalkCollectibleBehaviors<T>(this CollectibleObject collectible,
                              T initialResult,
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
