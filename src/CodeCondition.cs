using Newtonsoft.Json;

using System.Collections.Generic;
using System.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Gourmand;

public class CodeCondition : CollectibleCondition {
  [JsonProperty(Required = Required.Always)]
  readonly public AssetLocation Match;

  public CodeCondition(AssetLocation match, List<AssetLocation> output)
      : base(output) {
    Match = match;
  }

  public override void EnumerateMatches(MatchResolver resolver,
                                        EnumItemClass itemClass,
                                        ref List<CollectibleObject> matches) {
    if (matches == null) {
      matches = resolver.GetMatchingCollectibles(Match, itemClass).ToList();
      return;
    }
    matches.RemoveAll((c) => !WildcardUtil.Match(Match, c.Code));
  }

  public override IAttribute GetMatchValue(CollectibleObject match) {
    return new StringAttribute(match.Code.ToString());
  }
}
