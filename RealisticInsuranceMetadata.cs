using SPTarkov.Server.Core.Models.Spt.Mod;

namespace RealisticInsurance
{
    public record RealisticInsuranceMetadata : IModMetadata
    {
        public string ModGuid { get; init; } = "com.joelhauser.realisticinsurance";
        public string Name { get; init; } = "Realistic Insurance";
        public string Author { get; init; } = "JoelHauser";
        public List<string>? Contributors { get; init; }
        public SemanticVersioning.Version Version { get; init; } = new("1.0.0");
        public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
        public bool HasPrepatcher { get; init; } = false;
        public List<string>? Incompatibilities { get; init; }
        public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
        public string? Url { get; init; } = "https://github.com/JoelHauser/RealisticInsurance";
        public string License { get; init; } = "MIT";
    }
}
