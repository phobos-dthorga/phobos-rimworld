using System.Collections.Generic;
using Phobos.Dosing.Defs;
using Phobos.Dosing.Services;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Dosing.Jobs;

/// <summary>
/// Carry one dose to the patient, who takes it: the game's own feed-a-patient toils, so the item's outcomes apply exactly as
/// its mod wrote them. Target A is the item (in the doctor's inventory or on the map), B the patient.
/// </summary>
public class JobDriver_PhobosDose : JobDriver
{
    const TargetIndex ItemInd = TargetIndex.A;
    const TargetIndex PatientInd = TargetIndex.B;

    Thing Item => job.targetA.Thing;
    Pawn Patient => job.targetB.Pawn;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (!pawn.Reserve(Patient, job, 1, -1, null, errorOnFailed)) return false;
        if (pawn.inventory == null || !pawn.inventory.Contains(Item))
        {
            if (!pawn.Reserve(Item, job, 10, 1, null, errorOnFailed)) return false;
        }
        job.count = 1;
        return true;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(PatientInd);
        this.FailOn(() => Patient.Dead || !DoseService.NeedsDose(Patient, DosingMod.Settings, PhobosDosingRulesDef.Current));
        Toil fromInventory = Toils_Misc.TakeItemFromInventoryToCarrier(pawn, ItemInd);
        Toil toPatient = Toils_Goto.GotoThing(PatientInd, PathEndMode.Touch);
        yield return Toils_Jump.JumpIf(fromInventory, () => pawn.inventory != null && pawn.inventory.Contains(TargetThingA));
        yield return Toils_Goto.GotoThing(ItemInd, PathEndMode.ClosestTouch).FailOnForbidden(ItemInd);
        yield return Toils_Ingest.PickupIngestible(ItemInd, Patient);
        yield return Toils_Jump.Jump(toPatient);
        yield return fromInventory;
        yield return toPatient;
        yield return Toils_Ingest.ChewIngestible(Patient, 1f, ItemInd).FailOnCannotTouch(PatientInd, PathEndMode.Touch);
        yield return Toils_Ingest.FinalizeIngest(Patient, ItemInd);
    }
}
