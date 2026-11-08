using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Phobos.Framework.Rules;
using Verse;

namespace Phobos.Framework.Logging;

/// <summary>
/// Keeps the game's log going past its 10,000-message limit (owner, 8 Nov 2026: a bounded revive, no Harmony;
/// docs/development/logging-framework-design.md). The game counts every message; at 10,000 it stops every mod's logging, Player.log
/// included, for the rest of the session, and only loading a save or the dev log window's Clear turns it back on. One other mod's
/// error storm (Visit Settlements: 6,846 identical errors in six minutes) was enough to blind the owner's sessions.
///
/// Each frame (paused or not), <see cref="Update"/> reads the switch the game turns off at the limit; when it is off, it calls the
/// game's own public Log.ResetMessageCount(), at most <see cref="GameLogRule.MaxRevivals"/> times a session, and writes one line, under
/// our own category, naming the message that was loudest since the last time. The loudest is counted from the engine's public log
/// stream, which already carries every mod's messages (the game's own log window hooks it at startup, so listening adds only this
/// class's cost: one signature and one dictionary lookup per message). On by default with a setting; it changes nothing until the game
/// has already silenced itself. Nothing is saved.
/// </summary>
public static class GameLogKeeper
{
    public const string Category = "D'thorga Framework";

    static readonly object gate = new();
    static readonly StormTally tally = new();
    static int revivals;
    static bool gaveUp;

    // The probe that counts the stream by category for a while (dev action T3).
    static Dictionary<string, int>? probeCounts;
    static DateTime probeEnds;

    public static int Revivals => revivals;
    public static bool GaveUp => gaveUp;

    /// <summary>Null while the log stream is being counted; else why it is not.</summary>
    public static string? NotListening { get; private set; } = "not started";

    /// <summary>Starts counting the engine's log stream. Called once, from the framework's Mod constructor.</summary>
    public static void Listen()
    {
        try
        {
            Subscribe();
            NotListening = null;
        }
        catch (Exception error) when (error is TypeLoadException or MissingMethodException or MissingFieldException or FileNotFoundException)
        {
            NotListening = "the engine's log stream is not where it was (" + error.GetType().Name + ")";
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Subscribe() => Waylith.Logging.LogStream.MessageReceived += OnMessage;

    /// <summary>Every message any mod logs, on whichever thread logged it.</summary>
    static void OnMessage(LogLevel level, string category, string message, string? stackTrace)
    {
        if (category == Category) return;
        string signature = LogSignature.Of(message);
        lock (gate)
        {
            tally.Add(signature);
            if (probeCounts != null)
            {
                string key = (category ?? "(none)") + " " + level;
                probeCounts[key] = probeCounts.TryGetValue(key, out int n) ? n + 1 : 1;
            }
        }
    }

    /// <summary>Once a frame, from the framework's game component: revives the game's log if it has stopped, and ends a probe.</summary>
    public static void Update()
    {
        if (probeCounts != null && DateTime.UtcNow >= probeEnds) EndProbe();
        if (gaveUp || !FrameworkMod.Settings.keepGameLogAlive) return;
        if (UnityEngine.Debug.unityLogger.logEnabled) return;
        Revive();
    }

    static void Revive()
    {
        string loudest;
        lock (gate)
        {
            loudest = Describe(tally);
            tally.Clear();
        }
        if (!GameLogRule.MayRevive(revivals))
        {
            gaveUp = true;
            HostLog.Write(Category, HostLevel.Warning, "The game's log reached its 10,000-message limit again. It has been turned back on "
                + revivals + " times this session, so it is left off now to spare the disk; lines under this category still reach "
                + "Player.log. The loudest message since the last time: " + loudest);
            return;
        }
        revivals++;
        Log.ResetMessageCount();
        HostLog.Write(Category, HostLevel.Warning, "The game's log reached its 10,000-message limit and was turned back on (" + Ordinal(revivals)
            + " time this session, of at most " + GameLogRule.MaxRevivals + "; the setting \"Keep the game's log going\" turns this off). "
            + "The loudest message since the last time: " + loudest);
    }

    static string Describe(StormTally counted)
    {
        var top = counted.Top(1);
        if (top.Count == 0) return "(none counted; the log stream could not be read: " + (NotListening ?? "unknown") + ")";
        return top[0].Value + " times of " + counted.Total + ": " + top[0].Key;
    }

    static string Ordinal(int n) => n + (n % 100 is >= 11 and <= 13 ? "th" : (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" });

    /// <summary>The loudest signatures since the log was last turned back on, for the status dev action.</summary>
    public static string Status()
    {
        var text = new StringBuilder();
        text.Append("Turned back on ").Append(revivals).Append(" times this session (at most ").Append(GameLogRule.MaxRevivals).Append(")")
            .Append(gaveUp ? "; given up, the limit was reached again after the last." : ".")
            .Append(" Setting: ").Append(FrameworkMod.Settings.keepGameLogAlive ? "on" : "off").Append(". Log stream: ")
            .Append(NotListening == null ? "counted" : "not counted (" + NotListening + ")").Append(". Category lines: ")
            .Append(HostLog.Unavailable == null ? "through the engine" : "through the game's log (" + HostLog.Unavailable + ")").Append('.');
        lock (gate)
        {
            text.Append("\nCounted since the last time: ").Append(tally.Total).Append(" messages, ").Append(tally.Others).Append(" past the tally's ")
                .Append(GameLogRule.TallyCapacity).Append(" signatures.");
            foreach (var entry in tally.Top(5)) text.Append("\n  ").Append(entry.Value).Append("  ").Append(entry.Key);
        }
        return text.ToString();
    }

    /// <summary>Counts the stream by category and level for a while, then writes the counts (dev action T3).</summary>
    public static void StartProbe(TimeSpan length)
    {
        lock (gate)
        {
            probeCounts = new Dictionary<string, int>();
            probeEnds = DateTime.UtcNow + length;
        }
    }

    static void EndProbe()
    {
        Dictionary<string, int> counts;
        lock (gate)
        {
            counts = probeCounts!;
            probeCounts = null;
        }
        var text = new StringBuilder("Log stream probe (T3): messages by category and level over the probe's minute");
        if (counts.Count == 0) text.Append(": none.");
        foreach (var entry in counts) text.Append("\n  ").Append(entry.Value).Append("  ").Append(entry.Key);
        HostLog.Write(Category, HostLevel.Info, text.ToString());
    }
}
