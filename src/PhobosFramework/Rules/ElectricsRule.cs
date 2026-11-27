using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// A vehicle's electrics when the charging fails, game-free (Framework 0.106.0; cross-links round three G16 and avionics A5, A6;
/// docs/development/vehicle-crosslinks.md, docs/development/vehicle-avionics.md). Every threshold is authored.
/// </summary>
public static class ElectricsRule
{
    /// <summary>A generator (or alternator) turning out less than this share of its output counts as lost.</summary>
    public const double GeneratorLost = 0.2;
    /// <summary>Below this charge a battery that is running down with the engine on starts the shedding.</summary>
    public const double LowCharge = 0.3;
    /// <summary>Shedding that began on a low battery ends only once the charge is back above this (so it does not flicker).</summary>
    public const double RecoveredCharge = 0.35;

    /// <summary>
    /// How many ranks of switched loads are shed now: loads are ranked 1 (first off: air conditioning) upward to <paramref name="ranks"/>
    /// (last off: the lights); essential loads have no rank and are never shed. Nothing is shed with the engine stopped (a parked
    /// vehicle's lights run the battery down by the player's choice), nor while the charging keeps up. Once shedding, the depth grows as
    /// the charge falls: the comfort loads first, the lights last.
    /// </summary>
    /// <param name="generator">The generator's working share: its part's efficiency times its drive belt's, 1 when sound.</param>
    /// <param name="previousDepth">The depth at the last look, for the low-battery case's recovery margin.</param>
    /// <param name="generatorLost">Below this working share the generator counts as lost (a rules Def figure since Framework 0.130.0).</param>
    /// <param name="lowCharge">Below this charge a battery running down starts the shedding.</param>
    /// <param name="recoveredCharge">Shedding begun on a low battery ends only above this.</param>
    public static int ShedDepth(bool engineRunning, double generator, double charge, bool runningDown, int previousDepth, int ranks,
        double generatorLost = GeneratorLost, double lowCharge = LowCharge, double recoveredCharge = RecoveredCharge)
    {
        if (!engineRunning || ranks <= 0) return 0;
        bool lost = generator < generatorLost;
        bool low = charge < lowCharge && runningDown;
        bool recovering = previousDepth > 0 && charge < recoveredCharge;
        if (!lost && !low && !recovering) return 0;
        int depth = charge >= 0.6 ? 2 : charge >= 0.45 ? 4 : charge >= lowCharge ? 5 : ranks;
        return Math.Max(1, Math.Min(ranks, depth));
    }

    /// <summary>Whether a load of this rank is off at this depth (rank 0 is essential and never off).</summary>
    public static bool IsShed(int rank, int depth) => rank > 0 && rank <= depth;

    /// <summary>A battery that went flat powers its loads again only once it holds this many times the flat line (authored), so a
    /// trickle charge does not flick them on and off.</summary>
    public const double FlatRecoveryFactor = 2;

    /// <summary>
    /// Whether a vehicle's loads have power now (Framework 0.129.1; docs/development/vehicle-engine-loads.md, check C1): with no battery,
    /// a grid carrying them, or the engine turning a working generator, always; otherwise only while the battery holds charge. A battery
    /// that went flat comes back at <see cref="FlatRecoveryFactor"/> times the line. The one rule for every load, switch, weapon drive and
    /// fitting: before it, a lamp burned on a flat battery while the Systems tab called the same load dead.
    /// </summary>
    /// <param name="generator">The generator's working share: its part's efficiency times its drive belt's, 1 when sound.</param>
    /// <param name="poweredBefore">The answer at the last look, for the recovery margin.</param>
    /// <param name="generatorOnly">Electrics with a generator and no battery (Framework 0.376.0: a steam locomotive's turbo-generator): with no
    /// battery its loads have power only while the engine turns a working generator, not always.</param>
    public static bool LoadsPowered(bool engineRunning, double generator, bool gridCarries, bool hasBattery, double charge, double flatBelow,
        bool poweredBefore, double generatorLost = GeneratorLost, double recoveryFactor = FlatRecoveryFactor, bool generatorOnly = false)
    {
        if (gridCarries) return true;
        if (!hasBattery) return !generatorOnly || (engineRunning && generator >= generatorLost);
        if (engineRunning && generator >= generatorLost) return true;
        return charge >= (poweredBefore ? flatBelow : flatBelow * recoveryFactor);
    }

    /// <summary>
    /// What share of its output an aircraft's generator gives (A5): it is driven from the engine's gearbox, so it follows the spool speed
    /// on <paramref name="spoolOutput"/> (about half at ground idle, full from cruise, authored), and with engines out only the working
    /// engines' generators turn.
    /// </summary>
    public static double GeneratorShare(double spoolOutput, int workingEngines, int engines)
    {
        double output = Math.Max(0, Math.Min(1, spoolOutput));
        if (engines <= 0) return output;
        return output * Math.Max(0, Math.Min(engines, workingEngines)) / engines;
    }

    /// <summary>The game hours a battery lasts at this drain: charge (0 to 1) times capacity over the current; infinity with no drain.</summary>
    public static double HoursLeft(double charge, double ampHours, double drainAmps)
    {
        if (drainAmps <= 0 || ampHours <= 0) return double.PositiveInfinity;
        return Math.Max(0, charge) * ampHours / drainAmps;
    }
}
