using System.Collections.Generic;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand.Collectibles;

public interface ICondition {
  /// <summary>
  /// Gets the categories of a match. The result is undefined if the given
  /// object is not a match.
  /// </summary>
  /// <param name="match">the collectible to check</param>
  /// <returns>an enumerable of all the categories and their values</returns>
  public IEnumerable<KeyValuePair<AssetLocation, IAttribute[]>>
  GetCategories(IWorldAccessor resolver, IReadonlyCategoryDict catdict,
                CollectibleObject match);

  /// <summary>
  /// The categories outputed by this condition. The enumerable may contain
  /// duplicates.
  /// </summary>
  public IEnumerable<AssetLocation> Categories { get; }

  /// <summary>
  /// List collectibles that match this condition
  /// </summary>
  /// <param name="matches">An existing list of collectibles to further refine
  /// based on this condition. Pass null for this condition to start a new
  /// list.</param>
  public void EnumerateMatches(MatchResolver resolver,
                               ref List<CollectibleObject> matches);
}
