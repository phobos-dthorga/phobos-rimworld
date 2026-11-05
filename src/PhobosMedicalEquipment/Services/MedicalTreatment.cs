using Phobos.Medical.Comps;
using Verse;

namespace Phobos.Medical.Services;

/// <summary>Starting or keeping up a treatment condition. It lapses by itself once nothing refreshes it.</summary>
public static class MedicalTreatment
{
    public static void GiveOrRefresh(Pawn patient, HediffDef treatment)
    {
        Hediff? condition = patient.health.hediffSet.GetFirstHediffOfDef(treatment);
        condition ??= patient.health.AddHediff(treatment);
        condition?.TryGetComp<HediffComp_PhobosTreatment>()?.Refresh();
    }
}
