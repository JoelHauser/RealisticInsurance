# Realistic Insurance

Replaces SPT's flat per-trader insurance return chance with a model built from
**who killed you**, **how good they were**, and **whether they got out with your
gear** — then decides which specific items they walked off with by value.

Server mod for SPT 4.1.x.

## How it works

SPT rolls each insured item in `InsuranceController.RollForDelete`, comparing a
uniform 0–99 roll against a flat percentage from `insurance.json`
(85% Prapor / 95% Therapist by default). This mod replaces that percentage with:

```
returnChance = base[killerType]
             + (competence - 50) * returnPerCompetencePoint
             + looterDiedBonus      (only when the killer did NOT extract)
             + traderModifier       (optional)
             clamped to 0-100

itemsTaken   = round((1 - returnChance/100) * itemCount * jitter)
             picked weighted by price ^ greedBias
```

**Killer type** comes from the post-raid aggressor, using `Side` (`Usec`/`Bear`
= PMC, `Savage` = scav) with `Role` used only to split bosses and their
followers into their own bucket.

**Looter extraction is rolled once per raid, not per item.** The killer either
escaped with your kit or didn't — rolling per item would let one corpse both
escape and not escape.

### Looter competence — level is a hint, not a rule

A high-level PMC is *usually* pickier (takes less) and *usually* better at
getting out. But a level 50 can be terrible, and a level 10 can be a monster.
So level does not set the outcome — it sets the **mean of a distribution**:

```
mean       = competenceAtPivot + (level - pivotLevel) * competencePerLevel
competence = clamp(Normal(mean, sigma), 0, 100)      rolled ONCE per raid
```

`sigma` is the "level doesn't mean much" dial. At the shipped 22, a level 45
and a level 10 overlap constantly. On top of that, `wildcardChancePercent` of
raids ignore level entirely and draw competence flat 0–100 — the level 50
hatchling runner and the level 8 prodigy.

Competence then moves both factors in opposite directions:

```
returnChance  += (competence - 50) * returnPerCompetencePoint    (they take less)
extractChance += (competence - 50) * extractPerCompetencePoint   (they get out)
```

**Level is only available for PMC killers.** SPT caches `pmcUSEC`/`pmcBEAR`
bots (and only those) in `MatchBotDetailsCacheService`; the post-raid Aggressor
block carries no level at all. Scavs, bosses and unknown killers use the
`meanWhen*` values instead.

### Value-weighted looting — why it feels random

The real reason vanilla feels samey is not the percentage, it is that every
item is an **independent** roll at the same rate. Independent rolls concentrate:
12 items at 55% lands in the 40–60% band nearly 60% of the time, so you get
"about half my stuff back" over and over.

So instead of rolling per item, the mod decides **how many** items were taken
once per raid, then picks **which ones** weighted by `price ^ greedBias`. The
count varies per raid, and the expensive kit goes first — losing your gun and
rig but keeping ammo reads as being looted, rather than as dice.

Measured over 15k simulated raids at 12 insured items, level 25:

| `returnPerCompetencePoint` | mean returned | spread (sd) | cleaned out (0–20%) | barely touched (80–100%) |
|---|---|---|---|---|
| 0.35 | 64% | 12.9 | 0.0% | 13.8% |
| **0.9 (default)** | **64%** | **20.5** | **2.2%** | **24.4%** |
| 1.2 | 63% | 25.0 | 5.7% | 28.9% |
| *vanilla SPT (flat 85%)* | *85%* | *10.3* | *0.0%* | *73.6%* |

Level still reads through at the default — mean returned is 52% at level 10,
64% at 25, 78% at 45 — it just no longer *determines* the outcome.

**All insured gear is covered**, not just what was equipped. `RollForDelete` is
reached from both the regular-item and attachment paths.

## Config

`config/config.json`:

| Key | Meaning |
|---|---|
| `baseReturnChancePercent.pmc` | Base return chance when killed by a PMC (AI or player) |
| `baseReturnChancePercent.playerScav` | Base return chance when killed by a scav |
| `baseReturnChancePercent.boss` | Bosses, followers, cultists, Zryachiy, raiders/rogues |
| `baseReturnChancePercent.other` | No killer recorded — see edge cases |
| `looterCompetence.sigma` | Spread of the competence roll. Higher = level predicts less |
| `looterCompetence.wildcardChancePercent` | Raids that ignore level entirely and draw flat 0-100 |
| `looterCompetence.competencePerLevel` | How much each PMC level shifts the mean |
| `looterCompetence.meanWhenScav` / `Boss` / `Other` | Means used when no level exists |
| `looterCompetence.returnPerCompetencePoint` | Return % per competence point above 50 — **the main spread dial** |
| `looterCompetence.extractPerCompetencePoint` | Extract % per competence point above 50 |
| `valueWeightedLooting.greedBias` | Price exponent when choosing what to take. 1 = proportional, higher = greedier |
| `valueWeightedLooting.countJitter` | Random wobble on how many items were taken |
| `looterExtractedChancePercent` | Baseline chance the killer extracted, before competence |
| `looterDiedBonusPercent` | Added to the return chance when they did not extract |
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
