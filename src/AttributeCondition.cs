using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand;

public class AttributeCondition : ICollectibleCondition {
  [JsonProperty(Required = Required.Always)]
  readonly public string[] Path;
  [JsonProperty]
  public readonly JToken Value;
  [JsonProperty]
  public readonly AssetLocation[] Outputs;

  public IEnumerable<AssetLocation> Categories => Outputs;

  public AttributeCondition(string[] path, JToken value,
                            AssetLocation[] output) {
    Path = path;
    Value = value;
    Outputs = output ?? Array.Empty<AssetLocation>();
  }

  public IEnumerable<KeyValuePair<AssetLocation, IAttribute[]>>
  GetCategories(IReadonlyCategoryDict catdict, CollectibleObject match) {
    JsonObject found = match.Attributes;
    foreach (string name in Path) {
      found = found[name];
    }
    foreach (AssetLocation category in Outputs) {
      yield return new KeyValuePair<AssetLocation, IAttribute[]>(
          category, new IAttribute[1] { found.ToAttribute() });
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
    matches.RemoveAll((c) => !IsMatch(c.Attributes));
  }

  private bool IsMatch(JsonObject attributes) {
    foreach (string name in Path) {
      attributes = attributes[name];
    }
    if (Value != null) {
      return JToken.DeepEquals(attributes.Token, Value);
    } else {
      return attributes.Exists;
    }
  }
}
