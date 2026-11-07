using System.Collections.Generic;
using Phobos.Medical.Defs;
using Phobos.Medical.Services;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Medical.Jobs;

/// <summary>
/// Take one device to the patient and use it. A kit is fitted once and used up; a sustained device (a hand pump) is worked for as
/// long as the patient needs it, up to one turn, then kept. Target A is the device (in the doctor's inventory or on the map), B the patient.
/// </summary>
public class JobDriver_PhobosUseDevice : JobDriver
{
    const TargetIndex DeviceInd = TargetIndex.A;
    const TargetIndex PatientInd = TargetIndex.B;

    Thing Device => job.targetA.Thing;
    Pawn Patient => job.targetB.Pawn;
    PhobosPatientDeviceExtension Extension => DeviceService.Extension(Device.def);

    int ticksSinceRefresh;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ticksSinceRefresh, "PhobosMedical_TicksSinceRefresh");
    }

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (!pawn.Reserve(Patient, job, 1, -1, null, errorOnFailed)) return false;
        if (pawn.inventory == null || !pawn.inventory.Contains(Device))
        {
            if (!pawn.Reserve(Device, job, 1, 1, null, errorOnFailed)) return false;
        }
        return true;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(PatientInd);
        this.FailOn(() => Patient.Dead);
        Toil fromInventory = Toils_Misc.TakeItemFromInventoryToCarrier(pawn, DeviceInd);
        Toil toPatient = Toils_Goto.GotoThing(PatientInd, PathEndMode.Touch);
        yield return Toils_Jump.JumpIf(fromInventory, () => pawn.inventory != null && pawn.inventory.Contains(Device));
        yield return Toils_Goto.GotoThing(DeviceInd, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(DeviceInd);
        yield return Toils_Haul.StartCarryThing(DeviceInd, subtractNumTakenFromJobCount: true);
        yield return Toils_Jump.Jump(toPatient);
        yield return fromInventory;
        yield return toPatient;

        Toil use = Toils_General.Wait(Extension.workTicks, PatientInd).WithProgressBarToilDelay(PatientInd).FailOnCannotTouch(PatientInd, PathEndMode.Touch);
        // A kit is checked once, before it is fitted; a pump stops as soon as the patient no longer needs it.
        use.AddFailCondition(() => !Extension.sustained && !DeviceService.Needs(Patient, Extension));
        if (Extension.sustained)
        {
            // The game's wait already stops the doctor and turns them to the patient: keep both, and add to them.
            System.Action? stop = use.initAction;
            System.Action<int>? face = use.tickIntervalAction;
            use.initAction = () =>
            {
                stop?.Invoke();
                ticksSinceRefresh = 0;
                DeviceService.Apply(Patient, Extension);
            };
            use.tickIntervalAction = delta =>
            {
                face?.Invoke(delta);
                ticksSinceRefresh += delta;
                if (ticksSinceRefresh < Extension.refreshTicks) return;
                ticksSinceRefresh = 0;
                if (!DeviceService.Needs(Patient, Extension))
                {
                    ReadyForNextToil();
                    return;
                }
                DeviceService.Apply(Patient, Extension);
            };
        }
        use.activeSkill = () => SkillDefOf.Medicine;
        yield return use;

        yield return Toils_General.Do(() =>
        {
            if (!Extension.sustained) DeviceService.Apply(Patient, Extension);
            Thing? carried = pawn.carryTracker.CarriedThing;
            if (carried == null) return;
            if (Extension.consumed)
            {
                carried.SplitOff(1).Destroy();
            }
            else
            {
                pawn.carryTracker.innerContainer.TryTransferToContainer(carried, pawn.inventory.innerContainer);
            }
        });
    }
}
