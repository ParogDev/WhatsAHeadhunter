using System.Collections.Generic;
using System.Numerics;
using ExileCore;
using ExileCore.Shared;
using SharpDX;
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
                rare.WorldPos + new Vector3(-size, 0, 0),
                rare.WorldPos + new Vector3(size, 0, 0),
                2f,
                color);

            graphics.DrawLineInWorld(
                rare.WorldPos + new Vector3(0, -size, 0),
                rare.WorldPos + new Vector3(0, size, 0),
                2f,
                color);
        }
    }
}
