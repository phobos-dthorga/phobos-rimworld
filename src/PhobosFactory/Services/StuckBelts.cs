using System;
using HarmonyLib;
using Phobos.Factory.Rules;
using VanillaFurnitureExpandedFactory;
using Verse;

namespace Phobos.Factory;

/// <summary>
/// Wakes VFE Factory belts that are stuck empty in the Waiting state (<see cref="BeltRule.IsStuck"/>). Such a belt
/// never moves again by itself, and a reload or "Change direction" keeps the state, so the only fix in game was
/// to rebuild it. The owner's PG6 save (30 Oct 2026) had nine, two of them starving the human-meat neutroamine
/// line with a full meat hopper behind them. What empties them is not known yet: every path in VFE Factory's own
/// belt code resets a belt it empties. Waking calls VFE Factory's own reset to Empty, so the belt pulls again on
/// its next tick.
/// </summary>
public static class StuckBelts
{
    /// <summary>
    /// How many wakes a session writes to the log, each with the belt's position, so a pattern can show what
    /// empties them without flooding the log if something empties belts all the time.
    /// </summary>
    public const int LoggedWakesPerSession = 10;

    private static Action<Building_Conveyor>? reset;
    private static bool resetMissing;
    private static int wakes;

    /// <summary>
    /// Finds VFE Factory's private reset. False if an update renamed it; the guard then stays off and the log
    /// says so once.
    /// </summary>
    public static bool FindReset()
    {
        if (reset != null) return true;
        if (resetMissing) return false;
        var method = AccessTools.Method(typeof(Building_Conveyor), "ResetConveyorState");
        if (method == null)
        {
            resetMissing = true;
            Log.Warning("[D'thorga Factory Additions] VFE Factory's belt reset was not found, so stuck belts are not " +
                "woken; that part is switched off.");
            return false;
        }
        reset = AccessTools.MethodDelegate<Action<Building_Conveyor>>(method);
        return true;
    }

    /// <summary>Resets the belt if it is stuck. Runs every tick for every belt, so the common case is two field reads.</summary>
    public static void WakeIfStuck(Building_Conveyor belt)
    {
        if (!BeltRule.IsStuck(belt.state == Building_Conveyor.ConveyorState.Waiting, belt.innerContainer.Count)) return;
        reset!(belt);
        wakes++;
        if (wakes > LoggedWakesPerSession) return;
        Log.Message($"[D'thorga Factory Additions] Woke a stuck {belt.def.label} at {belt.Position}: it was empty but " +
            "waiting, which VFE Factory doesn't recover from." +
            (wakes == LoggedWakesPerSession ? " Further wakes this session are not logged." : ""));
    }
}
