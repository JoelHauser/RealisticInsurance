**This is the SPT 4.1.x build.** If you are on 4.0.x, download the 4.0 file instead. The two are not interchangeable.

## Fixes

- **Gear you lose after surviving now comes back at your trader's normal rate.** Survivors have no corpse, so nothing they lost was looted, but it was still being judged by an imaginary looter. Reported and fixed by [Tosox](https://github.com/Tosox) in #2 / #3. Thank you!
- Weapon attachments on gear you dropped now follow the trader's rate too, instead of the looter's.
- Dropped gear is now recognised even with `valueWeightedLooting` turned off.
- A death snapshot from a raid that lost no insured gear can no longer be applied to a later raid.
- The trader's "you'd dropped that before you went down" line no longer appears when nobody killed you.

## Installing

1. Stop the server.
2. Extract the archive into your SPT folder.
3. Start the server.

Your `config.json` has no new settings. The client plugin is unchanged and still reports 0.2.0.
