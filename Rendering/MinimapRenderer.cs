using System.Collections.Generic;
using System.Numerics;
using ExileCore;
using ExileCore.Shared;
using WhatsAHeadhunter.Detection;

namespace WhatsAHeadhunter.Rendering;

public class MinimapRenderer
{
    public void Render(
        Graphics graphics,
        GameController gc,
        WhatsAHeadhunterSettings settings,
        Dictionary<uint, TrackedRare> rares)
    {
        var player = gc.Player;
        if (player == null) return;
        var playerGrid = player.GridPosNum;

        foreach (var kvp in rares)
        {
            var rare = kvp.Value;
            if (!rare.IsValid) continue;
            if (!rare.HasShroudWalker) continue;

            var color = settings.Minimap.ShroudWalkerColor.Value;
            var size = settings.Minimap.IconSize.Value;

            // Draw cross on minimap at entity position
            var entityGrid = rare.GridPos;
            var delta = entityGrid - playerGrid;

            graphics.DrawLineInWorld(
                entityGrid + new Vector2(-size, 0),
                entityGrid + new Vector2(size, 0),
                2f,
                color);

            graphics.DrawLineInWorld(
                entityGrid + new Vector2(0, -size),
                entityGrid + new Vector2(0, size),
                2f,
                color);
        }
    }
}
