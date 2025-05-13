using Gourmand.Collectibles;

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
                            AssetLocation[] outputs) {
    if (path.Length < 1) {
      throw new FormatException("Path must have at least one element.");
    }
    Path = path;
    RawValue = value;
    if (value != null && value.Type != JTokenType.Null &&
        enumerateValues != null) {
      throw new ArgumentException(
          "Only one of 'value' or 'enumerateValues' may be present.");
    }

    if (value != null && value.Type != JTokenType.Null) {
      Value = new JsonObject(value).ToAttribute();
      EnumerateValues = new IAttribute[] { Value };
      RawEnumerateValues = null;
    } else {
      Value = null;
      if (enumerateValues != null) {
        EnumerateValues =
            enumerateValues.Select(v => new JsonObject(v).ToAttribute())
                .ToArray();
        RawEnumerateValues = enumerateValues;
      } else {
        EnumerateValues = Array.Empty<IAttribute>();
      }
    }
    AllowCast = allowCast;

    Outputs = outputs ?? Array.Empty<AssetLocation>();
  }

  private IAttribute GetAttribute(ItemStack stack) {
    IAttribute attributes = stack.Attributes;
    foreach (string name in Path) {
      attributes = (attributes as ITreeAttribute)?[name];
    }
    return attributes;
  }

  public bool IsMatch(IWorldAccessor resolver, IReadonlyCategoryDict catdict,
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
                          IReadonlyCategoryDict catdict, AssetLocation category,
                          ItemStack stack, List<IAttribute> result) {
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
  /// <param name="input">input enumerable of matched item stacks</param>
  public IEnumerable<ItemStack> EnumerateMatches(IWorldAccessor resolver,
                                                 IReadonlyCategoryDict catdict,
                                                 IEnumerable<ItemStack> input) {
    if (EnumerateValues.Length == 0) {
      yield break;
    }
    foreach (ItemStack stack in input ?? Array.Empty<ItemStack>()) {
      SetAttribute(stack.Attributes, EnumerateValues[0]);
      yield return stack;
      for (int valueIndex = 1; valueIndex < EnumerateValues.Length;
           ++valueIndex) {
        ItemStack newStack = stack.Clone();
        SetAttribute(newStack.Attributes, EnumerateValues[valueIndex]);
        yield return newStack;
      }
    }
  }

  private void SetAttribute(ITreeAttribute attributes, IAttribute value) {
    for (int i = 0; i < Path.Length - 1; ++i) {
      attributes = attributes.GetOrAddTreeAttribute(Path[i]);
    }
    attributes[Path[^1]] = value.Clone();
  }

  public bool Validate(IWorldAccessor resolver, ILogger logger,
                       IReadonlyCategoryDict catdict) {
    return true;
  }

  public string ExplainMismatch(IWorldAccessor resolver,
                                IReadonlyCategoryDict catdict,
                                ItemStack stack) {
    IAttribute attribute = stack.Attributes;
    for (int i = 0; i < Path.Length; ++i) {
      string name = Path[i];
      attribute = (attribute as ITreeAttribute)?[name];
      if (attribute == null) {
        return "missing attribute: " + string.Join(",", Path.Take(i + 1));
      }
    }
    if (attribute == null) {
      return "item has no attributes";
    }
    if (Value == null) {
      return null;
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
    if (!compareValue.Equals(stackValue)) {
      return $"collectible value {stackValue} does not equal condition value {compareValue}";
    }
    return null;
  }
}
