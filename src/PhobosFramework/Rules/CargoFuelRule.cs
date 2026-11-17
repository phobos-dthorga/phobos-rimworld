using System;
using System.Collections.Generic;

namespace Phobos.Framework.Rules;

/// <summary>One vehicle in a convoy, for <see cref="CargoFuelRule.BudgetTicks"/>: its tank, what it burns a tick on the move, and the
/// pool of carried fuel it may top up from (-1 for none: it runs on its tank alone).</summary>
public readonly struct FuelUser
{
    public readonly double Tank;
    public readonly double PerTick;
    public readonly int Pool;

    public FuelUser(double tank, double perTick, int pool)
    {
        Tank = tank;
        PerTick = perTick;
        Pool = pool;
    }
}

/// <summary>
/// Topping a tank up from fuel carried as cargo (D'thorga Framework 0.137.0; docs/development/vehicle-cargo-refuel.md): when, how much,
/// how long the stop takes, and how far a convoy gets when some of its vehicles draw on shared cans. Game-free, and in the core because
/// anything with a tank and a cargo hold can use it.
/// </summary>
public static class CargoFuelRule
{
    /// <summary>Whether a tank is low enough to top up: at or below <paramref name="belowShare"/> of its capacity.</summary>
    public static bool ShouldTopUp(double fuel, double capacity, double belowShare) => capacity > 0 && fuel <= capacity * belowShare;

    /// <summary>How much to pour: enough to fill the tank, never more than is carried, in whole units (fuel is carried as items).</summary>
    public static int TopUpAmount(double fuel, double capacity, double carried)
    {
        double room = Math.Max(0, capacity - fuel);
        return (int)Math.Floor(Math.Min(room, Math.Max(0, carried)));
    }

    /// <summary>How long pouring takes, in ticks: <paramref name="hoursPerTank"/> for a whole tank, in proportion for less.</summary>
    public static double StopTicks(double poured, double capacity, double hoursPerTank, double ticksPerHour) =>
        capacity <= 0 || poured <= 0 ? 0 : poured / capacity * Math.Max(0, hoursPerTank) * ticksPerHour;

    /// <summary>
    /// How many ticks a convoy can move before a vehicle runs dry. The convoy moves as one, so every vehicle burns for the same time. A
    /// vehicle on its tank alone lasts tank ÷ burn. Vehicles sharing a pool of carried fuel last, together, as long as their tanks and the
    /// pool feed all of them: (their tanks + the pool) ÷ their burns added. Topping up waits until a tank is low, but every can is
    /// poured in the end, so the order does not change the answer. Null when nothing burns fuel.
    /// </summary>
    public static double? BudgetTicks(IReadOnlyList<FuelUser> users, IReadOnlyList<double> pools)
    {
        double? budget = null;
        void Take(double ticks) => budget = budget == null ? ticks : Math.Min(budget.Value, ticks);
        var tanks = new double[pools.Count];
        var burns = new double[pools.Count];
        foreach (FuelUser user in users)
        {
            if (user.PerTick <= 0) continue;
            if (user.Pool < 0 || user.Pool >= pools.Count)
            {
                Take(Math.Max(0, user.Tank) / user.PerTick);
                continue;
            }
            tanks[user.Pool] += Math.Max(0, user.Tank);
            burns[user.Pool] += user.PerTick;
        }
        for (int i = 0; i < pools.Count; i++)
        {
            if (burns[i] > 0) Take((tanks[i] + Math.Max(0, pools[i])) / burns[i]);
        }
        return budget;
    }
}
