using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.ComponentModel;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand;

[JsonObject(MemberSerialization.OptIn)]
public class AttributeCondition : ICondition {
  [JsonProperty(Required = Required.Always)]
  readonly public string[] Path;

  [JsonProperty("value")]
  public readonly JToken RawValue;
  public readonly IAttribute Value;
  [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
  [DefaultValue(true)]
  public readonly bool AllowCast;
  [JsonProperty]
  public readonly AssetLocation[] Outputs;

  public IEnumerable<AssetLocation> Categories => Outputs;

  public AttributeCondition(string[] path, JToken value, bool allowCast,
                            AssetLocation[] output) {
    if (path.Length < 1) {
      throw new FormatException("Path must have at least one element.");
    }
    Path = path;
    RawValue = value;
    if (value != null) {
      Value = new JsonObject(value).ToAttribute();
    } else {
      Value = null;
    }
    AllowCast = allowCast;

    Outputs = output ?? Array.Empty<AssetLocation>();
  }

  private IAttribute GetAttribute(ItemStack stack) {
    IAttribute attributes = stack.Attributes;
    foreach (string name in Path) {
      attributes = (attributes as ITreeAttribute)?[name];
    }
    return attributes;
  }

  public bool IsMatch(Collectibles.IReadonlyCategoryDict catdict,
                      ItemStack stack) {
    IAttribute attribute = GetAttribute(stack);
    if (attribute == null) {
      return false;
    }
    if (Value == null) {
      return true;
    }
    object compareValue = Value.GetValue();
    object stackValue = attribute.GetValue();
    if (AllowCast && compareValue is IConvertible) {
      try {
        compareValue = Convert.ChangeType(compareValue, stackValue.GetType());
      } catch (InvalidCastException) {
        // After the cast failed, perform the comparison with the original value
        // anyway.
      }
    }
    return compareValue.Equals(stackValue);
  }

  public List<IAttribute> GetValue(Collectibles.IReadonlyCategoryDict catdict,
                                   AssetLocation category, ItemStack stack) {
    // It is the caller's responsibility to ensure the stack is a match, and the
    // category is one of the output categories. So with that assumed, the value
    // can be directly returned.
    return new List<IAttribute>() { GetAttribute(stack) };
  }

  /// <summary>
  /// Set or add the attribute on all existing matches. Only existing item
  /// stacks in matches are modified. No new item stacks are added.
  /// </summary>
  /// <param name="catdict">category dictionary</param>
  /// <param name="matches">input and output list of matched item stacks</param>
  public void EnumerateMatches(Collectibles.IReadonlyCategoryDict catdict,
                               ref List<ItemStack> matches) {
    matches ??= new();
    foreach (ItemStack stack in matches) {
      ITreeAttribute attributes = stack.Attributes;
      for (int i = 0; i < Path.Length - 1; ++i) {
        attributes = attributes.GetOrAddTreeAttribute(Path[i]);
      }
      attributes[Path[^1]] = Value.Clone();
    }
  }
}
