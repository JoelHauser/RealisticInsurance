using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace RealisticInsurance
{
    /// <summary>
    /// What the player still had on them at the moment they died.
    ///
    /// The raid-end payload cannot answer this. LostInsuredItems is a flat list
    /// of items with no world position and no timestamp, and EndRaidResult only
    /// carries the killer, the exit and a play time - so the server has no way
    /// to tell gear looted off a corpse from a helmet dropped twenty minutes
    /// earlier. Stock SPT never faces the question because it applies one flat
    /// number to everything.
    ///
    /// The client knows, though. It sends the contents of the player's inventory
    /// the instant they die; anything insured that is missing from that list left
    /// under the player's own control, so the killer never saw it.
    /// </summary>
    internal class CorpseSnapshotRequest : IRequestData
    {
        [JsonPropertyName("ids")]
        public List<string>? Ids { get; set; }
    }

    internal static class CorpseSnapshotStore
    {
        private static readonly ConcurrentDictionary<string, HashSet<string>> Pending = new();

        internal static void Record(MongoId sessionId, IEnumerable<string>? ids)
        {
            Pending[sessionId.ToString()] = ids is null
                ? new HashSet<string>(StringComparer.Ordinal)
                : new HashSet<string>(ids, StringComparer.Ordinal);
        }

        /// <summary>
        /// Taken rather than peeked: a snapshot belongs to one death, and leaving
        /// it behind would let the next raid inherit it.
        /// </summary>
        internal static HashSet<string>? Take(MongoId sessionId)
        {
            return Pending.TryRemove(sessionId.ToString(), out var ids) ? ids : null;
        }
    }

    [Injectable]
    public class CorpseSnapshotRouter : StaticRouter
    {
        // 4.0's RouteAction takes no CancellationToken; that parameter arrived in
        // 4.1. Otherwise this is identical to the 4.1 line's router.
        public CorpseSnapshotRouter(JsonUtil jsonUtil, ISptLogger<CorpseSnapshotRouter> logger)
            : base(jsonUtil, new List<RouteAction>
            {
                new RouteAction<CorpseSnapshotRequest>(
                    "/realisticinsurance/corpse",
                    (url, info, sessionId, output) =>
                    {
                        var count = info?.Ids?.Count ?? 0;
                        CorpseSnapshotStore.Record(sessionId, info?.Ids);

                        if (RealisticInsuranceMod.Config?.LogRolls == true)
                        {
                            logger.Info($"[RealisticInsurance] corpse snapshot: {count} item(s) still on the body at death");
                        }

                        return new ValueTask<string>(string.Empty);
                    })
            })
        {
        }
    }
}
