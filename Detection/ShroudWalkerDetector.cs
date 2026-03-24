using System;
using System.Collections.Generic;
using ExileCore.PoEMemory.Components;

namespace WhatsAHeadhunter.Detection;

public class ShroudWalkerDetector
{
    private const string SmokeMineSpeedBuff = "smoke_mine_movement_speed";

    private bool _smokeMineWasActive;
    private DateTime _lastTeleportAt;
    private DateTime _prevTeleportAt;
    private int _teleportCount;

    public bool IsActive { get; private set; }
    public bool TeleportReady { get; private set; }
    public float SmokeMineInterval { get; private set; } = 3f;
    public DateTime LastTeleportAt => _lastTeleportAt;
    public float ReadinessFraction { get; private set; }
    public float ShroudWalkerTimeRemaining { get; private set; }
    public int TeleportCount => _teleportCount;

    public void Update(List<Buff>? buffs, StolenModTracker tracker)
    {
        if (buffs == null || !tracker.HasStolenMods)
        {
            if (IsActive)
            {
                // Shroud Walker expired (all stolen mods gone)
                IsActive = false;
                TeleportReady = false;
                ReadinessFraction = 0f;
            }
            return;
        }

        bool smokeMineActive = false;
        for (int i = 0; i < buffs.Count; i++)
        {
            if (buffs[i]?.Name == SmokeMineSpeedBuff)
            {
                smokeMineActive = true;
                break;
            }
        }

        // Detect new teleport: smoke mine buff just appeared
        if (smokeMineActive && !_smokeMineWasActive)
        {
            OnTeleportDetected(tracker);
        }
        _smokeMineWasActive = smokeMineActive;

        // Update readiness
        if (IsActive)
        {
            var timeSince = (float)(DateTime.UtcNow - _lastTeleportAt).TotalSeconds;
            ReadinessFraction = Math.Clamp(timeSince / SmokeMineInterval, 0f, 1f);
            TeleportReady = ReadinessFraction >= 1f;

            // Update time remaining from the flagged group
            ShroudWalkerTimeRemaining = 0f;
            foreach (var group in tracker.ActiveGroups)
            {
                if (group.HasShroudWalker)
                {
                    ShroudWalkerTimeRemaining = group.Timer;
                    break;
                }
            }

            // If the flagged group expired, deactivate
            if (ShroudWalkerTimeRemaining <= 0f)
            {
                bool anyGroupFlagged = false;
                foreach (var group in tracker.ActiveGroups)
                {
                    if (group.HasShroudWalker)
                    {
                        anyGroupFlagged = true;
                        break;
                    }
                }
                if (!anyGroupFlagged)
                {
                    IsActive = false;
                    TeleportReady = false;
                }
            }
        }
    }

    private void OnTeleportDetected(StolenModTracker tracker)
    {
        _prevTeleportAt = _lastTeleportAt;
        _lastTeleportAt = DateTime.UtcNow;
        _teleportCount++;

        if (!IsActive)
        {
            // First detection: flag the most recent stolen mod group
            IsActive = true;
            var group = tracker.GetMostRecentGroup();
            if (group != null)
                group.HasShroudWalker = true;
        }

        // Calculate interval from consecutive teleports
        if (_prevTeleportAt != default)
        {
            var interval = (float)(_lastTeleportAt - _prevTeleportAt).TotalSeconds;
            if (interval > 0.5f && interval < 30f)
            {
                // Weighted average to smooth out jitter
                SmokeMineInterval = SmokeMineInterval * 0.3f + interval * 0.7f;
            }
        }

        ReadinessFraction = 0f;
        TeleportReady = false;
    }

    public void Reset()
    {
        IsActive = false;
        TeleportReady = false;
        ReadinessFraction = 0f;
        SmokeMineInterval = 3f;
        ShroudWalkerTimeRemaining = 0f;
        _smokeMineWasActive = false;
        _lastTeleportAt = default;
        _prevTeleportAt = default;
        _teleportCount = 0;
    }
}
