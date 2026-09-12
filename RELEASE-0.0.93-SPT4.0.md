**This is the SPT 4.0.x build.** If you are on 4.1.x, download the 4.1 file instead — the two are not interchangeable and the wrong one will not load at all.

This is the 4.0 backport of 0.2.1 and carries the same feature.

## The trader now tells you what happened

This mod has always worked out who killed you, how good a looter they were, and whether they got out with your gear. You have never been able to see any of it. Items were simply missing, exactly as in vanilla, and the reasoning behind it lived in the server console.

The insurance mail now says it out loud:

> Had someone out there before the dust settled. Most of it is here. They were in a hurry. Your AK-105 isn't coming back.

> My people went out to collect what they could. Only a small part of it was still there. A scav with a real head on his shoulders cleaned you out and got clear. Say goodbye to the Osprey.
>
> Whatever you dropped before you went down was still sitting there.

The note covers how much came back, who took the rest, whether they made it to an exit, and usually the one item worth naming. Prapor and Therapist have their own voices. A modded trader that offers insurance gets a neutral one rather than being put in Prapor's mouth.

It is one mail, not two — your gear is attached to the same message, exactly as before.

Vanilla insurance mail cannot do this. The server sends a locale **template id** and the client looks the text up, so the message can only ever be one of a handful of fixed strings, and none of them can mention a looter, because vanilla has no looter to mention. This sends literal text instead, which the profile stores, so old mail still reads correctly weeks later.

The note is built from four independently drawn pieces rather than picked from a list of canned messages — a list would start repeating within an evening.

How much came back is judged by **value**, not by item count. Losing a kitted rifle is not "thirteen of thirty-four entries", it is most of the raid, and the message says so.

Two cases deliberately keep SPT's stock message: packages insured before you installed this mod (there is no killer to describe), and Labs / Labyrinth, where nothing is returned because the *map* forbids it and no looter was involved at all.

The text is English only. That is the cost of bypassing the locale system, and the locale system is exactly what stopped the vanilla message being able to say anything.

## Config

One new section, on by default:

```json
"traderMessages": {
  "enabled": true,
  "includeItemNames": true
}
```

`enabled: false` restores SPT's stock insurance message and changes nothing else. `includeItemNames: false` keeps the note but stops it naming your gear.

## Installing

1. Stop the server.
2. Extract the archive into your SPT folder.
3. Start the server.

Your existing `config.json` is not overwritten, so the new section will be missing from it. That is fine — both settings default to on.

The client plugin is unchanged in this release. Nothing to do; it is the same file that shipped with 0.0.92.

## On the 4.0 build specifically

Built against 4.0.13. The hook this feature needs — `InsuranceController.SendMail` — has the same signature on 4.0 as on 4.1, and `MailSendService.SendDirectNpcMessageToPlayer` is identical apart from living in a different namespace. The message text itself was checked on both builds and produces the same output.

As with 0.0.92, the full pipeline has been observed end to end on a live **4.1** server rather than a 4.0 one.
