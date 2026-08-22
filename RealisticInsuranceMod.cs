using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Models.Utils;
using RealisticInsurance.Patches;

namespace RealisticInsurance
{
    [Injectable(TypePriority = OnLoadOrder.PostSptModLoader)]
    public class RealisticInsuranceMod(
        ModHelper modHelper,
#pragma warning disable CS0618 // ConfigServer is the 4.0 way; 4.1 injects configs directly
        ConfigServer configServer,
#pragma warning restore CS0618
        DatabaseService databaseService,
        // 4.1 collects patches as IEnumerable<IRuntimePatch>; that interface does not
        // exist in 4.0, where AbstractPatch implements nothing. The patches are
        // injected by concrete type instead and handed to a PatchManager.
        CaptureKillerPatch captureKillerPatch,
        StampPackagesPatch stampPackagesPatch,
        PackageContextPatch packageContextPatch,
        ReturnChancePatch returnChancePatch,
        ISptLogger<RealisticInsuranceMod> logger) : IOnLoad
    {
        internal static RealisticInsuranceConfig? Config { get; private set; }

        public Task OnLoad()
        {
            // 4.1 injects config records directly; 4.0 has no DI registration for
            // them, so InsuranceConfig comes from ConfigServer.
#pragma warning disable CS0618
            var insuranceConfig = configServer.GetConfig<InsuranceConfig>();
#pragma warning restore CS0618

            var path = Path.Combine(modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly()), "config");
            Config = modHelper.GetJsonDataFromFile<RealisticInsuranceConfig>(path, "config.json");

            if (Config is null || !Config.Enabled)
            {
                logger.Info("[RealisticInsurance] disabled via config; SPT's default insurance behaviour is unchanged.");
                return Task.CompletedTask;
            }

            // SPT skips the whole delete pass when this is false, which would silently
            // bypass this mod entirely. Warn rather than fail.
            if (!insuranceConfig.SimulateItemsBeingTaken)
            {
                logger.Warning("[RealisticInsurance] insurance.json has simulateItemsBeingTaken=false, so no items are ever lost and this mod will have no effect. Set it to true.");
            }

            // A modded trader that offers insurance but never registers itself in
            // insurance.json makes vanilla SPT throw. This mod handles those traders
            // itself, but the user should still know they are misconfigured.
            var registered = insuranceConfig.ReturnChancePercent?.Keys.ToHashSet() ?? new();
            foreach (var (traderId, trader) in databaseService.GetTraders())
            {
                if (trader?.Base?.Insurance?.Availability != true || registered.Contains(traderId))
                {
                    continue;
                }

                logger.Warning($"[RealisticInsurance] trader '{trader.Base.Nickname}' ({traderId}) offers insurance but is not listed in insurance.json returnChancePercent. Vanilla SPT would throw on it; this mod will use the '{Config.BaseReturnChancePercent.Other}%' fallback instead.");
            }

            var patchManager = new PatchManager { PatcherName = "RealisticInsurance" };
            patchManager.AddPatches(
            [
                captureKillerPatch,
                stampPackagesPatch,
                packageContextPatch,
                returnChancePatch
            ]);
            patchManager.EnablePatches();

            logger.Info($"[RealisticInsurance] loaded - pmc {Config.BaseReturnChancePercent.Pmc}% / playerScav {Config.BaseReturnChancePercent.PlayerScav}% / scav {Config.BaseReturnChancePercent.Scav}% / boss {Config.BaseReturnChancePercent.Boss}% / other {Config.BaseReturnChancePercent.Other}%, looter extracts {Config.LooterExtractedChancePercent}% (+{Config.LooterDiedBonusPercent}% when they don't)");
            return Task.CompletedTask;
        }
    }
}
