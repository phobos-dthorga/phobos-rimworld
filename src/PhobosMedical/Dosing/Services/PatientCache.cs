using System.Collections.Generic;
using Phobos.Dosing.Defs;
using Phobos.Framework.Health;
using Verse;

namespace Phobos.Dosing.Services;

/// <summary>
/// The patients on a map who need a dose and may be given one now, rebuilt at most once per authored interval when a doctor asks, so
/// the work giver never scans. What depends only on the patient (the need, and whether the safety rule allows anything) is decided here,
/// once for every doctor; what depends on the doctor (what they carry and can reach) is left to the service. A patient dosed since the
/// last rebuild stays listed until the next, and the service refuses them then as it always did. The framework's per-map cache; nothing
/// is saved.
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
            if (DoseService.IsPatient(pawns[i], settings, rules) && DoseService.AnyDoseAllowed(pawns[i], settings, rules)) patients.Add(pawns[i]);
        }
    }
}
