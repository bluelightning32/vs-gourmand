using System.Collections.Generic;
using System.Linq;

using Gourmand.EntityBehaviors;

using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace Gourmand;

public class ServerCommands {
  private readonly ICoreServerAPI _sapi;
  public ServerCommands(ICoreServerAPI sapi) {
    _sapi = sapi;
    Register();
  }

  private void Register() {
    IChatCommand player = _sapi.ChatCommands.Get("player");
    if (player == null) {
      _sapi.Logger.Error("The gourmand subcommands cannot be registered " +
                         "because the 'player' command is missing.");
      return;
    }
    player.BeginSubCommand("gourmand")
        .WithDesc("Debug commands for the Gourmand mod")
        .RequiresPrivilege(Privilege.gamemode)
        .BeginSubCommand("grantheld")
        .WithDesc("Grant the achievements of the food held by the caller to " +
                  "all target players.")
        .HandleWith((TextCommandCallingArgs args) =>
                        CmdPlayer.Each(args, FindBehavior(GrantHeld)))
        .EndSub()
        .BeginSub("clearheld")
        .WithDesc("Clear the achievements of the food held by the caller to " +
                  "all target players. The food is marked as a lost food.")
        .HandleWith((TextCommandCallingArgs args) =>
                        CmdPlayer.Each(args, FindBehavior(ClearHeld)))
        .EndSub()
        .BeginSub("clear")
        .WithDesc(
            "Clear all achievements and lost foods of the target players.")
        .HandleWith((TextCommandCallingArgs args) =>
                        CmdPlayer.Each(args, FindBehavior(Clear)))
        .EndSub()
        .BeginSub("givelost")
        .WithDesc(
            "Gives foods with the lost values of the target to the caller.")
        .WithArgs(_sapi.ChatCommands.Parsers.OptionalInt("max", 5))
        .HandleWith((TextCommandCallingArgs args) =>
                        CmdPlayer.Each(args, FindBehavior(GiveLost)))
        .EndSub()
        .BeginSub("givemissing")
        .WithDesc("Gives foods to the caller within the specified category " +
                  "that the target player has not achieved yet.")
        .WithArgs(_sapi.ChatCommands.Parsers.Word("category"),
                  _sapi.ChatCommands.Parsers.OptionalInt("max", 5))
        .HandleWith((TextCommandCallingArgs args) =>
                        CmdPlayer.Each(args, FindBehavior(GiveMissing)))
        .EndSub()
        .BeginSub("grantmissing")
        .WithDesc("Grants achievements to the caller for foods within the specified category " +
                  "that the target player has not achieved yet.")
        .WithArgs(_sapi.ChatCommands.Parsers.Word("category"),
                  _sapi.ChatCommands.Parsers.OptionalInt("max", 5))
        .HandleWith((TextCommandCallingArgs args) =>
                        CmdPlayer.Each(args, FindBehavior(GrantMissing)))
        .EndSub();
  }

  /// <summary>
  /// If the target user is different from the caller, then require
  /// controlserver privileges to run the command.
  /// </summary>
  /// <param name="target">delegate to this if the privileges are
  /// sufficient</param> <returns>result of the target, or a privileges error
  /// message</returns>
  private CmdPlayer.PlayerEachDelegate
  FindBehavior(System.Func<UpdateFoodAchievements, TextCommandCallingArgs,
                           TextCommandResult> target) {
    TextCommandResult Invoke(PlayerUidName targetPlayer,
                             TextCommandCallingArgs args) {
      Entity targetEntity = args.Caller.Entity;
      if (targetPlayer != args.Caller.Player) {
        if (!args.Caller.HasPrivilege(Privilege.controlserver)) {
          return TextCommandResult.Error(
              Lang.Get(
                  "Sorry, you don't have the privilege to use this command"),
              "noprivilege");
        }
        if (_sapi.World.PlayerByUid(targetPlayer.Uid)
                is not IServerPlayer target) {
          return TextCommandResult.Error(Lang.Get("Target must be online"));
        }
        targetEntity = target.Entity;
      }
      UpdateFoodAchievements behavior =
          targetEntity.GetBehavior<UpdateFoodAchievements>();
      return target(behavior, args);
    }
    return Invoke;
  }

  private TextCommandResult GrantHeld(UpdateFoodAchievements target,
                                      TextCommandCallingArgs args) {
    ItemStack held = args.Caller.Player.Entity.ActiveHandItemSlot.Itemstack;
    if (held == null) {
      return TextCommandResult.Error(Lang.Get("Nothing held"));
    }
    int pointsAdded = target.OnFoodEaten(held);
    return TextCommandResult.Success("Points added " + pointsAdded);
  }

  private TextCommandResult ClearHeld(UpdateFoodAchievements target,
                                      TextCommandCallingArgs args) {
    ItemStack held = args.Caller.Player.Entity.ActiveHandItemSlot.Itemstack;
    if (held == null) {
      return TextCommandResult.Error(Lang.Get("Nothing held"));
    }
    int pointsRemoved = target.ClearFood(held);
    return TextCommandResult.Success("Points removed " + pointsRemoved);
  }

  private TextCommandResult Clear(UpdateFoodAchievements target,
                                  TextCommandCallingArgs args) {
    target.Clear();
    return TextCommandResult.Success("Achievements reset");
  }

  private TextCommandResult GiveLost(UpdateFoodAchievements from,
                                     TextCommandCallingArgs args) {
    int max = (int)args[1];
    HashSet<ItemStack> gave = new(new ItemStackComparer(
        _sapi.World, GlobalConstants.IgnoredStackAttributes));
    foreach (ItemStack stack in from.GetLost()) {
      if (gave.Count >= max) {
        break;
      }
      if (!gave.Add(stack)) {
        // This item was already given for another category.
        continue;
      }
      if (!args.Caller.Entity.TryGiveItemStack(stack.Clone())) {
        return TextCommandResult.Error(Lang.Get("Failed to give all stacks"));
      }
    }
    return TextCommandResult.Success($"Gave {gave.Count} stack(s)");
  }

  private TextCommandResult GiveMissing(UpdateFoodAchievements from,
                                        TextCommandCallingArgs args) {
    AssetLocation category = new((string)args[1]);
    int max = (int)args[2];
    int gave = 0;
    foreach (ItemStack stack in from.GetMissing(category).Take(max)) {
      ++gave;
      if (!args.Caller.Entity.TryGiveItemStack(stack.Clone())) {
        return TextCommandResult.Error(Lang.Get("Failed to give all stacks"));
      }
    }
    return TextCommandResult.Success($"Gave {gave} stack(s)");
  }

  private TextCommandResult GrantMissing(UpdateFoodAchievements from,
                                        TextCommandCallingArgs args) {
    AssetLocation category = new((string)args[1]);
    int max = (int)args[2];
    int gavePoints = 0;
    foreach (ItemStack stack in from.GetMissing(category).Take(max)) {
      gavePoints += from.OnFoodEaten(stack);
    }
    return TextCommandResult.Success($"Granted {gavePoints} point(s)");
  }
}
