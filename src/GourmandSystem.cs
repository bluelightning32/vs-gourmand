using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Gourmand.Blocks;
using Gourmand.CollectibleBehaviors;
using Gourmand.EntityBehaviors;

using HarmonyLib;

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
  private Harmony _harmony;

  public static string Domain { get; private set; }
  public FoodAchievements FoodAchievements { get; private set; }
  public ServerConfig ServerConfig { get; private set; }
  public CategoryDict CatDict { get; private set; }

  public override double ExecuteOrder() {
    // This this mod built the category dict in the AssetsLoaded stage, this mod
    // had to run at an order greater than 1.0. Now that it builds the category
    // dict in the AssetsFinalize stage, there are no known dependencies.
    return 1.0;
  }

  public override void Start(ICoreAPI api) {
    Domain = Mod.Info.ModID;
    base.Start(api);

    string patchId = Mod.Info.ModID;
    if (!Harmony.HasAnyPatches(patchId)) {
      _harmony = new Harmony(patchId);
      _harmony.PatchAll();
    }

    _api = api;
    CatDict = api.RegisterRecipeRegistry<CategoryDict>("CategoryDict");
    api.RegisterCollectibleBehaviorClass("notifyeaten", typeof(NotifyEaten));
    api.RegisterCollectibleBehaviorClass("showpoints", typeof(ShowPoints));
    api.RegisterEntityBehaviorClass("updatefoodachievements",
                                    typeof(UpdateFoodAchievements));
    api.RegisterBlockClass(nameof(NotifyingMeal), typeof(NotifyingMeal));
    api.RegisterBlockClass(nameof(NotifyingPie), typeof(NotifyingPie));
  }

  public override void StartClientSide(ICoreClientAPI capi) {
    base.StartClientSide(capi);
    _ = new ClientCommands(capi);
    _ = new Gui.PlayerStatsDialog(capi);
    _ = new Gui.GourmandTab(capi);
  }

  public override void StartServerSide(ICoreServerAPI sapi) {
    base.StartServerSide(sapi);
    LoadConfigFile(sapi);
    _ = new ServerCommands(sapi);
  }

  public override void AssetsLoaded(ICoreAPI api) { base.AssetsLoaded(api); }

  public override void AssetsFinalize(ICoreAPI api) {
    base.AssetsFinalize(api);

    FoodAchievements = api.Assets
                           .Get(new AssetLocation(
                               Mod.Info.ModID, "config/food-achievements.json"))
                           .ToObject<FoodAchievements>();
    FoodAchievements.Resolve(Mod.Info.ModID, api.ModLoader);
    Mod.Logger.Debug("Loaded {0} food achievements",
                     FoodAchievements.RawAchievements.Count);

    if (api is ICoreServerAPI sapi) {
      // Other mods, such as "ACulinaryArtillery" and "Hydrate Or Diedrate" add
      // meal recipes in the AssetsLoaded stage. So load the categories here
      // after they are done with their modifications.
      LoadCategories(sapi);

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
    if (api is ICoreClientAPI capi) {
      foreach (CollectibleObject c in capi.World.Collectibles) {
        if (ShouldAddShowPointsBehavior(c)) {
          c.CollectibleBehaviors =
              c.CollectibleBehaviors.Append(new ShowPoints(c));
        }
      }
    }
  }

  private void LoadConfigFile(ICoreServerAPI api) {
    string configFile = $"{Domain}.json";
    try {
      ServerConfig = api.LoadModConfig<ServerConfig>(configFile);
    } catch (Exception e) {
      api.Logger.Fatal("Error parsing '{0}': {1}", configFile, e.Message);
      throw;
    }
    if (ServerConfig == null) {
      // The file doesn't exist. So create it.
      ServerConfig = new();
      api.StoreModConfig(ServerConfig, configFile);
    }
  }

  private static bool ShouldAddShowPointsBehavior(CollectibleObject c) {
    // The second condition handles containers with edible liquids.
    return c.NutritionProps != null ||
           c.GetType().GetMethod("GetNutritionProperties").DeclaringType !=
               typeof(CollectibleObject);
  }

  private void LoadCategories(ICoreServerAPI sapi) {
    Stopwatch stopwatch = new();
    stopwatch.Start();
    var collectibleFiles =
        sapi.Assets.GetMany<JToken>(Mod.Logger, "recipes/matchers/collectible");
    List<Collectibles.MatchRule> collectibleRules = new();
    foreach (var matcher in collectibleFiles) {
      try {
        collectibleRules.AddRange(LoadMaybeArray<Collectibles.MatchRule>(
            matcher.Key.Domain, matcher.Value));
      } catch (Exception) {
        Mod.Logger.Error("Gourmand: error parsing {0}", matcher.Key);
        throw;
      }
    }

    var stackFiles =
        sapi.Assets.GetMany<JToken>(Mod.Logger, "recipes/matchers/itemstack");
    List<MatchRule> stackRules = new();
    foreach (var matcher in stackFiles) {
      try {
        stackRules.AddRange(
            LoadMaybeArray<MatchRule>(matcher.Key.Domain, matcher.Value));
      } catch (Exception) {
        Mod.Logger.Error("Gourmand: error parsing {0}", matcher.Key);
        throw;
      }
    }

    CatDict.Set(sapi.World, Mod.Logger, collectibleRules, stackRules);
    stopwatch.Stop();
    Mod.Logger.Debug(
        "Loaded {0} collectible matchers and {1} item stack matchers in {2}.",
        collectibleRules.Count, stackRules.Count, stopwatch.Elapsed);
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

  public override void Dispose() {
    base.Dispose();

    _harmony?.UnpatchAll(_harmony.Id);
  }
}
