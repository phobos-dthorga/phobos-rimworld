using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Phobos.Framework.Rules;
using RimWorld;
using RimWorld.Planet;
using SmashTools.Targeting;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>Where a drop from the world map goes: onto a map, onto a tile with no map, or as a gift to a settlement.</summary>
public enum AirdropDestination
{
    Map,
    Tile,
    Gift,
    /// <summary>Onto a convoy of the player's in radio reach of home, which talks the drop in (Framework 0.325.0, EM33).</summary>
    Convoy,
}

/// <summary>
/// An aircraft's arrival for a parachute drop (D'thorga Framework 0.288.0; docs/development/airdrop.md, "From the world map"). Onto a map it
/// comes in over the edge on our flight and makes its pass, then flies home; onto a tile with no map it drops there at once and flies on home:
/// people (with any vehicles) form a caravan, cargo alone becomes a depot site, and cargo for a friendly settlement is a gift.
/// </summary>
public class ArrivalAction_PhobosAirdrop : VehicleArrivalAction
{
    AirdropDestination destination;
    MapParent? target;
    /// <summary>The convoy a radio-guided drop is for (Framework 0.325.0).</summary>
    public Caravan? convoy;
    IntVec3 dropCell = IntVec3.Invalid;
    MapParent? originParent;
    IntVec3 originCell = IntVec3.Invalid;
    Rot4 originRot = Rot4.North;

    public ArrivalAction_PhobosAirdrop() { }

    public ArrivalAction_PhobosAirdrop(VehiclePawn vehicle, AirdropDestination destination, MapParent? target, IntVec3 dropCell) : base(vehicle)
    {
        this.destination = destination;
        this.target = target;
        this.dropCell = dropCell;
        if (vehicle.Spawned)
        {
            originParent = vehicle.Map.Parent;
            originCell = vehicle.Position;
            originRot = vehicle.Rotation;
        }
    }

    /// <summary>Onto a map the aircraft leaves its world flight there; onto a tile it flies on home.</summary>
    public override bool DestroyOnArrival => destination == AirdropDestination.Map;

    public override void Arrived(GlobalTargetInfo globalTarget)
    {
        if (destination == AirdropDestination.Map)
        {
            ArriveOnMap(globalTarget);
            return;
        }
        AerialVehicleInFlight? flight = AerialVehicle;
        PlanetTile tile = flight?.Tile ?? globalTarget.Tile;
        // Framework 0.325.0 (EM33): a convoy still on the tile takes the drop straight in; one that has moved on misses it.
        Caravan? into = destination == AirdropDestination.Convoy && convoy is { Destroyed: false } c && c.Tile == tile ? c : null;
        AirdropOffMap.Drop(vehicle, tile, destination == AirdropDestination.Gift ? target as Settlement : null, into);
        FlyHome(flight, tile);
    }

    void ArriveOnMap(GlobalTargetInfo globalTarget)
    {
        if (target is not { HasMap: true } parent)
        {
            Messages.Message("PhobosAirdrop_MsgZoneGone".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve(), MessageTypeDefOf.RejectInput);
            AerialVehicle?.SwitchToCaravan();
            return;
        }
        base.Arrived(globalTarget);
        Map map = parent.Map;
        PlanetTile cameFrom = FlightLegs.From(vehicle);
        double bearing = cameFrom.Valid && cameFrom != map.Tile ? Find.WorldGrid.GetHeadingFromTo(map.Tile, cameFrom) : Rand.Range(0f, 360f);
        if (vehicle.GetComp<CompPhobosFlight>() is { } flight && FlightArrival.EnterAt(vehicle, map, bearing))
        {
            (List<FlightNode>? home, IArrivalAction? action) = Home(map.Tile);
            flight.BeginArrivalPass(dropCell, home, action);
            Seen.Note("airdrop.arrived", vehicle, variant: home != null ? "home-after" : "stays");
            return;
        }
        // It could not come in on our flight (no room at the edge): Vehicle Framework's own landing in the middle, with nothing dropped.
        var skyfaller = (VehicleSkyfaller_Arriving)VehicleSkyfallerMaker.MakeSkyfaller(vehicle.CompVehicleLauncher.Props.skyfallerIncoming, vehicle);
        GenSpawn.Spawn(skyfaller, map.Center, map);
        Messages.Message("PhobosAirdrop_MsgLandedInstead".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve(), MessageTypeDefOf.CautionInput);
    }

    /// <summary>The journey home and how it lands there (Framework 0.345.0: <see cref="ReturnFlight"/>, shared with the medevac).</summary>
    (List<FlightNode>?, IArrivalAction?) Home(PlanetTile from) => ReturnFlight.Home(vehicle, originParent, originCell, originRot, from);

    /// <summary>After a drop on a tile: on home, or down where it is as a caravan when it lacks the fuel.</summary>
    void FlyHome(AerialVehicleInFlight? flight, PlanetTile tile) => ReturnFlight.FlyHome(vehicle, flight, tile, originParent, originCell, originRot);

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref destination, "PhobosAirdrop_Destination");
        Scribe_References.Look(ref convoy, "PhobosAirdrop_Convoy");
        Scribe_References.Look(ref target, "PhobosAirdrop_Target");
        Scribe_Values.Look(ref dropCell, "PhobosAirdrop_DropCell", IntVec3.Invalid);
        Scribe_References.Look(ref originParent, "PhobosAirdrop_OriginParent");
        Scribe_Values.Look(ref originCell, "PhobosAirdrop_OriginCell", IntVec3.Invalid);
        Scribe_Values.Look(ref originRot, "PhobosAirdrop_OriginRot", Rot4.North);
    }
}

/// <summary>
/// A drop onto a tile with no map (Framework 0.288.0): everything comes down at once, as nobody watches it fall. People land with the same
/// risks (an ordinary breeze assumed: there is no map to read the wind from); with people, everything forms a caravan there and its spent
/// parachutes are carried along; without, it waits at a depot site to be collected. A gift to a settlement drops cargo only.
/// </summary>
public static class AirdropOffMap
{
    /// <summary>The wind assumed over a tile with no map, knots: an ordinary breeze, force 3 (authored).</summary>
    const float AssumedKnots = 8.5f;

    public static void Drop(VehiclePawn v, PlanetTile tile, Settlement? giftTo, Caravan? into = null)
    {
        PhobosAirdropExtension ext = Airdrop.Ext(v) ?? new PhobosAirdropExtension();
        var items = new List<Thing>();
        var spent = new List<Thing>();
        int bundles = 0;
        while (Airdrop.Cargo(v).Any() && Airdrop.NextCargoChute(v) is { } chute && Airdrop.UseOne(v, chute) && Airdrop.TakeBundle(v) is { } bundle)
        {
            items.AddRange(bundle);
            spent.Add(ThingMaker.MakeThing(AirdropDefOf.Phobos_SpentCargoParachute));
            bundles++;
        }
        if (giftTo != null)
        {
            Gift(v, giftTo, items);
            return;
        }
        int hour = GenLocalDate.HourOfDay(tile);
        bool night = hour < 6 || hour >= 20;
        var people = new List<Pawn>();
        while (Airdrop.Jumpers(v).FirstOrDefault() is { } jumper && Airdrop.UseOne(v, AirdropDefOf.Phobos_PersonnelParachute) && v.RemovePawn(jumper))
        {
            ParachuteLanding.HurtOffMap(jumper, (float)ParachuteRule.Person.PlannedFeet, night, AssumedKnots);
            people.Add(jumper);
            spent.Add(ThingMaker.MakeThing(AirdropDefOf.Phobos_SpentParachute));
        }
        var vehicles = new List<VehiclePawn>();
        foreach (VehiclePawn held in Airdrop.HeldVehicles(v).ToList())
        {
            if (vehicles.Count >= ext.platforms || Airdrop.VehicleKg(held) > ext.heaviestPlatformKg) continue;
            if (held.AllPawnsAboard.Any() && !VehicleSystemsMod.Settings.airdropCrewInside) continue;
            if (!Airdrop.UseOne(v, AirdropDefOf.Phobos_HeavyDropRig) || !Airdrop.TakeHeld(v, held)) continue;
            vehicles.Add(held);
            spent.Add(ThingMaker.MakeThing(AirdropDefOf.Phobos_SpentDropPlatform));
        }
        items.AddRange(spent);
        // Framework 0.325.0 (EM33): talked in by a convoy's radio, everything lands with the convoy and joins it.
        if (into != null)
        {
            foreach (Pawn person in people) into.AddPawn(person, addCarriedPawnToWorldPawnsIfAny: true);
            foreach (VehiclePawn vehicle in vehicles) into.AddPawn(vehicle, addCarriedPawnToWorldPawnsIfAny: true);
            foreach (Thing item in items) CaravanInventoryUtility.GiveThing(into, item);
            if (into is VehicleCaravan joined) joined.RecacheVehicles();
            Seen.Note("airdrop.tile", v, variant: "convoy");
            Messages.Message("PhobosAirdrop_MsgIntoConvoy".Translate(v.LabelShort.Named("VEHICLE"), into.LabelCap.Named("CONVOY"), people.Count.Named("PEOPLE"),
                bundles.Named("BUNDLES"), vehicles.Count.Named("VEHICLES")).Resolve(), into, MessageTypeDefOf.PositiveEvent);
            return;
        }
        if (people.Count > 0)
        {
            var members = new List<Pawn>(people);
            members.AddRange(vehicles);
            Caravan caravan = CaravanHelper.MakeVehicleCaravan(members, Faction.OfPlayer, tile, addToWorldPawnsIfNotAlready: true);
            foreach (Thing item in items) CaravanInventoryUtility.GiveThing(caravan, item);
            Seen.Note("airdrop.tile", v, variant: "caravan");
            Messages.Message("PhobosAirdrop_MsgCaravanFormed".Translate(v.LabelShort.Named("VEHICLE"), people.Count.Named("PEOPLE"), bundles.Named("BUNDLES"),
                vehicles.Count.Named("VEHICLES")).Resolve(), caravan, MessageTypeDefOf.PositiveEvent);
            return;
        }
        if (items.Count == 0 && vehicles.Count == 0) return;
        PlanetTile at = PhobosSlingWreck.FreeTileNear(tile);
        PhobosSlingWreck depot = PhobosSlingWreck.MakeDepot(at.Valid ? at : tile);
        foreach (VehiclePawn vehicle in vehicles) depot.Keep(vehicle);
        foreach (Thing item in items) depot.Keep(item);
        Seen.Note("airdrop.tile", v, variant: "depot");
        Messages.Message("PhobosAirdrop_MsgDepotLaid".Translate(v.LabelShort.Named("VEHICLE"), bundles.Named("BUNDLES"), vehicles.Count.Named("VEHICLES")).Resolve(),
            depot, MessageTypeDefOf.PositiveEvent);
    }

    /// <summary>Cargo dropped on a friendly settlement: the game's own gift, for goodwill (as transport pods give).</summary>
    static void Gift(VehiclePawn v, Settlement settlement, List<Thing> items)
    {
        if (items.Count == 0) return;
        var pod = new ActiveTransporterInfo();
        foreach (Thing item in items) pod.innerContainer.TryAdd(item, canMergeWithExistingStacks: false);
        FactionGiftUtility.GiveGift(new List<ActiveTransporterInfo> { pod }, settlement);
        Seen.Note("airdrop.tile", v, variant: "gift");
        Messages.Message("PhobosAirdrop_MsgGiftDropped".Translate(v.LabelShort.Named("VEHICLE"), settlement.LabelCap.Named("PLACE")).Resolve(), settlement,
            MessageTypeDefOf.PositiveEvent);
    }
}

/// <summary>
/// The arrival options of an aircraft that can drop (Framework 0.288.0): "Airdrop on" a map (aircraft on our flight), on a tile with no map,
/// and "Gift by airdrop" to a friendly settlement. Beside its other options; for another author's aircraft with its own airdrop mode (the
/// C-17), ours replaces its options while that mode is set and the player's setting is on. A postfix, last, after the C-17's own.
/// </summary>
[HarmonyPatch(typeof(LaunchProtocol), nameof(LaunchProtocol.GetArrivalOptions), typeof(GlobalTargetInfo))]
[HarmonyPriority(Priority.Last)]
public static class Patch_GetArrivalOptions_Airdrop
{
    public static void Postfix(LaunchProtocol __instance, GlobalTargetInfo target, ref IEnumerable<ArrivalOption> __result)
    {
        VehiclePawn? vehicle = __instance.Vehicle;
        if (vehicle == null || !vehicle.Spawned || Airdrop.Ext(vehicle) is not { } ext) return;
        if (ext.stockModeComp != null && !Airdrop.StockModeOn(vehicle, ext)) return;
        List<ArrivalOption> ours = Options(vehicle, target).ToList();
        if (ext.stockModeComp != null)
        {
            __result = ours;
            return;
        }
        if (ours.Count > 0) __result = __result.Concat(ours).ToList();
    }

    static IEnumerable<ArrivalOption> Options(VehiclePawn vehicle, GlobalTargetInfo target)
    {
        if (Airdrop.Refusal(vehicle, out _) != null) yield break;
        WorldObject? place = target.WorldObject;
        if (place is MapParent { HasMap: true } parent)
        {
            if (vehicle.GetComp<CompPhobosFlight>() == null) yield break;
            yield return new ArrivalOption("PhobosAirdrop_OptionOnMap".Translate(parent.LabelCap.Named("PLACE")), data =>
            {
                Current.Game.CurrentMap = parent.Map;
                CameraJumper.TryHideWorld();
                ParachuteLoad main = Airdrop.Jumpers(vehicle).Any() ? ParachuteLoad.Person : Airdrop.Cargo(vehicle).Any() ? ParachuteLoad.Bundle : ParachuteLoad.Vehicle;
                AirdropTargeter.Begin(parent.Map, main,
                    cell => vehicle.CompVehicleLauncher.Launch(data, new ArrivalAction_PhobosAirdrop(vehicle, AirdropDestination.Map, parent, cell)));
            });
            yield break;
        }
        if (place is Settlement settlement && settlement.Faction is { } faction && faction != Faction.OfPlayer)
        {
            if (!faction.HostileTo(Faction.OfPlayer) && Airdrop.Cargo(vehicle).Any())
            {
                yield return new ArrivalOption("PhobosAirdrop_OptionGift".Translate(settlement.LabelCap.Named("PLACE")),
                    data => vehicle.CompVehicleLauncher.Launch(data, new ArrivalAction_PhobosAirdrop(vehicle, AirdropDestination.Gift, settlement, IntVec3.Invalid)));
            }
            yield break;
        }
        // Framework 0.325.0 (EM33): a convoy of the player's can be dropped to only when it is in radio reach of home, to talk the drop in.
        // Without the Radio Framework there is no option, as before.
        if (place is Caravan convoy && convoy.Faction == Faction.OfPlayer && ConvoyRadio.Available)
        {
            if (ConvoyRadio.InTouch(convoy))
                yield return new ArrivalOption("PhobosAirdrop_OptionConvoy".Translate(convoy.LabelCap.Named("CONVOY")), data =>
                    vehicle.CompVehicleLauncher.Launch(data, new ArrivalAction_PhobosAirdrop(vehicle, AirdropDestination.Convoy, null, IntVec3.Invalid) { convoy = convoy }));
            else
                yield return new ArrivalOption("PhobosAirdrop_OptionConvoyOut".Translate(convoy.LabelCap.Named("CONVOY")), _ =>
                    Messages.Message("PhobosAirdrop_MsgConvoyOut".Translate(convoy.LabelCap.Named("CONVOY"), (ConvoyRadio.Of(convoy)?.Line ?? "").Named("WHY")).Resolve(),
                        convoy, MessageTypeDefOf.RejectInput, historical: false));
            yield break;
        }
        if (place != null || !target.Tile.Valid) yield break;
        yield return new ArrivalOption("PhobosAirdrop_OptionOnTile".Translate(),
            data => vehicle.CompVehicleLauncher.Launch(data, new ArrivalAction_PhobosAirdrop(vehicle, AirdropDestination.Tile, null, IntVec3.Invalid)));
    }
}
