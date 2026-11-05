using System.Collections.Generic;
using System.Linq;
using Phobos.Medical.Comps;
using Phobos.Medical.Defs;
using Phobos.Medical.Rules;
using RimWorld;
using Verse;

namespace Phobos.Medical.Services;

/// <summary>Decides and does a burn unit bed's care. The comp and the inspect text only call this and read what it reports.</summary>
public static class BurnCareService
{
    /// <summary>One pass: keep the treatment going for each occupant with an open burn. Returns who it served.</summary>
    public static List<Pawn> Care(CompPhobosBurnCare care)
    {
        if (care.parent is not Building_Bed bed || !care.parent.Spawned || StatusOf(care, 0) != BurnCareStatus.Waiting) return new List<Pawn>();
        List<Pawn> patients = bed.CurOccupants.Where(HasOpenBurn).ToList();
        foreach (Pawn patient in patients) MedicalTreatment.GiveOrRefresh(patient, care.Props.treatment);
        return patients;
    }

    public static BurnCareStatus StatusOf(CompPhobosBurnCare care, int patients)
    {
        CompPowerTrader? power = care.parent.TryGetComp<CompPowerTrader>();
        return BurnCareRule.Status(FlickUtility.WantsToBeOn(care.parent), power == null || power.PowerOn, patients);
    }

    /// <summary>An unhealed burn. A scar is not an open burn, and any mod can mark its own burn Def with the extension.</summary>
    public static bool HasOpenBurn(Pawn pawn)
    {
        if (!pawn.RaceProps.IsFlesh || pawn.Dead) return false;
        List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
        for (int i = 0; i < hediffs.Count; i++)
        {
            Hediff hediff = hediffs[i];
            if (hediff is Hediff_Injury && hediff.def.HasModExtension<PhobosBurnCareExtension>() && !hediff.IsPermanent()) return true;
        }
        return false;
    }
}
