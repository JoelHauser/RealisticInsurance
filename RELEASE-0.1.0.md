**This is a first release, and bug reports are the most useful thing you can send.**

If gear comes back that shouldn't have, or nothing comes back when it should, please say so. The mod can explain itself: set `"logRolls": true` in `config/config.json` and the server console will print what it decided about your killer and how it ruled on every item. Include that output with your report and the cause is usually obvious.

As far as I know this is the first mod of its kind. SPT's insurance has always been a flat percentage per trader, and I couldn't find anything that ties it to what happened in the raid. If something similar already exists, I'd like to hear about it.

## What it does

What comes back is decided by who killed you, how good they were, and whether they survived to extract with your gear. Items are treated as whole pieces of kit, so a rifle leaves with its optic and suppressor instead of losing attachments at random, and valuable gear is more likely to be taken than cheap gear.

## What has been tested

Confirmed in live raids: PMC, player scav and boss kills, correct identification of the killer, and gear returning properly across a server restart.

Not yet seen running: AI scav kills, environmental deaths, and custom traders. All are handled in code, but none have been watched happening — so they are the likeliest place for a first bug.

## Requirements

- SPT 4.1.x
- `simulateItemsBeingTaken` set to `true` in `SPT_Runtime/SPT_Data/configs/insurance.json` — this is the default

Server-side only. Nothing is installed into BepInEx.

## Notes

The defaults are tuned and need no changes. Everything is in `config/config.json` if you want to adjust it, and a server restart applies any edits. The outcome of a raid is calculated when that raid ends, so changes will not affect insurance already on its way back to you.

Return chances are decided by your killer rather than your trader, which means per-trader rates — including any set by trader mods — are overridden. `traderModifierPercent` can give a trader some of its character back.
