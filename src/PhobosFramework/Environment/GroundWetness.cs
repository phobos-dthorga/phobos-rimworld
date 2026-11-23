using System;
using Phobos.Framework.Defs;
using Phobos.Framework.Rules;
using Verse;

namespace Phobos.Framework.Environment;

/// <summary>The starting figures for how fast rain wets open ground and how fast it dries (GroundWetnessRule; Framework 0.204.0, LM2 of
/// docs/development/four-mods-study-2026-11-21.md). One Def; the first loaded wins. Every figure is authored, a gameplay choice.</summary>
public class PhobosGroundWetnessRulesDef : PhobosRulesDef
{
    /// <summary>Hours of ordinary rain (rate 1) that soak dry ground.</summary>
    public float soakHours = 1f;
    /// <summary>Hours soaked ground takes to dry at 20 °C, in full daylight and the game's ordinary wind.</summary>
    public float dryHours = 10f;
    /// <summary>The drying pace at or below freezing, against the reference.</summary>
    public float frozenFactor = 0.1f;
    /// <summary>The drying pace in full darkness, against full daylight.</summary>
    public float nightFactor = 0.4f;
    /// <summary>Game ticks between updates of a map's figure (2,500 is an in-game hour).</summary>
    public int passTicks = 250;
    /// <summary>Rain soaking open fields (Framework 0.269.0), where no other mod keeps the soil's water: how many times as fertile soaked
    /// open ground is, falling in a straight line to 1 as it dries. Dubs Bad Hygiene's own default irrigation strength.</summary>
    public float soakedFertilityFactor = 1.8f;

    public static PhobosGroundWetnessRulesDef Current => Current<PhobosGroundWetnessRulesDef>();
}

/// <summary>
/// How wet a map's open ground is, 0 to 1: one saved figure for the whole map, wetted by the game's rain and dried by its outdoor
/// temperature, daylight and wind (GroundWetnessRule). A roof is applied when the figure is read, so the inside of a building is
/// always dry. Read it through <see cref="At"/>; vehicles' grip and an aircraft's landing ground do. Safe to add mid-game: a map
/// with no saved figure starts dry and catches up with the next rain.
/// </summary>
public sealed class MapComponent_PhobosGroundWetness : MapComponent
{
    const float TicksPerHour = 2500f;

    float wetness;
    bool faulted;

    public MapComponent_PhobosGroundWetness(Map map) : base(map) { }

    /// <summary>The map's open ground, 0 (dry) to 1 (soaked).</summary>
    public float Open => wetness;

    /// <summary>How wet the ground is at <paramref name="cell"/>: the map's figure in the open, 0 under a roof or with no map.</summary>
    public static float At(Map? map, IntVec3 cell)
    {
        if (map == null || !cell.InBounds(map) || cell.Roofed(map)) return 0f;
        return For(map)?.wetness ?? 0f;
    }

    /// <summary>The map's figure for open ground, or 0 with no map.</summary>
    public static float OpenOn(Map? map) => For(map)?.wetness ?? 0f;

    public static MapComponent_PhobosGroundWetness? For(Map? map) => map?.GetComponent<MapComponent_PhobosGroundWetness>();

    /// <summary>The last map asked for and its component, together in one object so a reader never sees half of a pair.</summary>
    sealed class Last
    {
        public readonly Map map;
        public readonly MapComponent_PhobosGroundWetness component;
        public Last(Map map, MapComponent_PhobosGroundWetness component) { this.map = map; this.component = component; }
    }

    static Last? last;

    /// <summary>The map's figure for open ground, for methods called very often (every cell's fertility, Framework 0.269.0): the
    /// component is looked up once and kept while the same map is asked about, as a modded game's maps hold hundreds of components.</summary>
    public static float OpenOnQuick(Map? map)
    {
        if (map == null) return 0f;
        Last? known = last;
        if (known == null || known.map != map)
        {
            MapComponent_PhobosGroundWetness? found = For(map);
            if (found == null) return 0f;
            last = known = new Last(map, found);
        }
        return known.component.wetness;
    }

    public override void MapRemoved()
    {
        // Lets a closed map go, rather than holding it until another map is asked about.
        if (last?.map == map) last = null;
    }

    /// <summary>Sets the figure outright (dev mode's "soak" and "dry" actions).</summary>
    public void Set(float value) => wetness = Math.Max(0f, Math.Min(1f, value));

    public override void MapComponentTick()
    {
        if (faulted) return;
        PhobosGroundWetnessRulesDef rules = PhobosGroundWetnessRulesDef.Current;
        int pass = Math.Max(1, rules.passTicks);
        // Spread across maps, so several maps never update in the same tick.
        if ((Find.TickManager.TicksGame + map.uniqueID) % pass != 0) return;
        try
        {
            wetness = (float)GroundWetnessRule.Step(wetness, pass / TicksPerHour, map.weatherManager.RainRate, map.mapTemperature.OutdoorTemp,
                map.skyManager.CurSkyGlow, map.windManager.WindSpeed, rules.soakHours, rules.dryHours, rules.frozenFactor, rules.nightFactor);
        }
        catch (Exception error)
        {
            faulted = true;
            FrameworkMod.Logger.Error("could not work out how wet the ground is on " + map + ", so it stays as it was until the game restarts: " + error);
        }
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref wetness, "PhobosGroundWetness", 0f);
    }
}
