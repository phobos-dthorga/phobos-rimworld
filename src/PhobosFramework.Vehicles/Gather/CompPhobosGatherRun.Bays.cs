using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>An errand a truck was told to run while nobody was aboard to drive it (Framework 0.177.0). Saved by name: append only.</summary>
public enum ErrandWaiting
{
    None,
    Deliver,
    Gather,
}

/// <summary>
/// Unloading at marked bays, and Deliver (Framework 0.175.0; docs/development/vehicle-gathering-runs.md, "Marked bays and Deliver"). The
/// owner's request of 19 Nov 2026: something semi-automated like "Vehicles may load from here", for unloading. A gathering run set to
/// "any marked bay", and a delivery, make for the nearest place marked "Vehicles may unload here" that takes something aboard and has room,
/// back up to it and tip only what it takes; when it fills, or takes no more of the load, they make for the next. A delivery takes a load
/// already aboard (from the cargo window, a drill rig, a chute) and leaves spare parts aboard; afterwards it drives back to where it
/// started, or stays at the last bay, as the player chose for that truck. UnloadBayRule decides; VehicleUnloadBays says what is marked.
/// </summary>
public partial class CompPhobosGatherRun : IVehicleWantsDriver
{
    /// <summary>How many bays one look may try before it gives up finding a way to any. Authored.</summary>
    const int MaxBayTries = 8;

    /// <summary>How long the buttons keep an answer that lists every marked bay, in real seconds: they are drawn every frame. Authored.</summary>
    const float ButtonRecheckSeconds = 0.5f;

    /// <summary>Picture of the Deliver button: the game's own eject picture (the cryptosleep casket's), until one of ours is drawn.</summary>
    const string DeliverIconPath = "UI/Commands/PodEject";

    /// <summary>Picture of the drive-back switch: our depot's "go to bay" picture, an arrow into a bay.</summary>
    const string DriveBackIconPath = "Phobos/VehicleSystems/Stations/BayGoTo";

    /// <summary>The player's choice for a gathering run: unload at any marked bay.</summary>
    bool anyBay;
    /// <summary>This run is a delivery, not a gathering run.</summary>
    bool delivering;
    /// <summary>The player's choice for this truck's deliveries: stay at the last bay rather than drive back. Off: it drives back.</summary>
    bool deliverStays;
    /// <summary>The marked bay this trip makes for: one of the two, or neither.</summary>
    Building_Storage? bayBuilding;
    Zone_Stockpile? bayZone;
    /// <summary>Where a delivery started, to drive back to.</summary>
    IntVec3 homeCell = IntVec3.Invalid;
    Rot4 homeFacing = Rot4.Invalid;
    /// <summary>How a delivery ended, held while it drives back.</summary>
    GatherRunEnd deliveryEnd;
    string? deliveryReason;

    // Derived, never saved: bays found full or out of reach on this trip. After a load it may try one once more.
    readonly List<UnloadPlace> passedBays = new();

    float deliverCheckedAt = -1f;
    string? deliverRefusal;
    float bayCheckedAt = -1f;
    bool anyBayMarked;

    bool UsesBays => delivering || anyBay;

    /// <summary>The bay this trip makes for, or null: one gone, no longer the player's or no longer marked is dropped.</summary>
    UnloadPlace? Bay
    {
        get
        {
            if (bayBuilding == null && bayZone == null) return null;
            var place = new UnloadPlace(bayBuilding, bayZone);
            if (place.StillMarkedOn(Vehicle.MapHeld)) return place;
            ClearBay();
            return null;
        }
    }

    void SetBay(UnloadPlace place)
    {
        bayBuilding = place.Building;
        bayZone = place.Zone;
    }

    void ClearBay()
    {
        bayBuilding = null;
        bayZone = null;
    }

    /// <summary>Passes over the bay this trip was making for: full, or out of reach.</summary>
    void PassBay()
    {
        if (bayBuilding != null || bayZone != null) passedBays.Add(new UnloadPlace(bayBuilding, bayZone));
        ClearBay();
    }

    bool BayAccepts(Thing thing) => Bay is { } bay && bay.Accepts(thing);

    /// <summary>What a run would put in a bay: a delivery, everything but spare parts; a gathering run, what it gathers.</summary>
    bool ForBays(Thing thing, bool deliver) => deliver ? !VehicleUnloadBays.IsSparePart(thing.def) : Filter.Allows(thing);

    bool AnythingForBays(bool deliver)
    {
        ThingOwner cargo = Vehicle.inventory.innerContainer;
        for (int i = 0; i < cargo.Count; i++)
        {
            if (ForBays(cargo[i], deliver)) return true;
        }
        return false;
    }

    /// <summary>One thing aboard for each kind a run would put in a bay, to test the bays with.</summary>
    List<Thing> LoadKinds(bool deliver)
    {
        var kinds = new List<Thing>();
        var seen = new HashSet<ThingDef>();
        ThingOwner cargo = Vehicle.inventory.innerContainer;
        for (int i = 0; i < cargo.Count; i++)
        {
            Thing thing = cargo[i];
            if (ForBays(thing, deliver) && seen.Add(thing.def)) kinds.Add(thing);
        }
        return kinds;
    }

    /// <summary>The nearest marked bay that takes something aboard and has room, passing over any already found full or out of reach.</summary>
    UnloadPlace? PickBay(bool deliver, out BayShortfall why)
    {
        why = BayShortfall.NoneMarked;
        Map? map = Vehicle.MapHeld;
        if (map == null) return null;
        List<Thing> load = LoadKinds(deliver);
        List<UnloadPlace> places = VehicleUnloadBays.MarkedPlaces(map);
        IntVec3 from = Vehicle.PositionHeld;
        var candidates = new List<BayCandidate>(places.Count);
        foreach (UnloadPlace place in places)
        {
            bool passed = passedBays.Any(p => p.Same(place));
            bool takes = false;
            bool room = false;
            foreach (Thing thing in load)
            {
                if (!place.Accepts(thing)) continue;
                takes = true;
                if (passed) break;
                if (place.HasRoomFor(thing, map))
                {
                    room = true;
                    break;
                }
            }
            candidates.Add(new BayCandidate(place.DistanceSquaredFrom(from), takes, room, passed));
        }
        int best = UnloadBayRule.Nearest(candidates);
        if (best >= 0)
        {
            why = BayShortfall.None;
            return places[best];
        }
        why = UnloadBayRule.WhyNone(candidates);
        return null;
    }

    static string ShortfallLine(BayShortfall why) => why switch
    {
        BayShortfall.NoneMarked => "PhobosBays_NoneMarked".Translate().Resolve(),
        BayShortfall.NoneTakes => "PhobosBays_NoneTakes".Translate().Resolve(),
        BayShortfall.NoneWithRoom => "PhobosBays_NoneWithRoom".Translate().Resolve(),
        _ => "PhobosBays_NoWayToAny".Translate().Resolve(),
    };

    /// <summary>
    /// Makes for a bay, this one or the nearest with room: a bay it finds no way to back up to is passed over for the next. With none left,
    /// a delivery finishes and a gathering run ends.
    /// </summary>
    void GoToBay(UnloadPlace? first)
    {
        UnloadPlace? next = first;
        BayShortfall why = BayShortfall.None;
        for (int tries = 0; tries < MaxBayTries; tries++)
        {
            next ??= PickBay(delivering, out why);
            if (next is not { } bay) break;
            SetBay(bay);
            if (HeadFor(bay.Cells, bay.BackUpFacing()))
            {
                string variant = bay.Zone != null ? (delivering ? "deliver zone" : "gather zone") : (delivering ? "deliver building" : "gather building");
                if (Seen.Wants("vehicles.unload-bay.pick", Vehicle, variant)) Seen.Note("vehicles.unload-bay.pick", Vehicle, bay.Label, variant);
                return;
            }
            PassBay();
            next = null;
        }
        NoBayLeft(why);
    }

    void NoBayLeft(BayShortfall why)
    {
        ClearBay();
        if (delivering) FinishDelivery(UnloadBayRule.DeliveryEnd(AnythingForBays(true)), ShortfallLine(why));
        else End(GatherRunEnd.NoBay, ShortfallLine(why));
    }

    /// <summary>
    /// After a tip at a bay: on to the next bay while something aboard is for one that takes it; otherwise a delivery finishes, and a gathering
    /// run goes back out or ends by the usual rule (GatherRule.AfterUnload).
    /// </summary>
    void AfterBayUnload(UnloadEnd end, string? reason)
    {
        bool last = lastTrip;
        if (end == UnloadEnd.PlaceFull) PassBay();
        LookAgain();
        badSpots.Clear();
        failedDrives = 0;
        if (UnloadBayRule.LooksForAnother(end) && AnythingForBays(delivering) && PickBay(delivering, out _) is { } next)
        {
            string variant = delivering ? "deliver" : "gather";
            if (Seen.Wants("vehicles.unload-bay.next", Vehicle, variant)) Seen.Note("vehicles.unload-bay.next", Vehicle, next.Label, variant);
            lastTrip = last;
            GoToBay(next);
            return;
        }
        ClearBay();
        if (delivering)
        {
            if (UnloadBayRule.LooksForAnother(end)) FinishDelivery(UnloadBayRule.DeliveryEnd(AnythingForBays(true)), null);
            else End(GatherRule.AfterUnload(end, false, true) ?? GatherRunEnd.Fault, reason);
            return;
        }
        bool roomLeft = Worker is { GatherRoomKilograms: > 0f };
        if (GatherRule.AfterUnload(end, last, roomLeft) is { } runEnd) End(runEnd, runEnd is GatherRunEnd.UnloadFull or GatherRunEnd.Fault ? reason : null);
    }

    // ---- Deliver ----

    /// <summary>
    /// Ends a delivery, driving back to where it started first if the player chose that and it delivered or ran out of bays (UnloadBayRule.DrivesBack).
    /// </summary>
    void FinishDelivery(GatherRunEnd how, string? reason)
    {
        if (how == GatherRunEnd.Delivered) reason = null;
        bool away = homeCell.IsValid && Vehicle.Position != homeCell;
        if (!UnloadBayRule.DrivesBack(!deliverStays, away, how) || !Vehicle.Drafted)
        {
            End(how, reason);
            return;
        }
        deliveryEnd = how;
        deliveryReason = reason;
        if (!VehicleAutoDrive.DriveTo(Vehicle, homeCell, homeFacing.IsValid ? homeFacing : null))
        {
            End(how, Then(how, reason, "PhobosDeliver_NoWayHome"));
            return;
        }
        standCell = homeCell;
        phase = GatherPhase.Homing;
        lastMovedTick = Find.TickManager.TicksGame;
        lastPosition = Vehicle.Position;
    }

    /// <summary>A delivery's own line, or the reason it gave, then a line about the drive back.</summary>
    string Then(GatherRunEnd how, string? reason, string homeKey)
    {
        string first = reason ?? WhyLine(how, Vehicle.LabelShort.Named("VEHICLE"));
        return first + " " + homeKey.Translate().Resolve();
    }

    /// <summary>One look on the drive back: on its way it waits; there, it faces the way it started and ends; stopped short or stuck, it ends where it is.</summary>
    void HomeTick()
    {
        GatherRunEnd how = deliveryEnd == GatherRunEnd.None ? GatherRunEnd.Delivered : deliveryEnd;
        if (!Vehicle.Drafted)
        {
            End(GatherRunEnd.EngineOff);
            return;
        }
        if (VehicleAutoDrive.HeadingElsewhere(Vehicle, standCell))
        {
            End(GatherRunEnd.Orders);
            return;
        }
        int now = Find.TickManager.TicksGame;
        if (Vehicle.Position != lastPosition)
        {
            lastPosition = Vehicle.Position;
            lastMovedTick = now;
        }
        if (VehicleAutoDrive.HeadingFor(Vehicle, standCell) || VehicleAutoDrive.Moving(Vehicle))
        {
            if (!GatherRule.Stuck(now - lastMovedTick, Props.stuckTicks)) return;
            VehicleAutoDrive.Halt(Vehicle, standCell);
            End(how, Then(how, deliveryReason, "PhobosDeliver_NoWayHome"));
            return;
        }
        if (Vehicle.Position != homeCell)
        {
            End(how, Then(how, deliveryReason, "PhobosDeliver_NoWayHome"));
            return;
        }
        if (homeFacing.IsValid) VehicleAutoDrive.Face(Vehicle, homeFacing);
        Seen.Note("vehicles.deliver.home", Vehicle);
        End(how, Then(how, deliveryReason, "PhobosDeliver_Home"));
    }

    /// <summary>Why a delivery cannot start now, or null.</summary>
    public string? CannotDeliver() => CannotDeliver(needsDriver: true);

    /// <summary>Why a delivery cannot start now, or null; with <paramref name="needsDriver"/> false, as if a driver were aboard.</summary>
    string? CannotDeliver(bool needsDriver)
    {
        NamedArgument name = Vehicle.LabelShort.Named("VEHICLE");
        if (!CanUnload) return "PhobosDeliver_NoTip".Translate(name).Resolve();
        if (!Vehicle.Spawned) return "PhobosAutoDrive_NotOnMap".Translate().Resolve();
        if (!AnythingForBays(true)) return "PhobosDeliver_NothingAboard".Translate(name).Resolve();
        if (StopsForEnemies && GenHostility.AnyHostileActiveThreatToPlayer(Vehicle.Map)) return "PhobosGather_EnemiesNear".Translate().Resolve();
        if (VehicleAutoDrive.CannotDrive(Vehicle, needsDriver) is { } cannot) return cannot;
        if (PickBay(true, out BayShortfall why) == null) return ShortfallLine(why);
        return null;
    }

    /// <summary>
    /// <see cref="CannotDeliver(bool)"/> for the button, as if a driver were aboard (with none, pressing it calls one), asked at most twice
    /// a second of real time: it tests every marked bay.
    /// </summary>
    string? CannotDeliverForButton()
    {
        float now = UnityEngine.Time.realtimeSinceStartup;
        if (deliverCheckedAt < 0f || now - deliverCheckedAt > ButtonRecheckSeconds)
        {
            deliverRefusal = CannotDeliver(needsDriver: false);
            deliverCheckedAt = now;
        }
        return deliverRefusal;
    }

    /// <summary>Whether anything on the map is marked, for the buttons, asked at most twice a second of real time.</summary>
    bool AnyBayMarkedCached()
    {
        float now = UnityEngine.Time.realtimeSinceStartup;
        if (bayCheckedAt < 0f || now - bayCheckedAt > ButtonRecheckSeconds)
        {
            anyBayMarked = Vehicle.MapHeld is { } map && VehicleUnloadBays.AnyMarked(map);
            bayCheckedAt = now;
        }
        return anyBayMarked;
    }

    public void StartDelivery()
    {
        if (Running) return;
        StopWaiting(false);
        if (CannotDeliver() is { } refused)
        {
            Messages.Message(refused, Vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        // Vehicle Framework says why if it will not start.
        if (!VehicleAutoDrive.StartEngine(Vehicle, out bool started)) return;
        engineStarted = started;
        VehicleAutoDrive.ClearOrders(Vehicle);
        ResetRun();
        delivering = true;
        deliveryEnd = GatherRunEnd.None;
        deliveryReason = null;
        homeCell = Vehicle.Position;
        homeFacing = Vehicle.Rotation;
        phase = GatherPhase.Seeking;
        Messages.Message("PhobosDeliver_Started".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), Vehicle, MessageTypeDefOf.SilentInput,
            historical: false);
        Seen.Note("vehicles.deliver.start", Vehicle, variant: deliverStays ? "stays" : "drives back");
        BeginReturn(true);
    }

    void SetDeliverStays(bool stays)
    {
        deliverStays = stays;
        Messages.Message((stays ? "PhobosDeliver_StaysSet" : "PhobosDeliver_DrivesBackSet").Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(),
            Vehicle, MessageTypeDefOf.SilentInput, historical: false);
    }

    IEnumerable<Gizmo> DeliverGizmos()
    {
        NamedArgument name = Vehicle.LabelShort.Named("VEHICLE");
        bool on = Running && delivering;
        bool waiting = waitingFor == ErrandWaiting.Deliver;
        string then = (deliverStays ? "PhobosDeliver_ThenStay" : "PhobosDeliver_ThenDriveBack").Translate().Resolve();
        var deliver = new Command_Action
        {
            defaultLabel = (on ? "PhobosDeliver_GizmoStop" : waiting ? "PhobosDriverCall_StopWaiting" : "PhobosDeliver_Gizmo").Translate().Resolve(),
            defaultDesc = "PhobosDeliver_GizmoDesc".Translate(name, then.Named("THEN")).Resolve() + "\n\n" + DriverCalls.WaitsLine,
            icon = SystemIcons.For(DeliverIconPath),
            action = () =>
            {
                if (on) Stop();
                else if (waiting) StopWaiting(true);
                else OrderDelivery();
            },
        };
        if (Running && !delivering) deliver.Disable("PhobosDeliver_BusyGathering".Translate(name).Resolve());
        else if (BusyWaiting(ErrandWaiting.Deliver) is { } busy) deliver.Disable(busy);
        else if (!on && !waiting && CannotDeliverForButton() is { } refused) deliver.Disable(refused);
        yield return deliver;
        yield return new Command_Toggle
        {
            defaultLabel = "PhobosDeliver_DriveBack".Translate().Resolve(),
            defaultDesc = "PhobosDeliver_DriveBackDesc".Translate(name).Resolve(),
            icon = SystemIcons.For(DriveBackIconPath),
            isActive = () => !deliverStays,
            toggleAction = () => SetDeliverStays(!deliverStays),
        };
    }

    // ---- Waiting for a driver (Framework 0.177.0; docs/development/vehicles-at-work-tie-ins.md, VW3) ----

    /// <summary>The errand ordered while nobody was aboard to drive, or None. Saved by name: append only.</summary>
    ErrandWaiting waitingFor;

    /// <summary>Whether it waits for a driver now: an errand ordered, none running, and not enough crew aboard to drive.</summary>
    public bool WantsDriver => waitingFor != ErrandWaiting.None && !Running && Vehicle.Spawned && Vehicle.Faction == Faction.OfPlayer
        && !Vehicle.HasEnoughOperators;

    string WaitingWhat(ErrandWaiting what) =>
        (what == ErrandWaiting.Deliver ? "PhobosDriverCall_WhatDeliver" : "PhobosDriverCall_WhatGather").Translate().Resolve();

    /// <summary>Calls for a driver: a colonist with Drive work comes, and the errand starts once the crew is aboard.</summary>
    void WaitForDriver(ErrandWaiting what)
    {
        waitingFor = what;
        DriverCalls.Call(Vehicle.Map, this, true);
        Messages.Message("PhobosDriverCall_Waiting".Translate(Vehicle.LabelShort.Named("VEHICLE"), DriverCalls.WorkLabel.Named("WORK"),
            WaitingWhat(what).Named("WHAT")).Resolve(), Vehicle, MessageTypeDefOf.SilentInput, historical: false);
        Seen.Note("vehicles.driver-call.waiting", Vehicle, variant: what == ErrandWaiting.Deliver ? "deliver" : "gather");
    }

    /// <summary>Stops waiting, saying so when the player asked.</summary>
    void StopWaiting(bool say)
    {
        if (waitingFor == ErrandWaiting.None) return;
        waitingFor = ErrandWaiting.None;
        DriverCalls.Call(Vehicle.MapHeld, this, false);
        if (say)
        {
            Messages.Message("PhobosDriverCall_Cancelled".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), Vehicle,
                MessageTypeDefOf.SilentInput, historical: false);
        }
    }

    /// <summary>One look while waiting: once the crew is aboard, whoever brought them, the errand starts; it says why if it cannot.</summary>
    void WaitTick()
    {
        if (waitingFor == ErrandWaiting.None || !Vehicle.HasEnoughOperators) return;
        ErrandWaiting what = waitingFor;
        StopWaiting(false);
        if (what == ErrandWaiting.Deliver) StartDelivery();
        else Start();
    }

    /// <summary>Deliver pressed: at once with a driver aboard; otherwise, if nothing else stands in the way, it waits for one.</summary>
    void OrderDelivery()
    {
        if (Vehicle.HasEnoughOperators)
        {
            StartDelivery();
            return;
        }
        if (CannotDeliver(needsDriver: false) is { } refused)
        {
            Messages.Message(refused, Vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        WaitForDriver(ErrandWaiting.Deliver);
    }

    /// <summary>Gather pressed: as Deliver.</summary>
    void OrderGather()
    {
        if (Vehicle.HasEnoughOperators)
        {
            Start();
            return;
        }
        if (CannotStart(needsDriver: false) is { } refused)
        {
            Messages.Message(refused, Vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        WaitForDriver(ErrandWaiting.Gather);
    }

    /// <summary>Why the other errand's button is greyed out while one waits, or null.</summary>
    string? BusyWaiting(ErrandWaiting button) => waitingFor != ErrandWaiting.None && waitingFor != button
        ? "PhobosDriverCall_BusyWaiting".Translate(Vehicle.LabelShort.Named("VEHICLE"), WaitingWhat(waitingFor).Named("WHAT")).Resolve()
        : null;

    string? WaitingLine => waitingFor == ErrandWaiting.None ? null
        : "PhobosDriverCall_Line".Translate(WaitingWhat(waitingFor).Named("WHAT")).Resolve();

    // ---- The save ----

    void ExposeBays(bool saving)
    {
        Scribe_Values.Look(ref waitingFor, "PhobosGatherWaitingFor", ErrandWaiting.None);
        if (saving) _ = Bay;
        Scribe_Values.Look(ref anyBay, "PhobosGatherUnloadAnyBay");
        Scribe_Values.Look(ref delivering, "PhobosGatherDelivering");
        Scribe_Values.Look(ref deliverStays, "PhobosDeliverStays");
        if (!saving || bayBuilding != null) Scribe_References.Look(ref bayBuilding, "PhobosGatherBayBuilding");
        if (!saving || bayZone != null) Scribe_References.Look(ref bayZone, "PhobosGatherBayZone");
        Scribe_Values.Look(ref homeCell, "PhobosDeliverHomeCell", IntVec3.Invalid);
        Scribe_Values.Look(ref homeFacing, "PhobosDeliverHomeFacing", Rot4.Invalid);
        // The reason line is words in the player's language, so it is not saved: after a load the drive back reports the end's own line.
        Scribe_Values.Look(ref deliveryEnd, "PhobosDeliverEnd", GatherRunEnd.None);
    }
}
