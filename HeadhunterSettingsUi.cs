using System;
using System.Numerics;
using ImGuiNET;
using WhatsAHeadhunter.Automation;
using WhatsAHeadhunter.Detection;

namespace WhatsAHeadhunter;

// Settings panel: the generated SettingsMenu (see SettingsMenu.cs) draws every setting from its [Menu] label and
// tooltip; tabs come from the [Submenu] properties and [MenuTab] (Status / Overlay / HUD / Belt Swap). Hand-drawn on
// top: the live Status cards (stolen mods, Shroud Walker, teleport readiness), the Belt Swap mode picker (a named
// combo instead of a 0-2 slider) and the swap readiness line.
public sealed class HeadhunterSettingsUi
{
    private readonly SettingsMenu _menu;
    private WhatsAHeadhunterSettings? _settings;
    private StolenModTracker? _tracker;
    private ShroudWalkerDetector? _detector;
    private float _pulsePhase;

    // Gold/amber palette for the Headhunter theme
    private static readonly Vector4 Accent = new(1f, 0.75f, 0.2f, 1f);
    private static readonly Vector4 DangerRed = new(0.9f, 0.2f, 0.2f, 1f);
    private static readonly Vector4 SafeGreen = new(0.2f, 0.8f, 0.3f, 1f);
    private static readonly Vector4 Amber = new(0.9f, 0.8f, 0.1f, 1f);
    private static readonly Vector4 Muted = new(0.5f, 0.5f, 0.55f, 1f);

    private static readonly string[] ModeNames = { "Disabled", "Key Press", "Automatic" };

    public HeadhunterSettingsUi()
    {
        _menu = new SettingsMenu { Accent = Accent, RootTabName = "Status" }
            .Tab("Status", DrawStatusTab, first: true)
            .Custom("BeltSwap.Mode", DrawBeltSwapMode)
            .Tab("Belt Swap", DrawBeltSwapStatus);
    }

    public void Draw(
        WhatsAHeadhunterSettings settings,
        StolenModTracker tracker,
        ShroudWalkerDetector detector)
    {
        _settings = settings;
        _tracker = tracker;
        _detector = detector;
        _menu.Draw(settings);
    }

    private void DrawStatusTab()
    {
        var tracker = _tracker;
        var detector = _detector;
        if (tracker == null || detector == null)
        {
            SettingsMenu.Dim("Status: the plugin passed no tracker/detector (not initialised yet)");
            return;
        }
        var dl = ImGui.GetWindowDrawList();

        if (tracker.HasStolenMods)
        {
            StatusCard(dl, "Headhunter Active", Accent,
                $"Buffs: {tracker.TotalBuffCount} from {tracker.ActiveGroups.Count} kills");
            if (tracker.ActiveCompanionBuffs.Count > 0)
            {
                SettingsMenu.Text(Muted, "Companion Effects:");
                foreach (var kvp in tracker.ActiveCompanionBuffs)
                    SettingsMenu.Text(ImGui.GetStyle().Colors[(int)ImGuiCol.Text],
                        "  - " + (string.IsNullOrEmpty(kvp.Value) ? kvp.Key : kvp.Value));
                ImGui.Spacing();
            }
        }
        else
        {
            StatusCard(dl, "No Stolen Mods", Muted, "Kill rare monsters with Headhunter equipped");
        }

        if (detector.IsActive)
        {
            _pulsePhase += 0.02f;
            var pulse = (float)(Math.Sin(_pulsePhase * Math.PI * 2) * 0.3 + 0.7);
            StatusCard(dl, "!! SHROUD WALKER DETECTED !!", new Vector4(0.9f * pulse, 0.15f, 0.15f, 1f),
                $"Teleports: {detector.TeleportCount} | Expires: {detector.ShroudWalkerTimeRemaining:F0}s");
            ReadinessBar(dl, detector);
        }
        else
        {
            _pulsePhase = 0f;
            StatusCard(dl, "Shroud Walker: Not Detected", SafeGreen, "Safe to play");
        }
        ImGui.Spacing();
    }

    private static void ReadinessBar(ImDrawListPtr dl, ShroudWalkerDetector detector)
    {
        var readiness = detector.ReadinessFraction;
        var barMin = ImGui.GetCursorScreenPos();
        float barW = ImGui.GetContentRegionAvail().X;
        const float barH = 20f;
        var barMax = barMin + new Vector2(barW, barH);

        dl.AddRectFilled(barMin, barMax, U32(new Vector4(0.1f, 0.1f, 0.12f, 1f)), 3f);
        if (readiness > 0)
        {
            var fill = readiness < 0.5f
                ? Vector4.Lerp(SafeGreen, Amber, readiness * 2f)
                : Vector4.Lerp(Amber, DangerRed, Math.Clamp((readiness - 0.5f) * 2f, 0f, 1f));
            dl.AddRectFilled(barMin, barMin + new Vector2(barW * Math.Clamp(readiness, 0f, 1f), barH), U32(fill), 3f);
        }
        dl.AddRect(barMin, barMax, U32(new Vector4(0.3f, 0.3f, 0.35f, 1f)), 3f);

        var label = detector.TeleportReady ? "TELEPORT READY" : $"Next teleport: {detector.SmokeMineTimeRemaining:F1}s";
        var sz = ImGui.CalcTextSize(label);
        dl.AddText((barMin + barMax) * 0.5f - sz * 0.5f, U32(new Vector4(0.88f, 0.88f, 0.88f, 1f)), label);
        ImGui.Dummy(new Vector2(0, barH + 4));
    }

    // BeltSwap.Mode is a RangeNode<int> 0-2 (kept for the saved settings): drawn as a named combo with what each mode does
    private void DrawBeltSwapMode()
    {
        var node = _settings?.BeltSwap.Mode;
        if (node == null)
        {
            SettingsMenu.Dim("Mode: settings.BeltSwap.Mode is null");
            return;
        }
        ImGui.AlignTextToFramePadding();
        SettingsMenu.Text(ImGui.GetStyle().Colors[(int)ImGuiCol.Text], "Mode");
        if (ImGui.IsItemHovered()) SettingsMenu.Tooltip("What happens while Shroud Walker is active");
        // the generated rows' control column: widest label in this card + 26 px, at least 120 px
        ImGui.SameLine(Math.Max(120f, ImGui.CalcTextSize("Swap Cooldown").X + 26f));
        ImGui.SetNextItemWidth(-1);
        int mode = Math.Clamp(node.Value, 0, ModeNames.Length - 1);
        if (ImGui.Combo("##hh_beltmode", ref mode, ModeNames, ModeNames.Length))
            node.Value = mode;

        ImGui.PushTextWrapPos(0f);
        switch ((BeltSwapMode)mode)
        {
            case BeltSwapMode.KeyPress:
                SettingsMenu.Dim("Press the hotkey to unequip and re-equip your belt, clearing all Headhunter buffs. "
                    + "Only works while Shroud Walker is detected.");
                break;
            case BeltSwapMode.Automatic:
                SettingsMenu.Text(DangerRed, "WARNING: Automatic swaps your belt as soon as Shroud Walker is detected. "
                    + "Use with caution.");
                break;
            default:
                SettingsMenu.Dim("Belt swap is off.");
                break;
        }
        ImGui.PopTextWrapPos();
        ImGui.Dummy(new Vector2(1, 4f));
    }

    private void DrawBeltSwapStatus()
    {
        if (_settings == null || _detector == null) return;
        var mode = (BeltSwapMode)_settings.BeltSwap.Mode.Value;
        var canSwap = _detector.IsActive && mode != BeltSwapMode.Disabled;
        _menu.Badge(canSwap ? "Ready to swap" : "Inactive (no Shroud Walker or disabled)", canSwap ? SafeGreen : Muted);
    }

    private static void StatusCard(ImDrawListPtr dl, string title, Vector4 titleColor, string detail)
    {
        var pos = ImGui.GetCursorScreenPos();
        float w = ImGui.GetContentRegionAvail().X;
        const float h = 48f;
        dl.AddRectFilled(pos, pos + new Vector2(w, h), U32(new Vector4(0.06f, 0.06f, 0.08f, 1f)), 4f);
        dl.AddRect(pos, pos + new Vector2(w, h), U32(titleColor), 4f);
        dl.AddText(pos + new Vector2(8, 6), U32(titleColor), title);
        dl.AddText(pos + new Vector2(8, 26), U32(Muted), detail);
        ImGui.Dummy(new Vector2(0, h + 4));
    }

    private static uint U32(Vector4 c) => ImGui.GetColorU32(c);
}
