using System.Collections.Generic;
using System.Reflection;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Gourmand.Gui;

public class PlayerStatsDialog {
  private readonly ICoreClientAPI _capi;
  private readonly GuiDialogCharacterBase _dialog;
  /// <summary>
  /// Reflection info for the GuiComposer.staticElements field. Despite the
  /// name, this field has a list of all of the static and interactive elements
  /// in the composer.
  /// </summary>
  private readonly FieldInfo _staticElementsField;

  public PlayerStatsDialog(ICoreClientAPI capi) {
    _capi = capi;
    _dialog = _capi.Gui.LoadedGuis.Find(dlg => dlg is GuiDialogCharacterBase)
                  as GuiDialogCharacterBase;
    _dialog.OnOpened += OnOpened;
    _dialog.OnClosed += OnClosed;
    _dialog.ComposeExtraGuis += Compose;
    _staticElementsField =
        typeof(GuiComposer)
            .GetField("staticElements",
                      BindingFlags.Instance | BindingFlags.NonPublic);
  }

  private void OnClosed() {
    EntityPlayer entity = _capi.World.Player.Entity;
    entity.WatchedAttributes.UnregisterListener(UpdateStatBars);
  }

  private void OnOpened() {
    EntityPlayer entity = _capi.World.Player.Entity;
    entity.WatchedAttributes.RegisterModifiedListener(
        FoodAchievements.ModDataPath, UpdateStatBars);
  }

  private void Compose() {
    GuiComposer playerstats = _dialog.Composers["playerstats"];
    Dictionary<string, GuiElement> staticElements =
        (Dictionary<string, GuiElement>)_staticElementsField.GetValue(
            playerstats);

    ElementBounds secondToLastBar = playerstats.GetStatbar("proteinBar").Bounds;
    ElementBounds lastBar = playerstats.GetStatbar("dairyBar").Bounds;
    double barSpacingY = lastBar.fixedY - secondToLastBar.fixedY;

    GuiElementStaticText lastText = FindStaticTextElement(
        staticElements.Values, "playerinfo-nutrition-Dairy");

    ShiftElementsDown(staticElements.Values,
                      lastBar.fixedY + lastBar.OuterHeight, barSpacingY);

    // Composed needs to be set to false before adding the new elements,
    // otherwise the methods (such as AddStaticText) turn into no-ops.
    playerstats.Composed = false;
    ElementBounds textBounds = lastText.Bounds.BelowCopy();
    playerstats.AddStaticText(
        Lang.Get("gourmand:playerinfo-nutrition-Gourmand"), lastText.Font,
        textBounds);
    playerstats.AddStatbar(lastBar.CopyOffsetedSibling(0, barSpacingY),
                           GuiStyle.HealthBarColor, "gourmandBar");
    playerstats.Compose();

    UpdateStatBars();
  }

  private void UpdateStatBars() {
    EntityPlayer entity = _capi.World.Player.Entity;
    ITreeAttribute modData = FoodAchievements.GetModData(entity);

    FoodAchievements foodAchievements =
        _capi.ModLoader.GetModSystem<GourmandSystem>().FoodAchievements;
    int points =
        foodAchievements.GetPointsForAchievements(_capi.Logger, modData);
    float health = foodAchievements.GetHealthFunctionPiece(
        points, out float gainRate, out int untilPoints);

    GuiComposer playerstats = _dialog.Composers["playerstats"];
    GuiElementStatbar bar = playerstats.GetStatbar("gourmandBar");
    GourmandSystem gourmand = _capi.ModLoader.GetModSystem<GourmandSystem>();
    float max = gourmand.FoodAchievements.HealthPoints[^1].Health;
    bar.SetLineInterval(max / 10);
    bar.SetValues(health, 0, max);
  }

  private static GuiElementStaticText
  FindStaticTextElement(IEnumerable<GuiElement> elements, string langEntry) {
    string text = Lang.Get(langEntry);
    foreach (GuiElement e in elements) {
      if (e is GuiElementStaticText s) {
        if (s.Text == text) {
          return s;
        }
      }
    }
    return null;
  }

  private void ShiftElementsDown(IEnumerable<GuiElement> elements,
                                 double onlyBelow, double shiftY) {
    int shifted = 0;
    foreach (GuiElement element in elements) {
      if (element.Bounds.fixedY > onlyBelow) {
        element.Bounds.fixedY += shiftY;
        ++shifted;
      }
    }
    _capi.Logger.Debug("Shifted {0}", shifted);
  }
}
