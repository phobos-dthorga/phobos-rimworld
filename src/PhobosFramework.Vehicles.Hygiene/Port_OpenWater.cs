using DubsBadHygiene;
using Phobos.VehicleSystems;
using Verse;

namespace Phobos.VehicleSystems.Hygiene;

/// <summary>
/// Water drawn straight from a river, lake or the sea beside the vehicle (Framework 0.187.0; docs/development/vehicle-emergent-play.md, EM21:
/// a pump judged by what it can reach, not by a list of buildings). A contact cell on water terrain is enough. The water comes in untreated,
/// as Dubs Bad Hygiene counts water from the land, and the load keeps the worst quality it was drawn at, so a tanker filled at a river
/// does not pass for treated water. Draw only: nothing pumps a load back into a river.
/// </summary>
public class Port_OpenWater : TankPort
{
    public override float Draw(CompPhobosTank tank, float most, out string? reason)
    {
        reason = null;
        Map? map = tank.Vehicle.Map;
        if (map == null) return 0f;
        foreach (IntVec3 cell in tank.ContactCells())
        {
            if (!cell.InBounds(map) || cell.GetTerrain(map) is not { IsWater: true }) continue;
            tank.Quality = System.Math.Max(tank.Quality, (int)ContaminationLevel.Untreated);
            Seen.Note("vehicles.river-intake", tank.Vehicle);
            return System.Math.Min(most, tank.Space);
        }
        reason = "PhobosVehicleSystems_WaterNoOpenWater".Translate().Resolve();
        return 0f;
    }
}
