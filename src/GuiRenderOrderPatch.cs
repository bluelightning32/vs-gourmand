using System;

using HarmonyLib;

using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace Gourmand;

#pragma warning disable IDE1006

[HarmonyPatch(typeof(GuiDialogCharacter))]
// Fix a zorder glitch with the character dialog.
class GuiDialogPatch {
  [HarmonyPrefix]
  [HarmonyPatch("OnRenderGUI")]
  public static bool OnRenderGUI(GuiDialogCharacter __instance, float deltaTime,
                                 ref int ___curTab, ref Matrixf ___mat,
                                 ref Vec4f ___lighPos,
                                 ref ElementBounds ___insetSlotBounds,
                                 ref float ___yaw,
                                 ref bool ___rotateCharacter) {
    foreach (var val in __instance.Composers) {
      val.Value.Render(deltaTime);
      if (val.Key == "playercharacter" && ___curTab == 0) {
        // Render the avatar after rendering the playercharacter composer to
        // which is belongs. The base game instead renders it after all of the
        // composers, which causes it to go on top of other character dialog
        // composers such as the environment and playerstats composers.
        RenderAvatar(__instance, deltaTime, val.Value.Api, ___mat, ___lighPos,
                     ___insetSlotBounds, ref ___yaw, ___rotateCharacter);
      }
      // Translate the z position so that the next composer does not z fight
      // with the previous one. Ideally this would use val.Value.zDepth, but
      // that isn't initialized correctly by the game.
      val.Value.Api.Render.GlTranslate(0, 0, __instance.ZSize);

      __instance.MouseOverCursor = val.Value.MouseOverCursor;
    }
    return false;
  }

  // This is copied from GuiDialogCharacter.OnRenderGUI.
  public static void RenderAvatar(GuiDialogCharacter __instance,
                                  float deltaTime, ICoreClientAPI capi,
                                  Matrixf ___mat, Vec4f ___lighPos,
                                  ElementBounds ___insetSlotBounds,
                                  ref float ___yaw, bool ___rotateCharacter) {
    capi.Render.GlPushMatrix();
    if (__instance.Focused) {
      capi.Render.GlTranslate(0f, 0f, 150f);
    }
    double pad =
        GuiElement.scaled(GuiElementItemSlotGridBase.unscaledSlotPadding);
    capi.Render.GlRotate(-14f, 1f, 0f, 0f);
    ___mat.Identity();
    ___mat.RotateXDeg(-14f);
    Vec4f lightRot = ___mat.TransformVector(___lighPos);
    capi.Render.CurrentActiveShader.Uniform(
        "lightPosition", new Vec3f(lightRot.X, lightRot.Y, lightRot.Z));
    capi.Render.RenderEntityToGui(
        deltaTime, capi.World.Player.Entity,
        ___insetSlotBounds.renderX + pad - GuiElement.scaled(41.0),
        ___insetSlotBounds.renderY + pad - GuiElement.scaled(25.0),
        GuiElement.scaled(210.0), ___yaw, (float)GuiElement.scaled(135.0), -1);
    capi.Render.GlPopMatrix();
    capi.Render.CurrentActiveShader.Uniform("lightPosition",
                                            new Vec3f(1f, -1f, 0f).Normalize());
    if (!___insetSlotBounds.PointInside(capi.Input.MouseX, capi.Input.MouseY) &&
        !___rotateCharacter) {
      ___yaw +=
          (float)(Math.Sin((float)capi.World.ElapsedMilliseconds / 1000f) /
                  200.0);
    }
  }
}

#pragma warning restore IDE1006
