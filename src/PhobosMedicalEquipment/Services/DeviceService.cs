using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Breathing;
using Phobos.Framework.Defs;
using Phobos.Framework.Health;
using Phobos.Medical.Comps;
using Phobos.Medical.Defs;
using Phobos.Medical.Rules;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Medical.Services;

/// <summary>
/// Decides which hand-held device a patient needs and does its effect. The work giver and the job driver only call this. Devices
/// are indexed once from their Defs' <see cref="PhobosPatientDeviceExtension"/>, so any mod can add one in XML.
/// </summary>
public static class DeviceService
{
    const int CacheIntervalTicks = 250;

    static List<ThingDef>? devices;
    static readonly MapPatientCache<Pawn> patients = new(Rebuild);

    /// <summary>Every item that is a patient device, in the order a doctor should reach for them: whoever can't breathe comes first.</summary>
    public static IReadOnlyList<ThingDef> Devices => devices ??= DefDatabase<ThingDef>.AllDefsListForReading
        .Where(def => def.HasModExtension<PhobosPatientDeviceExtension>())
        .OrderBy(def => def.GetModExtension<PhobosPatientDeviceExtension>().need == DeviceNeed.BreathingAssist ? 0 : 1)
        .ToList();

    public static PhobosPatientDeviceExtension Extension(ThingDef device) => device.GetModExtension<PhobosPatientDeviceExtension>();

    /// <summary>The map's patients who need some device now, rebuilt at most every few seconds.</summary>
    public static IReadOnlyList<Pawn> PatientsNeedingDevices(Map map) => patients.Get(map, CacheIntervalTicks);

    /// <summary>Someone the doctors look after, lying down (in bed or downed), whose care setting lets doctors act.</summary>
    public static bool IsPatient(Pawn pawn, PhobosPatientDeviceExtension device) =>
        Patients.IsPlayersPerson(pawn) && pawn.RaceProps.IsFlesh && (pawn.Downed || pawn.InBed())
        && Patients.CareAllows(pawn, MedicalCareCategory.NoMeds, requireMedicine: device.consumed);

    /// <summary>Whether this device would help this patient now, and if not, why not.</summary>
    public static DeviceRefusal Refusal(Pawn patient, PhobosPatientDeviceExtension device)
    {
        var rules = PhobosRulesDef.Current<PhobosMedicalRulesDef>();
        float breathing = OxygenService.Breathing(patient);
        return device.need switch
        {
            DeviceNeed.SuppliedOxygen => DeviceRule.FieldOxygen(OxygenService.NeedsOxygen(patient, rules),
                PhobosBreathing.SuppliedCarbonMonoxideHalfLifeHours(patient) != null, breathing),
            DeviceNeed.BreathingAssist => DeviceRule.BreathingAssist(BreathingWithoutAssist(patient), rules.breathingAssistThreshold),
            _ => DeviceRefusal.NotNeeded,
        };
    }

    public static bool Needs(Pawn patient, PhobosPatientDeviceExtension device) =>
        IsPatient(patient, device) && Refusal(patient, device) == DeviceRefusal.None;

    /// <summary>The job for the first device this patient needs that the doctor can reach, or null. With <paramref name="explain"/>, a
    /// refusal is given as the reason the right-click menu shows.</summary>
    public static Job? MakeJob(Pawn doctor, Pawn patient, bool explain = false)
    {
        DeviceRefusal reason = DeviceRefusal.NotNeeded;
        foreach (ThingDef def in Devices)
        {
            PhobosPatientDeviceExtension device = Extension(def);
            if (!IsPatient(patient, device)) continue;
            DeviceRefusal refusal = Refusal(patient, device);
            if (refusal != DeviceRefusal.None)
            {
                reason = DeviceRule.MostUrgent(reason, refusal);
                continue;
            }
            Thing? item = FindDevice(doctor, def);
            if (item == null)
            {
                reason = DeviceRule.MostUrgent(reason, DeviceRefusal.NoDevice);
                continue;
            }
            Job job = JobMaker.MakeJob(MedicalDefOf.PhobosMedical_UseDevice, item, patient);
            job.count = 1;
            return job;
        }
        if (explain) JobFailReason.Is(("PhobosMedical_DeviceRefusal" + reason).Translate(patient.LabelShort.Named("PATIENT")));
        return null;
    }

    /// <summary>One the doctor carries, else the nearest one they can reach and reserve.</summary>
    public static Thing? FindDevice(Pawn doctor, ThingDef def)
    {
        Thing? carried = doctor.inventory?.innerContainer.FirstOrDefault(thing => thing.def == def);
        if (carried != null) return carried;
        return GenClosest.ClosestThingReachable(doctor.Position, doctor.Map, ThingRequest.ForDef(def), PathEndMode.ClosestTouch,
            TraverseParms.For(doctor), 9999f, thing => !thing.IsForbidden(doctor) && doctor.CanReserve(thing));
    }

    /// <summary>Gives or keeps up the device's treatment. The condition sets its own effect on breathing.</summary>
    public static void Apply(Pawn patient, PhobosPatientDeviceExtension device) => MedicalTreatment.GiveOrRefresh(patient, device.treatment);

    /// <summary>Breathing without what a hand pump already gives, so a doctor doesn't stop pumping because their own pumping helps.</summary>
    static float BreathingWithoutAssist(Pawn patient)
    {
        List<Hediff> hediffs = patient.health.hediffSet.hediffs;
        for (int i = 0; i < hediffs.Count; i++)
        {
            if (hediffs[i] is Hediff_PhobosOnOxygen treatment) return treatment.BreathingWithoutOxygen();
        }
        return OxygenService.Breathing(patient);
    }

    static void Rebuild(Map map, List<Pawn> list)
    {
        if (Devices.Count == 0) return;
        IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
        for (int i = 0; i < pawns.Count; i++)
        {
            Pawn pawn = pawns[i];
            foreach (ThingDef def in Devices)
            {
                if (!Needs(pawn, Extension(def))) continue;
                list.Add(pawn);
                break;
            }
        }
    }
}
