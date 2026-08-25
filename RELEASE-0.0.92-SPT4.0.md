**This is the SPT 4.0.x build.** If you are on 4.1.x, download the 4.1 file instead — the two are not interchangeable and the wrong one will not load at all.

## Gear you dropped is no longer charged to your killer

If you stashed a helmet in a bush twenty minutes before you died, the PMC who killed you never saw it. Until now the mod treated it as part of your corpse anyway, so their greed and skill decided whether it came home.

It no longer does. Anything you dropped before dying is handed straight back to SPT and returns at **the trader's own rate** — 85% at Prapor, 95% at Therapist — exactly as if this mod were not installed. Gear that was actually on your body is still judged by whoever killed you.

## This adds a client plugin

The server cannot work this out alone. It receives one flat list of lost insured items at raid end, with no positions and no timestamps, so looted gear and dropped gear are indistinguishable to it.

So the archive now contains **two** files, and both need to be installed:

```
BepInEx/plugins/RealisticInsurance/RealisticInsuranceClient.dll
SPT/user/mods/RealisticInsurance/...
```

Extracting the archive into your SPT folder puts both in the right place.

The plugin does one thing: at the moment you die it notes what is still in your inventory and tells the server. No timestamps are needed — anything missing from that list left under your own control, whenever that was.

**Without the plugin the mod still works.** Nothing is marked as dropped and it behaves as it did before, so a server-only install is degraded rather than broken.

## If you are on 0.0.9, please update

There were two different builds both called 0.0.9, and the first could not start a server. It asked the server for its insurance settings the way SPT 4.1 does; 4.0 supplies them by another route, so the server aborted with *"Unable to resolve service for type ... InsuranceConfig"* before the mod ever ran. Nothing touched your profile.

That was fixed in place, which left two archives sharing a version number with nothing to tell them apart. Later numbering ends that ambiguity — if you are on any 0.0.9, take this one.

## Also in this release

Console logging (`logRolls`) ships off. Turn it on in `config/config.json` if you are reporting a bug — it prints exactly what the mod decided and why.

Installing no longer overwrites your `config.json`. Previously an update replaced whatever you had tuned with the defaults.

## Installing

1. Stop the server.
2. Extract the archive into your SPT folder. Note the mod folder is `SPT`, the 4.0 name, not the `SPT_Runtime` used by 4.1.
3. Start the server. **Realistic Insurance** should appear in the mod list, and *Realistic Insurance (client)* in the BepInEx console.

`simulateItemsBeingTaken` must be `true` in `SPT/SPT_Data/configs/insurance.json`. It is by default; the server console warns you at startup if it is not.

## Still worth knowing

This build is compiled and checked against 4.0.13, but the in-game testing happens on the 4.1 line — including the dropped-gear feature above, which was verified there. Bug reports from 4.0 are genuinely useful.

If you hit something, set `"logRolls": true`, restart, and both halves report what they did:

```
[RealisticInsurance] death snapshot sent: N item(s) still on the body
[RealisticInsurance] corpse snapshot: N item(s) still on the body at death
[RealisticInsurance]   N entr(ies) were dropped before death -> handed to SPT
```

Those two counts should match.
