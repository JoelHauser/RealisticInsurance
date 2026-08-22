using System.Reflection;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace RealisticInsurance.Patches
{
    /// <summary>
    /// Decides, per package, WHICH items the looter walked off with.
    ///
    /// SPT rolls each item independently at a flat chance. Independent rolls
    /// concentrate: 12 items at 55% lands in the 40-60% band nearly 60% of the time,
    /// which is why vanilla returns feel identical every raid. Choosing a COUNT once
    /// per raid and then picking that many items weighted by price gives both the
    /// spread (some raids you are cleaned out, some barely touched) and the shape
    /// (they take the expensive things first).
    /// </summary>
    [Injectable(InjectionType.Transient, TypePriority = int.MaxValue)]
    public class PackageContextPatch : AbstractPatch
    {
        private static RagfairPriceService _priceService = null!;
        private static RandomUtil _randomUtil = null!;
        private static ItemHelper _itemHelper = null!;
        private static ISptLogger<PackageContextPatch> _logger = null!;

        public PackageContextPatch(
            RagfairPriceService priceService,
            RandomUtil randomUtil,
            ItemHelper itemHelper,
            ISptLogger<PackageContextPatch> logger)
        {
            _priceService = priceService;
            _randomUtil = randomUtil;
            _itemHelper = itemHelper;
            _logger = logger;
        }

        protected override MethodBase? GetTargetMethod()
        {
            return typeof(InsuranceController)
                .GetMethod("FindItemsToDelete", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        public static void Prefix(SPTarkov.Server.Core.Models.Eft.Profile.Insurance insured)
        {
            RaidLootPlan.Build(insured, _priceService, _randomUtil, _itemHelper, _logger);
        }

        [PatchPostfix]
        public static void Postfix()
        {
            RaidLootPlan.Clear();
        }
    }

    /// <summary>
    /// The per-package plan, published to RollForDelete as ambient state.
    /// </summary>
    internal static class RaidLootPlan
    {
        [ThreadStatic] private static HashSet<MongoId>? _taken;
        [ThreadStatic] private static double _fallbackReturnChance;
        [ThreadStatic] private static bool _active;
        [ThreadStatic] private static bool _legacy;

        internal static bool Active => _active;
        internal static bool Legacy => _legacy;
        internal static double FallbackReturnChance => _fallbackReturnChance;
        internal static bool HasPlan => _taken is not null;

        internal static bool WasTaken(MongoId id)
        {
            return _taken?.Contains(id) == true;
        }

        internal static void Clear()
        {
            _taken = null;
            _active = false;
            _legacy = false;

            // Safe default: if anything goes wrong, gear comes BACK rather than
            // being destroyed. Previously this kept the last package's value, and a
            // stale 0 here deletes an entire package.
            _fallbackReturnChance = 100d;
        }

        internal static void Build(
            SPTarkov.Server.Core.Models.Eft.Profile.Insurance insured,
            RagfairPriceService priceService,
            RandomUtil randomUtil,
            ItemHelper itemHelper,
            ISptLogger<PackageContextPatch> logger)
        {
            Clear();

            var config = RealisticInsuranceMod.Config;
            if (config is null || !config.Enabled)
            {
                return;
            }

            _active = true;

            var ext = insured.ExtensionData;
            var killerType = KillerType.Other;
            var competence = 50d;
            var looterExtracted = false;

            if (ext is not null
                && ext.TryGetValue(KillerContext.ExtKeyType, out var rawType)
                && Enum.TryParse<KillerType>(rawType?.ToString(), true, out var parsedType))
            {
                killerType = parsedType;
                competence = ReadDouble(ext, KillerContext.ExtKeyCompetence, 50d);
                looterExtracted = ReadBool(ext, KillerContext.ExtKeyExtracted);
            }
            else
            {
                // Insured before this mod existed, so there is no killer to reason about.
                _legacy = true;
                if (config.LegacyPackageBehaviour.Equals("spt", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            var returnChance = killerType switch
            {
                KillerType.Pmc => config.BaseReturnChancePercent.Pmc,
                KillerType.PlayerScav => config.BaseReturnChancePercent.PlayerScav,
                KillerType.Scav => config.BaseReturnChancePercent.Scav,
                KillerType.Boss => config.BaseReturnChancePercent.Boss,
                _ => config.BaseReturnChancePercent.Other
            };

            // If they never got out, the gear is recoverable regardless of skill.
            if (!looterExtracted)
            {
                returnChance += config.LooterDiedBonusPercent;
            }

            if (config.TraderModifierPercent.TryGetValue(insured.TraderId.ToString(), out var traderMod))
            {
                returnChance += traderMod;
            }

            returnChance = Math.Clamp(returnChance, 0d, 100d);

            // Competence shifts how much they take, and the SIGN depends on who they
            // are: a skilled PMC is picky, a skilled player scav is greedy.
            var fractionTaken = 1d - (returnChance / 100d);
            if (config.Greed.Enabled)
            {
                fractionTaken += (competence - 50d) * config.Greed.PerCompetencePoint.For(killerType);
            }

            // Never absolute in either direction: a chad still lifts a good gun to
            // sell, and the greediest looter still leaves something.
            fractionTaken = Math.Clamp(
                fractionTaken,
                config.ValueWeightedLooting.MinFractionTaken,
                config.ValueWeightedLooting.MaxFractionTaken);

            // Keep the attachment-counting fallback consistent with the plan.
            _fallbackReturnChance = (1d - fractionTaken) * 100d;

            var items = insured.Items;
            if (!config.ValueWeightedLooting.Enabled || items is null || items.Count == 0)
            {
                return; // fall back to a per-item probability roll
            }

            var jitter = config.ValueWeightedLooting.CountJitter;
            if (jitter > 0d)
            {
                fractionTaken *= 1d + randomUtil.GetDouble(-jitter, jitter);
            }

            var targetEntries = (int)Math.Round(Math.Clamp(fractionTaken, 0d, 1d) * items.Count);
            _taken = SelectRoots(items, targetEntries, config.ValueWeightedLooting.GreedBias,
                priceService, randomUtil, out var rootsTaken, out var rootsTotal);

            if (config.LogRolls)
            {
                logger.Info(
                    $"[RealisticInsurance] package {insured.TraderId}: killer={killerType}, competence={competence:0.#}, extracted={looterExtracted} -> target {fractionTaken * 100d:0.#}% | took {rootsTaken}/{rootsTotal} kit item(s) = {_taken.Count}/{items.Count} entries");

                // Listing what was taken vs kept is the only way to confirm the
                // value weighting is actually biting.
                foreach (var item in items)
                {
                    var price = priceService.GetDynamicItemPrice(item.Template, Money.ROUBLES) ?? 0d;
                    var verdict = _taken.Contains(item.Id) ? "LOST " : "kept ";
                    logger.Info($"[RealisticInsurance]   {verdict} {itemHelper.GetItemName(item.Template)} ({price:N0} RUB)");
                }
            }
        }

        /// <summary>
        /// Picks whole KIT ITEMS, not individual database entries.
        ///
        /// A 34-entry package is typically only ~5 real objects: a weapon and its 12
        /// attachments, a rig and its 14 inserts, a helmet, a backpack. Selecting
        /// entries individually and weighting by price targets the roots anyway (they
        /// are the expensive ones), and taking a root drags every child with it - so
        /// "3 entries taken" silently meant "your gun, rig and helmet, and everything
        /// on them". That emptied entire packages.
        ///
        /// So: candidates are roots, each priced INCLUDING its attachments, and the
        /// budget is spent in whole objects until the entry target is met.
        /// </summary>
        private static HashSet<MongoId> SelectRoots(
            List<Item> items,
            int targetEntries,
            double greedBias,
            RagfairPriceService priceService,
            RandomUtil randomUtil,
            out int rootsTaken,
            out int rootsTotal)
        {
            var chosen = new HashSet<MongoId>();
            rootsTaken = 0;

            // Item.ParentId is a string while Item.Id is a MongoId, so the graph is
            // keyed by string to avoid converting on every comparison.
            var byId = new Dictionary<string, Item>(items.Count, StringComparer.Ordinal);
            foreach (var item in items)
            {
                byId[item.Id.ToString()] = item;
            }

            var childrenOf = new Dictionary<string, List<Item>>(StringComparer.Ordinal);
            var roots = new List<Item>();
            foreach (var item in items)
            {
                // A root is anything whose parent is not itself inside this package -
                // i.e. the equipment slots, not the mods bolted onto them.
                if (item.ParentId is not null && byId.ContainsKey(item.ParentId))
                {
                    if (!childrenOf.TryGetValue(item.ParentId, out var list))
                    {
                        childrenOf[item.ParentId] = list = new List<Item>();
                    }

                    list.Add(item);
                }
                else
                {
                    roots.Add(item);
                }
            }

            rootsTotal = roots.Count;
            if (targetEntries <= 0 || roots.Count == 0)
            {
                return chosen;
            }

            IEnumerable<Item> Descendants(Item root)
            {
                var stack = new Stack<Item>();
                stack.Push(root);
                while (stack.Count > 0)
                {
                    var cur = stack.Pop();
                    if (!childrenOf.TryGetValue(cur.Id.ToString(), out var kids))
                    {
                        continue;
                    }

                    foreach (var kid in kids)
                    {
                        yield return kid;
                        stack.Push(kid);
                    }
                }
            }

            double PriceOf(Item item)
                => priceService.GetDynamicItemPrice(item.Template, Money.ROUBLES) ?? 0d;

            // A kitted weapon is worth its mods, so price each root as the whole object.
            var pool = new List<(Item Root, List<Item> Group, double Weight)>(roots.Count);
            foreach (var root in roots)
            {
                var group = new List<Item> { root };
                group.AddRange(Descendants(root));

                var value = 0d;
                foreach (var member in group)
                {
                    value += PriceOf(member);
                }

                pool.Add((root, group, Math.Pow(Math.Max(value, 1d), greedBias)));
            }

            // Each kit item gets its OWN roll rather than a shared budget being spent
            // greedily. A budget stops the moment it is met, which - given a rig or a
            // weapon is most of a package - meant almost every raid lost exactly one
            // thing. Whether a looter grabs your rig AND your helmet is up to them.
            //
            // Probabilities are scaled so the EXPECTED entries taken still equals the
            // target, while the actual count varies: sometimes nothing, sometimes
            // three things.
            var weightedSize = 0d;
            foreach (var entry in pool)
            {
                weightedSize += entry.Weight * entry.Group.Count;
            }

            if (weightedSize <= 0d)
            {
                return chosen;
            }

            foreach (var entry in pool)
            {
                var chance = Math.Clamp(targetEntries * entry.Weight / weightedSize, 0d, 1d);
                if (randomUtil.GetDouble(0d, 1d) >= chance)
                {
                    continue;
                }

                foreach (var member in entry.Group)
                {
                    chosen.Add(member.Id);
                }

                rootsTaken++;
            }

            return chosen;
        }

        private static double ReadDouble(IDictionary<string?, object?> ext, string key, double fallback)
        {
            if (!ext.TryGetValue(key, out var raw) || raw is null)
            {
                return fallback;
            }

            return raw switch
            {
                double d => d,
                int i => i,
                System.Text.Json.JsonElement je when je.TryGetDouble(out var v) => v,
                _ => double.TryParse(raw.ToString(), out var v2) ? v2 : fallback
            };
        }

        private static bool ReadBool(IDictionary<string?, object?> ext, string key)
        {
            if (!ext.TryGetValue(key, out var raw) || raw is null)
            {
                return false;
            }

            return raw switch
            {
                bool b => b,
                System.Text.Json.JsonElement je => je.ValueKind == System.Text.Json.JsonValueKind.True,
                _ => bool.TryParse(raw.ToString(), out var b2) && b2
            };
        }
    }

    /// <summary>
    /// Answers SPT's per-item question from the plan built above.
    /// </summary>
    [Injectable(InjectionType.Transient, TypePriority = int.MaxValue)]
    public class ReturnChancePatch : AbstractPatch
    {
        private static RandomUtil _randomUtil = null!;
        private static InsuranceConfig _insuranceConfig = null!;

        public ReturnChancePatch(RandomUtil randomUtil, InsuranceConfig insuranceConfig)
        {
            _randomUtil = randomUtil;
            _insuranceConfig = insuranceConfig;
        }

        /// <summary>
        /// SPT indexes insuranceConfig.ReturnChancePercent[traderId] directly, having
        /// only guarded that the trader exists in the trader table. A modded trader
        /// that offers insurance without registering itself in insurance.json throws
        /// KeyNotFoundException there. Never hand such a trader back to SPT.
        /// </summary>
        private static bool SptCanHandle(MongoId traderId)
        {
            return _insuranceConfig?.ReturnChancePercent?.ContainsKey(traderId) == true;
        }

        protected override MethodBase? GetTargetMethod()
        {
            return typeof(InsuranceController)
                .GetMethod("RollForDelete", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        /// <summary>Returns true when the item should be DELETED, matching SPT's contract.</summary>
        [PatchPrefix]
        public static bool Prefix(ref bool? __result, MongoId traderId, Item? insuredItem)
        {
            var config = RealisticInsuranceMod.Config;

            var deferToSpt = config is null || !config.Enabled || !RaidLootPlan.Active
                || (RaidLootPlan.Legacy && config.LegacyPackageBehaviour.Equals("spt", StringComparison.OrdinalIgnoreCase));

            if (deferToSpt)
            {
                // Only if SPT can actually price this trader; otherwise it would throw.
                if (SptCanHandle(traderId))
                {
                    return true;
                }

                // Unknown (modded) trader: answer it ourselves rather than crash.
                var fallbackChance = config?.BaseReturnChancePercent.Other ?? 90d;
                __result = (_randomUtil.GetInt(0, 9999) / 100) >= fallbackChance;
                return false;
            }

            // A known item and a plan: the answer was already decided for this raid.
            if (insuredItem is not null && RaidLootPlan.HasPlan)
            {
                __result = RaidLootPlan.WasTaken(insuredItem.Id);
                return false;
            }

            // No item supplied - SPT is counting how many attachments to strip, so this
            // stays a probability roll.
            var roll = _randomUtil.GetInt(0, 9999) / 100;
            __result = roll >= RaidLootPlan.FallbackReturnChance;
            return false;
        }
    }
}
