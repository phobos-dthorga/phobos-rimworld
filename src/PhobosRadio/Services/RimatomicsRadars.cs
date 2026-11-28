using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Phobos.Framework;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>
/// Dubs Rimatomics' radar dish on our radar console (Radio 0.74.0; the owner's card of 28 Nov 2026 for the radar's tie-ins): listed beside
/// our radars, working or not, with whether its ATOM upgrade is fitted. Read by name (Rimatomics.Building_Radar's <c>Working</c> and
/// <c>HasATOM</c>, read 28 Nov 2026), only while Rimatomics is active; its raid hold already works with ours under one hold per raid
/// (D'thorga Framework's RaidHolds). Cost: one pass over the colony's buildings while a console's scope is open.
/// </summary>
[StaticConstructorOnStartup]
public static class RimatomicsRadars
{
    private const string RadarTypeName = "Rimatomics.Building_Radar";

    static RimatomicsRadars()
    {
        if (!OptionalMods.IsActive(OptionalMods.Rimatomics) || AccessTools.TypeByName(RadarTypeName) is not { } type) return;
        PropertyInfo? working = AccessTools.Property(type, "Working");
        FieldInfo? atom = AccessTools.Field(type, "HasATOM");
        if (working == null || atom == null)
        {
            Log.Warning("[PhobosRadio] Dubs Rimatomics' radar dish has changed; it is not listed on the radar console.");
            return;
        }
        RadarTargets.Register(new Source(type, working, atom));
    }

    private sealed class Source : IOtherRadarSource
    {
        private readonly Type type;
        private readonly PropertyInfo working;
        private readonly FieldInfo atom;

        public Source(Type type, PropertyInfo working, FieldInfo atom)
        {
            this.type = type;
            this.working = working;
            this.atom = atom;
        }

        public void Collect(Map map, List<OtherRadar> into)
        {
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                if (!type.IsInstanceOfType(building)) continue;
                bool on = working.GetValue(building) is true;
                string note = (atom.GetValue(building) is true ? "PhobosRadar_RimatomicsAtom" : "PhobosRadar_RimatomicsNoAtom").Translate().Resolve();
                into.Add(new OtherRadar(building, on, note));
            }
        }
    }
}
