**This is the SPT 4.0.x build.** If you are on 4.1.x, download the 4.1 file instead. The two are not interchangeable.

This is the 4.0 backport of 0.2.2 and carries the same fixes.

## Fixes

- **Gear you lose after surviving now comes back at your trader's normal rate.** Survivors have no corpse, so nothing they lost was looted, but it was still being judged by an imaginary looter. Reported and fixed by [Tosox](https://github.com/Tosox) in #2 / #3. Thank you!
- Weapon attachments on gear you dropped now follow the trader's rate too, instead of the looter's.
- Dropped gear is now recognised even with `valueWeightedLooting` turned off.
- A death snapshot from a raid that lost no insured gear can no longer be applied to a later raid.
- The trader's "you'd dropped that before you went down" line no longer appears when nobody killed you.
- The client plugin is now built against SPT 4.0 (0.0.93 shipped the 4.1 build of it).

## Installing

1. Stop the server.
2. Extract the archive into your SPT folder.
3. Start the server.

Your `config.json` has no new settings.
