using System;
using System.Collections.Generic;
using ExileCore.PoEMemory.Components;

namespace WhatsAHeadhunter.Detection;

public class ShroudWalkerDetector
{
    private const string SmokeMineSpeedBuff = "smoke_mine_movement_speed";

    private bool _smokeMineWasActive;
    private bool _firstUpdate = true;
    private int _teleportCount;

    public bool IsActive { get; private set; }
    public bool TeleportReady { get; private set; }
    public float SmokeMineTimeRemaining { get; private set; }
    public float ReadinessFraction { get; private set; }
    public float ShroudWalkerTimeRemaining { get; private set; }
    public int TeleportCount => _teleportCount;

    public void Update(List<Buff>? buffs, StolenModTracker tracker)
    {
        if (buffs == null || !tracker.HasStolenMods)
        {
            if (IsActive)
            {
                IsActive = false;
                TeleportReady = false;
                ReadinessFraction = 0f;
                ShroudWalkerTimeRemaining = 0f;
                SmokeMineTimeRemaining = 0f;
            }
            return;
        }

        // Find smoke mine buff
        float smokeMineTimer = 0f;
        float smokeMineMaxTime = 0f;
        bool smokeMineActive = false;
        for (int i = 0; i < buffs.Count; i++)
        {
            if (buffs[i]?.Name == SmokeMineSpeedBuff)
            {
                smokeMineActive = true;
                smokeMineTimer = buffs[i].Timer;
                smokeMineMaxTime = buffs[i].MaxTime;
                break;
            }
        }

        // On first update, just record current state to avoid false edge
        if (_firstUpdate)
        {
            _smokeMineWasActive = smokeMineActive;
            _firstUpdate = false;
        }

        // Detect new teleport: smoke mine buff just appeared
        if (smokeMineActive && !_smokeMineWasActive)
        {
            _teleportCount++;
            if (!IsActive)
                IsActive = true;
        }
        _smokeMineWasActive = smokeMineActive;

        // Update readiness and expiry
        if (IsActive)
        {
            if (smokeMineActive)
            {
                // Buff active: show countdown until next teleport is possible
                SmokeMineTimeRemaining = smokeMineTimer;
                ReadinessFraction = 1f - (smokeMineTimer / Math.Max(smokeMineMaxTime, 0.1f));
                TeleportReady = false;
            }
            else
            {
                // Buff expired: teleport can happen any time
                SmokeMineTimeRemaining = 0f;
                ReadinessFraction = 1f;
                TeleportReady = true;
            }

            // Use the newest stolen mod group timer as expiry
            var newestGroup = tracker.GetMostRecentGroup();
            ShroudWalkerTimeRemaining = newestGroup?.Timer ?? 0f;

            if (ShroudWalkerTimeRemaining <= 0f || !tracker.HasStolenMods)
            {
                IsActive = false;
                TeleportReady = false;
                ReadinessFraction = 0f;
                ShroudWalkerTimeRemaining = 0f;
                SmokeMineTimeRemaining = 0f;
            }
        }
    }

    public void Reset()
    {
        IsActive = false;
        TeleportReady = false;
        ReadinessFraction = 0f;
        ShroudWalkerTimeRemaining = 0f;
        SmokeMineTimeRemaining = 0f;
        _smokeMineWasActive = false;
        _firstUpdate = true;
        _teleportCount = 0;
    }
}
