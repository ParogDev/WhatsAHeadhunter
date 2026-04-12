using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using WhatsAHeadhunter.Automation;
using WhatsAHeadhunter.Detection;
using WhatsAHeadhunter.Rendering;

namespace WhatsAHeadhunter;

public class WhatsAHeadhunter : BaseSettingsPlugin<WhatsAHeadhunterSettings>
{
    private StolenModTracker _tracker = null!;
    private ShroudWalkerDetector _detector = null!;
    private WorldOverlay _worldOverlay = null!;
    private MinimapRenderer _minimapRenderer = null!;
    private HudRenderer _hudRenderer = null!;
    private BeltSwapController _beltSwap = null!;
    private HeadhunterSettingsUi _settingsUi = null!;

    private readonly Dictionary<uint, TrackedRare> _trackedRares = new();
    private readonly List<uint> _capturedRareIds = new(4);
    private const string CapturedBuffName = "capture_monster_trapped";

    // Cached per-frame data for Render
    private bool _canRender;
    private List<Buff>? _cachedBuffs;

    public override bool Initialise()
    {
        Name = "Whats A Headhunter";

        _tracker = new StolenModTracker();
        _detector = new ShroudWalkerDetector();
        _worldOverlay = new WorldOverlay();
        _minimapRenderer = new MinimapRenderer();
        _hudRenderer = new HudRenderer();
        _beltSwap = new BeltSwapController();
        _settingsUi = new HeadhunterSettingsUi();

        Input.RegisterKey(Settings.BeltSwap.Hotkey.Value);
        Settings.BeltSwap.Hotkey.OnValueChanged += () =>
            Input.RegisterKey(Settings.BeltSwap.Hotkey.Value);

        return true;
    }

    public override void EntityAdded(Entity entity)
    {
        if (!Settings.Detection.PreScanMonsters.Value) return;

        try
        {
            var omp = entity.GetComponent<ObjectMagicProperties>();
            if (omp == null) return;

            var rarity = omp.Rarity;
            if (rarity != MonsterRarity.Rare) return;

            var mods = omp.Mods;
            if (mods == null) return;

            bool hasShroudWalker = false;
            for (int i = 0; i < mods.Count; i++)
            {
                if (mods[i].Contains("ShroudWalker", StringComparison.OrdinalIgnoreCase))
                {
                    hasShroudWalker = true;
                    break;
                }
            }

            if (hasShroudWalker || Settings.Overlay.ShowAllRares.Value)
            {
                var render = entity.GetComponent<Render>();
                _trackedRares[entity.Id] = new TrackedRare
                {
                    Entity = entity,
                    HasShroudWalker = hasShroudWalker,
                    Name = render?.Name ?? "",
                    GridPos = entity.GridPosNum,
                    WorldPos = render?.PosNum ?? default,
                };
            }
        }
        catch
        {
            // Ignore entities that can't be read
        }
    }

    public override void EntityRemoved(Entity entity)
    {
        _trackedRares.Remove(entity.Id);
    }

    public override void AreaChange(AreaInstance area)
    {
        _trackedRares.Clear();
        _tracker.Reset();
        _detector.Reset();
        _beltSwap.Reset();
    }

    public override Job Tick()
    {
        _canRender = false;

        if (!GameController.InGame) return null;
        var player = GameController.Player;
        if (player == null || !player.IsAlive) return null;

        // Read buffs
        _cachedBuffs = player.GetComponent<Buffs>()?.BuffsList;

        // Check for grace period
        if (_cachedBuffs != null)
        {
            for (int i = 0; i < _cachedBuffs.Count; i++)
            {
                if (_cachedBuffs[i]?.Name == "grace_period")
                    return null;
            }
        }

        // Update detection systems
        _tracker.Update(_cachedBuffs);
        _detector.Update(_cachedBuffs, _tracker);

        // Update belt swap automation
        _beltSwap.Update(GameController, Settings, _detector);

        // Update tracked rare positions and detect captured beasts
        _capturedRareIds.Clear();
        foreach (var kvp in _trackedRares)
        {
            var rare = kvp.Value;
            if (!rare.IsValid) continue;

            // Check if Einhar captured this beast
            var buffs = rare.Entity.GetComponent<Buffs>()?.BuffsList;
            if (buffs != null)
            {
                for (int i = 0; i < buffs.Count; i++)
                {
                    if (buffs[i]?.Name == CapturedBuffName)
                    {
                        _capturedRareIds.Add(kvp.Key);
                        goto nextRare;
                    }
                }
            }

            var render = rare.Entity.GetComponent<Render>();
            if (render != null)
            {
                rare.GridPos = rare.Entity.GridPosNum;
                rare.WorldPos = render.PosNum;
            }
            nextRare:;
        }

        // Remove captured beasts from tracking
        for (int i = 0; i < _capturedRareIds.Count; i++)
        {
            _trackedRares.Remove(_capturedRareIds[i]);
        }

        _canRender = true;
        return null;
    }

    public override void Render()
    {
        if (!_canRender) return;
        if (!GameController.InGame) return;

        // Skip drawing if fullscreen panels are open
        var ingameUi = GameController.IngameState.IngameUi;
        if (ingameUi.FullscreenPanels.Any(x => x.IsVisible)) return;

        if (Settings.Overlay.Enabled.Value)
            _worldOverlay.Render(Graphics, GameController, Settings, _trackedRares);

        if (Settings.Minimap.Enabled.Value)
            _minimapRenderer.Render(Graphics, GameController, Settings, _trackedRares);

        if (Settings.Hud.Enabled.Value)
            _hudRenderer.Render(Graphics, Settings, _tracker, _detector);
    }

    public override void DrawSettings()
    {
        _settingsUi.Draw(Settings, _tracker, _detector);
    }
}
