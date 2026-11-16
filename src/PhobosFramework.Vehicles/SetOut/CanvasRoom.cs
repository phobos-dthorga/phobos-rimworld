using RimWorld;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A canvas wall of a trailer's room (Framework 0.121.0; docs/development/vehicle-trailers.md, B0.4). When one goes, shot down or packed, the
/// canvas roof comes off first: the game drops a roof whose walls have gone, and a falling roof hurts whoever is beneath it. No Harmony.
/// </summary>
public class Building_PhobosCanvasWall : Building
{
    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        SetOuts.Owner(this)?.LiftRoof();
        base.DeSpawn(mode);
    }
}

/// <summary>A canvas room's flap door: the game's own door, which lifts the room's roof before it goes, as the walls do.</summary>
public class Building_PhobosCanvasDoor : Building_Door
{
    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        SetOuts.Owner(this)?.LiftRoof();
        base.DeSpawn(mode);
    }
}
