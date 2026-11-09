using System.Linq;
using UnityEngine;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>Outlines how far a tuning rig reaches, while it is being placed and while it is selected.</summary>
public sealed class PlaceWorker_PhobosTuningRigReach : PlaceWorker
{
    public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing? thing = null)
    {
        int reach = def.GetCompProperties<CompProperties_PhobosTuningRig>()?.reach ?? 0;
        GenDraw.DrawFieldEdges(GenAdj.OccupiedRect(center, rot, def.size).ExpandedBy(reach).ClipInsideMap(Find.CurrentMap).Cells.ToList());
    }
}
