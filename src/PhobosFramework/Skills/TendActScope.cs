using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Phobos.Framework.Skills;

/// <summary>
/// A tend done off the map (Framework 0.330.0): a convoy's doctor tends on the world map with no job (the game's caravan tending, Vehicle
/// Framework's for its convoys, and our litters all end in <see cref="TendUtility.DoTend"/>), so the tend is wrapped as an act and a skill lift
/// read for its quality is booked with its source once it is done. A prefix that only marks the act, never skipping the original; a
/// finalizer so the mark is always cleared. Spawned doctors are left alone: on a map a tend is a job, and a source keeps its own call.
/// Search and Rescue's own prefixes on this method run first (its highest priority) or beside it; this one is high so the mark is set before
/// any tend quality is read.
/// </summary>
[HarmonyPatch(typeof(TendUtility), nameof(TendUtility.DoTend))]
public static class TendActScope
{
    [HarmonyPriority(Priority.First + 10)]
    public static void Prefix(Pawn? doctor)
    {
        if (SkillLifts.Any && doctor is { Spawned: false } && doctor.Faction is { IsPlayer: true }) SkillLifts.Begin(doctor, SkillActs.Tend);
    }

    public static Exception? Finalizer(Exception? __exception)
    {
        if (SkillLifts.Current != null) SkillLifts.End();
        return __exception;
    }
}
