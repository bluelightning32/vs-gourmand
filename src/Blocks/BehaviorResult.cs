using Vintagestory.API.Common;

namespace Gourmand.Blocks;

public struct BehaviorResult<T> {
  public T Result;
  public EnumHandling Handled = EnumHandling.PassThrough;

  public BehaviorResult(T defaultResult) { Result = defaultResult; }
}
