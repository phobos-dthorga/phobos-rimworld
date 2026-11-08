using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// On every vehicle caravan (added to Vehicle Framework's VehicleCaravan world object by an XML patch): says in the caravan's inspect
/// text why a vehicle of it will not move, and offers the caravan jump-start when one needs it (docs/development/vehicle-world-map-design.md).
/// Presentation only: the rules are the comp's and JumpStarts'.
/// </summary>
public class WorldObjectComp_PhobosVehicleCaravan : WorldObjectComp
{
    VehicleCaravan? Caravan => parent as VehicleCaravan;

    static IEnumerable<CompPhobosVehicleSystems> Comps(VehicleCaravan caravan) =>
        caravan.VehiclesListForReading.Select(v => v.GetComp<CompPhobosVehicleSystems>()).Where(c => c != null && c.Usable)!;

    public override string? CompInspectStringExtra()
    {
        VehicleCaravan? caravan = Caravan;
        if (caravan == null) return null;
        var lines = new List<string>();
        foreach (CompPhobosVehicleSystems comp in Comps(caravan))
        {
            if (comp.MoveRefusal != null)
            {
                lines.Add("PhobosVehicleSystems_CaravanRefusal".Translate(comp.Vehicle.LabelShort.Named("VEHICLE"), comp.MoveRefusal.Named("REASON")).Resolve());
            }
        }
        return lines.Count == 0 ? null : string.Join("\n", lines);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        VehicleCaravan? caravan = Caravan;
        if (caravan == null || caravan.Faction != Faction.OfPlayer) yield break;
        foreach (CompPhobosVehicleSystems comp in Comps(caravan).ToList())
        {
            Gizmo? gizmo = JumpStarts.CaravanGizmo(caravan, comp);
            if (gizmo != null) yield return gizmo;
        }
    }
}
