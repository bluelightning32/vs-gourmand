using System.Collections.Generic;

using Newtonsoft.Json.Linq;

using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Gourmand;

public class GourmandSystem : ModSystem {
  private ICoreAPI _api;
  public FoodAchievements FoodAchievements {
    get;
    private set;
  }

  public CategoryDict CatDict { get; private set; }

  public override double ExecuteOrder() {
    // Use the same load order as the grid recipes loader.
    return 1;
  }

  public override void Start(ICoreAPI api) {
    _api = api;
    CatDict = api.RegisterRecipeRegistry<CategoryDict>("CategoryDict");
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
        .Get(new AssetLocation(Mod.Info.ModID, "config/food-achievements.json"))
        .ToObject<FoodAchievements>();
    FoodAchievements.Resolve(Mod.Info.ModID);
    api.Logger.Debug("Loaded {0} food achievements", FoodAchievements.RawAchievements.Count);
  }

  private void LoadCategories(ICoreServerAPI sapi) {
    var collectibleFiles = sapi.Assets.GetMany<JToken>(
        sapi.Server.Logger, "recipes/matchers/collectible");
    List<Collectibles.MatchRule> collectibleRules = new();
    foreach (var matcher in collectibleFiles) {
      collectibleRules.AddRange(LoadMaybeArray<Collectibles.MatchRule>(
          matcher.Key.Domain, matcher.Value));
    }

    var stackFiles = sapi.Assets.GetMany<JToken>(sapi.Server.Logger,
                                                 "recipes/matchers/itemstack");
    List<MatchRule> stackRules = new();
    foreach (var matcher in stackFiles) {
      stackRules.AddRange(
          LoadMaybeArray<MatchRule>(matcher.Key.Domain, matcher.Value));
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
