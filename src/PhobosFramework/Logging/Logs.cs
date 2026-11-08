using System.Collections.Generic;
using System.Diagnostics;
using Phobos.Logging;
using Verse;

namespace Phobos.Framework.Logging;

/// <summary>
/// The RimWorld host of the shared logging core (phobos-log, compiled into this assembly): a log per mod or part, under the mod's own
/// category in Player.log, with one line budget per mod however many parts it has. Name the static field Logger, not Log: inside a
/// class, a field called Log would hide the game's own class. docs/development/logging-framework-design.md.
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

    /// <summary>The log of a mod, or of one part of it. Debug lines are written only in dev mode.</summary>
    public static PhobosLog For(string mod, string? part = null)
    {
        LogBudget budget;
        lock (Budgets)
        {
            if (!Budgets.TryGetValue(mod, out budget!)) Budgets[mod] = budget = new LogBudget();
        }
        return new PhobosLog(mod, part, Sink, Clock, budget, () => Prefs.DevMode);
    }
}
