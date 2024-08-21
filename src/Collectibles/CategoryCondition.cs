using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand.Collectibles;

public class CategoryCondition : ICondition {
  [JsonProperty(Required = Required.Always)]
  readonly public AssetLocation Input;
  [JsonProperty]
  public readonly AssetLocation[] Outputs;

  public IEnumerable<AssetLocation> Categories => Outputs;

  public CategoryCondition(AssetLocation input, AssetLocation[] output) {
    Input = input;
    Outputs = output ?? Array.Empty<AssetLocation>();
  }

  public IEnumerable<KeyValuePair<AssetLocation, IAttribute[]>>
  GetCategories(IReadonlyCategoryDict catdict, CollectibleObject match) {
    CategoryValue value = catdict.GetValue(Input, match);
    foreach (AssetLocation category in Outputs) {
      yield return new KeyValuePair<AssetLocation, IAttribute[]>(
          category, value.Value.ToArray());
    }
  }

  public void EnumerateMatches(MatchResolver resolver,
                               ref List<CollectibleObject> matches) {
    if (matches == null) {
      matches = resolver.CatDict.EnumerateMatches(Input).ToList();
    } else {
      matches.RemoveAll((c) => !IsMatch(resolver.CatDict, c));
    }
  }

  private bool IsMatch(IReadonlyCategoryDict catdict, CollectibleObject c) {
    return catdict.GetValue(Input, c)?.Value != null;
  }
}
