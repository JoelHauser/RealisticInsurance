using System.Text.Json.Serialization;

namespace RealisticInsurance
{
    internal class RealisticInsuranceConfig
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("baseReturnChancePercent")]
        public BaseReturnChances BaseReturnChancePercent { get; set; } = new();

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
