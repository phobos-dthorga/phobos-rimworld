using System.Collections.Generic;
using System.Diagnostics;
using Phobos.Logging;
using Verse;

namespace Phobos.Framework.Logging;

/// <summary>One log a player can ask for detail from: a mod, or one part of it, as its lines are headed in Player.log.</summary>
public sealed class LogTopic
{
    public readonly string Mod;
    public readonly string? Part;
    /// <summary>The key the settings save: the mod's name, a slash and the part's (never translated, never renamed once saved).</summary>
    public readonly string Key;

    public LogTopic(string mod, string? part)
    {
        Mod = mod;
        Part = part;
        Key = part == null ? mod : mod + "/" + part;
    }

    /// <summary>As the settings window lists it. These are the headings the lines carry in Player.log, so they are shown as written there.</summary>
    public string Label => Part == null ? Mod : Mod + ": " + Part;
}

/// <summary>
/// The RimWorld host of the shared logging core (phobos-log, compiled into this assembly): a log per mod or part, under the mod's own
/// category in Player.log, with one line budget per mod however many parts it has. Name the static field Logger, not Log: inside a
/// class, a field called Log would hide the game's own class. docs/development/logging-framework-design.md.
///
/// Verbose logging (Framework 0.114.0; owner, 16 Nov 2026): a log's Debug lines are written when the player has ticked it, or the
/// master switch, in D'thorga Framework's settings. Dev mode no longer turns them on by itself: nothing wrote any before, and a player
/// who keeps dev mode on would have had every part's detail at once. Every log made here is listed there, so make a mod's logs in its
/// Mod class (static fields), where they exist by the time the settings window can open. Before writing a Debug line that takes work to
/// build, ask the log's DebugEnabled.
/// </summary>
public static class Logs
{
    sealed class GameSink : ILogSink
    {
        public void Write(string category, LogLevel level, string text) => HostLog.Write(category, level switch
        {
            LogLevel.Error => HostLevel.Error,
            LogLevel.Warning => HostLevel.Warning,
            _ => HostLevel.Info,
        }, text);
    }

    sealed class GameClock : ILogClock
    {
        readonly Stopwatch running = Stopwatch.StartNew();

        public long GameTicks => Current.Game?.tickManager?.TicksGame ?? -1;
        public double RealSeconds => running.Elapsed.TotalSeconds;
    }

    static readonly GameSink Sink = new();
    static readonly GameClock Clock = new();
    static readonly Dictionary<string, LogBudget> Budgets = new();
    static readonly List<LogTopic> TopicList = new();
    static readonly HashSet<string> TopicKeys = new();

    /// <summary>Every log made so far, in the order made, for the settings window's ticks.</summary>
    public static IReadOnlyList<LogTopic> Topics
    {
        get
        {
            lock (Budgets) return TopicList.ToArray();
        }
    }

    /// <summary>The log of a mod, or of one part of it. Debug lines are written only while the player has asked for its detail.</summary>
    public static PhobosLog For(string mod, string? part = null)
    {
        LogBudget budget;
        var topic = new LogTopic(mod, part);
        lock (Budgets)
        {
            if (!Budgets.TryGetValue(mod, out budget!)) Budgets[mod] = budget = new LogBudget();
            if (TopicKeys.Add(topic.Key)) TopicList.Add(topic);
        }
        string key = topic.Key;
        return new PhobosLog(mod, part, Sink, Clock, budget, () => FrameworkMod.Settings.Verbose(key));
    }
}
