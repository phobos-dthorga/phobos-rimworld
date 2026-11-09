using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>The generator truck's button. Presentation only: it reads the comp and calls one of its service methods.</summary>
public static class GeneratorGizmos
{
    const string Icons = "Phobos/VehicleSystems/Tank/";

    public static Gizmo SetUpButton(CompPhobosGeneratorSet set)
    {
        bool on = set.Deployed;
        var command = new Command_Action
        {
            defaultLabel = on ? "PhobosVehicleSystems_GenGizmoPackUp".Translate().Resolve() : "PhobosVehicleSystems_GenGizmoSetUp".Translate().Resolve(),
            defaultDesc = on ? "PhobosVehicleSystems_GenGizmoPackUpDesc".Translate().Resolve() : "PhobosVehicleSystems_GenGizmoSetUpDesc".Translate().Resolve(),
            icon = SystemIcons.For(Icons + (on ? "PackUp" : "SetUp")),
            action = () =>
            {
                if (on) set.PackUp();
                else if (set.CableReach > 1) ChooseCell(set);
                else set.SetUp(set.BestCableCell());
            },
        };
        string? refused = on ? null : set.CannotDeploy();
        if (refused != null) command.Disable(refused);
        return command;
    }

    /// <summary>With a cable reel the player picks where the cable end goes; the cells it reaches are outlined.</summary>
    static void ChooseCell(CompPhobosGeneratorSet set)
    {
        List<IntVec3> cells = set.CableCells();
        var parameters = new TargetingParameters { canTargetLocations = true, canTargetPawns = false, canTargetBuildings = false, canTargetItems = false };
        Find.Targeter.BeginTargeting(parameters, target => set.SetUp(target.Cell), _ => GenDraw.DrawFieldEdges(cells), target => set.CanPlaceCableAt(target.Cell));
    }
}
