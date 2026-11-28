using System.Collections.Generic;
using HarmonyLib;
using IRBM;
using Phobos.Radio.Services;
using Phobos.Shared;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Phobos.Radio.Irbm;

/// <summary>
/// InterRim Ballistic Missile with our radars (Radio 0.74.0; the owner's card of 28 Nov 2026, "IRBM missiles"). An enemy faction's missile
/// crosses the world map as "???" until IRBM's own anti-missile radars detect it (<c>RadarUtility.IsMissileDetected</c>, read 27 Oct 2026
/// from IRBM's build of 18 Oct 2026). Our radars see it too, by the sum: a small target (0.1 m², authored) but flying very high (50 km,
/// authored), so seen far. It shows on our scope and world map, its first sighting warns by letter, and IRBM counts it detected while ours
/// see it: at its first level while still unknown, at its second (its name and speed shown) once identified.
/// </summary>
[StaticConstructorOnStartup]
public static class IrbmRadar
{
    public const string HarmonyId = "PhobosDthorga.RadioFramework";
    private const string LogPrefix = "[D'thorga Radio Framework: InterRim Ballistic Missile]";
    /// <summary>A missile's radar size, in m² [authored]: a re-entry body is small, smaller than a fighter.</summary>
    public const double MissileSquareMetres = 0.1;
    /// <summary>The height a missile is taken to fly at, in metres [authored]: a ballistic missile climbs far above the air.</summary>
    public const double MissileMetres = 50000.0;

    static IrbmRadar()
    {
        HarmonyPatchApplier.ApplyEach(new Harmony(HarmonyId), typeof(IrbmRadar).Assembly, LogPrefix);
        RadarTargets.Register(new MissileTargets());
    }

    /// <summary>Whether the contact for this missile is seen by any of the player's colonies, and whether it is identified there.</summary>
    public static bool SeenByUs(WorldObject_Missile missile, out bool identified)
    {
        identified = false;
        bool seen = false;
        foreach (Map map in Find.Maps)
        {
            if (!map.IsPlayerHome || map.GetComponent<MapComponent_PhobosRadar>() is not { } picture || picture.Watching().Count == 0) continue;
            foreach (RadarContact contact in picture.Contacts())
            {
                if (contact.Key != missile) continue;
                seen = true;
                if (contact.Identity != RadarIdentity.Unknown) identified = true;
            }
        }
        return seen;
    }

    private sealed class MissileTargets : IRadarTargetSource
    {
        public void Collect(List<RadarTarget> into)
        {
            foreach (WorldObject place in Find.WorldObjects.AllWorldObjects)
            {
                if (place is not WorldObject_Missile { Destroyed: false } missile || missile.Faction == Faction.OfPlayer) continue;
                PlanetTile tile = RadarTargets.TileUnder(missile, ((WorldObject)missile).DrawPos, missile.Tile);
                if (!tile.Valid) continue;
                bool inbound = missile.MissileComp?.IsIncoming == true;
                into.Add(new RadarTarget(missile, tile, MissileMetres, MissileSquareMetres, missile.Faction, missile.def.LabelCap, RadarContactKind.Missile,
                    false, inbound));
            }
        }
    }
}

/// <summary>
/// IRBM counts an enemy missile detected while our radars see it (Radio 0.74.0): a postfix on <c>RadarUtility.IsMissileDetected</c> that
/// only ever adds a detection, never takes one away. Level 1 while it is unknown to us, 2 once identified (IRBM shows its name and speed at 2).
/// </summary>
[HarmonyPatch(typeof(RadarUtility), nameof(RadarUtility.IsMissileDetected))]
public static class IrbmDetectionPatch
{
    public static void Postfix(WorldObject_Missile missile, ref int level, ref bool __result)
    {
        if (missile == null || level >= 2 || !IrbmRadar.SeenByUs(missile, out bool identified)) return;
        __result = true;
        level = System.Math.Max(level, identified ? 2 : 1);
    }
}
