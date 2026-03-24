using System;
using System.Numerics;
using ImGuiNET;
using WhatsAHeadhunter.Automation;
using WhatsAHeadhunter.Detection;

namespace WhatsAHeadhunter;

public sealed class HeadhunterSettingsUi
{
    private int _activeTab;
    private float _pulsePhase;

    private static readonly string[] Tabs = { "Status", "Detection", "Overlay", "HUD", "Belt Swap" };

    // Gold/amber palette for Headhunter theme
    private static uint Accent => Col(1f, 0.75f, 0.2f);
    private static uint AccentDim => Col(0.7f, 0.5f, 0.1f);
    private static uint DangerRed => Col(0.9f, 0.2f, 0.2f);
    private static uint SafeGreen => Col(0.2f, 0.8f, 0.3f);
    private static uint Label => Col(0.88f, 0.88f, 0.88f);
    private static uint Muted => Col(0.5f, 0.5f, 0.55f);
    private static uint CardBg => Col(0.06f, 0.06f, 0.08f);
    private const float Row = 32f;

    public void Draw(
        WhatsAHeadhunterSettings settings,
        StolenModTracker tracker,
        ShroudWalkerDetector detector)
    {
        var contentMin = ImGui.GetCursorScreenPos();
        float contentW = ImGui.GetContentRegionAvail().X;
        var dl = ImGui.GetWindowDrawList();

        // Tab bar
        float tabW = contentW / Tabs.Length;
        for (int i = 0; i < Tabs.Length; i++)
        {
            var tMin = new Vector2(contentMin.X + i * tabW, contentMin.Y);
            var tMax = new Vector2(contentMin.X + (i + 1) * tabW, contentMin.Y + 28f);

            ImGui.SetCursorScreenPos(tMin);
            ImGui.InvisibleButton($"##hh_tab_{i}", tMax - tMin);
            if (ImGui.IsItemClicked()) _activeTab = i;

            bool active = i == _activeTab;
            if (active)
                dl.AddLine(new Vector2(tMin.X, tMax.Y - 1), new Vector2(tMax.X, tMax.Y - 1), Accent, 2f);

            CenterText(dl, Tabs[i], (tMin + tMax) * 0.5f, active ? Accent : Muted);
        }

        ImGui.SetCursorScreenPos(new Vector2(contentMin.X, contentMin.Y + 34f));
        ImGui.BeginChild("##hh_content", new Vector2(contentW, ImGui.GetContentRegionAvail().Y - 4));

        switch (_activeTab)
        {
            case 0: DrawStatusTab(dl, tracker, detector, settings); break;
            case 1: DrawDetectionTab(settings); break;
            case 2: DrawOverlayTab(settings); break;
            case 3: DrawHudTab(settings); break;
            case 4: DrawBeltSwapTab(settings, detector); break;
        }

        ImGui.EndChild();
    }

    private void DrawStatusTab(
        ImDrawListPtr dl,
        StolenModTracker tracker,
        ShroudWalkerDetector detector,
        WhatsAHeadhunterSettings settings)
    {
        ImGui.Spacing();

        // Stolen mods status
        if (tracker.HasStolenMods)
        {
            StatusCard(dl, "Headhunter Active", Accent,
                $"Buffs: {tracker.TotalBuffCount} from {tracker.ActiveGroups.Count} kills");

            // Companion buffs
            if (tracker.ActiveCompanionBuffs.Count > 0)
            {
                ImGui.Spacing();
                ImGui.TextColored(ToVec(Muted), "Companion Effects:");
                foreach (var kvp in tracker.ActiveCompanionBuffs)
                {
                    var name = string.IsNullOrEmpty(kvp.Value) ? kvp.Key : kvp.Value;
                    ImGui.BulletText(name);
                }
            }
        }
        else
        {
            StatusCard(dl, "No Stolen Mods", Muted, "Kill rare monsters with Headhunter equipped");
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Shroud Walker status
        if (detector.IsActive)
        {
            _pulsePhase += 0.02f;
            var pulse = (float)(Math.Sin(_pulsePhase * Math.PI * 2) * 0.3 + 0.7);
            var dangerColor = Col(0.9f * pulse, 0.15f, 0.15f);

            StatusCard(dl, "!! SHROUD WALKER DETECTED !!", dangerColor,
                $"Teleports: {detector.TeleportCount} | " +
                $"Expires: {detector.ShroudWalkerTimeRemaining:F0}s");

            // Readiness indicator
            ImGui.Spacing();
            var readiness = detector.ReadinessFraction;
            var barMin = ImGui.GetCursorScreenPos();
            float barW = ImGui.GetContentRegionAvail().X;
            float barH = 20f;
            var barMax = barMin + new Vector2(barW, barH);

            dl.AddRectFilled(barMin, barMax, Col(0.1f, 0.1f, 0.12f));
            if (readiness > 0)
            {
                var fillMax = barMin + new Vector2(barW * readiness, barH);
                var fillColor = readiness < 0.5f
                    ? LerpCol(SafeGreen, Col(0.9f, 0.8f, 0.1f), readiness * 2f)
                    : LerpCol(Col(0.9f, 0.8f, 0.1f), DangerRed, (readiness - 0.5f) * 2f);
                dl.AddRectFilled(barMin, fillMax, fillColor);
            }
            dl.AddRect(barMin, barMax, Col(0.3f, 0.3f, 0.35f));

            var label = detector.TeleportReady ? "TELEPORT READY" : $"Next teleport: {detector.SmokeMineTimeRemaining:F1}s";
            CenterText(dl, label, (barMin + barMax) * 0.5f, Label);

            ImGui.Dummy(new Vector2(0, barH + 4));
        }
        else
        {
            _pulsePhase = 0f;
            StatusCard(dl, "Shroud Walker: Not Detected", SafeGreen, "Safe to play");
        }
    }

    private void DrawDetectionTab(WhatsAHeadhunterSettings settings)
    {
        ImGui.Spacing();
        ToggleRow("Pre-scan Monster Mods", settings.Detection.PreScanMonsters,
            "Scan rare monsters for Shroud Walker before you kill them");
        ToggleRow("Track Smoke Mine Cooldown", settings.Detection.TrackSmokeMine,
            "Calculate teleport readiness from smoke mine buff timing");
    }

    private void DrawOverlayTab(WhatsAHeadhunterSettings settings)
    {
        ImGui.Spacing();
        ToggleRow("World Overlay", settings.Overlay.Enabled, "Draw circles on dangerous rares");
        ToggleRow("Show All Rares", settings.Overlay.ShowAllRares, "Mark all rares, not just Shroud Walker");
        ToggleRow("Text Labels", settings.Overlay.ShowLabel, "Show mod name on rares");
        SliderRow("Circle Radius", settings.Overlay.CircleRadius, "px");
        SliderRow("Label Font Size", settings.Overlay.LabelFontSize, "px");

        ImGui.Spacing();
        ToggleRow("Minimap Icons", settings.Minimap.Enabled, "Show markers on minimap");
        SliderRow("Icon Size", settings.Minimap.IconSize, "px");
    }

    private void DrawHudTab(WhatsAHeadhunterSettings settings)
    {
        ImGui.Spacing();
        ToggleRow("Enable HUD", settings.Hud.Enabled, "Show status bars on screen");
        SliderRow("Position X", settings.Hud.X, "px");
        SliderRow("Position Y", settings.Hud.Y, "px");
        SliderRow("Bar Width", settings.Hud.BarWidth, "px");
        SliderRow("Bar Height", settings.Hud.BarHeight, "px");

        ImGui.Spacing();
        ToggleRow("Buff Counter", settings.Hud.ShowBuffCounter, "Show stolen mod count");
        ToggleRow("Readiness Bar", settings.Hud.ShowReadinessBar, "Show teleport readiness progress");
        ToggleRow("Expiry Timer", settings.Hud.ShowExpiryTimer, "Show time until Shroud Walker expires");
    }

    private void DrawBeltSwapTab(WhatsAHeadhunterSettings settings, ShroudWalkerDetector detector)
    {
        ImGui.Spacing();

        var mode = (BeltSwapMode)settings.BeltSwap.Mode.Value;
        ImGui.TextColored(ToVec(Label), "Belt Swap Mode");
        ImGui.SameLine(200);
        string[] modeNames = { "Disabled", "Key Press", "Automatic" };
        int modeInt = settings.BeltSwap.Mode.Value;
        if (ImGui.Combo("##beltmode", ref modeInt, modeNames, modeNames.Length))
            settings.BeltSwap.Mode.Value = modeInt;

        ImGui.Spacing();

        if (mode == BeltSwapMode.KeyPress)
        {
            ImGui.TextColored(ToVec(Muted), "Press the hotkey to unequip and re-equip your belt,");
            ImGui.TextColored(ToVec(Muted), "clearing all Headhunter buffs. Only works when");
            ImGui.TextColored(ToVec(Muted), "Shroud Walker is detected.");
        }
        else if (mode == BeltSwapMode.Automatic)
        {
            ImGui.TextColored(ToVec(DangerRed), "WARNING: Auto mode will swap your belt immediately");
            ImGui.TextColored(ToVec(DangerRed), "when Shroud Walker is detected. Use with caution.");
        }

        ImGui.Spacing();
        SliderRow("Swap Cooldown", settings.BeltSwap.SwapCooldownMs, "ms");
        SliderRow("Click Delay", settings.BeltSwap.ClickDelayMs, "ms");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Status
        var canSwap = detector.IsActive && mode != BeltSwapMode.Disabled;
        var statusColor = canSwap ? SafeGreen : Muted;
        var statusText = canSwap ? "Ready to swap" : "Inactive (no Shroud Walker or disabled)";
        ImGui.TextColored(ToVec(statusColor), statusText);
    }

    // Helpers

    private static void StatusCard(ImDrawListPtr dl, string title, uint titleColor, string detail)
    {
        var pos = ImGui.GetCursorScreenPos();
        float w = ImGui.GetContentRegionAvail().X;
        float h = 48f;

        dl.AddRectFilled(pos, pos + new Vector2(w, h), CardBg, 4f);
        dl.AddRect(pos, pos + new Vector2(w, h), titleColor, 4f);

        dl.AddText(pos + new Vector2(8, 6), titleColor, title);
        dl.AddText(pos + new Vector2(8, 26), Muted, detail);

        ImGui.Dummy(new Vector2(0, h + 4));
    }

    private static void ToggleRow(string label, ExileCore.Shared.Nodes.ToggleNode node, string tooltip)
    {
        bool val = node.Value;
        ImGui.TextColored(ToVec(Label), label);
        ImGui.SameLine(280);
        if (ImGui.Checkbox($"##{label}", ref val))
            node.Value = val;
        if (!string.IsNullOrEmpty(tooltip) && ImGui.IsItemHovered())
            ImGui.SetTooltip(tooltip);
    }

    private static void SliderRow(string label, ExileCore.Shared.Nodes.RangeNode<int> node, string suffix)
    {
        int val = node.Value;
        ImGui.TextColored(ToVec(Label), label);
        ImGui.SameLine(200);
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 40);
        if (ImGui.SliderInt($"##{label}", ref val, node.Min, node.Max))
            node.Value = val;
        ImGui.SameLine();
        ImGui.TextColored(ToVec(Muted), suffix);
    }

    private static void SliderRow(string label, ExileCore.Shared.Nodes.RangeNode<float> node, string suffix)
    {
        float val = node.Value;
        ImGui.TextColored(ToVec(Label), label);
        ImGui.SameLine(200);
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 40);
        if (ImGui.SliderFloat($"##{label}", ref val, node.Min, node.Max))
            node.Value = val;
        ImGui.SameLine();
        ImGui.TextColored(ToVec(Muted), suffix);
    }

    private static void CenterText(ImDrawListPtr dl, string text, Vector2 center, uint color)
    {
        var sz = ImGui.CalcTextSize(text);
        dl.AddText(center - sz * 0.5f, color, text);
    }

    private static uint Col(float r, float g, float b, float a = 1f)
        => ImGui.GetColorU32(new Vector4(r, g, b, a));

    private static uint LerpCol(uint a, uint b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        var av = ImGui.ColorConvertU32ToFloat4(a);
        var bv = ImGui.ColorConvertU32ToFloat4(b);
        return ImGui.GetColorU32(new Vector4(
            av.X + (bv.X - av.X) * t,
            av.Y + (bv.Y - av.Y) * t,
            av.Z + (bv.Z - av.Z) * t,
            av.W + (bv.W - av.W) * t));
    }

    private static Vector4 ToVec(uint col) => ImGui.ColorConvertU32ToFloat4(col);
}
