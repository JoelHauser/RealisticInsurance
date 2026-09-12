using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace RealisticInsurance
{
    /// <summary>
    /// One kit item's verdict and what it was worth, kept so the trader's note
    /// can name the single item worth naming. Value is the whole object -- a
    /// rifle plus everything bolted to it.
    /// </summary>
    internal readonly record struct RootTake(Item Root, double Value, bool Taken);

    /// <summary>
    /// Which trader is writing. Only Prapor and Therapist offer insurance in
    /// vanilla; anything else is a modded trader and gets the neutral voice.
    /// </summary>
    internal enum TraderVoice { Prapor, Therapist, Generic }

    /// <summary>How much of the kit came back, in the terms a player thinks in.</summary>
    internal enum Recovery { None, Little, Half, Most, All }

    /// <summary>
    /// What actually happened to one package, recorded while the plan is being
    /// built and read again when the mail goes out.
    ///
    /// It has to be recorded rather than recomputed: by the time SendMail runs,
    /// the taken items have already been removed from the package, so the only
    /// thing still knowable there is what survived.
    /// </summary>
    internal class PackageOutcome
    {
        public KillerType Type { get; set; }
        public bool LooterExtracted { get; set; }

        /// <summary>Entries in the package before anything was removed.</summary>
        public int EntriesTotal { get; set; }

        /// <summary>
        /// Rouble value on the body and the part of it the looter took. Zero when
        /// no plan was built (value-weighted looting off), in which case the
        /// narrative falls back to counting entries.
        /// </summary>
        public double ValueOnCorpse { get; set; }
        public double ValueLost { get; set; }

        /// <summary>Entries the player had dropped before dying, which no looter judged.</summary>
        public int DroppedCount { get; set; }

        /// <summary>Most valuable kit item taken, and the most valuable one left.</summary>
        public string? TopLostName { get; set; }
        public string? TopKeptName { get; set; }
    }

    /// <summary>
    /// Carries the outcome from the planning patch to the mail patch.
    ///
    /// Deliberately NOT cleared by RaidLootPlan.Clear(): that runs in the
    /// FindItemsToDelete postfix, which is well before SendMail, and clearing
    /// there would throw the outcome away before anything could read it. It is
    /// cleared at the start of each plan instead, so a package that produces no
    /// plan can never inherit the previous package's story.
    /// </summary>
    internal static class PackageOutcomeStore
    {
        [ThreadStatic] private static PackageOutcome? _current;

        internal static void Set(PackageOutcome outcome) => _current = outcome;

        internal static void Clear() => _current = null;

        /// <summary>Taken rather than peeked - one outcome belongs to one package.</summary>
        internal static PackageOutcome? Take()
        {
            var outcome = _current;
            _current = null;
            return outcome;
        }
    }
}
