using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Phobos.Framework.Environment;

/// <summary>The fires burning on a thing itself, as the game attaches them to pawns and vehicles (CompAttachBase), and the one way our mods put them out.</summary>
public static class AttachedFires
{
    /// <summary>
    /// Whether the thing is on fire: a fire burning on it, or on the ground under any cell of it (Framework 0.220.0; vehicle-fire.md, FR1).
    /// The game's own IsBurning() sees a fire attached to a thing only when the thing is one cell; for anything larger it looks only at
    /// the ground, so a burning truck read as not burning. Use this for vehicles and other things larger than a cell.
    /// </summary>
    public static bool OnFire(Thing thing)
    {
        if (thing == null || thing.Destroyed || !thing.Spawned) return false;
        if (thing is ThingWithComps withComps && withComps.GetComp<CompAttachBase>()?.attachments is { Count: > 0 } attachments)
        {
            foreach (AttachableThing attachment in attachments)
            {
                if (attachment is not Fire { Destroyed: false }) continue;
                // Seen in play: a fire the game's own check would have missed, on a thing larger than a cell.
                if (thing.def.size != IntVec2.One) Seen.Note("framework.fire-on-vehicle", thing);
                return true;
            }
        }
        return thing.IsBurning();
    }

    /// <summary>Puts out every fire burning on the thing, as water closing over it would, and says how many there were.</summary>
    public static int PutOut(ThingWithComps thing)
    {
        List<AttachableThing>? attachments = thing.GetComp<CompAttachBase>()?.attachments;
        if (attachments == null || attachments.Count == 0) return 0;
        var fires = new List<Fire>();
        foreach (AttachableThing attachment in attachments)
        {
            if (attachment is Fire fire) fires.Add(fire);
        }
        foreach (Fire fire in fires)
        {
            // Destroy takes the fire off the list itself; one already destroyed but still listed is only taken off.
            if (!fire.Destroyed) fire.Destroy();
            else attachments.Remove(fire);
        }
        return fires.Count;
    }
}
