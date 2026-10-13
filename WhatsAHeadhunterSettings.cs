using System.Windows.Forms;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using SharpDX;

namespace WhatsAHeadhunter;

public class WhatsAHeadhunterSettings : ISettings
{
    [Menu("Enable", "Turn the whole plugin on or off")]
    public ToggleNode Enable { get; set; } = new(true);

    [Menu("Detection", "How Shroud Walker rares are detected")]
    public DetectionSettings Detection { get; set; } = new();

    [Menu("World Overlay", "Circles and labels drawn on rare monsters in the world")]
    public OverlaySettings Overlay { get; set; } = new();

    [Menu("Minimap", "Shroud Walker markers on the minimap / large map")]
    public MinimapSettings Minimap { get; set; } = new();

    [Menu("Status HUD", "On-screen bars for stolen mods, teleport readiness and Shroud Walker expiry")]
    public HudSettings Hud { get; set; } = new();

    [Menu("Belt Swap", "Automatic or hotkey belt swap while Shroud Walker is active")]
    public BeltSwapSettings BeltSwap { get; set; } = new();
}

[Submenu]
public class DetectionSettings
{
    [Menu("Pre-scan Monster Mods", "Scan rare monsters for the Shroud Walker mod before you kill them (needed for the world and minimap markers)")]
    public ToggleNode PreScanMonsters { get; set; } = new(true);

    [Menu("Track Smoke Mine Cooldown", "Reserved for teleport readiness from smoke mine buff timing; not read by the plugin yet")]
    public ToggleNode TrackSmokeMine { get; set; } = new(true);
}

[Submenu]
public class OverlaySettings
{
    [Menu("World Overlay", "Draw circles on dangerous rare monsters in the world")]
    public ToggleNode Enabled { get; set; } = new(true);

    [Menu("Show All Rares", "Mark every rare monster, not just those with Shroud Walker")]
    public ToggleNode ShowAllRares { get; set; } = new(false);

    [Menu("Shroud Walker Color", "Circle color for rares carrying Shroud Walker")]
    public ColorNode ShroudWalkerColor { get; set; } = new(new Color(255, 50, 50, 180));

    [Menu("Other Rare Color", "Circle color for other rares (only drawn with Show All Rares)")]
    public ColorNode OtherRareColor { get; set; } = new(new Color(255, 200, 50, 120));

    [Menu("Circle Radius", "Radius of the world circle, in world units (20-200)")]
    public RangeNode<int> CircleRadius { get; set; } = new(60, 20, 200);

    [Menu("Text Labels", "Show the mod name above marked rares")]
    public ToggleNode ShowLabel { get; set; } = new(true);

    [Menu("Label Font Size", "Label text size in px (8-32, 16 = normal)")]
    public RangeNode<int> LabelFontSize { get; set; } = new(16, 8, 32);
}

[Submenu]
public class MinimapSettings
{
    [Menu("Minimap Icons", "Draw a cross on the map for each Shroud Walker rare")]
    public ToggleNode Enabled { get; set; } = new(true);

    [Menu("Icon Color", "Color of the Shroud Walker map cross")]
    public ColorNode ShroudWalkerColor { get; set; } = new(new Color(255, 50, 50));

    [Menu("Icon Size", "Half-length of each cross arm, in grid units (2-20)")]
    public RangeNode<float> IconSize { get; set; } = new(8f, 2f, 20f);
}

[Submenu]
public class HudSettings
{
    [Menu("Enable HUD", "Show the status bars on screen")]
    public ToggleNode Enabled { get; set; } = new(true);

    [Menu("Position X", "Left edge of the HUD, in screen px")]
    public RangeNode<int> X { get; set; } = new(500, 0, 3840);

    [Menu("Position Y", "Top edge of the HUD, in screen px")]
    public RangeNode<int> Y { get; set; } = new(36, 0, 2160);

    [Menu("Bar Width", "Width of each bar, in px (50-600)")]
    public RangeNode<int> BarWidth { get; set; } = new(220, 50, 600);

    [Menu("Bar Height", "Height of each bar, in px (10-60)")]
    public RangeNode<int> BarHeight { get; set; } = new(22, 10, 60);

    [Menu("Buff Counter", "Show how many mods Headhunter has stolen")]
    public ToggleNode ShowBuffCounter { get; set; } = new(true);

    [Menu("Readiness Bar", "Show teleport readiness progress while Shroud Walker is active")]
    public ToggleNode ShowReadinessBar { get; set; } = new(true);

    [Menu("Expiry Timer", "Show the time left until Shroud Walker expires")]
    public ToggleNode ShowExpiryTimer { get; set; } = new(true);
}

[Submenu]
public class BeltSwapSettings
{
    [Menu("Mode", "0 = Disabled, 1 = Key Press (swap on hotkey), 2 = Automatic (swap as soon as Shroud Walker is active)")]
    public RangeNode<int> Mode { get; set; } = new(1, 0, 2);

    [Menu("Hotkey", "Key that triggers a belt swap in Key Press mode")]
    public HotkeyNodeV2 Hotkey { get; set; } = new(Keys.None);

    [Menu("Swap Cooldown", "Minimum time between swap attempts, in ms (500-5000)")]
    public RangeNode<int> SwapCooldownMs { get; set; } = new(1000, 500, 5000);

    [Menu("Click Delay", "Pause between inventory clicks during a swap, in ms (20-200)")]
    public RangeNode<int> ClickDelayMs { get; set; } = new(50, 20, 200);
}
