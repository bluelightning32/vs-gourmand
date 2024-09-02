using Microsoft.VisualStudio.TestTools.UnitTesting;

using PrefixClassName.MsTest;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Gourmand.Test;

using Real = Gourmand;

[PrefixTestClass]
public class SlotCondition {
  private static readonly Real.Collectibles.MatchResolver Resolver;

  static SlotCondition() {
    Resolver = new(LoadAssets.Server.World);

    string rulesJson = @"
    [
      {
        nutritionProps : {
          category: {
            outputs: [ ""food-category"" ]
          },
        },
      },
      {
        code : {
          match: ""game:firestarter"",
          type: ""item"",
        },
        outputs: {
          ""overlap"": [ ""tool"" ]
        }
      },
      {
        code : {
          match: ""game:tongs"",
          type: ""item"",
        },
        outputs: {
          ""overlap"": [ ""tool"" ]
        }
      },
      {
        code : {
          match: ""game:fruit-pineapple"",
          type: ""item""
        },
        outputs: {
          ""overlap"": [ ""fruit"" ]
        }
      },
      {
        code : {
          match: ""game:fruit-cranberry"",
          type: ""item""
        },
        outputs: {
          ""overlap"": [ ""fruit"" ]
        }
      },
      {
        categories: [
          {
            input: ""food-category"",
            outputs: [ ""pie-filling-category"" ]
          }
        ],
        code: {
          match: ""*:*"",
          type: ""item"",
          outputs: [ ""pie-filling"" ]
        },
        attributes: [
          {
            path: [""inPieProperties"", ""partType""],
            value: ""Filling""
          }
        ],
        outputs: {
          ""pie-filling"": [ ""item"" ]
        }
      },
      {
        categories: [
          {
            input: ""food-category"",
            outputs: [ ""pie-filling-category"" ]
          }
        ],
        code: {
          match: ""*:*"",
          type: ""block"",
          outputs: [ ""pie-filling"" ]
        },
        attributes: [
          {
            path: [""inPieProperties"", ""partType""],
            value: ""Filling""
          }
        ],
        outputs: {
          ""pie-filling"": [ ""block"" ]
        }
      },
    ]";
    List<Real.Collectibles.MatchRule> rules =
        JsonUtil.ToObject<List<Real.Collectibles.MatchRule>>(rulesJson,
                                                             "gourmand");

    Resolver.Load(rules);
    Item pineapple = LoadAssets.GetItem("game", "fruit-pineapple");
    Assert.IsTrue(Resolver.CatDict.InCategory(
        new AssetLocation("gourmand", "pie-filling"), pineapple));
    Item onion = LoadAssets.GetItem("game", "vegetable-onion");
    Assert.IsTrue(Resolver.CatDict.InCategory(
        new AssetLocation("gourmand", "food-category"), onion));
    Assert.IsTrue(Resolver.CatDict.InCategory(
        new AssetLocation("gourmand", "pie-filling"), onion));
  }

  [TestMethod]
  public void IsMatchMin() {
    string json = @"
    {
      min: 2
    }
    ";
    Real.SlotCondition cond =
        JsonObject.FromJson(json).AsObject<Real.SlotCondition>(null,
                                                               "gourmand");
    Item pineapple = LoadAssets.GetItem("game", "fruit-pineapple");
    ItemStack[] contents;

    // When the minimum is not met, then the match fails and nothing is removed.
    contents = new ItemStack[] { new(pineapple) };
    Assert.IsFalse(cond.IsMatch(Resolver.Resolver, Resolver.CatDict, contents));
    CollectionAssert.AllItemsAreNotNull(contents);

    // When the minimum is met, the match succeeds and the matching stacks are
    // taken.
    contents = new ItemStack[] { new(pineapple), new(pineapple) };
    Assert.IsTrue(cond.IsMatch(Resolver.Resolver, Resolver.CatDict, contents));
    Assert.IsNull(contents[0]);
    Assert.IsNull(contents[1]);

    // Without a maximum, all matching stacks are accepted.
    contents =
        new ItemStack[] { new(pineapple), new(pineapple), new(pineapple) };
    Assert.IsTrue(cond.IsMatch(Resolver.Resolver, Resolver.CatDict, contents));
    Assert.IsNull(contents[0]);
    Assert.IsNull(contents[1]);
    Assert.IsNull(contents[2]);
  }

  [TestMethod]
  public void IsMatchMax() {
    string json = @"
    {
      min: 0,
      max: 2
    }
    ";
    Real.SlotCondition cond =
        JsonObject.FromJson(json).AsObject<Real.SlotCondition>(null,
                                                               "gourmand");
    Item pineapple = LoadAssets.GetItem("game", "fruit-pineapple");
    ItemStack[] contents;

    // 0 item stacks are accepted, because it is below the maximum.
    contents = new ItemStack[] {};
    Assert.IsTrue(cond.IsMatch(Resolver.Resolver, Resolver.CatDict, contents));

    // 1 item stack is accepted, because it is below the maximum.
    contents = new ItemStack[] { new(pineapple) };
    Assert.IsTrue(cond.IsMatch(Resolver.Resolver, Resolver.CatDict, contents));
    Assert.IsNull(contents[0]);

    // 2 item stacks are accepted, because they is below the maximum.
    contents = new ItemStack[] { new(pineapple), new(pineapple) };
    Assert.IsTrue(cond.IsMatch(Resolver.Resolver, Resolver.CatDict, contents));
    Assert.IsNull(contents[0]);
    Assert.IsNull(contents[1]);

    // Only 2 out of 3 item stacks are accepted, because the maximum is 2.
    contents =
        new ItemStack[] { new(pineapple), new(pineapple), new(pineapple) };
    Assert.IsTrue(cond.IsMatch(Resolver.Resolver, Resolver.CatDict, contents));
    Assert.IsNull(contents[0]);
    Assert.IsNull(contents[1]);
    Assert.IsNotNull(contents[2]);
  }

  [TestMethod]
  public void IsMatchSlotRange() {
    string json = @"
    {
      slotBegin: 1,
      slotEnd: 5,
      min: 2
    }
    ";
    Real.SlotCondition cond =
        JsonObject.FromJson(json).AsObject<Real.SlotCondition>(null,
                                                               "gourmand");
    Item pineapple = LoadAssets.GetItem("game", "fruit-pineapple");
    ItemStack[] contents;

    // The minimum is not met within the slot range.
    contents = new ItemStack[] { new(pineapple), new(pineapple) };
    Assert.IsFalse(cond.IsMatch(Resolver.Resolver, Resolver.CatDict, contents));
    CollectionAssert.AllItemsAreNotNull(contents);

    // The minimum is met at the start of the range.
    contents = new ItemStack[] { new(pineapple), new(pineapple), new(pineapple),
                                 null };
    Assert.IsTrue(cond.IsMatch(Resolver.Resolver, Resolver.CatDict, contents));
    Assert.IsNotNull(contents[0]);
    Assert.IsNull(contents[1]);
    Assert.IsNull(contents[2]);
    Assert.IsNull(contents[3]);

    // The minimum is met at the end of the range.
    contents =
        new ItemStack[] { new(pineapple), null,           null,
                          new(pineapple), new(pineapple), new(pineapple) };
    Assert.IsTrue(cond.IsMatch(Resolver.Resolver, Resolver.CatDict, contents));
    // Unchanged
    Assert.IsNotNull(contents[0]);
    Assert.IsNull(contents[1]);
    Assert.IsNull(contents[2]);
    // Taken
    Assert.IsNull(contents[3]);
    Assert.IsNull(contents[4]);
    // Unchanged
    Assert.IsNotNull(contents[5]);

    // The minimum is not met at the end of the range.
    contents =
        new ItemStack[] { new(pineapple), null,           null,          null,
                          new(pineapple), new(pineapple), new(pineapple) };
    Assert.IsFalse(cond.IsMatch(Resolver.Resolver, Resolver.CatDict, contents));
    Assert.IsNotNull(contents[0]);
    Assert.IsNull(contents[1]);
    Assert.IsNull(contents[2]);
    Assert.IsNull(contents[3]);
    Assert.IsNotNull(contents[4]);
    Assert.IsNotNull(contents[5]);
    Assert.IsNotNull(contents[6]);
  }

  [TestMethod]
  public void IsMatch2Categories() {
    string json = @"
    {
      min: 1,
      categories: [
        {
          input: ""pie-filling"",
        },
        {
          input: ""overlap"",
        },
      ]
    }
    ";
    Real.SlotCondition cond =
        JsonObject.FromJson(json).AsObject<Real.SlotCondition>(null,
                                                               "gourmand");
    Item blueberry = LoadAssets.GetItem("game", "fruit-blueberry");
    Item cranberry = LoadAssets.GetItem("game", "fruit-cranberry");
    Item firestarter = LoadAssets.GetItem("game", "firestarter");
    ItemStack[] contents;

    // blueberry is not in the overlap category
    contents = new ItemStack[] { new(blueberry) };
    Assert.IsFalse(cond.IsMatch(Resolver.Resolver, Resolver.CatDict, contents));
    CollectionAssert.AllItemsAreNotNull(contents);

    // firestarter is not in the overlap category
    contents = new ItemStack[] { new(firestarter) };
    Assert.IsFalse(cond.IsMatch(Resolver.Resolver, Resolver.CatDict, contents));
    CollectionAssert.AllItemsAreNotNull(contents);

    // cranberry is in both
    contents = new ItemStack[] { new(cranberry) };
    Assert.IsTrue(cond.IsMatch(Resolver.Resolver, Resolver.CatDict, contents));
    Assert.IsNull(contents[0]);
  }

  [TestMethod]
  public void IsMatchDistinctMin() {
    string json = @"
    {
      min: 1,
      categories: [
        {
          input: ""pie-filling"",
          distinctMin: 3
        },
        {
          input: ""pie-filling-category"",
          distinctMin: 2
        },
      ]
    }
    ";
    Real.SlotCondition cond =
        JsonObject.FromJson(json).AsObject<Real.SlotCondition>(null,
                                                               "gourmand");
    Item pineapple = LoadAssets.GetItem("game", "fruit-pineapple");
    Item cranberry = LoadAssets.GetItem("game", "fruit-cranberry");
    Item blackcurrant = LoadAssets.GetItem("game", "fruit-blackcurrant");
    Item onion = LoadAssets.GetItem("game", "vegetable-onion");
    ItemStack[] contents;

    // not enough of either category
    contents =
        new ItemStack[] { new(pineapple), new(pineapple), new(pineapple) };
    Assert.IsFalse(cond.IsMatch(Resolver.Resolver, Resolver.CatDict, contents));
    CollectionAssert.AllItemsAreNotNull(contents);

    // not enough of the pie-filling-category category
    contents =
        new ItemStack[] { new(pineapple), new(cranberry), new(blackcurrant) };
    Assert.IsFalse(cond.IsMatch(Resolver.Resolver, Resolver.CatDict, contents));
    CollectionAssert.AllItemsAreNotNull(contents);

    // not enough of the pie-filling category
    contents = new ItemStack[] { new(pineapple), new(onion), new(onion) };
    Assert.IsFalse(cond.IsMatch(Resolver.Resolver, Resolver.CatDict, contents));
    CollectionAssert.AllItemsAreNotNull(contents);

    // enough of both categories
    contents =
        new ItemStack[] { new(pineapple), new(onion), new(blackcurrant) };
    Assert.IsTrue(cond.IsMatch(Resolver.Resolver, Resolver.CatDict, contents));
    Assert.IsTrue(contents.All(c => c == null));

    // enough of both categories, and more than enough of the pie-filling
    contents = new ItemStack[] { new(pineapple), new(onion), new(blackcurrant),
                                 new(cranberry) };
    Assert.IsTrue(cond.IsMatch(Resolver.Resolver, Resolver.CatDict, contents));
    // All of the fruit should be accepted, even though they are above the
    // distinctMin.
    Assert.IsTrue(contents.All(c => c == null));
  }

  [TestMethod]
  public void IsMatchDistinctMax() {
    string json = @"
    {
      min: 1,
      categories: [
        {
          input: ""pie-filling"",
          distinctMax: 2
        },
        {
          input: ""pie-filling-category"",
          distinctMax: 1
        },
      ]
    }
    ";
    Real.SlotCondition cond =
        JsonObject.FromJson(json).AsObject<Real.SlotCondition>(null,
                                                               "gourmand");
    Item pineapple = LoadAssets.GetItem("game", "fruit-pineapple");
    Item cranberry = LoadAssets.GetItem("game", "fruit-cranberry");
    Item blackcurrant = LoadAssets.GetItem("game", "fruit-blackcurrant");
    Item onion = LoadAssets.GetItem("game", "vegetable-onion");
    ItemStack[] contents;

    // The same repeated item is not too much of either
    contents =
        new ItemStack[] { new(pineapple), new(pineapple), new(pineapple) };
    Assert.IsTrue(cond.IsMatch(Resolver.Resolver, Resolver.CatDict, contents));
    Assert.IsTrue(contents.All(c => c == null));

    // 2 different pie-fillings is okay
    contents = new ItemStack[] { new(pineapple), new(cranberry) };
    Assert.IsTrue(cond.IsMatch(Resolver.Resolver, Resolver.CatDict, contents));
    Assert.IsTrue(contents.All(c => c == null));

    // 3 different pie-fillings is too much
    contents =
        new ItemStack[] { new(pineapple), new(cranberry), new(blackcurrant) };
    Assert.IsFalse(cond.IsMatch(Resolver.Resolver, Resolver.CatDict, contents));
    CollectionAssert.AllItemsAreNotNull(contents);

    // 2 different pie-filling-categories is too much
    contents = new ItemStack[] { new(pineapple), new(onion) };
    Assert.IsFalse(cond.IsMatch(Resolver.Resolver, Resolver.CatDict, contents));
    CollectionAssert.AllItemsAreNotNull(contents);
  }

  [TestMethod]
  public void OutputCategories() {
    string json = @"
    {
      min: 2,
      countOutputs: [ ""count"" ],
      categories: [
        {
          input: ""pie-filling"",
          outputs: [ ""extra1"" ],
          distinctOutputs: [ ""pie-contents"" ]
        },
        {
          input: ""pie-filling-category"",
          outputs: [ ""extra2"" ],
          distinctOutputs: [ ""pie-type"" ]
        },
      ]
    }
    ";
    Real.SlotCondition cond =
        JsonObject.FromJson(json).AsObject<Real.SlotCondition>(null,
                                                               "gourmand");
    CollectionAssert.AreEqual(
        new List<AssetLocation>() { new("gourmand", "extra1"),
                                    new("gourmand", "pie-contents"),
                                    new("gourmand", "extra2"),
                                    new("gourmand", "pie-type"),
                                    new("gourmand", "count") },
        cond.OutputCategories.ToList());
  }

  [TestMethod]
  public void AppendValueCount() {
    string json = @"
    {
      min: 2,
      countOutputs: [ ""count"" ]
    }
    ";
    Real.SlotCondition cond =
        JsonObject.FromJson(json).AsObject<Real.SlotCondition>(null,
                                                               "gourmand");
    Item pineapple = LoadAssets.GetItem("game", "fruit-pineapple");
    for (int i = 2; i < 5; ++i) {
      ItemStack[] contents =
          Enumerable.Repeat(new ItemStack(pineapple), i).ToArray();

      List<IAttribute> result = new();
      cond.AppendValue(Resolver.Resolver, Resolver.CatDict,
                       new AssetLocation("gourmand", "count"), contents,
                       result);
      Assert.IsTrue(contents.All(c => c == null));
      Assert.IsTrue(Real.CategoryValue.ValuesEqual(
          new IAttribute[] { new IntAttribute(i) }, result));
    }
  }

  [TestMethod]
  public void AppendValueDistinct() {
    string json = @"
    {
      min: 1,
      categories: [
        {
          input: ""pie-filling"",
          distinctOutputs: [ ""pie-contents"" ]
        },
        {
          input: ""pie-filling-category"",
          distinctOutputs: [ ""pie-type"" ]
        },
      ]
    }
    ";
    Real.SlotCondition cond =
        JsonObject.FromJson(json).AsObject<Real.SlotCondition>(null,
                                                               "gourmand");
    Item pineapple = LoadAssets.GetItem("game", "fruit-pineapple");
    Item cranberry = LoadAssets.GetItem("game", "fruit-cranberry");
    Item blackcurrant = LoadAssets.GetItem("game", "fruit-blackcurrant");
    Item onion = LoadAssets.GetItem("game", "vegetable-onion");
    AssetLocation pieFilling = new("gourmand", "pie-contents");
    AssetLocation pieType = new("gourmand", "pie-type");
    ItemStack[] contents;
    List<IAttribute> result;

    // The same fruit repeated
    contents =
        new ItemStack[] { new(pineapple), new(pineapple), new(pineapple) };
    result = new();
    cond.AppendValue(Resolver.Resolver, Resolver.CatDict, pieFilling, contents,
                     result);
    Assert.IsTrue(contents.All(c => c == null));
    Assert.IsTrue(Real.CategoryValue.ValuesEqual(
        new IAttribute[] { new StringAttribute("item"),
                           new StringAttribute("game:fruit-pineapple") },
        result));

    contents =
        new ItemStack[] { new(pineapple), new(pineapple), new(pineapple) };
    result = new();
    cond.AppendValue(Resolver.Resolver, Resolver.CatDict, pieType, contents,
                     result);
    Assert.IsTrue(contents.All(c => c == null));
    Assert.IsTrue(Real.CategoryValue.ValuesEqual(
        new IAttribute[] { new StringAttribute("Fruit") }, result));

    // Two different fruits
    contents = new ItemStack[] { new(pineapple), new(cranberry) };
    result = new();
    cond.AppendValue(Resolver.Resolver, Resolver.CatDict, pieFilling, contents,
                     result);
    Assert.IsTrue(contents.All(c => c == null));
    Assert.IsTrue(Real.CategoryValue.ValuesEqualEitherOrder(
        new IAttribute[] { new StringAttribute("item"),
                           new StringAttribute("game:fruit-pineapple") },
        new IAttribute[] { new StringAttribute("item"),
                           new StringAttribute("game:fruit-cranberry") },
        result));

    contents = new ItemStack[] { new(pineapple), new(cranberry) };
    result = new();
    cond.AppendValue(Resolver.Resolver, Resolver.CatDict, pieType, contents,
                     result);
    Assert.IsTrue(contents.All(c => c == null));
    Assert.IsTrue(Real.CategoryValue.ValuesEqual(
        new IAttribute[] { new StringAttribute("Fruit") }, result));

    // Two different categories
    contents = new ItemStack[] { new(pineapple), new(onion) };
    result = new();
    cond.AppendValue(Resolver.Resolver, Resolver.CatDict, pieFilling, contents,
                     result);
    Assert.IsTrue(contents.All(c => c == null));
    Assert.IsTrue(Real.CategoryValue.ValuesEqualEitherOrder(
        new IAttribute[] { new StringAttribute("item"),
                           new StringAttribute("game:fruit-pineapple") },
        new IAttribute[] { new StringAttribute("item"),
                           new StringAttribute("game:vegetable-onion") },
        result));

    contents = new ItemStack[] { new(pineapple), new(onion) };
    result = new();
    cond.AppendValue(Resolver.Resolver, Resolver.CatDict, pieType, contents,
                     result);
    Assert.IsTrue(contents.All(c => c == null));
    Assert.IsTrue(Real.CategoryValue.ValuesEqual(
        new IAttribute[] { new StringAttribute("Fruit"),
                           new StringAttribute("Vegetable") },
        result));
  }

  [TestMethod]
  public void EnumerateMatchContentsNoCategories() {
    string json = @"
    {
      min: 2,
      enumerateMax: 1
    }
    ";
    Real.SlotCondition cond =
        JsonObject.FromJson(json).AsObject<Real.SlotCondition>(null,
                                                               "gourmand");
    ItemStack[] contents = new ItemStack[5];
    Real.ContentBuilder builder = new(contents);
    int enumerated = 0;
    foreach (var _ in cond.EnumerateMatchContents(Resolver.Resolver,
                                                  Resolver.CatDict, builder,
                                                  new ValueTuple[] { new() })) {
      Assert.AreEqual(5, builder.Contents.Count);
      Assert.IsNotNull(builder.Contents[0]);
      Assert.IsNotNull(builder.Contents[1]);
      for (int i = 2; i < 5; ++i) {
        Assert.IsNull(builder.Contents[i]);
      }
      ++enumerated;
    }
    Assert.AreEqual(1, enumerated);
  }

  [TestMethod]
  public void EnumerateMatch1ContentsCategories() {
    string json = @"
    {
      min: 1,
      max: 1,
      categories: [
        {
          input: ""pie-filling"",
        },
        {
          input: ""overlap"",
        },
      ],
      enumerateMax: 1000
    }
    ";
    Real.SlotCondition cond =
        JsonObject.FromJson(json).AsObject<Real.SlotCondition>(null,
                                                               "gourmand");
    ItemStack[] contents = new ItemStack[5];
    Real.ContentBuilder builder = new(contents);
    List<CollectibleObject> expected =
        new() { LoadAssets.GetItem("game", "fruit-pineapple"),
                LoadAssets.GetItem("game", "fruit-cranberry") };
    int enumerated = 0;
    foreach (var _ in cond.EnumerateMatchContents(Resolver.Resolver,
                                                  Resolver.CatDict, builder,
                                                  new ValueTuple[] { new() })) {
      Assert.AreEqual(5, builder.Contents.Count);

      CollectionAssert.Contains(expected, builder.Contents[0].Collectible);
      for (int i = 1; i < 5; ++i) {
        Assert.IsNull(builder.Contents[i]);
      }
      ++enumerated;
    }
    Assert.AreEqual(2, enumerated);
  }

  [TestMethod]
  public void EnumerateMatchAny() {
    string json = @"
    {
      min: 3,
      max: 3,
      enumArrangement: ""any"",
      categories: [
        {
          input: ""pie-filling"",
        },
        {
          input: ""overlap"",
        },
      ],
      enumerateMax: 1000
    }
    ";
    Real.SlotCondition cond =
        JsonObject.FromJson(json).AsObject<Real.SlotCondition>(null,
                                                               "gourmand");
    Real.ContentBuilder builder = new(Array.Empty<ItemStack>());
    List<CollectibleObject> expected =
        new() { LoadAssets.GetItem("game", "fruit-pineapple"),
                LoadAssets.GetItem("game", "fruit-cranberry") };
    int enumerated = 0;
    HashSet<int> found = new();
    foreach (var _ in cond.EnumerateMatchContents(Resolver.Resolver,
                                                  Resolver.CatDict, builder,
                                                  new ValueTuple[] { new() })) {
      int id = 0;
      for (int i = 0; i < 3; ++i) {
        id = expected.IndexOf(builder.Contents[i].Collectible) + id * 2;
      }
      Assert.IsTrue(found.Add(id));
      ++enumerated;
    }
    Assert.AreEqual(2 * 2 * 2, enumerated);
  }

  [TestMethod]
  public void EnumerateMatchSortedWithRepeats() {
    string json = @"
    {
      min: 1,
      max: 3,
      enumArrangement: ""sortedWithRepeats"",
      categories: [
        {
          input: ""pie-filling"",
        },
        {
          input: ""overlap"",
        },
      ],
      enumerateMax: 1000
    }
    ";
    Real.SlotCondition cond =
        JsonObject.FromJson(json).AsObject<Real.SlotCondition>(null,
                                                               "gourmand");
    Real.ContentBuilder builder = new(Array.Empty<ItemStack>());
    List<CollectibleObject> allowed =
        cond.EnumerateAllowedStacks(Resolver.Resolver, Resolver.CatDict)
            .Select(i => i.Collectible)
            .ToList();
    CollectionAssert.AreEquivalent(
        new CollectibleObject[] { LoadAssets.GetItem("game", "fruit-pineapple"),
                                  LoadAssets.GetItem("game",
                                                     "fruit-cranberry") },
        allowed);
    allowed.Add(null);
    int enumerated = 0;
    HashSet<int> found = new();
    foreach (var _ in cond.EnumerateMatchContents(Resolver.Resolver,
                                                  Resolver.CatDict, builder,
                                                  new ValueTuple[] { new() })) {
      int id = 0;
      int last = -1;
      for (int i = 0; i < 3; ++i) {
        CollectibleObject c = null;
        if (i < builder.Contents.Count) {
          c = builder.Contents[i]?.Collectible;
        }
        int index = allowed.ToList().IndexOf(c);
        Assert.IsTrue(index >= last);
        last = index;
        id = index + id * 3;
      }
      Assert.IsTrue(found.Add(id));
      ++enumerated;
    }
    Assert.AreEqual(9, enumerated);
  }

  [TestMethod]
  public void EnumerateMatchSorted() {
    string json = @"
    {
      min: 1,
      max: 3,
      enumArrangement: ""sorted"",
      categories: [
        {
          input: ""pie-filling"",
        },
        {
          input: ""overlap"",
        },
      ],
      enumerateMax: 1000
    }
    ";
    Real.SlotCondition cond =
        JsonObject.FromJson(json).AsObject<Real.SlotCondition>(null,
                                                               "gourmand");
    Real.ContentBuilder builder = new(Array.Empty<ItemStack>());
    List<CollectibleObject> allowed =
        cond.EnumerateAllowedStacks(Resolver.Resolver, Resolver.CatDict)
            .Select(i => i.Collectible)
            .ToList();
    CollectionAssert.AreEquivalent(
        new CollectibleObject[] { LoadAssets.GetItem("game", "fruit-pineapple"),
                                  LoadAssets.GetItem("game",
                                                     "fruit-cranberry") },
        allowed);
    allowed.Add(null);
    int enumerated = 0;
    HashSet<int> found = new();
    foreach (var _ in cond.EnumerateMatchContents(Resolver.Resolver,
                                                  Resolver.CatDict, builder,
                                                  new ValueTuple[] { new() })) {
      int id = 0;
      int last = -1;
      for (int i = 0; i < 3; ++i) {
        CollectibleObject c = null;
        if (i < builder.Contents.Count) {
          c = builder.Contents[i]?.Collectible;
        }
        int index = allowed.ToList().IndexOf(c);
        Assert.IsTrue(index > last || (last == 2 && index == 2));
        last = index;
        id = index + id * 3;
      }
      Assert.IsTrue(found.Add(id));
      ++enumerated;
    }
    Assert.AreEqual(3, enumerated);
  }

  [TestMethod]
  public void EnumerateMatchRepeated() {
    string json = @"
    {
      min: 1,
      max: 3,
      enumArrangement: ""repeated"",
      categories: [
        {
          input: ""pie-filling"",
        },
        {
          input: ""overlap"",
        },
      ],
      enumerateMax: 1000
    }
    ";
    Real.SlotCondition cond =
        JsonObject.FromJson(json).AsObject<Real.SlotCondition>(null,
                                                               "gourmand");
    Real.ContentBuilder builder = new(Array.Empty<ItemStack>());
    List<CollectibleObject> allowed =
        cond.EnumerateAllowedStacks(Resolver.Resolver, Resolver.CatDict)
            .Select(i => i.Collectible)
            .ToList();
    CollectionAssert.AreEquivalent(
        new CollectibleObject[] { LoadAssets.GetItem("game", "fruit-pineapple"),
                                  LoadAssets.GetItem("game",
                                                     "fruit-cranberry") },
        allowed);
    allowed.Add(null);
    int enumerated = 0;
    HashSet<int> found = new();
    foreach (var _ in cond.EnumerateMatchContents(Resolver.Resolver,
                                                  Resolver.CatDict, builder,
                                                  new ValueTuple[] { new() })) {
      int id = 0;
      int last = -1;
      for (int i = 0; i < 3; ++i) {
        CollectibleObject c = null;
        if (i < builder.Contents.Count) {
          c = builder.Contents[i]?.Collectible;
        }
        int index = allowed.ToList().IndexOf(c);
        Assert.IsTrue(last == -1 || index == last || index == 2);
        last = index;
        id = index + id * 3;
      }
      Assert.IsTrue(found.Add(id));
      ++enumerated;
    }
    Assert.AreEqual(6, enumerated);
  }

  [TestMethod]
  public void EnumerateMatchDistinct() {
    string json = @"
    {
      min: 1,
      max: 4,
      enumArrangement: ""any"",
      categories: [
        {
          input: ""overlap"",
          distinctMin: 2,
          distinctMax: 3,
        },
      ],
      enumerateMax: 1000
    }
    ";
    Real.SlotCondition cond =
        JsonObject.FromJson(json).AsObject<Real.SlotCondition>(null,
                                                               "gourmand");
    Real.ContentBuilder builder = new(Array.Empty<ItemStack>());
    List<CollectibleObject> allowed =
        cond.EnumerateAllowedStacks(Resolver.Resolver, Resolver.CatDict)
            .Select(i => i.Collectible)
            .ToList();
    CollectionAssert.AreEquivalent(
        new CollectibleObject[] {
          LoadAssets.GetItem("game", "fruit-pineapple"),
          LoadAssets.GetItem("game", "fruit-cranberry"),
          LoadAssets.GetItem("game", "firestarter"),
          LoadAssets.GetItem("game", "tongs"),
        },
        allowed);
    int enumerated = 0;
    HashSet<int> found = new();
    foreach (var _ in cond.EnumerateMatchContents(Resolver.Resolver,
                                                  Resolver.CatDict, builder,
                                                  new ValueTuple[] { new() })) {
      HashSet<int> foundCollectibles = new();
      int id = 0;
      for (int i = 0; i < 4; ++i) {
        CollectibleObject c = null;
        if (i < builder.Contents.Count) {
          c = builder.Contents[i]?.Collectible;
        }
        int index;
        if (c == null) {
          index = allowed.Count;
        } else {
          index = allowed.ToList().IndexOf(c);
          foundCollectibles.Add(index);
        }
        Assert.IsTrue(0 <= index && index <= allowed.Count);
        id = index + id * (allowed.Count + 1);
      }
      Assert.IsTrue(found.Add(id));
      Assert.IsTrue(foundCollectibles.Count <= 4);
      ++enumerated;
    }
    Assert.IsTrue(enumerated > float.Pow(allowed.Count, 3));
    Assert.IsTrue(enumerated < float.Pow(allowed.Count + 1, 4) - 4);
  }

  [TestMethod]
  public void EnumerateMatchMultiplyExisting() {
    string json = @"
    {
      min: 1,
      max: 1,
      categories: [
        {
          input: ""pie-filling"",
        },
        {
          input: ""overlap"",
        },
      ],
      enumerateMax: 1000
    }
    ";
    Real.SlotCondition cond =
        JsonObject.FromJson(json).AsObject<Real.SlotCondition>(null,
                                                               "gourmand");
    Real.ContentBuilder builder = new(Array.Empty<ItemStack>());

    List<CollectibleObject> initialObjects = new() {
      LoadAssets.GetItem("game", "firestarter"),
      LoadAssets.GetItem("game", "tongs"),
    };

    IEnumerable<ValueTuple> GetInitial() {
      foreach (CollectibleObject c in initialObjects) {
        Assert.IsTrue(builder.PushValue(new ItemStack(c), 0, 1));
        yield return new();
        builder.PopValue();
      }
    };

    List<CollectibleObject> allowed =
        cond.EnumerateAllowedStacks(Resolver.Resolver, Resolver.CatDict)
            .Select(i => i.Collectible)
            .ToList();
    CollectionAssert.AreEquivalent(
        new CollectibleObject[] { LoadAssets.GetItem("game", "fruit-pineapple"),
                                  LoadAssets.GetItem("game",
                                                     "fruit-cranberry") },
        allowed);

    int enumerated = 0;
    HashSet<int> found = new();
    foreach (var _ in cond.EnumerateMatchContents(
                 Resolver.Resolver, Resolver.CatDict, builder, GetInitial())) {
      int id = 0;
      for (int i = 0; i < 1; ++i) {
        CollectibleObject c = null;
        if (i < builder.Contents.Count) {
          c = builder.Contents[i]?.Collectible;
        }
        int index = initialObjects.ToList().IndexOf(c);
        id = index + id * (initialObjects.Count + 1);
      }
      for (int i = 1; i < 4; ++i) {
        CollectibleObject c = null;
        if (i < builder.Contents.Count) {
          c = builder.Contents[i]?.Collectible;
        }
        int index;
        if (c == null) {
          index = allowed.Count;
        } else {
          index = allowed.ToList().IndexOf(c);
        }
        Assert.IsTrue(0 <= index && index <= allowed.Count);
        id = index + id * (allowed.Count + 1);
      }
      Assert.IsTrue(found.Add(id));
      ++enumerated;
    }
    Assert.AreEqual(4, enumerated);
  }
}
