using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Vehicles.Rules;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// One kind of coupling a hitch has or a trailer takes (Framework 0.182.0; docs/development/farming-machines.md, "Coupling types"): a
/// drawbar with clevis and pin, a pintle hook and ring, a pick-up hitch, a ball, a three-point linkage. Its label and description are the
/// player's words, in XML; its defName is what CouplingRule matches.
/// </summary>
public class PhobosCouplingDef : Def
{
}

/// <summary>An adapter fitted to a hitch by an upgrade: while the node is unlocked, the hitch also takes this coupling.</summary>
public class PhobosCouplingAdapter
{
    /// <summary>The vehicle's upgrade node that fits the adapter.</summary>
    public string node = "";
    /// <summary>The coupling the adapter takes.</summary>
    public PhobosCouplingDef? takes;
}

/// <summary>
/// The couplings of a vehicle, on its ThingDef (Framework 0.182.0): what its hitch has, the one it is towed by, and the adapters its
/// upgrades fit. A vehicle without it, or with an empty list, is universal (owner, 20 Nov 2026: everything built before joins anything).
/// </summary>
public class PhobosCouplingsExtension : DefModExtension
{
    /// <summary>The couplings its hitch has. Empty: it takes any trailer.</summary>
    public List<PhobosCouplingDef> hitch = new();
    /// <summary>The coupling it is towed by, as a trailer. Null: any hitch takes it.</summary>
    public PhobosCouplingDef? towedBy;
    /// <summary>Adapters fitted to its hitch by upgrades.</summary>
    public List<PhobosCouplingAdapter> adapters = new();

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        foreach (PhobosCouplingAdapter adapter in adapters)
        {
            if (adapter.node.NullOrEmpty()) yield return "a coupling adapter names no upgrade node";
            if (adapter.takes == null) yield return "a coupling adapter names no coupling it takes";
        }
    }
}

/// <summary>The couplings of a vehicle as the hitch asks for them, and the refusal when they do not join (Framework 0.182.0).</summary>
public static class Couplings
{
    static PhobosCouplingsExtension? Of(VehiclePawn vehicle) => vehicle.def.GetModExtension<PhobosCouplingsExtension>();

    /// <summary>The couplings this vehicle's hitch has; empty for universal.</summary>
    public static List<PhobosCouplingDef> Hitch(VehiclePawn vehicle) => Of(vehicle)?.hitch ?? new List<PhobosCouplingDef>();

    /// <summary>The coupling this vehicle is towed by; null for universal.</summary>
    public static PhobosCouplingDef? TowedBy(VehiclePawn vehicle) => Of(vehicle)?.towedBy;

    /// <summary>The couplings the adapters fitted to this vehicle's hitch take now.</summary>
    public static List<PhobosCouplingDef> Adapters(VehiclePawn vehicle)
    {
        var taken = new List<PhobosCouplingDef>();
        if (Of(vehicle) is not { } couplings) return taken;
        foreach (PhobosCouplingAdapter adapter in couplings.adapters)
        {
            if (adapter.takes != null && VehicleUpgrades.Unlocked(vehicle, adapter.node)) taken.Add(adapter.takes);
        }
        return taken;
    }

    /// <summary>How this trailer joins this vehicle's hitch (CouplingRule.Match).</summary>
    public static CouplingMatch Match(VehiclePawn tractor, VehiclePawn trailer) =>
        CouplingRule.Match(Hitch(tractor).Select(c => c.defName).ToList(), TowedBy(trailer)?.defName,
            Adapters(tractor).Select(c => c.defName).ToList());

    /// <summary>Why this trailer's coupling does not join this vehicle's hitch, or null when it does.</summary>
    public static string? Refusal(VehiclePawn tractor, VehiclePawn trailer)
    {
        if (CouplingRule.Joins(Match(tractor, trailer))) return null;
        string has = Hitch(tractor).Concat(Adapters(tractor)).Select(c => c.label).Distinct().ToCommaList();
        return "PhobosHitch_CouplingMismatch".Translate(trailer.LabelShort.Named("TRAILER"), TowedBy(trailer)!.label.Named("COUPLING"),
            tractor.LabelShort.Named("TRACTOR"), has.Named("HITCHES")).Resolve();
    }

    /// <summary>The hitch's line for its inspect text, naming the couplings it takes with its adapters, or null for a universal hitch.</summary>
    public static string? HitchLine(VehiclePawn vehicle)
    {
        List<PhobosCouplingDef> hitch = Hitch(vehicle).Concat(Adapters(vehicle)).Distinct().ToList();
        return hitch.Count == 0 ? null : "PhobosHitch_LineHitchCouplings".Translate(hitch.Select(c => c.label).ToCommaList().Named("COUPLINGS")).Resolve();
    }

    /// <summary>A trailer's line for its inspect text, naming the coupling it is towed by, or null for a universal one.</summary>
    public static string? TowedLine(VehiclePawn vehicle) =>
        TowedBy(vehicle) is { } towed ? "PhobosHitch_LineTowedBy".Translate(towed.label.Named("COUPLING")).Resolve() : null;
}
