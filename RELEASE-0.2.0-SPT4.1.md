**This is the SPT 4.1.x build.** If you are on 4.0.x, download the 4.0 file instead — the two are not interchangeable and the wrong one will not load at all.

## Gear you dropped is no longer charged to your killer

If you stashed a helmet in a bush twenty minutes before you died, the PMC who killed you never saw it. Until now the mod treated it as part of your corpse anyway, so their greed and skill decided whether it came home.

It no longer does. Anything you dropped before dying is handed straight back to SPT and returns at **the trader's own rate** — 85% at Prapor, 95% at Therapist — exactly as if this mod were not installed. Gear that was actually on your body is still judged by whoever killed you.

## This adds a client plugin

The server cannot work this out alone. It receives one flat list of lost insured items at raid end, with no positions and no timestamps, so looted gear and dropped gear are indistinguishable to it.

So the archive now contains **two** files, and both need to be installed:

```
BepInEx/plugins/RealisticInsurance/RealisticInsuranceClient.dll
SPT_Runtime/user/mods/RealisticInsurance/...
```

Extracting the archive into your SPT folder puts both in the right place.

The plugin does one thing: at the moment you die it notes what is still in your inventory and tells the server. No timestamps are needed — anything missing from that list left under your own control, whenever that was.

**Without the plugin the mod still works.** Nothing is marked as dropped and it behaves exactly as 0.1.0 did, so a server-only install is degraded rather than broken.

## Also in this release

Console logging (`logRolls`) now ships off. It prints every decision and every item verdict, which is what you want when reporting a bug and noise otherwise.

Installing no longer overwrites your `config.json`. Previously an update replaced whatever you had tuned with the defaults.

## Installing

1. Stop the server.
2. Extract the archive into your SPT folder.
3. Start the server. **Realistic Insurance** should appear in the mod list, and *Realistic Insurance (client)* in the BepInEx console.

`simulateItemsBeingTaken` must be `true` in `SPT_Runtime/SPT_Data/configs/insurance.json`. It is by default; the server console warns you at startup if it is not.

## Reporting problems

Set `"logRolls": true` in `config/config.json`, restart the server, and both halves will report what they did:

```
[RealisticInsurance] death snapshot sent: N item(s) still on the body
[RealisticInsurance] corpse snapshot: N item(s) still on the body at death
[RealisticInsurance]   N entr(ies) were dropped before death -> handed to SPT
```

Those two counts should match. Include that output and the cause is usually obvious.

One thing that is expected, not a bug: the per-item `LOST`/`kept` list is now shorter than the package. Dropped items are no longer decided by this mod, so they do not appear — SPT decides them after it bows out.
