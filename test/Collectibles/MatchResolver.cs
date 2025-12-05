using Moq;

using PrefixClassName.MsTest;

using Vintagestory.API.Common;

namespace Gourmand.Test.Collectibles;

using Real = Gourmand.Collectibles;

[PrefixTestClass]
public class MatchResolver {
  private readonly Mock<IWorldAccessor> _mock = new Mock<IWorldAccessor>();
  private readonly Real.MatchResolver _resolver;

  public MatchResolver() {
    _mock.Setup(x => x.GetItem(It.IsAny<AssetLocation>()))
        .Returns<AssetLocation>(LoadAssets.Server.GetItem);
    _mock.Setup(x => x.SearchItems(It.IsAny<AssetLocation>()))
        .Returns<AssetLocation>(LoadAssets.Server.SearchItems);
    _mock.SetupGet(x => x.Items).Returns(LoadAssets.Server.World.Items);
    _mock.Setup(x => x.GetBlock(It.IsAny<AssetLocation>()))
        .Returns<AssetLocation>(LoadAssets.Server.GetBlock);
    _mock.Setup(x => x.SearchBlocks(It.IsAny<AssetLocation>()))
        .Returns<AssetLocation>(LoadAssets.Server.SearchBlocks);
    _mock.SetupGet(x => x.Blocks).Returns(LoadAssets.Server.World.Blocks);

    _resolver = new(_mock.Object, LoadAssets.Server.Api.Logger);
  }

  [TestMethod]
  public void ResolveUnnecessaryRegexItem() {
    IReadOnlyList<Item> items = _resolver.GetMatchingItems(
        new AssetLocation("game", "@fruit-pineapple"));
    Assert.AreEqual("fruit-pineapple", items[0].Code.Path);
    _mock.Verify(x => x.SearchItems(It.IsAny<AssetLocation>()), Times.Never);
  }

  [TestMethod]
  public void ResolveComplicatedRegexItem() {
    IReadOnlyList<Item> items =
        _resolver.GetMatchingItems(new AssetLocation("game", "@frui.*"));
    Assert.IsTrue(items.Any((item) => item.Code.Path == "fruit-pineapple"));
    _mock.Verify(x => x.SearchItems(It.IsAny<AssetLocation>()), Times.Once);
  }

  [TestMethod]
  public void ResolveAcceleratedWildcardItem() {
    IReadOnlyList<Item> items =
        _resolver.GetMatchingItems(new AssetLocation("game", "fruit-*"));
    Assert.IsTrue(items.Any((item) => item.Code.Path == "fruit-pineapple"));
    _mock.Verify(x => x.SearchItems(It.IsAny<AssetLocation>()), Times.Never);
  }

  [TestMethod]
  public void ResolveUnacceleratedWildcardItem() {
    IReadOnlyList<Item> items =
        _resolver.GetMatchingItems(new AssetLocation("game", "frui*"));
    Assert.IsTrue(items.Any((item) => item.Code.Path == "fruit-pineapple"));
    _mock.Verify(x => x.SearchItems(It.IsAny<AssetLocation>()), Times.Once);
  }

  [TestMethod]
  public void ResolveExactItemWithHyphen() {
    IReadOnlyList<Item> items = _resolver.GetMatchingItems(
        new AssetLocation("game", "fruit-pineapple"));
    Assert.IsTrue(items.All((item) => item.Code.Path == "fruit-pineapple"));
    _mock.Verify(x => x.GetItem(It.IsAny<AssetLocation>()), Times.Once);
  }

  [TestMethod]
  public void ResolveExactItemWithoutHyphen() {
    IReadOnlyList<Item> items =
        _resolver.GetMatchingItems(new AssetLocation("game", "firestarter"));
    Assert.IsTrue(items.All((item) => item.Code.Path == "firestarter"));
    _mock.Verify(x => x.GetItem(It.IsAny<AssetLocation>()), Times.Once);
  }

  [TestMethod]
  public void ResolveAcceleratedWildcardBlock() {
    IReadOnlyList<Block> blocks =
        _resolver.GetMatchingBlocks(new AssetLocation("game", "egg-*"));
    Assert.IsTrue(blocks.Any((block) => block.Code.Path == "egg-chicken-1"));
    _mock.Verify(x => x.SearchBlocks(It.IsAny<AssetLocation>()), Times.Never);
  }

  [TestMethod]
  public void GetMatchingBlocksWildcardStart() {
    Block chickenEgg = LoadAssets.Server.World.GetBlock(
        new AssetLocation("game", "egg-chicken-1"));
    Assert.IsNotNull(chickenEgg);
    Assert.AreNotEqual(0, chickenEgg.Id);
    IReadOnlyList<Block> blocks =
        _resolver.GetMatchingBlocks(new AssetLocation("game", "*-chicken-1"));
    Assert.IsTrue(blocks.Any((block) => block.Id == chickenEgg.Id));
    Assert.IsTrue(
        blocks.All((block) => block.Code.Path.EndsWith("-chicken-1")));
  }

  [TestMethod]
  public void SearchAllBlocks() {
    IReadOnlyList<Block> blocks =
        _resolver.GetMatchingBlocks(new AssetLocation("*", "*"));
    Assert.AreEqual(_resolver.AllBlocks, blocks);
    _mock.Verify(x => x.SearchBlocks(It.IsAny<AssetLocation>()), Times.Never);
  }

  [TestMethod]
  public void SearchAllItems() {
    IReadOnlyList<Item> items =
        _resolver.GetMatchingItems(new AssetLocation("*", "*"));
    Assert.AreEqual(_resolver.AllItems, items);
    _mock.Verify(x => x.SearchItems(It.IsAny<AssetLocation>()), Times.Never);
  }
}
