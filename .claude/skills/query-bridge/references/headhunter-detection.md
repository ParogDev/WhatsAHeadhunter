# HeadHunter Detection Reference

Research findings from reverse-engineering HH stolen mods via the AI Bridge (2026-03-24).

## Stolen Mods Structure

All HeadHunter stolen mods appear as `stolen_mods_buff` with:
- `displayName`: "Headhunter" (same for all -- NOT distinguishable)
- `description`: "You have taken the mods of a slain Rare Monster for yourself"
- `maxTime`: 60.0 seconds
- `sourceSkillId`: 0 (no linked skill)
- `charges`: 1

Group buffs by matching `Timer` values (within 0.5s tolerance) to identify mods from the same rare kill. Each kill produces 1-4 stolen_mods_buff entries.

## Companion Buffs

Some stolen mods manifest as separate named buffs alongside the generic stolen_mods_buff:

| Buff Name | Display Name | Source Mod |
|-----------|-------------|------------|
| `monster_aura_energy_shield` | Energy Shield Aura | Discipline aura |
| `monster_aura_speed` | (speed aura) | Haste aura |
| `cold_beam_retaliation` | (ice retaliation) | Ice-Retaliation |
| `archnemesis_time_bubble` | Temporal Bubble | Temporal Bubble |
| `archnemesis_bubble_shield` | (bubble shield) | Bubble Shield |

These have `timer: 999999` (permanent) while their parent stolen_mods_buff is active.

## Shroud Walker (Ambushes) Detection

### What It Does
Shroud Walker teleports the player to nearby enemies via Smoke Mine on a ~3 second cooldown. It creates a smoke cloud at the destination. The mod is from the "Ambushes" prefix (family: `MonsterModShroudWalker`).

### Detection Methods (priority order)

**1. Monster Pre-Scan (proactive)**
Check `ObjectMagicProperties.Mods` on rare monsters for strings containing "ShroudWalker":
```csharp
var omp = entity.GetComponent<ObjectMagicProperties>();
bool hasShroudWalker = omp?.Mods?.Any(m =>
    m.Contains("ShroudWalker", StringComparison.OrdinalIgnoreCase)) ?? false;
```

**2. Smoke Mine Buff (reactive, 3s window)**
Watch for `smoke_mine_movement_speed` buff appearing while `stolen_mods_buff` is active:
```json
{
  "name": "smoke_mine_movement_speed",
  "displayName": "Faster Run",
  "description": "You have increased Movement Speed",
  "maxTime": 3.0,
  "sourceSkillId": 0
}
```
This appears for 3 seconds after each teleport. If the player does NOT have Smoke Mine as their own skill, this confirms Shroud Walker.

**Limitation**: This is reactive -- requires getting teleported at least once. If the player leaves range and returns, Smoke Mine fires instantly on its cooldown.

### What Does NOT Work
- **Player Stats**: `DisplayMonsterHasShroudWalkerDaemon` does NOT transfer via HH
- **ActorSkills**: No Smoke Mine skill appears in the player's skill list
- **Buff DisplayName**: All stolen_mods_buff share the same generic "Headhunter" name

## Buff Memory Offsets (research in progress)

Each buff instance has unexplored memory between offsets 0x4C and 0xF8:

| Offset | Content | Per-buff? |
|--------|---------|-----------|
| 0x58 | Varies (hinted as pointer to substructure) | Partially |
| 0x80-0x90 | StdVector with 1 int (unique per buff) | YES |
| 0x98 | **Table index** (upper 32 bits, sequential per kill) | YES |
| 0xD0-0xF0 | Various per-buff data | YES |

The index at 0x98 is sequential (same-kill buffs have adjacent indices) and likely references a game data table (ModsDat or similar). This could enable identifying specific stolen mods without companion buff detection, but requires further reverse-engineering.

## Character Stats Impact (observed)

With HH buffs active vs baseline:
- ES: 3,772 vs 2,506 (+1,266 from monster_aura_energy_shield)
- BaseEvasionRating: 1,729 vs 229 (+1,500)
- AttackSpeed: +12%
- CastSpeed: +12%
- Significant damage increases across all elements

## Skills Array

The `player` query includes a `skills[]` array with enhanced fields:
- `internalName`: Resolved via `EffectsPerLevel.SkillGemWrapper.ActiveSkill.InternalName`
- `displayName`: From `ActiveSkill.DisplayName`
- `isUserSkill`: false for granted skills (items/buffs)
- `isMine`: true for mine skills (relevant for Smoke Mine detection)

The `Name` property on ActorSkill has a bug that returns empty for granted skills. Always use `internalName` or `displayName` instead.
