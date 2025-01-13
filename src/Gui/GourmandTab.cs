using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Gourmand.Gui;

public class CachedGuiElementRichtext : GuiElementRichtext {
  private bool _keepCache = false;
  public Action OnDispose = null;
  public Action OnRedraw = null;
  public CachedGuiElementRichtext(ICoreClientAPI capi,
                                  RichTextComponentBase[] components,
                                  ElementBounds bounds)
      : base(capi, components, bounds) {}

  public void KeepCache(bool keep) { _keepCache = keep; }

  public override void Dispose() {
    if (!_keepCache) {
      OnDispose();
      base.Dispose();
      GC.SuppressFinalize(this);
    }
  }

  public void Redraw() { OnRedraw(); }
}

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
  // Shows up to two rows of icons
  private static readonly int MaxShowPreview = 16;
  private AssetLocation _focusCategory = null;
  private CachedGuiElementRichtext _overview = null;
  private float _cachedOverviewScroll = 0;
  private GuiElementScrollbar _scrollbar;

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
    GetDebugLogger()?.Debug("ComposeTab");
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

    _overview?.KeepCache(false);

    void Redraw() {
      var tabs = composer.GetHorizontalTabs("tabs");
      tabs.SetValue(tabs.activeElement, true);
    }

    _scrollbar = new GuiElementScrollbar(
        _capi, value => OnTextScroll(composer, value), scrollBounds);

    composer.BeginClip(clipBounds)
        .AddInset(insetBounds, 3)
        .AddInteractiveElement(
            GetRichtext(textBounds, Redraw, out float scrollTo), "richtext")
        .EndClip()
        .AddInteractiveElement(_scrollbar, "scrollbar");

    composer.OnComposed += () => SetScrollHeight(composer, scrollTo);
  }

  private GuiElementRichtext GetRichtext(ElementBounds textBounds,
                                         Action redraw, out float scrollTo) {
    if (_focusCategory == null) {
      if (_overview == null) {
        ITreeAttribute modData =
            FoodAchievements.GetModData(_capi.World.Player.Entity);
        GourmandSystem gourmand =
            _capi.ModLoader.GetModSystem<GourmandSystem>();
        FoodAchievements foodAchievements = gourmand.FoodAchievements;

        _overview = new CachedGuiElementRichtext(_capi, null, textBounds) {
          OnRedraw = () => redraw(),
          OnDispose =
              () => {
                GetDebugLogger()?.Debug("Clearing overview cache.");
                _overview = null;
              },
        };
        List<RichTextComponentBase> components = new();
        AddOverviewComponents(_overview.Redraw, components, gourmand,
                              foodAchievements, modData);
        _overview.Components = components.ToArray();

        _cachedOverviewScroll = 0;
        scrollTo = 0;
      } else {
        GetDebugLogger()?.Debug("Using cached overview.");
        scrollTo = _cachedOverviewScroll;
      }
      return _overview;
    } else {
      ITreeAttribute modData =
          FoodAchievements.GetModData(_capi.World.Player.Entity);
      GourmandSystem gourmand = _capi.ModLoader.GetModSystem<GourmandSystem>();
      FoodAchievements foodAchievements = gourmand.FoodAchievements;
      List<RichTextComponentBase> components = new();
      AddCategoryFocusComponents(redraw, components, _focusCategory, gourmand,
                                 foodAchievements, modData);
      scrollTo = 0;
      CachedGuiElementRichtext text =
          new(_capi, components.ToArray(),
              textBounds) { OnDispose = () => _overview?.Dispose() };
      return text;
    }
  }

  private void SetScrollHeight(GuiComposer composer, float scrollTo) {
    GuiElementScrollbar scrollbar = composer.GetScrollbar("scrollbar");
    GuiElementRichtext text = composer.GetRichtext("richtext");
    scrollbar.SetHeights((float)scrollbar.Bounds.OuterHeight,
                         (float)text.Bounds.fixedHeight + _shrinkParentPadding);
    if (scrollTo != 0) {
      scrollbar.CurrentYPosition = scrollTo;
      OnTextScroll(composer, scrollTo);
    }
  }

  private void OnTextScroll(GuiComposer composer, float value) {
    GuiElementRichtext text = composer.GetRichtext("richtext");
    text.Bounds.fixedY = -value + _shrinkParentPadding;
    text.Bounds.CalcWorldBounds();
  }

  private void AddOverviewComponents(Action redraw,
                                     List<RichTextComponentBase> components,
                                     GourmandSystem gourmand,
                                     FoodAchievements foodAchievements,
                                     ITreeAttribute modData) {
    HashSet<ItemStack> lost =
        new(FoodAchievements.GetLost(_capi.World, modData),
            new ItemStackComparer(_capi.World,
                                  GlobalConstants.IgnoredStackAttributes));
    AddGourmandSummary(components, gourmand.Mod.Logger, foodAchievements,
                       modData, lost);
    components.Add(new RichTextComponent(_capi, "\n", _headerFont));
    AddLostFoods(components, lost);
    components.Add(new RichTextComponent(_capi, "\n", _headerFont));

    Dictionary<AssetLocation, Tuple<int, AchievementPoints>> achievements =
        foodAchievements.GetAchievementStats(modData);
    Random rand = new();
    foreach (var category in achievements) {
      AddFoodAchievement(redraw, components, gourmand, foodAchievements,
                         modData, category.Key, category.Value.Item1,
                         category.Value.Item2, rand, MaxShowPreview);
    }
  }

  private void
  AddFoodAchievement(Action redraw, List<RichTextComponentBase> components,
                     GourmandSystem gourmand, FoodAchievements foodAchievements,
                     ITreeAttribute modData, AssetLocation category, int eaten,
                     AchievementPoints achievement, Random rand, int maxShow) {
    Stopwatch stopwatch = new();
    stopwatch.Start();
    bool more = foodAchievements.GetMissingDict(
        _capi.World, gourmand.CatDict, category, modData, maxShow,
        out Dictionary<string, List<ItemStack>> missing);
    stopwatch.Stop();
    GetDebugLogger()?.Debug("Enumerated category {0} with {1} values in {2}.",
                            category, missing.Sum(m => m.Value.Count),
                            stopwatch.Elapsed);
    AddFoodCategory(redraw, components, category, eaten, achievement, rand,
                    missing, more);
  }

  private void AddCategoryFocusComponents(
      Action redraw, List<RichTextComponentBase> components,
      AssetLocation focusCategory, GourmandSystem gourmand,
      FoodAchievements foodAchievements, ITreeAttribute modData) {
    LinkTextComponent link =
        new(_capi, Lang.Get("gourmand:back-to-overview") + "\n",
            CairoFont.WhiteSmallText(), (l) => BackToOverview(redraw));
    components.Add(link);

    Random rand = new();
    Dictionary<AssetLocation, Tuple<int, AchievementPoints>> achievements =
        foodAchievements.GetAchievementStats(modData);
    AddFoodAchievement(redraw, components, gourmand, foodAchievements, modData,
                       focusCategory, achievements[focusCategory].Item1,
                       achievements[focusCategory].Item2, rand, int.MaxValue);
  }

  private ILogger GetDebugLogger() {
    if (_capi.Settings.Bool["extendedDebugInfo"]) {
      GourmandSystem gourmand = _capi.ModLoader.GetModSystem<GourmandSystem>();
      return gourmand.Mod.Logger;
    } else {
      return null;
    }
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

  private void AddFoodCategory(Action redraw,
                               List<RichTextComponentBase> components,
                               AssetLocation category, int eaten,
                               AchievementPoints achievement, Random rand,
                               Dictionary<string, List<ItemStack>> missing,
                               bool more) {
    AssetLocation categoryName =
        new(category.Domain, category.Path + "-cat-name");
    components.Add(new RichTextComponent(
        _capi, Lang.Get(categoryName.ToString()) + "\n", _headerFont));

    StringBuilder text = new();
    AssetLocation categoryDesc;
    if (achievement.Description != null) {
      categoryDesc =
          AssetLocation.Create(achievement.Description, category.Domain);
    } else {
      categoryDesc = new(category.Domain, category.Path + "-cat-desc");
    }
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
    components.AddRange(VtmlUtil.Richtextify(_capi, text.ToString(),
                                             CairoFont.WhiteDetailText()));

    AddMissingIcons(redraw, components, category, rand, missing, more);
    components.Add(new RichTextComponent(_capi, "\n", _headerFont));
  }

  private void AddMissingIcons(Action redraw,
                               List<RichTextComponentBase> components,
                               AssetLocation category, Random rand,
                               Dictionary<string, List<ItemStack>> missing,
                               bool more) {
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
      if (more) {
        components.Add(
            new LinkTextComponent(_capi, Lang.Get("gourmand:more-entries"),
                                  CairoFont.WhiteSmallText(),
                                  (l) => FocusOnCategory(category, redraw)));
      }
    }
    components.Add(
        new RichTextComponent(_capi, "\n", CairoFont.WhiteDetailText()));
  }

  private void FocusOnCategory(AssetLocation category, Action redraw) {
    _focusCategory = category;
    _overview?.KeepCache(true);
    _cachedOverviewScroll = _scrollbar.CurrentYPosition;

    redraw();
  }

  private void BackToOverview(Action redraw) {
    _overview?.KeepCache(true);
    _focusCategory = null;
    redraw();
  }
}
