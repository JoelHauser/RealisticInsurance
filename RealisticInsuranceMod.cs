using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Spt.Config;

namespace RealisticInsurance
{
    [Injectable(TypePriority = OnLoadOrder.PostLoad)]
    public class RealisticInsuranceMod(
        ModHelper modHelper,
        InsuranceConfig insuranceConfig,
        IEnumerable<IRuntimePatch> patches,
        ISptLogger<RealisticInsuranceMod> logger) : IOnLoad
    {
        internal static RealisticInsuranceConfig? Config { get; private set; }

        public Task OnLoadAsync(CancellationToken cancellationToken)
        {
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

            foreach (var patch in patches)
            {
                patch.Enable();
            }

            logger.Info($"[RealisticInsurance] loaded - pmc {Config.BaseReturnChancePercent.Pmc}% / playerScav {Config.BaseReturnChancePercent.PlayerScav}% / scav {Config.BaseReturnChancePercent.Scav}% / boss {Config.BaseReturnChancePercent.Boss}% / other {Config.BaseReturnChancePercent.Other}%, looter extracts {Config.LooterExtractedChancePercent}% (+{Config.LooterDiedBonusPercent}% when they don't)");
            return Task.CompletedTask;
        }
    }
}
