using System;
using System.Collections.Generic;

namespace Phobos.Framework.Rules;

/// <summary>What sends a vehicle to a dyno bay by itself, the player's setting. Saved by name: append only.</summary>
public enum BayTripTrigger
{
    /// <summary>Only work that waits for a bay: a heavy overhaul, a queued tune or change of grade, a diagnosis.</summary>
    BayOnly,
    /// <summary>The above, and a destroyed part to replace or a serious fault that needs a service. The default (owner, 20 Nov 2026).</summary>
    Broken,
    /// <summary>The above, and every due service, fitting and damaged part.</summary>
    Any,
}

/// <summary>What a piece of work is, for the trigger.</summary>
public enum BayTripWorkKind
{
    /// <summary>It can be done nowhere but at a bay.</summary>
    BayOnly,
    /// <summary>The vehicle is broken: a destroyed part, a serious fault.</summary>
    Broken,
    /// <summary>Routine: a service, a fitting, a damaged part.</summary>
    Routine,
}

/// <summary>Where a vehicle goes once its work at the bay is done, in the owner's order (20 Nov 2026).</summary>
public enum BayTripHome
{
    DepotBay,
    Park,
    Nearby,
    /// <summary>It stays in the service yard it was worked in (Framework 0.210.0): it has no depot bay of its own.</summary>
    Yard,
}

/// <summary>How a vehicle gets to the bay.</summary>
public enum BayTripCarry
{
    /// <summary>It cannot go: it will not drive, nothing is free to tow it, and it is too heavy to push or pushing is off.</summary>
    None,
    Drive,
    /// <summary>Towed by a vehicle with a recovery winch.</summary>
    Tow,
    /// <summary>Pushed by hand.</summary>
    Push,
}

/// <summary>
/// Bay trips, game-free (Framework 0.201.0; docs/development/vehicle-bay-trips.md): what sends a vehicle to a dyno bay by itself, whether
/// its work can be paid for, when it looks again, when it has waited at the bay long enough, and where it goes afterwards.
/// </summary>
public static class BayTripRule
{
    /// <summary>Whether a piece of work of this kind sends a vehicle at this level.</summary>
    public static bool Sends(BayTripTrigger level, BayTripWorkKind kind) => kind switch
    {
        BayTripWorkKind.BayOnly => true,
        BayTripWorkKind.Broken => level >= BayTripTrigger.Broken,
        _ => level >= BayTripTrigger.Any,
    };

    /// <summary>
    /// Whether work can be paid for: it asks for nothing, or one of its ways of paying has every line met. A vehicle goes to the bay only
    /// for work that can be done there, so a wreck whose part nobody has made never takes a place.
    /// </summary>
    public static bool Payable<T>(IReadOnlyList<List<T>> options, Func<T, bool> met)
    {
        if (options.Count == 0) return true;
        for (int i = 0; i < options.Count; i++)
        {
            bool all = true;
            foreach (T line in options[i])
            {
                if (met(line)) continue;
                all = false;
                break;
            }
            if (all) return true;
        }
        return false;
    }

    /// <summary>
    /// How a vehicle gets to the bay, in the owner's order (20 Nov 2026: "Tow, or push light ones by hand"): driven if it drives; else
    /// towed, if towing is on and a recovery vehicle is free; else pushed, if pushing is on and no more people are needed than can get
    /// their hands on it; else it cannot go.
    /// </summary>
    public static BayTripCarry Carry(bool canDrive, bool towingOn, bool tugFree, bool pushingOn, int pushersNeeded, int mostPushers)
    {
        if (canDrive) return BayTripCarry.Drive;
        if (towingOn && tugFree) return BayTripCarry.Tow;
        if (pushingOn && pushersNeeded >= 1 && pushersNeeded <= mostPushers) return BayTripCarry.Push;
        return BayTripCarry.None;
    }

    /// <summary>Whether a vehicle's next look is due.</summary>
    public static bool LookDue(int now, int nextLook) => now >= nextLook;

    /// <summary>
    /// When a vehicle looks next: an interval on, moved by its ID within <paramref name="spread"/> so a fleet does not all look on one tick.
    /// </summary>
    public static int NextLook(int now, int interval, int id, int spread) =>
        now + Math.Max(1, interval) + (spread > 0 ? Math.Abs(id % spread) : 0);

    /// <summary>Whether a wait has gone on past its limit. No limit means never.</summary>
    public static bool Overstayed(int ticksWaited, int limit) => limit > 0 && ticksWaited > limit;

    /// <summary>
    /// Whether a trip under way goes on (Framework 0.276.4; the owner's card of 24 Nov 2026, "Your order always goes"): never once the vehicle
    /// is not the player's; a trip the player ordered (the Fleet board's Send to yard or Take out of yard) whatever the bay-trip setting or
    /// the vehicle's kept-out switch; an automatic trip only while trips are on and the vehicle is not kept out.
    /// </summary>
    public static bool GoesOn(bool players, bool tripsOn, bool keptOut, bool byOrder) => players && (byOrder || (tripsOn && !keptOut));

    /// <summary>Where a vehicle goes afterwards: its depot bay, else the vehicle park, else a spot nearby.</summary>
    public static BayTripHome Home(bool depotBayFree, bool parkHasRoom) =>
        depotBayFree ? BayTripHome.DepotBay : parkHasRoom ? BayTripHome.Park : BayTripHome.Nearby;
}
