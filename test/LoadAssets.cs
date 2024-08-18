using Microsoft.VisualStudio.TestTools.UnitTesting;

using PrefixClassName.MsTest;

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.Server;

namespace Gourmand.Tests;

[PrefixTestClass]
public class LoadAssets {
  // This property is set by the test framework:
  // https://learn.microsoft.com/en-us/visualstudio/test/how-to-create-a-data-driven-unit-test?view=vs-2022#add-a-testcontext-to-the-test-class
  public TestContext TestContext { get; set; } = null!;
  public static ServerMain Server = null;

  [AssemblyInitialize()]
  public static void AssemblyInitialize(TestContext context) {
    Dictionary<AssetCategory, HashSet<string>> allow = new();
    allow[AssetCategory.itemtypes] =
        new() { "fruit.json", "firestarter.json", "fish.json" };
    allow[AssetCategory.blocktypes] = new() { "egg.json" };
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

  public static void AssertCategoriesEqual(
      IEnumerable<KeyValuePair<AssetLocation, IAttribute>> expected,
      IEnumerable<KeyValuePair<AssetLocation, IAttribute>> actual) {
    CollectionAssert.AreEquivalent(
        expected
            .Select<KeyValuePair<AssetLocation, IAttribute>,
                    Tuple<AssetLocation, Type, string>>(
                (p) => new(p.Key, p.Value.GetType(), p.Value.ToString()))
            .ToList(),
        actual
            .Select<KeyValuePair<AssetLocation, IAttribute>,
                    Tuple<AssetLocation, Type, string>>(
                (p) => new(p.Key, p.Value.GetType(), p.Value.ToString()))
            .ToList());
  }
}
