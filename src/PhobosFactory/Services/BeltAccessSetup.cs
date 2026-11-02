using System.Linq;
using RimWorld;
using Verse;

namespace Phobos.Factory;

/// <summary>
/// Gives every storage building type the "Belts may take from here" switch at load, so it covers every mod's
/// shelves without a patch per building. It starts off, except on factory hoppers and storage flagged as one
/// (VCHE's chemfuel taps, the [sbz] corpse fridge), which feed belts already.
/// </summary>
public static class BeltAccessSetup
{
    public static void AddSwitches()
    {
        int added = 0;
        int startingOn = 0;
        foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
        {
            if (def.thingClass == null || !typeof(Building_Storage).IsAssignableFrom(def.thingClass)) continue;
            if (def.comps.Any(props => props is CompProperties_BeltAccess)) continue;
            bool startsOn = BeltAccess.IsHopperDef(def);
            def.comps.Add(new CompProperties_BeltAccess { startsOn = startsOn });
            added++;
            if (startsOn) startingOn++;
        }
        Log.Message($"[D'thorga Factory Additions] Added the \"Belts may take from here\" switch to {added} storage building types, {startingOn} of them starting on.");
    }
}
