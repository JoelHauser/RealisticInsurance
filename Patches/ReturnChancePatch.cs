using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Utils;

namespace RealisticInsurance.Patches
{
    /// <summary>
    /// SPT decides each item's fate in InsuranceController.RollForDelete(traderId, item),
    /// which is reached from both the regular-item path and the attachment path - so
    /// replacing it covers all insured gear, not just what was equipped on death.
    ///
    /// RollForDelete only receives a traderId, not the package, so we cannot tell which
    /// raid it belongs to from inside it. FindItemsToDelete runs once per package and
    /// does receive it, so we use it to publish the current package as ambient context.
    /// </summary>
    [Injectable(InjectionType.Transient, int.MaxValue)]
    public class ReturnChancePatch : AbstractPatch
    {
        private static RandomUtil _randomUtil = null!;
        private static ISptLogger<ReturnChancePatch> _logger = null!;

        [ThreadStatic] private static KillerType? _currentKillerType;
        [ThreadStatic] private static bool _currentLooterExtracted;
        [ThreadStatic] private static bool _currentPackageIsLegacy;

        public ReturnChancePatch(RandomUtil randomUtil, ISptLogger<ReturnChancePatch> logger)
        {
            _randomUtil = randomUtil;
            _logger = logger;
        }

        protected override MethodBase? GetTargetMethod()
        {
            return typeof(InsuranceController)
                .GetMethod("RollForDelete", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        internal static void SetContext(SPTarkov.Server.Core.Models.Eft.Profile.Insurance insured)
        {
            _currentKillerType = null;
            _currentLooterExtracted = false;
            _currentPackageIsLegacy = true;

            var ext = insured.ExtensionData;
            if (ext is null) return;

            if (ext.TryGetValue(KillerContext.ExtKeyType, out var rawType)
                && Enum.TryParse<KillerType>(rawType?.ToString(), true, out var parsed))
            {
                _currentKillerType = parsed;
                _currentPackageIsLegacy = false;
            }

            if (ext.TryGetValue(KillerContext.ExtKeyExtracted, out var rawExtracted))
            {
                _currentLooterExtracted = rawExtracted switch
                {
                    bool b => b,
                    System.Text.Json.JsonElement je => je.ValueKind == System.Text.Json.JsonValueKind.True,
                    _ => bool.TryParse(rawExtracted?.ToString(), out var b2) && b2
                };
            }
        }

        internal static void ClearContext() => _currentKillerType = null;

        /// <summary>Returns true when the item should be DELETED, matching SPT's contract.</summary>
        [PatchPrefix]
        public static bool Prefix(ref bool? __result, MongoId traderId, Item? insuredItem)
        {
            var config = RealisticInsuranceMod.Config;
            if (config is null || !config.Enabled)
            {
                return true; // fall through to SPT
            }

            // Package predates this mod (or came from another source): optionally let
            // SPT's flat per-trader chance handle it.
            if (_currentPackageIsLegacy && config.LegacyPackageBehaviour.Equals("spt", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var killerType = _currentKillerType ?? KillerType.Other;

            var chance = killerType switch
            {
                KillerType.Pmc => config.BaseReturnChancePercent.Pmc,
                KillerType.PlayerScav => config.BaseReturnChancePercent.PlayerScav,
                KillerType.Boss => config.BaseReturnChancePercent.Boss,
                _ => config.BaseReturnChancePercent.Other
            };

            // Second factor: if the looter never made it out, the gear is more likely
            // to be recoverable.
            if (!_currentLooterExtracted)
            {
                chance += config.LooterDiedBonusPercent;
            }

            if (config.TraderModifierPercent.TryGetValue(traderId.ToString(), out var traderMod))
            {
                chance += traderMod;
            }

            chance = Math.Clamp(chance, 0d, 100d);

            // Same distribution as SPT: uniform 0-99, delete when roll >= returnChance.
            var roll = _randomUtil.GetInt(0, 9999) / 100;
            var shouldDelete = roll >= chance;

            if (config.LogRolls)
            {
                _logger.Info($"[RealisticInsurance] {killerType} / extracted={_currentLooterExtracted} -> return {chance}% | roll {roll} -> {(shouldDelete ? "LOST" : "returned")}");
            }

            __result = shouldDelete;
            return false; // skip original
        }
    }

    /// <summary>
    /// Publishes the package being processed so ReturnChancePatch can read its stamped
    /// killer data. One package at a time, per thread.
    /// </summary>
    [Injectable(InjectionType.Transient, int.MaxValue)]
    public class PackageContextPatch : AbstractPatch
    {
        protected override MethodBase? GetTargetMethod()
        {
            return typeof(InsuranceController)
                .GetMethod("FindItemsToDelete", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        public static void Prefix(SPTarkov.Server.Core.Models.Eft.Profile.Insurance insured)
            => ReturnChancePatch.SetContext(insured);

        [PatchPostfix]
        public static void Postfix() => ReturnChancePatch.ClearContext();
    }
}
