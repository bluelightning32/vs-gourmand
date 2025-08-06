using Microsoft.VisualStudio.TestTools.UnitTesting;

using PrefixClassName.MsTest;

using Vintagestory.API.Common;

namespace Gourmand.Test;

using Real = Gourmand;

[PrefixTestClass]
public class ContentBuilder {
  [TestMethod]
  public void PushValueEmpty() {
    Real.ContentBuilder builder = new(Array.Empty<ItemStack>());
    ItemStack pineapple = new(LoadAssets.GetItem("game", "fruit-pineapple"));
    Assert.AreEqual(-1, builder.HighestUsed);
    Assert.IsTrue(builder.PushValue(pineapple, 0, 5));
    Assert.AreEqual(0, builder.HighestUsed);
    Assert.AreEqual(1, builder.Contents.Count);
    Assert.AreEqual(pineapple, builder.Contents[0]);
  }

  [TestMethod]
  public void PushValueSkipFull() {
    ItemStack pineapple = new(LoadAssets.GetItem("game", "fruit-pineapple"));
    ItemStack cranberry = new(LoadAssets.GetItem("game", "fruit-cranberry"));
    ItemStack[] initial = new[] { pineapple, null, pineapple };
    Real.ContentBuilder builder = new(initial);
    Assert.AreEqual(2, builder.HighestUsed);
    Assert.IsTrue(builder.PushValue(cranberry, 0, 5));
    Assert.AreEqual(3, builder.Contents.Count);
    Assert.AreEqual(cranberry, builder.Contents[1]);
    Assert.AreEqual(2, builder.HighestUsed);

    Assert.IsFalse(builder.PushValue(cranberry, 0, 3));
  }

  [TestMethod]
  public void PushPopPush() {
    Real.ContentBuilder builder = new(Array.Empty<ItemStack>());
    ItemStack pineapple = new(LoadAssets.GetItem("game", "fruit-pineapple"));
    Assert.IsTrue(builder.PushValue(pineapple, 0, 5));
    Assert.AreEqual(0, builder.HighestUsed);
    Assert.AreEqual(pineapple, builder.PopValue());
    Assert.AreEqual(-1, builder.HighestUsed);
    // The count is still 1 after removing the last item, because removed slots
    // are replaced with null.
    Assert.AreEqual(1, builder.Contents.Count);
    Assert.IsNull(builder.Contents[0]);

    Assert.IsTrue(builder.PushValue(pineapple, 0, 5));
    Assert.AreEqual(1, builder.Contents.Count);
    Assert.AreEqual(pineapple, builder.Contents[0]);
    Assert.AreEqual(0, builder.HighestUsed);
  }

  [TestMethod]
  public void PushPopPushStartNonEmpty() {
    ItemStack pineapple = new(LoadAssets.GetItem("game", "fruit-pineapple"));
    ItemStack cranberry = new(LoadAssets.GetItem("game", "fruit-cranberry"));
    ItemStack[] initial = new[] { pineapple, null, pineapple };
    Real.ContentBuilder builder = new(initial);

    Assert.IsTrue(builder.PushValue(cranberry, 0, 5));
    Assert.AreEqual(cranberry, builder.PopValue());
    Assert.AreEqual(2, builder.HighestUsed);
    Assert.IsNull(builder.Contents[1]);

    Assert.IsTrue(builder.PushValue(cranberry, 0, 5));
    Assert.AreEqual(cranberry, builder.Contents[1]);
    Assert.AreEqual(2, builder.HighestUsed);
  }

  [TestMethod]
  public void SetMinSlots() {
    ItemStack pineapple = new(LoadAssets.GetItem("game", "fruit-pineapple"));
    Real.ContentBuilder builder = new(Array.Empty<ItemStack>());
    Block bowl = LoadAssets.GetBlock("game", "bowl-blue-meal");
    Assert.IsNotNull(bowl);
    builder.Set(LoadAssets.Server, new ItemStack(bowl), 3);
    Assert.IsTrue(builder.PushValue(pineapple, 0, 5));

    var contents = Real.ContentBuilder.GetContents(
        LoadAssets.Server, builder.GetItemStack(LoadAssets.Server));
    Assert.HasCount(3, contents);
    Assert.Contains(pineapple, contents);
  }
}
