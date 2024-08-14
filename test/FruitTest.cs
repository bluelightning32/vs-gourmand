using System.Reflection;
using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Common;
using Vintagestory.GameContent;
using Vintagestory.Server;

namespace Gourmand.Tests;

[TestClass]
public class FruitTest {
  // This property is set by the test framework:
  // https://learn.microsoft.com/en-us/visualstudio/test/how-to-create-a-data-driven-unit-test?view=vs-2022#add-a-testcontext-to-the-test-class
  public TestContext TestContext { get; set; } = null!;
  static ServerMain s_server = null;

  [ClassInitialize()]
  public static void ClassInit(TestContext context) {
    Dictionary<AssetCategory, HashSet<string>> allow = new();
    allow[AssetCategory.itemtypes] = new() { "fruit.json" };
    s_server = ServerApiWithAssets.Create(allow);
  }

  [ClassCleanup()]
  public static void ClassCleanup() { s_server?.Dispose(); }

  [TestMethod]
  public void PineappleLoaded() {
    Item pineapple =
        s_server.World.GetItem(new AssetLocation("game:fruit-pineapple"));
    Assert.IsNotNull(pineapple);
    Assert.AreEqual(EnumFoodCategory.Fruit,
                    pineapple.NutritionProps.FoodCategory);
  }
}
