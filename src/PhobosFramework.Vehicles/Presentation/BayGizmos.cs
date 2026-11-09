using System.Collections.Generic;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>The depot bay's buttons. Presentation only: each reads the service and calls it.</summary>
public static class BayGizmos
{
    const string Icons = "Phobos/VehicleSystems/Stations/";

    public static Gizmo AssignButton(CompPhobosDepotBay bay) => new Command_Action
    {
        defaultLabel = "PhobosVehicleSystems_BayGizmoAssign".Translate().Resolve(),
        defaultDesc = "PhobosVehicleSystems_BayGizmoAssignDesc".Translate().Resolve(),
        icon = SystemIcons.For(Icons + "BayAssign"),
        action = () =>
        {
            var options = new List<FloatMenuOption> { new("PhobosVehicleSystems_BayAssignNone".Translate().Resolve(), () => DepotBays.Assign(bay, null)) };
            foreach (VehiclePawn vehicle in DepotBays.Candidates(bay))
            {
                VehiclePawn captured = vehicle;
                options.Add(new FloatMenuOption(captured.LabelShortCap, () => DepotBays.Assign(bay, captured)));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        },
    };

    public static Gizmo GoButton(CompPhobosVehicleSystems comp, CompPhobosDepotBay bay)
    {
        var command = new Command_Action
        {
            defaultLabel = "PhobosVehicleSystems_BayGizmoGo".Translate().Resolve(),
            defaultDesc = "PhobosVehicleSystems_BayGizmoGoDesc".Translate().Resolve(),
            icon = SystemIcons.For(Icons + "BayGoTo"),
            action = () => DepotBays.GoTo(comp, bay),
        };
        string? refused = DepotBays.CannotGo(comp.Vehicle, bay);
        if (refused != null) command.Disable(refused);
        return command;
    }
}

/// <summary>Outlines a depot bay's parking pad, while it is being placed and while it is selected.</summary>
public sealed class PlaceWorker_PhobosDepotBayPad : PlaceWorker
{
    public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, UnityEngine.Color ghostCol, Thing? thing = null)
    {
        CompProperties_PhobosDepotBay? props = def.GetCompProperties<CompProperties_PhobosDepotBay>();
        if (props != null) GenDraw.DrawFieldEdges(new List<IntVec3>(CompPhobosDepotBay.PadFor(center, rot, props).ClipInsideMap(Find.CurrentMap).Cells));
    }
}
