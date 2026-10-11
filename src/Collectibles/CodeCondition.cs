using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.ComponentModel;
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

  [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
  [DefaultValue(0)]
  readonly public int TrimStartParts;

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
    matches.RemoveAll((c) => !IsMatch(c));
  }

  public bool IsMatch(CollectibleObject c) {
    if (c.ItemClass != Type)
      return false;
    if (!WildcardUtil.Match(Match, c.Code))
      return false;
    int pos = 0;
    for (int i = 0; i < TrimStartParts; ++i) {
      pos = c.Code.Path.IndexOf('-', pos);
      if (pos == -1)
        return false;
      ++pos;
    }
    return true;
  }

  public IEnumerable<KeyValuePair<AssetLocation, IAttribute[]>>
  GetCategories(IWorldAccessor resolver, IReadonlyCategoryDict catdict,
                CollectibleObject match) {
    foreach (AssetLocation category in Outputs) {
      int pos = 0;
      for (int i = 0; i < TrimStartParts; ++i) {
        pos = match.Code.Path.IndexOf('-', pos);
        ++pos;
      }
      string matchString;
      if (pos > 0) {
        matchString = match.Code.Domain + ":" + match.Code.Path[pos..];
      } else {
        matchString = match.Code.ToString();
      }
      yield return new KeyValuePair<AssetLocation, IAttribute[]>(
          category, [new StringAttribute(matchString)]);
    }
  }
}
