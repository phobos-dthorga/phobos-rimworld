using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// What the ground under an aircraft on our own flight does to it as it lifts off or touches down (Framework 0.158.0; the owner's decision
/// of 18 Nov 2026, docs/development/vehicle-osprey.md, "What it lands on"), game-free. Three cases from three real events: loose sand or soil
/// thrown up by the rotors wears the engines' hot sections (an Osprey's engine stalled on dust at Bellows, Hawaii, in 2015, and engines lasted
/// under 400 hours in service); dry growth under the exhaust may catch fire (a grass fire at a North Carolina range in 2013), most in a heat
/// wave; built floor or rock costs nothing. An inlet barrier keeps most of the dust out, as the real one is meant to. Rolling on or off
/// throws up less dust than hovering over it, which is why crews in brown-out land rolling where they can. Every figure is authored, a
/// gameplay choice.
/// </summary>
public static class LandingSurfaceRule
{
    /// <summary>The share of each engine's full health one lift-off or touch-down costs: <paramref name="perEvent"/> on wholly loose ground,
    /// in proportion to the share of the ground beneath that is loose, times <paramref name="rollingFactor"/> when rolling, and times what
    /// an inlet barrier lets through (<paramref name="barrierStops"/> is the share it stops). Never below 0.</summary>
    public static double EngineWearShare(double looseShare, bool rolling, bool barrier, double perEvent, double rollingFactor, double barrierStops)
    {
        if (double.IsNaN(looseShare) || looseShare <= 0 || perEvent <= 0) return 0;
        double share = perEvent * Math.Min(1, looseShare);
        if (rolling) share *= Clamp01(rollingFactor);
        if (barrier) share *= 1 - Clamp01(barrierStops);
        return Math.Max(0, share);
    }

    /// <summary>The chance one lift-off or touch-down sets dry growth alight: <paramref name="perEvent"/> with growth under the whole
    /// footprint, in proportion to the share of it that holds dry growth, times <paramref name="heatWaveFactor"/> in a heat wave. Between 0
    /// and 1.</summary>
    public static double FireChance(double dryGrowthShare, bool heatWave, double perEvent, double heatWaveFactor)
    {
        if (double.IsNaN(dryGrowthShare) || dryGrowthShare <= 0 || perEvent <= 0) return 0;
        double chance = perEvent * Math.Min(1, dryGrowthShare);
        if (heatWave) chance *= Math.Max(1, heatWaveFactor);
        return Clamp01(chance);
    }

    /// <summary>Whether growth is dry enough to catch: no rain falling, and no more snow on it than <paramref name="snowAtMost"/>.</summary>
    public static bool Dry(double rainRate, double snowDepth, double snowAtMost) => rainRate <= 0 && snowDepth <= snowAtMost;

    static double Clamp01(double value) => double.IsNaN(value) ? 0 : Math.Max(0, Math.Min(1, value));
}
