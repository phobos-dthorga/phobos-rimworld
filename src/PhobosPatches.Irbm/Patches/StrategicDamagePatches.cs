using System.Collections.Generic;
using HarmonyLib;
using IRBM;
using Verse;

namespace Phobos.MissilePatches.Patches;

/// <summary>
/// Replaces the mod's one-frame pass over the struck map with a queued sweep. A prefix that skips the original, because the
/// original is one loop with no extension point: nothing in it can be paced from outside (recorded reason, AGENTS.md). With
/// the "spread the blast" setting off, the original runs untouched.
/// </summary>
[HarmonyPatch(typeof(StrategicImpactCountdown), nameof(StrategicImpactCountdown.ApplyStrategicDamage))]
public static class Patch_ApplyStrategicDamage_Spread
{
    public static bool Prefix(Map map, DamageDef damageDef, float amount, float armorPen, Thing launcher, string sourceLabel,
        List<ThingCategory> scanCategories, bool penetrateThickRoof, bool lockCorePart, bool completeDismantle)
    {
        if (!MissilePatchesMod.Settings.spreadBlast) return true;
        if (map == null || map.Index < 0) return false;
        StrategicSweeps.Enqueue(new StrategicSweep(map, damageDef, amount, armorPen, launcher, sourceLabel, scanCategories ?? new List<ThingCategory>(),
            penetrateThickRoof, lockCorePart, completeDismantle));
        return false;
    }
}

/// <summary>Runs the queued sweeps a slice per frame, paused or not, from the same frame hook the mod itself uses.</summary>
[HarmonyPatch(typeof(Root_Play), nameof(Root_Play.Update))]
public static class Patch_RootPlay_Update_Sweeps
{
    public static void Postfix() => StrategicSweeps.Update();
}
