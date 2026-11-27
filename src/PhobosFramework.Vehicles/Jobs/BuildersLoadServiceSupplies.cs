using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

/// <summary>
/// Builders bring the supplies for a vehicle's fittings, tunes, repairs and services (Framework 0.340.2;
/// docs/development/systems/upgrades-and-fittings.md, "Upgrade materials"). Our own supply work giver, offered under Construction by
/// PhobosVehicleSystems_BuildersLoadServiceSupplies as well as under Hauling, so a pawn with Construction but no Hauling (the Dead Man's
/// Switch uca) no longer waits beside a fitting order for a hauler: the same gap Framework 0.302.1 closed for Vehicle Framework's
/// upgrades. It adds only a seen-in-play note (test row 670).
/// </summary>
public class WorkGiver_PhobosBuildersLoadServiceSupplies : WorkGiver_PhobosLoadServiceSupplies
{
    public override Job? JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        Job? job = base.JobOnThing(pawn, t, forced);
        if (job != null && Seen.On)
        {
            // Which kind of pawn took it: one that cannot haul (the uca) is the case this exists for.
            string variant = pawn.WorkTypeIsDisabled(WorkTypeDefOf.Hauling) ? "non-hauler" : "builder";
            if (Seen.Wants("vehicles.builders-bring-supplies", t, variant))
                Seen.Note("vehicles.builders-bring-supplies", t, what: pawn.LabelShort, variant: variant);
        }
        return job;
    }
}
