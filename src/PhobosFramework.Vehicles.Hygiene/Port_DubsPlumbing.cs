using System;
using System.Collections.Generic;
using DubsBadHygiene;
using Phobos.Framework.Hygiene;
using Verse;

namespace Phobos.VehicleSystems.Hygiene;

/// <summary>
/// Water to and from Dubs Bad Hygiene's plumbing under or beside the tanker. The load remembers the worst quality it was drawn at, and
/// contaminated water pumped into a tower contaminates it, so a tanker cannot wash bad water clean. Dubs' towers decide treated or
/// untreated for themselves from their net's filters.
/// </summary>
public class Port_DubsPlumbing : TankPort
{
    public override float Draw(CompPhobosTank tank, float most, out string? reason)
    {
        reason = null;
        if (PlumbingFeed.LiteMode) return 0f;
        PlumbingNet? net = FindNet(tank);
        if (net == null)
        {
            reason = "PhobosVehicleSystems_WaterNoPlumbing".Translate().Resolve();
            return 0f;
        }
        // Take a smaller drink when the towers cannot give a whole step's worth.
        float litres = Math.Min(most, net.WaterStorage);
        if (litres < 1f || PlumbingFeed.TryPullFrom(net, litres, out ContaminationLevel quality) != WaterDraw.Flowing)
        {
            reason = "PhobosVehicleSystems_WaterTowersEmpty".Translate().Resolve();
            return 0f;
        }
        tank.Quality = Math.Max(tank.Quality, (int)quality);
        return litres;
    }

    public override float Give(CompPhobosTank tank, float most, out string? reason)
    {
        reason = null;
        if (PlumbingFeed.LiteMode) return 0f;
        PlumbingNet? net = FindNet(tank);
        if (net == null)
        {
            reason = "PhobosVehicleSystems_WaterNoPlumbing".Translate().Resolve();
            return 0f;
        }
        float given = most - net.PushWater(most);
        if (given <= 0f)
        {
            reason = "PhobosVehicleSystems_TankTowersFull".Translate().Resolve();
            return 0f;
        }
        if (tank.Quality >= (int)ContaminationLevel.Contaminated)
        {
            foreach (CompWaterStorage tower in net.WaterTowers) tower.WaterQuality = ContaminationLevel.Contaminated;
        }
        return given;
    }

    static PlumbingNet? FindNet(CompPhobosTank tank)
    {
        Map map = tank.Vehicle.Map;
        foreach (IntVec3 cell in tank.ContactCells())
        {
            if (!cell.InBounds(map)) continue;
            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                CompPipe? pipe = things[i].TryGetComp<CompPipe>();
                if (pipe == null || pipe.mode != PipeType.Sewage || pipe.closed || pipe.pipeNet is not { } net) continue;
                if (net.WaterTowers.Count > 0) return net;
            }
        }
        return null;
    }
}
