using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Common.Models.Logging;

namespace RealisticInsurance.Patches
{
    /// <summary>
    /// Insurance packages are created at raid end but processed on a timer
    /// (insurance.json runIntervalSeconds, default 600s) potentially hours later and
    /// across a server restart. The package model carries no killer information, so we
    /// capture it here and stamp it onto the package's [JsonExtensionData], which
    /// persists in profile.json for free.
    ///
    /// The "did the looter extract" roll happens ONCE here rather than per item: the
    /// killer either got out with your gear or didn't, and rolling it per item would
    /// let one corpse both escape and not escape.
    /// </summary>
    [Injectable(InjectionType.Transient, int.MaxValue)]
    public class CaptureKillerPatch : AbstractPatch
    {
        private static SaveServer _saveServer = null!;
        private static RandomUtil _randomUtil = null!;
        private static ISptLogger<CaptureKillerPatch> _logger = null!;

        public CaptureKillerPatch(SaveServer saveServer, RandomUtil randomUtil, ISptLogger<CaptureKillerPatch> logger)
        {
            _saveServer = saveServer;
            _randomUtil = randomUtil;
            _logger = logger;
        }

        protected override MethodBase? GetTargetMethod()
        {
            return typeof(SPTarkov.Server.Core.Services.Commerce.InsuranceService)
                .GetMethod("StartPostRaidInsuranceLostProcess", BindingFlags.Instance | BindingFlags.Public);
        }

        /// <summary>Record how many packages already existed so the postfix only stamps new ones.</summary>
        [PatchPrefix]
        public static void Prefix(MongoId sessionID, out int __state)
        {
            __state = _saveServer.GetProfile(sessionID)?.InsuranceList?.Count ?? 0;
        }

        [PatchPostfix]
        public static void Postfix(PmcData pmcData, MongoId sessionID, int __state)
        {
            var config = RealisticInsuranceMod.Config;
            if (config is null || !config.Enabled)
            {
                return;
            }

            var list = _saveServer.GetProfile(sessionID)?.InsuranceList;
            if (list is null || list.Count <= __state)
            {
                return;
            }

            var killerType = KillerContext.Classify(pmcData);
            var looterExtracted = _randomUtil.GetChance100(config.LooterExtractedChancePercent);

            for (var i = __state; i < list.Count; i++)
            {
                var package = list[i];
                package.ExtensionData ??= new Dictionary<string?, object?>();
                package.ExtensionData[KillerContext.ExtKeyType] = killerType.ToString();
                package.ExtensionData[KillerContext.ExtKeyExtracted] = looterExtracted;
            }

            if (config.LogRolls)
            {
                _logger.Info($"[RealisticInsurance] raid end: killer={killerType}, looterExtracted={looterExtracted}, packages stamped={list.Count - __state}");
            }
        }
    }
}
