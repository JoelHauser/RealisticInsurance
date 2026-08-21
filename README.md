# Realistic Insurance

Replaces SPT's flat per-trader insurance return chance with a two-factor model:
**who killed you**, and **whether they got out with your gear**.

Server mod for SPT 4.1.x.

## How it works

SPT rolls each insured item in `InsuranceController.RollForDelete`, comparing a
uniform 0–99 roll against a flat percentage from `insurance.json`
(85% Prapor / 95% Therapist by default). This mod replaces that percentage with:

```
chance = base[killerType]
       + looterDiedBonus      (only when the killer did NOT extract)
       + traderModifier       (optional)
       clamped to 0-100
```

**Killer type** comes from the post-raid aggressor, using `Side` (`Usec`/`Bear`
= PMC, `Savage` = scav) with `Role` used only to split bosses and their
followers into their own bucket.

**Looter extraction is rolled once per raid, not per item.** The killer either
escaped with your kit or didn't — rolling per item would let one corpse both
escape and not escape.

### Killer level (PMCs only)

A high-level PMC is pickier — takes less of your kit, but is far more likely to
walk out with what they did take. A low-level PMC grabs everything and then
frequently dies with it. Level therefore moves the two factors in **opposite**
directions:

```
returnChance  += (level - pivot) * returnChancePerLevel    (clamped)
extractChance += (level - pivot) * extractChancePerLevel   (clamped)
```

With the shipped defaults (base 55, pivot 25, +25 when the looter dies):

| Killer level | Extract chance | Return % if they extracted | Return % if they died |
|---|---|---|---|
| 5  | 45% | 43% | 68% |
| 25 | 65% | 55% | 80% |
| 45 | 85% | 67% | 92% |

**Level is only available for PMC killers.** SPT caches `pmcUSEC`/`pmcBEAR`
bots (and only those) in `MatchBotDetailsCacheService`; the post-raid Aggressor
block carries no level at all. Scav and boss kills use their flat buckets with
no level scaling, and a cache miss simply means no adjustment.

**Every insured item is rolled individually**, including things that were never
equipped. `RollForDelete` is reached from both the regular-item path and the
attachment path, so replacing it covers all insured gear.

## Config

`config/config.json`:

| Key | Meaning |
|---|---|
| `baseReturnChancePercent.pmc` | Base return chance when killed by a PMC (AI or player) |
| `baseReturnChancePercent.playerScav` | Base return chance when killed by a scav |
| `baseReturnChancePercent.boss` | Bosses, followers, cultists, Zryachiy, raiders/rogues |
| `baseReturnChancePercent.other` | No killer recorded — see edge cases |
| `looterExtractedChancePercent` | Chance the killer survived and extracted |
| `looterDiedBonusPercent` | Added to the base when they did not extract |
| `traderModifierPercent` | Optional per-trader adjustment, by trader ID |
| `legacyPackageBehaviour` | `spt` or `other` — how to treat packages insured before this mod was installed |
| `logRolls` | Log every roll to the server console |

## Edge cases

- **No aggressor** (environmental damage, bleeding out, falling, disconnect,
  MIA, or surviving but leaving gear behind) falls into the `other` bucket.
  Keep it high — nobody looted you.
- **Bosses, guards and cultists** get their own bucket rather than being lumped
  in with scavs.
- **`simulateItemsBeingTaken: false`** in `insurance.json` makes SPT skip the
  delete pass entirely, so nothing is ever lost and this mod does nothing. The
  mod logs a warning at startup if it sees this.
- **Labs / Labyrinth** insurance restrictions are handled by SPT before this
  mod's roll and are left alone.
- **Packages insured before installing** carry no killer data. By default they
  fall back to SPT's flat chance.
- Insurance returns run on a timer (`runIntervalSeconds`, default 600s) and can
  span a server restart, so the killer data is persisted into the profile
  rather than held in memory.

## Building

```
dotnet build -c Release -p:SPTPath="H:\SPT4.1.X"
```

Add `-p:DeployToSPT=true` to copy straight into
`SPT_Runtime\user\mods\RealisticInsurance`.

Compiled against the assemblies shipped in the install rather than the
`SPTarkov.*` NuGet packages, which are still on 4.1.2.
