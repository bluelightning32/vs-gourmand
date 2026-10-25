using System.Reflection;

using PrefixClassName.MsTest;

using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.Server;

namespace Gourmand.Test.EntityBehaviors;

using Real = Gourmand;

[PrefixTestClass]
public class UpdateFoodAchievements {
  static UpdateFoodAchievements() {
    Real.GourmandSystem gourmand =
        LoadAssets.ServerApi.ModLoader.GetModSystem<Real.GourmandSystem>();
    gourmand.StartServerSide(LoadAssets.ServerApi);

    ServerWorldPlayerData fakeData =
        ServerWorldPlayerData.CreateNew("fake-name", "fake-uid");
    ServerPlayer player = new(LoadAssets.Server, fakeData);
    ConnectedClient client =
        new(1) { Player = player, FallbackPlayerName = "fake-name" };

    FieldInfo clientField = player.GetType().GetField(
        "client", BindingFlags.Instance | BindingFlags.NonPublic);
    clientField.SetValue(player, client);
    LoadAssets.Server.PlayersByUid.Add("fake-uid", player);
  }

  private EntityPlayer CreateFakePlayer() {
    EntityPlayer player = new() { Api = LoadAssets.Server.Api,
                                  World = LoadAssets.Server, Code = "player" };
    player.WatchedAttributes.SetString("playerUID", "fake-uid");
    IServerPlayer serverPlayer = (IServerPlayer)player.Player;
    Assert.IsNotNull(serverPlayer.PlayerName);

    Real.EntityBehaviors.UpdateFoodAchievements behavior = new(player);
    EntityBehaviorHealth health = new(player);

    EntityProperties properties =
        new() { Server = new([], []), Client = new([], []) };
    player.Initialize(properties, LoadAssets.ServerApi, 0);
    player.SidedProperties.Behaviors = [behavior, health];
    health.Initialize(properties, JsonObject.FromJson("{}"));

    behavior.AfterInitialized(true);
    return player;
  }

  [TestMethod]
  public void ClearUnknownLost() {
    Item pineapple = LoadAssets.GetItem("game", "fruit-pineapple");
    Item firestarter = LoadAssets.GetItem("game", "firestarter");

    EntityPlayer player = CreateFakePlayer();
    Real.EntityBehaviors.UpdateFoodAchievements behavior =
        player.GetBehavior<Real.EntityBehaviors.UpdateFoodAchievements>();

    int firstFruitGained = behavior.OnFoodEaten(new ItemStack(pineapple));
    Assert.IsGreaterThan(0, firstFruitGained);

    ITreeAttribute moddata = behavior.GetModData();
    ITreeAttribute achieved = moddata.GetOrAddTreeAttribute("achieved");
    ITreeAttribute lost = moddata.GetOrAddTreeAttribute("lost");
    string achievementName = achieved.First().Key;
    ITreeAttribute lostAchievement =
        lost.GetOrAddTreeAttribute(achievementName);

    ItemStack firestarterStack = new(firestarter);
    lostAchievement.SetItemstack("gourmand:badcategoryvalue", firestarterStack);

    int called = 0;
    player.WatchedAttributes.RegisterModifiedListener(
        Real.FoodAchievements.ModDataPath, () => called++);

    int removed = behavior.ClearUnknownLost();
    Assert.AreEqual(1, removed);
    Assert.AreEqual(1, called);
  }
}
