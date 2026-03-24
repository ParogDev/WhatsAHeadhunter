using System;
using System.Numerics;
using ExileCore;
using ExileCore.Shared;
using SharpDX;
using WhatsAHeadhunter.Detection;

namespace WhatsAHeadhunter.Rendering;

public class HudRenderer
{
    private float _pulsePhase;

    public void Render(
        Graphics graphics,
        WhatsAHeadhunterSettings settings,
        StolenModTracker tracker,
        ShroudWalkerDetector detector)
    {
        float x = settings.Hud.X.Value;
        float y = settings.Hud.Y.Value;
        float w = settings.Hud.BarWidth.Value;
        float h = settings.Hud.BarHeight.Value;
        float yOffset = 0f;

        // Buff counter
        if (settings.Hud.ShowBuffCounter.Value && tracker.HasStolenMods)
        {
            var rect = new RectangleF(x, y + yOffset, w, h);
            DrawBar(graphics, rect,
                $"HH Buffs: {tracker.TotalBuffCount} ({tracker.ActiveGroups.Count} kills)",
                1f,
                new Color(80, 40, 0, 200),
                new Color(200, 150, 50));
            yOffset += h + 2;
        }

        // Shroud Walker warning + readiness bar
        if (detector.IsActive)
        {
            // Warning bar
            var warningRect = new RectangleF(x, y + yOffset, w, h);
            var warningColor = new Color(200, 30, 30, 220);
            DrawBar(graphics, warningRect,
                "!! SHROUD WALKER ACTIVE !!",
                1f,
                warningColor,
                Color.Red);
            yOffset += h + 2;

            // Readiness bar
            if (settings.Hud.ShowReadinessBar.Value)
            {
                var readyRect = new RectangleF(x, y + yOffset, w, h);
                var fraction = detector.ReadinessFraction;

                // Color transitions: green -> yellow -> red
                Color fillColor;
                if (fraction < 0.5f)
                    fillColor = Color.Lerp(new Color(50, 180, 50), new Color(220, 200, 30), fraction * 2f);
                else
                    fillColor = Color.Lerp(new Color(220, 200, 30), new Color(220, 50, 30), (fraction - 0.5f) * 2f);

                // Pulse when ready
                if (detector.TeleportReady)
                {
                    _pulsePhase += 0.05f;
                    var pulse = (float)(Math.Sin(_pulsePhase * Math.PI * 2) * 0.3 + 0.7);
                    fillColor = new Color(
                        (int)(220 * pulse),
                        (int)(30 * pulse),
                        (int)(30 * pulse),
                        220);
                }
                else
                {
                    _pulsePhase = 0f;
                }

                var label = detector.TeleportReady
                    ? "TELEPORT READY"
                    : $"Teleport in: {(detector.SmokeMineInterval * (1f - fraction)):F1}s";

                DrawBar(graphics, readyRect, label, fraction,
                    new Color(30, 30, 30, 200), fillColor);
                yOffset += h + 2;
            }

            // Expiry timer
            if (settings.Hud.ShowExpiryTimer.Value && detector.ShroudWalkerTimeRemaining > 0)
            {
                var expiryRect = new RectangleF(x, y + yOffset, w, h);
                var expiryFrac = detector.ShroudWalkerTimeRemaining / 60f;
                DrawBar(graphics, expiryRect,
                    $"Expires in: {detector.ShroudWalkerTimeRemaining:F0}s",
                    expiryFrac,
                    new Color(30, 30, 30, 200),
                    new Color(100, 100, 180));
                yOffset += h + 2;
            }
        }
    }

    private static void DrawBar(
        Graphics graphics,
        RectangleF rect,
        string label,
        float fraction,
        Color bgColor,
        Color fillColor)
    {
        // Background
        graphics.DrawBox(rect, bgColor);

        // Fill
        if (fraction > 0f)
        {
            var fillRect = new RectangleF(rect.X, rect.Y, rect.Width * Math.Clamp(fraction, 0f, 1f), rect.Height);
            graphics.DrawBox(fillRect, fillColor);
        }

        // Border
        graphics.DrawFrame(rect, Color.Gray, 1);

        // Text
        var textPos = new Vector2(rect.X + 4, rect.Y + 2);
        graphics.DrawText(label, textPos, Color.White);
    }
}
