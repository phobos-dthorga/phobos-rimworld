using System.Collections.Generic;
using Phobos.Medical.Defs;
using Phobos.Medical.Rules;
using Verse;

namespace Phobos.Medical.Services;

/// <summary>Pressure in the chest a needle relieves, read from the body by <see cref="PhobosPressureReliefExtension"/>.</summary>
public static class PressureRelief
{
    /// <summary>A needle never clears the condition: a trace stays, so the operation is still needed and offered.</summary>
    const float Trace = 0.01f;

    public static bool Any(Pawn patient)
    {
        List<Hediff> hediffs = patient.health.hediffSet.hediffs;
        for (int i = 0; i < hediffs.Count; i++)
        {
            PhobosPressureReliefExtension? relief = hediffs[i].def.GetModExtension<PhobosPressureReliefExtension>();
            if (relief != null && hediffs[i].Severity >= relief.minSeverity) return true;
        }
        return false;
    }

    public static void Relieve(Pawn patient)
    {
        List<Hediff> hediffs = patient.health.hediffSet.hediffs;
        for (int i = 0; i < hediffs.Count; i++)
        {
            PhobosPressureReliefExtension? relief = hediffs[i].def.GetModExtension<PhobosPressureReliefExtension>();
            if (relief == null || hediffs[i].Severity < relief.minSeverity) continue;
            hediffs[i].Severity = DeviceRule.AfterRelief(hediffs[i].Severity, relief.severityRelieved, Trace);
        }
    }
}
