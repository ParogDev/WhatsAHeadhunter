using System.Windows.Forms;
using ExileCore.Shared.Attributes;
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
    public ToggleNode PreScanMonsters { get; set; } = new(true);
    public ToggleNode TrackSmokeMine { get; set; } = new(true);
}

[Submenu]
public class OverlaySettings
{
    public ToggleNode Enabled { get; set; } = new(true);
    public ToggleNode ShowAllRares { get; set; } = new(false);
    public ColorNode ShroudWalkerColor { get; set; } = new(new Color(255, 50, 50, 180));
    public ColorNode OtherRareColor { get; set; } = new(new Color(255, 200, 50, 120));
    public RangeNode<int> CircleRadius { get; set; } = new(60, 20, 200);
    public ToggleNode ShowLabel { get; set; } = new(true);
    public RangeNode<int> LabelFontSize { get; set; } = new(16, 8, 32);
}

[Submenu]
public class MinimapSettings
{
    public ToggleNode Enabled { get; set; } = new(true);
    public ColorNode ShroudWalkerColor { get; set; } = new(new Color(255, 50, 50));
    public RangeNode<float> IconSize { get; set; } = new(8f, 2f, 20f);
}

[Submenu]
public class HudSettings
{
    public ToggleNode Enabled { get; set; } = new(true);
    public RangeNode<int> X { get; set; } = new(500, 0, 3840);
    public RangeNode<int> Y { get; set; } = new(36, 0, 2160);
    public RangeNode<int> BarWidth { get; set; } = new(220, 50, 600);
    public RangeNode<int> BarHeight { get; set; } = new(22, 10, 60);
    public ToggleNode ShowBuffCounter { get; set; } = new(true);
    public ToggleNode ShowReadinessBar { get; set; } = new(true);
    public ToggleNode ShowExpiryTimer { get; set; } = new(true);
}

[Submenu]
public class BeltSwapSettings
{
    public RangeNode<int> Mode { get; set; } = new(1, 0, 2);
    public HotkeyNodeV2 Hotkey { get; set; } = new(Keys.None);
    public RangeNode<int> SwapCooldownMs { get; set; } = new(1000, 500, 5000);
    public RangeNode<int> ClickDelayMs { get; set; } = new(50, 20, 200);
}
