using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Gourmand;

public class ClientCommands {
  private readonly ICoreClientAPI _capi;
  public ClientCommands(ICoreClientAPI capi) {
    _capi = capi;
    Register();
  }

  private void Register() {
    IChatCommand gourmand = _capi.ChatCommands.GetOrCreate("gourmand")
                                .RequiresPrivilege(Privilege.chat);
    gourmand.BeginSub("stats")
        .WithDesc("List the total points, and points per category.")
        .WithArgs(_capi.ChatCommands.Parsers.OptionalInt("max", 5))
        .HandleWith(FindModData(GetStats))
        .EndSub()
        .BeginSub("lost")
        .WithDesc("List foods that were previously achieved but then lost " +
                  "due to a death.")
        .WithArgs(_capi.ChatCommands.Parsers.OptionalInt("max", 5))
        .HandleWith(FindModData(GetLost))
        .EndSub()
        .BeginSub("missing")
        .WithDesc("List foods within the specified category that have not " +
                  "been achieved yet.")
        .WithArgs(_capi.ChatCommands.Parsers.Word("category"),
                  _capi.ChatCommands.Parsers.OptionalInt("max", 5))
        .HandleWith(FindModData(GetMissing))
        .EndSub()
        .BeginSub("chargui")
        .HandleWith(CharGui)
        .EndSub();
  }

  private TextCommandResult CharGui(TextCommandCallingArgs args) {
    (_capi.Gui.LoadedGuis.Find(dlg => dlg is GuiDialogCharacterBase)
         as GuiDialogCharacterBase)
        ?.Toggle();
    return TextCommandResult.Success("toggled");
  }

  private OnCommandDelegate
  FindModData(System.Func<ITreeAttribute, TextCommandCallingArgs,
                          TextCommandResult> target) {
    TextCommandResult Invoke(TextCommandCallingArgs args) {
      Entity targetEntity = args.Caller.Entity;
      ITreeAttribute modData = FoodAchievements.GetModData(targetEntity);
      return target(modData, args);
    }
    return Invoke;
  }

  private TextCommandResult GetStats(ITreeAttribute modData,
                                     TextCommandCallingArgs args) {
    FoodAchievements foodAchievements =
        _capi.ModLoader.GetModSystem<GourmandSystem>().FoodAchievements;
    int points =
        foodAchievements.GetPointsForAchievements(_capi.Logger, modData);
    float health = foodAchievements.GetHealthFunctionPiece(
        points, out float gainRate, out int untilPoints);
    if (gainRate != 0) {
      gainRate = 1 / gainRate;
    }
    HashSet<ItemStack> lost =
        new(FoodAchievements.GetLost(_capi.World, modData),
            new ItemStackComparer(_capi.World,
                                  GlobalConstants.IgnoredStackAttributes));
    Dictionary<AssetLocation, Tuple<int, AchievementPoints>> achievements =
        foodAchievements.GetAchievementStats(modData);
    StringBuilder result = new();
    result.AppendLine("Gourmand stats:");
    result.AppendLine($"  Earned points: {points}");
    result.AppendLine($"  Lost foods from death: {lost.Count}");
    result.AppendLine($"  Earned health: {health:F2}");
    result.AppendLine($"  Points necessary for next health: {gainRate:F2}");
    result.AppendLine($"  Until points: {untilPoints}");
    foreach (var category in achievements) {
      result.AppendLine();
      result.AppendLine($"  Food category: {category.Key.ToString()}");
      if (category.Value.Item2.BonusAt != 0) {
        result.AppendLine(
            $"    Eaten foods: {category.Value.Item1}/{category.Value.Item2.BonusAt}");
      } else {
        result.AppendLine($"    Eaten foods: {category.Value.Item1}");
      }
      result.AppendLine($"    Points per food: {category.Value.Item2.Points}");
      result.AppendLine($"    Completion Bonus: {category.Value.Item2.Bonus}");
    }
    return TextCommandResult.Success(result.ToString());
  }

  private string GetFoodName(ItemStack stack) {
    StringBuilder builder = new();
    builder.AppendLine(stack.Collectible.GetHeldItemName(stack));
    DummySlot slot = new(stack);
    stack.Collectible.GetHeldItemInfo(slot, builder, _capi.World, false);
    return builder.ToString();
  }

  private TextCommandResult GetLost(ITreeAttribute modData,
                                    TextCommandCallingArgs args) {
    StringBuilder result = new();
    int max = (int)args[0];
    HashSet<ItemStack> lost = new(new ItemStackComparer(_capi.World));
    foreach (ItemStack stack in FoodAchievements.GetLost(_capi.World,
                                                         modData)) {
      if (lost.Count >= max) {
        break;
      }
      if (!lost.Add(stack)) {
        // This item was already given for another category.
        continue;
      }
      result.Append("Lost: ");
      result.AppendLine(GetFoodName(stack));
    }
    result.Append($"Found {lost.Count} lost foods(s)");
    return TextCommandResult.Success(result.ToString());
  }

  private TextCommandResult GetMissing(ITreeAttribute modData,
                                       TextCommandCallingArgs args) {
    GourmandSystem gourmand = _capi.ModLoader.GetModSystem<GourmandSystem>();
    FoodAchievements foodAchievements = gourmand.FoodAchievements;
    StringBuilder result = new();
    AssetLocation category = new((string)args[0]);
    int max = (int)args[1];
    int found = 0;
    foreach (ItemStack stack in foodAchievements
                 .GetMissing(_capi.World, gourmand.CatDict, category, modData)
                 .Take(max)) {
      ++found;
      result.Append("Missing: ");
      result.AppendLine(GetFoodName(stack));
    }
    result.Append($"Found {found} missing food(s) in category");
    return TextCommandResult.Success(result.ToString());
  }
}
