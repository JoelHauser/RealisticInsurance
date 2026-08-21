using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services.Bot;
using SPTarkov.Server.Core.Services.InRaid;
using SPTarkov.Server.Core.Utils;

namespace RealisticInsurance.Patches
{
    /// <summary>
    /// Reads the killer at raid end and rolls whether they extracted.
    ///
    /// This targets HandlePostRaidPmc rather than the insurance call itself for two
    /// ordering reasons, both verified against 4.1.3:
    ///
    ///   1. HandlePostRaidPmc clears MatchBotDetailsCacheService at the END of the
    ///      method, but the insurance packages are created part-way through it. A
    ///      prefix here still sees a populated bot cache - which is the only place the
    ///      killer's LEVEL exists, since the Aggressor block carries no level.
    ///   2. Aggressor.ProfileId is not assigned until AFTER insurance is handled, so
    ///      the reliable killer id at this point is request.Results.KillerId.
    ///
    /// Only pmcUSEC / pmcBEAR are cached by SPT, so levels resolve for PMC killers
    /// only. Scav and boss kills fall back to their flat config buckets.
    /// </summary>
    [Injectable(InjectionType.Transient, int.MaxValue)]
    public class CaptureKillerPatch : AbstractPatch
    {
        private static MatchBotDetailsCacheService _botCache = null!;
        private static RandomUtil _randomUtil = null!;
        private static ISptLogger<CaptureKillerPatch> _logger = null!;

        internal static readonly ConcurrentDictionary<string, KillerContext> Pending = new();

        public CaptureKillerPatch(
            MatchBotDetailsCacheService botCache,
            RandomUtil randomUtil,
            ISptLogger<CaptureKillerPatch> logger)
        {
            _botCache = botCache;
            _randomUtil = randomUtil;
            _logger = logger;
        }

        protected override MethodBase? GetTargetMethod()
        {
            return typeof(LocationLifecycleService)
                .GetMethod("HandlePostRaidPmc", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        public static void Prefix(MongoId sessionId, bool isDead, EndLocalRaidRequestData request)
        {
            var config = RealisticInsuranceMod.Config;
            if (config is null || !config.Enabled)
            {
                return;
            }

            var profile = request?.Results?.Profile;
            var killerType = isDead ? KillerContext.Classify(profile) : KillerType.Other;

            int? killerLevel = null;
            if (isDead && killerType == KillerType.Pmc)
            {
                // Only PMCs are in SPT's cache; a miss here is normal, not an error.
                killerLevel = _botCache.GetBotById(request?.Results?.KillerId)?.Level;
            }

            var comp = config.LooterCompetence;

            // Mean competence: level is only a hint, and only PMCs have one.
            double mean = killerType switch
            {
                KillerType.PlayerScav => comp.MeanWhenPlayerScav,
                KillerType.Scav => comp.MeanWhenScav,
                KillerType.Boss => comp.MeanWhenBoss,
                KillerType.Pmc => killerLevel.HasValue ? comp.MeanForLevel(killerLevel.Value) : comp.CompetenceAtPivot,
                _ => comp.MeanWhenOther
            };

            // Wildcard raids ignore level completely - the level 50 hatchling runner
            // and the level 8 prodigy both exist.
            var wildcard = comp.Enabled && _randomUtil.GetChance100(comp.WildcardChancePercent);

            double competence;
            if (!comp.Enabled)
            {
                competence = mean;
            }
            else if (wildcard)
            {
                competence = _randomUtil.GetDouble(0d, 100d);
            }
            else
            {
                competence = Math.Clamp(_randomUtil.GetNormallyDistributedRandomNumber(mean, comp.Sigma), 0d, 100d);
            }

            var extractChance = Math.Clamp(
                config.LooterExtractedChancePercent + (competence - 50d) * comp.ExtractPerCompetencePoint,
                0d, 100d);

            var ctx = new KillerContext
            {
                Type = killerType,
                KillerLevel = killerLevel,
                Competence = competence,
                Wildcard = wildcard,
                LooterExtracted = _randomUtil.GetChance100(extractChance)
            };

            Pending[sessionId.ToString()] = ctx;

            if (config.LogRolls)
            {
                // Diagnostic: SPT only creates an insurance package when the client
                // reports lost insured items, so surface that count directly.
                var lostCount = request?.LostInsuredItems?.Count() ?? -1;
                _logger.Info($"[RealisticInsurance] raid end diag: isDead={isDead}, exit={request?.Results?.Result}, lostInsuredItems={lostCount} (0 or -1 means SPT will NOT create an insurance package)");

                _logger.Info($"[RealisticInsurance] raid end: killer={killerType}, level={(killerLevel?.ToString() ?? "unknown")}, competence={competence:0.#}{(wildcard ? " (WILDCARD)" : "")}, extractChance={extractChance:0.#}% -> extracted={ctx.LooterExtracted}");
            }
        }
    }

    /// <summary>
    /// Stamps the captured context onto the insurance packages created during this
    /// raid. Insurance.ExtensionData is [JsonExtensionData], so it persists in
    /// profile.json and survives the server restart that can happen before the
    /// return timer fires.
    /// </summary>
    [Injectable(InjectionType.Transient, int.MaxValue)]
    public class StampPackagesPatch : AbstractPatch
    {
        private static SaveServer _saveServer = null!;

        public StampPackagesPatch(SaveServer saveServer) => _saveServer = saveServer;

        protected override MethodBase? GetTargetMethod()
        {
            return typeof(SPTarkov.Server.Core.Services.Commerce.InsuranceService)
                .GetMethod("StartPostRaidInsuranceLostProcess", BindingFlags.Instance | BindingFlags.Public);
        }

        /// <summary>Count existing packages so the postfix only stamps this raid's.</summary>
        [PatchPrefix]
        public static void Prefix(MongoId sessionID, out int __state)
        {
            __state = _saveServer.GetProfile(sessionID)?.InsuranceList?.Count ?? 0;
        }

        [PatchPostfix]
        public static void Postfix(MongoId sessionID, int __state)
        {
            var config = RealisticInsuranceMod.Config;
            if (config is null || !config.Enabled)
            {
                return;
            }

            if (!CaptureKillerPatch.Pending.TryRemove(sessionID.ToString(), out var ctx))
            {
                return;
            }

            var list = _saveServer.GetProfile(sessionID)?.InsuranceList;
            if (list is null)
            {
                return;
            }

            for (var i = __state; i < list.Count; i++)
            {
                var package = list[i];
                // SPT declares ExtensionData with a nullable key type, so constructing
                // it trips CS8714. Nothing here ever writes a null key.
#pragma warning disable CS8714
                package.ExtensionData ??= new Dictionary<string?, object?>();
#pragma warning restore CS8714
                package.ExtensionData[KillerContext.ExtKeyType] = ctx.Type.ToString();
                package.ExtensionData[KillerContext.ExtKeyExtracted] = ctx.LooterExtracted;
                package.ExtensionData[KillerContext.ExtKeyCompetence] = ctx.Competence;
                package.ExtensionData[KillerContext.ExtKeyWildcard] = ctx.Wildcard;
                if (ctx.KillerLevel.HasValue)
                {
                    package.ExtensionData[KillerContext.ExtKeyLevel] = ctx.KillerLevel.Value;
                }
            }
        }
    }
}
