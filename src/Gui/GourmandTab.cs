using System;
using System.Collections.Generic;
using System.Text;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Gourmand.Gui;

public class GourmandTab {
  private class HeaderLine {
    public string Description;
    public string Value;
    public double Width = 0;

    public HeaderLine(string description, float value) {
      Description = description;
      Value = value.ToString();
    }

    public HeaderLine(string description, float value, string format) {
      Description = description;
      Value = value.ToString(format);
    }
  }
  private readonly ICoreClientAPI _capi;
  private readonly GuiDialogCharacterBase _dialog;
  // The parent's padding is too big. Grow 3 units into the parent's padding
  // in all directions.
  private readonly float _shrinkParentPadding = 3;
  private readonly double _itemSize = 40;
  private readonly CairoFont _headerFont = CairoFont.WhiteSmallishText();

  public GourmandTab(ICoreClientAPI capi) {
    _capi = capi;
    _dialog = _capi.Gui.LoadedGuis.Find(dlg => dlg is GuiDialogCharacterBase)
                  as GuiDialogCharacterBase;
    int tabIndex = _dialog.Tabs.Count;
    _dialog.Tabs.Add(
        new GuiTab() { Name = Lang.Get("gourmand:tabname-gourmand"),
                       DataInt = tabIndex });
    _dialog.RenderTabHandlers.Add(ComposeTab);
  }

  private void ComposeTab(GuiComposer composer) {
    ElementBounds dialogBounds = composer.CurParentBounds;
    float scrollWidth = 20;
    // The parent bounds were not offset by the dialog title, so do that now.
    // Also make room for the scroll bar to the right.
    ElementBounds clipBounds = ElementBounds.Fixed(
        EnumDialogArea.None, -_shrinkParentPadding,
        GuiStyle.TitleBarHeight - _shrinkParentPadding,
        dialogBounds.fixedWidth + 2 * _shrinkParentPadding - scrollWidth,
        dialogBounds.fixedHeight + 2 * _shrinkParentPadding -
            GuiStyle.TitleBarHeight);
    // The inset takes up the entire clip bounds.
    ElementBounds insetBounds =
        ElementBounds.FixedSize(clipBounds.fixedWidth, clipBounds.fixedHeight);
    // The text also takes up the entire clip bounds.
    ElementBounds textBounds =
        ElementBounds.FixedSize(clipBounds.fixedWidth - _shrinkParentPadding,
                                clipBounds.fixedHeight - _shrinkParentPadding);
    textBounds.WithFixedOffset(_shrinkParentPadding, _shrinkParentPadding);
    ElementBounds scrollBounds =
        clipBounds.RightCopy().WithFixedWidth(scrollWidth);

    composer.BeginClip(clipBounds)
        .AddInset(insetBounds, 3)
        .AddRichtext(GetComponents().ToArray(), textBounds, "richtext")
        .EndClip()
        .AddVerticalScrollbar(value => OnTextScroll(composer, value),
                              scrollBounds, "scrollbar");

    composer.OnComposed += () => SetScrollHeight(composer);
  }

  private void SetScrollHeight(GuiComposer composer) {
    GuiElementScrollbar scrollbar = composer.GetScrollbar("scrollbar");
    GuiElementRichtext text = composer.GetRichtext("richtext");
    scrollbar.SetHeights((float)scrollbar.Bounds.OuterHeight,
                         (float)text.Bounds.fixedHeight + _shrinkParentPadding);
  }

  private void OnTextScroll(GuiComposer composer, float value) {
    GuiElementRichtext text = composer.GetRichtext("richtext");
    text.Bounds.fixedY = -value + _shrinkParentPadding;
    text.Bounds.CalcWorldBounds();
  }

  private List<RichTextComponentBase> GetComponents() {
    List<RichTextComponentBase> components = new();
    ITreeAttribute modData =
        FoodAchievements.GetModData(_capi.World.Player.Entity);
    HashSet<ItemStack> lost =
        new(FoodAchievements.GetLost(_capi.World, modData),
            new ItemStackComparer(_capi.World,
                                  GlobalConstants.IgnoredStackAttributes));
    GourmandSystem gourmand = _capi.ModLoader.GetModSystem<GourmandSystem>();
    FoodAchievements foodAchievements = gourmand.FoodAchievements;

    AddGourmandSummary(components, gourmand.Mod.Logger, foodAchievements,
                       modData, lost);
    components.Add(new RichTextComponent(_capi, "\n", _headerFont));
    AddLostFoods(components, lost);

    Dictionary<AssetLocation, Tuple<int, AchievementPoints>> achievements =
        foodAchievements.GetAchievementStats(modData);
    Random rand = new();
    foreach (var category in achievements) {
      components.Add(new RichTextComponent(_capi, "\n", _headerFont));
      Dictionary<string, List<ItemStack>> missing =
          foodAchievements.GetMissingDict(_capi.World, gourmand.CatDict,
                                          category.Key, modData);
      AddFoodCategory(components, category.Key, category.Value.Item1,
                      category.Value.Item2, rand, missing);
    }
    return components;
  }

  private void AddGourmandSummary(List<RichTextComponentBase> components,
                                  ILogger logger,
                                  FoodAchievements foodAchievements,
                                  ITreeAttribute modData,
                                  HashSet<ItemStack> lost) {
    int points = foodAchievements.GetPointsForAchievements(logger, modData);
    float health = foodAchievements.GetHealthFunctionPiece(
        points, out float gainRate, out int untilPoints);
    if (gainRate != 0) {
      gainRate = 1 / gainRate;
    }

    // Generate two columns of text. The first column has the descriptions of
    // the fields and the second column has the values of the fields.
    List<HeaderLine> fields = new() {
      new(Lang.Get("gourmand:earned-points"), points),
      new(Lang.Get("gourmand:lost-foods-count"), lost.Count),
      new(Lang.Get("gourmand:earned-health"), health, "F2"),
      new(Lang.Get("gourmand:points-for-next-health"), gainRate, "F2"),
      new(Lang.Get("gourmand:until-points"), untilPoints),
    };

    // Find the width of the description of each field.
    CairoFont font = CairoFont.WhiteDetailText().WithLineHeightMultiplier(1.15);
    // Holds the width of the longest description. This turns into the width of
    // the first column.
    double widest = 0;
    foreach (HeaderLine field in fields) {
      field.Width = font.GetTextExtents(field.Description).Width;
      widest = double.Max(widest, field.Width);
    }

    // Add this much extra space between the columns.
    double descToValueSpace = 10;

    // Generate the text components for every field.
    foreach (HeaderLine field in fields) {
      RichTextComponent desc = new(_capi, field.Description, font);
      // RichTextComponents do not have any fields to directly set the width.
      // However, they do have PaddingLeft and PaddingRight fields that add to
      // the size. Use the PaddingRight field of the first column to space the
      // two columns. Add to the value instead of setting it, because the
      // RichTextComponent constructor converts trailing spaces of the input
      // string into a non-zero PaddingRight.
      desc.PaddingRight += widest - field.Width + descToValueSpace;
      components.Add(desc);

      RichTextComponent value = new(_capi, field.Value + "\n", font);
      components.Add(value);
    }
  }

  private void AddLostFoods(List<RichTextComponentBase> components,
                            HashSet<ItemStack> lost) {
    components.Add(new RichTextComponent(
        _capi, Lang.Get("gourmand:lost-foods") + "\n", _headerFont));
    components.Add(new RichTextComponent(
        _capi, Lang.Get("gourmand:lost-foods-desc") + "\n",
        CairoFont.WhiteDetailText()));
    if (lost.Count == 0) {
      components.Add(new RichTextComponent(_capi,
                                           Lang.Get("gourmand:lost-foods-none"),
                                           CairoFont.WhiteSmallText()));
    } else {
      foreach (ItemStack missing in lost) {
        components.Add(new ItemstackTextComponent(_capi, missing, _itemSize, 0,
                                                  EnumFloat.Inline));
      }
    }
    components.Add(
        new RichTextComponent(_capi, "\n", CairoFont.WhiteDetailText()));
  }

  private void AddFoodCategory(List<RichTextComponentBase> components,
                               AssetLocation category, int eaten,
                               AchievementPoints achievement, Random rand,
                               Dictionary<string, List<ItemStack>> missing) {
    AssetLocation categoryName =
        new(category.Domain, category.Path + "-cat-name");
    components.Add(new RichTextComponent(
        _capi, Lang.Get(categoryName.ToString()) + "\n", _headerFont));

    StringBuilder text = new();
    AssetLocation categoryDesc =
        new(category.Domain, category.Path + "-cat-desc");
    text.AppendLine(Lang.Get(categoryDesc.ToString()));
    if (achievement.BonusAt != 0) {
      text.AppendLine(
          Lang.Get("gourmand:eaten-foods", eaten, achievement.BonusAt));
    } else {
      text.AppendLine(Lang.Get("gourmand:eaten-foods-no-bonus", eaten));
    }
    text.AppendLine(Lang.Get("gourmand:points-per-food", achievement.Points));
    text.AppendLine(Lang.Get("gourmand:completion-bonus", achievement.Bonus));
    text.AppendLine(Lang.Get("gourmand:missing"));
    components.Add(new RichTextComponent(_capi, text.ToString(),
                                         CairoFont.WhiteDetailText()));

    if (missing.Count == 0) {
      components.Add(
          new RichTextComponent(_capi, Lang.Get("gourmand:missing-foods-none"),
                                CairoFont.WhiteSmallText()));
    } else {
      foreach (KeyValuePair<string, List<ItemStack>> foods in missing) {
        ItemStack[] foodsArray = foods.Value.ToArray();
        foodsArray.Shuffle(rand);
        components.Add(new SlideshowItemstackTextComponent(
            _capi, foodsArray, _itemSize, EnumFloat.Inline));
      }
    }
    components.Add(
        new RichTextComponent(_capi, "\n", CairoFont.WhiteDetailText()));
  }
}
