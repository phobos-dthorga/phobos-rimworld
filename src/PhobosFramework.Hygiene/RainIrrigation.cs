using DubsBadHygiene;
using HarmonyLib;
using Phobos.Framework.Environment;
using Phobos.Framework.Patching;
using Verse;

namespace Phobos.Framework.Hygiene;

/// <summary>
/// Rain soaks open fields through Dubs Bad Hygiene's own irrigation (Framework 0.269.0; the owner's cards of 23 Nov 2026, farming record
/// SL10: "Dub's is preferred when available"). Dubs wets a cell by at most 3 points of 100 at each visit while it rains, a visit about
/// every 40 in-game minutes, so soaking a field took a day or two of steady rain; this raises the cell to the map's ground wetness, which
/// rain fills within the hour. Only ever up: a sprinkler's wetter cell keeps its own, and drying stays Dubs' own. Dubs calls the method
/// only while it rains, on unroofed fertile ground, with its "rain irrigation" option on, so the player's own switch still rules it.
/// Our feature on top of Dubs' design, not a fix of a fault; Harmony, because Dubs has no extension point there. In the game only while
/// D'thorga Framework's setting is on (RainSoakedFields, SwitchedPatch).
/// </summary>
[StaticConstructorOnStartup]
public static class RainIrrigation
{
    static RainIrrigation()
    {
        // In lite mode Dubs keeps no water in the soil at all, so our own fertility rule does the job instead.
        if (DubsBadHygiene.Settings.LiteMode)
        {
            RainSoakedFields.KeeperStandsDown();
            return;
        }
        var harmony = new Harmony(FrameworkMod.PackageId + ".Hygiene.RainIrrigation");
        RainSoakedFields.KeptBy("Dubs Bad Hygiene", new SwitchedPatch(harmony,
            () => AccessTools.Method("DubsBadHygiene.MapComponent_Hygiene:DampenCell"),
            postfix: AccessTools.Method(typeof(RainIrrigation), nameof(Postfix))));
    }

    /// <summary>After Dubs wets a cell in the rain: up to the map's wetness, if that is wetter.</summary>
    static void Postfix(MapComponent_Hygiene __instance, IntVec3 c)
    {
        float floor = RainSoakedFields.IrrigationFloor(__instance.map);
        float now = __instance.IrrigationGrid.ValueAtFloat(c);
        if (floor <= now) return;
        __instance.IrrigationGrid.AddAtCapped(c, floor - now, 100f, regen: true, setdirty: false);
        RainSoakedFields.NoteSoaked(now, floor);
    }
}
