namespace Phobos.Framework.Rules;

/// <summary>
/// One person's back and forth with one thing, for the loop watch (Framework 0.205.0; docs/development/pick-up-loops.md): when the watch
/// began, how many times the thing was fetched and then put away again since, which came last, and when the player was last told.
/// </summary>
public sealed class LoopTrail
{
    public int startTick = -1;
    public int turns;
    public bool lastWasPutAway;
    public bool any;
    public int toldTick = -1;
}

/// <summary>
/// The loop watch's rule (Framework 0.205.0; the owner's choice of 21 Nov 2026, "notice and tell"): a person who fetches a thing and puts
/// it away again, over and over in a short while, is caught between two orders that disagree (the case found that day: one mod fetching a
/// sidearm that another's loadout dropped again). A turn is a fetch followed by a putting away; enough turns within the window is a loop,
/// told once, then not again for a quiet spell. Game-free.
/// </summary>
public static class LoopWatchRule
{
    /// <summary>How long a loop has to show itself, ticks: two in-game hours. Authored [A].</summary>
    public const int WindowTicks = 5000;
    /// <summary>How many fetch-and-put-away turns make a loop. Authored [A]: the loops seen ran far past it.</summary>
    public const int TurnsForLoop = 3;
    /// <summary>How long the same person and thing stay quiet after a telling, ticks: a day. Authored [A].</summary>
    public const int QuietTicks = 60000;

    /// <summary>Records a fetch (<paramref name="putAway"/> false) or a putting away (true) at <paramref name="now"/>. True when this makes
    /// the loop the player is to be told of now.</summary>
    public static bool Record(LoopTrail trail, bool putAway, int now, int windowTicks = WindowTicks, int turnsForLoop = TurnsForLoop,
        int quietTicks = QuietTicks)
    {
        if (trail.startTick < 0 || now - trail.startTick > windowTicks)
        {
            trail.startTick = now;
            trail.turns = 0;
            trail.any = false;
        }
        // A turn ends when a putting away follows a fetch; two of the same in a row are one.
        if (putAway && trail.any && !trail.lastWasPutAway) trail.turns++;
        trail.lastWasPutAway = putAway;
        trail.any = true;
        if (trail.turns < turnsForLoop) return false;
        if (trail.toldTick >= 0 && now - trail.toldTick < quietTicks) return false;
        trail.toldTick = now;
        return true;
    }

    /// <summary>Whether a trail has gone quiet long enough to forget: nothing in its window, and no telling to keep quiet about.</summary>
    public static bool Stale(LoopTrail trail, int now, int windowTicks = WindowTicks, int quietTicks = QuietTicks) =>
        now - trail.startTick > windowTicks && (trail.toldTick < 0 || now - trail.toldTick > quietTicks);
}
