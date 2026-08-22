**This is the SPT 4.0.x build.** If you are on 4.1.x, take the 4.1 file instead — the two are not interchangeable and the wrong one will not load at all.

Nothing about how the mod behaves has changed. What comes back is still decided by who killed you, how good they were, and whether they got out of the raid with your gear, and the settings file is identical to the 4.1 release. This build exists only because 4.0 and 4.1 need separately compiled versions.

## Please read before installing

This build compiles cleanly against 4.0.13, and every part of the server it hooks into is identical on 4.0 and 4.1. But **it has not yet been run on a live 4.0 server.** The 4.1 build is the one that has been tested in raids. If you install this, you are the first to do so, and bug reports are genuinely wanted.

Set `"logRolls": true` in `config/config.json` and the server console will print what the mod decided about your killer, along with every item it kept or took. That output is what turns a report into something fixable.

## Installing

Extract the archive into your SPT folder. It unpacks to `SPT/user/mods/RealisticInsurance` — note that the folder is `SPT`, which is the 4.0 name for it, and not the `SPT_Runtime` used by 4.1.

`simulateItemsBeingTaken` must be `true` in `SPT/SPT_Data/configs/insurance.json`. It is by default. If it has been turned off, SPT never removes anything from insurance and this mod has nothing to do; a warning appears in the server console if that setting is found.

Server-side only. Nothing is installed into BepInEx.

## What actually differs

Only the parts of the mod that talk to the server, and none of the parts that decide what happens to your gear. The four places it hooks into have identical signatures on both versions, so the killer identification, the skill rolls and the value-weighted looting are the same code running unchanged.

The rest was version plumbing: 4.0 runs on .NET 9 rather than .NET 10, uses a different base class for mod metadata and a different load hook, registers its patches by another route, and reads the trader list from a service that 4.1 removed.
