using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Gourmand.EntityBehaviors;

class UpdateFoodAchievements : EntityBehavior {
  private ItemStack _eating = null;
  private ICoreAPI _api = null;

  public UpdateFoodAchievements(Entity entity) : base(entity) {}

  public override string PropertyName() { return "updateachievements"; }

  public void SetCurrentFood(ICoreAPI api, ItemStack food) {
    api.Logger.Debug("Set current food to {0}",
                     food?.Collectible?.Code.ToString() ?? "none");
    _eating = food;
    _api = api;
  }

  public override void OnEntityReceiveSaturation(
      float saturation, EnumFoodCategory foodCat = EnumFoodCategory.Unknown,
      float saturationLossDelay = 10, float nutritionGainMultiplier = 1) {
    base.OnEntityReceiveSaturation(saturation, foodCat, saturationLossDelay,
                                   nutritionGainMultiplier);
    if (_eating != null) {
      _api.Logger.Debug("Ate food {0}", _eating?.Collectible?.Code.ToString());
      ITreeAttribute achievements =
          entity.WatchedAttributes.GetOrAddTreeAttribute("gourmand");
      GourmandSystem gourmand = _api.ModLoader.GetModSystem<GourmandSystem>();
      int newPoints = gourmand.FoodAchievements.UpdateAchievements(
          _api.World, gourmand.CatDict, achievements, _eating);
      if (newPoints > 0) {
        IServerPlayer player = (IServerPlayer)((EntityPlayer)entity).Player;
        player.SendMessage(GlobalConstants.InfoLogChatGroup,
                           Lang.GetL(player.LanguageCode,
                                     "gourmand:new-food-eaten", newPoints),
                           EnumChatType.Notification);
      }
      _eating = null;
    }
  }
}
