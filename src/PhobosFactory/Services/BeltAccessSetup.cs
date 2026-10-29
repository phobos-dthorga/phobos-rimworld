using System.Linq;
using PipeSystem;
using RimWorld;
using Verse;

namespace Phobos.Factory;

/// <summary>
/// Gives every storage building type the "Belts may take from here" switch at load, so it covers every mod's
/// shelves without a patch per building. Factory hoppers, and storage already flagged as one (VCHE's chemfuel
/// taps, the [sbz] corpse fridge), feed belts already and are left alone.
/// </summary>
public static class BeltAccessSetup
{
    public static void AddSwitches()
    {
        int added = 0;
        foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
        {
            if (def.thingClass == null || !typeof(Building_Storage).IsAssignableFrom(def.thingClass)) continue;
            if (def.GetModExtension<FactoryHopperExtension>()?.isfactoryHopper == true) continue;
            if (def.comps.Any(props => props is CompProperties_BeltAccess)) continue;
            def.comps.Add(new CompProperties_BeltAccess());
            added++;
        }
        Log.Message($"[D'thorga Factory Additions] Added the \"Belts may take from here\" switch to {added} storage building types.");
    }
}
