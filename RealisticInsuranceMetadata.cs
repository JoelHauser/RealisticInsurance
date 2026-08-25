using SPTarkov.Server.Core.Models.Spt.Mod;

namespace RealisticInsurance
{
    // 4.1 exposes IModMetadata as a plain interface; 4.0 has AbstractModMetadata,
    // an abstract record, so every member is an override. The 4.1 HasPrepatcher
    // flag is called IsBundleMod here, and is nullable.
    public record RealisticInsuranceMetadata : AbstractModMetadata
    {
        public override string ModGuid { get; init; } = "com.mybutthasarash.realisticinsurance";
        public override string Name { get; init; } = "Realistic Insurance";
        public override string Author { get; init; } = "JoelHauser";
        public override List<string>? Contributors { get; init; }
        public override SemanticVersioning.Version Version { get; init; } = new("0.0.92");
        public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
        public override bool? IsBundleMod { get; init; } = false;
        public override List<string>? Incompatibilities { get; init; }
        public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
        public override string? Url { get; init; } = "https://github.com/JoelHauser/RealisticInsurance";
        public override string License { get; init; } = "MIT";
    }
}
