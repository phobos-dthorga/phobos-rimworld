using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Interface;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>One of the player's vehicles as the fleet board shows it: a snapshot, rebuilt rather than kept up to date.</summary>
public sealed class FleetEntry
{
    public const int NoStatus = -1, Good = 0, Notable = 1, Stopped = 2;

    public VehiclePawn Vehicle = null!;
    public string Where = "";
    public GlobalTargetInfo Target;
    /// <summary>Share of a full tank, or a negative number for a vehicle that carries no fuel.</summary>
    public float Fuel = -1f;
    public float Health = 1f;
    public string? WorstPart;
    public int Crew;
    public int Seats;
    public int Status = NoStatus;
    public string? Waiting;
}

/// <summary>
/// Finds every vehicle the player owns, wherever it is (docs/development/vehicle-garage-design.md). Runs only when the fleet board asks,
/// and nothing here runs while the board is closed.
/// </summary>
public static class FleetService
{
    public static List<FleetEntry> Build()
    {
        var entries = new List<FleetEntry>();
        foreach (Map map in Find.Maps)
        {
            string where = map.Parent?.LabelCap ?? "";
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn is VehiclePawn vehicle && vehicle.Faction == Faction.OfPlayer) entries.Add(Entry(vehicle, where, new GlobalTargetInfo(vehicle)));
            }
        }
        foreach (WorldObject place in Find.WorldObjects.AllWorldObjects)
        {
            IEnumerable<VehiclePawn>? vehicles = place switch
            {
                VehicleCaravan convoy => convoy.VehiclesListForReading,
                AerialVehicleInFlight aircraft when aircraft.Vehicle != null => new[] { aircraft.Vehicle },
                StashedVehicle stash => stash.Vehicles,
                IPhobosVehicleHolder holder => holder.HeldVehicles,
                _ => null,
            };
            if (vehicles == null) continue;
            string where = WhereIs(place);
            foreach (VehiclePawn vehicle in vehicles)
            {
                if (vehicle.Faction == Faction.OfPlayer) entries.Add(Entry(vehicle, where, new GlobalTargetInfo(place)));
            }
        }
        return entries;
    }

    /// <summary>Goes to the vehicle, or to the world object holding it, and closes the board.</summary>
    public static void Jump(FleetEntry entry)
    {
        CameraJumper.TryJumpAndSelect(entry.Target);
        Find.MainTabsRoot.EscapeCurrentTab();
    }

    static string WhereIs(WorldObject place)
    {
        if (place is AerialVehicleInFlight) return "PhobosFleet_WhereInFlight".Translate();
        if (place is StashedVehicle)
        {
            TimeoutComp? timeout = place.GetComponent<TimeoutComp>();
            return timeout != null && timeout.Active
                ? "PhobosFleet_WhereParkedUntil".Translate(timeout.TicksLeft.ToStringTicksToPeriod().Named("TIME"))
                : "PhobosFleet_WhereParked".Translate();
        }
        return place.LabelCap;
    }

    static FleetEntry Entry(VehiclePawn vehicle, string where, GlobalTargetInfo target)
    {
        var entry = new FleetEntry
        {
            Vehicle = vehicle,
            Where = where,
            Target = target,
            Crew = vehicle.AllPawnsAboard.Count,
            Seats = vehicle.TotalSeats,
        };
        if (vehicle.CompFueledTravel != null) entry.Fuel = vehicle.CompFueledTravel.FuelPercent;
        float worst = 1f;
        foreach (VehicleComponent part in vehicle.statHandler.components)
        {
            float share = part.MaxHealth > 0f ? part.Health / part.MaxHealth : 1f;
            if (share >= worst) continue;
            worst = share;
            entry.WorstPart = part.props.label;
        }
        entry.Health = worst;
        CompPhobosVehicleSystems? systems = vehicle.GetComp<CompPhobosVehicleSystems>();
        if (systems != null && systems.Usable)
        {
            UnityEngine.Color light = WITab_PhobosVehicles.Light(systems);
            entry.Status = light == PhobosWidgets.Bad ? FleetEntry.Stopped : light == PhobosWidgets.Caution ? FleetEntry.Notable : FleetEntry.Good;
            entry.Waiting = InspectText.WaitingLine(systems);
        }
        return entry;
    }
}

/// <summary>
/// Warns before Vehicle Framework deletes a vehicle left parked in the wild, once per stash, naming what and where. Checks a few times a
/// day; whether a stash has been warned about is the only thing saved.
/// </summary>
public class GameComponent_PhobosParkedWarning : GameComponent
{
    const int CheckInterval = GenDate.TicksPerHour * 6;

    HashSet<int> warned = new();

    public GameComponent_PhobosParkedWarning(Game game)
    {
    }

    public override void GameComponentTick()
    {
        if (Find.TickManager.TicksGame % CheckInterval != 0) return;
        int warnDays = VehicleSystemsMod.Settings.parkedWarningDays;
        if (warnDays <= 0) return;
        List<StashedVehicle> stashes = Find.WorldObjects.AllWorldObjects.OfType<StashedVehicle>().ToList();
        warned.RemoveWhere(id => stashes.All(stash => stash.ID != id));
        foreach (StashedVehicle stash in stashes)
        {
            TimeoutComp? timeout = stash.GetComponent<TimeoutComp>();
            if (timeout == null || !timeout.Active || timeout.TicksLeft > warnDays * GenDate.TicksPerDay) continue;
            List<VehiclePawn> vehicles = stash.Vehicles.Where(v => v.Faction == Faction.OfPlayer).ToList();
            if (vehicles.Count == 0 || !warned.Add(stash.ID)) continue;
            Find.LetterStack.ReceiveLetter("PhobosFleet_ParkedWarningLabel".Translate(), "PhobosFleet_ParkedWarningText".Translate(
                string.Join(", ", vehicles.Select(v => v.LabelShort)).Named("VEHICLES"), timeout.TicksLeft.ToStringTicksToPeriod().Named("TIME")),
                LetterDefOf.NeutralEvent, stash);
        }
    }

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref warned, "PhobosFleet_WarnedStashes", LookMode.Value);
        warned ??= new HashSet<int>();
    }
}
