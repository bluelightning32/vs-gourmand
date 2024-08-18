using Newtonsoft.Json;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand;

public abstract class CollectibleCondition {
  [JsonProperty]
  public readonly List<AssetLocation> Output;

  public CollectibleCondition(List<AssetLocation> output) { Output = output; }

  /// <summary>
  /// Gets the category value of a match. The result is undefined if the given
  /// object is not a match.
  /// </summary>
  /// <param name="match">the collectible to check</param>
  /// <returns>the category value</returns>
  public abstract IAttribute GetMatchValue(CollectibleObject match);

  /// <summary>
  /// List collectibles that match this condition
  /// </summary>
  /// <param name="existing">An existing list of collectibles to further refine
  /// based on this condition. Pass null for this condition to start a new
  /// list.</param>
  public abstract void EnumerateMatches(MatchResolver resolver,
                                        EnumItemClass itemClass,
                                        ref List<CollectibleObject> matches);
}
