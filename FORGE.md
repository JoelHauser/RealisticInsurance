SPT decides insurance returns with a single fixed percentage per trader. Every insured item rolls against that same number, so it makes no difference whether a scav killed you thirty seconds into the raid or a boss took you apart at the end of it. You get back roughly the same proportion of your kit every time.

**Realistic Insurance** bases the outcome on what actually happened in the raid.

Someone killed you, looted your body, and then either survived to extract or did not. A geared PMC takes your rifle and leaves the rest behind. An AI scav takes one valuable item and often dies with it, so your gear comes back anyway. A player scav takes as much as they can carry.

> **⚠️ Important** — this mod replaces the return chance for **every** trader, including custom traders added by other mods. What you get back is determined by who killed you, not by who you insured with. A setting is provided to restore some of each trader's individual character — see **Compatibility**.

## How it works {.tabset}

### Who killed you

Each type of killer has its own baseline:

| Killed by | You get back | Reasoning |
|---|---|---|
| PMC | ~55% | already geared; takes only what upgrades their kit |
| Player scav | ~45% | entered with nothing, leaves with as much as possible |
| AI scav | ~75% | takes little, and frequently dies before extracting |
| Boss or raider | ~40% | loots thoroughly and rarely dies |
| Nobody | ~90% | bleeding, falling, mines — nobody looted the body |

Player scavs are identified separately from AI scavs, because the two behave very differently. All of these values are configurable.

**Skill matters as much as type.** A high level does not guarantee a good player, so level influences the outcome without determining it. Each raid, your killer is assigned a skill rating: their level shifts the likely result, but a level 45 can easily be worse than a level 10, and roughly one raid in ten disregards level entirely.

That rating affects two things:

- **Extraction** — a more skilled player is likelier to escape with your gear. If they die before extracting, more of your kit is recovered.
- **Amount taken** — this reverses depending on the killer. A skilled PMC is already well equipped and therefore selective. A skilled player scav arrived with nothing and takes considerably more.

Neither extreme is ever absolute. A fully equipped player will still take a good backpack or armour plate to sell, and even the most thorough looter leaves something behind.

### What they take

Items are handled as complete pieces of kit rather than as individual entries.

A weapon and every attachment on it count as a single item, as do a rig and the plates inside it. You lose the rifle *with its optic and suppressor*, or the carrier *with its armour*, rather than losing scattered mounts and magazines while the weapon itself somehow survives.

Valuable gear is more likely to be taken. From a test raid ending in a boss kill:

**Taken** — a fully built SCAR-H and an Osprey carrier fitted with Hesco plates, together worth around 1.4 million roubles.

**Returned** — an SKS and a helmet.

The valuable items went and the rest was left behind, which is the behaviour the mod exists to produce.

{.endtabset}

## Compatibility {.tabset}

### Trader mods

**Custom traders require no configuration.** There is nothing to register and no entry to add anywhere. Install the trader, install this mod, and it works.

> **⚠️ Note** — if a trader mod defines its own insurance return rate, this mod overrides it. A custom trader advertising a 95% return will behave the same as any other.

**Return times and insurance prices are left untouched.** A trader offering a 1–6 hour return still returns in 1–6 hours, and still charges what it charges. Only the chance of an item surviving is replaced.

To give a trader some of its own character back, apply a modifier:

```json
"traderModifierPercent": {
  "54cb50c76803fa8b248b4571": 0,
  "54cb57776803fa99248b456e": 10,
  "put-the-custom-trader-id-here": 15
}
```

Prapor unchanged, Therapist at +10%, and a custom trader at +15%, each applied on top of the result for that raid.

> **Note** — some trader mods enable insurance without registering a return rate, which causes an error in SPT's own insurance run. This mod handles those traders rather than allowing the failure, and lists them in the server console at startup so the responsible mod can be identified.

### Before you install

**One requirement.** `simulateItemsBeingTaken` must be `true` in `SPT_Runtime/SPT_Data/configs/insurance.json`. This is the default. If it has been disabled, SPT never removes anything from insurance and this mod has no effect; a warning appears in the server console if that setting is found.

**Server-side only.** Nothing is installed into BepInEx, and there is nothing to update when the game version changes.

**Labs and the Labyrinth** return nothing, as in vanilla SPT.

**Compatible with related mods.** Anything that changes insurance prices, return times, or which items become insured works alongside this.

{.endtabset}

## Settings

All settings are in `config/config.json`. Edit the file and restart the server.

The defaults are tuned and require no changes, but the following are available:

| Setting | Effect |
|---|---|
| `baseReturnChancePercent` | The headline values — how much is returned for each killer type |
| `looterCompetence.sigma` | How strongly level predicts skill. Higher values weaken the link |
| `looterCompetence.wildcardChancePercent` | How often level is disregarded entirely |
| `greed.perCompetencePoint` | How much skill affects the quantity taken |
| `valueWeightedLooting.greedBias` | How strongly looters favour expensive gear |
| `minFractionTaken` / `maxFractionTaken` | Limits that prevent all-or-nothing outcomes |
| `logRolls` | Records each decision in the server console. Disabled by default |

Setting `enabled` to `false` disables the mod without uninstalling it.

> **Note** — the outcome is calculated when the raid ends and stored with the insurance. Changing settings does not affect insurance that is already in progress.

## Installation

1. Stop the server.
2. Extract the archive into your SPT folder.
3. Start the server. **Realistic Insurance** should appear in the mod list.

## Status

**0.1.0 — tested in game, but new.**

Confirmed in live raids: PMC, player scav and boss kills, with gear returning correctly across a server restart.

Not yet observed: AI scav kills, environmental deaths, and custom traders. All are handled in code but have not been seen running.

For bug reports, enable `logRolls` and include the console output, which records what the mod decided and why.
