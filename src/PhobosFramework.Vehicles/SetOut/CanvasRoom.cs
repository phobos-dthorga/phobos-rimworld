using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A canvas wall of a trailer's room (Framework 0.121.0; docs/development/vehicle-trailers.md, B0.4). When one goes, shot down or packed, the
/// canvas roof comes off first: the game drops a roof whose walls have gone, and a falling roof hurts whoever is beneath it. No Harmony.
/// </summary>
public class Building_PhobosCanvasWall : Building
{
    /// <summary>
    /// The roof this wall holds up, when it is a camp's tent and no trailer's (Framework 0.217.0): every wall and the flap of a camp tent keep the
    /// list, as an awning's poles do, so the canvas comes off with the first of them to go even where the vehicle that pitched it is lost.
    /// </summary>
    internal List<IntVec3>? roof;

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        SetOuts.Owner(this)?.LiftRoof();
        if (roof != null) CanvasRoofs.Lift(Map, roof);
        base.DeSpawn(mode);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref roof, "PhobosCanvas_Roof", LookMode.Value);
    }
}

/// <summary>A canvas room's flap door: the game's own door, which lifts the room's roof before it goes, as the walls do.</summary>
public class Building_PhobosCanvasDoor : Building_Door
{
    /// <summary>The roof of a camp's tent, as on its walls (Framework 0.217.0).</summary>
    internal List<IntVec3>? roof;

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        SetOuts.Owner(this)?.LiftRoof();
        if (roof != null) CanvasRoofs.Lift(Map, roof);
        base.DeSpawn(mode);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref roof, "PhobosCanvas_Roof", LookMode.Value);
    }
}
