using System.Collections.Generic;
using LudeonTK;
using Phobos.Radio.Services;
using Vehicles;
using Verse;

namespace Phobos.Radio.Vehicles;

/// <summary>
/// On a vehicle's def: the radio it can carry, and the upgrade that puts it in place (Radio 0.10.0; the autonomous vehicles record,
/// step 2). With no upgrade named, the radio is always in place. D'thorga Vehicles names it on its service trucks, with the remote
/// control kit's node, in a later step; another author's vehicle may name it the same way.
/// </summary>
public class VehicleRadioFitting : DefModExtension
{
    public RadioFittingDef? fitting;
    /// <summary>The key of the vehicle's upgrade node that fits the radio; empty for a radio that is always fitted.</summary>
    public string? upgradeNode;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors()) yield return error;
        if (fitting == null) yield return "VehicleRadioFitting needs a fitting";
    }
}

/// <summary>
/// The tie-in's start-up: each vehicle def that names a radio fitting gets that radio's comp, so every vehicle of it is built with
/// one; and the rule that tells the net what a vehicle's radio does.
/// <para>Cost: once at start-up, one pass over the vehicle defs. Nothing runs per tick.</para>
/// </summary>
[StaticConstructorOnStartup]
public static class VehicleRadios
{
    static VehicleRadios()
    {
        CarriedRadios.Register(new VehicleRadioRule());
        foreach (var def in DefDatabase<VehicleDef>.AllDefsListForReading)
        {
            var radio = def.GetModExtension<VehicleRadioFitting>()?.fitting?.radio;
            if (radio == null || def.comps.Exists(comp => comp is CompProperties_Radio)) continue;
            def.comps.Add(radio);
        }
    }

    /// <summary>Whether the vehicle's upgrade that fits its radio is done, or none is named.</summary>
    public static bool Fitted(VehiclePawn vehicle)
    {
        var key = vehicle.VehicleDef.GetModExtension<VehicleRadioFitting>()?.upgradeNode;
        if (string.IsNullOrEmpty(key)) return true;
        var tree = vehicle.CompUpgradeTree;
        return tree?.Props?.def?.GetNode(key) is { } node && tree.NodeUnlocked(node);
    }

    /// <summary>
    /// A test: fits the vehicle control radio to the selected vehicles of the colony until the game is loaded again, so its links can
    /// be seen before any vehicle carries the kit. It changes nothing that is saved.
    /// </summary>
    [DebugAction("Phobos Radio", "Fit a control radio to selected vehicles (until loaded again)", actionType = DebugActionType.Action,
        allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void FitForSession()
    {
        var fitting = DefDatabase<RadioFittingDef>.GetNamedSilentFail("PhobosRadio_FittingVehicleControl");
        if (fitting?.radio == null)
        {
            Log.Warning("[PhobosRadio] The vehicle control radio fitting is missing.");
            return;
        }
        var fitted = 0;
        var skipped = 0;
        foreach (var selected in Find.Selector.SelectedObjects)
        {
            if (selected is not VehiclePawn vehicle) continue;
            if (CarriedRadios.FitForSession(vehicle, fitting.radio) != null) fitted++;
            else skipped++;
        }
        Log.Message(fitted + skipped == 0
            ? "[PhobosRadio] Select one or more vehicles first."
            : $"[PhobosRadio] Fitted a control radio to {fitted} vehicle(s); {skipped} already had a radio.");
    }
}

/// <summary>
/// What a vehicle's radio does on the net. Until a vehicle can be driven from a station (the record's step 3), a fitted radio stands
/// by: its links are worked out and shown, and it asks no channel. A vehicle whose upgrade is not done is off the net.
/// </summary>
public sealed class VehicleRadioRule : ICarriedRadioRule
{
    public CarriedRadioUse? UseOf(Pawn carrier, CompRadio radio)
    {
        if (carrier is not VehiclePawn vehicle) return null;
        return VehicleRadios.Fitted(vehicle) ? CarriedRadioUse.StandingBy : CarriedRadioUse.Off;
    }
}
