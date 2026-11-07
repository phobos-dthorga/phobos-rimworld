using System.Collections.Generic;
using HarmonyLib;
using IRBM;
using Phobos.Framework.Work;
using Verse;

namespace Phobos.MissilePatches.Patches;

/// <summary>
/// Replaces the missile mod's one-frame pass over a struck map with a queued sweep (<see cref="IrbmSweep"/>), run a slice per frame by
/// the Framework's queue. A prefix that skips the original, because the original is one loop with no extension point: nothing in it
/// can be paced from outside (recorded reason, AGENTS.md). With the "spread the blast" setting off, the original runs untouched.
/// </summary>
[HarmonyPatch(typeof(StrategicImpactCountdown), nameof(StrategicImpactCountdown.ApplyStrategicDamage))]
public static class Patch_ApplyStrategicDamage_Spread
{
    public static bool Prefix(Map map, DamageDef damageDef, float amount, float armorPen, Thing launcher, string sourceLabel,
        List<ThingCategory> scanCategories, bool penetrateThickRoof, bool lockCorePart, bool completeDismantle)
    {
        if (!MissilePatchesMod.Settings.spreadBlast) return true;
        if (map == null || map.Index < 0) return false;
        FrameWorkQueue.Enqueue(new IrbmSweep(map, damageDef, amount, armorPen, launcher, sourceLabel, scanCategories, penetrateThickRoof,
            lockCorePart, completeDismantle));
        return false;
    }
}
