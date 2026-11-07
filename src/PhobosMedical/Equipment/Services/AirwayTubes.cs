using System.Collections.Generic;
using Phobos.Medical.Defs;
using Verse;

namespace Phobos.Medical.Services;

/// <summary>Breathing tubes, read from the body by Def extension: whether one is in, whether the upper airway is closing, and what a tube bypasses.</summary>
public static class AirwayTubes
{
    public static bool HasTube(Pawn pawn) => Any<PhobosOpenAirwayExtension>(pawn);

    /// <summary>A condition a tube bypasses, with breathing already weak: the airway is closing and a tube would keep it open.</summary>
    public static bool Closing(Pawn pawn, float breathing, float weakBelow) => breathing < weakBelow && Any<PhobosBypassedByTubeExtension>(pawn);

    /// <summary>Removes the upper-airway conditions a tube has just bypassed.</summary>
    public static void RemoveBypassed(Pawn pawn)
    {
        List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
        for (int i = hediffs.Count - 1; i >= 0; i--)
        {
            if (i < hediffs.Count && hediffs[i].def.HasModExtension<PhobosBypassedByTubeExtension>()) pawn.health.RemoveHediff(hediffs[i]);
        }
    }

    static bool Any<T>(Pawn pawn) where T : DefModExtension
    {
        List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
        for (int i = 0; i < hediffs.Count; i++)
        {
            if (hediffs[i].def.HasModExtension<T>()) return true;
        }
        return false;
    }
}
