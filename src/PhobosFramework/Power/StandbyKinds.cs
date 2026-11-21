using System;
using System.Collections.Generic;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;

namespace Phobos.Framework.Power;

/// <summary>
/// Which buildings standby power looks after (Framework 0.222.0; docs/development/standby-power.md), found once at start-up by what each
/// building is and does, never by its name (the owner's rule: match effects, not names), so other mods' buildings of the same kind join in.
/// Only buildings powered by the game's own power comp, exactly, that draw power: a comp of another class, a generator or a battery
/// manages its own draw. A light is a plain building that glows, needs no ticking and is not a sun lamp: anything that ticks is a machine
/// of some kind, and is left alone unless it is one of the kinds below.
/// </summary>
[StaticConstructorOnStartup]
public static class StandbyKinds
{
    /// <summary>LightsOut and Turn It On and Off - RePowered: they do the same job, so standby power stands down while either is active.</summary>
    public static readonly string[] SameJobPackageIds = { "juanlopez2008.lightsout", "mlie.turnonoffrepowered" };

    static readonly Dictionary<StandbyKind, int> counts = new();

    static StandbyKinds()
    {
        foreach (string id in SameJobPackageIds)
        {
            if (ModsConfig.IsActive(id)) OtherModActive ??= ModLister.GetActiveModWithIdentifier(id, ignorePostfix: true)?.Name ?? id;
        }
        List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading;
        for (int i = 0; i < defs.Count; i++)
        {
            ThingDef def = defs[i];
            if (Classify(def) is not StandbyKind kind) continue;
            def.comps ??= new List<CompProperties>();
            var props = new CompProperties_PhobosStandby { kind = kind };
            def.comps.Add(props);
            props.ResolveReferences(def);
            counts[kind] = counts.TryGetValue(kind, out int n) ? n + 1 : 1;
        }
    }

    /// <summary>The name of a mod that does the same job and is active, or null.</summary>
    public static string? OtherModActive { get; private set; }

    /// <summary>How many kinds of building of each kind standby power looks after, for the load line.</summary>
    public static IReadOnlyDictionary<StandbyKind, int> Counts => counts;

    /// <summary>The kind a building is, or null when standby power leaves it alone.</summary>
    static StandbyKind? Classify(ThingDef def)
    {
        Type? type = def.thingClass;
        if (def.category != ThingCategory.Building || type == null || !typeof(Building).IsAssignableFrom(type)) return null;
        if (def.HasComp(typeof(CompPhobosStandby))) return null;
        PhobosStandbyExtension? asked = def.GetModExtension<PhobosStandbyExtension>();
        if (asked != null && asked.never) return null;
        CompProperties_Power? power = def.GetCompProperties<CompProperties_Power>();
        // At start-up PowerConsumption is the Def's own figure, before any research upgrade.
        if (power == null || power.compClass != typeof(CompPowerTrader) || power.PowerConsumption <= 0f) return null;
        if (asked?.kind is StandbyKind chosen) return chosen;
        if (typeof(Building_Door).IsAssignableFrom(type)) return StandbyKind.Door;
        if (typeof(Building_Turret).IsAssignableFrom(type)) return StandbyKind.Turret;
        if (typeof(Building_WorkTable).IsAssignableFrom(type) || typeof(Building_ResearchBench).IsAssignableFrom(type)) return StandbyKind.Machine;
        if (typeof(Building_PlantGrower).IsAssignableFrom(type)) return StandbyKind.Grower;
        if (def.HasComp(typeof(CompFacility))) return StandbyKind.Facility;
        if (def.building?.joyKind != null) return StandbyKind.Watch;
        CompProperties_Glower? glow = def.GetCompProperties<CompProperties_Glower>();
        if (glow != null && glow.overlightRadius <= 0f && def.tickerType == TickerType.Never && type == typeof(Building)
            && !def.HasComp(typeof(CompSchedule)))
        {
            return StandbyKind.Light;
        }
        return null;
    }

    /// <summary>Whether standby power works on this kind now: its setting, the master setting, and no other mod doing the same job.</summary>
    public static bool On(StandbyKind kind)
    {
        FrameworkSettings s = FrameworkMod.Settings;
        if (!s.standbyPower || OtherModActive != null) return false;
        return kind switch
        {
            StandbyKind.Light => s.standbyLights,
            StandbyKind.Machine or StandbyKind.Watch => s.standbyMachines,
            StandbyKind.Door => s.standbyDoors,
            _ => s.standbyOthers,
        };
    }

    /// <summary>The load line's words for what standby power found.</summary>
    public static string Describe()
    {
        var parts = new List<string>();
        foreach (StandbyKind kind in (StandbyKind[])Enum.GetValues(typeof(StandbyKind)))
        {
            if (counts.TryGetValue(kind, out int n)) parts.Add(n + " " + kind.ToString().ToLowerInvariant());
        }
        return "standby power on " + (parts.Count == 0 ? "no buildings" : string.Join(", ", parts)) + " (kinds of building)";
    }
}
