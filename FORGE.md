Insurance in SPT is a coin flip. Every item you insure rolls against one fixed number for whichever trader you insured with, and that's the whole system. It doesn't matter whether a scav clipped you in the first minute or a boss stripped you bare — you get roughly the same middling handful of stuff back, every single time.

**Realistic Insurance** decides what comes back based on what actually happened to you.

Someone killed you, looted you, and then either made it out of the raid or didn't. That's the idea. A geared PMC takes your rifle and leaves the rest. A scav grabs something shiny and then dies to a Gluhar guard, so your kit comes home anyway. A player scav takes everything that isn't nailed down.

> **⚠️ Worth knowing up front** — this mod replaces the return chance for **every** trader, including custom traders from other mods. What you get back depends on who killed you, not who you insured with. There's a setting to give traders their own flavour back — see **Compatibility** below.

## How it works {.tabset}

### Who killed you

Different killers, different odds:

| Killed by | You get back | Because |
|---|---|---|
| PMC | ~55% | already geared, only takes upgrades |
| Player scav | ~45% | came in with nothing, leaves with everything |
| AI scav | ~75% | grabs a bit, usually dies with it |
| Boss or raider | ~40% | thorough, and they aren't leaving |
| Nobody | ~90% | bled out, fell, mine — nobody looted you |

Player scavs and AI scavs get told apart properly, because they behave nothing alike. Every one of these numbers is yours to change.

**Then it matters how good they were.** Being high level doesn't make someone good at the game, so level nudges the odds here rather than deciding them. Each raid, your killer gets rolled a rough skill rating — their level shifts what's *likely*, but a level 45 can absolutely be worse than a level 10, and about one raid in ten ignores level completely. The level 50 hatchling runner and the level 8 prodigy both exist.

That rating does two things:

- **Getting out** — a better player is more likely to escape with your gear. If they die before extracting, your odds jump.
- **How much they take** — and this one flips depending on who they are. A good PMC is already kitted, so they're *picky*. A good player scav came in with nothing, so they're *greedy*.

Nothing is ever guaranteed at either end. Even a fully-kitted chad will lift a good backpack or a nice plate to sell, and even the greediest looter leaves something behind.

### What they take

This is the part that makes it feel right.

Your gun and everything bolted to it counts as **one thing**. Same for your rig and the plates inside it. So you lose the rifle *with its scope and suppressor*, or the carrier *with its armour* — instead of a random scattering of screws and mags going missing while the gun somehow survives.

Better gear is more likely to walk. Here's a real test raid, killed by a boss:

**Gone** — a kitted SCAR-H, and an Osprey carrier loaded with Hesco plates. Around 1.4 million roubles.

**Came back** — an SKS and a helmet.

He took the good stuff and walked straight past the rest. That's the point.

{.endtabset}

## Compatibility {.tabset}

### Trader mods

**Custom traders just work.** No setup, no config entry, nothing to add anywhere. Install the trader, install this, done.

> **⚠️ The catch** — if a trader mod sets its own insurance return rate, this mod overrides it. A custom trader advertising "95% return!" will behave like everyone else.

Their **return times and prices are left alone**. A trader with a 1–6 hour return still takes 1–6 hours, and still charges what they charge. It's only the odds of your gear surviving that get taken over.

If you'd like a trader to feel special again, give them a bonus:

```json
"traderModifierPercent": {
  "54cb50c76803fa8b248b4571": 0,
  "54cb57776803fa99248b456e": 10,
  "put-the-custom-trader-id-here": 15
}
```

That's Prapor unchanged, Therapist at +10%, and a custom trader at +15% on top of whatever the raid decided.

> **ℹ️ Bonus fix** — some trader mods offer insurance without telling SPT what their return rate should be, which crashes the insurance run in vanilla SPT. This mod handles those traders instead of letting it break, and names them in the server console at startup so you know which mod to report it to.

### Before you install

**One requirement.** `simulateItemsBeingTaken` needs to be `true` in `SPT_Runtime/SPT_Data/configs/insurance.json`. It already is by default. If you've turned it off, SPT never removes anything from insurance at all, so this mod has nothing to do — and it'll warn you in the server console if it spots that.

**Server mod only.** Nothing goes into BepInEx, and there's nothing to keep in sync when the game updates.

**Labs and the Labyrinth** still return nothing, exactly as always.

**One conflict to know about.** Another mod that changes insurance *return chances* will fight with this one — pick whichever you prefer. Mods that change insurance prices, timers, or what ends up insured in the first place are all fine alongside it.

{.endtabset}

## Settings

Everything lives in `config/config.json`. Edit it, restart the server.

The defaults are tuned and you don't have to touch any of this. But if you want to:

| Setting | What it does |
|---|---|
| `baseReturnChancePercent` | The headline numbers — how much comes back, per killer |
| `looterCompetence.sigma` | How much level matters. Bigger number = matters less |
| `looterCompetence.wildcardChancePercent` | How often level gets ignored completely |
| `greed.perCompetencePoint` | How strongly skill changes the *amount* someone takes |
| `valueWeightedLooting.greedBias` | How much looters favour expensive gear. Higher = greedier |
| `minFractionTaken` / `maxFractionTaken` | Keeps things from ever being all-or-nothing |
| `logRolls` | Prints what happened and why to the server console. Off by default |

Set `enabled` to `false` to switch the whole thing off without uninstalling it.

> **ℹ️** What comes back is decided **the moment the raid ends**, then saved. Changing settings won't affect insurance that's already on its way to you.

## Install

1. Close the server.
2. Extract the archive into your SPT folder.
3. Start the server — you should see **Realistic Insurance** in the mod list.

## Status

**0.1.0 — working and tested in game, but new.**

Tested in live raids: PMC, player scav and boss kills, and gear arriving correctly even after a server restart in between.

Not yet seen in the wild: AI scav kills, dying to the map itself, and custom traders. All handled in the code, just not yet watched happening.

Found a bug? Switch on `logRolls`, grab the console output, and it'll tell you exactly what the mod decided and why.
