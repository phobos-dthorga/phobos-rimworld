using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Life aboard and hotboxing (Framework 0.233.0; docs/development/vehicle-accessories.md, Y4, and docs/development/vehicle-hotboxing.md):
/// the arithmetic, game-free and tested. Every figure passed in is the rules Def's, authored unless the records name a source.
/// </summary>
public static class LifeAboardRule
{
    /// <summary>
    /// The share of a smoker's dose each bystander takes: the closed figure in a closed cab with its windows up, the open figure with the
    /// windows down or in an open cab (Herrmann and colleagues, 2015: unventilated, mild effects; ventilated, none worth showing).
    /// </summary>
    public static double BystanderShare(bool closedCab, bool windowsDown, double closedShare, double openShare) =>
        closedCab && !windowsDown ? closedShare : openShare;

    /// <summary>The severity a bystander takes from a dose, or 0 when it is below <paramref name="least"/> (a trace worth nothing).</summary>
    public static double BystanderSeverity(double dose, double share, double least)
    {
        double taken = Math.Max(0, dose) * Math.Max(0, share);
        return taken < least ? 0 : taken;
    }

    public enum Feeling
    {
        /// <summary>"Hotboxed": a small good memory.</summary>
        Good,
        /// <summary>"Hotboxed against my will".</summary>
        Bad,
    }

    /// <summary>
    /// What a bystander remembers: a bad memory for someone against drugs (a teetotaler by the game's test) or for a hard drug's smoke (the
    /// owner, 12 Nov 2026: a secondhand flake high is always a bad one); a good one otherwise.
    /// </summary>
    public static Feeling Remembers(bool teetotaler, bool hardDrug) => teetotaler || hardDrug ? Feeling.Bad : Feeling.Good;

    /// <summary>The smoke left in a cab after this many hours, clearing evenly over <paramref name="clearHours"/>; never below 0.</summary>
    public static double SmokeLeft(double smoke, double hours, double clearHours) =>
        clearHours <= 0 ? 0 : Math.Max(0, smoke - hours / clearHours);

    /// <summary>The recreation gained over this many hours at a rate an hour.</summary>
    public static double JoyGained(double perHour, double hours) => Math.Max(0, perHour) * Math.Max(0, hours);

    /// <summary>Whether a person may do a thing again: never done, or at least <paramref name="gapTicks"/> since (a clock set back counts as since).</summary>
    public static bool Rested(int lastTick, int now, int gapTicks) => lastTick < 0 || now < lastTick || now - lastTick >= gapTicks;
}
