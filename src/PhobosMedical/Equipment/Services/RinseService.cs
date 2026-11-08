using System.Collections.Generic;
using System.Linq;
using Phobos.BurnsAndSmoke;
using Phobos.BurnsAndSmoke.Services;
using Phobos.Framework.Health;
using Phobos.Medical.Comps;
using Phobos.Medical.Defs;
using Phobos.BurnsAndSmoke.Rules;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Medical.Services;

/// <summary>Who needs rinsing, where to, and the water it takes. The jobs only call this; the effect on the body is <see cref="ChemicalService"/>'s.</summary>
public static class RinseService
{
    const int CacheIntervalTicks = 250;
    /// <summary>How far a pawn runs for a shower before giving up on it (agent choice).</summary>
    const float MaxRunDistance = 60f;
    const float MaxFetchDistance = 9999f;

    static List<ThingDef>? stationDefs;
    static readonly MapPatientCache<Pawn> patients = new(Rebuild);

    public static IReadOnlyList<ThingDef> StationDefs => stationDefs ??= DefDatabase<ThingDef>.AllDefsListForReading
        .Where(def => def.HasComp(typeof(CompPhobosRinseStation))).ToList();

    public static IPhobosWaterSource? WaterSource(Thing station) => (station as ThingWithComps)?.AllComps.OfType<IPhobosWaterSource>().FirstOrDefault();

    public static bool NeedsRinse(Pawn pawn) => ChemicalService.NeedsRinse(pawn);

    /// <summary>Patients lying down with acid on their skin, whose care setting lets doctors act; rebuilt at most every few seconds.</summary>
    public static IReadOnlyList<Pawn> PatientsNeedingRinse(Map map) => patients.Get(map, CacheIntervalTicks);

    public static bool IsPatientNeedingRinse(Pawn pawn) =>
        Patients.IsLyingPatient(pawn) && Patients.CareAllows(pawn, MedicalCareCategory.NoMeds, requireMedicine: false) && NeedsRinse(pawn);

    /// <summary>The nearest usable station this pawn can walk to: for themselves (within a run) or to fetch water for a patient.</summary>
    public static Thing? ClosestStation(Pawn walker, bool forSelf)
    {
        Thing? best = null;
        float bestDistance = float.MaxValue;
        foreach (ThingDef def in StationDefs)
        {
            Thing? found = GenClosest.ClosestThingReachable(walker.Position, walker.Map, ThingRequest.ForDef(def),
                forSelf ? PathEndMode.OnCell : PathEndMode.Touch, TraverseParms.For(walker), forSelf ? MaxRunDistance : MaxFetchDistance,
                thing => Usable(thing, walker));
            if (found == null) continue;
            float distance = (found.Position - walker.Position).LengthHorizontalSquared;
            if (distance >= bestDistance) continue;
            best = found;
            bestDistance = distance;
        }
        return best;
    }

    static bool Usable(Thing station, Pawn walker) =>
        !station.IsForbidden(walker) && walker.CanReserve(station) && WaterSource(station)?.LooksDry != true;

    public static Job? SelfRinseJob(Pawn pawn)
    {
        if (!NeedsRinse(pawn) || pawn.CurJobDef == MedicalDefOf.PhobosMedical_Rinse || PawnUtility.PlayerForcedJobNowOrSoon(pawn)) return null;
        Thing? station = ClosestStation(pawn, forSelf: true);
        return station == null ? null : JobMaker.MakeJob(MedicalDefOf.PhobosMedical_Rinse, station);
    }

    public static Job? RinsePatientJob(Pawn doctor, Pawn patient)
    {
        if (patient == doctor || !IsPatientNeedingRinse(patient) || !doctor.CanReserve(patient)) return null;
        Thing? station = ClosestStation(doctor, forSelf: false);
        if (station == null) return null;
        Job job = JobMaker.MakeJob(MedicalDefOf.PhobosMedical_RinsePatient, station, patient);
        job.count = 1;
        return job;
    }

    public static int RinseTicks(Thing station) => station.TryGetComp<CompPhobosRinseStation>()?.Props.rinseTicks ?? 625;
    public static int FillTicks(Thing station) => station.TryGetComp<CompPhobosRinseStation>()?.Props.fillTicks ?? 120;

    /// <summary>Draws the water for one rinse of <paramref name="rinsed"/>. Says why when there is none.</summary>
    public static bool TryDrawWater(Thing station, Pawn rinsed)
    {
        CompProperties_PhobosRinseStation? props = station.TryGetComp<CompPhobosRinseStation>()?.Props;
        IPhobosWaterSource? source = WaterSource(station);
        if (props == null || source == null) return true;
        float litres = ChemicalRule.RinseLitres(ChemicalService.OnEyesOnly(rinsed), props.showerLitres, props.eyewashLitres);
        if (source.TryDraw(litres, out string reason)) return true;
        Messages.Message("PhobosMedical_RinseNoWater".Translate(rinsed.LabelShort.Named("PAWN"), reason.Named("REASON")), station, MessageTypeDefOf.NegativeEvent);
        return false;
    }

    /// <summary>The rinse ends: with strip and rinse on, contaminated clothes come off first; then the skin is washed.</summary>
    public static void Finish(Pawn rinsed)
    {
        if (BurnsAndSmokeMod.Settings.causticStripAndRinse) CausticService.StripContaminated(rinsed);
        ChemicalService.Rinse(rinsed);
    }

    static void Rebuild(Map map, List<Pawn> list)
    {
        IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
        for (int i = 0; i < pawns.Count; i++)
        {
            if (IsPatientNeedingRinse(pawns[i])) list.Add(pawns[i]);
        }
    }
}
