namespace Phobos.Framework.Rules;

/// <summary>
/// The rule behind "drones come home in time" and "wrecked drones are taken home" (Framework 0.240.0; docs/development/drone-recall-design.md;
/// owner, 23 Nov 2026: the drone bays starved because drones died of a flat power cell instead of walking home, where a return gives their
/// steel back), game-free. A drone is called home once the power it has left would barely cover the walk home, its wait at the platform
/// and a spare margin; a wreck gives its platform back what it was salvaged for, as far as the store has room, and the rest is set down
/// beside it.
/// </summary>
public static class RecallRule
{
    /// <summary>The walk home is reckoned half as long again as the path finder prices it: drones were seen walking far slower than their
    /// path promised (one 64 cells out after 3,000 ticks), so the price is padded. Authored (agent default).</summary>
    public const double WalkSafety = 1.5;

    /// <summary>Power kept spare on arrival, in ticks: ten seconds of game time. Authored (agent default).</summary>
    public const int SpareTicks = 600;

    /// <summary>How far a straight line is stretched before a path is worth pricing. A straight line can only under-price the walk, so a
    /// drone whose power is above this many times its straight-line walk is left alone without asking the path finder. Authored.</summary>
    public const double RoughStretch = 4.0;

    /// <summary>The power, in ticks, a drone needs to walk home and dock: the padded walk, the wait at its platform and the spare.</summary>
    /// <param name="walkTicks">The walk home as priced (the path finder's cost, or a stretched straight line).</param>
    /// <param name="dockTicks">How long the drone waits at its platform before it is taken in.</param>
    public static int Needed(double walkTicks, int dockTicks)
    {
        double walk = walkTicks > 0 ? walkTicks : 0;
        return (int)System.Math.Ceiling(walk * WalkSafety) + System.Math.Max(0, dockTicks) + SpareTicks;
    }

    /// <summary>Whether the drone should be called home now.</summary>
    public static bool CallHome(int powerTicksLeft, double walkTicks, int dockTicks) => powerTicksLeft <= Needed(walkTicks, dockTicks);

    /// <summary>Whether the walk is worth pricing with the path finder: only when even a stretched straight line comes close.</summary>
    public static bool WorthPricing(int powerTicksLeft, double straightTicks, int dockTicks) =>
        powerTicksLeft <= Needed(straightTicks * RoughStretch, dockTicks);

    /// <summary>
    /// How a wreck's salvage divides: what goes into the store (as far as it has room) and what is set down beside it. A store with no
    /// known capacity (<paramref name="room"/> below 0) takes it all. Nothing is made from nothing: the two always add up to the salvage.
    /// </summary>
    public static (int IntoStore, int Beside) TakeBack(int salvage, int room)
    {
        int amount = System.Math.Max(0, salvage);
        if (room < 0) return (amount, 0);
        int into = System.Math.Min(amount, room);
        return (into, amount - into);
    }
}
