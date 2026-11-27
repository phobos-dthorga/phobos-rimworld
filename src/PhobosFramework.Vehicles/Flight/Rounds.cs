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

/// <summary>What it does at an ally's town (Framework 0.315.0; the owner's card: chosen per stop).</summary>
public enum RoundStopAction
{
    /// <summary>It lands as a caravan beside the town and waits its time for the player to trade (someone aboard must talk).</summary>
    Trade,
    /// <summary>It hands everything aboard to the town for goodwill, then waits its time there as a caravan.</summary>
    Gift,
}

/// <summary>What the sling does at a round's stop (Framework 0.340.0; supply runs by air, the owner's cards of 27 Nov 2026).</summary>
public enum RoundSlingOrder
{
    /// <summary>Nothing of its own: a load hanging as it lands on a map is set down there, as after any landing.</summary>
    None,
    /// <summary>Before it leaves, it hooks up the thing picked for this stop, or else the nearest filled cargo net within reach.</summary>
    Lift,
    /// <summary>At a tile with no map, it leaves its load there as a depot a caravan can collect from.</summary>
    Leave,
}

/// <summary>One stop of a round: a tile, and on a place with a map the spot it lands on there and the way it faces.</summary>
public sealed class RoundStop : IExposable
{
    /// <summary>What the sling does here (Framework 0.340.0).</summary>
    public RoundSlingOrder sling;
    /// <summary>The thing to lift here, picked by the player, or null for the nearest filled cargo net (the owner's card: your pick, else a net).</summary>
    public Thing? liftThis;

    /// <summary>At an ally's town: trade or give (Framework 0.315.0). Unused elsewhere.</summary>
    public RoundStopAction action = RoundStopAction.Trade;

    /// <summary>The town, when this stop is another faction's (an ally's when picked).</summary>
    public Settlement? Town => parent is Settlement { Faction: { } faction } town && faction != Faction.OfPlayer ? town : null;

    /// <summary>Whether it may fly here now: anywhere but a town whose people have turned hostile since.</summary>
    public bool Usable => Town is not { } town || !town.Faction.HostileTo(Faction.OfPlayer);

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
        Scribe_Values.Look(ref action, "action", RoundStopAction.Trade);
        Scribe_Values.Look(ref sling, "PhobosRound_Sling", RoundSlingOrder.None);
        Scribe_References.Look(ref liftThis, "PhobosRound_LiftThis");
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
/// takes off for the next once Vehicle Framework would let it launch (a pilot aboard, or from 0.310.0 a working optionally piloted kit; not
/// overloaded, not under a roof) and it has the fuel for the leg (0.310.0, the owner's answer: exactly that); otherwise it waits there and says
/// why once. A place with a map it lands on at the spot picked; a tile
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
    /// <summary>
    /// Whether it flies with nobody aboard (Framework 0.310.0): set as it leaves a stop with no pilot and a working optionally piloted kit, and
    /// kept until it lands at a stop with its round stopped. Vehicle Framework's crew check reads it (RemoteControlOperatorsPatch). Saved.
    /// </summary>
    bool crewless;

    /// <summary>What the optionally piloted kit grants, and the electrical load that powers it (Fittings_Uncrewed.xml).</summary>
    public const string KitGrant = "CrewlessFlight";
    public const string KitLoad = "kitUncrewed";

    /// <summary>Whether it flies with nobody aboard now: Vehicle Framework's crew check answers yes for it (one field read).</summary>
    public bool FliesCrewless => crewless;

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

    /// <summary>Trade or give at an ally's town (Framework 0.315.0).</summary>
    public void SetAction(int index, RoundStopAction action)
    {
        if (index < 0 || index >= stops.Count || stops[index].Town == null) return;
        stops[index].action = action;
    }

    /// <summary>The stop it is at or flying to, or -1.</summary>
    public int At => at;

    /// <summary>Sets what the sling does at this stop, and the thing to lift there (null for the nearest net).</summary>
    public void SetSling(int index, RoundSlingOrder order, Thing? lift = null)
    {
        if (index < 0 || index >= stops.Count) return;
        stops[index].sling = order;
        stops[index].liftThis = order == RoundSlingOrder.Lift ? lift : null;
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
        if (!(running || crewless) || !Vehicle.Spawned || !Gen.IsHashIntervalTick(Vehicle, LookTicks)) return;
        if (!running)
        {
            // Its round stopped while it flew with nobody aboard: once down, it is an empty aircraft again.
            if (Vehicle.GetComp<CompPhobosFlight>() is not { Airborne: true }) crewless = false;
            return;
        }
        Look(null);
    }

    /// <summary>A look while it stands as a caravan on a tile (WorldComponent_PhobosRounds).</summary>
    internal void LookInCaravan(VehicleCaravan caravan)
    {
        if (running) Look(caravan);
        else crewless = false;
    }

    /// <summary>Why its optionally piloted kit cannot fly it now, or null: not fitted, without power, or its autopilot not working.</summary>
    string? KitRefusal()
    {
        if (Vehicle.GetComp<CompPhobosVehicleSystems>() is not { } systems || !systems.Fittings.Carries(KitGrant)) return "PhobosRound_WaitNoPilot".Translate().Resolve();
        if (!systems.Fittings.Grants(KitGrant) || !systems.LoadActive(KitLoad)) return "PhobosRound_WaitKitDown".Translate().Resolve();
        if (!AirCombat.Works(Vehicle, AirCombat.AutopilotQuantity)) return "PhobosRound_WaitAutopilotDown".Translate().Resolve();
        return null;
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
            // At an ally's town to trade (Framework 0.315.0): the game's own Trade button is on the caravan now.
            if (caravan != null && stops[at].Town is { } town && stops[at].action == RoundStopAction.Trade)
            {
                Messages.Message("PhobosRound_ReadyToTrade".Translate(Vehicle.LabelShort.Named("VEHICLE"), town.LabelCap.Named("PLACE"),
                    stops[at].waitTicks.ToStringTicksToPeriod().Named("WAIT")), caravan, MessageTypeDefOf.PositiveEvent, false);
                if (Seen.Wants("aircraft.round-trade", Vehicle)) Seen.Note("aircraft.round-trade", Vehicle, town.LabelCap);
            }
        }
        if (!RoundRule.WaitOver(now, waitUntil)) return;
        // Framework 0.340.0 (supply runs by air): the sling's order at this stop, before it leaves.
        if (SlingAtStop(stops[at], caravan) is { } slingWait)
        {
            Waits(slingWait);
            return;
        }
        int next = RoundRule.NextUsable(at, stops.Select(s => s.Usable).ToList());
        if (next < 0)
        {
            Waits("PhobosRound_WaitNowhere".Translate().Resolve());
            return;
        }
        if (LeaveRefusal(stops[next], caravan) is { } why)
        {
            Waits(why);
            return;
        }
        Fly(next, caravan);
    }

    /// <summary>
    /// The sling's order at a stop, as its wait ends (Framework 0.340.0; the owner's cards of 27 Nov 2026). Leave, at a tile: the load goes
    /// into a depot there. Lift, on a map, with nothing on the hook: it hooks up the thing picked for the stop, or else the nearest filled
    /// cargo net in reach, and goes on only once that would not strain the hook ("Wait and say why"); with nothing to lift it says so once
    /// and flies on. Returns why it waits, or null to go on.
    /// </summary>
    string? SlingAtStop(RoundStop stop, VehicleCaravan? caravan)
    {
        if (stop.sling == RoundSlingOrder.None || Vehicle.GetComp<CompPhobosSling>() is not { } hook) return null;
        if (stop.sling == RoundSlingOrder.Leave)
        {
            if (caravan != null && hook.Load != null) hook.LeaveInDepot(stop.tile);
            return null;
        }
        if (caravan != null || hook.Load != null) return null;
        if (hook.HookedLoad != null) return hook.AwaitingCrew ? "PhobosRound_WaitSlingCrew".Translate().Resolve() : null;
        Thing? target = stop.liftThis is { Spawned: true } picked && picked.Map == Vehicle.Map ? picked : CompPhobosSling.NearestNet(Vehicle);
        if (target == null)
        {
            Messages.Message("PhobosRound_SlingNothing".Translate(Vehicle.LabelShort.Named("VEHICLE"), stop.Label.Named("PLACE")), Vehicle,
                MessageTypeDefOf.NeutralEvent, false);
            return null;
        }
        if (hook.QuietRefusal(target) is { } why) return why;
        hook.HookQuietly(target);
        if (Seen.Wants("aircraft.round-sling", Vehicle, "lift")) Seen.Note("aircraft.round-sling", Vehicle, target.LabelShort, "lift");
        return null;
    }

    /// <summary>Why it cannot take off for this stop now, or null: Vehicle Framework's own launch check, and the fuel for the leg.</summary>
    string? LeaveRefusal(RoundStop next, VehicleCaravan? caravan)
    {
        if (Vehicle.CompVehicleLauncher is not { } launcher) return "PhobosRound_WaitNoLauncher".Translate().Resolve();
        if (caravan != null && (caravan.LeadVehicle != Vehicle || caravan.VehiclesListForReading.Count > 1))
            return "PhobosRound_WaitCaravanOthers".Translate().Resolve();
        // With a pilot aboard the pilot flies; with nobody, its optionally piloted kit, if it works (Framework 0.310.0).
        bool before = crewless;
        crewless = false;
        if (!RemoteControl.CrewDrives(Vehicle))
        {
            if (KitRefusal() is { } kit) return kit;
            crewless = true;
        }
        if (!launcher.CanLaunchWithCargoCapacity(out string reason))
        {
            crewless = before;
            return reason;
        }
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
        // At an ally's town it gives everything aboard as it lands, or lands to trade (Framework 0.315.0).
        IArrivalAction action = stop.Town is { } town && stop.action == RoundStopAction.Gift ? new ArrivalAction_PhobosRoundGift(Vehicle, town)
            : stop.Town == null && stop.OnMap ? new ArrivalAction_LandToCell(Vehicle, stop.parent!, stop.cell, stop.rot)
            : new ArrivalAction_LandToCaravan(Vehicle);
        at = next;
        waitUntil = -1;
        waitingWhy = null;
        if (Seen.Wants("aircraft.round-leg", Vehicle, stop.OnMap ? "map" : "tile")) Seen.Note("aircraft.round-leg", Vehicle, stop.Label, stop.OnMap ? "map" : "tile");
        if (crewless && Seen.Wants("aircraft.round-crewless", Vehicle, stop.OnMap ? "map" : "tile")) Seen.Note("aircraft.round-crewless", Vehicle, stop.Label, stop.OnMap ? "map" : "tile");
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
                stops[Mathf.Max(0, RoundRule.NextUsable(at, stops.Select(s => s.Usable).ToList()))].Label.Named("NEXT")).Resolve();
        }
        return crewless ? "PhobosRound_InspectFlyingCrewless".Translate().Resolve() : "PhobosRound_InspectFlying".Translate().Resolve();
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
        Scribe_Values.Look(ref crewless, "PhobosRound_Crewless");
        if (Scribe.mode == LoadSaveMode.PostLoadInit) stops ??= new List<RoundStop>();
    }
}

/// <summary>Looks at aircraft flying a round that stand as caravans on tiles with no map (Framework 0.307.0), every 250 ticks; and draws the
/// rounds of selected aircraft on the world map (Framework 0.315.0).</summary>
public class WorldComponent_PhobosRounds : WorldComponent
{
    public WorldComponent_PhobosRounds(World world) : base(world)
    {
    }

    /// <summary>The rounds of selected aircraft, and the one being planned, drawn while the world map is shown (Framework 0.315.0).</summary>
    public override void WorldComponentUpdate()
    {
        foreach (CompPhobosRound round in RoundDrawing.Shown()) RoundDrawing.DrawLines(round);
    }

    public override void WorldComponentOnGUI()
    {
        foreach (CompPhobosRound round in RoundDrawing.Shown()) RoundDrawing.DrawNumbers(round);
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
                if (vehicle.GetComp<CompPhobosRound>() is { } round && (round.Running || round.FliesCrewless)) round.LookInCaravan(convoy);
            }
        }
    }
}

/// <summary>The round's buttons (Framework 0.307.0). Presentation only: each hands the order to the round comp or its picker.</summary>
public static class RoundGizmos
{
    // The round's own pictures (Framework 0.336.2, the owner's night-16 picks). Halt still borrows the flight's Hold until its paid
    // redraw (requests/remote-icons-paid.json) is picked.
    static Texture2D Plan => SystemIcons.For("Phobos/VehicleSystems/Flight/RoundPlan");
    static Texture2D Fly => SystemIcons.For("Phobos/VehicleSystems/Flight/RoundFly");
    static Texture2D Halt => SystemIcons.For("Phobos/VehicleSystems/Flight/Hold");
    static Texture2D Waits => SystemIcons.For("Phobos/VehicleSystems/Flight/RoundWaits");

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
            string wait = stop.waitTicks.ToStringTicksToPeriod();
            string label = stop.Town == null
                ? "PhobosRound_StopLine".Translate((i + 1).Named("INDEX"), stop.Label.Named("PLACE"), wait.Named("WAIT")).Resolve()
                : "PhobosRound_StopLineTown".Translate((i + 1).Named("INDEX"), stop.Label.Named("PLACE"), wait.Named("WAIT"),
                    (stop.action == RoundStopAction.Gift ? "PhobosRound_ActionGift" : "PhobosRound_ActionTrade").Translate().Named("ACTION")).Resolve();
            options.Add(new FloatMenuOption(label, () => Find.WindowStack.Add(new FloatMenu(WaitOptions(round, index)))));
        }
        return options;
    }

    /// <summary>Shows the stop's map and asks which thing the sling lifts there (Framework 0.340.0). Presentation only.</summary>
    static void PickLift(CompPhobosRound round, int index)
    {
        if (round.Stops[index].parent is not { HasMap: true } parent) return;
        Current.Game.CurrentMap = parent.Map;
        CameraJumper.TryHideWorld();
        Find.Targeter.BeginTargeting(new TargetingParameters
        {
            canTargetPawns = false,
            canTargetBuildings = true,
            canTargetItems = true,
            mapObjectTargetsMustBeAutoAttackable = false,
            validator = target => CompPhobosSling.IsLiftableThing(target.Thing),
        }, target =>
        {
            round.SetSling(index, RoundSlingOrder.Lift, target.Thing);
            Messages.Message("PhobosRound_SlingLiftSet".Translate(round.Vehicle.LabelShort.Named("VEHICLE"), target.Thing.LabelShort.Named("LOAD"),
                round.Stops[index].Label.Named("PLACE")), target.Thing, MessageTypeDefOf.SilentInput, false);
        });
    }

    static List<FloatMenuOption> WaitOptions(CompPhobosRound round, int index)
    {
        var options = new List<FloatMenuOption>();
        if (round.Stops[index].Town != null)
        {
            options.Add(new FloatMenuOption("PhobosRound_SetTrade".Translate(), () => round.SetAction(index, RoundStopAction.Trade)));
            options.Add(new FloatMenuOption("PhobosRound_SetGift".Translate(), () => round.SetAction(index, RoundStopAction.Gift)));
        }
        // Framework 0.340.0: the sling's order here, for an aircraft with a cargo hook.
        if (round.Vehicle.GetComp<CompPhobosSling>() is { HookFitted: true })
        {
            RoundStop stop = round.Stops[index];
            options.Add(new FloatMenuOption("PhobosRound_SlingNone".Translate(), () => round.SetSling(index, RoundSlingOrder.None)));
            if (stop.OnMap)
            {
                options.Add(new FloatMenuOption("PhobosRound_SlingLiftNet".Translate(), () => round.SetSling(index, RoundSlingOrder.Lift)));
                options.Add(new FloatMenuOption("PhobosRound_SlingLiftPick".Translate(), () => PickLift(round, index)));
            }
            else options.Add(new FloatMenuOption("PhobosRound_SlingLeave".Translate(), () => round.SetSling(index, RoundSlingOrder.Leave)));
        }
        options.AddRange(RoundRule.WaitChoices.Select(hours => new FloatMenuOption("PhobosRound_SetWait".Translate(
            (RoundRule.WaitTicks(hours) == 0 ? "PhobosRound_WaitNone".Translate().Resolve() : RoundRule.WaitTicks(hours).ToStringTicksToPeriod()).Named("WAIT")),
            () => round.SetWait(index, hours))));
        return options;
    }
}

/// <summary>
/// Picks a round's stops (Framework 0.307.0; the owner's card: on the world map): where it stands is the first; each click on the world map
/// adds the next, and on a place with a map the player then picks the spot it lands on there, as for any landing, before going on. Right-click
/// or Escape ends the picking. Presentation only: the round comp says what may be added and keeps it.
/// </summary>
public static class RoundPlanner
{
    /// <summary>The round being planned, drawn on the world map while its picker runs (Framework 0.315.0).</summary>
    public static CompPhobosRound? Planning { get; private set; }

    public static void Begin(CompPhobosRound round)
    {
        Planning = round;
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
        // An ally's town (Framework 0.315.0): it lands beside it, to trade by default; the stops menu can make it a gift instead.
        if (target.WorldObject is Settlement { Faction: { } faction } town && faction != Faction.OfPlayer)
        {
            round.AddStop(town.Tile, town, IntVec3.Invalid, Rot4.North);
            return false;
        }
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

/// <summary>
/// Arriving at an ally's town to give (Framework 0.315.0; the owner's card: trade or give, chosen per stop): everything aboard that is not a
/// person goes to the town as the game's own gift, for goodwill, as an airdrop's gift does; then it lands there as a caravan and waits its
/// time. A town turned hostile on the way gets nothing.
/// </summary>
public class ArrivalAction_PhobosRoundGift : VehicleArrivalAction
{
    Settlement? town;

    public ArrivalAction_PhobosRoundGift() { }

    public ArrivalAction_PhobosRoundGift(VehiclePawn vehicle, Settlement town) : base(vehicle) => this.town = town;

    public override void Arrived(GlobalTargetInfo target)
    {
        AerialVehicleInFlight? flight = AerialVehicle;
        Give();
        flight?.SwitchToCaravan();
    }

    void Give()
    {
        if (town is not { Spawned: true, Faction: { } faction } || faction.HostileTo(Faction.OfPlayer))
        {
            Messages.Message("PhobosRound_GiftRefused".Translate(vehicle.LabelShort.Named("VEHICLE")), MessageTypeDefOf.NegativeEvent, false);
            return;
        }
        // Framework 0.340.0: the sling set stays aboard; the next stop's lift needs it.
        List<Thing> items = vehicle.inventory.innerContainer.Where(thing => thing is not Pawn && thing.def != VehicleSystemsThingDefOf.PhobosVehicleSystems_SlingSet).ToList();
        if (items.Count == 0)
        {
            Messages.Message("PhobosRound_GiftNothing".Translate(vehicle.LabelShort.Named("VEHICLE"), town.LabelCap.Named("PLACE")), MessageTypeDefOf.NeutralEvent, false);
            return;
        }
        var pod = new ActiveTransporterInfo();
        foreach (Thing item in items)
        {
            vehicle.inventory.innerContainer.Remove(item);
            pod.innerContainer.TryAdd(item, canMergeWithExistingStacks: false);
        }
        FactionGiftUtility.GiveGift(new List<ActiveTransporterInfo> { pod }, town);
        Seen.Note("aircraft.round-gift", vehicle, town.LabelCap);
        Messages.Message("PhobosRound_GiftGiven".Translate(vehicle.LabelShort.Named("VEHICLE"), town.LabelCap.Named("PLACE")), town, MessageTypeDefOf.PositiveEvent, false);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref town, "PhobosRound_Town");
    }
}

/// <summary>
/// The round drawn on the world map (Framework 0.315.0; the owner's card: lines and numbered stops, the next leg brighter): for an aircraft
/// selected on the world map (in flight or standing as a caravan) and for the round being planned. Each leg follows the globe's curve.
/// Presentation only; nothing is saved.
/// <para>Cost: nothing unless the world map is shown with such an aircraft selected; then a few line pieces a leg and a label a stop, each
/// frame.</para>
/// </summary>
public static class RoundDrawing
{
    static Material? legMaterial;
    static Material? nextMaterial;
    static Material LegMaterial => legMaterial ??= MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.WorldOverlayTransparent, new Color(0.55f, 0.75f, 0.95f, 0.6f), 3590);
    static Material NextMaterial => nextMaterial ??= MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.WorldOverlayTransparent, new Color(1f, 0.8f, 0.3f, 0.95f), 3591);
    const float Lift = 0.05f;
    const float DegreesPerPiece = 1.5f;

    /// <summary>The rounds to draw now.</summary>
    public static IEnumerable<CompPhobosRound> Shown()
    {
        if (!WorldRendererUtility.WorldSelected) yield break;
        if (RoundPlanner.Planning is { } planning && Find.WorldTargeter.IsTargeting && planning.Stops.Count > 0) yield return planning;
        foreach (WorldObject selected in Find.WorldSelector.SelectedObjects)
        {
            if (selected.Faction != Faction.OfPlayer) continue;
            if (selected is AerialVehicleInFlight aircraft && aircraft.Vehicle?.GetComp<CompPhobosRound>() is { } flying && flying.Stops.Count > 1) yield return flying;
            else if (selected is VehicleCaravan caravan)
            {
                foreach (VehiclePawn vehicle in caravan.VehiclesListForReading)
                {
                    if (vehicle.GetComp<CompPhobosRound>() is { } standing && standing.Stops.Count > 1) yield return standing;
                }
            }
        }
    }

    public static void DrawLines(CompPhobosRound round)
    {
        IReadOnlyList<RoundStop> stops = round.Stops;
        int count = stops.Count;
        int legs = count == 2 ? 1 : count;
        if (round == RoundPlanner.Planning && Find.WorldTargeter.IsTargeting) legs = count - 1;
        int next = round.Running ? round.At : -1;
        for (int i = 0; i < legs; i++)
        {
            int to = (i + 1) % count;
            bool bright = next >= 0 && to == next;
            Arc(Find.WorldGrid.GetTileCenter(stops[i].tile), Find.WorldGrid.GetTileCenter(stops[to].tile), bright ? NextMaterial : LegMaterial, bright ? 1.2f : 0.7f);
        }
    }

    /// <summary>A line along the globe's surface from one point to another, in short straight pieces.</summary>
    static void Arc(Vector3 a, Vector3 b, Material material, float width)
    {
        float radius = a.magnitude + Lift;
        float degrees = Vector3.Angle(a, b);
        int pieces = Mathf.Max(1, Mathf.CeilToInt(degrees / DegreesPerPiece));
        Vector3 previous = a.normalized * radius;
        for (int i = 1; i <= pieces; i++)
        {
            Vector3 point = Vector3.Slerp(a, b, i / (float)pieces).normalized * radius;
            GenDraw.DrawWorldLineBetween(previous, point, material, width);
            previous = point;
        }
    }

    /// <summary>Each stop's number, where it faces the camera.</summary>
    public static void DrawNumbers(CompPhobosRound round)
    {
        IReadOnlyList<RoundStop> stops = round.Stops;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        for (int i = 0; i < stops.Count; i++)
        {
            Vector3 centre = Find.WorldGrid.GetTileCenter(stops[i].tile);
            if (WorldRendererUtility.HiddenBehindTerrainNow(centre)) continue;
            Vector2 at = GenWorldUI.WorldToUIPosition(centre);
            var rect = new Rect(at.x - 11f, at.y - 30f, 22f, 22f);
            Widgets.DrawBoxSolid(rect, round.Running && i == round.At ? new Color(0.45f, 0.32f, 0.08f, 0.9f) : new Color(0.1f, 0.1f, 0.12f, 0.85f));
            Widgets.Label(rect, (i + 1).ToString());
        }
        Text.Anchor = TextAnchor.UpperLeft;
    }
}
