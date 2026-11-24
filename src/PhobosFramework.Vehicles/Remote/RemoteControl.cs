using System.Collections.Generic;
using HarmonyLib;
using Phobos.Framework;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A vehicle's link to whatever drives it from afar, as its source reports it: whether it has one, where that station stands, and (Framework
/// 0.280.0, step 6) whether the station also has the vehicle's picture, so an operator can watch and aim its guns.
/// </summary>
public readonly record struct RemoteLink(bool Linked, IntVec3 Station, bool Picture = false);

/// <summary>
/// Whatever can drive a vehicle with nobody aboard (Framework 0.278.0; docs/development/autonomous-vehicles.md, step 3): the Radio
/// Framework's stations today, a route or a mechanitor later. This part never names the radio; a source registers itself.
/// </summary>
public interface IRemoteControlSource
{
    /// <summary>Whether this vehicle carries this source's kit and it works, so it can be driven by it.</summary>
    bool Fitted(VehiclePawn vehicle);
    /// <summary>
    /// Whether the kit is on the vehicle at all, working or not (Framework 0.284.0): a kit carried but not working (its power shed, its
    /// part shot away) is a lost link, with the lost-link letter, rather than a kit taken off. A source that cannot tell says
    /// <see cref="Fitted"/>.
    /// </summary>
    bool Carried(VehiclePawn vehicle) => Fitted(vehicle);
    /// <summary>The vehicle's link now. Read from what the source already worked out; nothing is worked out here.</summary>
    RemoteLink LinkOf(VehiclePawn vehicle);
    /// <summary>The player asked for the vehicle to be driven, or let it go: the source works its link out again at once.</summary>
    void WantChanged(VehiclePawn vehicle);
    /// <summary>The icon of the button that asks for it, or null for ours.</summary>
    Texture2D? Icon { get; }
    /// <summary>
    /// Its name, saved with a vehicle it drives (Framework 0.281.0), so a vehicle with two kits knows which one drives it. Stable once a save
    /// holds it.
    /// </summary>
    string Key => GetType().Name;
    /// <summary>
    /// Whether it is asked for with the "Drive from a station" button and has a picture to watch through (a station: the radio), rather than
    /// buttons of its own (the route level). Remote guns answer only to such a source.
    /// </summary>
    bool StationToggle => true;
    /// <summary>Whether its setting lets it drive now.</summary>
    bool Enabled => RemoteControl.On;
    /// <summary>The start of its letters' text keys (Framework 0.282.0): a station's, a route's or a mechanitor's own words for a lost link.</summary>
    string TextPrefix => "PhobosRemote_";
}

/// <summary>The sources, and whether the setting lets any vehicle be driven from afar.</summary>
public static class RemoteControl
{
    static readonly List<IRemoteControlSource> sources = new();

    /// <summary>Adds a source, once; called from its start-up.</summary>
    public static void Register(IRemoteControlSource source)
    {
        if (!sources.Contains(source)) sources.Add(source);
    }

    /// <summary>The setting "Remote control of vehicles by radio" (on by default; the risk is moderate). Live.</summary>
    public static bool On => VehicleSystemsMod.Settings.remoteControl;

    /// <summary>The first source whose kit this vehicle carries, or null.</summary>
    public static IRemoteControlSource? SourceFor(VehiclePawn vehicle)
    {
        foreach (var source in sources) if (source.Fitted(vehicle)) return source;
        return null;
    }

    /// <summary>The source of this name, if the vehicle carries its kit (Framework 0.281.0).</summary>
    public static IRemoteControlSource? SourceFor(VehiclePawn vehicle, string key)
    {
        foreach (var source in sources) if (source.Key == key && source.Fitted(vehicle)) return source;
        return null;
    }

    /// <summary>The station-driven source whose kit this vehicle carries (the radio's), or null (Framework 0.281.0).</summary>
    public static IRemoteControlSource? StationSource(VehiclePawn vehicle)
    {
        foreach (var source in sources) if (source.StationToggle && source.Fitted(vehicle)) return source;
        return null;
    }

    /// <summary>
    /// The source of this name (a station's when the name is empty) whose kit is on the vehicle, working or not (Framework 0.284.0), so a
    /// vehicle whose kit fails keeps its source and acts on a lost link.
    /// </summary>
    public static IRemoteControlSource? CarriedSource(VehiclePawn vehicle, string? key)
    {
        foreach (var source in sources)
        {
            if (key.NullOrEmpty() ? !source.StationToggle : source.Key != key) continue;
            if (source.Carried(vehicle)) return source;
        }
        return null;
    }

    /// <summary>
    /// Whether the player has asked for this vehicle to be driven from a station, with the setting on: a station source asks its link for
    /// it. A vehicle on a route answers no here (Framework 0.281.0), so it asks no radio channel.
    /// </summary>
    public static bool Wanted(VehiclePawn vehicle) => vehicle.GetComp<CompPhobosRemoteControl>() is { WantedByStation: true };

    /// <summary>
    /// Whether the vehicle's own crew can drive it, as Vehicle Framework counts them (VehiclePawn.HasEnoughOperators), read without our
    /// answer to that check: every seat that drives is filled, or it needs nobody. Then remote control stands aside.
    /// </summary>
    public static bool CrewDrives(VehiclePawn vehicle)
    {
        if ((vehicle.MovementPermissions & VehiclePermissions.Autonomous) != 0) return true;
        var any = false;
        foreach (var handler in vehicle.handlers)
        {
            if ((handler.role.HandlingTypes & HandlingType.Movement) == 0) continue;
            if (!handler.RoleFulfilled) return false;
            any = true;
        }
        return any;
    }
}

/// <summary>
/// Lets a vehicle be driven from afar by whatever source carries its kit (the Radio Framework's stations): its two buttons, its
/// lost-link choice and letters. It takes no figures; a source gives it to the vehicles that carry the kit, so XML seldom names it.
/// </summary>
public class CompProperties_PhobosRemoteControl : CompProperties
{
    public CompProperties_PhobosRemoteControl() => compClass = typeof(CompPhobosRemoteControl);
}

/// <summary>
/// A vehicle that can be driven from afar (Framework 0.278.0; docs/development/autonomous-vehicles.md, step 3): the player's ask, its
/// state with its link, what it does when the link is lost (the owner's choice, per vehicle, hold by default), and the letters. Its
/// source gives it to the vehicles that carry the kit. It looks at its link on the vehicle's rare tick, every 250 ticks, and at once when
/// the player asks; Vehicle Framework's move check reads <see cref="Drives"/> through <see cref="RemoteControlOperatorsPatch"/>.
/// </summary>
public sealed class CompPhobosRemoteControl : VehicleComp
{
    bool wanted;
    /// <summary>Which source drives it (Framework 0.281.0): a source's <see cref="IRemoteControlSource.Key"/>; empty for a station's, as
    /// saves from before this knew only the radio.</summary>
    string? drivenBy;
    /// <summary>Whether that source's setting let it drive at the last look: what the move check reads. Never saved.</summary>
    bool? enabled;
    LostLinkChoice choice = LostLinkChoice.Hold;
    RemoteState state = RemoteState.Off;
    int lostSinceTick = -1;
    IntVec3 station = IntVec3.Invalid;
    IntVec3 comeBackCell = IntVec3.Invalid;
    /// <summary>Whether its station has its picture now (Framework 0.280.0). Derived from the link at each look; never saved.</summary>
    bool picture;
    /// <summary>What decided its remote guns at the last look (RemoteGuns.Inputs), so a change re-checks them (Framework 0.280.0). Never saved.</summary>
    int? gunInputs;

    /// <summary>It asks for no tick of its own: the vehicle's rare tick reaches every comp.</summary>
    public override bool TickByRequest => true;

    /// <summary>Whether a station drives it and has its picture: an operator can watch it and aim its guns (Framework 0.280.0).</summary>
    public bool HasPicture => picture && Drives;

    /// <summary>
    /// Whether it asks its station for a picture as well as orders (Framework 0.280.0; the record's kinds table: the picture is held while
    /// the operator drives it under direct orders): asked to be driven, and drafted.
    /// </summary>
    public bool WantsPicture => WantedByStation && Vehicle.Drafted;

    /// <summary>
    /// The source that drives it, or would: the one the player asked for, if its kit is still on (Framework 0.281.0), working or not
    /// (Framework 0.284.0: a failed kit is a lost link, <see cref="KitDown"/>).
    /// </summary>
    public IRemoteControlSource? Current => RemoteControl.CarriedSource(Vehicle, drivenBy);

    /// <summary>Asked to be driven, and the kit that drives it is on the vehicle but not working: its power shed or its part shot away.</summary>
    public bool KitDown => wanted && Current is { } source && !source.Fitted(Vehicle);

    /// <summary>The player asked for it to be driven with nobody aboard, that source's setting allows it, and its own crew is not at the wheel.</summary>
    public bool Wanted => wanted && Current is { Enabled: true } && !RemoteControl.CrewDrives(Vehicle);

    /// <summary>Asked to be driven from a station (the radio), not along a route.</summary>
    public bool WantedByStation => Wanted && Current is { StationToggle: true };

    /// <summary>Asked to be driven by the source of this name.</summary>
    public bool WantedBy(string key) => wanted && drivenBy == key;

    /// <summary>Whether something drives it now: what Vehicle Framework's move check reads. Fields only, after the first look.</summary>
    public bool Drives => RemoteControlRule.Drives(state) && (enabled ??= Current is { Enabled: true });

    public RemoteState State => state;
    public LostLinkChoice Choice => choice;

    /// <summary>The "Drive from a station" button: asks the station source, or lets the vehicle go.</summary>
    public void SetWanted(bool on) => SetWanted(on, on ? RemoteControl.StationSource(Vehicle)?.Key : null);

    /// <summary>
    /// The player's ask (Framework 0.281.0: by a source's name, so the route level asks too): the sources work the link out at once, and the
    /// vehicle can be drafted straight away. Asking one source lets the other go.
    /// </summary>
    public void SetWanted(bool on, string? key)
    {
        var before = Current;
        if (wanted == on && (!on || drivenBy == key)) return;
        wanted = on;
        drivenBy = on ? key : null;
        before?.WantChanged(Vehicle);
        if (Current is { } now && now != before) now.WantChanged(Vehicle);
        Look();
    }

    public void SetChoice(LostLinkChoice to) => choice = to;

    public override void CompTickRare()
    {
        if (!Vehicle.Spawned) return;
        if (wanted || state != RemoteState.Off) Look();
        CheckGuns();
    }

    /// <summary>
    /// Re-checks its remote guns when whether they may fire has changed (Framework 0.280.0): a picture won or lost, the kit fitted or
    /// taken off, the setting changed. Vehicle Framework keeps a turret's manned state until something asks it again.
    /// </summary>
    void CheckGuns()
    {
        if (!RemoteGuns.HasAny(Vehicle)) return;
        // Since 0.285.0 a gun may be governed by its Def rather than the kit, so what is compared is what decides, not one answer.
        var now = RemoteGuns.Inputs(Vehicle);
        if (gunInputs == now) return;
        gunInputs = now;
        RemoteGuns.Recache(Vehicle);
    }

    /// <summary>Its link now, and the state that follows from it.</summary>
    void Look()
    {
        var vehicle = Vehicle;
        if (!vehicle.Spawned) return;
        var source = Current;
        enabled = source is { Enabled: true };
        var want = source != null && Wanted;
        // A kit carried but not working (Framework 0.284.0) asks nothing of its source: the vehicle holds and the player is told why.
        var kitDown = want && !source!.Fitted(vehicle);
        var link = want && !kitDown ? source!.LinkOf(vehicle) : default;
        var hadPicture = picture;
        picture = link.Linked && link.Picture;
        if (picture != hadPicture && Seen.Wants("vehicles.remote-picture", vehicle, picture ? "won" : "lost"))
        {
            Seen.Note("vehicles.remote-picture", vehicle, vehicle.LabelShort, picture ? "won" : "lost");
        }
        var now = Find.TickManager.TicksGame;
        if (link.Linked)
        {
            lostSinceTick = -1;
            station = link.Station;
        }
        else if (!want)
        {
            lostSinceTick = -1;
        }
        else if (lostSinceTick < 0 && state is RemoteState.Linked or RemoteState.Waiting)
        {
            lostSinceTick = now;
        }
        var before = state;
        var after = RemoteControlRule.Next(before, want, link.Linked, lostSinceTick < 0 ? 0 : now - lostSinceTick,
            PhobosRemoteControlRulesDef.Current.lostLinkDelayTicks, choice, VehicleAutoDrive.Moving(vehicle), kitDown);
        if (after == before) return;
        state = after;
        Changed(vehicle, before, after, kitDown);
    }

    void Changed(VehiclePawn vehicle, RemoteState before, RemoteState after, bool kitDown)
    {
        if (before == RemoteState.ComingBack && comeBackCell.IsValid)
        {
            VehicleAutoDrive.Halt(vehicle, comeBackCell);
            comeBackCell = IntVec3.Invalid;
        }
        if (after == RemoteState.ComingBack && !DriveBack(vehicle)) state = after = RemoteState.Holding;
        if (after == RemoteState.Linked && Seen.Wants("vehicles.remote-control", vehicle)) Seen.Note("vehicles.remote-control", vehicle, vehicle.LabelShort);
        if (RemoteControlRule.TellsLost(before, after))
        {
            // Each source says it in its own words: a station, a route's leader, a mechanitor (Framework 0.282.0).
            var prefix = Current?.TextPrefix ?? "PhobosRemote_";
            // A failed kit says so, whoever's kit it is (Framework 0.284.0).
            var key = kitDown ? "PhobosRemote_LostKitDown" : prefix + after switch
            {
                RemoteState.ComingBack => "LostComeBack",
                RemoteState.CarryingOn => "LostCarryOn",
                _ => "LostHold",
            };
            Find.LetterStack.ReceiveLetter((prefix + "LostLabel").Translate(vehicle.LabelShort.Named("VEHICLE")),
                (prefix + "LostText").Translate(vehicle.LabelShort.Named("VEHICLE"), key.Translate().Named("ACTION")),
                LetterDefOf.NegativeEvent, vehicle);
            if (kitDown)
            {
                if (Seen.Wants("vehicles.remote-kit-down", vehicle)) Seen.Note("vehicles.remote-kit-down", vehicle, vehicle.LabelShort);
            }
            else if (Seen.Wants("vehicles.remote-lost", vehicle, after.ToString())) Seen.Note("vehicles.remote-lost", vehicle, vehicle.LabelShort, after.ToString());
        }
        else if (RemoteControlRule.TellsFound(before, after))
        {
            var prefix = Current?.TextPrefix ?? "PhobosRemote_";
            Find.LetterStack.ReceiveLetter((prefix + "FoundLabel").Translate(vehicle.LabelShort.Named("VEHICLE")),
                (prefix + "FoundText").Translate(vehicle.LabelShort.Named("VEHICLE")), LetterDefOf.NeutralEvent, vehicle);
            if (Seen.Wants("vehicles.remote-found", vehicle)) Seen.Note("vehicles.remote-found", vehicle, vehicle.LabelShort);
        }
    }

    /// <summary>Orders the drive back toward the station it last heard, to a place on a ring round it; false where there is none.</summary>
    bool DriveBack(VehiclePawn vehicle)
    {
        if (!station.IsValid || !station.InBounds(vehicle.Map)) return false;
        if (!VehicleAutoDrive.TryFindStandCell(vehicle, station, PhobosRemoteControlRulesDef.Current.comeBackRing, out var cell)) return false;
        // Asked first (Framework 0.284.0), so a vehicle that cannot go (no fuel, set-up work) holds without Vehicle Framework's refusal message.
        if (VehicleAutoDrive.CannotDrive(vehicle) != null) return false;
        if (!VehicleAutoDrive.StartEngine(vehicle, out _) || !VehicleAutoDrive.DriveTo(vehicle, cell)) return false;
        comeBackCell = cell;
        return true;
    }

    /// <summary>Off the map (a caravan, a hold) no station reaches it: it lets go, and takes up the link again once back if still asked.</summary>
    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);
        state = RemoteState.Off;
        lostSinceTick = -1;
        comeBackCell = IntVec3.Invalid;
        picture = false;
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        var vehicle = Vehicle;
        // A vehicle whose kit has failed keeps its buttons (Framework 0.284.0), so the player can still let it go.
        if (vehicle.Faction != Faction.OfPlayer || RemoteControl.SourceFor(vehicle) == null && Current == null) yield break;
        // The station's button (Framework 0.281.0: only for a station source; a route has buttons of its own).
        if ((RemoteControl.StationSource(vehicle) ?? (wanted && Current is { StationToggle: true } held ? held : null)) is { Enabled: true } station)
        {
            yield return new Command_Toggle
            {
                defaultLabel = "PhobosRemote_GizmoDrive".Translate(),
                defaultDesc = "PhobosRemote_GizmoDriveDesc".Translate(),
                icon = station.Icon ?? SystemIcons.Switch,
                isActive = () => wanted && Current is { StationToggle: true },
                toggleAction = () => SetWanted(!(wanted && Current is { StationToggle: true })),
            };
        }
        yield return new Command_Action
        {
            defaultLabel = ChoiceLabel(choice),
            defaultDesc = "PhobosRemote_GizmoLostDesc".Translate(),
            icon = SystemIcons.For("Phobos/VehicleSystems/BayTrips/VehiclePark"),
            action = () => Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
            {
                new("PhobosRemote_ChoiceHold".Translate(), () => SetChoice(LostLinkChoice.Hold)),
                new("PhobosRemote_ChoiceComeBack".Translate(), () => SetChoice(LostLinkChoice.ComeBack)),
                new("PhobosRemote_ChoiceCarryOn".Translate(), () => SetChoice(LostLinkChoice.CarryOn)),
            })),
        };
    }

    static string ChoiceLabel(LostLinkChoice choice) => choice switch
    {
        LostLinkChoice.ComeBack => "PhobosRemote_GizmoLostComeBack".Translate(),
        LostLinkChoice.CarryOn => "PhobosRemote_GizmoLostCarryOn".Translate(),
        _ => "PhobosRemote_GizmoLostHold".Translate(),
    };

    public override string CompInspectStringExtra()
    {
        // A route says its own (CompPhobosRoute, Framework 0.281.0).
        if (!wanted || Current is not { Enabled: true, StationToggle: true }) return string.Empty;
        if (KitDown) return "PhobosRemote_InspectKitDown".Translate();
        return state switch
        {
            RemoteState.Unlinked => "PhobosRemote_InspectUnlinked".Translate(),
            // Orders and picture, or orders only (Framework 0.280.0): in Plain display the record's words, never a figure.
            RemoteState.Linked => picture ? "PhobosRemote_InspectLinkedPicture".Translate() : "PhobosRemote_InspectLinked".Translate(),
            RemoteState.Waiting => "PhobosRemote_InspectWaiting".Translate(),
            RemoteState.Holding => "PhobosRemote_InspectHolding".Translate(),
            RemoteState.ComingBack => "PhobosRemote_InspectComingBack".Translate(),
            RemoteState.CarryingOn => "PhobosRemote_InspectCarryingOn".Translate(),
            _ => RemoteControl.CrewDrives(Vehicle) ? "PhobosRemote_InspectCrew".Translate() : string.Empty,
        };
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref wanted, "PhobosRemote_Wanted");
        Scribe_Values.Look(ref drivenBy, "PhobosRemote_DrivenBy");
        Scribe_Values.Look(ref choice, "PhobosRemote_Choice", LostLinkChoice.Hold);
        Scribe_Values.Look(ref state, "PhobosRemote_State", RemoteState.Off);
        Scribe_Values.Look(ref lostSinceTick, "PhobosRemote_LostSince", -1);
        Scribe_Values.Look(ref station, "PhobosRemote_Station", IntVec3.Invalid);
        Scribe_Values.Look(ref comeBackCell, "PhobosRemote_ComeBackCell", IntVec3.Invalid);
    }
}

/// <summary>
/// Vehicle Framework lets a vehicle be drafted and driven only when every seat that drives is filled (VehiclePawn.HasEnoughOperators, read by
/// CanDraft and CanMoveFinal, the latter on every tick a drafted vehicle has a path). Our trucks keep their driver's seat (the owner, 18 Nov
/// 2026), so a vehicle a station drives answers yes here with its seat empty. Postfix: it changes only a "no", only on the map, and reads one
/// field of our comp; nothing is worked out here.
/// </summary>
[HarmonyPatch(typeof(VehiclePawn), nameof(VehiclePawn.HasEnoughOperators), MethodType.Getter)]
public static class RemoteControlOperatorsPatch
{
    public static void Postfix(VehiclePawn __instance, ref bool __result)
    {
        if (__result || !__instance.Spawned) return;
        if (__instance.GetComp<CompPhobosRemoteControl>() is { Drives: true }) __result = true;
    }
}
