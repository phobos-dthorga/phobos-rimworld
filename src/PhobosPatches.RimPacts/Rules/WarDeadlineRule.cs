namespace Phobos.RimPactsPatches.Rules;

/// <summary>
/// When a RimPacts world war quest really ends. Game-free. RimPacts sets its deadline at acceptance plus three days, but only acts on
/// it in its daily check, which runs once a game day at its own saved tick (read in RimPacts 1.7, 7 Nov 2026: the check fails the war
/// when the tick is at or past the deadline). So the war is lost at the first daily check at or after the deadline, up to a day later
/// than the deadline itself (owner's Lance Village war, 7 Nov 2026: due at tick 48,008,059, failed at 48,060,000). The quest window
/// shows this tick, so the countdown says when the war is actually lost.
/// </summary>
public static class WarDeadlineRule
{
    /// <summary>
    /// The tick of the daily check that fails the war. <paramref name="nextDailyCheck"/> is RimPacts' next scheduled check; when it is
    /// unknown (zero or less) the deadline itself is the best figure.
    /// </summary>
    public static int EndTick(int deadline, int nextDailyCheck, int checkInterval)
    {
        if (nextDailyCheck <= 0 || checkInterval <= 0) return deadline;
        if (nextDailyCheck >= deadline) return nextDailyCheck;
        int checks = (deadline - nextDailyCheck + checkInterval - 1) / checkInterval;
        return nextDailyCheck + checks * checkInterval;
    }
}
