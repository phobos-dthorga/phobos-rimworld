using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>The working vehicles' buttons. Presentation only: each reads the comp and calls one of its service methods.</summary>
public static class RigGizmos
{
    const string TankIcons = "Phobos/VehicleSystems/Tank/";
    const string Icons = "Phobos/VehicleSystems/Stations/";

    public static Gizmo SetUpButton(CompPhobosRigWork work)
    {
        bool on = work.Deployed;
        var command = new Command_Action
        {
            defaultLabel = on ? "PhobosVehicleSystems_RigGizmoPackUp".Translate().Resolve() : "PhobosVehicleSystems_RigGizmoSetUp".Translate().Resolve(),
            defaultDesc = on ? work.PackUpDescription : work.SetUpDescription,
            icon = SystemIcons.For(TankIcons + (on ? "PackUp" : "SetUp")),
            action = () =>
            {
                if (on) work.PackUp();
                else work.SetUp();
            },
        };
        string? refused = on ? null : work.CannotDeploy();
        if (refused != null) command.Disable(refused);
        return command;
    }

    public static Gizmo LongRangeToggle(CompPhobosSurveyTruck survey) => new Command_Toggle
    {
        defaultLabel = "PhobosVehicleSystems_SurveyGizmoLongRange".Translate().Resolve(),
        defaultDesc = "PhobosVehicleSystems_SurveyGizmoLongRangeDesc".Translate().Resolve(),
        icon = SystemIcons.For(Icons + "SurveyLongRange"),
        isActive = () => survey.LongRange,
        toggleAction = () => survey.SetLongRange(!survey.LongRange),
    };

    public static Gizmo TargetButton(CompPhobosSurveyTruck survey)
    {
        ThingDef mineable = survey.Target.building.mineableThing;
        return new Command_Action
        {
            defaultLabel = "PhobosVehicleSystems_SurveyGizmoTarget".Translate(mineable.LabelCap.Named("ORE")).Resolve(),
            defaultDesc = "PhobosVehicleSystems_SurveyGizmoTargetDesc".Translate().Resolve(),
            icon = mineable.uiIcon,
            action = () =>
            {
                var options = new List<FloatMenuOption>();
                foreach (ThingDef candidate in ((GenStep_PreciousLump)GenStepDefOf.PreciousLump.genStep).mineables)
                {
                    ThingDef captured = candidate;
                    options.Add(new FloatMenuOption(captured.building.mineableThing.LabelCap, () => survey.SetTarget(captured)));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            },
        };
    }
}
