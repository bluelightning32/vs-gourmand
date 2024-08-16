using Microsoft.VisualStudio.TestTools.UnitTesting;

using Vintagestory.API.Common;
using Vintagestory.Server;

namespace Gourmand.Tests;

[TestClass]
public class LoadAssets {
  // This property is set by the test framework:
  // https://learn.microsoft.com/en-us/visualstudio/test/how-to-create-a-data-driven-unit-test?view=vs-2022#add-a-testcontext-to-the-test-class
  public TestContext TestContext { get; set; } = null!;
  public static ServerMain Server = null;

  [AssemblyInitialize()]
  public static void AssemblyInitialize(TestContext context) {
    Dictionary<AssetCategory, HashSet<string>> allow = new();
    allow[AssetCategory.itemtypes] = new() { "fruit.json", "firestarter.json" };
    allow[AssetCategory.blocktypes] = new() { "egg.json" };
    Server = ServerApiWithAssets.Create(allow);
  }

  [AssemblyCleanup()]
  public static void AssemblyCleanup() { Server?.Dispose(); }

  [TestMethod]
  public void PineappleLoaded() {
    Item pineapple =
        Server.World.GetItem(new AssetLocation("game:fruit-pineapple"));
    Assert.IsNotNull(pineapple);
    Assert.AreEqual(EnumFoodCategory.Fruit,
                    pineapple.NutritionProps.FoodCategory);
  }
}
