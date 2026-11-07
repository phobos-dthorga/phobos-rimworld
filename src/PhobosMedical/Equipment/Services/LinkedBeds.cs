using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Phobos.Medical.Services;

/// <summary>Whoever lies in the beds a bedside machine links to. One copy for every machine (the oxygen supply, the ventilator, the nebuliser).</summary>
public static class LinkedBeds
{
    public static IEnumerable<Pawn> Patients(CompFacility? facility)
    {
        if (facility?.LinkedBuildings == null) yield break;
        foreach (Thing linked in facility.LinkedBuildings)
        {
            if (linked is not Building_Bed bed) continue;
            foreach (Pawn occupant in bed.CurOccupants) yield return occupant;
        }
    }
}
