using System.Collections.Generic;
using System.Numerics;
using ExileCore;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using SharpDX;
using WhatsAHeadhunter.Detection;
using Vector2N = System.Numerics.Vector2;

namespace WhatsAHeadhunter.Rendering;

public class WorldOverlay
{
    /// <summary>
    /// The size the HUD draws text at (CoreSettings font size; DrawText scales from it), so LabelFontSize stays in pixels.
    /// 16 when the setting can't be read.
    /// </summary>
    private static float HudFontSize(GameController gc)
    {
        var size = gc.Settings?.CoreSettings?.FontSettings?.FontSize?.Value ?? 0;
        return size > 0 ? size : 16f;
    }

    public void Render(
        Graphics graphics,
        GameController gc,
        WhatsAHeadhunterSettings settings,
        Dictionary<uint, TrackedRare> rares)
    {
        var camera = gc.IngameState.Camera;

        foreach (var kvp in rares)
        {
            var rare = kvp.Value;
            if (!rare.IsValid) continue;

            var color = rare.HasShroudWalker
                ? settings.Overlay.ShroudWalkerColor.Value
                : settings.Overlay.OtherRareColor.Value;

            if (!rare.HasShroudWalker && !settings.Overlay.ShowAllRares.Value)
                continue;

            // World circle
            graphics.DrawFilledCircleInWorld(
                rare.WorldPos,
                settings.Overlay.CircleRadius.Value,
                color);

            // Text label
            if (settings.Overlay.ShowLabel.Value)
            {
                var screenPos = camera.WorldToScreen(rare.WorldPos);
                if (screenPos == Vector2N.Zero) continue;

                var label = rare.HasShroudWalker ? "SHROUD WALKER" : rare.Name;
                var textColor = rare.HasShroudWalker ? Color.Red : Color.Yellow;

                using (graphics.SetTextScale(settings.Overlay.LabelFontSize.Value / HudFontSize(gc)))
                {
                    graphics.DrawText(
                        label,
                        new Vector2N(screenPos.X, screenPos.Y - 30),
                        textColor,
                        FontAlign.Center);
                }
            }
        }
    }
}
