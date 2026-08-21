using System.Text.Json.Serialization;

namespace RealisticInsurance
{
    internal class RealisticInsuranceConfig
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("baseReturnChancePercent")]
        public BaseReturnChances BaseReturnChancePercent { get; set; } = new();

        [JsonPropertyName("looterCompetence")]
        public LooterCompetence LooterCompetence { get; set; } = new();

        [JsonPropertyName("greed")]
        public GreedModel Greed { get; set; } = new();

        [JsonPropertyName("valueWeightedLooting")]
        public ValueWeightedLooting ValueWeightedLooting { get; set; } = new();

        [JsonPropertyName("looterExtractedChancePercent")]
        public double LooterExtractedChancePercent { get; set; } = 65;

        [JsonPropertyName("looterDiedBonusPercent")]
        public double LooterDiedBonusPercent { get; set; } = 25;

        [JsonPropertyName("traderModifierPercent")]
        public Dictionary<string, double> TraderModifierPercent { get; set; } = new();

        [JsonPropertyName("legacyPackageBehaviour")]
        public string LegacyPackageBehaviour { get; set; } = "spt";

        [JsonPropertyName("logRolls")]
        public bool LogRolls { get; set; }
    }

    internal class BaseReturnChances
    {
        [JsonPropertyName("pmc")] public double Pmc { get; set; } = 55;
        [JsonPropertyName("playerScav")] public double PlayerScav { get; set; } = 45;
        [JsonPropertyName("scav")] public double Scav { get; set; } = 75;
        [JsonPropertyName("boss")] public double Boss { get; set; } = 40;
        [JsonPropertyName("other")] public double Other { get; set; } = 90;
    }

    /// <summary>
    /// A single "how good was this looter" value rolled ONCE per raid, on 0-100.
    ///
    /// Level only sets the MEAN. Sigma decides how much level actually predicts:
    /// a wide sigma means a level 50 is frequently no better than a level 10, which
    /// is the point - level is a hint, not a rule.
    /// </summary>
    internal class LooterCompetence
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>Spread around the mean. Larger = level matters less.</summary>
        [JsonPropertyName("sigma")]
        public double Sigma { get; set; } = 22;

        /// <summary>Chance the roll ignores level entirely and draws flat 0-100.</summary>
        [JsonPropertyName("wildcardChancePercent")]
        public double WildcardChancePercent { get; set; } = 10;

        [JsonPropertyName("pivotLevel")]
        public double PivotLevel { get; set; } = 25;

        /// <summary>Competence at the pivot level.</summary>
        [JsonPropertyName("competenceAtPivot")]
        public double CompetenceAtPivot { get; set; } = 50;

        [JsonPropertyName("competencePerLevel")]
        public double CompetencePerLevel { get; set; } = 1.2;

        /// <summary>Means used when no level is available.</summary>
        [JsonPropertyName("meanWhenPlayerScav")] public double MeanWhenPlayerScav { get; set; } = 65;
        [JsonPropertyName("meanWhenScav")] public double MeanWhenScav { get; set; } = 30;
        [JsonPropertyName("meanWhenBoss")] public double MeanWhenBoss { get; set; } = 70;
        [JsonPropertyName("meanWhenOther")] public double MeanWhenOther { get; set; } = 50;

        /// <summary>Return % added per point of competence above 50 (they take less).</summary>
        [JsonPropertyName("returnPerCompetencePoint")]
        public double ReturnPerCompetencePoint { get; set; } = 0.9;

        /// <summary>Extract % added per point of competence above 50.</summary>
        [JsonPropertyName("extractPerCompetencePoint")]
        public double ExtractPerCompetencePoint { get; set; } = 0.6;

        public double MeanForLevel(int level)
            => Math.Clamp(CompetenceAtPivot + (level - PivotLevel) * CompetencePerLevel, 0d, 100d);
    }

    /// <summary>
    /// Competence and greed are different traits, and conflating them was wrong.
    ///
    ///   A skilled PMC is skilled AND PICKY   - already kitted, takes only upgrades.
    ///   A skilled player scav is skilled AND GREEDY - came in with nothing, takes everything.
    ///
    /// Both extract more often, but they empty your corpse to very different degrees.
    /// So the competence -> amount-taken relationship carries a per-killer SIGN:
    /// negative for PMCs and bosses, positive for player scavs.
    /// </summary>
    internal class GreedModel
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>Change in fraction taken per competence point above 50.</summary>
        [JsonPropertyName("perCompetencePoint")]
        public GreedSlopes PerCompetencePoint { get; set; } = new();
    }

    internal class GreedSlopes
    {
        [JsonPropertyName("pmc")] public double Pmc { get; set; } = -0.009;
        [JsonPropertyName("playerScav")] public double PlayerScav { get; set; } = 0.009;
        [JsonPropertyName("scav")] public double Scav { get; set; } = -0.004;
        [JsonPropertyName("boss")] public double Boss { get; set; } = -0.009;
        [JsonPropertyName("other")] public double Other { get; set; }

        public double For(KillerType t) => t switch
        {
            KillerType.Pmc => Pmc,
            KillerType.PlayerScav => PlayerScav,
            KillerType.Scav => Scav,
            KillerType.Boss => Boss,
            _ => Other
        };
    }

    /// <summary>
    /// Instead of an independent coin flip per item, decide how MANY items the looter
    /// took and then pick WHICH ones weighted by price. Fixing the count per raid is
    /// what produces the "cleaned out" / "barely touched" spread; weighting by price
    /// is what makes the result read as looting rather than dice.
    /// </summary>
    internal class ValueWeightedLooting
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>Price exponent. 1 = proportional to price, higher = greedier.</summary>
        [JsonPropertyName("greedBias")]
        public double GreedBias { get; set; } = 2.0;

        /// <summary>Random +/- wobble applied to the taken count, as a fraction.</summary>
        [JsonPropertyName("countJitter")]
        public double CountJitter { get; set; } = 0.15;
    }
}
