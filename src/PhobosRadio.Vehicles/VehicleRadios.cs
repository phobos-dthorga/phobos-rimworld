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
        // Radio 0.49.0 (docs/development/vehicle-standard.md, VS1): the Comms system's readings on every powered vehicle.
        VehicleComms.Register(new RadioComms());
        // Radio 0.21.0 (EM33): a convoy's reach home on the world map, asked by D'thorga Framework.
        ConvoyRadio.Register(new ConvoyReachSource());
        // Radio 0.22.0: halted convoys and vehicles parked at depots relay calls on.
        RelayNodes.Register(new VehicleRelaySource());
        // Radio 0.50.0 (EW7): aircraft in flight with the repeater kit relay from their height, holding station or in transit.
        RelayNodes.Register(new AircraftRelaySource());
        // Radio 0.45.0 (EW1): D'thorga Framework's jam fields that send out radio watts (the Busbar's mast, switched to radios) are noise.
        RadioJamming.Register(JammersFromFields);
        // Radio 0.42.0: a convoy is a place the world map's cover picture can be drawn for, and the picture's raised mast is the Datum's.
        WorldCover.Register(new ConvoyCoverSites());
        // Radio 0.44.0: the in-touch mood on the road reads a caravan's kept reach home, never working it out.
        AwayReach.RegisterCaravans(CaravanTouch.KeptOutcome);
        // Radio 0.54.0: the allied supply drop on the radio call too.
        Phobos.Radio.UI.RadioCallDialog.Register(RadioSupplyCalls.Option);
        if (DefDatabase<RadioCoverKindDef>.GetNamedSilentFail("PhobosRadioCover_VehicleMast") is { } mastKind) mastKind.antennaHeightMetres = ConvoyRadio.MastMetres;
        // Radio 0.23.0: a convoy's reach home carries advice by radio to the people in it.
        RemoteReach.Register(new ConvoyRemoteReach());
        // Radio 0.18.0: the base turret follows D'thorga Framework's "Remote guns fire on their own", which lives in its Vehicle Framework part.
        RemoteTurrets.FireAloneSwitch = () => RemoteGuns.FireAlone;
        // Radio 0.14.0: the cover map for vehicles driven from afar works a little each tick, through the net's own tick.
        RadioNet.Ticked += RemoteCoverMap.Tick;
        var kitRadio = DefDatabase<RadioFittingDef>.GetNamedSilentFail(KitRadioName)?.radio;
        var aviationRadio = DefDatabase<RadioFittingDef>.GetNamedSilentFail(AviationRadioName)?.radio;
        var marineRadio = DefDatabase<RadioFittingDef>.GetNamedSilentFail(MarineRadioName)?.radio;
        foreach (var def in DefDatabase<VehicleDef>.AllDefsListForReading)
        {
            // A design that names a fitting (our trucks), or one with a loadout of ours, which can take the kit fitting (Radio 0.15.0). Since
            // Radio 0.49.0 an aircraft with a loadout of ours carries the aviation radio instead: it has no remote control kit bay. Since
            // Radio 0.57.0 a boat carries the marine radio, for the same reason.
            bool ours = def.comps.Exists(comp => comp is CompProperties_PhobosVehicleSystems);
            bool aircraft = def.GetCompProperties<CompProperties_VehicleLauncher>() != null;
            bool boat = def.type == VehicleType.Sea;
            var radio = def.GetModExtension<VehicleRadioFitting>()?.fitting?.radio
                        ?? (ours ? (aircraft ? aviationRadio ?? kitRadio : boat ? marineRadio ?? kitRadio : kitRadio) : null);
            if (radio == null) continue;
            if (!def.comps.Exists(comp => comp is CompProperties_Radio)) def.comps.Add(radio);
            if (!def.comps.Exists(comp => comp is CompProperties_PhobosRemoteControl)) def.comps.Add(new CompProperties_PhobosRemoteControl());
        }
    }

    /// <summary>
    /// The jam fields on a map that send out radio watts, as the radio's jammers (Radio 0.45.0; docs/development/electronic-warfare.md, EW1,
    /// "the join"). Fields are D'thorga Framework's, read at most once a second; across every band the radio knows (barrage jamming). Any
    /// faction's, ours included: noise is noise (the owner's card of 27 Nov 2026).
    /// </summary>
    private static IEnumerable<RadioJammer> JammersFromFields(Map map)
    {
        foreach (var field in JamFields.On(map))
        {
            if (field.radioWatts > 0f) yield return new RadioJammer(field.centre.ToIntVec3(), field.radioWatts, null, field.source);
        }
    }

    /// <summary>The vehicle control radio, which the kit puts in place.</summary>
    public const string KitRadioName = "PhobosRadio_FittingVehicleControl";
    /// <summary>The aviation radio every aircraft carries (Radio 0.49.0) and its fitting's grant (AviationRadio.xml). Whether it has power is
    /// its fitting's feed, read through the Framework's Feeds (the owner's card of 28 Nov 2026, "Unread feed"), as for every set below.</summary>
    public const string AviationRadioName = "PhobosRadio_FittingAviation";
    public const string AviationGrant = "AviationRadio";
    /// <summary>The load's name, kept so other authors' C# still compiles (the owner's card of 28 Nov 2026, "Unread feed"): the fitting's feed now says what powers it.</summary>
    [System.Obsolete("Use Feeds.Works(vehicle, \"AviationRadio\")")]
    public const string AviationLoad = "aviationRadio";

    /// <summary>Whether this aircraft's aviation radio is fitted and powered (Radio 0.49.0).</summary>
    public static bool AviationFitted(VehiclePawn vehicle) => Feeds.Works(vehicle, AviationGrant);

    /// <summary>The marine radio every boat carries (Radio 0.57.0) and its fitting's grant (MarineRadio.xml).</summary>
    public const string MarineRadioName = "PhobosRadio_FittingMarine";
    public const string MarineGrant = "MarineRadio";
    /// <summary>The load's name, kept so other authors' C# still compiles (the owner's card of 28 Nov 2026, "Unread feed"): the fitting's feed now says what powers it.</summary>
    [System.Obsolete("Use Feeds.Works(vehicle, \"MarineRadio\")")]
    public const string MarineLoad = "marineRadio";

    /// <summary>Whether this boat's marine radio is fitted and powered (Radio 0.57.0).</summary>
    public static bool MarineFitted(VehiclePawn vehicle) => Feeds.Works(vehicle, MarineGrant);

    /// <summary>
    /// Whether the vehicle's own radio is on the net (Radio 0.49.0): the remote control kit's radio, an aircraft's aviation radio, or (Radio
    /// 0.57.0) a boat's marine radio. On the
    /// net it stands by, its links worked out and shown on its Comms system; only the kit's radio can be driven from a station.
    /// </summary>
    public static bool OnNet(VehiclePawn vehicle) => Fitted(vehicle) || AviationFitted(vehicle) || MarineFitted(vehicle);
    /// <summary>What the remote control kit fitting grants (Radio 0.15.0; RemoteControlKit.xml).</summary>
    public const string KitGrant = "RemoteControl";
    /// <summary>The load's name, kept so other authors' C# still compiles (the owner's card of 28 Nov 2026, "Unread feed"): the fitting's feed now says what powers it.</summary>
    [System.Obsolete("Use Feeds.Works(vehicle, \"RemoteControl\")")]
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
        return Feeds.Works(vehicle, KitGrant);
    }

    /// <summary>
    /// As <see cref="Fitted"/>, working or not (Radio 0.16.1): the remote control kit counts while it is on the vehicle, its power shed or
    /// its part shot away, so the vehicle acts on a lost link and the player is told, rather than letting go without a word.
    /// </summary>
    /// <summary>What the repeater kit grants (Radio 0.22.0; RepeaterKit.xml).</summary>
    public const string RepeaterGrant = "RadioRepeater";
    /// <summary>The load's name, kept so other authors' C# still compiles (the owner's card of 28 Nov 2026, "Unread feed"): the fitting's feed now says what powers it.</summary>
    [System.Obsolete("Use Feeds.Works(vehicle, \"RadioRepeater\")")]
    public const string RepeaterLoad = "repeaterKit";

    /// <summary>Whether this vehicle's radio passes calls on between tiles: the repeater kit fitted and powered (the owner's card of 26 Nov
    /// 2026: a relay between tiles needs a repeater).</summary>
    public static bool Repeats(VehiclePawn vehicle) => Feeds.Works(vehicle, RepeaterGrant);

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
/// The Comms system's answers (Radio 0.49.0; docs/development/vehicle-standard.md, VS1): whether a vehicle carries a working radio (the
/// remote control kit's, the dual-band radio, an aircraft's aviation radio) and how its own radio's best link to a station on its map stands,
/// from the net's last pass. Away from a map it says so: a convoy's own radio button tells how it reaches home. Cost: a few fitting lookups and
/// one dictionary lookup each time the systems step, on their own rare schedule.
/// </summary>
public sealed class RadioComms : IVehicleCommsSource
{
    private static readonly string[] Radios = { VehicleRadios.KitGrant, CrossBand.Grant, VehicleRadios.AviationGrant, VehicleRadios.MarineGrant };

    public CommsRadio RadioOf(VehiclePawn vehicle)
    {
        if (vehicle.GetComp<CompPhobosVehicleSystems>() is not { } systems) return CommsRadio.None;
        var carried = false;
        foreach (var grant in Radios)
        {
            if (Feeds.Works(systems, grant)) return CommsRadio.Working;
            carried |= systems.Fittings.Carries(grant);
        }
        return carried || VehicleRadios.Fitted(vehicle) ? CommsRadio.Dead : CommsRadio.None;
    }

    public CommsLink LinkOf(VehiclePawn vehicle)
    {
        if (RadioOf(vehicle) != CommsRadio.Working) return CommsLink.NoRadio;
        if (!vehicle.Spawned) return CommsLink.Away;
        if (vehicle.Faction != RimWorld.Faction.OfPlayer) return CommsLink.OutOfTouch;
        var net = RadioNet.For(vehicle.Map);
        if (!net.TryGetBest(vehicle, out var link) || link == null) return CommsLink.OutOfTouch;
        var reading = !link.InTouch && link.JammingLimits ? CommsLink.Jammed
            : link.Outcome == Phobos.Radio.Physics.LinkOutcome.Marginal ? CommsLink.Faint
            : link.InTouch ? CommsLink.InTouch : CommsLink.OutOfTouch;
        // Seen in play (Radio 0.49.0, row 711): an aircraft's aviation radio in touch with a station.
        if (reading == CommsLink.InTouch && VehicleRadios.AviationFitted(vehicle) && Seen.Wants("radio.aviation-link", vehicle))
            Seen.Note("radio.aviation-link", vehicle, link.Band.label);
        // Seen in play (Radio 0.57.0, row 746): a boat's marine radio in touch with a station.
        if (reading == CommsLink.InTouch && VehicleRadios.MarineFitted(vehicle) && Seen.Wants("radio.marine-link", vehicle))
            Seen.Note("radio.marine-link", vehicle, link.Band.label);
        return reading;
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
        // Radio 0.49.0: an aircraft's aviation radio is on the net too, standing by (it cannot be driven from a station).
        if (!VehicleRadios.OnNet(vehicle)) return CarriedRadioUse.Off;
        if (!VehicleRadios.Fitted(vehicle)) return CarriedRadioUse.StandingBy;
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
            // Seen in play (Radio 0.45.0, row 708): a vehicle driven by radio that jamming has cut off.
            if (Seen.Wants("radio.vehicle-jammed", vehicle) && net.TryGetBest(vehicle, out var jammed) && jammed is { InTouch: false, JammingLimits: true })
                Seen.Note("radio.vehicle-jammed", vehicle, jammed.Mode.label);
            return new RemoteLink(false, IntVec3.Invalid)
            {
                Why = net.IsBusy(vehicle) ? "PhobosRadio_VehicleWhyChannelsFull".Translate().Resolve()
                    // Radio 0.45.0 (EW1): a link jamming drowned says so, rather than "out of reach".
                    : net.TryGetBest(vehicle, out var best) && best is { InTouch: false, JammingLimits: true } ? "PhobosRadio_VehicleWhyJammed".Translate().Resolve()
                    : "PhobosRadio_VehicleWhyOutOfReach".Translate().Resolve(),
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
