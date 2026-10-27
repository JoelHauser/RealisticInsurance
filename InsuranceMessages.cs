using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Utils;

namespace RealisticInsurance
{
    /// <summary>
    /// Writes the note the trader sends with a returned insurance package.
    ///
    /// The mod already decides who looted you, how good they were and whether
    /// they got out, then throws all of it away: the player sees only that some
    /// items are missing. This turns that into the sentence the trader would
    /// actually have written.
    ///
    /// The message is assembled from independent slots -- an opener in the
    /// trader's voice, what came back, who took the rest, and optionally the one
    /// item worth naming. Each slot is drawn separately, so a handful of lines
    /// per slot produces hundreds of distinct notes; writing whole messages
    /// instead would repeat within an evening's play.
    ///
    /// The text is English only. Vanilla insurance mail goes through locale
    /// template ids, which this deliberately bypasses -- a template id can only
    /// name a fixed string, and the whole point here is a sentence built from
    /// what actually happened.
    /// </summary>
    internal static class InsuranceMessages
    {
        // The only two traders offering insurance in vanilla. Modded traders get
        // the neutral voice rather than being put in Prapor's mouth.
        private const string PraporId = "54cb50c76803fa8b248b4571";
        private const string TherapistId = "54cb57776803fa99248b456e";

        /// <summary>
        /// How often the note names a specific item. Always naming one makes
        /// every message the same length and the same shape, which is the
        /// sameness this feature exists to break.
        /// </summary>
        private const int NameAnItemChancePercent = 70;

        private static readonly Dictionary<TraderVoice, string[]> Openers = new()
        {
            [TraderVoice.Prapor] = new[]
            {
                "Sent a man out to your last known spot.",
                "Had someone out there before the dust settled.",
                "Took my guy a while to find the place.",
                // Openers must not presume an outcome -- "you're lucky I had
                // people nearby" landed in front of "empty, the whole lot of it".
                "My lot got out there as quick as they could.",
                "So. Your kit."
            },
            [TraderVoice.Therapist] = new[]
            {
                "My people went out to collect what they could.",
                "I had someone recover what was left of your equipment.",
                "I've had a look at what came back.",
                "Another one of these, then.",
                "You really must stop doing this to yourself."
            },
            [TraderVoice.Generic] = new[]
            {
                "Someone went out to recover your gear.",
                "The recovery is done.",
                "Had a look at the spot where you went down.",
                "Here is what came back.",
                "Your claim has been processed."
            }
        };

        private static readonly Dictionary<TraderVoice, Dictionary<Recovery, string[]>> Outcomes = new()
        {
            [TraderVoice.Prapor] = new Dictionary<Recovery, string[]>
            {
                [Recovery.None] = new[]
                {
                    "There was nothing left worth carrying back.",
                    "Empty. The whole lot of it, gone.",
                    "Picked clean. I've got nothing for you."
                },
                [Recovery.Little] = new[]
                {
                    "Scraps. That's all that was still lying there.",
                    "You're getting a fraction of it back, and I'm being generous.",
                    "Not much survived. Don't shoot the messenger."
                },
                [Recovery.Half] = new[]
                {
                    "About half of it is here. Could be worse.",
                    "They left more than I expected.",
                    "Call it an even split with whoever got there first."
                },
                [Recovery.Most] = new[]
                {
                    "Most of it is here. They were in a hurry.",
                    "You're getting nearly all of it back.",
                    "Only a couple of pieces missing. You'll live."
                },
                [Recovery.All] = new[]
                {
                    "Every last piece of it. Don't get used to it.",
                    "All of it. Nobody touched a thing.",
                    "The whole kit, exactly as you left it."
                }
            },
            [TraderVoice.Therapist] = new Dictionary<Recovery, string[]>
            {
                [Recovery.None] = new[]
                {
                    "I'm afraid there was nothing left to recover.",
                    "Not a single piece of it survived.",
                    "There was simply nothing to bring back."
                },
                [Recovery.Little] = new[]
                {
                    "Only a small part of it was still there.",
                    "What I'm returning is barely worth the paperwork.",
                    "Very little of your equipment survived, I'm sorry to say."
                },
                [Recovery.Half] = new[]
                {
                    "Roughly half of your equipment came back.",
                    "A fair portion of it survived.",
                    "Some of it was still there. Not all, but some."
                },
                [Recovery.Most] = new[]
                {
                    "Nearly all of your equipment has been returned.",
                    "Very little of it is actually missing.",
                    "Almost everything is here. A small loss, this time."
                },
                [Recovery.All] = new[]
                {
                    "Everything came back, untouched.",
                    "Your kit is complete. Do try to keep it that way.",
                    "All of it recovered. You were fortunate."
                }
            },
            [TraderVoice.Generic] = new Dictionary<Recovery, string[]>
            {
                [Recovery.None] = new[]
                {
                    "Nothing was recovered.",
                    "The kit was stripped clean.",
                    "There was nothing left at the scene."
                },
                [Recovery.Little] = new[]
                {
                    "Only a little of it was still there.",
                    "Most of your kit is gone.",
                    "A small part of it was recovered."
                },
                [Recovery.Half] = new[]
                {
                    "About half of it came back.",
                    "A decent share of the kit was recovered.",
                    "Some of it survived, some of it did not."
                },
                [Recovery.Most] = new[]
                {
                    "Most of it came back.",
                    "Only a few pieces are missing.",
                    "Nearly the whole kit was recovered."
                },
                [Recovery.All] = new[]
                {
                    "Everything was recovered.",
                    "The whole kit came back untouched.",
                    "Nothing was missing."
                }
            }
        };

        // Who took it, keyed by killer and by whether they got out. A looter who
        // died on the way to an exit is the reason anything came back at all,
        // and the note should say so -- otherwise the extra gear reads as the
        // killer having been oddly generous.
        private static readonly string[] PmcExtracted =
        {
            "Whoever put you down walked out with the rest.",
            "The one who got you knew exactly what he was looking at, and he made it to an exit.",
            "A PMC did it, and he was long gone before anyone got there."
        };

        private static readonly string[] PmcDied =
        {
            "The one who killed you never made it out himself.",
            "Whoever shot you died not long after. That is the only reason you're seeing any of this.",
            "He took your kit and then somebody took his. Fair trade, in a way."
        };

        private static readonly string[] PlayerScavExtracted =
        {
            "A scav with a real head on his shoulders cleaned you out and got clear.",
            "Whoever was wearing that scav rig knew what all of it was worth.",
            "That was no wild man. He took his pick and walked out."
        };

        private static readonly string[] PlayerScavDied =
        {
            "The scav that got you was still picking through your kit when somebody else got him.",
            "He took his time over your body. It cost him.",
            "Whoever it was, he never got clear with it."
        };

        private static readonly string[] ScavExtracted =
        {
            "A scav got you, took whatever caught his eye, and wandered off.",
            "Some wild man shot you and helped himself. No taste, thankfully.",
            "A scav did it. They never do know what the good stuff is."
        };

        private static readonly string[] ScavDied =
        {
            "The scav that killed you didn't last the hour.",
            "He died a stone's throw from your body, still carrying it.",
            "Whatever he took off you, he didn't keep it long."
        };

        private static readonly string[] BossExtracted =
        {
            "You picked a fight with the wrong people. They kept what they liked.",
            "That crowd doesn't leave much behind.",
            "Their kind hold the ground and take their time over a body."
        };

        private static readonly string[] BossDied =
        {
            "Whatever is left of the man who killed you is lying next to you.",
            "His people held the ground, but he didn't walk away either.",
            "He didn't outlive you by much."
        };

        /// <summary>
        /// No aggressor: environmental death, a bleed-out, a disconnect, or
        /// simply surviving and leaving gear behind. Nobody looted the player at
        /// all, so the note must not imply anyone did.
        /// </summary>
        private static readonly string[] NoKiller =
        {
            "Nobody had been near it. It was lying exactly where you left it.",
            "No one had touched any of it.",
            "Whatever happened out there, it wasn't a person that did it."
        };

        /// <summary>
        /// The killer got out and still took nothing -- a picky, well equipped
        /// PMC, which the greed model produces often. Without this the note says
        /// "he walked out with the rest" over a package that is entirely intact.
        /// </summary>
        private static readonly string[] TookNothing =
        {
            "Whoever killed you walked off without taking a thing - better equipped than you already, most likely.",
            "He didn't want any of it. Make of that what you will.",
            "Whoever it was left the lot where it fell and walked out."
        };

        /// <summary>
        /// Nothing came back, and the story on file cannot explain it: the killer
        /// died, or there was no killer at all. Saying he never got out, or that
        /// nobody touched it, would contradict an empty package.
        /// </summary>
        private static readonly string[] NothingFound =
        {
            "Whatever was there had gone long before anyone arrived.",
            "Someone had already been through it.",
            "There was nothing at the spot at all."
        };

        private static readonly string[] LostItem =
        {
            "Your {0} isn't coming back.",
            "The {0} is gone for good.",
            "Say goodbye to the {0}."
        };

        private static readonly string[] KeptItem =
        {
            "At least the {0} made it.",
            "Your {0} is in there.",
            "The {0} came through fine."
        };

        /// <summary>
        /// Gear dropped before dying is judged by nobody and comes back at the
        /// trader's own rate. Saying so stops it reading as the killer having
        /// missed things.
        /// </summary>
        private static readonly string[] Dropped =
        {
            "The gear you'd left lying about came back on its own - nobody had been near it.",
            "Whatever you dropped before you went down was still sitting there.",
            "The pieces you'd stashed were untouched. Those are in there too."
        };

        internal static string Compose(
            PackageOutcome outcome,
            MongoId traderId,
            int entriesReturned,
            bool includeItemNames,
            RandomUtil randomUtil)
        {
            var voice = VoiceFor(traderId);
            var recovery = RecoveryFor(outcome, entriesReturned);

            var parts = new List<string>(4)
            {
                Pick(Openers[voice], randomUtil),
                Pick(Outcomes[voice][recovery], randomUtil),
                Pick(CauseFor(outcome, recovery), randomUtil)
            };

            var named = NameAnItem(outcome, recovery, includeItemNames, randomUtil);
            if (named is not null)
            {
                parts.Add(named);
            }

            var text = string.Join(" ", parts);

            // On its own line: it is an aside about different gear, not part of
            // the story about the looter. Skipped when everything was dropped,
            // e.g. after surviving, as there is no other gear to set it apart from.
            if (outcome.DroppedCount > 0 && outcome.DroppedCount < outcome.EntriesTotal)
            {
                text += "\n" + Pick(Dropped, randomUtil);
            }

            return text;
        }

        private static string? NameAnItem(
            PackageOutcome outcome,
            Recovery recovery,
            bool includeItemNames,
            RandomUtil randomUtil)
        {
            if (!includeItemNames || !randomUtil.GetChance100(NameAnItemChancePercent))
            {
                return null;
            }

            if (recovery != Recovery.All && !string.IsNullOrWhiteSpace(outcome.TopLostName))
            {
                return string.Format(Pick(LostItem, randomUtil), outcome.TopLostName);
            }

            // Naming what survived is only worth saying when something didn't,
            // and when there is in fact something in the mail to name.
            if (recovery is not (Recovery.All or Recovery.None) && !string.IsNullOrWhiteSpace(outcome.TopKeptName))
            {
                return string.Format(Pick(KeptItem, randomUtil), outcome.TopKeptName);
            }

            return null;
        }

        /// <summary>
        /// The killer line has to agree with the outcome line. Two corners
        /// contradict it outright, and both were produced on the first run of a
        /// harness over these pools rather than reasoned about in advance.
        /// </summary>
        private static string[] CauseFor(PackageOutcome outcome, Recovery recovery)
        {
            if (recovery == Recovery.None && (outcome.Type == KillerType.Other || !outcome.LooterExtracted))
            {
                return NothingFound;
            }

            if (recovery == Recovery.All && outcome.LooterExtracted && outcome.Type != KillerType.Other)
            {
                return TookNothing;
            }

            return outcome.Type switch
            {
                KillerType.Pmc => outcome.LooterExtracted ? PmcExtracted : PmcDied,
                KillerType.PlayerScav => outcome.LooterExtracted ? PlayerScavExtracted : PlayerScavDied,
                KillerType.Scav => outcome.LooterExtracted ? ScavExtracted : ScavDied,
                KillerType.Boss => outcome.LooterExtracted ? BossExtracted : BossDied,
                _ => NoKiller
            };
        }

        /// <summary>
        /// Brackets by VALUE where a plan exists, because value is what a player
        /// means by "most of it came back" -- losing a kitted rifle is not
        /// "thirteen of thirty-four entries", it is most of the raid.
        ///
        /// Both ends are decided by what is actually in the mail instead. SPT
        /// strips attachments off surviving weapons in a pass of its own after
        /// the plan is made, so the plan alone can claim a loss the player never
        /// sees, or miss one they do.
        /// </summary>
        private static Recovery RecoveryFor(PackageOutcome outcome, int entriesReturned)
        {
            if (entriesReturned <= 0)
            {
                return Recovery.None;
            }

            if (outcome.EntriesTotal > 0 && entriesReturned >= outcome.EntriesTotal)
            {
                return Recovery.All;
            }

            double returnedFraction;
            if (outcome.ValueOnCorpse > 0d)
            {
                returnedFraction = 1d - (outcome.ValueLost / outcome.ValueOnCorpse);
            }
            else if (outcome.EntriesTotal > 0)
            {
                returnedFraction = entriesReturned / (double)outcome.EntriesTotal;
            }
            else
            {
                returnedFraction = 1d;
            }

            return returnedFraction switch
            {
                <= 0.05d => Recovery.None,
                <= 0.34d => Recovery.Little,
                <= 0.67d => Recovery.Half,
                _ => Recovery.Most
            };
        }

        private static TraderVoice VoiceFor(MongoId traderId)
        {
            var id = traderId.ToString();
            if (string.Equals(id, PraporId, StringComparison.Ordinal))
            {
                return TraderVoice.Prapor;
            }

            return string.Equals(id, TherapistId, StringComparison.Ordinal)
                ? TraderVoice.Therapist
                : TraderVoice.Generic;
        }

        private static string Pick(string[] lines, RandomUtil randomUtil)
        {
            return lines[randomUtil.GetInt(0, lines.Length - 1)];
        }
    }
}
