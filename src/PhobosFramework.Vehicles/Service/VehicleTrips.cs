using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using Phobos.Framework.WorldMap;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// One journey of a vehicle away from any map, in a convoy or in the air (Framework 0.181.0, EM25): where it set out, how far it has come,
/// the fuel it has burnt, the faults it set out with, whether it was stranded, and who drove. Saved with the vehicle while the journey lasts.
/// </summary>
public sealed class VehicleTrip : IExposable
{
    public int startTick;
    public PlanetTile startTile = PlanetTile.Invalid;
    public PlanetTile lastTile = PlanetTile.Invalid;
    public float tiles;
    public float fuelUsed;
    public float lastFuel = -1f;
    public int faultsAtStart;
    public bool halted;
    public List<string> drivers = new();

    public void ExposeData()
    {
        Scribe_Values.Look(ref startTick, "startTick");
        Scribe_Values.Look(ref startTile, "startTile", PlanetTile.Invalid);
        Scribe_Values.Look(ref lastTile, "lastTile", PlanetTile.Invalid);
        Scribe_Values.Look(ref tiles, "tiles");
        Scribe_Values.Look(ref fuelUsed, "fuelUsed");
        Scribe_Values.Look(ref lastFuel, "lastFuel", -1f);
        Scribe_Values.Look(ref faultsAtStart, "faultsAtStart");
        Scribe_Values.Look(ref halted, "halted");
        Scribe_Collections.Look(ref drivers, "drivers", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) drivers ??= new List<string>();
    }
}

/// <summary>
/// The player's vehicles' journeys (Framework 0.181.0; docs/development/vehicle-emergent-play.md, EM25 with EM1 and EM13): a journey opens
/// when the vehicle is found away from any map in a convoy or in flight, follows it at the rare look it already gets there, and closes
/// when the vehicle stands on a map again. Its end goes in the logbook with the distance, fuel, new faults and drivers; the drivers'
/// records count the kilometres; a long one is a tale; and a journey where something went wrong is also a letter, behind its setting.
/// A gravship's flight is no journey: the vehicle rides in its hold, neither in a convoy nor flying itself.
/// </summary>
public static class VehicleTrips
{
    /// <summary>A journey this long or longer is a tale for its drivers (authored, 10 tiles, about 300 km).</summary>
    public const float TaleTiles = 10f;

    /// <summary>Whether the vehicle is away on a journey of its own: in a convoy, or an aircraft in flight.</summary>
    public static bool Away(VehiclePawn vehicle)
    {
        if (vehicle.Spawned) return false;
        if (vehicle.GetCaravan() != null) return true;
        return vehicle.ParentHolder is AerialVehicleInFlight || vehicle.ParentHolder is ThingOwner { Owner: AerialVehicleInFlight };
    }

    /// <summary>The rare look: opens, follows or closes the journey.</summary>
    public static void Look(CompPhobosVehicleSystems comp)
    {
        VehiclePawn vehicle = comp.Vehicle;
        if (vehicle.Faction != Faction.OfPlayer || Current.Game == null) return;
        if (Away(vehicle))
        {
            if (comp.Trip == null) Start(comp);
            else Follow(comp, comp.Trip);
        }
        else if (vehicle.Spawned && comp.Trip != null)
        {
            End(comp);
        }
    }

    static void Start(CompPhobosVehicleSystems comp)
    {
        VehiclePawn vehicle = comp.Vehicle;
        PlanetTile tile = WorldPlaces.TileOf(vehicle);
        var trip = new VehicleTrip
        {
            startTick = Find.TickManager.TicksGame,
            startTile = tile,
            lastTile = tile,
            lastFuel = vehicle.CompFueledTravel?.Fuel ?? -1f,
            faultsAtStart = comp.Conditions.Count,
        };
        trip.drivers.AddRange(VehicleStory.Drivers(vehicle).Select(p => p.LabelShort));
        comp.Trip = trip;
        VehicleStory.Log(comp, VehicleStoryDefOf.PhobosLog_TripStart, who: Names(trip));
        if (Seen.Wants("vehicles.trip-start", vehicle)) Seen.Note("vehicles.trip-start", vehicle);
    }

    static void Follow(CompPhobosVehicleSystems comp, VehicleTrip trip)
    {
        VehiclePawn vehicle = comp.Vehicle;
        PlanetTile tile = WorldPlaces.TileOf(vehicle);
        if (tile.Valid && trip.lastTile.Valid && tile != trip.lastTile && tile.Layer == trip.lastTile.Layer)
        {
            trip.tiles += Find.WorldGrid.ApproxDistanceInTiles(trip.lastTile, tile);
        }
        if (tile.Valid) trip.lastTile = tile;
        float fuel = vehicle.CompFueledTravel?.Fuel ?? -1f;
        if (fuel >= 0f && trip.lastFuel >= 0f && fuel < trip.lastFuel) trip.fuelUsed += trip.lastFuel - fuel;
        trip.lastFuel = fuel;
        foreach (Pawn driver in VehicleStory.Drivers(vehicle))
        {
            if (!trip.drivers.Contains(driver.LabelShort)) trip.drivers.Add(driver.LabelShort);
        }
        // Stranded (EM1): a refusal to move, or an empty tank, while away. Written once a journey.
        string? reason = Stranding.Reason(comp);
        if (reason != null && !trip.halted)
        {
            trip.halted = true;
            VehicleStory.Log(comp, VehicleStoryDefOf.PhobosLog_Stranded, detail: reason);
            if (Seen.Wants("vehicles.stranded", vehicle)) Seen.Note("vehicles.stranded", vehicle, reason);
        }
    }

    /// <summary>Closes the journey: the logbook, the drivers' records, a tale for a long one, and a letter where something went wrong.</summary>
    public static void End(CompPhobosVehicleSystems comp)
    {
        VehicleTrip? trip = comp.Trip;
        if (trip == null) return;
        comp.Trip = null;
        VehiclePawn vehicle = comp.Vehicle;
        PlanetTile here = WorldPlaces.TileOf(vehicle);
        // The last stretch, from the tile last seen to where it arrived, and the fuel since; on a map nothing reads as stranded.
        Follow(comp, trip);
        float km = (float)WorldScaleRule.Kilometres(trip.tiles);
        int newFaults = System.Math.Max(0, comp.Conditions.Count - trip.faultsAtStart);
        bool hurt = SomeoneHurt(vehicle, trip.startTick);
        string detail = Summary(comp, trip, newFaults, hurt);
        VehicleStory.Log(comp, VehicleStoryDefOf.PhobosLog_TripEnd, who: Names(trip), detail: detail, value: km);
        List<Pawn> drivers = VehicleStory.Drivers(vehicle).ToList();
        foreach (Pawn driver in drivers) driver.records?.AddTo(VehicleStoryDefOf.PhobosRecord_KilometresDriven, km);
        if (trip.tiles >= TaleTiles && drivers.Count > 0) VehicleStory.LongJourney(drivers[0], vehicle);
        if (Seen.Wants("vehicles.trip-end", vehicle))
        {
            Seen.Note("vehicles.trip-end", vehicle, km.ToString("0") + " km", values: new[] { new KeyValuePair<string, double>("km", km) });
        }
        if (!VehicleSystemsMod.Settings.tripLetters || !TripRule.WentWrong(newFaults, trip.halted, hurt)) return;
        Find.LetterStack.ReceiveLetter("PhobosVehicleSystems_TripLetterLabel".Translate(vehicle.LabelShort.Named("VEHICLE")),
            "PhobosVehicleSystems_TripLetter".Translate(vehicle.LabelShort.Named("VEHICLE"), WorldPlaces.Describe(trip.startTile).Named("FROM"),
                WorldPlaces.Describe(here).Named("TO"), WorldPlaces.DistanceText(trip.tiles).Named("DISTANCE"), detail.Named("DETAIL")),
            LetterDefOf.NeutralEvent, vehicle.Spawned ? vehicle : LookTargets.Invalid);
        if (Seen.Wants("vehicles.trip-letter", vehicle)) Seen.Note("vehicles.trip-letter", vehicle, newFaults + " faults, halted " + trip.halted + ", hurt " + hurt);
    }

    static string Names(VehicleTrip trip) => trip.drivers.Count == 0 ? "PhobosVehicleSystems_LogNobody".Translate().Resolve() : trip.drivers.ToCommaList(useAnd: true);

    static string Summary(CompPhobosVehicleSystems comp, VehicleTrip trip, int newFaults, bool hurt)
    {
        var lines = new List<string>();
        if (trip.fuelUsed > 0f && comp.Vehicle.CompFueledTravel?.Props.fuelType is { } fuel)
        {
            lines.Add("PhobosVehicleSystems_TripFuel".Translate(trip.fuelUsed.ToString("0").Named("AMOUNT"), fuel.label.Named("FUEL")).Resolve());
        }
        lines.Add(newFaults == 0 ? "PhobosVehicleSystems_TripNoFaults".Translate().Resolve() : "PhobosVehicleSystems_TripFaults".Translate(newFaults.Named("COUNT")).Resolve());
        if (trip.halted) lines.Add("PhobosVehicleSystems_TripHalted".Translate().Resolve());
        if (hurt) lines.Add("PhobosVehicleSystems_TripHurt".Translate().Resolve());
        return string.Join(" ", lines);
    }

    /// <summary>Whether anyone aboard is down, or took an injury since the journey began.</summary>
    static bool SomeoneHurt(VehiclePawn vehicle, int since)
    {
        int age = Find.TickManager.TicksGame - since;
        foreach (Pawn pawn in vehicle.AllPawnsAboard)
        {
            if (pawn?.health == null) continue;
            if (pawn.Downed) return true;
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff is Hediff_Injury && hediff.ageTicks <= age) return true;
            }
        }
        return false;
    }
}
