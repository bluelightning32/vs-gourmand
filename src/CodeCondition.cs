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
  public readonly AssetLocation[] Outputs;

  public CodeCondition(AssetLocation match, AssetLocation[] output) {
    Match = match;
    Outputs = output ?? Array.Empty<AssetLocation>();
  }

  public IEnumerable<AssetLocation> Categories => Outputs;

  public void EnumerateMatches(MatchResolver resolver, EnumItemClass itemClass,
                               ref List<CollectibleObject> matches) {
    if (matches == null) {
      matches = resolver.GetMatchingCollectibles(Match, itemClass).ToList();
      return;
    }
    matches.RemoveAll((c) => !WildcardUtil.Match(Match, c.Code));
  }

  public IEnumerable<KeyValuePair<AssetLocation, IAttribute[]>>
  GetCategories(IReadonlyCategoryDict catdict, CollectibleObject match) {
    foreach (AssetLocation category in Outputs) {
      yield return new KeyValuePair<AssetLocation, IAttribute[]>(
          category,
          new IAttribute[1] { new StringAttribute(match.Code.ToString()) });
    }
  }
}
