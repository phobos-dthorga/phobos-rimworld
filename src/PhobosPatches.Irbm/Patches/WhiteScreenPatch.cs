using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using IRBM;
using Phobos.MissilePatches.Rules;
using Phobos.MissilePatches.Strike;
using UnityEngine;
using Verse;

namespace Phobos.MissilePatches.Patches;

/// <summary>
/// For a warhead marked with <see cref="PhobosStrikeExtension"/>, runs D'thorga's strike (<see cref="StrikeLauncher"/>) in place of the
/// missile mod's private StrategicImpactCountdown.OnWhiteScreenReached. A prefix that skips the original (recorded reason, AGENTS.md):
/// the original is one block that damages every map, sets fires on a fifth of every cell, registers conditions and sends the letter,
/// with no extension point between. Every other warhead, and every strike while the "D'thorga's blast model" setting is off, runs the
/// original, whose damage pass the fast sweep replaces. The countdown's private state is read and reset by name, as the original would.
/// </summary>
[HarmonyPatch(typeof(StrategicImpactCountdown), "OnWhiteScreenReached")]
public static class Patch_OnWhiteScreenReached_Strike
{
    static readonly FieldInfo? TimeLeft = AccessTools.Field(typeof(StrategicImpactCountdown), "timeLeft");
    static readonly FieldInfo? TargetMaps = AccessTools.Field(typeof(StrategicImpactCountdown), "targetMaps");
    static readonly FieldInfo? Missile = AccessTools.Field(typeof(StrategicImpactCountdown), "missile");
    static readonly FieldInfo? Sound = AccessTools.Field(typeof(StrategicImpactCountdown), "customExplosionSound");

    public static bool Prefix()
    {
        MissilePatchesSettings settings = MissilePatchesMod.Settings;
        if (!settings.spreadBlast || !settings.blastModel || TimeLeft == null || TargetMaps == null || Missile == null) return true;
        var missile = Missile.GetValue(null) as WorldObject_Missile;
        MissileWarheadDef? warhead = missile?.MissileComp?.warheadDef;
        WorldObjectCompProperties_StrategicPayload? props = warhead?.payloadComps?.OfType<WorldObjectCompProperties_StrategicPayload>().FirstOrDefault();
        PhobosStrikeExtension? zones = warhead?.GetModExtension<PhobosStrikeExtension>();
        if (props == null || zones == null) return true;
        var maps = TargetMaps.GetValue(null) as HashSet<Map>;
        try
        {
            StrikeLauncher.Launch(missile, props, zones, maps?.ToList() ?? new List<Map>(), Sound?.GetValue(null) as SoundDef);
        }
        catch (Exception error)
        {
            MissilePatchesMod.Logger.Error("The strike could not start; the screen is cleared. " + error);
            ScreenFader.StartFade(Color.clear, IrbmBudgetRule.WhiteScreenSeconds);
        }
        finally
        {
            TimeLeft.SetValue(null, -1000f);
            maps?.Clear();
            Missile.SetValue(null, null);
            Sound?.SetValue(null, null);
        }
        return false;
    }
}
