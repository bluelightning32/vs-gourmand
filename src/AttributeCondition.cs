using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

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

  [JsonProperty("enumerateValues")]
  public readonly JToken[] RawEnumerateValues;
  public readonly IAttribute[] EnumerateValues;

  [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
  [DefaultValue(true)]
  public readonly bool AllowCast;
  [JsonProperty]
  public readonly AssetLocation[] Outputs;

  public IEnumerable<AssetLocation> Categories => Outputs;

  public AttributeCondition(string[] path, JToken value,
                            JToken[] enumerateValues, bool allowCast,
                            AssetLocation[] output) {
    if (path.Length < 1) {
      throw new FormatException("Path must have at least one element.");
    }
    Path = path;
    RawValue = value;
    if ((value != null) && (enumerateValues != null)) {
      throw new ArgumentException(
          "Only one of 'value' or 'enumerateValues' may be present.");
    }

    if (value != null) {
      Value = new JsonObject(value).ToAttribute();
      EnumerateValues = new IAttribute[] { Value };
    } else {
      Value = null;
      if (enumerateValues != null) {
        EnumerateValues =
            enumerateValues.Select(v => new JsonObject(v).ToAttribute())
                .ToArray();
      } else {
        EnumerateValues = Array.Empty<IAttribute>();
      }
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

  public bool IsMatch(IWorldAccessor resolver,
                      Collectibles.IReadonlyCategoryDict catdict,
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

  public void AppendValue(IWorldAccessor resolver,
                          Collectibles.IReadonlyCategoryDict catdict,
                          AssetLocation category, ItemStack stack,
                          List<IAttribute> result) {
    // It is the caller's responsibility to ensure the stack is a match, and the
    // category is one of the output categories. So with that assumed, the value
    // can be directly returned.
    result.Add(GetAttribute(stack));
  }

  /// <summary>
  /// Set or add the attribute on all existing matches. Only existing item
  /// stacks in matches are modified. Any added item stacks are only copies of
  /// existing stacks with a different attribute value set.
  /// </summary>
  /// <param name="catdict">category dictionary</param>
  /// <param name="matches">input and output list of matched item stacks</param>
  public void EnumerateMatches(IWorldAccessor resolver,
                               Collectibles.IReadonlyCategoryDict catdict,
                               ref List<ItemStack> matches) {
    matches ??= new();
    if (EnumerateValues.Length == 0) {
      matches.Clear();
      return;
    }
    int matchesCount = matches.Count;
    for (int matchIndex = 0; matchIndex < matchesCount; ++matchIndex) {
      ItemStack stack = matches[matchIndex];
      SetAttribute(stack.Attributes, EnumerateValues[0]);
      for (int valueIndex = 1; valueIndex < EnumerateValues.Length;
           ++valueIndex) {
        ItemStack newStack = stack.Clone();
        SetAttribute(stack.Attributes, EnumerateValues[valueIndex]);
        matches.Add(newStack);
      }
    }
  }

  private void SetAttribute(ITreeAttribute attributes, IAttribute value) {
    for (int i = 0; i < Path.Length - 1; ++i) {
      attributes = attributes.GetOrAddTreeAttribute(Path[i]);
    }
    attributes[Path[^1]] = value.Clone();
  }
}
