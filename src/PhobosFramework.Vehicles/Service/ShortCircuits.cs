using HarmonyLib;
using Phobos.Framework.Persistence;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The game's short circuit reaches a vehicle plugged into the same grid (Framework 0.187.0; docs/development/vehicle-emergent-play.md, EM27).
/// When a conduit or a building shorts out, every vehicle of the player's whose hook-up lead joins that power net takes the surge through
/// its lead: its wiring is shorted, as a hit on its battery shorts it (cross-links G12), and the switched loads on the cut circuits stop
/// until it is repaired or patched. The vehicle's own battery is not drained: the game drains only its batteries. The condition's cause
/// says it came from the grid. A prefix that never skips the game's own short circuit; it reads the net before the short can change it.
/// </summary>
[HarmonyPatch(typeof(ShortCircuitUtility), nameof(ShortCircuitUtility.DoShortCircuit))]
public static class ShortCircuitReachesVehicles
{
    /// <summary>The surge's severity on the wiring (authored: a bad hit's worth, past the first stage, short of the worst).</summary>
    public const float Severity = 0.4f;

    const string Condition = "PhobosVehicleCondition_ShortedWiring";
    const string System = "PhobosVehicleSystem_Electrical";
    const string Role = "Battery";

    public static void Prefix(Building culprit)
    {
        PowerNet? net = culprit?.PowerComp?.PowerNet;
        Map? map = culprit?.Map;
        if (net == null || map == null || VehicleSystemsMod.Settings.showOnly) return;
        PhobosVehicleConditionDef? shorted = DefDatabase<PhobosVehicleConditionDef>.GetNamedSilentFail(Condition);
        if (shorted == null) return;
        foreach (Pawn pawn in map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
        {
            if (pawn is not VehiclePawn vehicle || vehicle.GetComp<CompPhobosVehicleSystems>() is not { Usable: true } comp || !OnNet(vehicle, map, net)) continue;
            comp.AddCondition(Role, shorted, Severity, System, CauseNote.Words("PhobosVehicleSystems_CauseShortCircuit", vehicle.Tile));
            Messages.Message("PhobosVehicleSystems_ShortCircuitReached".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve(), vehicle, MessageTypeDefOf.NegativeEvent);
            Seen.Note("vehicles.short-circuit-surge", vehicle);
        }
    }

    /// <summary>Whether the vehicle's lead joins this net: a cell under it carries the net (its hook-up conduit lies under its footprint).</summary>
    static bool OnNet(VehiclePawn vehicle, Map map, PowerNet net)
    {
        foreach (IntVec3 cell in vehicle.OccupiedRect())
        {
            if (cell.InBounds(map) && map.powerNetGrid.TransmittedPowerNetAt(cell) == net) return true;
        }
        return false;
    }
}
