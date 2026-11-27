using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;

namespace Phobos.Framework.Debug;

/// <summary>
/// The game's own figures, for tools outside it (Framework 0.380.0; the owner's "implement all of these", 28 Nov 2026: phobos-modkit's
/// rimworld-perf could not give ticks per second, since sampling counts no calls). A .NET meter named <see cref="MeterName"/>, which
/// Microsoft's dotnet-counters reads through the runtime's own diagnostics pipe beside its garbage collection figures:
///
/// <list type="bullet">
/// <item><c>rimworld.ticks</c>: ticks simulated (a counter; tools show it per second, so ticks per second);</item>
/// <item><c>rimworld.frames</c>: frames drawn while a game is loaded (frames per second, likewise);</item>
/// <item><c>rimworld.frame.worst</c>: the longest frame since the last reading, in milliseconds;</item>
/// <item><c>rimworld.speed</c>: the game speed (0 paused, 1 normal, 2 fast, 3 superfast, 4 ultrafast);</item>
/// <item><c>rimworld.pawns</c> and <c>rimworld.maps</c>: spawned pawns on every map, and maps, counted every 60 frames.</item>
/// </list>
///
/// Cost: one timestamp and a few field writes a frame, from the framework's per-frame update; nothing else runs unless a tool listens,
/// and then the runtime reads the fields on its own thread once a refresh. Nothing is saved, and no new class enters a save. The meter
/// name is ours and new (the brand rule: D'thorga); the kit's RimWorld adapter names it.
/// </summary>
public static class GameMeter
{
    public const string MeterName = "DThorga.RimWorld";
    const int CountEvery = 60;

    static Meter? meter;
    static bool failed;
    static long ticks, frames, worstFrame, lastFrame;
    static int lastTick = -1, speed, pawns, maps, sinceCount;

    /// <summary>Opens the meter, once; a runtime without one leaves a single warning and nothing else.</summary>
    static void Start()
    {
        try
        {
            meter = new Meter(MeterName);
            meter.CreateObservableCounter("rimworld.ticks", () => Volatile.Read(ref ticks), "{tick}", "Ticks simulated");
            meter.CreateObservableCounter("rimworld.frames", () => Volatile.Read(ref frames), "{frame}", "Frames drawn in a game");
            meter.CreateObservableGauge("rimworld.frame.worst", ReadWorstFrame, "ms", "The longest frame since the last reading");
            meter.CreateObservableGauge("rimworld.speed", () => Volatile.Read(ref speed), "{speed}", "The game speed, 0 when paused");
            meter.CreateObservableGauge("rimworld.pawns", () => Volatile.Read(ref pawns), "{pawn}", "Spawned pawns on every map");
            meter.CreateObservableGauge("rimworld.maps", () => Volatile.Read(ref maps), "{map}", "Maps");
        }
        catch (Exception error)
        {
            failed = true;
            meter = null;
            FrameworkMod.Logger.Warn("The game's figures for outside tools could not be published: " + error.Message);
        }
    }

    /// <summary>A new game, or a load: the first frame after it does not count as a long one.</summary>
    public static void NewGame()
    {
        lastFrame = 0;
        lastTick = -1;
        sinceCount = CountEvery;
    }

    /// <summary>Once a frame while a game is loaded, from the framework's per-frame update.</summary>
    public static void OnFrame()
    {
        if (meter == null)
        {
            if (failed) return;
            Start();
            if (meter == null) return;
        }
        long now = Stopwatch.GetTimestamp();
        if (lastFrame != 0) KeepWorst(now - lastFrame);
        lastFrame = now;
        Volatile.Write(ref frames, frames + 1);
        TickManager? manager = Find.TickManager;
        if (manager != null)
        {
            int tick = manager.TicksGame;
            if (lastTick >= 0) Volatile.Write(ref ticks, ticks + GameMeterRule.TicksAdvanced(lastTick, tick));
            lastTick = tick;
            Volatile.Write(ref speed, manager.Paused ? 0 : (int)manager.CurTimeSpeed);
        }
        if (++sinceCount < CountEvery) return;
        sinceCount = 0;
        List<Map> all = Find.Maps;
        int spawned = 0;
        for (int i = 0; i < all.Count; i++) spawned += all[i].mapPawns.AllPawnsSpawnedCount;
        Volatile.Write(ref pawns, spawned);
        Volatile.Write(ref maps, all.Count);
    }

    static void KeepWorst(long took)
    {
        long seen = Interlocked.Read(ref worstFrame);
        while (took > seen)
        {
            long found = Interlocked.CompareExchange(ref worstFrame, took, seen);
            if (found == seen) return;
            seen = found;
        }
    }

    static double ReadWorstFrame() => GameMeterRule.Milliseconds(Interlocked.Exchange(ref worstFrame, 0), Stopwatch.Frequency);
}
