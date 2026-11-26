using RimWorld;
using Vehicles;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

/// <summary>
/// Builders bring the materials for a vehicle's upgrade (Framework 0.302.1; docs/development/systems/upgrades-and-fittings.md, "Upgrade
/// materials"). Vehicle Framework's own loading work giver, offered under Construction by
/// PhobosFramework_BuildersLoadUpgradeMaterials as well as under Hauling by its own Def, so a pawn with Construction but no Hauling (the
/// Dead Man's Switch uca) no longer waits beside the vehicle for a hauler. It changes nothing in what Vehicle Framework offers; since
/// Framework 0.303.2 it only notes the job when seen in play (test row 546).
/// </summary>
public class WorkGiver_PhobosBuildersLoadUpgradeMaterials : WorkGiver_BringUpgradeMaterial
{
    public override Job? JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        Job? job = base.JobOnThing(pawn, t, forced);
        if (job != null && Seen.On)
        {
            // Which kind of pawn took it: one that cannot haul (the uca) is the case this exists for.
            string variant = pawn.WorkTypeIsDisabled(WorkTypeDefOf.Hauling) ? "non-hauler" : "builder";
            if (Seen.Wants("vehicles.builders-bring-materials", t, variant))
                Seen.Note("vehicles.builders-bring-materials", t, what: pawn.LabelShort, variant: variant);
        }
        return job;
    }
}
