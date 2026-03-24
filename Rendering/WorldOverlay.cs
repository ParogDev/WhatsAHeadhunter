using System.Collections.Generic;
using System.Numerics;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.Shared;
using SharpDX;
using WhatsAHeadhunter.Detection;
using Vector2N = System.Numerics.Vector2;

namespace WhatsAHeadhunter.Rendering;

public class WorldOverlay
{
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

                graphics.DrawText(
                    label,
                    new Vector2N(screenPos.X, screenPos.Y - 30),
                    textColor,
                    settings.Overlay.LabelFontSize.Value,
                    FontAlign.Center);
            }
        }
    }
}
