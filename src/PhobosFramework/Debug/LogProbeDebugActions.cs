using System;
using System.Diagnostics;
using LudeonTK;
using Phobos.Framework.Logging;
using Verse;

namespace Phobos.Framework.Debug;

/// <summary>
/// The in-game probes of docs/development/logging-framework-design.md ("In-game probes"), for the owner to run: each writes what it
/// did to the log. Every probe's own messages count toward the game's 10,000-message limit; the flood is meant to reach it.
/// </summary>
public static class LogProbeDebugActions
{
    const string Category = GameLogKeeper.Category;

    /// <summary>Logs the state of the game's log: how often the game log keeper has turned it back on after the game's message limit,
    /// the loudest messages since, and the errors noted with D'thorga code on their path. Reads only.</summary>
    [DebugAction(DebugCategory.Name, "Framework: game log status", allowedGameStates = AllowedGameStates.Entry | AllowedGameStates.Playing)]
    public static void Status() => HostLog.Write(Category, HostLevel.Info, ErrorNotes.Status() + "\nGame log keeper: " + GameLogKeeper.Status());

    /// <summary>Log probe T2 (docs/development/logging-framework-design.md): writes one warning under D'thorga Framework's own log
    /// category, to look for in the log window and in Player.log.</summary>
    [DebugAction(DebugCategory.Name, "Framework: log probe T2, one line through our category", allowedGameStates = AllowedGameStates.Entry | AllowedGameStates.Playing)]
    public static void OneLine() => HostLog.Write(Category, HostLevel.Warning, "Log probe T2: this line was written under our own category. "
        + "Look for it in the in-game log window and in Player.log with [D'thorga Framework] as its category.");

    /// <summary>Log probe T1 and T4: writes 10,001 messages to pass the game's 10,000-message limit, then one line each through our
    /// category, Unity's log and the game's, to show which still reach Player.log. Needs a game in play; the flood stays in the log for
    /// the session.</summary>
    [DebugAction(DebugCategory.Name, "Framework: log probe T1/T4, flood past the game's limit", allowedGameStates = AllowedGameStates.Playing)]
    public static void Flood()
    {
        const int count = 10001;
        for (int i = 1; i <= count; i++) Log.Message("[D'thorga Framework] Log probe T1 flood, message " + i + " of " + count + ".");
        HostLog.Write(Category, HostLevel.Warning, "Log probe T1: after the flood, through our category. Player.log should hold this line.");
        UnityEngine.Debug.Log("[D'thorga Framework] Log probe T1: after the flood, through Unity's Debug.Log. Player.log should not hold this line "
            + "unless the game's log was already turned back on.");
        Log.Message("[D'thorga Framework] Log probe T1: after the flood, through the game's own log. Same as the line above.");
    }

    /// <summary>Log probe T3: counts every message on the log stream for one minute, then logs the counts, to compare with Player.log
    /// for the same minute.</summary>
    [DebugAction(DebugCategory.Name, "Framework: log probe T3, count the log stream for a minute", allowedGameStates = AllowedGameStates.Entry | AllowedGameStates.Playing)]
    public static void CountStream()
    {
        GameLogKeeper.StartProbe(TimeSpan.FromMinutes(1));
        HostLog.Write(Category, HostLevel.Info, "Log probe T3: counting every message on the log stream for one minute; the counts follow then. "
            + "Compare them with Player.log's lines for the same minute.");
    }

    /// <summary>Log probe T5: writes 1,000 messages each through the game's Log.Message, our category and Log.Error with an exception,
    /// and logs the time and memory each took; all 3,000 count toward the game's message limit.</summary>
    [DebugAction(DebugCategory.Name, "Framework: log probe T5, time 1,000 messages each way", allowedGameStates = AllowedGameStates.Playing)]
    public static void Benchmark()
    {
        const int count = 1000;
        string report = "Log probe T5, " + count + " messages each (they count toward the game's limit):"
            + Time("the game's Log.Message", count, i => Log.Message("[D'thorga Framework] T5 message " + i))
            + Time("our category", count, i => HostLog.Write(Category, HostLevel.Info, "T5 category message " + i))
            + Time("the game's Log.Error with an exception", count, i => Log.Error("[D'thorga Framework] T5 error " + i + ": "
                + new InvalidOperationException("probe").ToString()));
        HostLog.Write(Category, HostLevel.Info, report);
    }

    static string Time(string what, int count, Action<int> write)
    {
        long bytes = GC.GetAllocatedBytesForCurrentThread();
        var watch = Stopwatch.StartNew();
        for (int i = 0; i < count; i++) write(i);
        watch.Stop();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - bytes;
        return "\n  " + what + ": " + (watch.Elapsed.TotalMilliseconds * 1000.0 / count).ToString("0.0") + " µs and "
            + (allocated / count) + " bytes a message";
    }
}
