using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>Whether a convoy can talk to home now, and why: what the radio's own link budget found (EM33). <paramref name="HomeTile"/> is the
/// home it reaches (Framework 0.330.0, for advice by radio), or -1.</summary>
public readonly record struct ConvoyReach(bool InTouch, double DistanceKm, double HorizonKm, double MarginDb, string Line, int HomeTile = -1);

/// <summary>Something that can answer whether a convoy is in radio reach of home: the Radio Framework's tie-in, when it is installed.</summary>
public interface IConvoyReachSource
{
    ConvoyReach? Reach(Caravan convoy, float mastMetres);

    /// <summary>Buttons the radio offers a convoy on the world map (Framework 0.329.0: putting up a relay mast).</summary>
    IEnumerable<Gizmo> CaravanGizmos(Caravan convoy) => System.Linq.Enumerable.Empty<Gizmo>();

    /// <summary>A line for the radio's logbook (Framework 0.348.0: the medevac's calls); nothing without the Radio Framework.</summary>
    void Log(string text) { }
}

/// <summary>
/// A convoy's radio reach home (Framework 0.325.0, EM33; docs/development/vehicle-road-work.md; the owner's card of 26 Nov 2026: "Both",
/// a supply drop guided in and an early ambush warning). D'thorga Framework only asks: the Radio Framework's vehicle tie-in answers, through
/// its one link budget, so without it a convoy is never in reach and nothing here shows. A Datum stopped with its mast up lifts the convoy's
/// antenna to <see cref="MastMetres"/>. Asked at most once a tenth of a game hour for each convoy; nothing runs while nobody asks.
/// </summary>
public static class ConvoyRadio
{
    /// <summary>The Datum's boom raised as a mast, in metres (agent's figure: a light truck's telescopic mast is commonly 9 to 15 m).</summary>
    public const float MastMetres = 10f;

    static IConvoyReachSource? source;
    static readonly Dictionary<Caravan, (int tick, ConvoyReach? reach)> cache = new();

    public static void Register(IConvoyReachSource reachSource) => source = reachSource;

    /// <summary>Whether anything can answer: the radio tie-in is loaded.</summary>
    public static bool Available => source != null;

    /// <summary>The convoy's mast height now: a Datum's boom raised at a halt, or nothing.</summary>
    public static float Mast(Caravan convoy) =>
        WorldComponent_PhobosRoadWork.Current?.JobOf<RoadMast>(convoy) is { heldUp: null } && !RoadWork.Moving(convoy) ? MastMetres : 0f;

    /// <summary>The convoy's reach home, or null with no radio tie-in or no radio to ask with; kept for a tenth of a game hour.</summary>
    public static ConvoyReach? Of(Caravan convoy)
    {
        if (source == null || convoy.Faction != Faction.OfPlayer) return null;
        int now = Find.TickManager.TicksGame;
        if (cache.TryGetValue(convoy, out var held) && now - held.tick < RoadWork.Interval) return held.reach;
        if (cache.Count > 64) cache.Clear();
        ConvoyReach? reach = source.Reach(convoy, Mast(convoy));
        cache[convoy] = (now, reach);
        return reach;
    }

    /// <summary>Asks again at once: a mast went up or down.</summary>
    public static void Forget(Caravan convoy) => cache.Remove(convoy);

    public static bool InTouch(Caravan convoy) => Of(convoy) is { InTouch: true };

    /// <summary>Writes a line in the radio's logbook, when the Radio Framework keeps one (Framework 0.348.0).</summary>
    public static void Log(string text) => source?.Log(text);

    /// <summary>The radio's buttons for a convoy, or none without the Radio Framework.</summary>
    public static IEnumerable<Gizmo> Gizmos(Caravan convoy) =>
        source == null || convoy.Faction != Faction.OfPlayer ? System.Linq.Enumerable.Empty<Gizmo>() : source.CaravanGizmos(convoy);
}

/// <summary>The Datum's boom raised as a radio mast at a halt (Framework 0.325.0, EM33): no work to finish; it stands until the convoy drives
/// on or the player lowers it.</summary>
public sealed class RoadMast : RoadHaltJob
{
    public override string? Refusal(bool starting) => RoadWork.MastRefusal(caravan!, vehicle!);

    public override void Work(float hours) { }

    public override string LeftText() => "PhobosRadioReach_MastLeft".Translate(caravan!.LabelCap.Named("CARAVAN")).Resolve();

    public override string InspectLine() => "PhobosRadioReach_InspectMast".Translate(vehicle!.LabelShort.Named("VEHICLE"),
        ConvoyRadio.MastMetres.ToString("0").Named("METRES")).Resolve();

    public override void Ended()
    {
        if (caravan != null) ConvoyRadio.Forget(caravan);
    }

    public override void ExposeData()
    {
        Scribe_References.Look(ref caravan, "PhobosMastCaravan");
        Scribe_References.Look(ref vehicle, "PhobosMastTruck");
    }
}
