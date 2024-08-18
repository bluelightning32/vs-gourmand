using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Gourmand;

public class CodeCondition : ICollectibleCondition {
  [JsonProperty(Required = Required.Always)]
  readonly public AssetLocation Match;

  [JsonProperty]
  public readonly AssetLocation[] Output;

  public CodeCondition(AssetLocation match, AssetLocation[] output) {
    Match = match;
    Output = output ?? Array.Empty<AssetLocation>();
  }

  public void EnumerateMatches(MatchResolver resolver, EnumItemClass itemClass,
                               ref List<CollectibleObject> matches) {
    if (matches == null) {
      matches = resolver.GetMatchingCollectibles(Match, itemClass).ToList();
      return;
    }
    matches.RemoveAll((c) => !WildcardUtil.Match(Match, c.Code));
  }

  public IEnumerable<KeyValuePair<AssetLocation, IAttribute>>
  GetCategories(CollectibleObject match) {
    foreach (AssetLocation category in Output) {
      yield return new KeyValuePair<AssetLocation, IAttribute>(
          category, new StringAttribute(match.Code.ToString()));
    }
  }
}
