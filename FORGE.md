SPT gives every insured item the same fixed chance of coming back, no matter what happened to you. A scav who killed you thirty seconds in and a boss who took you apart at the end of the raid produce the same result.

**Realistic Insurance** decides it from the raid instead: who killed you, how good they were, and whether they lived to carry your gear out.

| Killed by | You get back |
|---|---|
| PMC | ~55% — already geared, takes only upgrades |
| Player scav | ~45% — came in with nothing, leaves with everything |
| AI scav | ~75% — grabs one thing, often dies with it |
| Boss or raider | ~40% — thorough, and rarely dies |
| Nobody | ~90% — bleeding, falling, mines |

Level plays into it too, loosely: a level 45 can still be worse than a level 10.

**Your kit stays together.** A rifle and its optic and suppressor are one item, and so are a rig and the plates in it. You lose the weapon *built*, or you get it back *built* — never a pile of orphaned mounts.

**Valuable gear goes first.** In one test raid a boss walked off with a kitted SCAR-H and a plated Osprey, and left an SKS and a helmet behind.

**Gear you dropped is safe.** A helmet you stashed in a bush twenty minutes before you died was never seen by whoever killed you, so it returns at your trader's normal rate instead.

## Details {.tabset}

### Installing

Two downloads: **0.2.0** for SPT 4.1.x, **0.0.92** for SPT 4.0.x. They are not interchangeable.

1. Stop the server.
2. Extract the archive into your SPT folder.
3. Start the server. **Realistic Insurance** appears in the mod list, and *Realistic Insurance (client)* in the BepInEx console.

The archive holds two files — a server mod and a small client plugin that reports what you were still carrying when you died. Extracting puts both where they belong.

`simulateItemsBeingTaken` must be `true` in `configs/insurance.json`. It is by default, and the server warns you at startup if it is not.

### Settings

Everything lives in `config/config.json`. The defaults are tuned — you do not need to touch any of this.

| Setting | Effect |
|---|---|
| `baseReturnChancePercent` | The headline numbers in the table above |
| `traderModifierPercent` | Per-trader nudge, e.g. `+10` for Therapist |
| `greed.perCompetencePoint` | How much a looter's skill changes what they take |
| `valueWeightedLooting.greedBias` | How strongly they favour expensive gear |
| `logRolls` | Prints every decision to the console. Off by default |

`enabled: false` turns the mod off without uninstalling it.

Settings are read when a raid ends, so changing them will not affect insurance already on its way back.

### Other trader mods

Custom traders need no setup at all. Install the trader, install this mod, done.

> **Worth knowing** — this replaces the return chance for *every* trader, including custom ones. A trader advertising 95% will behave like any other, because what you get back depends on who killed you rather than who you insured with. `traderModifierPercent` gives a trader some of its character back if you want it.

Return times and insurance prices are untouched.

{.endtabset}

## Status

**Tested in game and working.** Confirmed across live raids: PMC, player scav and boss kills, dropped gear, and returns surviving a server restart.

Not yet seen running: AI scav kills, environmental deaths, and custom traders — all handled in code, none watched in a raid. The 4.0.x build is compiled and checked against 4.0.13, with the in-game testing done on 4.1.

Bug reports are welcome. Set `"logRolls": true`, restart, and include the console output — it says exactly what the mod decided and why.
