**This is the SPT 4.0.x build.** If you are on 4.1.x, download the 4.1 file instead. The two are not interchangeable.

This is the 4.0 backport of 0.2.3 and carries the same change.

## New

- **Dying late makes your killer less likely to get away with your gear.** Someone who kills you in the last minutes of a raid has little time to loot you and reach an exit. In the last 10 minutes their extract chance drops steadily, by up to 50 points as time runs out, and a killer who doesn't get out takes nothing home. Suggested by RagNoRock on the mod page. Thank you!

## Installing

1. Stop the server.
2. Extract the archive into your SPT folder.
3. Start the server.

Your `config.json` gets one new block, `timeRemaining`. If you keep your old config, the feature still runs with its defaults. Copy the block from the new config to tune it or set `"enabled": false`. Install both halves: the client plugin now reports 0.0.95.
