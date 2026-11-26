using System.Collections.Generic;
using System.Linq;
using Phobos.Framework;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using RimWorld.Planet;
using SmashTools.Targeting;
using UnityEngine;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

// An aircraft's round between places (Framework 0.307.0; docs/development/autonomous-vehicles.md, step 9; the owner's cards of 26 Nov 2026):
// stops picked on the world map, a landing spot on each place with a map, a wait set per stop; flown by a pilot aboard, whatever is loaded.

/// <summary>One stop of a round: a tile, and on a place with a map the spot it lands on there and the way it faces.</summary>
public sealed class RoundStop : IExposable
{
    public PlanetTile tile = PlanetTile.Invalid;
    /// <summary>The place, when it had a map as the stop was picked; null for a tile with no map, where it lands as a caravan.</summary>
    public MapParent? parent;
    public IntVec3 cell = IntVec3.Invalid;
    public Rot4 rot = Rot4.North;
    public int waitTicks = RoundRule.WaitTicks(RoundRule.DefaultWaitHours);

    /// <summary>Whether it lands on a map here: the place still has one and a spot was picked on it.</summary>
    public bool OnMap => parent is { HasMap: true } && cell.IsValid;

    public string Label => parent != null ? parent.LabelCap : (string)"PhobosRound_TileLabel".Translate(Find.WorldGrid.LongLatOf(tile).x.ToStringLongitude().Named("LONG"),
        Find.WorldGrid.LongLatOf(tile).y.ToStringLatitude().Named("LAT"));

    public void ExposeData()
    {
        Scribe_Values.Look(ref tile, "tile", PlanetTile.Invalid);
        Scribe_References.Look(ref parent, "parent");
        Scribe_Values.Look(ref cell, "cell", IntVec3.Invalid);
        Scribe_Values.Look(ref rot, "rot", Rot4.North);
        Scribe_Values.Look(ref waitTicks, "waitTicks", RoundRule.WaitTicks(RoundRule.DefaultWaitHours));
    }
}

/// <summary>Lets an aircraft on our flight fly a round between places. It takes no figures (RoundRule's); D'thorga Framework gives it to every
/// aircraft on our flight that flies on the map.</summary>
public class CompProperties_PhobosRound : CompProperties
{
    public CompProperties_PhobosRound() => compClass = typeof(CompPhobosRound);
}

/// <summary>
/// An aircraft flying a round (Framework 0.307.0). At each stop it waits that stop's time, for colonists to unload, load and refuel it, then
/// takes off for the next once Vehicle Framework would let it launch (a pilot aboard, not overloaded, not under a roof) and it has the fuel
/// for the leg with a sixth to spare; otherwise it waits there and says why once. A place with a map it lands on at the spot picked; a tile
/// with none it lands on as a caravan. Vehicle Framework flies each leg; the round only orders it.
/// <para>Cost: nothing while no round is flown; while one is, a look every 250 ticks on a map (a few comparisons), and the same for a caravan
/// from <see cref="WorldComponent_PhobosRounds"/>; nothing in flight.</para>
/// </summary>
public sealed class CompPhobosRound : VehicleComp
{
    const int LookTicks = 250;
    List<RoundStop> stops = new();
    bool running;
    /// <summary>The stop it is at, or flying to.</summary>
    int at = -1;
    int waitUntil = -1;
    /// <summary>Why it waits past its time, told once when it starts; derived at each look, never saved.</summary>
    string? waitingWhy;

    public IReadOnlyList<RoundStop> Stops => stops;
    public bool Running => running;

    // ---- Planning (the picker calls these) ----

    /// <summary>Why this place may not be the next stop, or null: the most stops already, the same place as the last, or an enemy's town.</summary>
    public string? AddStopRefusal(GlobalTargetInfo target)
    {
        if (!target.IsValid || !target.Tile.Valid) return "PhobosRound_RefuseNoPlace".Translate().Resolve();
        if (!RoundRule.MayAdd(stops.Count, stops.Count > 0 && stops[stops.Count - 1].tile == target.Tile))
            return stops.Count >= RoundRule.MostStops ? "PhobosRound_RefuseTooMany".Translate(RoundRule.MostStops.Named("MOST")).Resolve()
                : "PhobosRound_RefuseSameAsLast".Translate().Resolve();
        if (target.WorldObject is Settlement { Faction: { } faction } && faction != Faction.OfPlayer && faction.HostileTo(Faction.OfPlayer))
            return "PhobosRound_RefuseHostile".Translate(faction.Name.Named("FACTION")).Resolve();
        return null;
    }

    /// <summary>Starts a new plan: the round is stopped and cleared, and where it stands now is its first stop.</summary>
    public void BeginPlan()
    {
        running = false;
        stops.Clear();
        at = -1;
        waitUntil = -1;
        waitingWhy = null;
        if (Vehicle.Spawned) AddStop(Vehicle.Map.Tile, Vehicle.Map.Parent, Vehicle.Position, Vehicle.Rotation);
    }

    public void AddStop(PlanetTile tile, MapParent? parent, IntVec3 cell, Rot4 rot)
    {
        stops.Add(new RoundStop { tile = tile, parent = parent, cell = cell, rot = rot });
        Messages.Message("PhobosRound_StopAdded".Translate(Vehicle.LabelShort.Named("VEHICLE"), stops.Count.Named("INDEX"), stops[stops.Count - 1].Label.Named("PLACE")),
            MessageTypeDefOf.SilentInput, false);
    }

    public void SetWait(int index, float hours)
    {
        if (index < 0 || index >= stops.Count) return;
        stops[index].waitTicks = RoundRule.WaitTicks(hours);
        if (index == at && waitUntil >= 0) waitUntil = Find.TickManager.TicksGame + stops[index].waitTicks;
    }

    /// <summary>Why the round cannot be flown, or null.</summary>
    public string? StartRefusal()
    {
        if (!RouteControl.On) return "PhobosRound_RefuseSettingOff".Translate().Resolve();
        if (!RoundRule.Flyable(stops.Count, stops.Count > 0 && stops[0].tile == stops[stops.Count - 1].tile))
            return "PhobosRound_RefuseTooFew".Translate().Resolve();
        return null;
    }

    public void Start()
    {
        if (StartRefusal() is { } why)
        {
            Messages.Message(why, Vehicle, MessageTypeDefOf.RejectInput, false);
            return;
        }
        running = true;
        at = -1;
        waitUntil = -1;
        waitingWhy = null;
        Messages.Message("PhobosRound_Started".Translate(Vehicle.LabelShort.Named("VEHICLE"), stops.Count.Named("COUNT")), Vehicle, MessageTypeDefOf.SilentInput, false);
    }

    public void Stop()
    {
        running = false;
        waitUntil = -1;
        waitingWhy = null;
        Messages.Message("PhobosRound_Stopped".Translate(Vehicle.LabelShort.Named("VEHICLE")), Vehicle, MessageTypeDefOf.SilentInput, false);
    }

    // ---- Flying it ----

    public override void CompTick()
    {
        base.CompTick();
        if (!running || !Vehicle.Spawned || !Gen.IsHashIntervalTick(Vehicle, LookTicks)) return;
        Look(null);
    }

    /// <summary>A look while it stands as a caravan on a tile (WorldComponent_PhobosRounds).</summary>
    internal void LookInCaravan(VehicleCaravan caravan)
    {
        if (running) Look(caravan);
    }

    void Look(VehicleCaravan? caravan)
    {
        if (!RouteControl.On)
        {
            running = false;
            return;
        }
        // Still in the air over a stop: it lands where it was told, or waits for a hand where its spot was taken.
        if (caravan == null && Vehicle.GetComp<CompPhobosFlight>() is { Airborne: true })
        {
            Waits("PhobosRound_WaitInAir".Translate().Resolve());
            return;
        }
        PlanetTile here = caravan?.Tile ?? Vehicle.Map.Tile;
        if (at < 0 || at >= stops.Count || stops[at].tile != here)
        {
            int found = stops.FindIndex(s => s.tile == here);
            if (found < 0)
            {
                Waits("PhobosRound_WaitNotAtStop".Translate().Resolve());
                return;
            }
            at = found;
            waitUntil = -1;
        }
        int now = Find.TickManager.TicksGame;
        if (waitUntil < 0)
        {
            waitUntil = now + stops[at].waitTicks;
            string variant = caravan != null ? "tile" : "map";
            if (Seen.Wants("aircraft.round-stop", Vehicle, variant)) Seen.Note("aircraft.round-stop", Vehicle, stops[at].Label, variant);
        }
        if (!RoundRule.WaitOver(now, waitUntil)) return;
        int next = RouteRule.Next(at, stops.Count);
        if (LeaveRefusal(stops[next], caravan) is { } why)
        {
            Waits(why);
            return;
        }
        Fly(next, caravan);
    }

    /// <summary>Why it cannot take off for this stop now, or null: Vehicle Framework's own launch check, and the fuel for the leg.</summary>
    string? LeaveRefusal(RoundStop next, VehicleCaravan? caravan)
    {
        if (Vehicle.CompVehicleLauncher is not { } launcher) return "PhobosRound_WaitNoLauncher".Translate().Resolve();
        if (caravan != null && (caravan.LeadVehicle != Vehicle || caravan.VehiclesListForReading.Count > 1))
            return "PhobosRound_WaitCaravanOthers".Translate().Resolve();
        if (!launcher.CanLaunchWithCargoCapacity(out string reason)) return reason;
        if (Vehicle.CompFueledTravel is { } fuel)
        {
            float needed = launcher.FuelNeededToLaunchAtDist(next.tile);
            if (!RoundRule.HasFuelFor(fuel.Fuel, needed))
                return "PhobosRound_WaitFuel".Translate(Mathf.CeilToInt(needed * (float)RoundRule.FuelMargin).Named("NEEDED"), Mathf.FloorToInt(fuel.Fuel).Named("ABOARD"),
                    next.Label.Named("PLACE")).Resolve();
        }
        return null;
    }

    void Fly(int next, VehicleCaravan? caravan)
    {
        RoundStop stop = stops[next];
        var data = new TargetData<GlobalTargetInfo>();
        data.targets.Add(new GlobalTargetInfo(stop.tile));
        // A place whose map has gone since it was picked (abandoned, or a camp packed up) is landed on as a tile.
        IArrivalAction action = stop.OnMap ? new ArrivalAction_LandToCell(Vehicle, stop.parent!, stop.cell, stop.rot) : new ArrivalAction_LandToCaravan(Vehicle);
        at = next;
        waitUntil = -1;
        waitingWhy = null;
        if (Seen.Wants("aircraft.round-leg", Vehicle, stop.OnMap ? "map" : "tile")) Seen.Note("aircraft.round-leg", Vehicle, stop.Label, stop.OnMap ? "map" : "tile");
        if (caravan != null) caravan.Launch(data, action);
        else Vehicle.CompVehicleLauncher.Launch(data, action);
    }

    void Waits(string why)
    {
        if (why == waitingWhy) return;
        waitingWhy = why;
        Messages.Message("PhobosRound_Waits".Translate(Vehicle.LabelShort.Named("VEHICLE"), why.Named("REASON")), Vehicle, MessageTypeDefOf.CautionInput, false);
    }

    // ---- What the player sees ----

    public override string? CompInspectStringExtra()
    {
        if (!running) return stops.Count > 0 ? "PhobosRound_InspectPlanned".Translate(stops.Count.Named("COUNT")).Resolve() : null;
        if (waitingWhy != null) return "PhobosRound_InspectWaiting".Translate(waitingWhy.Named("REASON")).Resolve();
        if (at >= 0 && at < stops.Count && waitUntil >= 0)
        {
            int left = Mathf.Max(0, waitUntil - Find.TickManager.TicksGame);
            return "PhobosRound_InspectAtStop".Translate(stops[at].Label.Named("PLACE"), left.ToStringTicksToPeriod().Named("TIME"),
                stops[RouteRule.Next(at, stops.Count)].Label.Named("NEXT")).Resolve();
        }
        return "PhobosRound_InspectFlying".Translate().Resolve();
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (Vehicle.Faction != Faction.OfPlayer || !RouteControl.On) yield break;
        foreach (Gizmo gizmo in RoundGizmos.For(this, inCaravan: false)) yield return gizmo;
    }

    public override IEnumerable<Gizmo> CompCaravanGizmos()
    {
        foreach (Gizmo gizmo in base.CompCaravanGizmos()) yield return gizmo;
        if (Vehicle.Faction != Faction.OfPlayer || !RouteControl.On || stops.Count == 0) yield break;
        foreach (Gizmo gizmo in RoundGizmos.For(this, inCaravan: true)) yield return gizmo;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Collections.Look(ref stops, "PhobosRound_Stops", LookMode.Deep);
        Scribe_Values.Look(ref running, "PhobosRound_Running");
        Scribe_Values.Look(ref at, "PhobosRound_At", -1);
        Scribe_Values.Look(ref waitUntil, "PhobosRound_WaitUntil", -1);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) stops ??= new List<RoundStop>();
    }
}

/// <summary>Looks at aircraft flying a round that stand as caravans on tiles with no map (Framework 0.307.0), every 250 ticks.</summary>
public class WorldComponent_PhobosRounds : WorldComponent
{
    public WorldComponent_PhobosRounds(World world) : base(world)
    {
    }

    public override void WorldComponentTick()
    {
        base.WorldComponentTick();
        if (Find.TickManager.TicksGame % 250 != 61) return;
        List<Caravan> caravans = Find.WorldObjects.Caravans;
        for (int i = caravans.Count - 1; i >= 0; i--)
        {
            if (caravans[i] is not VehicleCaravan convoy || convoy.Faction != Faction.OfPlayer) continue;
            foreach (VehiclePawn vehicle in convoy.VehiclesListForReading.ToList())
            {
                if (vehicle.GetComp<CompPhobosRound>() is { Running: true } round) round.LookInCaravan(convoy);
            }
        }
    }
}

/// <summary>The round's buttons (Framework 0.307.0). Presentation only: each hands the order to the round comp or its picker.</summary>
public static class RoundGizmos
{
    // Borrowed from the flight's own buttons until the round's are drawn (queued for night 16).
    static Texture2D Plan => SystemIcons.For("Phobos/VehicleSystems/Flight/FlyTo");
    static Texture2D Fly => SystemIcons.For("Phobos/VehicleSystems/Flight/Circle");
    static Texture2D Halt => SystemIcons.For("Phobos/VehicleSystems/Flight/Hold");
    static Texture2D Waits => SystemIcons.For("Phobos/VehicleSystems/Flight/CircleSize");

    public static IEnumerable<Gizmo> For(CompPhobosRound round, bool inCaravan)
    {
        if (!inCaravan)
        {
            var plan = new Command_Action
            {
                defaultLabel = "PhobosRound_GizmoPlan".Translate(),
                defaultDesc = "PhobosRound_GizmoPlanDesc".Translate(RoundRule.MostStops.Named("MOST")),
                icon = Plan,
                action = () => RoundPlanner.Begin(round),
            };
            if (round.Running) plan.Disable("PhobosRound_RefuseWhileFlying".Translate());
            yield return plan;
        }
        if (round.Stops.Count == 0) yield break;
        if (round.Running)
        {
            yield return new Command_Action
            {
                defaultLabel = "PhobosRound_GizmoStop".Translate(),
                defaultDesc = "PhobosRound_GizmoStopDesc".Translate(),
                icon = Halt,
                action = round.Stop,
            };
        }
        else
        {
            var fly = new Command_Action
            {
                defaultLabel = "PhobosRound_GizmoFly".Translate(),
                defaultDesc = "PhobosRound_GizmoFlyDesc".Translate(),
                icon = Fly,
                action = round.Start,
            };
            if (round.StartRefusal() is { } why) fly.Disable(why);
            yield return fly;
        }
        yield return new Command_Action
        {
            defaultLabel = "PhobosRound_GizmoWaits".Translate(),
            defaultDesc = "PhobosRound_GizmoWaitsDesc".Translate(),
            icon = Waits,
            action = () => Find.WindowStack.Add(new FloatMenu(StopOptions(round))),
        };
    }

    static List<FloatMenuOption> StopOptions(CompPhobosRound round)
    {
        var options = new List<FloatMenuOption>();
        for (int i = 0; i < round.Stops.Count; i++)
        {
            int index = i;
            RoundStop stop = round.Stops[i];
            string label = "PhobosRound_StopLine".Translate((i + 1).Named("INDEX"), stop.Label.Named("PLACE"), stop.waitTicks.ToStringTicksToPeriod().Named("WAIT"));
            options.Add(new FloatMenuOption(label, () => Find.WindowStack.Add(new FloatMenu(WaitOptions(round, index)))));
        }
        return options;
    }

    static List<FloatMenuOption> WaitOptions(CompPhobosRound round, int index) =>
        RoundRule.WaitChoices.Select(hours => new FloatMenuOption(RoundRule.WaitTicks(hours) == 0 ? "PhobosRound_WaitNone".Translate().Resolve()
            : RoundRule.WaitTicks(hours).ToStringTicksToPeriod(), () => round.SetWait(index, hours))).ToList();
}

/// <summary>
/// Picks a round's stops (Framework 0.307.0; the owner's card: on the world map): where it stands is the first; each click on the world map
/// adds the next, and on a place with a map the player then picks the spot it lands on there, as for any landing, before going on. Right-click
/// or Escape ends the picking. Presentation only: the round comp says what may be added and keeps it.
/// </summary>
public static class RoundPlanner
{
    public static void Begin(CompPhobosRound round)
    {
        round.BeginPlan();
        PickNext(round);
    }

    static void PickNext(CompPhobosRound round)
    {
        CameraJumper.TryShowWorld();
        PlanetTile last = round.Stops.Count > 0 ? round.Stops[round.Stops.Count - 1].tile : PlanetTile.Invalid;
        Find.WorldTargeter.BeginTargeting(target => Picked(round, target), canTargetTiles: true, mouseAttachment: SystemIcons.For("Phobos/VehicleSystems/Flight/FlyTo"),
            closeWorldTabWhenFinished: false, onUpdate: null, extraLabelGetter: target => Label(round, target, last), canSelectTarget: null,
            originForClosest: last, showCancelButton: true);
    }

    static bool Picked(CompPhobosRound round, GlobalTargetInfo target)
    {
        if (round.AddStopRefusal(target) is { } why)
        {
            Messages.Message(why, MessageTypeDefOf.RejectInput, false);
            return false;
        }
        VehiclePawn vehicle = round.Vehicle;
        if (target.WorldObject is MapParent { HasMap: true } parent)
        {
            Map map = parent.Map;
            Current.Game.CurrentMap = map;
            CameraJumper.TryHideWorld();
            LandingTargeter.Instance.BeginTargeting(vehicle, map, (cell, rot) =>
            {
                round.AddStop(parent.Tile, parent, cell.Cell, rot);
                PickNext(round);
            }, cell => cell.Cell.InBounds(map) && !Ext_Vehicles.IsRoofRestricted(vehicle.VehicleDef, cell.Cell, map), allowRotating: vehicle.VehicleDef.rotatable);
            return true;
        }
        round.AddStop(target.Tile, null, IntVec3.Invalid, Rot4.North);
        return false;
    }

    /// <summary>The line under the mouse: the leg's fuel against what it can carry, and why a place may not be picked.</summary>
    static TaggedString Label(CompPhobosRound round, GlobalTargetInfo target, PlanetTile last)
    {
        if (!target.IsValid) return default;
        if (round.AddStopRefusal(target) is { } why) return why;
        VehiclePawn vehicle = round.Vehicle;
        if (vehicle.CompVehicleLauncher is not { } launcher || vehicle.CompFueledTravel is not { } fuel || !last.Valid) return default;
        float needed = launcher.FuelNeededToLaunchAtDist(WorldHelper.GetTilePos(last), target.Tile) * (float)RoundRule.FuelMargin;
        return "PhobosRound_LegFuel".Translate(Mathf.CeilToInt(needed).Named("NEEDED"), Mathf.FloorToInt(fuel.FuelCapacity).Named("CAPACITY"));
    }
}
