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
    /// <summary>Whether this vehicle carries this source's kit, so it can be driven by it.</summary>
    bool Fitted(VehiclePawn vehicle);
    /// <summary>The vehicle's link now. Read from what the source already worked out; nothing is worked out here.</summary>
    RemoteLink LinkOf(VehiclePawn vehicle);
    /// <summary>The player asked for the vehicle to be driven, or let it go: the source works its link out again at once.</summary>
    void WantChanged(VehiclePawn vehicle);
    /// <summary>The icon of the button that asks for it, or null for ours.</summary>
    Texture2D? Icon { get; }
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

    /// <summary>The source whose kit this vehicle carries, or null.</summary>
    public static IRemoteControlSource? SourceFor(VehiclePawn vehicle)
    {
        foreach (var source in sources) if (source.Fitted(vehicle)) return source;
        return null;
    }

    /// <summary>Whether the player has asked for this vehicle to be driven from afar, with the setting on: a source asks its link for it.</summary>
    public static bool Wanted(VehiclePawn vehicle) => vehicle.GetComp<CompPhobosRemoteControl>() is { Wanted: true };

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
    LostLinkChoice choice = LostLinkChoice.Hold;
    RemoteState state = RemoteState.Off;
    int lostSinceTick = -1;
    IntVec3 station = IntVec3.Invalid;
    IntVec3 comeBackCell = IntVec3.Invalid;
    /// <summary>Whether its station has its picture now (Framework 0.280.0). Derived from the link at each look; never saved.</summary>
    bool picture;
    /// <summary>Whether its remote guns could fire at the last look, so a change re-checks them (Framework 0.280.0). Never saved.</summary>
    bool? gunsMayFire;

    /// <summary>It asks for no tick of its own: the vehicle's rare tick reaches every comp.</summary>
    public override bool TickByRequest => true;

    /// <summary>Whether a station drives it and has its picture: an operator can watch it and aim its guns (Framework 0.280.0).</summary>
    public bool HasPicture => picture && Drives;

    /// <summary>
    /// Whether it asks its station for a picture as well as orders (Framework 0.280.0; the record's kinds table: the picture is held while
    /// the operator drives it under direct orders): asked to be driven, and drafted.
    /// </summary>
    public bool WantsPicture => Wanted && Vehicle.Drafted;

    /// <summary>The player asked for it to be driven from afar, the setting allows it, and its own crew is not at the wheel.</summary>
    public bool Wanted => wanted && RemoteControl.On && !RemoteControl.CrewDrives(Vehicle);

    /// <summary>Whether a station drives it now: what Vehicle Framework's move check reads. One field and the setting.</summary>
    public bool Drives => RemoteControl.On && RemoteControlRule.Drives(state);

    public RemoteState State => state;
    public LostLinkChoice Choice => choice;

    /// <summary>The player's ask, from the button: the source works the link out at once, and the vehicle can be drafted straight away.</summary>
    public void SetWanted(bool on)
    {
        if (wanted == on) return;
        wanted = on;
        if (RemoteControl.SourceFor(Vehicle) is { } source) source.WantChanged(Vehicle);
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
        var now = RemoteGuns.MayFire(Vehicle);
        if (gunsMayFire == now) return;
        gunsMayFire = now;
        RemoteGuns.Recache(Vehicle);
    }

    /// <summary>Its link now, and the state that follows from it.</summary>
    void Look()
    {
        var vehicle = Vehicle;
        if (!vehicle.Spawned) return;
        var source = RemoteControl.SourceFor(vehicle);
        var want = source != null && Wanted;
        var link = want ? source!.LinkOf(vehicle) : default;
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
            PhobosRemoteControlRulesDef.Current.lostLinkDelayTicks, choice, VehicleAutoDrive.Moving(vehicle));
        if (after == before) return;
        state = after;
        Changed(vehicle, before, after);
    }

    void Changed(VehiclePawn vehicle, RemoteState before, RemoteState after)
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
            var key = after switch
            {
                RemoteState.ComingBack => "PhobosRemote_LostComeBack",
                RemoteState.CarryingOn => "PhobosRemote_LostCarryOn",
                _ => "PhobosRemote_LostHold",
            };
            Find.LetterStack.ReceiveLetter("PhobosRemote_LostLabel".Translate(vehicle.LabelShort.Named("VEHICLE")),
                "PhobosRemote_LostText".Translate(vehicle.LabelShort.Named("VEHICLE"), key.Translate().Named("ACTION")),
                LetterDefOf.NegativeEvent, vehicle);
            if (Seen.Wants("vehicles.remote-lost", vehicle, after.ToString())) Seen.Note("vehicles.remote-lost", vehicle, vehicle.LabelShort, after.ToString());
        }
        else if (RemoteControlRule.TellsFound(before, after))
        {
            Find.LetterStack.ReceiveLetter("PhobosRemote_FoundLabel".Translate(vehicle.LabelShort.Named("VEHICLE")),
                "PhobosRemote_FoundText".Translate(vehicle.LabelShort.Named("VEHICLE")), LetterDefOf.NeutralEvent, vehicle);
            if (Seen.Wants("vehicles.remote-found", vehicle)) Seen.Note("vehicles.remote-found", vehicle, vehicle.LabelShort);
        }
    }

    /// <summary>Orders the drive back toward the station it last heard, to a place on a ring round it; false where there is none.</summary>
    bool DriveBack(VehiclePawn vehicle)
    {
        if (!station.IsValid || !station.InBounds(vehicle.Map)) return false;
        if (!VehicleAutoDrive.TryFindStandCell(vehicle, station, PhobosRemoteControlRulesDef.Current.comeBackRing, out var cell)) return false;
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
        if (vehicle.Faction != Faction.OfPlayer || !RemoteControl.On || RemoteControl.SourceFor(vehicle) is not { } source) yield break;
        yield return new Command_Toggle
        {
            defaultLabel = "PhobosRemote_GizmoDrive".Translate(),
            defaultDesc = "PhobosRemote_GizmoDriveDesc".Translate(),
            icon = source.Icon ?? SystemIcons.Switch,
            isActive = () => wanted,
            toggleAction = () => SetWanted(!wanted),
        };
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
        if (!wanted || !RemoteControl.On) return string.Empty;
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
