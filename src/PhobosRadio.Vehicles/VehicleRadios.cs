using System.Collections.Generic;
using LudeonTK;
using Phobos.Framework;
using Phobos.Radio.Services;
using Phobos.Radio.UI;
using Phobos.VehicleSystems;
using UnityEngine;
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
/// The tie-in's start-up: each vehicle def that names a radio fitting gets that radio's comp, and D'thorga Framework's remote control
/// (Radio 0.11.0), so every vehicle of it is built with both; the rule that tells the net what a vehicle's radio does; and the radio as
/// a source the remote control reads its links from.
/// <para>Cost: once at start-up, one pass over the vehicle defs. Nothing runs per tick.</para>
/// </summary>
[StaticConstructorOnStartup]
public static class VehicleRadios
{
    static VehicleRadios()
    {
        CarriedRadios.Register(new VehicleRadioRule());
        RemoteControl.Register(new RadioRemoteControl());
        foreach (var def in DefDatabase<VehicleDef>.AllDefsListForReading)
        {
            var radio = def.GetModExtension<VehicleRadioFitting>()?.fitting?.radio;
            if (radio == null) continue;
            if (!def.comps.Exists(comp => comp is CompProperties_Radio)) def.comps.Add(radio);
            if (!def.comps.Exists(comp => comp is CompProperties_PhobosRemoteControl)) def.comps.Add(new CompProperties_PhobosRemoteControl());
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
            // The remote control comes with it (Radio 0.11.0), so the vehicle can be driven from a station in the test too.
            SessionComps.Add(vehicle, new CompProperties_PhobosRemoteControl());
            if (CarriedRadios.FitForSession(vehicle, fitting.radio) != null) fitted++;
            else skipped++;
        }
        Log.Message(fitted + skipped == 0
            ? "[PhobosRadio] Select one or more vehicles first."
            : $"[PhobosRadio] Fitted a control radio to {fitted} vehicle(s); {skipped} already had a radio.");
    }
}

/// <summary>
/// What a vehicle's radio does on the net. A fitted radio stands by (its links worked out and shown, no channel asked) until the player
/// asks for the vehicle to be driven from a station; then it is in use, and asks a channel of its kind (Radio 0.11.0). A vehicle whose
/// upgrade is not done is off the net.
/// </summary>
public sealed class VehicleRadioRule : ICarriedRadioRule
{
    public CarriedRadioUse? UseOf(Pawn carrier, CompRadio radio)
    {
        if (carrier is not VehiclePawn vehicle) return null;
        if (!VehicleRadios.Fitted(vehicle)) return CarriedRadioUse.Off;
        return RemoteControl.Wanted(vehicle) ? CarriedRadioUse.InUse : CarriedRadioUse.StandingBy;
    }
}

/// <summary>
/// The radio as what drives a vehicle from afar (Radio 0.11.0; the autonomous vehicles record, step 3): a vehicle is linked while a
/// station serves its radio on a channel, and comes back toward that station. It reads the net's last pass; nothing is worked out here
/// but a dictionary lookup.
/// </summary>
public sealed class RadioRemoteControl : IRemoteControlSource
{
    public bool Fitted(VehiclePawn vehicle) => vehicle.GetComp<CompRadio>() != null && VehicleRadios.Fitted(vehicle);

    public RemoteLink LinkOf(VehiclePawn vehicle)
    {
        if (!vehicle.Spawned) return default;
        var station = RadioNet.For(vehicle.Map)?.ServedBy(vehicle);
        return station == null ? default : new RemoteLink(true, station.Position);
    }

    public void WantChanged(VehiclePawn vehicle)
    {
        if (vehicle.Spawned) RadioNet.For(vehicle.Map)?.MarkDirty();
    }

    public Texture2D? Icon => RadioTextures.Signal;
}
