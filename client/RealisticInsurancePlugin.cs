using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using EFT;
using HarmonyLib;
using SPT.Common.Http;

namespace RealisticInsuranceClient
{
    /// <summary>
    /// Tells the server what was still on the player when they died.
    ///
    /// The server cannot work this out alone. The raid-end payload gives it a
    /// flat list of lost insured items with no world position and no timestamps,
    /// so gear looted off a corpse is indistinguishable from a helmet dropped
    /// twenty minutes earlier. Without this, a killer's greed gets applied to
    /// items they never saw.
    ///
    /// One snapshot at the moment of death is enough. Anything insured that is
    /// missing from it left under the player's own control, whether that was
    /// twenty minutes or twenty seconds before - so no timestamps are needed,
    /// and there is no need to hook every drop.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class RealisticInsurancePlugin : BaseUnityPlugin
    {
        // Deliberately identical to the server mod's ModGuid. The Forge checks
        // that both halves declare the registered GUID, and BepInEx keeps its
        // own plugin registry, so there is nothing here for the server mod to
        // collide with.
        public const string PluginGuid = "com.mybutthasarash.realisticinsurance";
        public const string PluginName = "Realistic Insurance (client)";
        public const string PluginVersion = "0.0.92";

        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            new Harmony(PluginGuid).PatchAll(typeof(DeathSnapshotPatch));
            Log.LogInfo("[RealisticInsurance] client loaded");
        }
    }

    internal static class DeathSnapshotPatch
    {
        [HarmonyPatch(typeof(Player), nameof(Player.OnDead))]
        [HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        private static void AfterDeath(Player __instance)
        {
            try
            {
                // Bots die constantly; only the local player's kit is insured.
                if (__instance == null || !__instance.IsYourPlayer)
                {
                    return;
                }

                var inventory = __instance.Profile?.InventoryInfo;
                if (inventory == null)
                {
                    RealisticInsurancePlugin.Log.LogWarning("[RealisticInsurance] no inventory at death; the server will treat everything as looted.");
                    return;
                }

                var ids = inventory.AllRealPlayerItems
                    .Where(item => item != null)
                    .Select(item => item.Id.ToString())
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Distinct()
                    .ToList();

                Send(ids);

                RealisticInsurancePlugin.Log.LogInfo(
                    $"[RealisticInsurance] death snapshot sent: {ids.Count} item(s) still on the body");
            }
            catch (Exception ex)
            {
                // A failure here must never disturb the death sequence. The server
                // simply falls back to treating the whole package as looted.
                RealisticInsurancePlugin.Log.LogError("[RealisticInsurance] death snapshot failed: " + ex);
            }
        }

        /// <summary>
        /// Hand-built JSON rather than a serializer dependency. The payload is a
        /// flat array of ids, and item ids are hex MongoIds, so nothing here
        /// needs escaping.
        /// </summary>
        private static void Send(IReadOnlyList<string> ids)
        {
            var json = new StringBuilder(ids.Count * 28 + 16);
            json.Append("{\"ids\":[");
            for (var i = 0; i < ids.Count; i++)
            {
                if (i > 0) json.Append(',');
                json.Append('"').Append(ids[i]).Append('"');
            }
            json.Append("]}");

            RequestHandler.PostJson("/realisticinsurance/corpse", json.ToString());
        }
    }
}
