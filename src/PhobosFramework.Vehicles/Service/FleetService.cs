using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Interface;
using Phobos.Framework.Persistence;
using Phobos.Framework.WorldMap;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>What a row of the fleet board is (Framework 0.188.0): one of the player's vehicles, one of theirs lying lost or wrecked on the
/// world map, the hull of a destroyed vehicle, or another faction's vehicle standing where it could be claimed.</summary>
public enum FleetKind
{
    Own,
    Lost,
    Hull,
    Claimable,
}

/// <summary>One row of the fleet board: a snapshot, rebuilt rather than kept up to date.</summary>
public sealed class FleetEntry
{
    public const int NoStatus = -1, Good = 0, Notable = 1, Stopped = 2;

    public FleetKind Kind;
    /// <summary>The vehicle; for a hull, the destroyed vehicle it was, where Vehicle Framework still knows it.</summary>
    public VehiclePawn? Vehicle;
    /// <summary>The thing the row is about and jumps to: the vehicle, or the hull.</summary>
    public Thing Subject = null!;
    public VehicleDef? Def;
    public string Label = "";
    public string Where = "";
    public GlobalTargetInfo Target;
    /// <summary>The place it is grouped under: a map, a convoy, in flight, a garage, parked in the wild, lost, hulls, claimable.</summary>
    public string GroupKey = "";
    public string GroupLabel = "";
    public int GroupOrder;
    /// <summary>What it is doing now, in a few words.</summary>
    public string Doing = "";
    /// <summary>The one thing it most needs, or null when it needs nothing.</summary>
    public string? Needs;
    public bool NeedsAttention;
    /// <summary>Share of a full tank, or a negative number for a vehicle that carries no fuel.</summary>
    public float Fuel = -1f;
    public float Health = 1f;
    public string? WorstPart;
    public int Crew;
    public int Seats;
    public int Status = NoStatus;
    public string? Waiting;
    /// <summary>The row of the vehicle pulling this one, so a trailer sits under its tractor.</summary>
    public FleetEntry? TowedBy;
    public CompPhobosVehicleSystems? Systems;
    /// <summary>The world object holding it, when it is on no map.</summary>
    public WorldObject? Place;
    /// <summary>The helicopter it hangs under, for a slung load.</summary>
    public VehiclePawn? Carrier;
    /// <summary>A hull's salvaged parts.</summary>
    public int HullParts;

    public int Id => Subject.thingIDNumber;
}

/// <summary>One fault as the fleet board's detail pane shows it.</summary>
public readonly struct FleetFault
{
    public readonly string Text;
    public readonly string? Cause;
    public readonly bool Severe;

    public FleetFault(string text, string? cause, bool severe)
    {
        Text = text;
        Cause = cause;
        Severe = severe;
    }
}

/// <summary>One logbook line as the detail pane shows it.</summary>
public readonly struct FleetLogLine
{
    public readonly string Date;
    public readonly string Text;
    public readonly bool Bad;

    public FleetLogLine(string date, string text, bool bad)
    {
        Date = date;
        Text = text;
        Bad = bad;
    }
}

/// <summary>What the fleet board's detail pane says about the selected row: built once when the row is chosen or the board refreshes,
/// so the pane draws ready words and works nothing out each frame.</summary>
public sealed class FleetDetail
{
    public int ForId;
    public string? Note;
    public string? Journey;
    public List<FleetFault> Faults = new();
    public List<FleetLogLine> Log = new();
    public int LogTotal;
    public List<DamageSheet.SheetPart>? Parts;
    /// <summary>Its regular driver's name (Framework 0.190.0, EM12), or null.</summary>
    public string? Regular;
}

/// <summary>
/// Finds every vehicle the player owns, wherever it is, and since Framework 0.188.0 what the newer features made worth listing beside
/// them: trailers under their tractors, vehicles lying lost or wrecked on the world map, hulls waiting to be taken apart, and other
/// factions' vehicles that could be claimed; and says of each what it is doing and what it needs (docs/development/vehicle-stations.md).
/// Runs only when the fleet board asks, and nothing here runs while the board is closed.
/// </summary>
public static class FleetService
{
    public const int OrderHome = 0, OrderMap = 1, OrderConvoy = 2, OrderFlight = 3, OrderGarage = 4, OrderParked = 5, OrderLost = 6, OrderHulls = 7,
        OrderClaimable = 8;

    /// <summary>Fuel below this share is worth saying (authored: a quarter of a tank, as the board's gauge turns amber).</summary>
    public const float LowFuel = 0.25f;

    /// <summary>How many of the newest logbook lines the detail pane shows (authored).</summary>
    public const int DetailLogLines = 6;

    public static List<FleetEntry> Build()
    {
        var entries = new List<FleetEntry>();
        var byVehicle = new Dictionary<VehiclePawn, FleetEntry>();

        FleetEntry Add(FleetEntry entry)
        {
            entries.Add(entry);
            if (entry.Vehicle != null && entry.Kind != FleetKind.Hull) byVehicle[entry.Vehicle] = entry;
            return entry;
        }

        string hulls = "PhobosFleet_GroupHulls".Translate();

        FleetEntry HullEntry(VehicleBuilding hull, CompPhobosHull comp, string where, GlobalTargetInfo target) => new()
        {
            Kind = FleetKind.Hull, Vehicle = hull.vehicle, Subject = hull, Def = hull.VehicleDef, Label = hull.LabelShortCap, Where = where,
            Target = target, GroupKey = "hulls", GroupLabel = hulls, GroupOrder = OrderHulls, HullParts = comp.Parts(),
            Health = hull.MaxHitPoints > 0 ? hull.HitPoints / (float)hull.MaxHitPoints : 0f,
        };

        // A hull hanging from a helicopter's sling (Framework 0.197.0) is on no map: it is found through the helicopter.
        void AddSlungHull(VehiclePawn carrier, string where, GlobalTargetInfo target)
        {
            if (carrier.GetComp<CompPhobosSling>()?.CarriedHull is not { } hung || hung.GetComp<CompPhobosHull>() is not { } comp) return;
            Add(HullEntry(hung, comp, where, target)).Carrier = carrier;
        }
        string claimable = "PhobosFleet_GroupClaimable".Translate();
        foreach (Map map in Find.Maps)
        {
            string where = map.Parent?.LabelCap ?? "";
            string key = "map/" + map.uniqueID;
            int order = map.IsPlayerHome ? OrderHome : OrderMap;
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn is not VehiclePawn vehicle) continue;
                if (vehicle.Faction == Faction.OfPlayer)
                {
                    Add(Entry(vehicle, FleetKind.Own, where, new GlobalTargetInfo(vehicle), key, where, order));
                    // A load still on a landed helicopter's sling is on no map of its own: it is found through the helicopter.
                    if (SlingLoads.Of(vehicle) is { } slung)
                    {
                        Add(Entry(slung, FleetKind.Own, where, new GlobalTargetInfo(vehicle), key, where, order)).Carrier = vehicle;
                    }
                    AddSlungHull(vehicle, where, new GlobalTargetInfo(vehicle));
                }
                else if (VehicleClaims.Offered(vehicle) && VehicleClaims.Refusal(vehicle) == null)
                {
                    Add(Entry(vehicle, FleetKind.Claimable, where, new GlobalTargetInfo(vehicle), "claimable", claimable, OrderClaimable));
                }
            }
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
            {
                if (thing is not VehicleBuilding hull || hull.GetComp<CompPhobosHull>() is not { } comp) continue;
                Add(HullEntry(hull, comp, where, new GlobalTargetInfo(hull)));
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
            (string key, string label, int order) = GroupOf(place);
            FleetKind kind = place is PhobosSlingWreck { IsDepot: false } ? FleetKind.Lost : FleetKind.Own;
            foreach (VehiclePawn vehicle in SlingLoads.WithLoads(vehicles).ToList())
            {
                if (vehicle.Faction != Faction.OfPlayer || byVehicle.ContainsKey(vehicle)) continue;
                FleetEntry entry = Add(Entry(vehicle, kind, where, new GlobalTargetInfo(place), key, label, order));
                entry.Place = place;
                if (!vehicles.Contains(vehicle)) entry.Carrier = vehicles.FirstOrDefault(v => SlingLoads.Of(v) == vehicle);
                AddSlungHull(vehicle, where, new GlobalTargetInfo(place));
            }
        }
        // Trailers and towed vehicles: under whatever pulls them, and found through it when it holds them off the map.
        foreach (FleetEntry entry in entries.ToList())
        {
            if (entry.Kind == FleetKind.Hull || entry.Vehicle?.GetComp<CompPhobosHitch>()?.Trailer is not { } trailer) continue;
            if (!byVehicle.TryGetValue(trailer, out FleetEntry? towed))
            {
                towed = Add(Entry(trailer, entry.Kind, entry.Where, entry.Target, entry.GroupKey, entry.GroupLabel, entry.GroupOrder));
                towed.Place = entry.Place;
            }
            if (towed != entry) towed.TowedBy = entry;
        }
        foreach (FleetEntry entry in entries) Describe(entry);
        if (Seen.On) NoteSeen(entries);
        return entries;
    }

    /// <summary>Notes the newer kinds of row the first time each is listed, for the test list (seen working, never "looked right").</summary>
    static void NoteSeen(List<FleetEntry> entries)
    {
        foreach (FleetEntry entry in entries)
        {
            string? variant = entry.Kind switch
            {
                FleetKind.Hull => "hull",
                FleetKind.Claimable => "claimable",
                FleetKind.Lost => "lost",
                _ => entry.TowedBy != null ? "trailer" : null,
            };
            if (variant != null && Seen.Wants("vehicles.fleet-board", entry.Subject, variant)) Seen.Note("vehicles.fleet-board", entry.Subject, variant: variant);
        }
    }

    /// <summary>Goes to the vehicle, the hull or the world object holding it, and closes the board.</summary>
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

    static (string key, string label, int order) GroupOf(WorldObject place) => place switch
    {
        AerialVehicleInFlight => ("flight", "PhobosFleet_WhereInFlight".Translate(), OrderFlight),
        StashedVehicle => ("parked", "PhobosFleet_GroupParked".Translate(), OrderParked),
        PhobosSlingWreck { IsDepot: true } => ("depot/" + place.ID, place.LabelCap, OrderGarage),
        PhobosSlingWreck => ("lost", "PhobosFleet_GroupLost".Translate(), OrderLost),
        VehicleCaravan => ("convoy/" + place.ID, place.LabelCap, OrderConvoy),
        _ => ("garage/" + place.ID, place.LabelCap, OrderGarage),
    };

    static FleetEntry Entry(VehiclePawn vehicle, FleetKind kind, string where, GlobalTargetInfo target, string groupKey, string groupLabel, int groupOrder)
    {
        var entry = new FleetEntry
        {
            Kind = kind, Vehicle = vehicle, Subject = vehicle, Def = vehicle.VehicleDef, Label = vehicle.LabelShortCap, Where = where, Target = target,
            GroupKey = groupKey, GroupLabel = groupLabel, GroupOrder = groupOrder,
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
        if (systems != null && systems.Usable && kind != FleetKind.Claimable)
        {
            entry.Systems = systems;
            UnityEngine.Color light = WITab_PhobosVehicles.Light(systems);
            entry.Status = light == PhobosWidgets.Bad ? FleetEntry.Stopped : light == PhobosWidgets.Caution ? FleetEntry.Notable : FleetEntry.Good;
            entry.Waiting = InspectText.WaitingLine(systems);
        }
        return entry;
    }

    /// <summary>Says what the row's vehicle is doing and the one thing it most needs, once every row and what pulls what is known.</summary>
    static void Describe(FleetEntry entry)
    {
        entry.Doing = Doing(entry);
        entry.Needs = Needs(entry);
        // A hull and a vehicle that could be claimed are offers, not troubles: they say what they give, and ask for nothing.
        entry.NeedsAttention = entry.Kind is not (FleetKind.Hull or FleetKind.Claimable) && (entry.Needs != null || entry.Status == FleetEntry.Stopped);
    }

    static string Doing(FleetEntry entry)
    {
        if (entry.Kind == FleetKind.Hull)
        {
            return entry.Carrier != null ? "PhobosFleet_DoingSlung".Translate(entry.Carrier.LabelShort.Named("CARRIER")) : "PhobosFleet_DoingHull".Translate();
        }
        if (entry.Kind == FleetKind.Claimable) return "PhobosFleet_DoingClaimable".Translate();
        if (entry.Kind == FleetKind.Lost) return "PhobosFleet_DoingLost".Translate();
        VehiclePawn vehicle = entry.Vehicle!;
        if (entry.Carrier != null) return "PhobosFleet_DoingSlung".Translate(entry.Carrier.LabelShort.Named("CARRIER"));
        if (entry.TowedBy != null) return "PhobosFleet_DoingTowed".Translate(entry.TowedBy.Label.Named("TRACTOR"));
        return WithTow(vehicle, Alone(entry, vehicle));
    }

    static string WithTow(VehiclePawn vehicle, string doing) => vehicle.GetComp<CompPhobosHitch>()?.Trailer is { } trailer
        ? "PhobosFleet_DoingTowing".Translate(doing.Named("DOING"), trailer.LabelShort.Named("TRAILER")).Resolve()
        : doing;

    static string Alone(FleetEntry entry, VehiclePawn vehicle)
    {
        CompPhobosVehicleSystems? systems = entry.Systems;
        float tiles = systems?.Trip?.tiles ?? 0f;
        switch (entry.Place)
        {
            case PhobosSlingWreck:
                return "PhobosFleet_DoingDepot".Translate();
            case StashedVehicle:
                return "PhobosFleet_DoingStash".Translate();
            case AerialVehicleInFlight:
                return tiles >= 1f ? "PhobosFleet_DoingFlyingFar".Translate(WorldPlaces.DistanceText(tiles).Named("DISTANCE")) : "PhobosFleet_DoingFlying".Translate();
            case VehicleCaravan convoy:
                if (systems != null && Stranding.Reason(systems) != null) return "PhobosFleet_DoingStranded".Translate();
                if (convoy.vehiclePather is not { MovingNow: true }) return "PhobosFleet_DoingHalted".Translate();
                return tiles >= 1f ? "PhobosFleet_DoingRoadFar".Translate(WorldPlaces.DistanceText(tiles).Named("DISTANCE")) : "PhobosFleet_DoingRoad".Translate();
            case not null:
                return "PhobosFleet_DoingGarage".Translate();
        }
        if (OnMapFlight.Airborne(vehicle)) return "PhobosFleet_DoingAir".Translate();
        if (systems?.Overrun != null) return "PhobosFleet_DoingOverrun".Translate();
        if (vehicle.GetComp<CompPhobosHitch>()?.Winching is { } winched) return "PhobosFleet_DoingWinching".Translate(winched.LabelShort.Named("TOWED"));
        foreach (ThingComp comp in vehicle.AllComps)
        {
            if (comp is CompPhobosVehicleRun { Running: true }) return "PhobosFleet_DoingErrand".Translate();
            if (comp is CompPhobosSetUp { Deployed: true } setUp) return (setUp.Stalled ? "PhobosFleet_DoingSetUpStalled" : "PhobosFleet_DoingSetUp").Translate();
        }
        if (vehicle.vehiclePather is { Moving: true }) return "PhobosFleet_DoingDriving".Translate();
        if (vehicle.Drafted) return "PhobosFleet_DoingEngine".Translate();
        if (DepotBays.Of(vehicle) is { } bay && bay.Pad.Contains(vehicle.Position)) return "PhobosFleet_DoingBay".Translate();
        return "PhobosFleet_DoingParked".Translate();
    }

    static string? Needs(FleetEntry entry)
    {
        switch (entry.Kind)
        {
            case FleetKind.Hull:
                return entry.HullParts > 0 ? "PhobosFleet_NeedsHull".Translate(entry.HullParts.Named("COUNT")).Resolve() : null;
            case FleetKind.Claimable:
                return "PhobosFleet_NeedsClaim".Translate();
            case FleetKind.Lost:
                return "PhobosFleet_NeedsRecovery".Translate();
        }
        CompPhobosVehicleSystems? systems = entry.Systems;
        if (systems != null)
        {
            if (Stranding.Reason(systems) is { } stranded) return stranded;
            if (systems.MoveRefusal != null) return systems.MoveRefusal;
            if (entry.Waiting != null) return entry.Waiting;
            if (systems.NoBrakes()) return "PhobosFleet_NeedsBrakes".Translate();
            if (systems.Conditions.Count > 0) return "PhobosFleet_NeedsFaults".Translate(systems.Conditions.Count.Named("COUNT")).Resolve();
        }
        if (entry.Fuel >= 0f && entry.Fuel < LowFuel) return "PhobosFleet_NeedsFuel".Translate();
        return null;
    }

    /// <summary>The detail pane's words for a row: its faults with their causes, its newest logbook lines, its journey.</summary>
    public static FleetDetail Detail(FleetEntry entry)
    {
        var detail = new FleetDetail { ForId = entry.Id };
        switch (entry.Kind)
        {
            case FleetKind.Hull:
                detail.Note = "PhobosFleet_DetailHull".Translate(entry.HullParts.Named("COUNT")).Resolve();
                return detail;
            case FleetKind.Claimable:
                detail.Note = "PhobosFleet_DetailClaimable".Translate();
                break;
            case FleetKind.Lost:
                detail.Note = "PhobosFleet_DetailLost".Translate();
                break;
        }
        if (entry.Vehicle is not { } vehicle) return detail;
        detail.Parts = DamageSheet.PartsOf(vehicle);
        if (entry.Systems is not { } systems) return detail;
        if (entry.Kind == FleetKind.Own || entry.Kind == FleetKind.Lost) detail.Regular = VehiclePractice.RegularDriver(systems)?.LabelShort;
        if (systems.Trip is { } trip)
        {
            detail.Journey = "PhobosFleet_DetailJourney".Translate(WorldPlaces.DistanceText(trip.tiles).Named("DISTANCE"),
                WorldPlaces.Describe(trip.startTile).Named("PLACE")).Resolve();
        }
        foreach (VehicleCondition condition in systems.Conditions.All)
        {
            if (condition.def == null) continue;
            StageSpec? stage = condition.CurrentStage;
            string part = vehicle.statHandler.GetComponent(condition.partKey)?.props.label ?? condition.partKey;
            string text = stage?.label.NullOrEmpty() == false
                ? "PhobosFleet_DetailFaultStage".Translate(condition.def.label.CapitalizeFirst().Named("CONDITION"), part.Named("PART"), stage!.label.Named("STAGE")).Resolve()
                : "PhobosFleet_DetailFault".Translate(condition.def.label.CapitalizeFirst().Named("CONDITION"), part.Named("PART")).Resolve();
            detail.Faults.Add(new FleetFault(text, condition.cause == null ? null : VehicleStory.CauseLine(condition.cause), stage?.Severe == true));
        }
        IReadOnlyList<PhobosLogEntry> log = systems.Book.Entries;
        detail.LogTotal = log.Count;
        for (int i = log.Count - 1; i >= 0 && detail.Log.Count < DetailLogLines; i--)
        {
            detail.Log.Add(new FleetLogLine(log[i].DateText(), log[i].Text(), log[i].def?.bad == true));
        }
        Seen.Note("vehicles.fleet-detail", vehicle);
        return detail;
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
