using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Verse;

namespace Phobos.Framework.Debug;

/// <summary>
/// A dev-mode timing accumulator for any Phobos mod (docs/development/vehicle-health-design.md, "Performance"): wrap a piece of work
/// in <c>using (PhobosTiming.Measure("label"))</c> and a dev action reads the totals. Costs one bool test when dev mode is off (the
/// scope is an empty struct then), and a dictionary lookup and a few additions when it is on; never anything else. Nothing is
/// saved. Labels are short and stable, such as "vehicle rare tick / full".
/// </summary>
public static class PhobosTiming
{
    sealed class Entry
    {
        public long Ticks;
        public long Worst;
        public int Calls;
    }

    static readonly Dictionary<string, Entry> entries = new();
    static readonly double millisecondsPerTick = 1000.0 / Stopwatch.Frequency;

    /// <summary>Starts timing a piece of work; dispose the result to stop. Empty, and free, outside dev mode.</summary>
    public static Scope Measure(string label) => Prefs.DevMode ? new Scope(label, Stopwatch.GetTimestamp()) : default;

    static void Add(string label, long elapsed)
    {
        if (!entries.TryGetValue(label, out Entry? entry))
        {
            entry = new Entry();
            entries[label] = entry;
        }
        entry.Ticks += elapsed;
        entry.Calls++;
        if (elapsed > entry.Worst) entry.Worst = elapsed;
    }

    public static void Reset() => entries.Clear();

    /// <summary>One line per label: total milliseconds, calls, the average and the worst call in microseconds.</summary>
    public static string Report()
    {
        if (entries.Count == 0) return "nothing timed yet (dev mode must be on while the work runs)";
        var text = new StringBuilder();
        foreach (KeyValuePair<string, Entry> pair in entries)
        {
            Entry e = pair.Value;
            double total = e.Ticks * millisecondsPerTick;
            double average = e.Calls > 0 ? total * 1000.0 / e.Calls : 0;
            if (text.Length > 0) text.Append('\n');
            text.Append(pair.Key).Append(": ").Append(total.ToString("0.000")).Append(" ms over ").Append(e.Calls).Append(" calls, average ")
                .Append(average.ToString("0.0")).Append(" us, worst ").Append((e.Worst * millisecondsPerTick * 1000.0).ToString("0.0")).Append(" us");
        }
        return text.ToString();
    }

    /// <summary>A running measurement. A default scope (dev mode off) does nothing.</summary>
    public readonly struct Scope : IDisposable
    {
        readonly string? label;
        readonly long start;

        public Scope(string label, long start)
        {
            this.label = label;
            this.start = start;
        }

        public void Dispose()
        {
            if (label != null) Add(label, Stopwatch.GetTimestamp() - start);
        }
    }
}
