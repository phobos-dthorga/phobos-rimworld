using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Phobos.Framework.Environment;

/// <summary>The fires burning on a thing itself, as the game attaches them to pawns and vehicles (CompAttachBase), and the one way our mods put them out.</summary>
public static class AttachedFires
{
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
