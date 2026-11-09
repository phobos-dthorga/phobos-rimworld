using System;
using PipeSystem;
using Verse;

namespace Phobos.VehicleSystems.Pipes;

/// <summary>
/// A Vanilla Expanded Framework pipe network under or beside the tanker. The tanker never registers as a connector: the framework
/// registers those at spawn from their neighbours, and a thing that moves would leave a stale one. It reads the net at its cells and
/// moves units through the net's public storage calls, only while set up.
/// </summary>
public class Port_PipeNet : TankPort
{
    public PipeNetDef? net;

    public override float Draw(CompPhobosTank tank, float most, out string? reason)
    {
        reason = null;
        PipeNet? found = Find(tank);
        if (found == null)
        {
            reason = "PhobosVehicleSystems_TankNoPipe".Translate().Resolve();
            return 0f;
        }
        float taken = Math.Min(most, found.Stored);
        if (taken <= 0f)
        {
            reason = "PhobosVehicleSystems_TankPipeEmpty".Translate().Resolve();
            return 0f;
        }
        // The net's stored figure is refreshed only now and then, so what came out, not what was asked for.
        found.DrawAmongStorage(taken, out float drawn, null);
        if (drawn <= 0f) reason = "PhobosVehicleSystems_TankPipeEmpty".Translate().Resolve();
        return drawn;
    }

    public override float Give(CompPhobosTank tank, float most, out string? reason)
    {
        reason = null;
        PipeNet? found = Find(tank);
        if (found == null)
        {
            reason = "PhobosVehicleSystems_TankNoPipe".Translate().Resolve();
            return 0f;
        }
        float given = Math.Min(most, found.AvailableCapacity);
        if (given <= 0f)
        {
            reason = "PhobosVehicleSystems_TankPipeFull".Translate().Resolve();
            return 0f;
        }
        // Its capacity counts tanks marked for transfer, which take nothing: only what was really stored leaves the tanker.
        found.DistributeAmongStorage(given, out float stored);
        if (stored <= 0f) reason = "PhobosVehicleSystems_TankPipeFull".Translate().Resolve();
        return stored;
    }

    PipeNet? Find(CompPhobosTank tank)
    {
        if (net == null) return null;
        Map map = tank.Vehicle.Map;
        PipeNetManager? manager = map.GetComponent<PipeNetManager>();
        if (manager == null) return null;
        foreach (IntVec3 cell in tank.ContactCells())
        {
            if (!cell.InBounds(map)) continue;
            PipeNet? found = manager.GetPipeNetAt(cell, net);
            if (found != null) return found;
        }
        return null;
    }
}
