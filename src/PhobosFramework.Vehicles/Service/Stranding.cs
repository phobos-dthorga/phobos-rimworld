using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.WorldMap;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>One vehicle that holds its convoy up: why, what could fix it here, and how far home is.</summary>
public readonly struct StrandedVehicle
{
    public readonly VehicleCaravan Caravan;
    public readonly CompPhobosVehicleSystems Comp;
    public readonly string Reason;

    public StrandedVehicle(VehicleCaravan caravan, CompPhobosVehicleSystems comp, string reason)
    {
        Caravan = caravan;
        Comp = comp;
        Reason = reason;
    }
}

/// <summary>
/// Stranded convoys (Framework 0.181.0; docs/development/vehicle-emergent-play.md, EM1): a player's convoy away from any map whose vehicle
/// will not move (one of our refusals, or an empty tank) is a state with choices, not only a halt. This service says which vehicle, why,
/// what aboard the whole convoy could fix it now, and how far home is; the alert and the logbook read it. Reads cached state only.
/// </summary>
public static class Stranding
{
    /// <summary>Why this player's vehicle cannot go on with its convoy, or null: a refusal of ours, or an empty tank. Null on a map.</summary>
    public static string? Reason(CompPhobosVehicleSystems comp)
    {
        VehiclePawn vehicle = comp.Vehicle;
        if (vehicle.Spawned || vehicle.Faction != Faction.OfPlayer || !comp.Usable) return null;
        if (comp.MoveRefusal != null) return comp.MoveRefusal;
        if (vehicle.CompFueledTravel is { EmptyTank: true } && vehicle.CompFueledTravel.Props.fuelType != null)
        {
            return "PhobosVehicleSystems_StrandedNoFuel".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve();
        }
        return null;
    }

    /// <summary>Every vehicle holding up one of the player's convoys now.</summary>
    public static IEnumerable<StrandedVehicle> All()
    {
        if (Find.WorldObjects == null) yield break;
        foreach (Caravan caravan in Find.WorldObjects.Caravans)
        {
            if (caravan is not VehicleCaravan convoy || convoy.Faction != Faction.OfPlayer) continue;
            foreach (VehiclePawn vehicle in convoy.VehiclesListForReading)
            {
                if (vehicle.GetComp<CompPhobosVehicleSystems>() is not { Usable: true } comp) continue;
                if (Reason(comp) is { } reason) yield return new StrandedVehicle(convoy, comp, reason);
            }
        }
    }

    /// <summary>What could fix it here, in words: a repair the convoy holds the supplies for, a jump-start a donor could give, what a
    /// repair still needs, or fuel to carry. Never empty.</summary>
    public static string FixLine(StrandedVehicle stranded)
    {
        CompPhobosVehicleSystems comp = stranded.Comp;
        if (comp.Repairs.Ready is { } ready) return "PhobosVehicleSystems_StrandedFixHere".Translate(ready.Label.Named("WORK")).Resolve();
        if (comp.Repairs.Pending.Count > 0) return comp.Repairs.NeedsLine(comp.Repairs.Pending[0]);
        if (comp.Vehicle.CompFueledTravel is { EmptyTank: true }) return "PhobosVehicleSystems_StrandedFixFuel".Translate().Resolve();
        return "PhobosVehicleSystems_StrandedFixAsk".Translate().Resolve();
    }

    /// <summary>How far home is, in words; empty where the player has no settlement.</summary>
    public static string HomeLine(StrandedVehicle stranded)
    {
        Settlement? home = WorldPlaces.NearestHome(stranded.Caravan.Tile, out float tiles);
        if (home == null) return "";
        return "PhobosVehicleSystems_StrandedHome".Translate(WorldPlaces.DistanceText(tiles).Named("DISTANCE"), home.LabelCap.Named("HOME")).Resolve();
    }

    /// <summary>The convoy's cargo, for repairs at a halt: everything in every vehicle's and walker's inventory (Framework 0.181.0: the
    /// supplies were counted in the broken vehicle's own cargo alone, though fuel came from all of it).</summary>
    public static IEnumerable<Thing> ConvoyCargo(Caravan caravan) => CaravanInventoryUtility.AllInventoryItems(caravan);
}

/// <summary>
/// The alert for a stranded convoy (Framework 0.181.0, EM1): names each vehicle holding a convoy up, why, what could fix it there and how
/// far home is. Presentation only: the service decides.
/// </summary>
public class Alert_PhobosConvoyStranded : Alert
{
    readonly List<StrandedVehicle> found = new();
    readonly List<GlobalTargetInfo> culprits = new();

    public Alert_PhobosConvoyStranded()
    {
        defaultLabel = "PhobosVehicleSystems_StrandedAlert".Translate();
        defaultPriority = AlertPriority.High;
    }

    void Refresh()
    {
        found.Clear();
        culprits.Clear();
        found.AddRange(Stranding.All());
        foreach (VehicleCaravan caravan in found.Select(s => s.Caravan).Distinct()) culprits.Add(caravan);
    }

    public override AlertReport GetReport()
    {
        Refresh();
        return found.Count == 0 ? AlertReport.Inactive : AlertReport.CulpritsAre(culprits);
    }

    public override TaggedString GetExplanation()
    {
        var lines = new List<string> { "PhobosVehicleSystems_StrandedAlertIntro".Translate() };
        foreach (StrandedVehicle stranded in found)
        {
            string home = Stranding.HomeLine(stranded);
            lines.Add("PhobosVehicleSystems_StrandedAlertLine".Translate(stranded.Caravan.LabelCap.Named("CONVOY"), stranded.Comp.Vehicle.LabelShort.Named("VEHICLE"),
                stranded.Reason.Named("REASON"), Stranding.FixLine(stranded).Named("FIX")).Resolve() + (home.NullOrEmpty() ? "" : " " + home));
        }
        return string.Join("\n\n", lines);
    }
}
