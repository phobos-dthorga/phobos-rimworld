using System.Collections.Generic;
using System.Linq;
using System.Text;
using LudeonTK;
using Phobos.Framework.Bills;
using Phobos.Framework.Debug;
using Phobos.Framework.Rules;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// On an upgrade tree: its nodes are priced like our fittings and tunes (Framework 0.106.0; owner, 13 Nov 2026: upgrades "greatly need to
/// be increased in both resource cost and that of invested time ... reference that of our tunes"). Put on our own trees only; every
/// other author's tree keeps the costs its author wrote.
/// </summary>
public sealed class PhobosUpgradeCostsLikeTunes : DefModExtension
{
}

/// <summary>
/// Prices the nodes of every opted-in upgrade tree by the rule that prices fittings and tunes (FitCostRule): the node's own figures in XML
/// are the base, raised once by the rules Def's uplifts to the fittings' scale, then by the vehicle's weight and the player's two presets,
/// "Time to fit and tune" and "Materials to fit and tune". Vehicle Framework reads a node's work and ingredients live, so writing them to
/// the node is enough; nothing is saved and nothing is patched. Bulk materials scale; a part counted in ones (an alternator, a wheel set)
/// stays as the node or its parts bill asks.
/// </summary>
[StaticConstructorOnStartup]
public static class UpgradePricing
{
    sealed class Base
    {
        public float work;
        public List<ThingDefCountClass> ingredients = new();
        /// <summary>The list this service last wrote, so a parts bill that rewrites the node later is taken as the new base.</summary>
        public List<ThingDefCountClass>? written;
    }

    static readonly Dictionary<UpgradeNode, Base> bases = new();

    static PhobosVehicleRulesDef Rules => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();
    static VehicleSystemsSettings Settings => VehicleSystemsMod.Settings;

    /// <summary>How many nodes were priced at the last pass.</summary>
    public static int Priced { get; private set; }

    static UpgradePricing()
    {
        // The parts bills fill some nodes' ingredients at start-up; price after them, never before.
        _ = PartsBills.Applied;
        Reprice();
    }

    /// <summary>Prices every opted-in tree again, from the base figures. Called at start-up and whenever a cost setting changes.</summary>
    public static void Reprice()
    {
        Priced = 0;
        PhobosVehicleRulesDef rules = Rules;
        if (rules == null) return;
        var seen = new HashSet<UpgradeTreeDef>();
        foreach (VehicleDef vehicle in DefDatabase<VehicleDef>.AllDefsListForReading)
        {
            UpgradeTreeDef? tree = vehicle.GetCompProperties<CompProperties_UpgradeTree>()?.def;
            if (tree?.nodes == null || tree.GetModExtension<PhobosUpgradeCostsLikeTunes>() == null || !seen.Add(tree)) continue;
            double kilograms = Powertrain.KerbFor(vehicle, null, null);
            double workFactor = FitCostRule.NodeFactor(rules.upgradeWorkUplift, WeightFactor(kilograms, rules.fitWorkWeightExponent), Settings.fitTimeFactor);
            double materialFactor = FitCostRule.NodeFactor(rules.upgradeMaterialUplift, WeightFactor(kilograms, rules.fitMaterialWeightExponent), Settings.fitMaterialFactor);
            foreach (UpgradeNode node in tree.nodes)
            {
                Base basis = BaseOf(node);
                node.work = FitCostRule.Work(basis.work, workFactor);
                node.ingredients = basis.ingredients
                    .Select(i => new ThingDefCountClass(i.thingDef, FitCostRule.IsBulk(i.thingDef.stackLimit, rules.upgradeBulkStackAbove) ? FitCostRule.Count(i.count, materialFactor) : i.count))
                    .ToList();
                basis.written = node.ingredients;
                Priced++;
            }
        }
    }

    static double WeightFactor(double kilograms, double exponent)
    {
        PhobosVehicleRulesDef rules = Rules;
        return Settings.fitCostScalesWithWeight
            ? FitCostRule.WeightFactor(kilograms, rules.fitCostReferenceKilograms, exponent, rules.fitWeightFactorMin, rules.fitWeightFactorMax)
            : 1;
    }

    static Base BaseOf(UpgradeNode node)
    {
        if (!bases.TryGetValue(node, out Base? basis) || basis == null)
        {
            basis = new Base { work = node.work, ingredients = Copy(node.ingredients) };
            bases[node] = basis;
        }
        else if (!ReferenceEquals(node.ingredients, basis.written))
        {
            // Something else (a parts bill worked out again) wrote the node since we did: its list is the new base.
            basis.ingredients = Copy(node.ingredients);
        }
        return basis;
    }

    static List<ThingDefCountClass> Copy(List<ThingDefCountClass>? list) =>
        list == null ? new List<ThingDefCountClass>() : list.Select(i => new ThingDefCountClass(i.thingDef, i.count)).ToList();

    /// <summary>Whether this node is one we price.</summary>
    public static bool Prices(UpgradeNode? node) => node != null && bases.ContainsKey(node);

    /// <summary>The node's own figures before our pricing: what an upgrade unlocked before Framework 0.106.0 was paid at.</summary>
    public static List<ThingDefCountClass> BaseIngredients(UpgradeNode node) => Copy(BaseOf(node).ingredients);

    /// <summary>Every priced node with its base and its price now, in the log: for checking a change of settings or of the rules Def.</summary>
    [DebugAction(DebugCategory.Name, "Vehicle systems: list upgrade prices", allowedGameStates = AllowedGameStates.Entry | AllowedGameStates.Playing)]
    static void ListPrices()
    {
        var text = new StringBuilder("[D'thorga dev] Upgrades priced like tunes (" + Priced + " nodes):");
        foreach (KeyValuePair<UpgradeNode, Base> pair in bases)
        {
            text.Append("\n  ").Append(pair.Key.key).Append(": work ").Append(pair.Value.work.ToString("0")).Append(" -> ").Append(pair.Key.work.ToString("0"));
            text.Append("; ").Append(string.Join(", ", pair.Value.ingredients.Select(i => i.thingDef.defName + " " + i.count)));
            text.Append(" -> ").Append(string.Join(", ", pair.Key.ingredients.Select(i => i.thingDef.defName + " " + i.count)));
        }
        Log.Message(text.ToString());
    }
}

/// <summary>What one vehicle paid for one priced upgrade, so taking it off refunds a share of that and not of today's price.</summary>
public sealed class PaidUpgrade : IExposable
{
    public string vehicle = "";
    public string node = "";
    public List<ThingDefCountClass> paid = new();

    public void ExposeData()
    {
        Scribe_Values.Look(ref vehicle, "vehicle", "");
        Scribe_Values.Look(ref node, "node", "");
        Scribe_Collections.Look(ref paid, "paid", LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            paid ??= new List<ThingDefCountClass>();
            paid.RemoveAll(p => p?.thingDef == null);    // a material whose mod was removed
        }
    }
}

/// <summary>
/// The prices our vehicles' upgrades were bought at (Framework 0.106.0). Vehicle Framework refunds a share of a node's ingredients as they
/// stand when the upgrade is taken off, so without this an upgrade bought before the prices rose, or at a lighter preset, would refund
/// more than it cost: a loop that makes steel from nothing. Safe to add to a running game; an upgrade with no record refunds from the
/// node's own figures, the price before 0.106.0.
/// </summary>
public sealed class GameComponent_PhobosUpgradePrices : GameComponent
{
    List<PaidUpgrade> records = new();

    public GameComponent_PhobosUpgradePrices(Game game) { }

    public static GameComponent_PhobosUpgradePrices? Store => Current.Game?.GetComponent<GameComponent_PhobosUpgradePrices>();

    public void Record(Thing vehicle, UpgradeNode node, List<ThingDefCountClass> paid)
    {
        records.RemoveAll(r => r.vehicle == vehicle.ThingID && r.node == node.key);
        records.Add(new PaidUpgrade { vehicle = vehicle.ThingID, node = node.key, paid = paid });
    }

    /// <summary>What this vehicle paid for this node, taken off the books; null when nothing was recorded.</summary>
    public List<ThingDefCountClass>? Take(Thing vehicle, UpgradeNode node)
    {
        PaidUpgrade? found = records.FirstOrDefault(r => r.vehicle == vehicle.ThingID && r.node == node.key);
        if (found == null) return null;
        records.Remove(found);
        return found.paid;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref records, "PhobosUpgradePrices_Paid", LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) records ??= new List<PaidUpgrade>();
    }
}

/// <summary>A priced upgrade has just been finished: remember what it cost, for a fair refund later. Never skips the original.</summary>
[HarmonyLib.HarmonyPatch(typeof(CompUpgradeTree), nameof(CompUpgradeTree.FinishUnlock))]
public static class UpgradePaidPatch
{
    public static void Postfix(CompUpgradeTree __instance, UpgradeNode node)
    {
        if (!UpgradePricing.Prices(node) || !__instance.NodeUnlocked(node)) return;
        GameComponent_PhobosUpgradePrices.Store?.Record(__instance.Vehicle, node, node.ingredients.Select(i => new ThingDefCountClass(i.thingDef, i.count)).ToList());
    }
}

/// <summary>
/// A priced upgrade is being taken off: for the length of Vehicle Framework's refund, the node asks what this vehicle paid (or, with no
/// record, the price before 0.106.0), then its priced ingredients are put back. The prefix never skips the original; the finalizer always
/// restores, so a fault in the refund leaves the node priced as before.
/// </summary>
[HarmonyLib.HarmonyPatch(typeof(CompUpgradeTree), nameof(CompUpgradeTree.ResetUnlock))]
public static class UpgradeRefundPatch
{
    public static void Prefix(CompUpgradeTree __instance, UpgradeNode node, out List<ThingDefCountClass>? __state)
    {
        __state = null;
        if (!UpgradePricing.Prices(node) || !__instance.NodeUnlocked(node)) return;
        List<ThingDefCountClass> paid = GameComponent_PhobosUpgradePrices.Store?.Take(__instance.Vehicle, node) ?? UpgradePricing.BaseIngredients(node);
        __state = node.ingredients;
        node.ingredients = paid;
    }

    public static void Finalizer(UpgradeNode node, List<ThingDefCountClass>? __state)
    {
        if (__state != null) node.ingredients = __state;
    }
}
