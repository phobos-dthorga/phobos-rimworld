using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Framework.Rules;

/// <summary>
/// The service yard, game-free (D'thorga Framework 0.210.0; docs/development/vehicle-service-yard.md; owner, 20 Nov 2026: "a restrained size
/// so that the player cannot build something of unlimited size and start cheesing things"): how many cells a yard's equipment allows, whether
/// a yard works, what share of the usual work a job in it takes, the order vehicles take turns on its dyno bays and its pumps, and which
/// vehicle makes room when it is full.
/// </summary>
public static class ServiceYardRule
{
    /// <summary>
    /// The cells a yard may cover: the sum of what each piece of its equipment adds, but nothing at all without a piece the yard needs (a dyno
    /// bay: "a yard without one is not a yard").
    /// </summary>
    public static int Allowance(IEnumerable<(int cells, bool required)> equipment)
    {
        int total = 0;
        bool hasRequired = false;
        foreach ((int cells, bool required) in equipment)
        {
            total += Math.Max(0, cells);
            hasRequired |= required;
        }
        return hasRequired ? total : 0;
    }

    /// <summary>How many cells a yard has past its allowance: equipment lost makes a yard too big, and it is never shrunk for the player.</summary>
    public static int Overflow(int cells, int allowance) => Math.Max(0, cells - allowance);

    /// <summary>How many more cells a yard may take.</summary>
    public static int Room(int cells, int allowance) => Math.Max(0, allowance - cells);

    /// <summary>A yard gives its services only with the equipment it needs, and within its allowance.</summary>
    public static bool Works(int cells, int allowance, bool hasRequired) => hasRequired && cells > 0 && cells <= allowance;

    /// <summary>
    /// The share of the usual work a job takes on a vehicle: the better of a working yard's and its own depot bay's pad, where it stands in
    /// either; the usual work otherwise.
    /// </summary>
    public static float WorkFactor(bool inWorkingYard, float yardFactor, bool onOwnPad, float padFactor)
    {
        float factor = 1f;
        if (inWorkingYard) factor = Math.Min(factor, Clamp(yardFactor));
        if (onOwnPad) factor = Math.Min(factor, Clamp(padFactor));
        return factor;
    }

    static float Clamp(float factor) => factor <= 0f ? 1f : Math.Min(1f, factor);

    /// <summary>
    /// The order vehicles take turns in a line: the player's latest Move up first, then the longest waiting (the earliest to ask), then by
    /// ID so the order never flickers. <paramref name="movedUp"/> is the tick of the vehicle's Move up, or a negative number for none.
    /// </summary>
    public static List<T> Line<T>(IEnumerable<T> waiting, Func<T, int> asked, Func<T, int> movedUp, Func<T, int> id) =>
        waiting.OrderByDescending(v => movedUp(v) >= 0)
            .ThenByDescending(movedUp)
            .ThenBy(asked)
            .ThenBy(id)
            .ToList();

    /// <summary>How many vehicles a line serves at once: each working dyno bay's turns (one, or two with a second roller set).</summary>
    public static int Turns(IEnumerable<int> turnsPerBay) => turnsPerBay.Sum(t => Math.Max(0, t));

    /// <summary>
    /// The next vehicle a pump fills: the emptiest share of its tank first, then by ID; none that are full (at or past
    /// <paramref name="full"/>).
    /// </summary>
    public static T? NextToFill<T>(IEnumerable<T> candidates, Func<T, float> share, Func<T, int> id, float full = 0.995f) where T : class =>
        candidates.Where(c => share(c) < full).OrderBy(share).ThenBy(id).FirstOrDefault();

    /// <summary>
    /// The vehicle taken out to make room in a full yard: the one that has stood there longest with nothing left to do (the earliest
    /// <paramref name="since"/>), then by ID. None when every vehicle in the yard still has work there.
    /// </summary>
    public static T? ToMakeRoom<T>(IEnumerable<T> idle, Func<T, int> since, Func<T, int> id) where T : class =>
        idle.OrderBy(since).ThenBy(id).FirstOrDefault();

    /// <summary>
    /// Which yard a piece of equipment touching several belongs to, so one dyno bay never pays for two yards: the yard with the lowest ID.
    /// </summary>
    public static int Owner(IEnumerable<int> yardIds)
    {
        int best = int.MaxValue;
        foreach (int id in yardIds) best = Math.Min(best, id);
        return best;
    }
}
