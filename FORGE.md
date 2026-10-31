In SPT, every insured item has the same fixed chance of coming back, no matter how you died. **Realistic Insurance** decides it from what actually happened in your raid: who killed you, how good they were, and whether they got out with your gear.

## How it works

When you die, the mod works through three questions:

1. **Who killed you?** Each kind of killer starts from a different return chance.
2. **Did they get out?** A killer who dies before extracting takes nothing home, so more of your kit comes back. Better killers extract more often.
3. **What did they take?** Valuable gear goes first, and it goes whole. A rifle with its optic and suppressor is one item, and so is a rig with its plates. You get the weapon back built, or not at all.

| Killed by | Starting return chance | How they loot |
|---|---|---|
| PMC | 55% | Already geared. The better they are, the less they bother taking |
| Player scav | 45% | Came in with nothing. The better they are, the more they take |
| AI scav | 75% | Grabs a thing or two, often dies with it |
| Boss, guard or raider | 40% | Thorough, and rarely dies |
| Nobody (bleeding, falling, mines) | 90% | Nobody looted you |

A PMC's level hints at how good they are, but only loosely. A level 45 can still be worse than a level 10. No two raids come out exactly the same, and nothing is ever all or nothing: something is always taken, and something always survives.

**Gear you dropped is safe.** A backpack you stashed in a bush before you died was never seen by your killer, so it comes back at your trader's normal rate. If you survive the raid, everything you lost counts as dropped.

**The trader tells you what happened.** The insurance mail says how much came back, who took the rest and whether they got away, sometimes naming the item you will not see again. Prapor and Therapist each have their own voice. Messages are in English only.

## Details {.tabset}

### Installing

Pick the download for your SPT version: **0.2.2** for SPT 4.1.x, **0.0.94** for SPT 4.0.x. They are not interchangeable.

1. Stop the server.
2. Extract the archive into your SPT folder.
3. Start the server. **Realistic Insurance** appears in the server's mod list.

The archive holds a server mod and a small client plugin that notes what you were still carrying when you died. That is how the mod knows what you dropped. Without the plugin, everything else still works.

Insurance already on its way back when you install keeps SPT's normal return chance.

### Settings

Everything lives in `config/config.json` in the mod's folder. The defaults are tuned, so you do not need to change anything.

| Setting | Effect |
|---|---|
| `enabled` | `false` turns the mod off without uninstalling it |
| `baseReturnChancePercent` | The starting return chances in the table above |
| `traderModifierPercent` | Per-trader nudge, e.g. `10` for Therapist |
| `greed.perCompetencePoint` | How much a killer's skill changes what they take |
| `valueWeightedLooting.greedBias` | How strongly killers go for expensive gear |
| `traderMessages.enabled` | `false` keeps SPT's stock insurance mail |
| `logRolls` | Prints every decision to the server console |

Changes apply to raids that end after you save. Insurance already on its way back is not affected.

### Compatibility

Custom traders need no setup. This mod replaces the return chance for every trader, including custom ones, because what comes back depends on who killed you rather than who you insured with. Use `traderModifierPercent` to give a trader its own edge back.

Return times and insurance prices are not changed. Labs and the Labyrinth still return nothing, as in normal SPT.

`simulateItemsBeingTaken` must stay `true` in SPT's `configs/insurance.json`. It is by default, and the server warns you at startup if it is not.

### Uninstalling

Delete `user/mods/RealisticInsurance` from your server folder and `BepInEx/plugins/RealisticInsurance` from your game folder. Insurance already on its way back returns at SPT's normal rate.

{.endtabset}

## Reporting bugs

Set `"logRolls": true`, restart the server, and include the console output with your report. It shows exactly what the mod decided and why.
