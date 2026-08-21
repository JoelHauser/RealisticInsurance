Insurance in SPT is a coin flip. Every insured item rolls against one flat number per trader — 85% at Prapor, 95% at Therapist — so every raid returns roughly the same middling fraction and nothing about the raid itself matters.

**Realistic Insurance** replaces that with a model built from what actually happened to you: **who killed you**, **how good they were**, and **whether they got out with your gear**. Then it decides which specific items they walked off with, weighted by value — because a looter takes your armour and your rifle, not a random 15% of your inventory.

::: warning
This mod **replaces per-trader return chances entirely**, including any set by trader mods. Return chance is keyed to your killer, not to which trader you insured with. See the **Compatibility** section — this is deliberate, and there is a config option to give traders their character back.
:::

## How it works {.tabset}

### Who killed you

Your killer is read from the post-raid data and sorted into a bucket, each with its own baseline return chance:

| Killer | Default return | Why |
|---|---|---|
| PMC | 55% | takes what upgrades their kit |
| Player scav | 45% | came in with nothing, leaves with everything |
| AI scav | 75% | grabs a little, frequently dies with it |
| Boss / guard / raider | 40% | thorough, and they are not leaving |
| Nobody | 90% | bled out, fell, mine, disconnect — no one looted you |

Player scavs are told apart from AI scavs properly. A real player on a scav run carries a player identity even though their in-raid name is a generated scav name, and they behave nothing like an AI scav.

### How good they were

A level 50 PMC is not automatically good at the game, so level does not decide anything on its own. It sets the **mean** of a competence roll, made once per raid:

```
mean       = competenceAtPivot + (level - pivotLevel) * competencePerLevel
competence = clamp(Normal(mean, sigma), 0, 100)
```

`sigma` controls how much level actually predicts — at the default of 22 a level 45 and a level 10 overlap constantly. On top of that, 10% of raids ignore level entirely and draw flat 0–100: the level 50 hatchling runner and the level 8 prodigy both exist.

Competence then does two things, and the second one flips sign depending on who they are:

- **Extraction** — a better looter is more likely to get out with your gear. If they die before extracting, your odds improve.
- **Greed** — a skilled PMC is already kitted and takes *less*. A skilled player scav arrived with nothing and takes *more*.

Neither end is ever absolute. Even a fully-kitted chad lifts a good plate or backpack to sell, and even the greediest looter leaves something behind.

### What they took

The important part: looting works in **whole kit items**, not inventory rows.

A 53-item insurance package is usually only four or five real objects — a rifle and its seventeen attachments, a plate carrier and its plates and inserts, a helmet, a backpack. Each is priced *including* everything attached to it, and each gets its own roll weighted by that total value.

So you lose your gun **with its scope and suppressor**, or your carrier **with its plates**, and the cheap items in your pockets survive. From a real test raid:

```
killer=Boss, competence=81.9, extracted=True
  -> target 43.9% | took 2/4 kit item(s) = 32/53 entries

  LOST  FN SCAR-H 7.62x51 (35,604 RUB)      ...and all 17 attachments
  LOST  CQC Osprey MK4A carrier (194,098 RUB) ...and both Hesco plates
  kept  TOZ Simonov SKS 7.62x39 (80,371 RUB)
  kept  Team Wendy EXFIL Helmet (8,514 RUB)
```

He took roughly 1.4 million roubles of rifle and armour, and walked past the SKS.

{.endtabset}

## Compatibility {.tabset}

### Trader mods

**Custom traders work without any configuration.** Return chance is keyed to your killer, not to the trader, so a modded trader needs no entry anywhere.

::: warning
The trade-off: if a trader mod sets its own insurance return rate, **this mod overrides it**. A custom trader advertising "95% return" will behave like every other trader.
:::

Their **return times and insurance prices are untouched** — a trader offering a 1–6 hour return still returns in 1–6 hours, and their price coefficients still apply. Only the survival chance is taken over.

If you want a trader to feel distinctive again, give them a modifier:

```json
"traderModifierPercent": {
  "54cb50c76803fa8b248b4571": 0,
  "54cb57776803fa99248b456e": 10,
  "690766de550bc322a810ea1e": 15
}
```

That is Prapor, Therapist, and a custom trader given +15% on top of whatever the killer-based model decided.

::: information
There is a bug in SPT itself where a modded trader that offers insurance without registering a return chance crashes the insurance run. This mod handles those traders instead of letting SPT throw, and names them in the server console at startup so you know which mod to report.
:::

### Other insurance mods

Anything that changes **which items reach insurance** — insurance fraud style mods, mods that make dropped gear insurable — works fine. Those run before this mod sees the package.

Anything that changes **the return chance itself** will conflict, since both are rewriting the same decision. Pick one.

Mods that change insurance **timing or cost** are unaffected.

### Requirements

::: error
`simulateItemsBeingTaken` must be `true` in `SPT_Runtime/SPT_Data/configs/insurance.json`.

It is `true` by default. If you have set it to `false`, SPT never removes anything from insurance and this mod does nothing at all. The server console warns you at startup if it sees this.
:::

Server mod only — there is no client plugin, so nothing to install into BepInEx and no version gate to worry about.

Labs and the Labyrinth still return nothing, exactly as in vanilla SPT.

{.endtabset}

## Configuration

Everything lives in `config/config.json` inside the mod folder. Changes need a server restart.

The dials most worth touching:

| Key | Effect |
|---|---|
| `baseReturnChancePercent.*` | Baseline return per killer type — the headline numbers |
| `looterCompetence.sigma` | How much level predicts skill. Higher = less |
| `looterCompetence.wildcardChancePercent` | Raids that ignore level entirely |
| `greed.perCompetencePoint.*` | How strongly skill changes the amount taken, signed per killer |
| `valueWeightedLooting.greedBias` | Price exponent when choosing what to take. `1` = proportional, `2` = strongly favours expensive gear |
| `valueWeightedLooting.minFractionTaken` / `maxFractionTaken` | Floor and ceiling, so no outcome is ever absolute |
| `logRolls` | Prints every decision and every item as `LOST` or `kept`. Off by default |

Set `enabled: false` to leave SPT's vanilla behaviour completely untouched without uninstalling.

::: information
Return chance is decided **when the raid ends** and stored with the insurance package, so editing the config will not change insurance that is already in the post.
:::

## Installation

1. Close the server.
2. Extract the archive into your SPT folder. It contains `SPT_Runtime/user/mods/RealisticInsurance/`.
3. Start the server. You should see `Realistic Insurance` in the mod list.

## Status

**0.1.0 — working and tested, but young.**

Verified in a live raid: killer classification for PMCs, player scavs and bosses; the PMC level lookup; the competence and extraction rolls; the decision surviving a server restart; and the correct items coming back in the post.

Not yet watched running: AI scav kills, environmental deaths, packages insured before installing the mod, and modded traders. All are handled in code.

Bug reports welcome — turn on `logRolls` and include the console output, which prints exactly what the mod decided and why.
