using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory.Components;

namespace WhatsAHeadhunter.Detection;

public class StolenModGroup
{
    public float Timer { get; set; }
    public float MaxTime { get; set; }
    public int ModCount { get; set; }
    public DateTime AcquiredAt { get; set; }
    public bool HasShroudWalker { get; set; }
}

public class StolenModTracker
{
    private const string StolenModsBuffName = "stolen_mods_buff";
    private const float TimerGroupTolerance = 0.5f;

    private static readonly HashSet<string> CompanionBuffNames = new()
    {
        "monster_aura_energy_shield",
        "monster_aura_speed",
        "cold_beam_retaliation",
        "archnemesis_bubble_shield",
        "archnemesis_time_bubble",
        "ailment_bearer_activation_buff",
    };

    public List<StolenModGroup> ActiveGroups { get; } = new();
    public Dictionary<string, string> ActiveCompanionBuffs { get; } = new();
    public bool HasStolenMods { get; private set; }
    public int TotalBuffCount { get; private set; }
    public float OldestBuffTimer { get; private set; }
    public float NewestBuffTimer { get; private set; }

    public void Update(List<Buff>? buffs)
    {
        ActiveGroups.Clear();
        ActiveCompanionBuffs.Clear();
        HasStolenMods = false;
        TotalBuffCount = 0;
        OldestBuffTimer = 0f;
        NewestBuffTimer = 0f;

        if (buffs == null) return;

        var stolenTimers = new List<float>();

        for (int i = 0; i < buffs.Count; i++)
        {
            var b = buffs[i];
            if (b == null) continue;

            if (b.Name == StolenModsBuffName)
            {
                stolenTimers.Add(b.Timer);
            }
            else if (CompanionBuffNames.Contains(b.Name))
            {
                ActiveCompanionBuffs[b.Name] = b.DisplayName;
            }
        }

        if (stolenTimers.Count == 0) return;

        HasStolenMods = true;
        TotalBuffCount = stolenTimers.Count;

        stolenTimers.Sort();
        OldestBuffTimer = stolenTimers[0];
        NewestBuffTimer = stolenTimers[stolenTimers.Count - 1];

        // Group by timer (same kill = timers within tolerance)
        float groupTimer = stolenTimers[0];
        int groupCount = 1;

        for (int i = 1; i < stolenTimers.Count; i++)
        {
            if (Math.Abs(stolenTimers[i] - groupTimer) <= TimerGroupTolerance)
            {
                groupCount++;
            }
            else
            {
                ActiveGroups.Add(new StolenModGroup
                {
                    Timer = groupTimer,
                    MaxTime = 60f,
                    ModCount = groupCount,
                    AcquiredAt = DateTime.UtcNow - TimeSpan.FromSeconds(60f - groupTimer),
                });
                groupTimer = stolenTimers[i];
                groupCount = 1;
            }
        }

        ActiveGroups.Add(new StolenModGroup
        {
            Timer = groupTimer,
            MaxTime = 60f,
            ModCount = groupCount,
            AcquiredAt = DateTime.UtcNow - TimeSpan.FromSeconds(60f - groupTimer),
        });
    }

    public StolenModGroup? GetMostRecentGroup()
    {
        return ActiveGroups.Count > 0 ? ActiveGroups[ActiveGroups.Count - 1] : null;
    }

    public void Reset()
    {
        ActiveGroups.Clear();
        ActiveCompanionBuffs.Clear();
        HasStolenMods = false;
        TotalBuffCount = 0;
    }
}
