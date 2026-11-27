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
/// (Radio 0.11.0), so every vehicle of it is built with both; since 0.15.0 (step 6; the owner's card of 25 Nov 2026, "a fitting for any
/// vehicle") so does every design D'thorga Framework gives a loadout, with the vehicle control radio, for the remote control kit fitting;
/// the rule that tells the net what a vehicle's radio does; and the radio as a source the remote control reads its links from.
/// <para>Cost: once at start-up, one pass over the vehicle defs. Nothing runs per tick. A design given the comps but not the kit costs the
/// net one rule check a pass for each of the colony's vehicles of it.</para>
/// </summary>
[StaticConstructorOnStartup]
public static class VehicleRadios
{
    static VehicleRadios()
    {
        CarriedRadios.Register(new VehicleRadioRule());
        RemoteControl.Register(new RadioRemoteControl());
        // Radio 0.21.0 (EM33): a convoy's reach home on the world map, asked by D'thorga Framework.
        ConvoyRadio.Register(new ConvoyReachSource());
        // Radio 0.22.0: halted convoys and vehicles parked at depots relay calls on.
        RelayNodes.Register(new VehicleRelaySource());
        // Radio 0.23.0: a convoy's reach home carries advice by radio to the people in it.
        RemoteReach.Register(new ConvoyRemoteReach());
        // Radio 0.18.0: the base turret follows D'thorga Framework's "Remote guns fire on their own", which lives in its Vehicle Framework part.
        RemoteTurrets.FireAloneSwitch = () => RemoteGuns.FireAlone;
        // Radio 0.14.0: the cover map for vehicles driven from afar works a little each tick, through the net's own tick.
        RadioNet.Ticked += RemoteCoverMap.Tick;
        var kitRadio = DefDatabase<RadioFittingDef>.GetNamedSilentFail(KitRadioName)?.radio;
        foreach (var def in DefDatabase<VehicleDef>.AllDefsListForReading)
        {
            // A design that names a fitting (our trucks), or one with a loadout of ours, which can take the kit fitting (Radio 0.15.0).
            var radio = def.GetModExtension<VehicleRadioFitting>()?.fitting?.radio
                        ?? (def.comps.Exists(comp => comp is CompProperties_PhobosVehicleSystems) ? kitRadio : null);
            if (radio == null) continue;
            if (!def.comps.Exists(comp => comp is CompProperties_Radio)) def.comps.Add(radio);
            if (!def.comps.Exists(comp => comp is CompProperties_PhobosRemoteControl)) def.comps.Add(new CompProperties_PhobosRemoteControl());
        }
    }

    /// <summary>The vehicle control radio, which the kit puts in place.</summary>
    public const string KitRadioName = "PhobosRadio_FittingVehicleControl";
    /// <summary>What the remote control kit fitting grants, and the electrical load that powers it (Radio 0.15.0; RemoteControlKit.xml).</summary>
    public const string KitGrant = "RemoteControl";
    public const string KitLoad = "remoteKit";

    /// <summary>Vehicles given a radio by the dev action for this session (never saved).</summary>
    private static readonly HashSet<int> fittedForSession = new();

    /// <summary>
    /// Whether the vehicle's radio is in place: the upgrade its design names is done (or it names none), or (Radio 0.15.0) the remote control
    /// kit fitting is on it with its part standing and its power on, or the dev action gave it one for this session.
    /// </summary>
    public static bool Fitted(VehiclePawn vehicle)
    {
        var extension = vehicle.VehicleDef.GetModExtension<VehicleRadioFitting>();
        if (extension != null)
        {
            var key = extension.upgradeNode;
            if (string.IsNullOrEmpty(key)) return true;
            var tree = vehicle.CompUpgradeTree;
            if (tree?.Props?.def?.GetNode(key) is { } node && tree.NodeUnlocked(node)) return true;
        }
        if (fittedForSession.Contains(vehicle.thingIDNumber)) return true;
        return vehicle.GetComp<CompPhobosVehicleSystems>() is { } systems && systems.Fittings.Grants(KitGrant) && systems.LoadActive(KitLoad);
    }

    /// <summary>
    /// As <see cref="Fitted"/>, working or not (Radio 0.16.1): the remote control kit counts while it is on the vehicle, its power shed or
    /// its part shot away, so the vehicle acts on a lost link and the player is told, rather than letting go without a word.
    /// </summary>
    /// <summary>What the repeater kit grants, and its load (Radio 0.22.0; RepeaterKit.xml).</summary>
    public const string RepeaterGrant = "RadioRepeater";
    public const string RepeaterLoad = "repeaterKit";

    /// <summary>Whether this vehicle's radio passes calls on between tiles: the repeater kit fitted and powered (the owner's card of 26 Nov
    /// 2026: a relay between tiles needs a repeater).</summary>
    public static bool Repeats(VehiclePawn vehicle) =>
        vehicle.GetComp<CompPhobosVehicleSystems>() is { } systems && systems.Fittings.Grants(RepeaterGrant) && systems.LoadActive(RepeaterLoad);

    /// <summary>What the Morse key grants (Radio 0.40.0; MorseKey.xml).</summary>
    public const string MorseGrant = "RadioMorse";

    /// <summary>Whether this vehicle can work Morse: its radio fitted and the Morse key on it.</summary>
    public static bool Keys(VehiclePawn vehicle) =>
        Fitted(vehicle) && vehicle.GetComp<CompPhobosVehicleSystems>() is { } systems && systems.Fittings.Grants(MorseGrant);

    public static bool Carried(VehiclePawn vehicle) =>
        Fitted(vehicle) || vehicle.GetComp<CompPhobosVehicleSystems>() is { } systems && systems.Fittings.Carries(KitGrant);

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
            // Since 0.15.0 most vehicles carry the radio already, waiting for the kit: the session counts as the kit.
            var had = vehicle.GetComp<CompRadio>() != null;
            if (!had) CarriedRadios.FitForSession(vehicle, fitting.radio);
            if (fittedForSession.Add(vehicle.thingIDNumber)) fitted++;
            else skipped++;
            RadioNet.For(vehicle.Map)?.MarkDirty();
        }
        Log.Message(fitted + skipped == 0
            ? "[PhobosRadio] Select one or more vehicles first."
            : $"[PhobosRadio] Fitted a control radio to {fitted} vehicle(s) for this session; {skipped} already had one.");
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
        if (!RemoteControl.Wanted(vehicle)) return CarriedRadioUse.StandingBy;
        // Driven drafted, it asks a picture too (Radio 0.15.0, step 6).
        return vehicle.GetComp<CompPhobosRemoteControl>() is { WantsPicture: true } ? CarriedRadioUse.InUseWithPicture : CarriedRadioUse.InUse;
    }
}

/// <summary>
/// The radio as what drives a vehicle from afar (Radio 0.11.0; the autonomous vehicles record, step 3): a vehicle is linked while a
/// station serves its radio on a channel and, since 0.12.0 (step 5), a colonist works that station with a hand free for it; it comes
/// back toward that station. It reads the net's last pass; nothing is worked out here
/// but a dictionary lookup. Since 0.14.0 (step 8) it also says which ground its stations cover (<see cref="RemoteCoverMap"/>), and a vehicle
/// asked to be driven keeps that map at work.
/// </summary>
public sealed class RadioRemoteControl : IRemoteControlSource, IRemoteCoverSource
{
    public bool Fitted(VehiclePawn vehicle) => vehicle.GetComp<CompRadio>() != null && VehicleRadios.Fitted(vehicle);

    public bool Carried(VehiclePawn vehicle) => vehicle.GetComp<CompRadio>() != null && VehicleRadios.Carried(vehicle);

    public RemoteCover? CoverOf(Map map) => RemoteCoverMap.CoverOf(map);

    public RemoteLink LinkOf(VehiclePawn vehicle)
    {
        if (!vehicle.Spawned) return default;
        // Asked for only while the vehicle is wanted, on its rare tick: that keeps its map's cover worked out.
        RemoteCoverMap.Want(vehicle);
        // Served on a channel, and (Radio 0.12.0, step 5) a colonist works that station with a hand free for this vehicle.
        var net = RadioNet.For(vehicle.Map);
        if (net == null) return default;
        var station = net.ServedBy(vehicle);
        // Why it is not driven, for the lost-link letter (Radio 0.20.1): no station hears it or its channels are full; or the station that
        // serves it has nobody at the console, or its operator's hands are full.
        if (station == null)
        {
            return new RemoteLink(false, IntVec3.Invalid)
            {
                Why = net.IsBusy(vehicle) ? "PhobosRadio_VehicleWhyChannelsFull".Translate().Resolve() : "PhobosRadio_VehicleWhyOutOfReach".Translate().Resolve(),
            };
        }
        if (!net.Drives(station, vehicle))
        {
            var label = station.LabelShort.Named("STATION");
            return new RemoteLink(false, IntVec3.Invalid)
            {
                Why = net.OperatorAt(station) is { } busy
                    ? "PhobosRadio_VehicleWhyHandsFull".Translate(busy.LabelShort.Named("OPERATOR"), label).Resolve()
                    : "PhobosRadio_VehicleWhyNoOperator".Translate(label).Resolve(),
            };
        }
        // And its picture, where the same station serves it (Radio 0.15.0, step 6).
        return new RemoteLink(true, station.Position, net.HasPicture(vehicle));
    }

    public void WantChanged(VehiclePawn vehicle)
    {
        if (!vehicle.Spawned) return;
        RadioNet.For(vehicle.Map)?.MarkDirty();
        // Start the cover map at once, so it is ready by the vehicle's first drive or soon after.
        if (RemoteControl.Wanted(vehicle)) RemoteCoverMap.Want(vehicle);
    }

    public Texture2D? Icon => RadioTextures.DriveFromStation;
}
