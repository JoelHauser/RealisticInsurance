using System.Text.Json.Serialization;

namespace RealisticInsurance
{
    internal class RealisticInsuranceConfig
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("baseReturnChancePercent")]
        public BaseReturnChances BaseReturnChancePercent { get; set; } = new();

        [JsonPropertyName("pmcLevelScaling")]
        public PmcLevelScaling PmcLevelScaling { get; set; } = new();

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

    /// <summary>
    /// Killer level pushes the two factors in opposite directions: a high-level PMC
    /// takes less of your gear but is more likely to walk out with what they took.
    /// </summary>
    internal class PmcLevelScaling
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>Level at which no adjustment is applied.</summary>
        [JsonPropertyName("pivotLevel")]
        public double PivotLevel { get; set; } = 25;

        /// <summary>Return chance added per level above the pivot (they take less).</summary>
        [JsonPropertyName("returnChancePerLevel")]
        public double ReturnChancePerLevel { get; set; } = 0.6;

        /// <summary>Extraction chance added per level above the pivot.</summary>
        [JsonPropertyName("extractChancePerLevel")]
        public double ExtractChancePerLevel { get; set; } = 1.0;

        [JsonPropertyName("maxReturnAdjustPercent")]
        public double MaxReturnAdjustPercent { get; set; } = 20;

        [JsonPropertyName("maxExtractAdjustPercent")]
        public double MaxExtractAdjustPercent { get; set; } = 30;

        public double ReturnAdjustFor(int level)
            => Math.Clamp((level - PivotLevel) * ReturnChancePerLevel, -MaxReturnAdjustPercent, MaxReturnAdjustPercent);

        public double ExtractAdjustFor(int level)
            => Math.Clamp((level - PivotLevel) * ExtractChancePerLevel, -MaxExtractAdjustPercent, MaxExtractAdjustPercent);
    }

    internal class BaseReturnChances
    {
        [JsonPropertyName("pmc")]
        public double Pmc { get; set; } = 55;

        [JsonPropertyName("playerScav")]
        public double PlayerScav { get; set; } = 70;

        [JsonPropertyName("boss")]
        public double Boss { get; set; } = 40;

        [JsonPropertyName("other")]
        public double Other { get; set; } = 90;
    }
}
