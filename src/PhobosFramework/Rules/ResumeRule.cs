using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// How unfinished work is picked up again, game-free (Framework 0.274.0; the owner's report of 24 Nov 2026 that setting up the Billy
/// started from nothing each time a colonist came back to it). The work left is kept as a share of the whole, so a job whose total has
/// changed in between (parked at a bay, a setting changed) carries on from the same point of the way through, as a building's frame does.
/// </summary>
public static class ResumeRule
{
    /// <summary>
    /// Work left untouched this many days is forgotten, so a thing that is gone or an order given up leaves nothing behind for ever.
    /// The owner's choice (24 Nov 2026, "Keep 20 days"), from the agent's proposal: long enough for anything a colony comes back to.
    /// </summary>
    public const int ForgetAfterDays = 20;

    /// <summary>The share of the work still to do, 0 to 1; a total of nothing has nothing to keep.</summary>
    public static double ShareLeft(double workLeft, double total) =>
        total <= 0 ? 0 : Math.Max(0, Math.Min(1, workLeft / total));

    /// <summary>Whether a share is worth keeping: some work done, and some still to do.</summary>
    public static bool WorthKeeping(double shareLeft) => shareLeft > 0 && shareLeft < 1;

    /// <summary>The work to do now: the kept share of today's total, or all of it when nothing was kept.</summary>
    public static double WorkLeft(double? keptShare, double total) =>
        keptShare is { } share && WorthKeeping(share) ? share * Math.Max(0, total) : Math.Max(0, total);

    /// <summary>Whether work last touched at <paramref name="touchedTick"/> is forgotten by <paramref name="nowTick"/>.</summary>
    public static bool Forgotten(int touchedTick, int nowTick, int ticksPerDay, int afterDays = ForgetAfterDays) =>
        (long)nowTick - touchedTick > (long)afterDays * ticksPerDay;
}
