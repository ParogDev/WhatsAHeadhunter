using System.Windows.Forms;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using SharpDX;

namespace WhatsAHeadhunter;

public class WhatsAHeadhunterSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new(true);
    public DetectionSettings Detection { get; set; } = new();
    public OverlaySettings Overlay { get; set; } = new();
    public MinimapSettings Minimap { get; set; } = new();
    public HudSettings Hud { get; set; } = new();
    public BeltSwapSettings BeltSwap { get; set; } = new();
}

[Submenu]
public class DetectionSettings
{
    [Menu("Pre-scan Monster Mods")]
    public ToggleNode PreScanMonsters { get; set; } = new(true);

    [Menu("Track Smoke Mine Cooldown")]
    public ToggleNode TrackSmokeMine { get; set; } = new(true);
}

[Submenu]
public class OverlaySettings
{
    [Menu("Enable World Overlay")]
    public ToggleNode Enabled { get; set; } = new(true);

    [Menu("Show All Rares")]
    public ToggleNode ShowAllRares { get; set; } = new(false);

    [Menu("Shroud Walker Color")]
    public ColorNode ShroudWalkerColor { get; set; } = new(new Color(255, 50, 50, 180));

    [Menu("Other Rare Color")]
    public ColorNode OtherRareColor { get; set; } = new(new Color(255, 200, 50, 120));

    [Menu("Circle Radius")]
    public RangeNode<int> CircleRadius { get; set; } = new(60, 20, 200);

    [Menu("Show Text Label")]
    public ToggleNode ShowLabel { get; set; } = new(true);

    [Menu("Label Font Size")]
    public RangeNode<int> LabelFontSize { get; set; } = new(16, 8, 32);
}

[Submenu]
public class MinimapSettings
{
    [Menu("Enable Minimap Icons")]
    public ToggleNode Enabled { get; set; } = new(true);

    [Menu("Shroud Walker Color")]
    public ColorNode ShroudWalkerColor { get; set; } = new(new Color(255, 50, 50));

    [Menu("Icon Size")]
    public RangeNode<float> IconSize { get; set; } = new(8f, 2f, 20f);
}

[Submenu]
public class HudSettings
{
    [Menu("Enable HUD")]
    public ToggleNode Enabled { get; set; } = new(true);

    [Menu("Position X")]
    public RangeNode<int> X { get; set; } = new(500, 0, 3840);

    [Menu("Position Y")]
    public RangeNode<int> Y { get; set; } = new(36, 0, 2160);

    [Menu("Bar Width")]
    public RangeNode<int> BarWidth { get; set; } = new(220, 50, 600);

    [Menu("Bar Height")]
    public RangeNode<int> BarHeight { get; set; } = new(22, 10, 60);

    [Menu("Show Buff Counter")]
    public ToggleNode ShowBuffCounter { get; set; } = new(true);

    [Menu("Show Readiness Bar")]
    public ToggleNode ShowReadinessBar { get; set; } = new(true);

    [Menu("Show Expiry Timer")]
    public ToggleNode ShowExpiryTimer { get; set; } = new(true);
}

[Submenu]
public class BeltSwapSettings
{
    [Menu("Belt Swap Mode (0=Off, 1=Key, 2=Auto)")]
    public RangeNode<int> Mode { get; set; } = new(1, 0, 2);

    [Menu("Belt Swap Hotkey")]
    public HotkeyNodeV2 Hotkey { get; set; } = new(Keys.None);

    [Menu("Swap Cooldown (ms)")]
    public RangeNode<int> SwapCooldownMs { get; set; } = new(1000, 500, 5000);

    [Menu("Click Delay (ms)")]
    public RangeNode<int> ClickDelayMs { get; set; } = new(50, 20, 200);
}
