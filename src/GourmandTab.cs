using System;
using System.Reflection;

using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace Gourmand;

public class GourmandTab {
  private readonly ICoreClientAPI _capi;
  private readonly GuiDialogCharacterBase _dialog;
  // The parent's padding is too big. Grow 3 units into the parent's padding
  // in all directions.
  private readonly float _shrinkParentPadding = 3;

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
    string text = @"line1
A really really really really really really really really really really really really really really long line
line3
line4
line5
line6
line7
line8
line9
line10
line11
line12
line13
line14
line15
line16
line17
line18
line19
line20
line21
line22
line23
line24
line25
line26
line27
line28
line29";
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
        .AddRichtext(text,
                     CairoFont.WhiteDetailText().WithLineHeightMultiplier(1.15),
                     textBounds, "richtext")
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
}
