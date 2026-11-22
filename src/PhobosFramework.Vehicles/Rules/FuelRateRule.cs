using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>What a vehicle's fuel is burned for (Framework 0.130.0; docs/development/vehicle-engine-loads.md, step S2). Append only: the
/// names are shown and logged.</summary>
public enum FuelUse
{
    /// <summary>The engine model's correction to Vehicle Framework's own flat rate (driving, idling, the loads): may be negative, a refund.</summary>
    Engine,
    /// <summary>Standing work: a drill, a scanner, a generator head.</summary>
    Work,
    /// <summary>A fuel-burning heater: the night heater, an electric vehicle's cab or pack heater.</summary>
    Heater,
    /// <summary>Fuel lost from a holed tank.</summary>
    Leak,
    /// <summary>A helicopter's own flying work outside Vehicle Framework's flight, such as a water-bucket run.</summary>
    Flight,
    /// <summary>Taken out because the tank or pack can hold less than it holds (a worn pack): not a rate, never shown as one.</summary>
    Spilled,
    /// <summary>An electric vehicle's converter feeding its 12 V side from the drive battery (Framework 0.243.0).</summary>
    Electrics,
}

/// <summary>
/// How fast a vehicle burns its fuel for each use, from what it burned lately (Framework 0.130.0, step S2). Each use keeps one figure that
/// grows by what is burned and fades with a time constant of a game hour, so the rate it gives is the last hour or so, smoothed, and
/// nothing is stored per burn. Game-free.
/// </summary>
public static class FuelRateRule
{
    /// <summary>The fade's time constant in game ticks: an hour (authored).</summary>
    public const double FadeTicks = 2500;

    /// <summary>Game ticks in a day, as the game has it.</summary>
    public const double TicksPerDay = 60000;

    /// <summary>The figure after fading from <paramref name="lastTick"/> to <paramref name="now"/> and adding <paramref name="amount"/>.</summary>
    public static double Add(double figure, int lastTick, int now, double amount) => Faded(figure, lastTick, now) + amount;

    /// <summary>The figure faded to <paramref name="now"/>.</summary>
    public static double Faded(double figure, int lastTick, int now)
    {
        if (now <= lastTick || figure == 0) return figure;
        return figure * Math.Exp(-(now - lastTick) / FadeTicks);
    }

    /// <summary>The rate per day the figure stands for: a steady burn of r a tick settles at r times the time constant.</summary>
    public static double PerDay(double figure, int lastTick, int now) => Faded(figure, lastTick, now) / FadeTicks * TicksPerDay;

    /// <summary>Vehicle Framework's own burn now, per day: its daily rate in full while moving, half while drafted and standing on a map
    /// (CompFueledTravel.ConsumptionRatePerTick), nothing undrafted.</summary>
    public static double FrameworkPerDay(double dailyRate, bool burning, bool moving) => !burning ? 0 : moving ? dailyRate : dailyRate * FrameworkIdleShare;

    /// <summary>The share of its daily rate Vehicle Framework burns while a vehicle stands drafted on a map (a fact of its code).</summary>
    public const double FrameworkIdleShare = 0.5;

    /// <summary>
    /// How far Vehicle Framework's fuel rate for a vehicle stands from the rate its Def writes (owner, 20 Nov 2026: "defensively for other
    /// cases like this, and with other vehicles"). An upgrade moves it (the Harrier's plasteel armour: 172.5 against 150, so 1.15), and so
    /// does the player's own Vehicle Framework setting. Every rate Vehicle Framework burns by already carries it; a flight mod's own flat
    /// rate on a map does not, so ours carries that rate by this factor, and whatever moves Vehicle Framework's rate moves the engines'
    /// burn too, without reading any upgrade by name. 1 when either figure is unusable; kept between <see cref="MinRateFactor"/> and
    /// <see cref="MaxRateFactor"/>, so a broken figure can never stop the burn or run away with it.
    /// </summary>
    public static double RateFactor(double current, double written)
    {
        if (!(written > 0) || !(current > 0) || double.IsInfinity(written) || double.IsInfinity(current)) return 1;
        return Math.Clamp(current / written, MinRateFactor, MaxRateFactor);
    }

    /// <summary>The least <see cref="RateFactor"/> gives (authored: a tenth, far below any real fuel upgrade).</summary>
    public const double MinRateFactor = 0.1;

    /// <summary>The most <see cref="RateFactor"/> gives (authored: ten times, far above any real fuel upgrade).</summary>
    public const double MaxRateFactor = 10;
}
