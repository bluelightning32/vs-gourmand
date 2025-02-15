using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Gourmand.Collectibles;

public class CodeCondition : ICondition {
  [JsonProperty(Required = Required.Always)]
  readonly public AssetLocation Match;

  [JsonProperty(Required = Required.Always)]
  readonly public EnumItemClass Type;

  [JsonProperty]
  public readonly AssetLocation[] Outputs;

  public CodeCondition(AssetLocation match, EnumItemClass type,
                       AssetLocation[] outputs) {
    Match = match;
    Type = type;
    Outputs = outputs ?? Array.Empty<AssetLocation>();
  }

  [JsonIgnore]
  public IEnumerable<AssetLocation> Categories => Outputs;

  public void EnumerateMatches(MatchResolver resolver,
                               ref List<CollectibleObject> matches) {
    if (matches == null) {
      matches = resolver.GetMatchingCollectibles(Match, Type).ToList();
      return;
    }
    matches.RemoveAll((c) => c.ItemClass != Type ||
                             !WildcardUtil.Match(Match, c.Code));
  }

  public IEnumerable<KeyValuePair<AssetLocation, IAttribute[]>>
  GetCategories(IWorldAccessor resolver, IReadonlyCategoryDict catdict,
                CollectibleObject match) {
    foreach (AssetLocation category in Outputs) {
      yield return new KeyValuePair<AssetLocation, IAttribute[]>(
          category,
          new IAttribute[1] { new StringAttribute(match.Code.ToString()) });
    }
  }
}
