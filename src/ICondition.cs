using System.Collections.Generic;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand;

public interface ICondition {
  /// <summary>
  /// Determine whether the given ItemStack matches the condition
  /// </summary>
  /// <param name="catdict">a precomputed dictionary of categories for
  /// collectible objects</param> <param name="stack">the ItemStack to
  /// check</param> <returns>true, if it is a match</returns>
  bool IsMatch(Collectibles.IReadonlyCategoryDict catdict, ItemStack stack);

  /// <summary>
  /// Gets the value of a category of a match. The result is undefined if the
  /// given stack is not a match, or if this condition does not output the given
  /// category.
  /// </summary>
  /// <param name="catdict">a precomputed dictionary of categories for
  /// collectible objects</param> <param name="category">the category to look
  /// up</param> <param name="stack">the stack to look up</param> <returns>the
  /// category value's attribute array</returns>
  public List<IAttribute> GetValue(Collectibles.IReadonlyCategoryDict catdict,
                                   AssetLocation category, ItemStack stack);

  /// <summary>
  /// All categories outputted by this condition for matches.
  /// </summary>
  public IEnumerable<AssetLocation> Categories { get; }

  /// <summary>
  /// List collectibles that match this condition
  /// </summary>
  /// <param name="catdict">a precomputed dictionary of categories for
  /// collectible objects</param> <param name="matches">
  ///   An existing list of ItemStacks to further refine
  ///   based on this condition. Pass null for this condition to start a new
  ///   list.
  /// </param>
  public void EnumerateMatches(Collectibles.IReadonlyCategoryDict catdict,
                               ref List<ItemStack> matches);
}
