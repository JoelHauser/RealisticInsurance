using SPTarkov.Server.Core.Models.Eft.Common;

namespace RealisticInsurance
{
    internal enum KillerType { Pmc, PlayerScav, Boss, Other }

    /// <summary>
    /// What we learned at raid end and need again (potentially hours later, across a
    /// server restart) when the insurance return actually runs.
    /// </summary>
    internal class KillerContext
    {
        public KillerType Type { get; set; }
        public bool LooterExtracted { get; set; }

        /// <summary>Killer's level, or null when unknown (scavs, bosses, cache miss).</summary>
        public int? KillerLevel { get; set; }

        /// <summary>0-100, rolled once per raid. Level only sets its mean.</summary>
        public double Competence { get; set; } = 50;

        /// <summary>True when this raid's competence ignored level entirely.</summary>
        public bool Wildcard { get; set; }

        // Stamped into Insurance.ExtensionData so it persists in profile.json.
        public const string ExtKeyType = "riKillerType";
        public const string ExtKeyExtracted = "riLooterExtracted";
        public const string ExtKeyLevel = "riKillerLevel";
        public const string ExtKeyCompetence = "riCompetence";
        public const string ExtKeyWildcard = "riWildcard";

        /// <summary>
        /// Classify from the post-raid aggressor. Side is the reliable signal: EFT
        /// reports Usec/Bear for any PMC (AI or human) and Savage for any scav.
        /// Role is only used to separate bosses out of the scav bucket.
        /// </summary>
        public static KillerType Classify(PmcData? pmcData)
        {
            var aggressor = pmcData?.Stats?.Eft?.Aggressor;
            if (aggressor is null)
            {
                // No aggressor recorded: environmental death, bleed-out, disconnect,
                // MIA, or the player simply left gear behind while surviving.
                return KillerType.Other;
            }

            var role = aggressor.Role ?? string.Empty;
            if (IsBossRole(role))
            {
                return KillerType.Boss;
            }

            var side = aggressor.Side ?? string.Empty;
            if (side.Equals("Savage", StringComparison.OrdinalIgnoreCase))
            {
                return KillerType.PlayerScav;
            }

            if (side.Equals("Usec", StringComparison.OrdinalIgnoreCase)
                || side.Equals("Bear", StringComparison.OrdinalIgnoreCase))
            {
                return KillerType.Pmc;
            }

            return KillerType.Other;
        }

        /// <summary>
        /// WildSpawnType roles for bosses and their guards. Prefix matching keeps this
        /// working when BSG adds new bosses without a mod update.
        /// </summary>
        private static bool IsBossRole(string role)
        {
            return role.StartsWith("boss", StringComparison.OrdinalIgnoreCase)
                || role.StartsWith("follower", StringComparison.OrdinalIgnoreCase)
                || role.StartsWith("sectant", StringComparison.OrdinalIgnoreCase)
                || role.StartsWith("arenaFighter", StringComparison.OrdinalIgnoreCase)
                || role.StartsWith("pmcBot", StringComparison.OrdinalIgnoreCase)
                || role.Contains("Zryachiy", StringComparison.OrdinalIgnoreCase);
        }
    }
}
