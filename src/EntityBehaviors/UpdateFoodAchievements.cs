using System;
using System.Collections.Generic;

using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Gourmand.EntityBehaviors;

class UpdateFoodAchievements : EntityBehavior {
  private ItemStack _eating = null;
  private EntityBehaviorHealth _healthBehavior = null;

  public UpdateFoodAchievements(Entity entity) : base(entity) {}

  public override string PropertyName() { return "updateachievements"; }

  public override void AfterInitialized(bool onFirstSpawn) {
    base.AfterInitialized(onFirstSpawn);
    _healthBehavior = entity.GetBehavior<EntityBehaviorHealth>();

    GourmandSystem gourmand = GetGourmandSystem();
    MarkDirty(gourmand);
  }

  public void SetCurrentFood(ItemStack food) {
    GourmandSystem gourmand = GetGourmandSystem();
    if (gourmand.ServerConfig.DebugLogging) {
      IServerPlayer player = (IServerPlayer)((EntityPlayer)entity).Player;
      string name;
      try {
        name = food?.GetName() ?? "none";
      } catch (Exception) {
        name = "exception";
      }
      gourmand.Mod.Logger.Audit(
          "{0} set current food to {1} - {2}", player.PlayerName,
          food?.Collectible?.Code.ToString() ?? "none", name);
    }
    _eating = food;
  }

  public override void OnEntityReceiveSaturation(
      float saturation, EnumFoodCategory foodCat = EnumFoodCategory.Unknown,
      float saturationLossDelay = 10, float nutritionGainMultiplier = 1) {
    base.OnEntityReceiveSaturation(saturation, foodCat, saturationLossDelay,
                                   nutritionGainMultiplier);
    if (_eating != null) {
      OnFoodEaten(_eating);
      _eating = null;
    }
  }

  public int OnFoodEaten(ItemStack food) {
    GourmandSystem gourmand = GetGourmandSystem();
    IServerPlayer player = (IServerPlayer)((EntityPlayer)entity).Player;
    if (gourmand.ServerConfig.DebugLogging) {
      gourmand.Mod.Logger.Audit("{0} ate food {1}", player.PlayerName,
                                food?.Collectible?.Code.ToString());
    }
    ItemStack clonedFood = food.Clone();
    clonedFood.StackSize = 1;
    // Prevent the food as showing up as rotten if it is later moved to the lost
    // foods list. The collectible will reset freshHours if createdTotalHours is
    // missing.
    FoodAchievements.ClearFoodCreatedTime(clonedFood);
    int newPoints = gourmand.FoodAchievements.AddAchievements(
        entity.Api.World, gourmand.CatDict, GetModData(), clonedFood);
    if (newPoints > 0) {
      MarkDirty(gourmand);
      player.SendMessage(
          GlobalConstants.InfoLogChatGroup,
          Lang.GetL(player.LanguageCode, "gourmand:new-food-eaten", newPoints),
          EnumChatType.Notification);
    }
    return newPoints;
  }

  public int ClearFood(ItemStack food) {
    GourmandSystem gourmand = GetGourmandSystem();
    ItemStack clonedFood = food.Clone();
    clonedFood.StackSize = 1;
    int removedPoints = gourmand.FoodAchievements.RemoveAchievements(
        entity.Api.World, gourmand.CatDict, GetModData(), clonedFood);
    if (removedPoints > 0) {
      MarkDirty(gourmand);
    }
    return removedPoints;
  }

  public void Clear() {
    GourmandSystem gourmand = GetGourmandSystem();
    FoodAchievements.ClearAchievements(GetModData());
    MarkDirty(gourmand);
  }

  public IEnumerable<ItemStack> GetLost() {
    return FoodAchievements.GetLost(entity.Api.World, GetModData());
  }

  public ITreeAttribute GetModData() {
    return FoodAchievements.GetModData(entity);
  }

  public void MarkDirty(GourmandSystem gourmand) {
    entity.WatchedAttributes.MarkPathDirty(FoodAchievements.ModDataPath);
    int points = gourmand.FoodAchievements.GetPointsForAchievements(
        gourmand.Mod.Logger, GetModData());
    float health = gourmand.FoodAchievements.GetHealthForPoints(points);
    _healthBehavior.SetMaxHealthModifiers("gourmand", health);
    gourmand.Mod.Logger.Debug(
        $"Set extra health to {health} for {entity.GetName()}");
  }

  public GourmandSystem GetGourmandSystem() {
    return entity.Api.ModLoader.GetModSystem<GourmandSystem>();
  }

  public IEnumerable<ItemStack> GetMissing(AssetLocation category) {
    GourmandSystem gourmand = GetGourmandSystem();
    return gourmand.FoodAchievements.GetMissing(
        entity.Api.World, gourmand.CatDict, category, GetModData());
  }

  // Override OnEntityDeath instead of onEntityRevive, because in the Unconcious
  // mod, players can be revived without dying.
  public override void OnEntityDeath(DamageSource source) {
    float deathPenalty =
        entity.Api.World.Config.GetFloat("gourmandDeathPenalty", 0.3f);
    float deathPenaltyMax =
        entity.Api.World.Config.GetFloat("gourmandDeathPenaltyMax", 0.5f);
    GourmandSystem gourmand = GetGourmandSystem();
    int removedPoints = gourmand.FoodAchievements.ApplyDeath(
        gourmand.Mod.Logger, entity.GetName(), entity.Api.World,
        gourmand.CatDict, GetModData(), deathPenalty, deathPenaltyMax);
    if (removedPoints > 0) {
      MarkDirty(gourmand);
    }
    base.OnEntityDeath(source);
  }
}
