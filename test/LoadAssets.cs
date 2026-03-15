using PrefixClassName.MsTest;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.Server;

using Real = Gourmand;

namespace Gourmand.Test;

[PrefixTestClass]
public class LoadAssets {
  // This property is set by the test framework:
  // https://learn.microsoft.com/en-us/visualstudio/test/how-to-create-a-data-driven-unit-test?view=vs-2022#add-a-testcontext-to-the-test-class
  public TestContext TestContext { get; set; } = null!;
  public static ServerMain Server = null;

  [AssemblyInitialize()]
  public static void AssemblyInitialize(TestContext context) {
    Dictionary<AssetCategory, HashSet<string>> allow =
        new() { [AssetCategory.itemtypes] =
                    new() { "fruit.json", "firestarter.json", "fishfillet.json",
                            "tongs.json", "vegetable.json",
                            // Allow meatystew.topping to be loaded to prevent a
                            // Gourmand warning during the unit tests.
                            "jamhoneyportion.json", "honeyportion.json",
                            // Allow meatystew.egg-base to be loaded to prevent
                            // a Gourmand warning during the unit tests.
                            "egg.json" },
                [AssetCategory.blocktypes] =
                    new() { "bowl-meal.json", "egg.json", "mushroom.json",
                            "pie.json" },
                [AssetCategory.recipes] =
                    new() { "leather.json", "meatystew.json",
                            "vegetablestew.json" } };
    Server = ServerApiWithAssets.Create(allow);
  }

  [AssemblyCleanup()]
  public static void AssemblyCleanup() { Server?.Dispose(); }

  public static Item GetItem(string domain, string code) {
    return Server.World.GetItem(new AssetLocation(domain, code));
  }

  public static Block GetBlock(string domain, string code) {
    return Server.World.GetBlock(new AssetLocation(domain, code));
  }

  [TestMethod]
  public void PineappleLoaded() {
    Item pineapple =
        Server.World.GetItem(new AssetLocation("game:fruit-pineapple"));
    Assert.IsNotNull(pineapple);
    Assert.AreEqual(EnumFoodCategory.Fruit,
                    pineapple.NutritionProps.FoodCategory);
  }

  [TestMethod]
  public void FishLoaded() {
    Item fishRaw = Server.World.GetItem(new AssetLocation("game:fish-raw"));
    Assert.IsNotNull(fishRaw);
    Item fishCooked =
        Server.World.GetItem(new AssetLocation("game:fish-cooked"));
    Assert.IsNotNull(fishCooked);
    Assert.AreEqual(EnumFoodCategory.Protein,
                    fishCooked.NutritionProps.FoodCategory);
  }

  [TestMethod]
  public void EggChickenItemLoaded() {
    Item egg = Server.World.GetItem(new AssetLocation("game:egg-chicken-raw"));
    Assert.IsNotNull(egg);
  }

  [TestMethod]
  public void HoneyPortionLoaded() {
    Item item = Server.World.GetItem(new AssetLocation("game:honeyportion"));
    Assert.IsNotNull(item);
  }

  public static void AssertCategoriesEqual(
      IEnumerable<KeyValuePair<AssetLocation, IAttribute[]>> expected,
      IEnumerable<KeyValuePair<AssetLocation, IAttribute[]>> actual) {
    CollectionAssert.AreEquivalent(
        expected
            .Select<KeyValuePair<AssetLocation, IAttribute[]>,
                    KeyValuePair<AssetLocation, Real.CategoryValue>>(
                (p) => new(p.Key, new(1, p.Value.ToList())))
            .ToList(),
        actual
            .Select<KeyValuePair<AssetLocation, IAttribute[]>,
                    KeyValuePair<AssetLocation, Real.CategoryValue>>(
                (p) => new(p.Key, new(1, p.Value.ToList())))
            .ToList());
  }

  public static void AssertCategoriesEqual(
      IEnumerable<KeyValuePair<AssetLocation, IAttribute>> expected,
      IEnumerable<KeyValuePair<AssetLocation, IAttribute[]>> actual) {
    AssertCategoriesEqual(
        expected.Select((p) => new KeyValuePair<AssetLocation, IAttribute[]>(
                            p.Key, new IAttribute[] { p.Value })),
        actual);
  }
}
