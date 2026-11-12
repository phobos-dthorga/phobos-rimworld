using System.Collections.Generic;
using RimWorld;
using UnityEngine;
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
                else ChooseWay(set);
            },
        };
        string? refused = on ? null : set.CannotDeploy();
        if (refused != null) command.Disable(refused);
        return command;
    }

    /// <summary>
    /// The battery drive (Framework 0.104.0, the hybrid): a switch to move the set on its battery buffer with the engine off, quiet and slow.
    /// Disabled while it is set up, with the reason. An empty tank needs no switch: the set creeps on the battery by itself.
    /// </summary>
    public static Gizmo BatteryDriveButton(CompPhobosGeneratorSet set)
    {
        var command = new Command_Toggle
        {
            defaultLabel = "PhobosVehicleSystems_BatteryDriveGizmo".Translate().Resolve(),
            defaultDesc = "PhobosVehicleSystems_BatteryDriveGizmoDesc".Translate(set.CellsLeft.Named("CELLS")).Resolve(),
            icon = SystemIcons.For("Phobos/VehicleSystems/Dash/Battery"),
            isActive = () => set.BatteryDriveOn,
            toggleAction = () => set.SetBatteryDrive(!set.BatteryDriveOn),
        };
        if (set.Deployed) command.Disable("PhobosVehicleSystems_BatteryDriveSetUp".Translate().Resolve());
        return command;
    }

    /// <summary>The service lead (Framework 0.101.0): run it to one of our vehicles in reach, or take it in. Disabled with the reason.</summary>
    public static Gizmo LeadButton(CompPhobosGeneratorSet set)
    {
        bool connected = set.LeadTo != null;
        var command = new Command_Action
        {
            defaultLabel = connected ? "PhobosVehicleSystems_LeadGizmoTakeIn".Translate().Resolve() : "PhobosVehicleSystems_LeadGizmo".Translate().Resolve(),
            defaultDesc = "PhobosVehicleSystems_LeadGizmoDesc".Translate(set.LeadReach.Named("CELLS")).Resolve(),
            icon = SystemIcons.For("Phobos/VehicleSystems/ServiceLead"),
            action = () =>
            {
                if (connected)
                {
                    set.TakeInLead(null);
                    return;
                }
                var parameters = new TargetingParameters { canTargetPawns = true, canTargetBuildings = false, canTargetItems = false, validator = t => t.Thing is Vehicles.VehiclePawn };
                Find.Targeter.BeginTargeting(parameters, target =>
                {
                    if (target.Thing is Vehicles.VehiclePawn vehicle) set.RunLead(vehicle);
                });
            },
        };
        string? refused = connected ? null : set.LeadRefusal();
        if (refused != null) command.Disable(refused);
        return command;
    }

    /// <summary>
    /// How to join the grid (Framework 0.107.0; owner, 14 Nov 2026): run the cable end out to a cell, or join through the game's power conduits laid beneath the set.
    /// A choice that cannot be made now stays in the menu, greyed, with its reason.
    /// </summary>
    static void ChooseWay(CompPhobosGeneratorSet set)
    {
        var options = new List<FloatMenuOption>
        {
            new("PhobosVehicleSystems_GenWayCable".Translate().Resolve(), () =>
            {
                if (set.CableReach > 1) ChooseCell(set);
                else set.SetUp(set.BestCableCell());
            }, SystemIcons.For(Icons + "SetUp"), Color.white),
        };
        string label = "PhobosVehicleSystems_GenWayBeneath".Translate().Resolve();
        Texture2D conduit = SystemIcons.For(ConduitIcon);
        options.Add(set.CannotSetUpBeneath() is { } refused
            ? new FloatMenuOption(label + ": " + refused, null, conduit, Color.white)
            : new FloatMenuOption(label, set.SetUpBeneath, conduit, Color.white));
        Find.WindowStack.Add(new FloatMenu(options));
    }

    /// <summary>The game's own power conduit picture, as its build menu shows it.</summary>
    const string ConduitIcon = "Things/Building/Linked/PowerConduit_MenuIcon";

    /// <summary>With a cable reel the player picks where the cable end goes; the cells it reaches are outlined.</summary>
    static void ChooseCell(CompPhobosGeneratorSet set)
    {
        List<IntVec3> cells = set.CableCells();
        var parameters = new TargetingParameters { canTargetLocations = true, canTargetPawns = false, canTargetBuildings = false, canTargetItems = false };
        Find.Targeter.BeginTargeting(parameters, target => set.SetUp(target.Cell), _ => GenDraw.DrawFieldEdges(cells), target => set.CanPlaceCableAt(target.Cell));
    }
}
