using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Environment;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;

namespace Phobos.Framework.Health;

/// <summary>
/// Fallout lying on a map, dosing the people there by its age and their cover (Framework 0.364.0; docs/development/radiation-bursts.md; the
/// owner's choice of 28 Nov 2026, "our own fallout, squeezed", after Dubs Rimatomics' fallout gave about 3 Gy a day in the open and nothing
/// under any roof). A blast lays a field with <see cref="Lay"/>: its rate in the open at the first hour, falling by the 7:10 rule
/// (<see cref="FalloutRule"/>). Every <see cref="IntervalTicks"/> ticks each flesh person on the map takes the dose of that interval, cut by
/// their cover, or aboard a vehicle by its hull's share, through <see cref="RadiationDoses"/> as "fallout", so only while D'thorga Medical's
/// model is on; people who arrive later take it too. Rimatomics' own fallout dose stands down on a map with a field (D'thorga Medical), and
/// its sky, plants and clouds stay. The fields are saved with the map.
/// </summary>
public class MapComponent_PhobosFallout : MapComponent
{
    /// <summary>How often the field doses, in ticks: about a quarter of a game hour.</summary>
    public const int IntervalTicks = 600;
    const double TicksPerHour = 2500;

    public sealed class Field : IExposable
    {
        public int startTick;
        public int endTick;
        public float graysPerHour;
        public string source = "";

        public void ExposeData()
        {
            Scribe_Values.Look(ref startTick, "start");
            Scribe_Values.Look(ref endTick, "end");
            Scribe_Values.Look(ref graysPerHour, "rate");
            Scribe_Values.Look(ref source, "source", "");
        }
    }

    List<Field> fields = new();

    public MapComponent_PhobosFallout(Map map) : base(map) { }

    public bool Active => fields.Count > 0;

    public void Add(float graysPerHourAtFirstHour, float days, string source)
    {
        int now = Find.TickManager.TicksGame;
        fields.Add(new Field { startTick = now, endTick = now + (int)(days * GenDate.TicksPerDay), graysPerHour = graysPerHourAtFirstHour, source = source });
    }

    public override void MapComponentTick()
    {
        if (fields.Count == 0) return;
        int now = Find.TickManager.TicksGame;
        if (now % IntervalTicks != 0) return;
        fields.RemoveAll(f => now >= f.endTick);
        if (fields.Count == 0 || !RadiationDoses.Active) return;
        double outside = 0;
        foreach (Field f in fields)
        {
            outside += FalloutRule.DoseBetween((now - IntervalTicks - f.startTick) / TicksPerHour, (now - f.startTick) / TicksPerHour, f.graysPerHour);
        }
        if (outside < RadiationBurstRule.LeastGrays / 100) return;
        try
        {
            Dose(outside);
        }
        catch (Exception error)
        {
            FrameworkMod.Logger.Error("Fallout on " + (map.Parent?.Label ?? "a map") + " could not dose this time; it will try again. " + error);
        }
    }

    void Dose(double outside)
    {
        foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned.ToList())
        {
            if (pawn.Dead) continue;
            double pass = FalloutRule.Pass(ShelterCovers.Of(pawn));
            if (LargeBlasts.TakerFor(pawn) is { } taker)
            {
                foreach ((Pawn inside, float share) in taker.Aboard(pawn)) Give(inside, outside * pass * share);
                continue;
            }
            Give(pawn, outside * pass);
        }
    }

    static void Give(Pawn pawn, double grays)
    {
        if (pawn.Dead || pawn.RaceProps is not { IsFlesh: true } || grays <= 0) return;
        if (RadiationDoses.Give(pawn, (float)grays, "fallout") && Seen.Wants("health.fallout-dose", pawn))
        {
            Seen.Note("health.fallout-dose", pawn, (grays * 1000).ToString("0.0") + " mGy this quarter hour");
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref fields, "PhobosFallout_Fields", LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) fields ??= new List<Field>();
    }
}

/// <summary>Where blasts lay fallout, and where D'thorga Medical asks whether a map has ours (so Rimatomics' own dose stands down).</summary>
public static class FalloutFields
{
    /// <summary>Lays fallout on a map: its dose rate in the open at the first hour, in grays an hour, for this many days.</summary>
    public static void Lay(Map? map, float graysPerHourAtFirstHour, float days, string source)
    {
        if (map == null || graysPerHourAtFirstHour <= 0f || days <= 0f) return;
        map.GetComponent<MapComponent_PhobosFallout>()?.Add(graysPerHourAtFirstHour, days, source);
        FrameworkMod.Logger.Info("Fallout lies on " + (map.Parent?.Label ?? "a map") + " from " + source + ": " + graysPerHourAtFirstHour.ToString("0.##")
            + " Gy an hour in the open at the first hour, falling by the 7:10 rule, for " + days.ToString("0.#") + " days.");
    }

    /// <summary>Whether a map has fallout of ours lying on it.</summary>
    public static bool Active(Map? map) => map?.GetComponent<MapComponent_PhobosFallout>() is { Active: true };
}
