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

    /// <summary>Every item that is a patient device, in the order a doctor should reach for them: a tube first (it is quick and only
    /// offered when an airway is closing or a ventilator waits for it), then whoever can't breathe, then the rest.</summary>
    public static IReadOnlyList<ThingDef> Devices => devices ??= DefDatabase<ThingDef>.AllDefsListForReading
        .Where(def => def.HasModExtension<PhobosPatientDeviceExtension>())
        .OrderBy(def => Order(def.GetModExtension<PhobosPatientDeviceExtension>().need))
        .ToList();

    static int Order(DeviceNeed need) => need switch
    {
        DeviceNeed.Intubation => 0,
        DeviceNeed.BreathingAssist => 1,
        _ => 2,
    };

    public static PhobosPatientDeviceExtension Extension(ThingDef device) => device.GetModExtension<PhobosPatientDeviceExtension>();

    /// <summary>The map's patients who need some device now, rebuilt at most every few seconds.</summary>
    public static IReadOnlyList<Pawn> PatientsNeedingDevices(Map map) => patients.Get(map, CacheIntervalTicks);

    /// <summary>Someone the doctors look after, lying down (in bed or downed), whose care setting lets doctors act.</summary>
    public static bool IsPatient(Pawn pawn, PhobosPatientDeviceExtension device) =>
        Patients.IsLyingPatient(pawn) && Patients.CareAllows(pawn, MedicalCareCategory.NoMeds, requireMedicine: device.consumed);

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
            DeviceNeed.Decompression => DeviceRule.Decompression(PressureRelief.Any(patient)),
            DeviceNeed.Intubation => DeviceRule.Intubation(AirwayTubes.HasTube(patient),
                AirwayTubes.Closing(patient, breathing, rules.breathingAssistThreshold), VentilatorService.WaitingForTube(patient)),
            _ => DeviceRefusal.NotNeeded,
        };
    }

    public static bool Needs(Pawn patient, PhobosPatientDeviceExtension device) =>
        IsPatient(patient, device) && Refusal(patient, device) == DeviceRefusal.None;

    /// <summary>The job for the first device this patient needs that the doctor can reach, or null. With <paramref name="explain"/>, a
    /// refusal worth knowing is given as the reason the right-click menu shows: only for a real patient (never a vehicle or anyone
    /// standing), and never "breathes well enough", which would follow every right-click on a healthy person (owner's report, 7 Nov 2026).</summary>
    public static Job? MakeJob(Pawn doctor, Pawn patient, bool explain = false)
    {
        DeviceRefusal reason = DeviceRefusal.NotNeeded;
        int skillNeeded = 0;
        bool anyPatient = false;
        foreach (ThingDef def in Devices)
        {
            PhobosPatientDeviceExtension device = Extension(def);
            if (!IsPatient(patient, device)) continue;
            anyPatient = true;
            DeviceRefusal refusal = Refusal(patient, device);
            if (refusal != DeviceRefusal.None)
            {
                reason = DeviceRule.MostUrgent(reason, refusal);
                continue;
            }
            if (device.minMedicineSkill > 0 && (doctor.skills?.GetSkill(SkillDefOf.Medicine)?.Level ?? 0) < device.minMedicineSkill)
            {
                reason = DeviceRule.MostUrgent(reason, DeviceRefusal.NeedsSkill);
                skillNeeded = device.minMedicineSkill;
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
        if (explain && anyPatient && DeviceRule.WorthTelling(reason))
            JobFailReason.Is(("PhobosMedical_DeviceRefusal" + reason).Translate(patient.LabelShort.Named("PATIENT"), skillNeeded.Named("SKILL")));
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

    /// <summary>Gives or keeps up the device's treatment. The condition sets its own effect on breathing; a tube also bypasses the
    /// upper-airway conditions it gets past.</summary>
    public static void Apply(Pawn patient, PhobosPatientDeviceExtension device)
    {
        MedicalTreatment.GiveOrRefresh(patient, device.treatment);
        if (device.need == DeviceNeed.Intubation) AirwayTubes.RemoveBypassed(patient);
        if (device.need == DeviceNeed.Decompression) PressureRelief.Relieve(patient);
    }

    /// <summary>Breathing without what a hand pump (or any breathing treatment) already gives, so a doctor doesn't stop pumping because
    /// their own pumping helps.</summary>
    static float BreathingWithoutAssist(Pawn patient) => Hediff_PhobosOnOxygen.BreathingWithoutTreatments(patient);

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
