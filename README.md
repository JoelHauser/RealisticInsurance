# Realistic Insurance

Replaces SPT's flat per-trader insurance return chance with a model built from
**who killed you**, **how good they were**, and **whether they got out with your
gear** — then decides which specific items they walked off with by value.

Server mod for SPT 4.0.x (built and verified against 4.0.13).

## Status

**0.0.92 — the 4.0.x backport. Builds against 4.0.13, not yet run on a 4.0 server.**

The full pipeline has been observed end to end on a live **4.1** server: the
raid-end capture, killer classification, the PMC level lookup, persistence of
that context across a server restart, the per-package plan, the per-item
verdicts, and the returned mail. Every hook point it relies on has an identical
signature on 4.0, so the same behaviour is expected here - but expected is not
observed, and nobody has yet run this build on 4.0.

Observed working, per killer type:

| killer | seen | result |
|---|---|---|
| PMC | level 73 resolved from SPT's bot cache | took nothing at competence 100 (see below) |
| player scav | distinguished from an AI scav correctly | classified, package stamped |
| boss | no level, fell back to `meanWhenBoss` | **took 2 of 4 kit items, 32 of 53 entries** |

The boss raid is the one that demonstrates the whole design: he took a kitted
SCAR-H and an Osprey carrier loaded with Hesco plates - roughly 1.4 million
roubles - and walked past an 80,371 RUB SKS and an 8,514 RUB helmet. Every
attachment travelled with its parent.

Two bugs were caught by testing rather than by reasoning, and both are fixed:

- Selection worked on database entries rather than kit items, so taking the
  three most valuable entries meant taking your weapon, rig and helmet *and
  everything attached to them*. An entire 62-item package was destroyed while
  the log cheerfully reported "took 3".
- The competence slopes could cancel the base exactly, producing absolutes: a
  competence-100 PMC computed to 0% and ignored a 291,890 RUB armour plate,
  while a competence-100 player scav computed to 100% and took every item.
  `minFractionTaken` / `maxFractionTaken` now bound both ends.

Not yet observed in game: AI scav kills (distinct from player scavs),
environmental deaths, wildcard raids, packages insured before installing, and
modded traders. All are handled in code and reasoned through, none have been
watched running.

Note this mod **replaces per-trader return chances entirely**, including any set
by trader mods. That is deliberate — the model is built on who killed you, not on
which trader you insured with — but if you value a custom trader's own insurance
rate, `traderModifierPercent` is the way to express it.

## How it works

SPT rolls each insured item in `InsuranceController.RollForDelete`, comparing a
uniform 0–99 roll against a flat percentage from `insurance.json`
(85% Prapor / 95% Therapist by default). This mod replaces that percentage with:

```
returnChance  = base[killerType]
              + looterDiedBonus     (only when the killer did NOT extract)
              + traderModifier      (optional)
              clamped to 0-100

fractionTaken = (1 - returnChance/100)
              + (competence - 50) * greed.perCompetencePoint[killerType]

itemsTaken    = round(fractionTaken * itemCount * jitter)
                picked weighted by price ^ greedBias
```

**Killer type** comes from the post-raid aggressor. `Side` gives the broad class
(`Usec`/`Bear` = PMC, `Savage` = scav) and `Role` splits bosses, guards, raiders,
rogues and cultists into their own bucket.

Player scavs are separated from AI scavs, because they behave nothing alike. A
real player on a scav run carries a player identity even though its in-raid name
is a generated scav name:

| | player scav | AI scav |
|---|---|---|
| `MainProfileNickname` | `"LeaveAMark"` | `null` |
| `Category` | `"UniqueId"` | `"Default"` |
| `Role` / `Name` | indistinguishable | indistinguishable |

### Competence and greed are different traits

Conflating them is wrong. A skilled PMC is skilled **and picky** - already kitted,
takes only upgrades. A skilled player scav is skilled **and greedy** - arrived with
nothing, takes everything. Both extract more often, but they empty your corpse to
very different degrees.

So competence drives *extraction odds* for everyone, while its effect on *how much
is taken* carries a per-killer sign:

```
fractionTaken = (1 - returnChance/100) + (competence - 50) * greed.perCompetencePoint[killer]
```

Fraction of kit taken, when the looter extracted:

| killer | comp 20 | comp 40 | comp 60 | comp 85 |
|---|---|---|---|---|
| pmc | 72% | 54% | 36% | 13% |
| playerScav | 28% | 46% | 64% | 86% |
| scav | 37% | 29% | 21% | 11% |
| boss | 87% | 69% | 51% | 28% |

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

Competence raises the odds they extracted:

```
extractChance += (competence - 50) * extractPerCompetencePoint
```

and shifts how much they took, signed per killer, as described above.

**Level is only available for PMC killers.** SPT caches `pmcUSEC`/`pmcBEAR`
bots (and only those) in `MatchBotDetailsCacheService`; the post-raid Aggressor
block carries no level at all. Scavs, bosses and unknown killers use the
`meanWhen*` values instead.

### Value-weighted looting — why it feels random

The real reason vanilla feels samey is not the percentage, it is that every
item is an **independent** roll at the same rate. Independent rolls concentrate:
12 items at 55% lands in the 40–60% band nearly 60% of the time, so you get
"about half my stuff back" over and over.

So instead the mod works in **kit items**, not database entries. A 34-entry
package is usually only ~5 real objects - a weapon and its 12 attachments, a rig
and its 14 inserts, a helmet, a backpack - and taking a weapon takes everything
bolted to it. Each kit item is priced *including* its attachments and gets its
own roll, scaled so the expected loss matches the configured fraction while the
actual count varies.

Measured against two real insurance packages (4k trials each):

| package | killer | 0 lost | 1 | 2 | 3 | avg returned |
|---|---|---|---|---|---|---|
| 49 entries / 6 kit items | PMC, competence 87 | 61.8% | 32.2% | 5.7% | 0.2% | 43 / 49 |
| 34 entries / 5 kit items | player scav, competence 37 | 9.4% | 73.5% | 16.1% | 0.9% | 19 / 34 |

A picky PMC usually walks past or takes one thing, and occasionally takes three.
A player scav nearly always takes something. The count is never fixed.

Neither end is ever absolute. Before `minFractionTaken`/`maxFractionTaken`
existed, a competence-100 PMC computed to exactly 0% and walked past a 291,890
RUB armour plate, while a competence-100 player scav computed to exactly 100%
and took every last item. Both were observed in testing.

**All insured gear is covered**, not just what was equipped. `RollForDelete` is
reached from both the regular-item and attachment paths.

## Config

`config/config.json`:

| Key | Meaning |
|---|---|
| `enabled` | Master switch. `false` leaves SPT's vanilla behaviour completely untouched |
| `greed.enabled` | `false` makes the amount taken depend only on the base rate, ignoring competence |
| `baseReturnChancePercent.pmc` | Base return chance when killed by a PMC (AI or player) |
| `baseReturnChancePercent.playerScav` | Base return chance when killed by a **player** scav |
| `baseReturnChancePercent.scav` | Base return chance when killed by an **AI** scav |
| `baseReturnChancePercent.boss` | Bosses, followers, cultists, Zryachiy, raiders/rogues |
| `baseReturnChancePercent.other` | No killer recorded — see edge cases |
| `looterCompetence.sigma` | Spread of the competence roll. Higher = level predicts less |
| `looterCompetence.wildcardChancePercent` | Raids that ignore level entirely and draw flat 0-100 |
| `looterCompetence.competencePerLevel` | How much each PMC level shifts the mean |
| `looterCompetence.meanWhenScav` / `Boss` / `Other` | Means used when no level exists |
| `looterCompetence.extractPerCompetencePoint` | Extract % per competence point above 50 |
| `greed.perCompetencePoint.*` | Fraction-of-kit taken per competence point above 50, **signed per killer** — negative = picky, positive = greedy. The main spread dial |
| `valueWeightedLooting.greedBias` | Price exponent when choosing what to take. 1 = proportional, higher = greedier |
| `valueWeightedLooting.countJitter` | Random wobble on how many items were taken |
| `valueWeightedLooting.minFractionTaken` | Floor, so no looter is ever perfectly disciplined — a chad still lifts a good plate to sell |
| `valueWeightedLooting.maxFractionTaken` | Ceiling, so something always survives |
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
- **Modded traders that offer insurance** work fine: the return chance is keyed
  by *killer type*, not by trader, and `traderModifierPercent` is an optional
  per-trader tweak that simply defaults to 0 for traders it does not list.
  Note that vanilla SPT indexes `insurance.json` `returnChancePercent[traderId]`
  directly, so a modded trader that enables insurance without registering there
  throws `KeyNotFoundException` in SPT itself. This mod never hands such a trader
  back to SPT, and logs a warning at startup naming any trader in that state.
- **Labs / Labyrinth**: SPT wipes the returned item list *after* this mod has
  already chosen what was taken, so you correctly get nothing back - but the log
  will still show a plan that was then discarded. Do not use those maps to verify
  tuning. The gate is conditional: it only fires while the location's `Insurance`
  flag is false.
- **Packages insured before installing** carry no killer data. By default they
  fall back to SPT's flat chance.
- Insurance returns run on a timer (`runIntervalSeconds`, default 600s) and can
  span a server restart, so the killer data is persisted into the profile
  rather than held in memory.

## Testing it yourself

Insurance returns are slow by design, so to watch the mod work without waiting
overnight, temporarily set in `SPT/SPT_Data/configs/insurance.json`:

```json
"returnTimeOverrideSeconds": 60,
"runIntervalSeconds": 30,
```

and `"logRolls": true` in this mod's config. Packages then return about a minute
after the raid, and the console prints the decision plus every item as `LOST` or
`kept` with its price. Put both settings back afterwards (`0` and `600`) — the
override applies to every trader, so it collapses custom traders' return times
too.

Note the override only applies when a package is **created**, so it cannot
speed up insurance that is already pending.

## Building

```
dotnet build -c Release -p:SPTPath="H:\SPT2026"
```

Add `-p:DeployToSPT=true` to copy straight into
`SPT\user\mods\RealisticInsurance`.

Compiled against the assemblies shipped in the 4.0.x install. Requires a
.NET 9 SDK - the 4.1.x branch targets .NET 10 and will not build here.
