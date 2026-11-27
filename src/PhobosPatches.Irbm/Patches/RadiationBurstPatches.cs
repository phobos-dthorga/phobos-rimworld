using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using IRBM;
using Phobos.Framework.Health;
using Verse;

namespace Phobos.MissilePatches.Patches;

/// <summary>
/// The radiation of IRBM's tactical warheads (Patches 0.47.0; docs/development/radiation-bursts.md; owner, 27 Nov 2026: "the tactical nukes
/// from the IRBM mod don't have our realistic radiation modelling applied to them"). A warhead carrying the Framework's PhobosRadiationBurst
/// (the Mini-Nuke and the antimatter warhead, by XML) gives its dose at each impact, before the blast: one dose per hit, from the warhead's
/// first payload part only (the Mini-Nuke's pulse is a second part), never from a chain explosion; a cluster's every hit counts. Every route
/// IRBM has to a tactical blast passes here: a missile, a cluster, the quantum strike ability and the chains. A prefix that never skips.
/// </summary>
[HarmonyPatch(typeof(WorldObjectComp_TacticalPayload), nameof(WorldObjectComp_TacticalPayload.ExecuteWithProps))]
public static class Patch_TacticalPayload_RadiationBurst
{
    public static void Prefix(WorldObjectCompProperties_TacticalPayload Props, IntVec3 cell, Map map, int chainDepth, MissileWarheadDef warheadDef)
    {
        if (chainDepth > 0 || map == null || RadiationBursts.Of(warheadDef) is not { } burst) return;
        if (warheadDef.payloadComps?.OfType<WorldObjectCompProperties_TacticalPayload>().FirstOrDefault() != Props) return;
        RadiationBursts.Burst(map, cell, burst, Props.explosionRadius, warheadDef.defName);
    }
}

/// <summary>
/// A strategic warhead whose whole effect on people is a burst of radiation (IRBM's Gamma-Ray Burst; Patches 0.47.0): our one dose on each map
/// it strikes replaces the damage IRBM's passes would do to people there, for as long as its pulses run. Its plants still wither as IRBM made
/// them. Called from the white-screen prefix, before any of IRBM's passes.
/// </summary>
public static class StrategicRadiationBursts
{
    static readonly List<(Map map, DamageDef damage, int until)> exempt = new();

    /// <summary>Gives the burst on each struck map, when the warhead carries a whole-map burst and its setting is on.</summary>
    public static void Strike(MissileWarheadDef? warhead, WorldObjectCompProperties_StrategicPayload? props, IEnumerable<Map>? maps)
    {
        if (warhead == null || maps == null || RadiationBursts.Of(warhead) is not { wholeMap: true } burst || !RadiationBursts.On(burst.kind)) return;
        DamageDef? damage = props?.damageDef;
        int now = Find.TickManager.TicksGame;
        int times = props != null && props.damageTimes > 1 ? props.damageTimes : 1;
        // IRBM repeats a strike every 60 ticks (StrategicImpactManager, read 27 Nov 2026); a little to spare.
        int until = now + times * 60 + 600;
        exempt.RemoveAll(e => e.until < now || e.map == null || e.map.Index < 0);
        foreach (Map map in maps.ToList())
        {
            if (map == null || map.Index < 0) continue;
            if (damage != null) exempt.Add((map, damage, until));
            RadiationBursts.Burst(map, map.Center, burst, 0f, warhead.defName);
        }
    }

    /// <summary>Whether people on this map are spared this damage from IRBM's passes, our dose having taken its place.</summary>
    public static bool PeopleSpared(Map map, DamageDef damage)
    {
        if (exempt.Count == 0) return false;
        int now = Find.TickManager.TicksGame;
        for (int i = 0; i < exempt.Count; i++)
        {
            (Map m, DamageDef d, int until) = exempt[i];
            if (m == map && d == damage && until >= now) return true;
        }
        return false;
    }
}
