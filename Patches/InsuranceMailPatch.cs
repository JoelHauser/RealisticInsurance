using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Commerce;
using SPTarkov.Server.Core.Utils;

namespace RealisticInsurance.Patches
{
    /// <summary>
    /// Replaces the trader's insurance mail with a note describing what actually
    /// happened to the kit.
    ///
    /// Vanilla sends a locale TEMPLATE ID and the client looks the text up, so
    /// the message can only ever be one of a handful of fixed strings - it
    /// cannot mention the looter, because vanilla has no looter to mention.
    /// MailSendService.SendDirectNpcMessageToPlayer takes literal text instead
    /// and stores it in the profile's dialogue, so the note persists and reads
    /// back correctly forever after.
    ///
    /// The items ride along on the same message, so this is still ONE mail from
    /// the trader, not a note bolted onto the side of the real delivery.
    /// </summary>
    [Injectable(InjectionType.Transient, int.MaxValue)]
    public class InsuranceMailPatch : AbstractPatch
    {
        private static MailSendService _mailSendService = null!;
        private static LocationTable _locationTable = null!;
        private static RandomUtil _randomUtil = null!;
        private static ISptLogger<InsuranceMailPatch> _logger = null!;

        public InsuranceMailPatch(
            MailSendService mailSendService,
            LocationTable locationTable,
            RandomUtil randomUtil,
            ISptLogger<InsuranceMailPatch> logger)
        {
            _mailSendService = mailSendService;
            _locationTable = locationTable;
            _randomUtil = randomUtil;
            _logger = logger;
        }

        protected override MethodBase? GetTargetMethod()
        {
            return typeof(InsuranceController)
                .GetMethod("SendMail", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        /// <summary>Returns false to suppress SPT's own mail, matching its contract.</summary>
        [PatchPrefix]
        public static bool Prefix(MongoId sessionId, SPTarkov.Server.Core.Models.Eft.Profile.Insurance insurance)
        {
            // Consumed unconditionally, including on every path that defers: an
            // outcome left behind would be picked up by the next package in the
            // loop and tell that package's owner somebody else's story.
            var outcome = PackageOutcomeStore.Take();

            var config = RealisticInsuranceMod.Config;
            if (config is null || !config.Enabled || !config.TraderMessages.Enabled)
            {
                return true;
            }

            // No outcome means no plan was built - a package insured before this
            // mod was installed, or simulateItemsBeingTaken switched off. Nothing
            // is known about a looter, so there is no story to tell.
            if (outcome is null)
            {
                return true;
            }

            if (InsuranceDisabledAtLocation(insurance))
            {
                return true;
            }

            var entriesReturned = insurance.Items?.Count ?? 0;
            var text = InsuranceMessages.Compose(
                outcome,
                insurance.TraderId,
                entriesReturned,
                config.TraderMessages.IncludeItemNames,
                _randomUtil);

            _mailSendService.SendDirectNpcMessageToPlayer(
                sessionId,
                insurance.TraderId.ToString(),
                insurance.MessageType ?? MessageType.InsuranceReturn,
                text,
                insurance.Items,
                insurance.MaxStorageTime,
                insurance.SystemData);

            if (config.LogRolls)
            {
                _logger.Info($"[RealisticInsurance] mail to {insurance.TraderId} ({entriesReturned}/{outcome.EntriesTotal} entries returned): {text.Replace("\n", " | ")}");
            }

            return false;
        }

        /// <summary>
        /// Labs and Labyrinth refuse insurance returns unless the location is
        /// edited, and SPT empties the package and sends its own failure message
        /// for them. That is the MAP forbidding the return, not a looter taking
        /// anything, so telling this mod's story over the top of it would be a
        /// lie -- hand those back to SPT untouched.
        ///
        /// This mirrors InsuranceController.IsMapLabsAndInsuranceDisabled and
        /// its Labyrinth twin, both non-public. Duplicating two lines is cheaper
        /// than reflecting into them, but it does mean a new no-insurance map
        /// would need adding here too.
        /// </summary>
        private static bool InsuranceDisabledAtLocation(SPTarkov.Server.Core.Models.Eft.Profile.Insurance insurance)
        {
            var location = insurance.SystemData?.Location;
            if (string.IsNullOrEmpty(location))
            {
                return false;
            }

            if (!location.Equals("laboratory", StringComparison.OrdinalIgnoreCase)
                && !location.Equals("labyrinth", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return _locationTable.GetLocation(location)?.Base?.Insurance != true;
        }
    }
}
