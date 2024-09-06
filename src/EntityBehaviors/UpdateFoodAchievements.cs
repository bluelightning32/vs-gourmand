using System.Collections.Generic;

using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Gourmand.EntityBehaviors;

class UpdateFoodAchievements : EntityBehavior {
  private ItemStack _eating = null;

  public UpdateFoodAchievements(Entity entity) : base(entity) {}

  public override string PropertyName() { return "updateachievements"; }

  public void SetCurrentFood(ItemStack food) {
    entity.Api.Logger.Debug("Set current food to {0}",
                            food?.Collectible?.Code.ToString() ?? "none");
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
    entity.Api.Logger.Debug("Ate food {0}", food?.Collectible?.Code.ToString());
    GourmandSystem gourmand = GetGourmandSystem();
    ItemStack clonedFood = food.Clone();
    clonedFood.StackSize = 1;
    int newPoints = gourmand.FoodAchievements.AddAchievements(
        entity.Api.World, gourmand.CatDict, GetModData(), clonedFood);
    if (newPoints > 0) {
      MarkDirty();
      IServerPlayer player = (IServerPlayer)((EntityPlayer)entity).Player;
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
      MarkDirty();
    }
    return removedPoints;
  }

  public void Clear() {
    FoodAchievements.ClearAchievements(GetModData());
    MarkDirty();
  }

  public IEnumerable<ItemStack> GetLost() {
    return FoodAchievements.GetLost(entity.Api.World, GetModData());
  }

  public ITreeAttribute GetModData() {
    return FoodAchievements.GetModData(entity);
  }

  public void MarkDirty() {
    entity.WatchedAttributes.MarkPathDirty(FoodAchievements.ModDataPath);
  }

  public GourmandSystem GetGourmandSystem() {
    return entity.Api.ModLoader.GetModSystem<GourmandSystem>();
  }

  public IEnumerable<ItemStack> GetMissing(AssetLocation category) {
    GourmandSystem gourmand = GetGourmandSystem();
    return gourmand.FoodAchievements.GetMissing(
        entity.Api.World, gourmand.CatDict, category, GetModData());
  }
}
