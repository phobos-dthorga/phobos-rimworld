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
        double towedKg = CompPhobosSling.LoadKilograms(candidate);
        double tractorKg = CompPhobosSling.LoadKilograms(Vehicle);
        if (TrailerProps(candidate) == null && !RecoveryRule.CanTow(tractorKg, towedKg, Rules.recoveryMaxTowRatio))
        {
            outlook = TowRule.TooHeavy(tractorKg, towedKg);
        }
        else if (FullModel && Vehicle.Spawned && Systems?.Powertrain is { } train)
        {
            outlook = train.ForecastTow(towedKg);
        }
        else
        {
            outlook = TowRule.Plain(tractorKg, towedKg, DrivetrainModel.CrawlFloor);
        }
        return outlook;
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
        TrailerPose ideal = HitchRule.AxleFor(hx, hz, VehicleHeading(Vehicle), props.HitchToAxle);
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
        if (!Place(towed, at, map, rotation)) return;
        // Its pose is where it really stands, pointing at the hitch: the follow rule draws it into line as soon as the vehicle moves.
        (double tx, double tz) = HitchRule.TrueCentre(at.x, at.z, size.x, size.z, facing);
        double heading = HitchRule.HeadingOf(hx - tx, hz - tz);
        // AxleFor puts the axle its length behind a point along the heading: from the centre, that is the axle (CentreOf's inverse).
        Remember(HitchRule.AxleFor(tx, tz, heading, props.AxleToCentre));
        settle = SettleTicks;
        needsLook = true;
        weightTick = -1;
    }

    /// <summary>A towed thing with no tow geometry left (its Def changed): it comes down near the vehicle, let go.</summary>
    void PlaceAnywhere(VehiclePawn towed, Map map)
    {
        if (!FindStand(Vehicle.Position, Vehicle.Rotation, towed, map, out IntVec3 at)) return;
        if (Place(towed, at, map, Vehicle.Rotation)) Unhitch(quiet: true);
    }

    /// <summary>Takes the towed vehicle out of the holder and onto the map. If the game refuses partway, one that never reached the map goes back
    /// into the holder, so it is never left in neither place; the player is told.</summary>
    bool Place(VehiclePawn towed, IntVec3 cell, Map map, Rot4 rotation)
    {
        Held.Remove(towed);
        try
        {
            GenSpawn.Spawn(towed, cell, map, rotation);
        }
        catch (System.Exception e)
        {
            VehicleSystemsMod.Logger.Error($"Setting {towed.LabelShort} down behind {Vehicle.LabelShort} at {cell} failed: {e}");
            if (!towed.Spawned && !towed.Destroyed && !Held.Contains(towed)) Held.TryAdd(towed, canMergeWithExistingStacks: false);
            Say("PhobosHitch_SetDownFailed".Translate(towed.LabelShort.Named("TRAILER")).Resolve(), Vehicle, MessageTypeDefOf.NegativeEvent);
            nextSetDownTry = Find.TickManager.TicksGame + SetDownRetryTicks;
            return false;
        }
        return true;
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
            CellRect rect = GenAdj.OccupiedRect(cell, rotation, towed.def.Size);
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

    /// <summary>The vehicle held off the map behind this one, if any: a caravan's world-map lists show it after its tractor.</summary>
    public static VehiclePawn? TowedAlong(VehiclePawn? vehicle) =>
        vehicle?.GetComp<CompPhobosHitch>() is { HoldsTowed: true } hitch ? hitch.trailer : null;

    void ExposeHeld()
    {
        Scribe_Deep.Look(ref held, "PhobosHitch_Held", this);
        Scribe_Values.Look(ref nextSetDownTry, "PhobosHitch_NextSetDownTry");
    }
}
