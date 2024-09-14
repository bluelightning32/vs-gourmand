using System;
using System.Collections.Generic;
using System.Linq;

using Gourmand.Blocks;
using Gourmand.CollectibleBehaviors;
using Gourmand.EntityBehaviors;

using Newtonsoft.Json.Linq;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Gourmand;

public class GourmandSystem : ModSystem {
  private ICoreAPI _api;
  public FoodAchievements FoodAchievements { get; private set; }

  public CategoryDict CatDict { get; private set; }

  public override double ExecuteOrder() {
    // Use the same load order as the grid recipes loader.
    return 1;
  }

  public override void Start(ICoreAPI api) {
    base.Start(api);
    _api = api;
    CatDict = api.RegisterRecipeRegistry<CategoryDict>("CategoryDict");
    api.RegisterCollectibleBehaviorClass("notifyeaten", typeof(NotifyEaten));
    api.RegisterEntityBehaviorClass("updatefoodachievements",
                                    typeof(UpdateFoodAchievements));
    api.RegisterBlockClass(nameof(NotifyingMeal), typeof(NotifyingMeal));
    api.RegisterBlockClass(nameof(NotifyingPie), typeof(NotifyingPie));
  }

  public override void StartClientSide(ICoreClientAPI capi) {
    base.StartClientSide(capi);
    _ = new ClientCommands(capi);
    _ = new PlayerStatsDialog(capi);
    _ = new GourmandTab(capi);
  }

  public override void StartServerSide(ICoreServerAPI sapi) {
    base.StartServerSide(sapi);
    _ = new ServerCommands(sapi);
  }

  public override void AssetsLoaded(ICoreAPI api) {
    base.AssetsLoaded(api);
    if (api is ICoreServerAPI sapi) {
      LoadCategories(sapi);
    }
  }

  public override void AssetsFinalize(ICoreAPI api) {
    base.AssetsFinalize(api);
    FoodAchievements = api.Assets
                           .Get(new AssetLocation(
                               Mod.Info.ModID, "config/food-achievements.json"))
                           .ToObject<FoodAchievements>();
    FoodAchievements.Resolve(Mod.Info.ModID);
    api.Logger.Debug("Loaded {0} food achievements",
                     FoodAchievements.RawAchievements.Count);

    if (api is ICoreServerAPI sapi) {
      foreach (CollectibleObject c in sapi.World.Collectibles) {
        if (c.NutritionProps != null ||
            c.GetType().GetMethod("GetNutritionProperties").DeclaringType !=
                typeof(CollectibleObject)) {
          c.CollectibleBehaviors =
              c.CollectibleBehaviors.Append(new NotifyEaten(c));
        }
      }

      EntityProperties playerProperties =
          sapi.World.GetEntityType(GlobalConstants.EntityPlayerTypeCode);
      playerProperties.Server.BehaviorsAsJsonObj =
          playerProperties.Server.BehaviorsAsJsonObj.Append(
              JsonObject.FromJson("{ code: \"updatefoodachievements\" }"));
    }
  }

  private void LoadCategories(ICoreServerAPI sapi) {
    var collectibleFiles = sapi.Assets.GetMany<JToken>(
        sapi.Server.Logger, "recipes/matchers/collectible");
    List<Collectibles.MatchRule> collectibleRules = new();
    foreach (var matcher in collectibleFiles) {
      try {
        collectibleRules.AddRange(LoadMaybeArray<Collectibles.MatchRule>(
            matcher.Key.Domain, matcher.Value));
      } catch (Exception) {
        sapi.Logger.Error("Gourmand: error parsing {0}", matcher.Key);
        throw;
      }
    }

    var stackFiles = sapi.Assets.GetMany<JToken>(sapi.Server.Logger,
                                                 "recipes/matchers/itemstack");
    List<MatchRule> stackRules = new();
    foreach (var matcher in stackFiles) {
      try {
        stackRules.AddRange(
            LoadMaybeArray<MatchRule>(matcher.Key.Domain, matcher.Value));
      } catch (Exception) {
        sapi.Logger.Error("Gourmand: error parsing {0}", matcher.Key);
        throw;
      }
    }

    CatDict.Set(sapi.World, collectibleRules, stackRules);
    sapi.Logger.Debug(
        "Loaded {0} collectible matchers and {1} item stack matchers.",
        collectibleRules.Count, stackRules.Count);
  }

  private static IEnumerable<T> LoadMaybeArray<T>(string domain, JToken value) {
    if (value is JArray jarray) {
      foreach (JToken token in jarray) {
        yield return token.ToObject<T>(domain);
      }
    } else {
      yield return value.ToObject<T>(domain);
    }
  }
}
