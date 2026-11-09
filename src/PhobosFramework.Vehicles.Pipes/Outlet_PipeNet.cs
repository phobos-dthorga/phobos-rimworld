using PipeSystem;
using Verse;

namespace Phobos.VehicleSystems.Pipes;

/// <summary>
/// A pipe laid to a drill rig. Like the tanker, the rig never registers as a connector, because the pipe code registers those at spawn from their neighbours and a thing that moves would leave a stale one.
/// It reads the net at its cells and stores into it through the net's public call, so only what really went in leaves the buffer.
/// </summary>
public class Outlet_PipeNet : RigBufferOutlet
{
    public PipeNetDef? net;
    public ThingDef? thing;

    public override float Give(CompPhobosDrillRig rig, float most)
    {
        if (net == null || thing == null || rig.BufferKind != thing) return 0f;
        Map map = rig.Vehicle.Map;
        PipeNetManager? manager = map.GetComponent<PipeNetManager>();
        if (manager == null) return 0f;
        foreach (IntVec3 cell in rig.ContactCells())
        {
            if (!cell.InBounds(map)) continue;
            PipeNet? found = manager.GetPipeNetAt(cell, net);
            if (found == null) continue;
            float room = System.Math.Min(most, found.AvailableCapacity);
            if (room <= 0f) return 0f;
            found.DistributeAmongStorage(room, out float stored);
            return rig.TakeFromBuffer(thing, stored);
        }
        return 0f;
    }
}
