using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using IRBM;
using Phobos.Framework.Work;
using Verse;

namespace Phobos.MissilePatches.Patches;

/// <summary>
/// Replaces the missile mod's one-frame pass over a struck map with a queued sweep (<see cref="IrbmSweep"/>), run a slice per frame by
/// the Framework's queue. A prefix that skips the original, because the original is one loop with no extension point: nothing in it
/// can be paced from outside (recorded reason, AGENTS.md). With the "spread the blast" setting off, the original runs, with people left out of
/// the passes of a warhead whose radiation we gave as a dose (Patches 0.47.0).
/// </summary>
[HarmonyPatch(typeof(StrategicImpactCountdown), nameof(StrategicImpactCountdown.ApplyStrategicDamage))]
public static class Patch_ApplyStrategicDamage_Spread
{
    public static bool Prefix(Map map, DamageDef damageDef, float amount, float armorPen, Thing launcher, string sourceLabel,
        ref List<ThingCategory> scanCategories, bool penetrateThickRoof, bool lockCorePart, bool completeDismantle)
    {
        // People struck by a warhead whose radiation we gave as a dose (the Gamma-Ray Burst; Patches 0.47.0) are left out of its passes, by
        // either route: the list is IRBM's Def's own, so a copy without them.
        if (map != null && scanCategories != null && scanCategories.Contains(ThingCategory.Pawn)
            && StrategicRadiationBursts.PeopleSpared(map, damageDef))
        {
            scanCategories = scanCategories.Where(category => category != ThingCategory.Pawn).ToList();
        }
        if (!MissilePatchesMod.Settings.spreadBlast) return true;
        if (map == null || map.Index < 0) return false;
        FrameWorkQueue.Enqueue(new IrbmSweep(map, damageDef, amount, armorPen, launcher, sourceLabel, scanCategories, penetrateThickRoof,
            lockCorePart, completeDismantle));
        return false;
    }
}
