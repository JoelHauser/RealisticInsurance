**Please report bugs in this thread.**

This is a first release. It has been tested in live raids, but only by me, and a few paths have not been watched running yet: AI scav kills, environmental deaths (bleeding out, falling, mines), and custom traders. If something looks wrong, it most likely happened in one of those.

To make a report useful, set `"logRolls": true` in `config/config.json` and restart the server. The mod will then print exactly what it decided — who killed you, how skilled it rolled them, whether they got out, and every item it kept or took. Paste that output with your report and the cause is usually obvious.

Two things are working as intended, so there is no need to report them:

- **Per-trader return rates are overridden**, including any set by trader mods. What comes back depends on who killed you, not on who you insured with. Use `traderModifierPercent` if you want a particular trader to feel distinct again.
- **Labs and the Labyrinth return nothing.** That is vanilla SPT behaviour and not something this mod changes.

Thanks for trying it.
