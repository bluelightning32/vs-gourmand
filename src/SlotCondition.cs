using Newtonsoft.Json;

using Gourmand.Collectibles;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using System.Diagnostics;
using Vintagestory.GameContent;

namespace Gourmand;

public class ContentCategory : CategoryCondition {
  [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
  [DefaultValue(0)]
  public readonly int DistinctMin;
  [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
  [DefaultValue(int.MaxValue)]
  public readonly int DistinctMax;
  [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
  [DefaultValue(int.MaxValue)]
  public readonly int EnumerateDistinctMax;
  [JsonProperty]
  public readonly AssetLocation[] DistinctOutputs;

  public ContentCategory(AssetLocation input, AssetLocation[] outputs,
                         int distinctMin, int distinctMax,
                         int enumerateDistinctMax,
                         AssetLocation[] distinctOutputs)
      : base(input, outputs) {
    DistinctMin = distinctMin;
    DistinctMax = distinctMax;
    DistinctOutputs = distinctOutputs ?? Array.Empty<AssetLocation>();
    EnumerateDistinctMax = int.Min(enumerateDistinctMax, distinctMax);
  }

  /// <summary>
  /// Get the category value of a stack. The caller must ensure that the stack
  /// is a match.
  /// </summary>
  /// <param name="resolver"></param>
  /// <param name="catdict"></param>
  /// <param name="stack"></param>
  /// <param name="distinctValues"></param>
  /// <returns>the category value of the stack</returns>
  public List<IAttribute> GetValue(IWorldAccessor resolver,
                                   IReadonlyCategoryDict catdict,
                                   ItemStack stack) {
    return catdict.GetValue(Input, stack.Collectible).Value;
  }

  public bool IsDistinctOk(int distinctValues) {
    return DistinctMin <= distinctValues && distinctValues <= DistinctMax;
  }

  public bool IsDistinctMinOk(int distinctValues) {
    return DistinctMin <= distinctValues;
  }

  public bool IsDistinctMaxOk(int distinctValues) {
    return distinctValues <= DistinctMax;
  }

  public bool IsEnumerateDistinctMaxOk(int distinctValues) {
    return distinctValues <= EnumerateDistinctMax;
  }
}

public enum Arrangement {
  Any,
  SortedWithRepeats,
  Sorted,
  Repeated,
}

[JsonObject(MemberSerialization.OptIn)]
public class SlotCondition {
  [JsonProperty("code")]
  readonly public string Code;
  [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
  [DefaultValue(0)]
  readonly public int SlotBegin;
  [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
  [DefaultValue(int.MaxValue)]
  readonly public int SlotEnd;

  [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
  [DefaultValue(1)]
  public int Min;
  [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
  [DefaultValue(int.MaxValue)]
  public int Max;
  [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
  [DefaultValue(Arrangement.Sorted)]
  public Arrangement EnumArrangement;

  /// <summary>
  /// Enumerate up to this many matches, for each of the input enumerations.
  /// </summary>
  [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
  [DefaultValue(5)]
  public int EnumerateMax;

  [JsonProperty]
  public readonly ContentCategory[] Categories;

  [JsonProperty]
  public readonly AssetLocation[] CountOutputs;

  [JsonConstructor]
  public SlotCondition(int slotBegin, int slotEnd, ContentCategory[] categories,
                       AssetLocation[] countOutputs) {
    SlotBegin = slotBegin;
    SlotEnd = slotEnd < 0 ? int.MaxValue : slotEnd;
    if (SlotBegin < 0) {
      throw new ArgumentException("slotBegin must be non-negative.");
    }
    Categories = categories ?? Array.Empty<ContentCategory>();
    CountOutputs = countOutputs ?? Array.Empty<AssetLocation>();
  }

  public SlotCondition(string recipe, CookingRecipeIngredient ingred) {
    Code = ingred.Code;
    SlotBegin = 0;
    SlotEnd = 4;
    Min = ingred.MinQuantity;
    Max = ingred.MaxQuantity;
    Categories = new ContentCategory[] { new(
        Collectibles.CategoryDict.ImplictIngredientCategory(recipe,
                                                            ingred.Code),
        null, 0, int.MaxValue, int.MaxValue, null) };
    Categories[0].EnumeratePerDistinct = int.MaxValue;
    EnumArrangement = Arrangement.Repeated;
    EnumerateMax = 5;
    CountOutputs = Array.Empty<AssetLocation>();
  }

  private bool IsContentMatch(IWorldAccessor resolver,
                              IReadonlyCategoryDict catdict, ItemStack stack) {
    return Categories.All(cond => cond.IsMatch(resolver, catdict, stack));
  }

  private bool IsDistinctOk(HashSet<List<IAttribute>>[] distinct) {
    for (int distIndex = 0; distIndex < Categories.Length; ++distIndex) {
      if (!Categories[distIndex].IsDistinctOk(distinct[distIndex].Count)) {
        return false;
      }
    }
    return true;
  }

  private bool IsDistinctMinOk(Dictionary<List<IAttribute>, int>[] distinct) {
    for (int distIndex = 0; distIndex < Categories.Length; ++distIndex) {
      if (!Categories[distIndex].IsDistinctMinOk(distinct[distIndex].Count)) {
        return false;
      }
    }
    return true;
  }

  private bool IsDistinctMaxOk(Dictionary<List<IAttribute>, int>[] distinct) {
    for (int distIndex = 0; distIndex < Categories.Length; ++distIndex) {
      if (!Categories[distIndex].IsDistinctMaxOk(distinct[distIndex].Count)) {
        return false;
      }
    }
    return true;
  }

  private bool
  IsEnumerateDistinctMaxOk(Dictionary<List<IAttribute>, int>[] distinct) {
    for (int distIndex = 0; distIndex < Categories.Length; ++distIndex) {
      if (!Categories[distIndex].IsDistinctMaxOk(distinct[distIndex].Count)) {
        return false;
      }
    }
    return true;
  }

  /// <summary>
  /// Get the content slots that match the subconditions, assuming that the
  /// contents come from a matched item stack.
  /// </summary>
  /// <param name="resolver">resolver</param>
  /// <param name="catdict">catdict</param>
  /// <param name="contents">contents of a match</param>
  /// <returns>the indices of the slots that match all the
  /// subconditions</returns>
  private List<int> GetMatchingSlots(IWorldAccessor resolver,
                                     IReadonlyCategoryDict catdict,
                                     IList<ItemStack> contents,
                                     out HashSet<List<IAttribute>>[] distinct) {
    distinct = new HashSet<List<IAttribute>>[Categories.Length];
    ListIAttributeComparer comparer = new();
    for (int distIndex = 0; distIndex < Categories.Length; ++distIndex) {
      distinct[distIndex] = new(comparer);
    }
    List<int> matches = new();
    int slotEnd = int.Min(SlotEnd, contents.Count);
    for (int i = SlotBegin; i < slotEnd; ++i) {
      ItemStack stack = contents[i];
      if (stack == null) {
        continue;
      }
      if (IsContentMatch(resolver, catdict, stack)) {
        for (int distIndex = 0; distIndex < Categories.Length; ++distIndex) {
          distinct[distIndex].Add(
              Categories[distIndex].GetValue(resolver, catdict, stack));
        }
        matches.Add(i);
        if (matches.Count == Max) {
          break;
        }
      }
    }
    if (matches.Count < Min) {
      return null;
    }
    if (!IsDistinctOk(distinct)) {
      return null;
    }
    return matches;
  }

  public bool IsMatch(IWorldAccessor resolver, IReadonlyCategoryDict catdict,
                      ItemStack[] contents) {
    List<int> matches =
        GetMatchingSlots(resolver, catdict, contents, out var _);
    if (matches == null) {
      return false;
    }
    foreach (int i in matches) {
      contents[i] = null;
    }
    return true;
  }

  public void AppendValue(IWorldAccessor resolver,
                          IReadonlyCategoryDict catdict, AssetLocation category,
                          ItemStack[] contents, List<IAttribute> result) {
    List<int> matches = GetMatchingSlots(
        resolver, catdict, contents, out HashSet<List<IAttribute>>[] distinct);
    for (int i = 0; i < Categories.Length; ++i) {
      ContentCategory cond = Categories[i];
      if (cond.Outputs.Contains(category)) {
        foreach (int slot in matches) {
          cond.AppendValue(resolver, catdict, category, contents[slot], result);
        }
      }
      if (cond.DistinctOutputs.Contains(category)) {
        List<List<IAttribute>> lists = distinct[i].ToList();
        lists.Sort(new ListIAttributeComparer());
        result.AddRange(lists.SelectMany(l => l));
      }
    }
    if (CountOutputs.Contains(category)) {
      result.Add(new IntAttribute(matches.Count));
    }
    foreach (int i in matches) {
      contents[i] = null;
    }
  }

  /// <summary>
  /// Get the stacks that <see cref="EnumerateMatchContents"/> will try to
  /// return.
  /// </summary>
  /// <returns>the stacks</returns>
  public IReadOnlyList<ItemStack>
  EnumerateAllowedStacks(IWorldAccessor resolver,
                         IReadonlyCategoryDict catdict) {
    List<ItemStack> contentMatchesList = null;
    foreach (ICondition cond in Categories) {
      cond.EnumerateMatches(resolver, catdict, ref contentMatchesList);
    }
    return (IReadOnlyList<ItemStack>)contentMatchesList ??
           new AllStacksList(resolver);
  }

  public IEnumerable<ValueTuple>
  EnumerateMatchContents(IWorldAccessor resolver, IReadonlyCategoryDict catdict,
                         ContentBuilder builder,
                         IEnumerable<ValueTuple> input) {
    if (EnumerateMax == 0) {
      yield break;
    }
    // This holds the ItemStacks that can possibly be arranged in builder to
    // make a match for the ContentsCondition.
    IReadOnlyList<ItemStack> contentMatches =
        EnumerateAllowedStacks(resolver, catdict);
    // Each element in the stack contains an index in contentMatches to try the
    // next time it is at the top of the stack.
    Stack<int> contentIndices = new();
    Dictionary<List<IAttribute>, int>[] distinct =
        new Dictionary<List<IAttribute>, int>[Categories.Length];
    ListIAttributeComparer comparer = new();
    for (int i = 0; i < Categories.Length; ++i) {
      distinct[i] = new(comparer);
    }
    // Builder is modified by the input enumerator on each iteration. The input
    // enumerator does not output any data other than what it stores in builder,
    // which is why the value directly from the input enumerator is unused.
    foreach (ValueTuple unused in input) {
      int enumerated = 0;
      if (Min <= 0 && IsDistinctMinOk(distinct) && IsDistinctMaxOk(distinct)) {
        yield return new();
        ++enumerated;
      }
      if (contentMatches.Count == 0) {
        continue;
      }
      // The number of entries added to builder by this function. This is
      // decremented when entries are removed from builder. This is one less
      // than contentMatches.Count when the next item has not been pushed onto
      // the stack yet. Otherwise it is equal to contentMatches.count.
      int added = 0;
      contentIndices.Push(0);
      while (contentIndices.Count > 0) {
        if (added == contentIndices.Count) {
          ItemStack removed = builder.PopValue();
          DecrementDistinct(resolver, catdict, distinct, removed);
          --added;
        }
        int index = contentIndices.Peek();
        if (index >= contentMatches.Count ||
            (EnumArrangement == Arrangement.Repeated &&
             contentIndices.Count > 1 &&
             index >= contentIndices.ElementAt(1))) {
          contentIndices.Pop();
          Debug.Assert(added == contentIndices.Count);
          continue;
        }
        if (!builder.PushValue(contentMatches[index], SlotBegin, SlotEnd)) {
          // The slots are full. End this iteration and try again with fewer
          // ItemStacks.
          contentIndices.Pop();
          Debug.Assert(added == contentIndices.Count);
          continue;
        }
        IncrementDistinct(resolver, catdict, distinct, contentMatches[index]);
        ++added;
        Debug.Assert(added == contentIndices.Count);
        bool distinctMinOk = IsDistinctMinOk(distinct);
        bool distinctMaxOk = IsEnumerateDistinctMaxOk(distinct);
        if (Min <= added && distinctMinOk && distinctMaxOk) {
          yield return new();
          if (++enumerated >= EnumerateMax) {
            for (int i = 0; i < added; ++i) {
              builder.PopValue();
            }
            foreach (var d in distinct) {
              d.Clear();
            }
            contentIndices.Clear();
            added = 0;
            break;
          }
        }

        contentIndices.Pop();
        // Use the next index the next time this is at the top of the stack.
        contentIndices.Push(index + 1);
        // Only add more ItemStacks to builder when distinctMaxOk is true. When
        // it is false there are too many distinct ItemStacks in builder. Adding
        // more ItemStacks does not cause it to go from false to true.
        if (distinctMaxOk && added < Max) {
          // On the next iteration, try to put another element in the builder.
          int nextIndex = EnumArrangement switch {
            Arrangement.Any => 0, Arrangement.SortedWithRepeats => index,
            Arrangement.Sorted => index + 1, Arrangement.Repeated => index,
            _ => throw new NotImplementedException()
          };
          contentIndices.Push(nextIndex);
        }
      }
      Debug.Assert(contentIndices.Count == 0);
      Debug.Assert(added == 0);
      Debug.Assert(distinct.All(d => d.Count == 0));
    }
  }

  private void IncrementDistinct(IWorldAccessor resolver,
                                 IReadonlyCategoryDict catdict,
                                 Dictionary<List<IAttribute>, int>[] distinct,
                                 ItemStack added) {
    for (int distIndex = 0; distIndex < Categories.Length; ++distIndex) {
      List<IAttribute> catValue =
          Categories[distIndex].GetValue(resolver, catdict, added);
      if (!distinct[distIndex].TryGetValue(catValue, out int oldCount)) {
        distinct[distIndex].Add(catValue, 1);
      } else {
        Debug.Assert(oldCount > 0);
        distinct[distIndex][catValue] = oldCount + 1;
      }
    }
  }

  private void DecrementDistinct(IWorldAccessor resolver,
                                 IReadonlyCategoryDict catdict,
                                 Dictionary<List<IAttribute>, int>[] distinct,
                                 ItemStack removed) {
    for (int distIndex = 0; distIndex < Categories.Length; ++distIndex) {
      List<IAttribute> catValue =
          Categories[distIndex].GetValue(resolver, catdict, removed);
      int newCount = --distinct[distIndex][catValue];
      Debug.Assert(newCount >= 0);
      if (newCount == 0) {
        distinct[distIndex].Remove(catValue);
      }
    }
  }

  /// <summary>
  /// All categories that this condition outputs, possibly with duplicates
  /// </summary>
  public IEnumerable<AssetLocation> OutputCategories =>
      Categories.SelectMany(c => c.Categories.Concat(c.DistinctOutputs))
          .Concat(CountOutputs);

  public bool Validate(IWorldAccessor resolver, ILogger logger,
                       IReadonlyCategoryDict catdict) {
    bool result = true;
    foreach (ContentCategory category in Categories) {
      result &= category.Validate(resolver, logger, catdict);
    }
    return result;
  }
}
