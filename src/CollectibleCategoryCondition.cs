using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand;

public class CollectibleCategoryCondition : ICollectibleCondition {
  [JsonProperty(Required = Required.Always)]
  readonly public AssetLocation Input;
  [JsonProperty]
  public readonly AssetLocation[] Outputs;

  public IEnumerable<AssetLocation> Categories => Outputs;

  public CollectibleCategoryCondition(AssetLocation input,
                                      AssetLocation[] output) {
    Input = input;
    Outputs = output ?? Array.Empty<AssetLocation>();
  }

  public IEnumerable<KeyValuePair<AssetLocation, IAttribute[]>>
  GetCategories(IReadonlyCategoryDict catdict, CollectibleObject match) {
    CategoryValue value = catdict.GetValue(match, Input);
    foreach (AssetLocation category in Outputs) {
      yield return new KeyValuePair<AssetLocation, IAttribute[]>(
          category, value.Value.ToArray());
    }
  }

  public void EnumerateMatches(MatchResolver resolver, EnumItemClass itemClass,
                               ref List<CollectibleObject> matches) {
    matches ??= itemClass switch {
      EnumItemClass.Block =>
          resolver.Resolver.Blocks.ToList<CollectibleObject>(),
      EnumItemClass.Item => resolver.Resolver.Items.ToList<CollectibleObject>(),
      _ => throw new ArgumentException("Invalid enum value", nameof(itemClass)),
    };
    matches.RemoveAll((c) => !IsMatch(resolver.CatDict, c));
  }

  private bool IsMatch(IReadonlyCategoryDict catdict, CollectibleObject c) {
    return catdict.GetValue(c, Input)?.Value != null;
  }
}
