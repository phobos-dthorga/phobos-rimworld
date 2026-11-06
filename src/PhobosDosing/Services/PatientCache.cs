using System.Collections.Generic;
using Phobos.Dosing.Defs;
using Phobos.Framework.Health;
using Verse;

namespace Phobos.Dosing.Services;

/// <summary>
/// The patients on a map who need a dose, rebuilt at most once per authored interval when a doctor asks, so the work giver
/// never scans. The framework's per-map cache; nothing is saved.
/// </summary>
public static class PatientCache
{
    static readonly MapPatientCache<Pawn> cache = new(Rebuild);

    public static IReadOnlyList<Pawn> Patients(Map map) => cache.Get(map, PhobosDosingRulesDef.Current.recomputeIntervalTicks);

    static void Rebuild(Map map, List<Pawn> patients)
    {
        DosingSettings settings = DosingMod.Settings;
        if (!settings.enabled) return;
        PhobosDosingRulesDef rules = PhobosDosingRulesDef.Current;
        IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
        for (int i = 0; i < pawns.Count; i++)
        {
            if (DoseService.IsPatient(pawns[i], settings, rules)) patients.Add(pawns[i]);
        }
    }
}
