**This is the SPT 4.0.x build.** If you are on 4.1.x, download the 4.1 file instead — the two are not interchangeable and the wrong one will not load at all.

## If you are on 0.0.9, please update

There were two different builds both called 0.0.9, and the first one could not start a server. It asked the server for its insurance settings the way SPT 4.1 does; 4.0 supplies them by another route, so the server aborted with *"Unable to resolve service for type ... InsuranceConfig"* before the mod ever ran. Nothing touched your profile.

That was fixed in place, which meant two archives shared a version number and nothing distinguished them. **0.0.92 exists to end that ambiguity.** If you are on any 0.0.9, take this one.

## What is in it

The same mod, with the crash fixed and the version made unambiguous. What comes back is decided by who killed you, how good they were, and whether they got out of the raid with your gear.

Console logging (`logRolls`) ships off. Turn it on in `config/config.json` if you are reporting a bug — it prints exactly what the mod decided and why.

## Not in this build

The 4.1 line has a newer feature: gear you dropped *before* dying is no longer charged to your killer's greed, and instead returns at the trader's own rate. That needs a client plugin built against 4.1's assemblies, so it is not part of this backport.

## Installing

Extract into your SPT folder. It unpacks to `SPT/user/mods/RealisticInsurance` — note the folder is `SPT`, the 4.0 name, not the `SPT_Runtime` used by 4.1.

`simulateItemsBeingTaken` must be `true` in `SPT/SPT_Data/configs/insurance.json`. It is by default. If it has been turned off, SPT never removes anything from insurance and this mod has nothing to do; a warning appears in the server console if that setting is found.

Server-side only. Nothing is installed into BepInEx.

## Still worth knowing

This build has been compiled and checked against 4.0.13, but it has not been run through a raid on a 4.0 server — the 4.1 line is the one that gets tested in game. Bug reports are welcome and genuinely useful.
