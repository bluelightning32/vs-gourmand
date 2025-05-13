using System.Collections.Generic;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand;

public interface ICondition {
  bool Validate(IWorldAccessor resolver, ILogger logger,
                Collectibles.IReadonlyCategoryDict catdict);

  /// <summary>
  /// Determine whether the given ItemStack matches the condition
  /// </summary>
  /// <param name="catdict">a precomputed dictionary of categories for
  /// collectible objects</param>
  /// <param name="stack">the ItemStack to
  /// check</param>
  /// <returns>true, if it is a match</returns>
  bool IsMatch(IWorldAccessor resolver,
               Collectibles.IReadonlyCategoryDict catdict, ItemStack stack);

  /// <summary>
  /// Returns a string explaining why the stack does not match the condition, or
  /// null if the stack does match.
  /// </summary>
  /// <param name="resolver"></param>
  /// <param name="catdict"></param>
  /// <param name="stack"></param>
  /// <returns></returns>
  string ExplainMismatch(IWorldAccessor resolver,
                         Collectibles.IReadonlyCategoryDict catdict,
                         ItemStack stack);

  /// <summary>
  /// Appends the value of a category of a match to an existing list. The result
  /// is undefined if the given stack is not a match, or if this condition does
  /// not output the given category.
  /// </summary>
  /// <param name="resolver">resolver</param>
  /// <param name="catdict">
  /// a precomputed dictionary of categories for collectible objects</param>
  /// <param name="category">
  /// the category to look up</param>
  /// <param name="stack">the stack to look up</param>
  /// <param name="result">the list to append the result to</param>
  public void AppendValue(IWorldAccessor resolver,
                          Collectibles.IReadonlyCategoryDict catdict,
                          AssetLocation category, ItemStack stack,
                          List<IAttribute> result);

  /// <summary>
  /// All categories outputted by this condition for matches. This may contain
  /// duplicates.
  /// </summary>
  public IEnumerable<AssetLocation> Categories { get; }

  /// <summary>
  /// List collectibles that match this condition
  /// </summary>
  /// <param name="catdict">a precomputed dictionary of categories for
  /// collectible objects</param>
  /// <param name="input">
  ///   An existing enumerable of ItemStacks to further refine
  ///   based on this condition. Pass null for this condition to enumerate all
  ///   of its possible values instead of refining an existing enumerable.
  /// </param>
  /// <returns>
  /// An enumerable of matches
  /// </returns>
  public IEnumerable<ItemStack>
  EnumerateMatches(IWorldAccessor resolver,
                   Collectibles.IReadonlyCategoryDict catdict,
                   IEnumerable<ItemStack> input);
}
