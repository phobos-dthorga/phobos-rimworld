using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Hitch slice 3 (Framework 0.104.0; docs/development/vehicle-hitch-design.md, "Reading it"): the forecast before a tow, and the tow on the
/// world map. A vehicle that leaves the map towing (a caravan, a garage, any journey) takes what it tows with it, held in this comp off the
/// map as the sling holds its load; its weight stays on the drivetrain model, so the caravan's speed and fuel feel it. When the vehicle is
/// next put on a map, the trailer is set down behind it at its next tick (never from inside its spawning: AGENTS.md), still hitched.
/// </summary>
public sealed partial class CompPhobosHitch : IThingHolder
{
    /// <summary>How long a forecast is kept for the same candidate while the player chooses (the mouse label asks every frame).</summary>
    const int OutlookTicks = 60;
    /// <summary>How long to wait before trying again to set a held trailer down when there was no room.</summary>
    const int SetDownRetryTicks = 250;

    ThingOwner<VehiclePawn>? held;
    VehiclePawn? outlookFor;
    int outlookTick = -1;
    TowVerdict outlook;
    int nextSetDownTry;

    ThingOwner<VehiclePawn> Held => held ??= new ThingOwner<VehiclePawn>(this, oneStackOnly: true);

    /// <summary>Whether what this vehicle tows travels with it off the map now.</summary>
    public bool HoldsTowed => trailer != null && held != null && held.Contains(trailer);

    // ---- "Can this pull that?" ----

    /// <summary>
    /// The forecast for towing <paramref name="candidate"/> from where this vehicle stands: refused over the recovery tow's weight ratio for a
    /// vehicle that is not a trailer; the drivetrain model's answer on the full model; the plain rule otherwise. Kept a second for the same
    /// candidate, since the mouse label asks every frame.
    /// </summary>
    public TowVerdict Outlook(VehiclePawn candidate)
    {
        int now = Find.TickManager.TicksGame;
        if (candidate == outlookFor && outlookTick >= 0 && now - outlookTick < OutlookTicks) return outlook;
        outlookFor = candidate;
        outlookTick = now;
        // Framework 0.193.0: in a chain, the vehicle in front drives and pulls all of it, the candidate and what it tows besides.
        CompPhobosHitch front = HeadOf(Vehicle).GetComp<CompPhobosHitch>() ?? this;
        double towedKg = ChainKilograms(candidate);
        if (front != this && front.trailer != null) towedKg += ChainKilograms(front.trailer);
        double tractorKg = CompPhobosSling.LoadKilograms(front.Vehicle);
        if (TrailerProps(candidate) == null && !RecoveryRule.CanTow(tractorKg, towedKg, Rules.recoveryMaxTowRatio))
        {
            outlook = TowRule.TooHeavy(tractorKg, towedKg);
        }
        else if (front.FullModel && front.Vehicle.Spawned && front.Systems?.Powertrain is { } train)
        {
            outlook = train.ForecastTow(towedKg);
        }
        else
        {
            outlook = TowRule.Plain(tractorKg, towedKg, DrivetrainModel.CrawlFloor);
        }
        // The same tow with the candidate working in the ground (the fault-read of 21 Nov 2026, H7: the forecast never counted an
        // implement's draught, so a small tractor "pulled" a plough at full speed and then crawled once it was lowered).
        double draught = Draughts.Working(candidate);
        workingOutlook = draught > 0 && front.FullModel && front.Vehicle.Spawned && front.Systems?.Powertrain is { } working
            ? working.ForecastTow(towedKg, draught)
            : null;
        return outlook;
    }

    TowVerdict? workingOutlook;

    /// <summary>The forecast for the candidate last asked about while it works in the ground, or null when it drags nothing.</summary>
    public TowVerdict? WorkingOutlook(VehiclePawn candidate)
    {
        Outlook(candidate);
        return workingOutlook;
    }

    // ---- Off the map ----

    /// <summary>The vehicle is leaving the map, not destroyed (a destroyed vehicle lets go first, in OnDestroy): what it tows goes with it.
    /// Called from OnDeSpawn.</summary>
    void TakeAlong()
    {
        VehiclePawn towed = trailer!;
        Map map = towed.Map;
        IntVec3 cell = towed.Position;
        Rot4 rotation = towed.Rotation;
        towed.vehiclePather?.StopDead();
        towed.Transform.rotation = 0f;
        towed.DeSpawn();
        if (!Held.TryAdd(towed, canMergeWithExistingStacks: false))
        {
            // Never lost to a holder that would not take it: it stays where it stood, let go, and the player is told.
            GenSpawn.Spawn(towed, cell, map, rotation);
            Unhitch(quiet: true);
            if (Vehicle.Faction == Faction.OfPlayer) Say("PhobosHitch_LeftBehind".Translate(towed.LabelShort.Named("TRAILER")).Resolve(), towed, MessageTypeDefOf.NeutralEvent);
            return;
        }
        Vehicle.RequestTickStop(this);
    }

    /// <summary>The vehicle is on a map again with what it towed still held: set it down behind, at the next tick (CompTick).</summary>
    void SetDownHeld()
    {
        VehiclePawn towed = trailer!;
        Map map = Vehicle.Map;
        if (Geometry(towed) is not { } props)
        {
            PlaceAnywhere(towed, map);
            return;
        }
        int now = Find.TickManager.TicksGame;
        if (now < nextSetDownTry) return;
        (double hx, double hz) = HitchPoint();
        TrailerPose ideal = HitchRule.AxleFor(hx, hz, Heading(), props.HitchToAxle);
        (double cx, double cz) = HitchRule.CentreOf(ideal, props.AxleToCentre);
        int facing = HitchRule.Cardinal(ideal.Heading);
        IntVec2 size = towed.def.Size;
        (int x, int z) = HitchRule.CellFor(cx, cz, size.x, size.z, facing);
        var rotation = new Rot4(facing);
        if (!FindStand(new IntVec3(x, 0, z), rotation, towed, map, out IntVec3 at))
        {
            // No room near the vehicle yet (a crowded arrival): it stays on the tow bar, off the map, and is tried again shortly.
            nextSetDownTry = now + SetDownRetryTicks;
            if (Vehicle.Faction == Faction.OfPlayer) Say("PhobosHitch_NoRoomToSetDown".Translate(towed.LabelShort.Named("TRAILER"), Vehicle.LabelShort.Named("TRACTOR")).Resolve(), Vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        // Its pose is where it really stands, pointing at the hitch: the follow rule draws it into line as soon as the vehicle moves.
        (double tx, double tz) = HitchRule.TrueCentre(at.x, at.z, size.x, size.z, facing);
        double heading = HitchRule.HeadingOf(hx - tx, hz - tz);
        // AxleFor puts the axle its length behind a point along the heading: from the centre, that is the axle (CentreOf's inverse).
        TrailerPose stand = HitchRule.AxleFor(tx, tz, heading, props.AxleToCentre);
        // Framework 0.120.5 (fault-read H3): set down beside the vehicle at a map edge, the follow rule's first step drew it into line off
        // the map, and the vehicle was stopped each time it moved. Where that first step has no room, it is set down let go, and the player
        // told why; hitched again later, it follows from where it stands.
        TrailerPose first = HitchRule.Limit(hx, hz, HitchRule.Follow(hx, hz, stand, props.HitchToAxle), Heading(), props.JackKnife, props.HitchToAxle);
        bool follows = PoseFits(first, props, towed, map);
        if (!Place(towed, at, map, rotation)) return;
        // Seen in play (Framework 0.301.0, row 70): a held trailer set down on arrival beside its vehicle, following or let go.
        Seen.Note("vehicles.trailer-arrived", towed, Vehicle.LabelShort, follows ? "behind" : "let-go");
        Remember(stand);
        settle = SettleTicks;
        needsLook = true;
        weightTick = -1;
        // A half of an articulated vehicle is never let go (the fault-read of 21 Nov 2026, H5): it stays joined, beside its front half
        // for now, and is drawn into line as soon as there is room.
        if (!follows && !Couplings.Permanent(towed))
        {
            Unhitch(quiet: true);
            if (Vehicle.Faction == Faction.OfPlayer)
            {
                Say("PhobosHitch_SetDownLetGo".Translate(towed.LabelShort.Named("TRAILER"), Vehicle.LabelShort.Named("TRACTOR")).Resolve(),
                    new LookTargets(Vehicle, towed), MessageTypeDefOf.NeutralEvent);
            }
        }
    }

    /// <summary>A towed thing with no tow geometry left (its Def changed): it comes down near the vehicle, let go.</summary>
    void PlaceAnywhere(VehiclePawn towed, Map map)
    {
        // Tried again only after a pause, as the set-down behind is (fault-read H10): it looked for room every tick.
        int now = Find.TickManager.TicksGame;
        if (now < nextSetDownTry) return;
        if (!FindStand(Vehicle.Position, Vehicle.Rotation, towed, map, out IntVec3 at))
        {
            nextSetDownTry = now + SetDownRetryTicks;
            return;
        }
        if (Place(towed, at, map, Vehicle.Rotation) && !Couplings.Permanent(towed)) Unhitch(quiet: true);
    }

    /// <summary>Takes the towed vehicle out of the holder and onto the map. If the game refuses partway, one that never reached the map goes back
    /// into the holder, so it is never left in neither place; the player is told.</summary>
    bool Place(VehiclePawn towed, IntVec3 cell, Map map, Rot4 rotation)
    {
        Held.Remove(towed);
        string? failure = null;
        try
        {
            GenSpawn.Spawn(towed, cell, map, rotation);
            // The game can also refuse without throwing (fault-read H10): it is set down only if it really stands on the map.
            if (!towed.Spawned) failure = "the game did not put it on the map";
            // Vehicle Framework moves an even-sided vehicle facing south or west a cell as it spawns it (the fault-read of 21 Nov 2026,
            // H10): it is put back on the cells that were found clear for it.
            else if (towed.Position != cell && towed.VehicleRect(cell, rotation).InBounds(map)) towed.Position = cell;
        }
        catch (System.Exception e)
        {
            failure = e.ToString();
        }
        if (failure == null) return true;
        VehicleSystemsMod.Logger.Error($"Setting {towed.LabelShort} down behind {Vehicle.LabelShort} at {cell} failed: {failure}");
        if (!towed.Spawned && !towed.Destroyed && !Held.Contains(towed)) Held.TryAdd(towed, canMergeWithExistingStacks: false);
        Say("PhobosHitch_SetDownFailed".Translate(towed.LabelShort.Named("TRAILER")).Resolve(), Vehicle, MessageTypeDefOf.NegativeEvent);
        nextSetDownTry = Find.TickManager.TicksGame + SetDownRetryTicks;
        return false;
    }

    /// <summary>The nearest cell to <paramref name="ideal"/>, within the hitch's reach, where the towed vehicle stands clear: on walkable ground,
    /// off every other vehicle and off the vehicle towing it.</summary>
    bool FindStand(IntVec3 ideal, Rot4 rotation, VehiclePawn towed, Map map, out IntVec3 found)
    {
        CellRect own = Vehicle.OccupiedRect();
        int count = GenRadial.NumCellsInRadius(Rules.hitchReachCells);
        for (int i = 0; i < count; i++)
        {
            IntVec3 cell = ideal + GenRadial.RadialPattern[i];
            // Vehicle Framework's own footprint (Framework 0.120.5, fault-read H1): south reads as north and west as east, so the game's rect
            // was a cell out for an even-length trailer, and one set down on the north or east edge was destroyed as it spawned.
            CellRect rect = towed.VehicleRect(cell, rotation);
            if (rect.Overlaps(own) || !Clear(rect, map, towed)) continue;
            found = cell;
            return true;
        }
        found = IntVec3.Invalid;
        return false;
    }

    // ---- Holding ----

    public ThingOwner GetDirectlyHeldThings() => Held;

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        if (held != null) ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, held);
    }

    /// <summary>Lets go of the trailer held off the map and hands it over (Framework 0.199.0: left at a depot by a convoy), or null.</summary>
    internal VehiclePawn? LetGoHeld()
    {
        if (!HoldsTowed) return null;
        VehiclePawn towed = trailer!;
        Held.Remove(towed);
        Unhitch(quiet: true);
        return towed;
    }

    /// <summary>The vehicle held off the map behind this one, if any: a caravan's world-map lists show it after its tractor.</summary>
    public static VehiclePawn? TowedAlong(VehiclePawn? vehicle) =>
        vehicle?.GetComp<CompPhobosHitch>() is { HoldsTowed: true } hitch ? hitch.trailer : null;

    /// <summary>As <see cref="TowedAlong"/>, but only what can be left behind by itself: never a half of an articulated vehicle.</summary>
    public static VehiclePawn? LeavableTowedAlong(VehiclePawn? vehicle) => TowedAlong(vehicle) is { } towed && !Couplings.Permanent(towed) ? towed : null;

    void ExposeHeld()
    {
        Scribe_Deep.Look(ref held, "PhobosHitch_Held", this);
        Scribe_Values.Look(ref nextSetDownTry, "PhobosHitch_NextSetDownTry");
    }
}
