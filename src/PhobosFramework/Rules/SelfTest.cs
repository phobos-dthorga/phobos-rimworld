using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Phobos.Framework.Rules;

/// <summary>One check of a self-test: what was checked, whether it passed, and why not.</summary>
public sealed record SelfTestResult(string Name, bool Passed, string Detail);

/// <summary>
/// The bookkeeping of a dev-mode self-test, game-free: any Phobos mod can run its checks through this and show one report. A step that
/// throws is a failed step and the run carries on, because the point of a self-test is to say everything that is wrong at once.
/// </summary>
public sealed class SelfTest
{
    readonly List<SelfTestResult> results = new();
    readonly List<string> skipped = new();

    public IReadOnlyList<SelfTestResult> Results => results;

    public int Passed => results.Count(r => r.Passed);

    public int Failed => results.Count(r => !r.Passed);

    public int Skipped => skipped.Count;

    public void Expect(string name, bool passed, string detail = "") => results.Add(new SelfTestResult(name, passed, passed ? "" : detail));

    /// <summary>Passes when the two figures agree within the tolerance.</summary>
    public void Near(string name, double actual, double expected, double tolerance = 0.5) =>
        Expect(name, Math.Abs(actual - expected) <= tolerance, "got " + actual.ToString("0.###") + ", expected " + expected.ToString("0.###"));

    public void Skip(string name, string reason) => skipped.Add(name + ": " + reason);

    /// <summary>Runs one stage; if it throws, that is recorded as a failure and false is returned.</summary>
    public bool Step(string name, Action body)
    {
        try
        {
            body();
            return true;
        }
        catch (Exception error)
        {
            results.Add(new SelfTestResult(name, false, FirstLines(error.ToString(), 4)));
            return false;
        }
    }

    static string FirstLines(string text, int lines) => string.Join("\n", text.Split('\n').Take(lines).Select(line => line.TrimEnd()));

    /// <summary>The counts, then every failure and skip. What passed is only counted.</summary>
    public string Report()
    {
        var text = new StringBuilder();
        text.Append(Passed).Append(" passed, ").Append(Failed).Append(" failed, ").Append(Skipped).Append(" skipped.");
        if (Failed > 0)
        {
            text.Append("\n\nFAILED:");
            foreach (SelfTestResult result in results.Where(r => !r.Passed))
            {
                text.Append("\n- ").Append(result.Name);
                if (result.Detail.Length > 0) text.Append(": ").Append(result.Detail);
            }
        }
        if (Skipped > 0)
        {
            text.Append("\n\nSkipped:");
            foreach (string line in skipped) text.Append("\n- ").Append(line);
        }
        return text.ToString();
    }
}

/// <summary>Whether measured work fits its budget, game-free (docs/development/vehicle-health-design.md, "Performance").</summary>
public static class TimeBudgetRule
{
    public static double MsPerTick(double totalMs, int gameTicks) => gameTicks > 0 ? totalMs / gameTicks : 0;

    public static bool Within(double msPerTick, double worstCallMs, double averageBudgetMs, double singleCallBudgetMs) =>
        msPerTick <= averageBudgetMs && worstCallMs <= singleCallBudgetMs;
}
