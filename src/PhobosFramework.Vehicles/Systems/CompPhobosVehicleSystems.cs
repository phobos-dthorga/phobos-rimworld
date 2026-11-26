using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Phobos.Framework;
using Phobos.Framework.Debug;
using Phobos.Framework.Defs;
using Phobos.Framework.Items;
using Phobos.Framework.Rules;
using Phobos.Framework.Sounds;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The comp the service puts on each vehicle design a loadout claims. Built in code while Defs load (VehicleSystems.Inject), never
/// written in XML: it carries the resolved systems, which of the vehicle's parts play each role, and what was appended. Its tuning
/// fields appear in Vehicle Framework's per-vehicle settings window (see <see cref="Tuning"/>).
/// </summary>
[VehicleSettingsClass]
public class CompProperties_PhobosVehicleSystems : VehicleCompProperties
{
    // ---- Tuning in Vehicle Framework's settings window: each field is one shipped system's parameter. ----

    [PostToSettings(Label = "PhobosVehicleSystems_TuneBatteryAmpHours", Tooltip = "PhobosVehicleSystems_TuneBatteryAmpHoursDesc", Translate = true, UISettingsType = UISettingsType.FloatBox)]
    [NumericBoxValues(MinValue = 1f, MaxValue = 2000f)]
    [TunesParameter("PhobosVehicleSystem_Electrical", "batteryAmpHours")]
    public float batteryAmpHours;

    [PostToSettings(Label = "PhobosVehicleSystems_TuneAlternatorAmps", Tooltip = "PhobosVehicleSystems_TuneAlternatorAmpsDesc", Translate = true, UISettingsType = UISettingsType.FloatBox)]
    [NumericBoxValues(MinValue = 0f, MaxValue = 1000f)]
    [TunesParameter("PhobosVehicleSystem_Electrical", "alternatorAmps")]
    public float alternatorAmps;

    [PostToSettings(Label = "PhobosVehicleSystems_TuneStarterAmpHours", Tooltip = "PhobosVehicleSystems_TuneStarterAmpHoursDesc", Translate = true, UISettingsType = UISettingsType.FloatBox)]
    [NumericBoxValues(MinValue = 0f, MaxValue = 10f)]
    [TunesParameter("PhobosVehicleSystem_Electrical", "starterAmpHours")]
    public float starterAmpHours;

    [PostToSettings(Label = "PhobosVehicleSystems_TuneEngineHeat", Tooltip = "PhobosVehicleSystems_TuneEngineHeatDesc", Translate = true, UISettingsType = UISettingsType.FloatBox)]
    [NumericBoxValues(MinValue = 0f, MaxValue = 5000f)]
    [TunesParameter("PhobosVehicleSystem_Cooling", "heatAtFullLoad")]
    public float engineHeatAtFullLoad;

    [PostToSettings(Label = "PhobosVehicleSystems_TuneRadiatorRate", Tooltip = "PhobosVehicleSystems_TuneRadiatorRateDesc", Translate = true, UISettingsType = UISettingsType.FloatBox)]
    [NumericBoxValues(MinValue = 0f, MaxValue = 100f)]
    [TunesParameter("PhobosVehicleSystem_Cooling", "radiatorRate")]
    public float radiatorRate;

    [PostToSettings(Label = "PhobosVehicleSystems_TuneThermostat", Tooltip = "PhobosVehicleSystems_TuneThermostatDesc", Translate = true, UISettingsType = UISettingsType.FloatBox)]
    [NumericBoxValues(MinValue = 40f, MaxValue = 130f)]
    [TunesParameter("PhobosVehicleSystem_Cooling", "thermostatOpens")]
    public float thermostatOpens;

    [PostToSettings(Label = "PhobosVehicleSystems_TuneOilBurn", Tooltip = "PhobosVehicleSystems_TuneOilBurnDesc", Translate = true, UISettingsType = UISettingsType.FloatBox)]
    [NumericBoxValues(MinValue = 0f, MaxValue = 1f)]
    [TunesParameter("PhobosVehicleSystem_Lubrication", "oilBurnPerHour")]
    public float oilBurnPerHour;

    [PostToSettings(Label = "PhobosVehicleSystems_TuneTreadWear", Tooltip = "PhobosVehicleSystems_TuneTreadWearDesc", Translate = true, UISettingsType = UISettingsType.FloatBox)]
    [NumericBoxValues(MinValue = 0f, MaxValue = 1f)]
    [TunesParameter("PhobosVehicleSystem_RunningGear", "wearPerHour")]
    public float treadWearPerHour;

    [PostToSettings(Label = "PhobosVehicleSystems_TuneGearboxHeat", Tooltip = "PhobosVehicleSystems_TuneGearboxHeatDesc", Translate = true, UISettingsType = UISettingsType.FloatBox)]
    [NumericBoxValues(MinValue = 0f, MaxValue = 5000f)]
    [TunesParameter("PhobosVehicleSystem_Transmission", "heatAtFullLoad")]
    public float gearboxHeatAtFullLoad;

    [PostToSettings(Label = "PhobosVehicleSystems_TuneCompressor", Tooltip = "PhobosVehicleSystems_TuneCompressorDesc", Translate = true, UISettingsType = UISettingsType.FloatBox)]
    [NumericBoxValues(MinValue = 0f, MaxValue = 100f)]
    [TunesParameter("PhobosVehicleSystem_AirBrakes", "compressorRate")]
    public float compressorRate;

    [PostToSettings(Label = "PhobosVehicleSystems_TuneAirLeak", Tooltip = "PhobosVehicleSystems_TuneAirLeakDesc", Translate = true, UISettingsType = UISettingsType.FloatBox)]
    [NumericBoxValues(MinValue = 0f, MaxValue = 10f)]
    [TunesParameter("PhobosVehicleSystem_AirBrakes", "leakPerHour")]
    public float airLeakPerHour;

    [PostToSettings(Label = "PhobosVehicleSystems_TuneEmptyMass", Tooltip = "PhobosVehicleSystems_TuneEmptyMassDesc", Translate = true, UISettingsType = UISettingsType.FloatBox)]
    [NumericBoxValues(MinValue = 10f, MaxValue = 500000f)]
    [TunesParameter(Tuning.Drivetrain, Tuning.EmptyKilograms)]
    public float emptyKilograms;

    public PhobosVehicleLoadoutDef? loadout;
    public List<ResolvedSystem> systems = new();
    public Dictionary<string, List<string>> roleKeys = new();
    /// <summary>The parts this service appended, by key, in order.</summary>
    public List<string> addedKeys = new();
    /// <summary>Appended parts that do not count toward the vehicle's destruction: empty mounting points for fittings (Framework 0.114.5).</summary>
    public HashSet<string> uncountedKeys = new();
    /// <summary>How many of the design's weapons this service tied to a mount at start-up (Framework 0.115.0, T1).</summary>
    public int weaponsTied;
    /// <summary>The design's tracks, by key: a thrown one stops the vehicle (Framework 0.115.0, M2).</summary>
    public List<string> trackKeys = new();
    /// <summary>The part holding the ammunition rack that can blow the turret off, if the design has one (Framework 0.116.0, T8).</summary>
    public string? ammoRackKey;
    /// <summary>How many parts the design had before the service appended any; the uninstall sweep cuts saved vehicles back to it.</summary>
    public int originalPartCount;
    /// <summary>The design is a trailer (Framework 0.133.0): no engine, crew or hitch of its own; it moves when what tows it moves.</summary>
    public bool trailer;
    /// <summary>The loadout's drivetrain (with its overrides) and flight profile, if any.</summary>
    public LoadoutDrivetrain? drivetrain;
    public PhobosVehicleFlightProfileDef? flightProfile;

    public CompProperties_PhobosVehicleSystems()
    {
        compClass = typeof(CompPhobosVehicleSystems);
    }
}

/// <summary>
/// One vehicle's systems: the saved quantities, the stages they are in, and what those stages do. Runs on the rare tick (every 250
/// ticks, which Vehicle Framework calls on every comp) and on the vehicle's own events, catching up on the hours since it last ran.
/// Draft and move refusals, stat factors, inspect text and gizmos all read the state; the schema's effects change it.
/// </summary>
public class CompPhobosVehicleSystems : VehicleComp, VehiclePipes.IHost, Phobos.Framework.Persistence.ILogbookHolder
{
    List<SystemState>? states;
    Dictionary<string, float> savedValues = new();
    Dictionary<string, bool> loadSwitches = new();
    Dictionary<string, float> savedPartHealth = new();
    readonly Dictionary<string, int> lastNotice = new();
    readonly Dictionary<VehicleStatDef, float> statFactors = new();
    readonly HashSet<string> servicedThisRequest = new();
    List<string> servicedSaved = new();
    int lastUpdateTick = -1;
    bool serviceRequested;
    bool updating;
    bool healthsRestored;
    /// <summary>Whether the stages have been taken from the first real update since the states were made (Framework 0.114.1; never saved).
    /// Made at load, before the systems' first proper update, the derived readings were not yet true, so every vehicle passed through
    /// "too weak to start", "oil pressure gone" and "hydraulics too low" for one update (the owner's verbose log, 16 Nov 2026).</summary>
    bool stagesSettled;
    /// <summary>Whether the detail tier has been read once, so the first reading after a load is not logged as a change.</summary>
    bool tierRead;
    // Whether damage had stopped this vehicle at the last look, so the player hears of it once (Framework 0.115.0, M1). Derived, never saved:
    // null until the first look after a load, which sets it without a message.
    bool? wasStopped;
    bool registered;
    Powertrain? powertrain;
    bool powertrainBuilt;
    VehicleConditions? conditions;
    /// <summary>The kind of damage the reactor last saw on each part, until the health it cost is counted; derived, never saved.</summary>
    readonly Dictionary<string, DamageDef> recentDamage = new();

    VehicleRepairs? repairs;

    /// <summary>Repairs that need supplies: destroyed parts, serious conditions, field fixes (slice 3 of the vehicle health design).</summary>
    public VehicleRepairs Repairs => repairs ??= new VehicleRepairs(this);

    VehicleFittings? fittings;

    /// <summary>This vehicle's fittings and tunes (docs/development/vehicle-tuning-and-upgrades.md).</summary>
    public VehicleFittings Fittings => fittings ??= new VehicleFittings(this);

    /// <summary>
    /// Every electrical load on this vehicle: its loadout's, then those the fittings on standing parts bring (Framework 0.129.0, vehicle
    /// accessories: a fridge, a power outlet). A fitting's load never replaces a loadout's of the same name. Rebuilt at each look and whenever
    /// the fittings change; derived, never saved.
    /// </summary>
    public IReadOnlyList<ElectricalLoadSpec> Loads
    {
        get
        {
            if (loadsCache != null) return loadsCache;
            var list = new List<ElectricalLoadSpec>();
            if (Props.loadout != null) list.AddRange(Props.loadout.loads);
            foreach (PhobosVehicleFittingDef def in Fittings.FittedDefs())
            {
                foreach (ElectricalLoadSpec load in def.loads)
                {
                    if (!list.Exists(l => l.name == load.name)) list.Add(load);
                }
            }
            // What it tows may bring loads through the trailer's lead (Framework 0.216.0: a packed cold box). The rules Def lists them; each
            // one's own "available" says whether this tow has it.
            if (Hitch is { Trailer: not null })
            {
                foreach (ElectricalLoadSpec load in Rules.towLoads)
                {
                    if (!list.Exists(l => l.name == load.name)) list.Add(load);
                }
            }
            // What its camp sets out may draw on it (Framework 0.231.0: camp lights on a pole). Every vehicle that can camp has them listed;
            // each one's own "available" says whether its camp has any out.
            if (Vehicle.GetComp<CompPhobosCamp>() != null)
            {
                foreach (ElectricalLoadSpec load in Rules.campLoads)
                {
                    if (!list.Exists(l => l.name == load.name)) list.Add(load);
                }
            }
            loadsCache = list;
            return list;
        }
    }

    List<ElectricalLoadSpec>? loadsCache;

    /// <summary>What the hydraulic fluid can give a vehicle's hydraulic work, 0 to 1; 1 for a vehicle with no hydraulics (T1, Framework 0.73.0).</summary>
    public float HydraulicSupply => TryGetQuantity("hydraulicSupply", out float supply) ? supply : 1f;

    /// <summary>Why hydraulic work cannot go on now, or null: too little fluid left to work it.</summary>
    public string? HydraulicRefusal() => HydraulicSupply < Rules.hydraulicWorkFloor ? "PhobosVehicleSystems_HydraulicsDry".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve() : null;

    VehicleDiagnostics? diagnostics;

    /// <summary>A diagnosis at a tuning rig: asked for, worked by a mechanic, reported in a letter (Framework 0.73.0).</summary>
    public VehicleDiagnostics Diagnostics => diagnostics ??= new VehicleDiagnostics(this);

    /// <summary>The conditions on this vehicle's parts (slice 2 of the vehicle health design).</summary>
    public VehicleConditions Conditions => conditions ??= new VehicleConditions(this);

    Phobos.Framework.Persistence.Logbook? book;

    /// <summary>The vehicle's lasting logbook (Framework 0.181.0, EM23): journeys, repairs, faults with their causes, close calls.</summary>
    public Phobos.Framework.Persistence.Logbook Book => book ??= new Phobos.Framework.Persistence.Logbook();

    /// <summary>The logbook tab shows the player's vehicles' logbooks only: another faction's is never written.</summary>
    Phobos.Framework.Persistence.Logbook? Phobos.Framework.Persistence.ILogbookHolder.Logbook => Vehicle?.Faction == Faction.OfPlayer ? Book : null;

    /// <summary>When a hit last came, so a fight is one "came under fire" entry, not one a bullet (Framework 0.181.0). Saved.</summary>
    public int LastUnderFireTick = -1;

    /// <summary>The journey under way away from any map, or null (Framework 0.181.0, EM25). Saved while it lasts.</summary>
    public VehicleTrip? Trip;

    /// <summary>Rolling on past where it meant to stop, with no brakes (Framework 0.188.0, EM29(b)); derived, never saved.</summary>
    internal BrakeOverrun? Overrun;

    /// <summary>Who has driven it, for how long, and whether it is lost (Framework 0.190.0, EM11 and EM12). Saved.</summary>
    public readonly VehicleDriverHours Practised = new();

    /// <summary>Whether another faction's vehicle has had its arrival fuel set (Framework 0.192.0, EM5). Saved, so it is set once.</summary>
    public bool ArrivalFuelSet;

    /// <summary>Whether a stage in force says the brakes are gone (Framework 0.188.0): the player's vehicles on the full model only.</summary>
    public bool NoBrakes()
    {
        if (states == null || Tier != DetailTier.Full || Vehicle.Faction != Faction.OfPlayer) return false;
        foreach ((SystemContext context, StageSpec stage, string _) in StagesInForce())
        {
            foreach (VehicleSystemEffect effect in stage.effects)
            {
                if (effect is Effect_NoBrakes && effect.Active(context)) return true;
            }
        }
        return false;
    }

    VehicleEventLog? events;

    /// <summary>What happened to the vehicle, for the marks on its graphs (Framework 0.84.0). Kept for the player's vehicles only.</summary>
    public VehicleEventLog Events => events ??= new VehicleEventLog();

    /// <summary>Marks an event on the vehicle's graphs, if it is the player's: nobody reads another faction's graphs.</summary>
    internal void RecordEvent(VehicleEventKind kind, string system, string detail = "", int stage = -1)
    {
        if (Vehicle?.Faction == Faction.OfPlayer) Events.Record(kind, system, detail, stage);
    }

    /// <summary>Spawned vehicles with this comp, for the per-tick speed cap (one dictionary read instead of a comp search).</summary>
    // Weak: loading a game despawns nothing, and a plain dictionary would keep the last game's vehicles.
    static readonly ConditionalWeakTable<VehiclePawn, CompPhobosVehicleSystems> spawned = new();

    public static CompPhobosVehicleSystems? Of(VehiclePawn vehicle) => spawned.TryGetValue(vehicle, out CompPhobosVehicleSystems? comp) ? comp : null;

    /// <summary>The vehicle's drivetrain or turbine, built on first use from its loadout; null when it has none.</summary>
    public Powertrain? Powertrain
    {
        get
        {
            if (!powertrainBuilt && Vehicle != null)
            {
                powertrainBuilt = true;
                powertrain = Powertrain.For(this);
            }
            return powertrain;
        }
    }

    /// <summary>The share of its speed the vehicle may use this tick (the path follower's patch reads it): the drivetrain model's, and a
    /// tow's on the plain rule where the model does not work the tow out (Framework 0.101.0).</summary>
    public float SpeedCap => Usable ? (Powertrain?.SpeedCap ?? 1f) * (Hitch?.PlainSpeedCap ?? 1f) * (BatteryDrive is { OnBattery: true } drive ? drive.SpeedFactor : 1f)
        * Glass.ViewFactor(Vehicle) : 1f;

    IVehicleBatteryDrive? batteryDrive;
    bool batteryDriveFound;

    /// <summary>The vehicle's battery drive (the generator set's hybrid, Framework 0.104.0), if it has one. Found once: the speed cap and
    /// the fuel patch ask every tick.</summary>
    public IVehicleBatteryDrive? BatteryDrive
    {
        get
        {
            if (!batteryDriveFound && Vehicle != null)
            {
                batteryDriveFound = true;
                batteryDrive = Vehicle.AllComps.OfType<IVehicleBatteryDrive>().FirstOrDefault();
            }
            return batteryDrive;
        }
    }

    CompPhobosHitch? hitch;
    bool hitchFound;

    /// <summary>The vehicle's hitch, if it has one (found once: the speed cap asks every tick).</summary>
    public CompPhobosHitch? Hitch
    {
        get
        {
            if (!hitchFound && Vehicle != null)
            {
                hitchFound = true;
                hitch = Vehicle.GetComp<CompPhobosHitch>();
            }
            return hitch;
        }
    }
    /// <summary>What the engine burns, for its sounds (diesel unless its drivetrain says petrol).</summary>
    public EngineFuel EngineFuel => Powertrain?.Def?.engine.fuel ?? EngineFuel.Diesel;

    /// <summary>Engine running by Vehicle Framework's own signs (drafted, caravan moving, in flight), before any flight phase.</summary>
    public bool EngineRunningRaw { get; private set; }

    /// <summary>A set-up vehicle's work is driving the engine now, whether or not someone also holds it on: its load is the work's, not the drivetrain's.</summary>
    public bool WorkTurningEngine { get; private set; }
    /// <summary>This vehicle's home bay, on its map. Read on the rare tick, never saved: the bay holds the assignment.</summary>
    CompPhobosDepotBay? homeBay;
    /// <summary>True from a "go to bay" order until it arrives. Not saved: at worst a load leaves the vehicle drafted on its pad.</summary>
    public bool GoingToBay;

    /// <summary>What a job's work is multiplied by: less on a vehicle standing in its own bay, or in a working service yard (Framework
    /// 0.210.0), the better of the two where it stands in both.</summary>
    public float BayWorkFactor => Phobos.Framework.Rules.ServiceYardRule.WorkFactor(ServiceYards.InWorkingYard(Vehicle), Rules.serviceYardWorkFactor,
        homeBay != null && homeBay.Holds(Vehicle), homeBay?.Props.workFactor ?? 1f);

    void RefreshBay()
    {
        if (!Vehicle.Spawned || Vehicle.Faction != Faction.OfPlayer)
        {
            homeBay = null;
            return;
        }
        homeBay = DepotBays.Of(Vehicle);
        if (GoingToBay && !Vehicle.vehiclePather.Moving)
        {
            GoingToBay = false;
            if (homeBay != null && homeBay.Holds(Vehicle) && Vehicle.Drafted) Vehicle.ignition.Drafted = false;
        }
    }

    /// <summary>Comps that may turn the engine while parked. Found once, never saved.</summary>
    List<IVehicleEngineDemand>? engineDemands;

    // Read once per update and by the inputs during it.
    public bool EngineRunning { get; private set; }
    public bool Moving { get; private set; }

    /// <summary>Whether the vehicle is moving now, on any tier (Framework 0.123.0): the player's from the last update, another faction's (whose
    /// update does not run) read from its pather on the map.</summary>
    public bool MovingNow => Tier == DetailTier.Lite ? Vehicle.Spawned && Vehicle.vehiclePather is { Moving: true } : Moving;

    /// <summary>Whether someone is driving it now, on any tier (Framework 0.123.0): the player's while its engine runs, another faction's while
    /// it moves on the map.</summary>
    public bool BeingDriven => Tier == DetailTier.Lite ? MovingNow : EngineRunning;
    public float EngineLoad { get; private set; }
    public float Ambient { get; private set; } = 21f;
    float loadAmps;
    bool? shorePowered;

    public CompProperties_PhobosVehicleSystems Props => (CompProperties_PhobosVehicleSystems)props;

    /// <summary>Whether the service runs for this vehicle: it has systems and the player has not swept them out.</summary>
    public bool Usable => Vehicle != null && Props.systems.Count > 0 && !VehicleSystems.Swept;

    // ---- A trailer's systems (Framework 0.133.0; docs/development/vehicle-trailers.md, "A trailer's own systems") ----

    /// <summary>Whether this vehicle is a trailer: towed, with no engine, crew or hitch of its own.</summary>
    public bool IsTrailer => Props.trailer;

    /// <summary>The tractor catching this trailer up while it rides off the map in the hitch's holder, where nothing ticks it. Set only
    /// for the length of that update; never saved.</summary>
    CompPhobosVehicleSystems? catchingUpFor;

    /// <summary>Whether what tows this trailer is moving: on a map, its tractor's pather (the hitch drags the trailer cell by cell, so
    /// the trailer's own pather never moves); off the map, the tractor that is catching it up.</summary>
    bool TowMoving()
    {
        if (catchingUpFor != null) return catchingUpFor.Moving;
        // The vehicle that drives the line, not the one just in front: in a line that one is itself towed, and its own wheels never "move"
        // (the fault-read of 21 Nov 2026, H6: nothing behind an articulated tractor's rear half, or behind a plough, wore its tyres).
        return Hitches.Towing(Vehicle) != null && CompPhobosHitch.HeadOf(Vehicle) is { Spawned: true, vehiclePather.Moving: true };
    }

    /// <summary>
    /// The tractor's rare tick, while this trailer rides with it off the map (a caravan, the garage): brings the trailer's systems up to
    /// now, moving as the tractor moves. A trailer in the holder has no ticks of its own, so without this its tyres would never wear on
    /// the world map, and its next update on a map would count every hour away as standing still.
    /// </summary>
    public void CatchUpTowedBy(CompPhobosVehicleSystems tractor)
    {
        if (!IsTrailer || catchingUpFor != null) return;
        catchingUpFor = tractor;
        try { UpdateNow(); }
        finally { catchingUpFor = null; }
    }

    /// <summary>
    /// How closely this vehicle is simulated (the core's DetailTierRule; owner, 8 Nov 2026): the player's own in full; another
    /// faction's lightly while on a map (hits are counted, nothing evolves) and not at all off it. Re-read on the rare tick, so a
    /// captured vehicle wakes up on the player's settings (owner, 11 Nov 2026). Derived, never saved.
    /// </summary>
    public DetailTier Tier { get; private set; } = DetailTier.Full;

    DetailTier ReadTier() => Vehicle == null ? DetailTier.Off
        : DetailTierRule.For(Vehicle.Faction == Faction.OfPlayer, Vehicle.Spawned);

    public IReadOnlyList<SystemState> States
    {
        get
        {
            EnsureStates();
            return states ?? (IReadOnlyList<SystemState>)System.Array.Empty<SystemState>();
        }
    }

    static PhobosVehicleRulesDef Rules => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();

    bool Enabled(SystemState state) => !VehicleSystemsMod.Settings.disabledSystems.Contains(state.Def.defName) && (!state.Def.needsClosedCab || ClosedCab)
        && (state.Def.needsGrant == null || Grants(state.Def.needsGrant))
        && (state.Def.needsStat == null || Vehicle.GetStatValue(state.Def.needsStat) > 0f);

    /// <summary>Whether the vehicle has this ability for a system's needsGrant: a fitting grants it, or the design has it built in (Framework
    /// 0.320.0: an engine turbocharged as built has its turbocharger).</summary>
    public bool Grants(string ability) => Fittings.Grants(ability) || (ability == Turbochargers.Grant && Turbochargers.BuiltIn(Props));

    /// <summary>Whether a system runs on this vehicle now (not switched off in the settings, and its cab is closed if it needs one).</summary>
    public bool IsEnabled(SystemState state) => Enabled(state);

    /// <summary>The context of one of this vehicle's systems, if it has it and it runs now (Framework 0.129.0), for an input that reads another
    /// system's figures.</summary>
    public SystemContext? ContextOf(PhobosVehicleSystemDef def)
    {
        if (states == null) return null;
        foreach (SystemState state in states)
        {
            if (state.Def == def && Enabled(state)) return new SystemContext(this, state);
        }
        return null;
    }

    /// <summary>Counts up whenever something the Health tab shows may have changed (a hit, a repair, an update), so the tab rebuilds
    /// its rows then and only then. Derived, never saved.</summary>
    public int HealthViewVersion { get; private set; }

    /// <summary>The last day of each shown quantity, for the Systems tab's graphs.</summary>
    public QuantityHistory History { get; } = new();

    bool? closedCab;

    /// <summary>The cab's windows are down (Framework 0.233.0, hotboxing): smoke clears and the cab's air meets the outside faster. Saved.</summary>
    public bool WindowsDown;

    /// <summary>
    /// Whether colonists may use this vehicle by themselves while it is parked (Framework 0.289.0, people use parked vehicles; the owner's card
    /// of 25 Nov 2026: one switch per vehicle, on). Off keeps its stores for the road. Saved; set through <see cref="VehicleVisits.SetOpen"/>.
    /// </summary>
    public bool OpenToColony = true;

    /// <summary>Another faction's vehicle has had its one roll for arriving hotboxed (Framework 0.292.0: saved, so a load never rolls again).</summary>
    public bool RaiderHotboxRolled;

    /// <summary>Winds the windows down or up (Framework 0.253.0: the winder heard going down, S145, the owner's pick).</summary>
    public void SetWindowsDown(bool down)
    {
        if (down && !WindowsDown) VehicleSystemSounds.PlayOnce(this, VehicleSystemSoundDefOf.PhobosVehicleSystems_WindowsDown);
        WindowsDown = down;
    }

    /// <summary>Whether this vehicle's cab shelters everyone aboard (<see cref="Cabins"/>: the vehicle's own extension, its loadout's
    /// cabin block, then whether its author left every seat exposed).</summary>
    public bool ClosedCab
    {
        get
        {
            if (closedCab == null)
            {
                (bool closed, CabinReason reason) = Cabins.Judge(Vehicle?.VehicleDef, Props.loadout);
                closedCab = closed;
                if (!closed && Vehicle != null && Seen.Wants("vehicles.open-cab", Vehicle, reason.ToString()))
                    Seen.Note("vehicles.open-cab", Vehicle, Glass.PartOf(Vehicle) != null ? "open, with glass " + Glass.KindOf(Vehicle) : "open, no glass",
                        reason.ToString());
            }
            return closedCab.Value;
        }
    }

    /// <summary>Why the vehicle will not move now, cached on each update for the caravan's inspect text and Vehicle Framework's
    /// world-map checks; null when it may.</summary>
    public string? MoveRefusal { get; private set; }

    /// <summary>
    /// Why colonists should not take this vehicle out on their own now, or null: a stage carrying <see cref="Effect_KeepFromWorkTrips"/>
    /// (boiling over, nearly out of coolant, no oil pressure; Framework 0.183.0, VW8). The player's orders are never refused by it. Cached
    /// beside <see cref="MoveRefusal"/> at every update; derived, never saved.
    /// </summary>
    public string? WorkTripRefusal { get; private set; }

    /// <summary>Whether a load of the loadout is on: switched on by the player when it has a switch, and its own condition holds.</summary>
    public bool SwitchOn(string name)
    {
        ElectricalLoadSpec? load = LoadNamed(name);
        return load != null && IsOn(load);
    }

    /// <summary>The load of this name, the loadout's or a fitting's, or null.</summary>
    public ElectricalLoadSpec? LoadNamed(string name)
    {
        IReadOnlyList<ElectricalLoadSpec> loads = Loads;
        for (int i = 0; i < loads.Count; i++)
        {
            if (loads[i].name == name) return loads[i];
        }
        return null;
    }

    /// <summary>The least a system's parameter may be, set by what is fitted (a large battery). Derived, never saved.</summary>
    public void SetParameterFloor(string name, float floor) => (ParameterFloors ??= new Dictionary<string, float>())[name] = floor;

    internal Dictionary<string, float>? ParameterFloors { get; private set; }

    public bool LoadActive(string name)
    {
        if (states == null || states.Count == 0) return false;
        foreach (ElectricalLoadSpec load in Loads)
        {
            if (load.name != name) continue;
            if (load.toggle && !IsOn(load)) return false;
            if (!AutoHolds(load)) return false;
            if (IsShorted(load)) return false;
            if (OnHouseBattery(load))
            {
                if (!HouseBatteryRuns) return false;
            }
            else if (KeptForStart(load)) return false;
            else if (ElectricsRule.IsShed(load.shedOrder, ShedDepth)) return false;
            // Check C1 (Framework 0.129.1): a flat battery with nothing else to carry the loads powers none of them.
            else if (!LoadsPowered) return false;
            var context = new SystemContext(this, states[0]);
            if (load.available != null && !load.available.Holds(context)) return false;
            return load.when == null || load.when.Holds(context);
        }
        return false;
    }

    /// <summary>What one load is doing now, for the Systems tab (Framework 0.117.0). Reads the same rules as <see cref="LoadActive"/>.</summary>
    public LoadState StateOf(ElectricalLoadSpec load)
    {
        if (states == null || states.Count == 0) return LoadState.Idle;
        var context = new SystemContext(this, states[0]);
        if (load.available != null && !load.available.Holds(context)) return LoadState.NotFitted;
        if (load.toggle && !IsOn(load)) return LoadState.SwitchedOff;
        if (IsShorted(load)) return LoadState.Shorted;
        // Switched on, but its automatic rule does not want it yet (Framework 0.171.0): standing by, as for its own "when".
        if (!AutoHolds(load)) return LoadState.Idle;
        // A camp load on a house battery stops at its cut-off instead of being shed (Framework 0.129.0).
        if (OnHouseBattery(load))
        {
            if (!HouseBatteryRuns) return LoadState.NoPower;
            return load.when != null && !load.when.Holds(context) ? LoadState.Idle : LoadState.Running;
        }
        // Framework 0.276.3: out to keep enough in the starting battery to start the engine (camp lights with no house battery).
        if (KeptForStart(load)) return LoadState.KeptForStart;
        if (ElectricsRule.IsShed(load.shedOrder, ShedDepth)) return LoadState.Shed;
        // Check C1 (Framework 0.129.1): the same rule as LoadActive, so the tab and the load agree; before, the tab called a load dead on a
        // flat battery even while the alternator carried it, and the load ran on regardless.
        if (!LoadsPowered) return LoadState.NoPower;
        return load.when != null && !load.when.Holds(context) ? LoadState.Idle : LoadState.Running;
    }

    /// <summary>
    /// Whether a load could run now if its work began (Framework 0.115.0, W4): it is fitted, switched on, not shed, and it has power (since
    /// 0.129.1 the one rule, <see cref="LoadsPowered"/>; a camp load, its house battery). Unlike <see cref="LoadActive"/> it does not ask the load's own "when" (a winch's load runs only while it pulls). A vehicle whose
    /// loadout has no such load is not refused: the load is not modelled there.
    /// </summary>
    public bool LoadCouldRun(string name)
    {
        ElectricalLoadSpec? load = LoadNamed(name);
        if (load == null) return true;
        if (states == null || states.Count == 0) return true;
        if (load.toggle && !IsOn(load)) return false;
        if (!LoadAvailable(load)) return false;
        if (IsShorted(load)) return false;
        if (OnHouseBattery(load)) return HouseBatteryRuns;
        if (KeptForStart(load)) return false;
        if (ElectricsRule.IsShed(load.shedOrder, ShedDepth)) return false;
        return LoadsPowered;
    }

    /// <summary>Whether this vehicle has the load fitted (its <c>available</c> condition), so its switch shows.</summary>
    public bool LoadAvailable(ElectricalLoadSpec load) =>
        load.available == null || states == null || states.Count == 0 || load.available.Holds(new SystemContext(this, states[0]));

    /// <summary>
    /// Whether the cab has automatic climate control fitted: the player's setting gives it to every closed cab; otherwise the vehicle's
    /// own cab extension, its loadout's cabin block, or a spacer-tech design or later (agent rule, owner to revise).
    /// </summary>
    public bool ClimateControlFitted
    {
        get
        {
            if (!ClosedCab) return false;
            if (VehicleSystemsMod.Settings.climateControlAll) return true;
            climateFitted ??= Vehicle?.VehicleDef?.GetModExtension<PhobosVehicleCabinExtension>()?.climateControl == true
                || Props.loadout?.cabin.climateControl == true
                || (Vehicle?.VehicleDef != null && Vehicle.VehicleDef.techLevel >= TechLevel.Spacer);
            return climateFitted.Value;
        }
    }

    bool? climateFitted;

    /// <summary>Whether this vehicle tops its tank up from the caravan's cargo on the world map (Framework 0.137.0; owner, 18 Nov 2026:
    /// off until the player switches it on, so fuel carried to sell is not burned). Saved.</summary>
    public bool RefuelFromCargo;

    /// <summary>Whether the player has been told the cargo holds no more of this vehicle's fuel, so it is said once (derived, never saved).</summary>
    internal bool CargoFuelGoneTold;

    /// <summary>An aircraft set down on a journey to refuel from its cargo (Framework 0.139.0): the tick it lifts off again, 0 when it is
    /// not stopped. Saved, so a stop survives a save and load; CargoRefuel owns it.</summary>
    public int LandedRefuelUntil;
    /// <summary>The aircraft flew on the world map and has not yet been judged on arrival (avionics A4, RoughLandings). Saved, in case the game
    /// is saved in flight.</summary>
    public bool ArrivingFromWorld;
    /// <summary>Whether it was in the air over its map at the last rare tick (RoughLandings). Derived, never saved.</summary>
    public bool WasAirborneOnMap;

    /// <summary>Whether this session has already grounded the aircraft for its stop (Vehicle Framework's load puts it back in the air, so
    /// the stop is put back once after a load). Derived, never saved.</summary>
    internal bool LandedHoldApplied;

    /// <summary>Whether the player has been told the aircraft cannot set down here (water, mountains, an enemy's ground), so it is said
    /// once until it can. Derived, never saved.</summary>
    internal bool LandingRefusedTold;

    // ---- Saving ----

    /// <summary>Framework 0.247.0's each engine's hot-section life, by its role: read from older saves only, then taken into
    /// <see cref="partFigures"/> and dropped (PartFigures.Migrate).</summary>
    internal Dictionary<string, float>? engineHotSection;
    /// <summary>Each part's own figure of a quantity kept per part, by "quantity|part key" (Framework 0.248.0); null until one is kept. Saved.</summary>
    internal Dictionary<string, float>? partFigures;
    PartFigures? perPart;
    StartSequence? startSequence;
    /// <summary>Whether Vehicle Framework had it in flight at the last update, and where it stood then (StripDebris). Derived, never saved.</summary>
    internal bool debrisWasFlying;
    internal IntVec3 debrisLastCell = IntVec3.Invalid;
    SideTanks? sideTanks;

    /// <summary>An aircraft's engines lit one after another before take-off (Framework 0.252.0). Derived, never saved.</summary>
    public StartSequence StartSequence => startSequence ??= new StartSequence(this);

    /// <summary>A twin's tank on each side and its cross-feed (Framework 0.252.0). Its split and mode are saved here.</summary>
    public SideTanks SideTanks => sideTanks ??= new SideTanks(this);

    /// <summary>Quantities kept per part: each engine's hot-section life, each wheel's brake wear (Framework 0.248.0, PartFigures).</summary>
    public PartFigures PartFigures => perPart ??= new PartFigures(this);

    public override void PostExposeData()
    {
        base.PostExposeData();
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            savedValues = new Dictionary<string, float>();
            if (states != null)
            {
                foreach (SystemState state in states)
                {
                    for (int i = 0; i < state.values.Length; i++)
                    {
                        if (!state.Def.quantities[i].Derived) savedValues[ValueKey(state, i)] = (float)state.values[i];
                    }
                }
            }
            savedPartHealth = new Dictionary<string, float>();
            if (Vehicle?.statHandler != null)
            {
                foreach (string key in Props.addedKeys)
                {
                    VehicleComponent? part = Vehicle.statHandler.GetComponent(key);
                    if (part != null) savedPartHealth[key] = part.Health;
                }
            }
            servicedSaved = servicedThisRequest.ToList();
        }
        Scribe_Collections.Look(ref savedValues, "PhobosVehicleSystems_Values", LookMode.Value, LookMode.Value);
        // Framework 0.247.0 (per-engine realism step 4) saved each engine's hot-section life by its role; 0.248.0 reads it once, into the
        // per-part figures, and writes the new key only.
        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            Scribe_Collections.Look(ref engineHotSection, "PhobosVehicleSystems_EngineHotSection", LookMode.Value, LookMode.Value);
        }
        Scribe_Collections.Look(ref partFigures, "PhobosVehicleSystems_PartFigures", LookMode.Value, LookMode.Value);
        // Framework 0.252.0: a twin's side tanks (step 11): how its fuel is split, which side leaks, and its cross-feed.
        Scribe_Values.Look(ref SideTanks.imbalance, "PhobosVehicleSystems_SideTankImbalance", 0f);
        Scribe_Values.Look(ref SideTanks.leakSide, "PhobosVehicleSystems_SideTankLeak", -1);
        Scribe_Values.Look(ref SideTanks.mode, "PhobosVehicleSystems_CrossFeed", CrossFeedMode.Automatic);
        Scribe_Values.Look(ref SideTanks.crewOpened, "PhobosVehicleSystems_CrossFeedCrewOpened", false);
        Scribe_Collections.Look(ref loadSwitches, "PhobosVehicleSystems_Loads", LookMode.Value, LookMode.Value);
        // The graphs' history: the owner's exception to "derived state is never saved" (8 Nov 2026), a few kilobytes per vehicle.
        History.ExposeData(Props.systems);
        Scribe_Collections.Look(ref savedPartHealth, "PhobosVehicleSystems_PartHealth", LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref servicedSaved, "PhobosVehicleSystems_Serviced", LookMode.Value);
        Scribe_Collections.Look(ref pipeUnderlays, "PhobosVehicleSystems_PipeUnderlays", LookMode.Value, LookMode.Deep);
        pipeUnderlays ??= new Dictionary<string, Phobos.Framework.Persistence.PipeUnderlay>();
        Scribe_Values.Look(ref lastUpdateTick, "PhobosVehicleSystems_LastUpdate", -1);
        Scribe_Values.Look(ref WindowsDown, "PhobosVehicleSystems_WindowsDown", false);
        Scribe_Values.Look(ref OpenToColony, "PhobosVisit_OpenToColony", true);
        Scribe_Values.Look(ref RaiderHotboxRolled, "PhobosLifeAboard_RaiderRolled", false);
        Scribe_Values.Look(ref serviceRequested, "PhobosVehicleSystems_ServiceRequested", false);
        Scribe_Values.Look(ref RefuelFromCargo, "PhobosVehicleSystems_RefuelFromCargo", false);
        Scribe_Values.Look(ref LandedRefuelUntil, "PhobosVehicleSystems_LandedRefuelUntil", 0);
        Scribe_Values.Look(ref ArrivingFromWorld, "PhobosVehicleSystems_ArrivingFromWorld", false);
        Conditions.ExposeData();
        Repairs.ExposeData();
        Fittings.ExposeData();
        Diagnostics.ExposeData();
        Events.ExposeData();
        Book.ExposeData("PhobosVehicleSystems_Logbook");
        Scribe_Values.Look(ref LastUnderFireTick, "PhobosVehicleSystems_LastUnderFire", -1);
        Scribe_Deep.Look(ref Trip, "PhobosVehicleSystems_Trip");
        Practised.ExposeData();
        Scribe_Values.Look(ref ArrivalFuelSet, "PhobosVehicleSystems_ArrivalFuelSet", false);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            savedValues ??= new Dictionary<string, float>();
            loadSwitches ??= new Dictionary<string, bool>();
            savedPartHealth ??= new Dictionary<string, float>();
            servicedSaved ??= new List<string>();
            servicedThisRequest.Clear();
            servicedThisRequest.AddRange(servicedSaved);
            states = null;
            healthsRestored = false;
        }
    }

    static string ValueKey(SystemState state, int quantity) => state.Def.defName + "/" + state.Def.quantities[quantity].name;

    // ---- Lifecycle ----

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (Usable) spawned.AddOrUpdate(Vehicle, this);
        VehicleSystemSounds.Settle(this);
        engineSound ??= EngineSound.For(Vehicle);
        fittings?.ReapplyGrades();    // before anything clamps a part to its maximum
        fittings?.SyncFuelCapacity(FuelExcess.Keep);    // extra tanks, before the tank first burns and is trimmed to capacity
        RestorePartHealths();
        if (Usable && !respawningAfterLoad) Fittings.RollEnemySet();
        // Framework 0.321.0: what it throws up as it moves, drawn by its map's effects emitter (registered only; nothing is made here).
        VehicleMotionFx.Register(this);
        // Aircraft service-life upgrades: a vehicle from an older save keeps what its loadout no longer starts with.
        if (Usable) Fittings.GiveOlderSaveFittings();
        // Raiders arrive with the journey behind them (Framework 0.192.0, EM5): set once, on another faction's first spawn.
        if (!respawningAfterLoad) RaidFuel.SetOnArrival(this);
        powertrain?.ForgetGround();
        // After a load, once every thing stands: the old hidden pipes earlier versions laid are taken back once and made visible, and the pipes and conduits
        // are laid as the vehicle stands. Never from here directly (AGENTS.md: nothing is spawned from PostSpawnSetup).
        if (respawningAfterLoad && Usable) LongEventHandler.ExecuteWhenFinished(SettlePipesAfterLoad);
    }

    void SettlePipesAfterLoad()
    {
        if (!Vehicle.Spawned) return;
        VehiclePipes.AfterLoad(this);
        VehiclePipes.Update(this);
    }

    /// <summary>The work lamps (the work lights family, Framework 0.103.0): their light on the map, derived and never saved.</summary>
    public WorkLamps Lamps => lamps ??= new WorkLamps(this);
    WorkLamps? lamps;

    /// <summary>The headlights' beam on the map (Framework 0.104.0): derived and never saved.</summary>
    public Headlights Beams => beams ??= new Headlights(this);

    bool ranWithFuel;

    /// <summary>The tank is empty and the engine was running when it last had fuel (T6): the pump ran dry.</summary>
    public bool RanDry => ranWithFuel && Vehicle?.CompFueledTravel is { EmptyTank: true };
    Headlights? beams;

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        lamps?.PutOut();
        beams?.PutOut();
        // A gravship lifts its pipes along with it.
        if (mode != DestroyMode.WillReplace) VehiclePipes.Leave(this);
        VehicleSystemSounds.EndAll(this);
        engineSound?.End();
        if (Vehicle != null) VehicleMotionFx.Unregister(Vehicle, map);
        base.PostDeSpawn(map, mode);
        if (Vehicle != null) spawned.Remove(Vehicle);
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        base.PostDestroy(mode, previousMap);
        if (Vehicle != null) CabClimate.Forget(Vehicle);
    }

    public override void EventRegistration()
    {
        base.EventRegistration();
        if (registered || !Usable) return;
        registered = true;
        var events = new HashSet<VehicleEventDef>
        {
            VehicleEventDefOf.IgnitionOn, VehicleEventDefOf.IgnitionOff, VehicleEventDefOf.MoveStart, VehicleEventDefOf.MoveStop,
        };
        foreach (ResolvedSystem system in Props.systems)
        {
            foreach (EventRule rule in system.def.onEvents)
            {
                if (rule.eventDef != null) events.Add(rule.eventDef);
            }
        }
        foreach (VehicleEventDef eventDef in events)
        {
            VehicleEventDef captured = eventDef;
            Vehicle.AddEvent(captured, () => OnVehicleEvent(captured));
        }
        // The Health tab's rows go stale on a hit or a repair (DamageTaken fires under Combat Extended too; HealthChanged does not).
        Vehicle.AddEvent(VehicleEventDefOf.DamageTaken, () => HealthViewVersion++);
        Vehicle.AddEvent(VehicleEventDefOf.Repaired, () => HealthViewVersion++);
        Vehicle.AddEvent(VehicleEventDefOf.HealthChanged, () => HealthViewVersion++);
        // Under Combat Extended a hit raises "damage taken" alone, so the part requirements are asked again here (fault-read K1).
        Vehicle.AddEvent(VehicleEventDefOf.DamageTaken, () => WeaponMounts.AfterDamage(Vehicle));
        // People use parked vehicles (Framework 0.289.0): visitors climb out as the vehicle is drafted or sets off, before it goes anywhere;
        // one still aboard as it leaves the map rides on as a rider.
        Vehicle.AddEvent(VehicleEventDefOf.IgnitionOn, () => VehicleVisits.SettingOff(Vehicle));
        Vehicle.AddEvent(VehicleEventDefOf.MoveStart, () => VehicleVisits.SettingOff(Vehicle));
        Vehicle.AddEvent(VehicleEventDefOf.Despawned, () => VehicleVisits.Left(Vehicle));
        // Taking off through Vehicle Framework's own launch need not draft it first (fault-read F7); someone getting out ends their visit (F8).
        Vehicle.AddEvent(VehicleEventDefOf.AerialVehicleLaunch, () => VehicleVisits.SettingOff(Vehicle));
        Vehicle.AddEvent(VehicleEventDefOf.PawnExited, () => VehicleVisits.Exited(Vehicle));
        // Defenders in their trucks (Framework 0.292.0): a hit on another faction's vehicle at its base brings its sitters out.
        Vehicle.AddEvent(VehicleEventDefOf.DamageTaken, () => MapComponent_PhobosBaseDefenders.Hit(Vehicle));
        // The ammunition locker (Framework 0.319.0): what it holds decides the cargo figure, so cargo coming or going works it out again.
        Vehicle.AddEvent(VehicleEventDefOf.CargoAdded, () => AmmoLocker.CargoChanged(Vehicle));
        Vehicle.AddEvent(VehicleEventDefOf.CargoRemoved, () => AmmoLocker.CargoChanged(Vehicle));
        // An aircraft taking off hears the refusals the player set to warn only (Framework 0.110.0).
        if (Vehicle.CompVehicleLauncher != null) Vehicle.AddEvent(VehicleEventDefOf.AerialVehicleLaunch, WarnAtLaunch);
        // Framework 0.254.0: grit thrown up as it leaves the ground through Vehicle Framework's own launch.
        if (Vehicle.CompVehicleLauncher != null) Vehicle.AddEvent(VehicleEventDefOf.AerialVehicleLaunch, () => StripDebris.OnLaunch(this));
        if (Powertrain != null)
        {
            // Once a second while it moves or flies, the drivetrain or turbine takes a sample; cargo and passengers change its mass.
            Vehicle.AddEvent(VehicleEventDefOf.ScanShort, OnScanShort);
            Vehicle.AddEvent(VehicleEventDefOf.CargoAdded, () => Powertrain?.MarkMassDirty());
            Vehicle.AddEvent(VehicleEventDefOf.CargoRemoved, () => Powertrain?.MarkMassDirty());
            Vehicle.AddEvent(VehicleEventDefOf.PawnEntered, () => Powertrain?.MarkMassDirty());
            Vehicle.AddEvent(VehicleEventDefOf.PawnExited, () => Powertrain?.MarkMassDirty());
            // An aircraft lifting off through Vehicle Framework's own launch pays for a lift beyond its engines (the player's setting).
            if (Powertrain.ChecksLiftAtLaunch) Vehicle.AddEvent(VehicleEventDefOf.AerialVehicleLaunch, () => HoverLimits.OnLaunch(Vehicle));
        }
    }

    void OnScanShort()
    {
        Powertrain? train = Powertrain;
        if (train == null || !Usable || Tier != DetailTier.Full) return;
        bool active = train.IsAircraft ? Vehicle.Drafted || train.AircraftRunning : Vehicle.Spawned && Vehicle.vehiclePather != null && Vehicle.vehiclePather.Moving;
        if (!active && train.Cap >= 1f && train.Result.Rpm <= 0) return;
        using (PhobosTiming.Measure("vehicle sample")) train.Sample();
        // The engine load the systems read follows the sample just taken, so it never lags a change of flight phase or gear by the
        // seconds until the next full update (the inspect text showed "Load 50%" beside "Engine load 100%"; owner's screenshot, 11 Nov 2026).
        // Since Framework 0.130.0 (step S1) the sample refreshes the engine's one picture, which the load is read from.
        if (train.Full && EngineRunning && !WorkTurningEngine)
        {
            Engine = Engine.WithDrive((float)train.Result.Rpm, (float)train.Result.Load);
            EngineLoad = Engine.Load;
            // Step S4 (Framework 0.130.0): a fault's lost power holding the vehicle back where power runs short.
            if (EnginePowerFactor < 1f && train.Result.Limit == DriveLimit.Power && SeenInPlay.On) Seen.Note("vehicles.engine-power-lost", Vehicle, EnginePowerFactor.ToStringPercent());
        }
    }

    /// <summary>The engine sound that follows rpm and load, for a vehicle whose Def names one (derived, never saved).</summary>
    EngineSound? engineSound;

    /// <summary>Every tick, only for a vehicle with an engine sound of ours: a few assignments that keep its pitch and blend gliding.</summary>
    public override void CompTick()
    {
        base.CompTick();
        engineSound?.Tick(this);
        beams?.Tick();
        Overrun?.Tick();
    }

    public override void CompTickRare()
    {
        base.CompTickRare();
        VehicleCostMeasure.CheckDone();
        RefreshBay();
        DetailTier was = Tier;
        Tier = ReadTier();
        bool firstRead = !tierRead;
        tierRead = true;
        if (Tier != was && !firstRead && VehicleSystemsMod.Logger.DebugEnabled)
        {
            VehicleSystemsMod.Logger.Debug(Vehicle.LabelShort + " (" + Vehicle.ThingID + ", " + (Vehicle.Faction?.Name ?? "no faction") + ") detail tier " + was + " -> " + Tier);
        }
        // A vehicle that has just become the player's (captured) starts its clock now: the hours it spent as another faction's,
        // when nothing evolved, are not caught up at once. From here it runs on the player's own settings.
        if (Tier == DetailTier.Full && was != DetailTier.Full && Current.Game != null) lastUpdateTick = Find.TickManager.TicksGame;
        // Avionics A4 (aircraft service-life upgrades): a landing since the last look, any faction's aircraft.
        RoughLandings.Watch(this);
        // Framework 0.321.0: drips under a vehicle leaking fuel, any faction's.
        VehicleFx.FuelDrips(Vehicle);
        // Walk-in cabins (Framework, vehicle cabins): close an empty one on the ground, and walk idle colonists out.
        if (Vehicle.GetComp<CompPhobosVehicleCabin>() is { } cabin) VehicleCabins.RareLook(cabin);
        // Life aboard (Framework 0.233.0): on a map here; on the road, the world component's look.
        // In flight between tiles too (Framework 0.236.0): Vehicle Framework keeps the aircraft among the world's pawns and ticks it there.
        if (Usable && (Vehicle.Spawned || Vehicle.GetAerialVehicle() != null)) LifeAboard.Look(this, null);
        switch (Tier)
        {
            case DetailTier.Full:
                using (PhobosTiming.Measure("vehicle rare tick / full")) UpdateNow();
                // A trailer riding with this vehicle off the map does not tick: this vehicle catches it up (Framework 0.133.0).
                if (!Vehicle.Spawned && Hitch is { HoldsTowed: true, Trailer: { } held })
                {
                    // The vehicle held, and everything behind it in the line.
                    if (held.GetComp<CompPhobosVehicleSystems>() is { Usable: true } heldSystems) heldSystems.CatchUpTowedBy(this);
                    // Its cargo has no ticks either: without this, food in a towed trailer never aged on the road (Framework 0.216.0).
                    HeldGoods.AgeTowed(held);
                    foreach (VehiclePawn further in CompPhobosHitch.Behind(held))
                    {
                        if (further.GetComp<CompPhobosVehicleSystems>() is { Usable: true } furtherSystems) furtherSystems.CatchUpTowedBy(this);
                        HeldGoods.AgeTowed(further);
                    }
                }
                NoticeStopped();
                // Journeys away from any map (Framework 0.181.0, EM25): one state read on a vehicle at home.
                VehicleTrips.Look(this);
                // Hours on type and "their" vehicle (Framework 0.190.0, EM11 and EM12): a few dictionary additions while it moves.
                VehiclePractice.Look(this);
                OwnVehicles.LookHome(this);
                Lamps.Update();
                Beams.Update();
                CookOff.RareTick(this);
                // The work store (Framework 0.272.0): what the work needs comes out of the cargo room; leftovers are set down once no work is pending.
                WorkStore?.TakeOutOfCargo(this);
                WorkStore?.SetDownLeftovers(this);
                FireWear.RareTick(this, lite: false);
                // A leaking tank feeds the fire (Framework 0.245.0, FR4; a setting, off): read only while the vehicle burns.
                LeakFire.RareTick(this);
                // Fire zones, their spread and the suppression systems (Framework 0.241.0, FR10): read only while the vehicle burns.
                if (Vehicle.GetComp<CompPhobosFireFighting>() is { } fireFighting) FireFighting.Look(fireFighting);
                // The player's crew leaves a burning vehicle once it stops (Framework 0.245.0, FR8; a setting): after suppression has had its go.
                CrewBailOut.PlayerRareTick(this);
                if (++pipeLooks >= RareTicksPerPipeLook)
                {
                    pipeLooks = 0;
                    using (PhobosTiming.Measure("vehicle pipes")) VehiclePipes.Update(this);
                }
                break;
            case DetailTier.Lite:
                // Hits are counted (Combat Extended writes part health without an event), nothing evolves.
                using (PhobosTiming.Measure("vehicle rare tick / lite"))
                {
                    if (states != null) SweepPartHealth();
                    UpdateLiteQuantities();
                }
                Beams.Update();
                // Any faction's burning vehicle cooks off its ammunition (G13): cheap, read only while it burns.
                CookOff.RareTick(this);
                // Fire wears another faction's vehicle as it does the player's (Framework 0.221.0, FR6; a setting).
                FireWear.RareTick(this, lite: true);
                LeakFire.RareTick(this);
                // Any faction's vehicle with suppression fitted puts its fire out (Framework 0.241.0, FR10).
                if (Vehicle.GetComp<CompPhobosFireFighting>() is { } fireFightingLite) FireFighting.Look(fireFightingLite);
                // Another faction's crew leaves a burning vehicle or a dead hull (Framework 0.115.0, C2).
                CrewBailOut.RareTick(this);
                break;
            default:
                beams?.PutOut();
                break;
        }
    }

    /// <summary>
    /// The few quantities another faction's vehicle keeps up on the light tier (Framework 0.123.0; owner, 17 Nov 2026: raiders feel the driver
    /// and the dark too): the rules Def's <c>liteTierQuantities</c>, today the driver's fitness (G1, G2) and driving in the dark (G9). Each is
    /// read from its own inputs and takes its stage quietly (no message, no effect on entering), so only its stat factors reach the vehicle:
    /// a raid with a hurt or tired driver, or a vehicle with no headlights at night, comes in slower. Nothing else evolves. Read on the rare
    /// tick: a few capacity reads and one light-grid look per moving vehicle.
    /// </summary>
    void UpdateLiteQuantities()
    {
        List<string> names = Rules.liteTierQuantities;
        if (names.Count == 0 || !Usable || Current.Game == null) return;
        EnsureStates();
        if (states == null) return;
        bool changed = false;
        foreach (SystemState state in states)
        {
            if (!Enabled(state)) continue;
            SystemContext? context = null;
            foreach (string name in names)
            {
                int q = state.Def.QuantityIndex(name);
                if (q < 0 || state.Def.quantities[q].derivedFrom is not { } inputs) continue;
                context ??= new SystemContext(this, state);
                double value = 1;
                foreach (VehicleSystemInput input in inputs) value *= input.Evaluate(context);
                state.values[q] = SystemIntegrator.Clamp(value, state.min[q], state.max[q]);
                int stage = StageOf(state, q);
                if (stage == state.stage[q]) continue;
                state.stage[q] = stage;
                changed = true;
            }
        }
        if (!changed) return;
        RecacheStatFactors();
        if (StatFactor(VehicleStatDefOf.MoveSpeed) < 1f && Seen.Wants("vehicles.lite-tier-slowed", Vehicle)) Seen.Note("vehicles.lite-tier-slowed", Vehicle, StatFactor(VehicleStatDefOf.MoveSpeed).ToString("0.##"));
    }

    /// <summary>
    /// Gives the appended parts back the healths saved by key, since Vehicle Framework loads them by place in the list: a part new since
    /// the save starts whole, and none is left above its maximum (PartHealthRule; Framework 0.83.1).
    /// </summary>
    void RestorePartHealths()
    {
        if (healthsRestored || Vehicle?.statHandler == null) return;
        healthsRestored = true;
        bool saveHeldParts = savedPartHealth.Count > 0;
        foreach (string key in Props.addedKeys)
        {
            VehicleComponent? part = Vehicle.statHandler.GetComponent(key);
            if (part == null) continue;
            double? saved = savedPartHealth.TryGetValue(key, out float value) ? value : null;
            // A part added one for each part of a role to an older save took the worn health of the one it replaces as it was made
            // (PartsOnLoad, Framework 0.273.3): it keeps that, rather than starting whole (fault-read 25 Nov 2026, E1).
            if (saved == null && EachPart.HealthFrom(Vehicle.VehicleDef, key) != null) saved = part.Health;
            // A spall liner starts at nothing, "not fitted", unless the save holds its health or its upgrade is fitted (Framework 0.207.0):
            // Vehicle Framework makes every part whole, which would hand a new truck a liner nobody fitted.
            // The turbocharger likewise starts at nothing on an engine without one (Framework 0.320.0).
            float health = key == Spall.LinerKey
                ? Spall.StartingHealth(Vehicle, part, saved.HasValue ? (float)saved.Value : null)
                : key == Turbochargers.Key ? Turbochargers.StartingHealth(this, part, saved.HasValue ? (float)saved.Value : null)
                : (float)PartHealthRule.Restored(saved, saveHeldParts, part.Health, part.MaxHealth);
            if (Mathf.Abs(part.Health - health) > 0.01f) part.SetHealth(health);
            RememberPartHealth(part);
        }
        Vehicle.statHandler.MarkAllDirty();
    }

    void EnsureStates()
    {
        if (states != null || !Usable) return;
        states = new List<SystemState>();
        RefreshCaches();
        foreach (ResolvedSystem system in Props.systems)
        {
            var state = new SystemState(system);
            var context = new SystemContext(this, state);
            system.def.Worker.Initialise(context);
            for (int i = 0; i < state.values.Length; i++)
            {
                if (savedValues.TryGetValue(ValueKey(state, i), out float saved))
                {
                    state.values[i] = SystemIntegrator.Clamp(saved, state.min[i], state.max[i]);
                }
            }
            system.def.Worker.UpdateDerived(context);
            for (int i = 0; i < state.values.Length; i++) state.stage[i] = StageOf(state, i);
            states.Add(state);
        }
        Conditions.RestoreStages();
        if (lastUpdateTick < 0 && Current.Game != null) lastUpdateTick = Find.TickManager.TicksGame;
        RecacheStatFactors();
    }

    // ---- The update ----

    /// <summary>Catches the systems up to now: flows, stages and their effects. Safe to call often; does nothing twice in a tick.</summary>
    /// <param name="evenIfNoTimePassed">After a hit (Framework 0.120.5, fault-read A10): a second hit in the same tick found no time
    /// passed and read the systems as they were before it, so oxygen lost to it never counted. Forced, it advances by one tick.</param>
    public void UpdateNow(bool evenIfNoTimePassed = false)
    {
        if (updating || !Usable || Current.Game == null) return;
        updating = true;
        try
        {
            EnsureStates();
            RestorePartHealths();
            if (states == null) return;
            int now = Find.TickManager.TicksGame;
            double hours = ElapsedRule.Hours(now, lastUpdateTick, Rules.maxCatchUpHours);
            if (hours <= 0)
            {
                if (!evenIfNoTimePassed) return;
                hours = 1.0 / GenDate.TicksPerHour;
            }
            lastUpdateTick = now;
            SweepPartHealth();
            RefreshCaches();
            Powertrain?.ApplyFuel(hours);
            ApplyConverterDraw(hours);
            if (EngineRunning) ChargeCarriedPacks(hours);
            foreach (SystemState state in states)
            {
                if (Enabled(state)) state.Def.Worker.Advance(new SystemContext(this, state), hours);
            }
            // Framework 0.247.0, generic since 0.248.0: a per-part quantity's gain shared among its parts, the figure then the worst part's,
            // before stages are judged.
            PartFigures.Sync();
            // Framework 0.252.0: grit thrown into the intakes on rough ground, and how a twin's fuel is split between its sides.
            StripDebris.Update(this, (float)hours);
            SideTanks.Update();
            if (!stagesSettled)
            {
                // The first update: take the stages the true readings give, quietly, as where each starts (no effects on entry or exit).
                stagesSettled = true;
                foreach (SystemState state in states)
                {
                    for (int q = 0; q < state.values.Length; q++) state.stage[q] = StageOf(state, q);
                }
            }
            foreach (SystemState state in states)
            {
                // A switched-off system's values are frozen, so its stage is too: applying it would go on burning fuel or wearing a
                // part for as long as it stays off. Its stat factors are already left out (StagesInForce).
                if (Enabled(state)) EvaluateStages(state, (float)hours);
            }
            Conditions.Advance((float)hours);
            RecacheStatFactors();
            History.Sample(states, now);
            string? refusalBefore = MoveRefusal;
            MoveRefusal = FirstRefusal(draft: false);
            WorkTripRefusal = FirstWorkTripRefusal();
            if (MoveRefusal != refusalBefore && VehicleSystemsMod.Logger.DebugEnabled)
            {
                VehicleSystemsMod.Logger.Debug(Vehicle.LabelShort + " (" + Vehicle.ThingID + ") " + (MoveRefusal == null ? "may move again (was: " + refusalBefore + ")" : "will not move: " + MoveRefusal));
            }
            HealthViewVersion++;
            UpdateHookUp();
            if (ClosedCab && TryGetQuantity("cabTemperature", out float cab)) CabClimate.Set(Vehicle, cab);
            else CabClimate.Forget(Vehicle);
        }
        finally
        {
            updating = false;
        }
    }

    /// <summary>
    /// Booster packs and anything else with charges that recharge over time, carried in this vehicle's cargo, charge while its engine
    /// runs (the alternator's surplus; its draw on the electrical system is left out, an authored simplification).
    /// </summary>
    void ChargeCarriedPacks(double hours)
    {
        ThingOwner? cargo = Vehicle.inventory?.innerContainer;
        if (cargo == null) return;
        for (int i = 0; i < cargo.Count; i++)
        {
            if (cargo[i].TryGetComp<CompPhobosCharges>() is { } charges && charges.Props.rechargeHoursPerUse > 0 && !charges.Full) charges.Recharge(hours);
        }
    }

    void RefreshCaches()
    {
        VehiclePawn vehicle = Vehicle;
        Ambient = vehicle.AmbientTemperature;
        CompFueledTravel? fuel = vehicle.CompFueledTravel;
        bool hasFuel = fuel == null || !fuel.EmptyTank;
        bool flying = vehicle.CompVehicleLauncher?.inFlight ?? false;
        Vehicles.World.VehicleCaravan? caravan = vehicle.GetVehicleCaravan();
        RefreshAir(vehicle, caravan);
        bool caravanMoving = caravan != null && caravan.vehiclePather != null && caravan.vehiclePather.MovingNow;
        bool drafted = vehicle.Spawned && vehicle.Drafted;
        bool movingOnMap = vehicle.Spawned && vehicle.vehiclePather != null && vehicle.vehiclePather.Moving;
        // A trailer never moves under its own pather: the hitch sets its place each tick (Framework 0.133.0).
        Moving = movingOnMap || caravanMoving || flying || (IsTrailer && TowMoving());
        if (IsTrailer && Moving && SeenInPlay.On)
        {
            if (catchingUpFor != null) Seen.Note("vehicles.trailer-towed-world", vehicle);
            else Seen.Note("vehicles.trailer-towed", vehicle);
        }
        EngineRunning = hasFuel && (drafted || caravanMoving || flying);
        EngineRunningRaw = EngineRunning;
        // Framework 0.252.0: an aircraft's engines light in turn from Vehicle Framework's own sign that the engine is on.
        StartSequence.Note(EngineRunningRaw);
        EngineLoad = EngineRunning ? (Moving ? 1f : 0.5f) : 0f;
        Powertrain? train = Powertrain;
        if (train != null && VehicleSystemsMod.Settings.drivetrainMode != DrivetrainMode.Off)
        {
            train.Sample();
            if (train.IsAircraft)
            {
                // Aircraft on a map: the flight framework's phase says whether the engines run (Vehicle Framework's flag is set only
                // while leaving the map).
                EngineRunning = hasFuel && (train.AircraftRunning || flying);
                Moving = train.AircraftMoving || flying;
            }
            if (train.Full) EngineLoad = EngineRunning ? (float)train.Result.Load : 0f;
        }
        // On the battery (the generator set's hybrid, Framework 0.104.0) the engine is off however the vehicle moves: no heat, sound, load or fuel.
        if (BatteryDrive is { OnBattery: true })
        {
            EngineRunning = false;
            EngineRunningRaw = false;
            EngineLoad = 0f;
        }
        // A set-up vehicle's work turns the engine with nobody driving. EngineRunningRaw stays as it was, so the drivetrain's fuel sum adds nothing: the work burns its own.
        // A trailer has no engine for its work to turn: its pumps and set-up run without one (Framework 0.133.0).
        engineDemands ??= IsTrailer ? new List<IVehicleEngineDemand>() : vehicle.AllComps.OfType<IVehicleEngineDemand>().ToList();
        WorkTurningEngine = false;
        bool workWanted = false;
        bool workByModel = true;
        float workPower = 0f;
        foreach (IVehicleEngineDemand demand in engineDemands)
        {
            if (demand.WantsEngine) workWanted = true;
            if (!demand.WantsEngine || !hasFuel || demand is CompPhobosSetUp { Stalled: true }) continue;
            EngineRunning = true;
            WorkTurningEngine = true;
            EngineLoad = Mathf.Max(EngineLoad, demand.EngineDemand);
            workPower = Mathf.Max(workPower, demand.WorkPowerShare);
            workByModel &= demand.FuelFromModel;
        }
        WorkPowerShare = WorkTurningEngine ? workPower : 0f;
        WorkFuelFromModel = WorkTurningEngine && workByModel && ModelBurnsWorkFuel;
        // T6 (Framework 0.106.0): whether the engine turned at the last look that still found fuel; frozen once the tank is empty, so a
        // tank that ran dry under a running engine is told from one drained while parked. Derived, never saved.
        if (hasFuel) ranWithFuel = EngineRunning;
        shorePowered = null;
        gridCarries = null;
        loadsCache = null;
        RefreshAutoSwitches();
        UpdateShedding();
        RefreshShortedLoads();
        UpdatePower();
        UpdateStartReserve();
        loadAmps = 0f;
        if (states != null && states.Count > 0) loadAmps = SumLoads(new SystemContext(this, states[0]));
        Electrics = ReadElectrics();
        NotifyShedding();
        NotifyPower();
        NotifyStartReserve();
        UpdateFridge();
        // A cold box packed on a trailer in tow stays cold while its load on this vehicle runs (Framework 0.216.0).
        if (Hitch is { Trailer: not null } && LoadRuns(ColdInTow.LoadName)) ColdInTow.Chill(Vehicle);
        // A tank trailer's electric pump behind it runs while its load on this vehicle runs (Framework 0.283.0).
        if (Hitch is { Trailer: not null } && LoadRuns(TrailerPumpInTow.LoadName)) TrailerPumpInTow.Power(Vehicle);
        // A tank trailer's reverse-osmosis kit behind it treats while its load on this vehicle runs, on a map or the world map (Framework 0.318.0).
        if (Hitch is { Trailer: not null } && LoadRuns(TrailerTreatmentInTow.LoadName)) TrailerTreatmentInTow.Power(Vehicle);
        // Crosslink 5: the alternator takes shaft power for what the loads draw, so the engine works a little harder for them.
        float driveLoad = EngineLoad;
        bool wanted = drafted || caravanMoving || flying || (train?.IsAircraft == true && train.AircraftRunning) || workWanted;
        EngineWhy why = EngineStateRule.Why(BatteryDrive is { OnBattery: true }, EngineRunning, wanted, hasFuel, flying || (train?.IsAircraft == true && EngineRunning),
            Moving, WorkTurningEngine);
        // Step S1 (Framework 0.130.0): the engine's one picture, put together here and nowhere else. Since 0.131.0 the loads' share is the
        // whole ledger's: the alternator and everything else the engine drives.
        // Framework 0.249.0: steering effort, from the way the vehicle points changing since the last look while it moves.
        double heading = Vehicle.Spawned ? Phobos.VehicleSystems.CompPhobosHitch.VehicleHeading(Vehicle) : double.NaN;
        bool turning = EngineRunning && Moving && !double.IsNaN(heading) && !double.IsNaN(lastHeading) && heading != lastHeading;
        lastHeading = heading;
        SteeringWork = (float)ShaftLoadRule.SteeringWork(turning, Powertrain?.LastMovingSpeed ?? 0);
        float rpm = EngineRunning ? EngineRpm(AlternatorFallbackIdleRpm, AlternatorFallbackCruiseRpm) : 0f;
        Engine = new EngineState(why, rpm, driveLoad, ReadLedger(rpm));
        EngineLoad = Engine.Load;
    }

    // ---- Thin air (Framework 0.132.0; docs/development/vehicle-altitude-and-turbines.md) ----

    /// <summary>How high the vehicle stands, in metres above the sea: its map's tile, or its caravan's. 0 with the player's setting off.
    /// A local map has no height of its own. Derived, never saved.</summary>
    public float Elevation { get; private set; }

    /// <summary>The air's pressure here as a share of sea level's, for an engine that breathes it; 1 at the sea, with the setting off,
    /// and for an electric motor or a thruster. A turbine's output and a rotor's lift read it.</summary>
    public float AirPressure { get; private set; } = 1f;

    /// <summary>The share of its rated power thin air leaves the engine here: 1 at the sea and for a turbocharged engine below the
    /// height it holds power to. The drivetrain model takes it as lost power (step S4's way), where the vehicle has the full model.</summary>
    public float AirPowerFactor { get; private set; } = 1f;

    /// <summary>How well the air here carries a radiator's heat away, as a share of sea level's.</summary>
    public float AirCooling { get; private set; } = 1f;

    PlanetTile airTile = PlanetTile.Invalid;
    bool airRead;

    void RefreshAir(VehiclePawn vehicle, Vehicles.World.VehicleCaravan? caravan)
    {
        if (!VehicleSystemsMod.Settings.thinAir)
        {
            Elevation = 0f;
            AirPressure = AirPowerFactor = AirCooling = 1f;
            airRead = false;
            return;
        }
        // In flight between tiles a vehicle's comps do not tick: it keeps the air of the tile it left.
        PlanetTile tile = vehicle.Spawned ? vehicle.Map.Tile : caravan != null ? caravan.Tile : airTile;
        if (!airRead || tile != airTile)
        {
            airRead = true;
            airTile = tile;
            Elevation = Phobos.Framework.WorldMap.TileAir.ElevationMetres(tile);
        }
        Powertrain? train = Powertrain;
        AirPressure = train is { BreathesAir: true } ? (float)AirRule.PressureRatio(Elevation) : 1f;
        AirPowerFactor = train == null ? 1f : (float)train.AirPowerShare(Elevation);
        AirCooling = (float)AirRule.Cooling(Elevation, Rules.airCoolingExponent);
        if (AirPowerFactor < ThinAirNoticed && EngineRunning && SeenInPlay.On)
        {
            Seen.Note("vehicles.thin-air", vehicle, Elevation.ToString("0") + " m x" + AirPowerFactor.ToString("0.##"));
        }
    }

    /// <summary>Thin air worth a line on the inspect pane: the engine has less than this share of its power left (authored).</summary>
    public const float ThinAirNoticed = 0.97f;

    /// <summary>The share of the engine's rated power standing work takes at the shaft now (Framework 0.131.0); 0 with none. Derived.</summary>
    public float WorkPowerShare { get; private set; }

    /// <summary>Whether a reading means anything on this vehicle, for the tabs and the diagnosis: a ground-only one does not on an aircraft.</summary>
    public bool Shows(QuantitySpec quantity) => !quantity.groundOnly || Powertrain?.IsAircraft != true;

    /// <summary>Whether the engine model burns the fuel for the standing work going on now (Framework 0.131.0): fuel follows engine load,
    /// the vehicle has a full ground drivetrain, and every work turning the engine allows it. Then the work burns no rate of its own.</summary>
    public bool WorkFuelFromModel { get; private set; }

    /// <summary>Whether this vehicle's work fuel could come from the engine model at all.</summary>
    public bool ModelBurnsWorkFuel => VehicleFittings.FuelFollowsModel && Powertrain is { Full: true, Ground: true }
        && Vehicle.CompFueledTravel?.Props.electricPowered != true;

    /// <summary>The engine's one picture (Framework 0.130.0, step S1): whether it turns and why, its speed, and the shares of its power that
    /// driving and the loads take. Put together at each update and refreshed by the once-a-second drive sample. Derived, never saved.</summary>
    public EngineState Engine { get; private set; }

    /// <summary>What this vehicle burned lately for each use (step S2), written by <see cref="VehicleFuel"/>. Derived, never saved.</summary>
    public VehicleFuelLog FuelLog { get; } = new();

    /// <summary>The share of the engine's rated power the switched-on loads take through the alternator now (crosslink 5): 0 with the
    /// player's setting off, the engine stopped, no rated power known, or an electric vehicle (its loads draw on the traction battery).
    /// Derived on each update, never saved.</summary>
    public float AccessoryShare => Engine.AccessoryShare;

    // ---- The engine-load ledger (Framework 0.131.0; docs/development/vehicle-engine-loads.md) ----

    readonly List<ShaftLoadNow> shaftLoads = new();

    /// <summary>What the engine drives besides the wheels at the last look, each with the shaft power it took: the alternator first, then
    /// every <see cref="PhobosVehicleShaftLoadDef"/> working. Derived, never saved.</summary>
    public IReadOnlyList<ShaftLoadNow> ShaftLoads => shaftLoads;

    /// <summary>All of it together, kW.</summary>
    public float LedgerKw { get; private set; }

    /// <summary>The part that comes off the power left for driving, kW: everything but the baseline loads a rated figure already includes.</summary>
    public float LedgerExtraKw { get; private set; }

    /// <summary>The same as a share of the engine's rated power, for the world map's range.</summary>
    public float LedgerExtraShare { get; private set; }

    /// <summary>The share of what it is asked to carry that the drive belt passes now: 1 gripping, less slipping, 0 snapped.</summary>
    public float BeltGrip { get; private set; } = 1f;

    /// <summary>What the alternator or generator takes at the shaft now, kW (Framework 0.132.0): an aircraft's turbine counts it into
    /// its demand. 0 with the engine stopped.</summary>
    public float GeneratorKw { get; private set; }

    /// <summary>What the alternator takes at the shaft now before the belt's grip, kW (Framework 0.243.0, for the fuel line): what it makes,
    /// at the charging voltage, and its drag. 0 with the engine stopped.</summary>
    public float AlternatorKw { get; private set; }

    static string? reactionLabel, cabinAirLabel;

    const string BeltRole = "Belt";
    const string AlternatorRole = "Alternator";
    static string? alternatorLabel;

    /// <summary>
    /// Works the ledger out for this look and returns its share of the engine's rated power: 0 with the player's setting off, the engine
    /// stopped, no rated power known, or an electric vehicle (its loads draw on the traction battery). The alternator is charged for the
    /// amps it actually makes: what the loads draw and what the batteries take, never more than it can give at this speed.
    /// </summary>
    float ReadLedger(float rpm)
    {
        shaftLoads.Clear();
        LedgerKw = 0f;
        LedgerExtraKw = 0f;
        LedgerExtraShare = 0f;
        BeltGrip = 1f;
        GeneratorKw = 0f;
        AlternatorKw = 0f;
        Powertrain? train = Powertrain;
        if (!VehicleSystemsMod.Settings.accessoriesLoadEngine || !EngineRunning || train == null || states == null || states.Count == 0) return 0f;
        if (Vehicle.CompFueledTravel?.Props.electricPowered == true) return 0f;
        double rated = train.Spec.RatedPowerKw;
        if (rated <= 0) return 0f;
        PhobosVehicleRulesDef rules = Rules;
        // Framework 0.243.0 (the owner's report of 23 Nov 2026; docs/development/vehicle-engine-loads.md, the second round): priced at the
        // charging voltage, with its drag while it turns, and split into the standing part (the engine's own electronics and that drag, which a
        // rated power and the author's fuel rate already allow for) and the rest, which comes off driving.
        double efficiency = rules.alternatorEfficiency.Evaluate(AlternatorRpm());
        double chargingVolts = ElectricalVolts() * rules.chargingVoltsFactor;
        double outputAmps = ShaftLoadRule.AlternatorOutputAmps(AlternatorSupplyAmps(), loadAmps + CampLoadAmps, ChargingAmps());
        double dragKw = ShaftLoadRule.AlternatorDragKilowatts(AlternatorRatedAmps(), chargingVolts, rules.alternatorDragShare);
        double alternatorKw = ShaftLoadRule.AlternatorKilowatts(outputAmps, chargingVolts, efficiency) + dragKw;
        double alternatorBaselineKw = ShaftLoadRule.AlternatorKilowatts(System.Math.Min(outputAmps, BaselineLoadAmps), chargingVolts, efficiency) + dragKw;
        AlternatorKw = (float)alternatorKw;
        if (BaselineLoadAmps > 0f && dragKw > 0 && Seen.Wants("vehicles.alternator-sized", Vehicle))
        {
            Seen.Note("vehicles.alternator-sized", Vehicle, alternatorKw.ToString("0.00") + " kW, standing " + alternatorBaselineKw.ToString("0.00") + " kW");
        }
        // An aircraft's generator is on its engine's gearbox and its other loads are bleed air (the turbine design), listed below.
        bool ground = train.Ground && !train.IsAircraft;
        double beltKw = alternatorKw, otherKw = 0, baselineKw = ground ? alternatorBaselineKw : 0;
        if (alternatorKw > 0) shaftLoads.Add(new ShaftLoadNow(alternatorLabel ??= "PhobosVehicleLedger_Alternator".Translate().Resolve(), (float)alternatorKw));
        int firstDef = shaftLoads.Count;
        if (ground)
        {
            var context = new SystemContext(this, states[0]);
            List<PhobosVehicleShaftLoadDef> defs = ShaftLoadDefs;
            for (int i = 0; i < defs.Count; i++)
            {
                PhobosVehicleShaftLoadDef def = defs[i];
                double kw = ShaftKilowatts(def, context, rpm, train);
                if (kw <= 0) continue;
                if (def.beltDriven) beltKw += kw;
                else otherKw += kw;
                if (def.baseline) baselineKw += kw;
                shaftLoads.Add(new ShaftLoadNow(def.label, (float)kw));
                // Framework 0.249.0: which load was seen taking what, by its defName, so a test row can name one.
                if (Seen.Wants("vehicles.shaft-load", Vehicle, def.defName))
                {
                    Seen.Note("vehicles.shaft-load", Vehicle, kw.ToString("0.00") + " kW of " + train.Spec.RatedPowerKw.ToString("0") + " kW rated", def.defName);
                }
                beltDrivenNow.Add(def.beltDriven);
            }
        }
        else if (train.IsAircraft)
        {
            // Gap T-2 and step S7 (Framework 0.132.0): bleed air in the ledger's shape. Each is the share of flying's work the engines
            // give up as air, so in kilowatts it grows with how hard they fly.
            double flying = train.FlightDemand * rated;
            double reaction = train.ReactionBleedShare * flying, cabin = train.CabinBleedShare * flying;
            if (reaction > 0)
            {
                otherKw += reaction;
                shaftLoads.Add(new ShaftLoadNow(reactionLabel ??= "PhobosVehicleLedger_ReactionControl".Translate().Resolve(), (float)reaction));
                beltDrivenNow.Add(false);
            }
            if (cabin > 0)
            {
                otherKw += cabin;
                shaftLoads.Add(new ShaftLoadNow(cabinAirLabel ??= "PhobosVehicleLedger_CabinAir".Translate().Resolve(), (float)cabin));
                beltDrivenNow.Add(false);
            }
        }
        // The belt passes only what it can carry (a slipping belt underdrives everything on it; a snapped one drives nothing).
        float beltCondition = PartEfficiency(BeltRole, -1f);
        NoticeBeltSnap(beltCondition);
        double grip = beltCondition < 0f ? 1 : ShaftLoadRule.BeltGrip(beltKw, rules.beltCapacityShare, rated, beltCondition);
        if (grip < 1)
        {
            int flag = 0;
            for (int i = 0; i < shaftLoads.Count; i++)
            {
                bool onBelt = i < firstDef || beltDrivenNow[flag++];
                if (onBelt) shaftLoads[i] = new ShaftLoadNow(shaftLoads[i].Label, (float)(shaftLoads[i].Kilowatts * grip));
            }
            NoticeBeltSlip((float)grip, beltCondition);
        }
        beltDrivenNow.Clear();
        BeltGrip = (float)grip;
        GeneratorKw = (float)(alternatorKw * grip);
        // The baseline loads are all on the belt or steady; a slipping belt takes their share down with the rest.
        double total = beltKw * grip + otherKw;
        double baselineTaken = baselineKw * grip;
        LedgerKw = (float)total;
        LedgerExtraKw = (float)System.Math.Max(0, total - baselineTaken);
        LedgerExtraShare = (float)System.Math.Min(1, LedgerExtraKw / rated);
        if (LedgerExtraKw > 0f && SeenInPlay.On) Seen.Note("vehicles.engine-loads", Vehicle, LedgerKw.ToString("0.0") + " kW");
        return (float)System.Math.Min(1, total / rated);
    }

    /// <summary>Whether each ledger entry after the alternator is on the belt, in step with <see cref="shaftLoads"/> while a look is worked out.</summary>
    readonly List<bool> beltDrivenNow = new();

    static List<PhobosVehicleShaftLoadDef>? shaftLoadDefs;

    /// <summary>Every shaft load Def, in the order the Systems tab lists them.</summary>
    static List<PhobosVehicleShaftLoadDef> ShaftLoadDefs =>
        shaftLoadDefs ??= DefDatabase<PhobosVehicleShaftLoadDef>.AllDefsListForReading.OrderBy(d => d.order).ThenBy(d => d.defName).ToList();

    static double ShaftKilowatts(PhobosVehicleShaftLoadDef def, SystemContext context, double rpm, Powertrain train)
    {
        if (def.available != null && !def.available.Holds(context)) return 0;
        if (def.when != null && !def.when.Holds(context)) return 0;
        double inputs = 1;
        for (int i = 0; i < def.inputs.Count && inputs > 0; i++) inputs *= def.inputs[i].Evaluate(context);
        return ShaftLoadRule.Kilowatts(def.kilowatts, def.shareOfRated, train.Spec.RatedPowerKw, rpm, train.Spec.RatedRpm, def.speedExponent, inputs);
    }

    /// <summary>
    /// What the baseline loads take at an engine speed, kW: the standing set a rated figure already includes, which the fuel model counts
    /// into its nominal cruise so that a cruise still burns the author's rate. 0 with the player's setting off or on an electric vehicle.
    /// </summary>
    public double BaselineShaftKwAt(double rpm)
    {
        Powertrain? train = Powertrain;
        if (!VehicleSystemsMod.Settings.accessoriesLoadEngine || train == null || !train.Ground || train.IsAircraft || states == null || states.Count == 0) return 0;
        if (Vehicle.CompFueledTravel?.Props.electricPowered == true) return 0;
        var context = new SystemContext(this, states[0]);
        double total = 0;
        List<PhobosVehicleShaftLoadDef> defs = ShaftLoadDefs;
        for (int i = 0; i < defs.Count; i++)
        {
            PhobosVehicleShaftLoadDef def = defs[i];
            if (!def.baseline || (def.available != null && !def.available.Holds(context))) continue;
            total += ShaftLoadRule.Kilowatts(def.kilowatts, def.shareOfRated, train.Spec.RatedPowerKw, rpm, train.Spec.RatedRpm, def.speedExponent, 1);
        }
        // Framework 0.243.0: the alternator's standing part (the engine's electronics and its drag), so that a cruise with nothing switched on
        // still burns the author's rate.
        PhobosVehicleRulesDef rules = Rules;
        double volts = ElectricalVolts() * rules.chargingVoltsFactor;
        float alternatorRpm = (float)(rpm * train.AlternatorRatio);
        double supply = AlternatorRatedAmps() * rules.alternatorOutput.Evaluate(alternatorRpm);
        total += ShaftLoadRule.AlternatorKilowatts(System.Math.Min(supply, BaselineLoadAmpsRunning(context)), volts, rules.alternatorEfficiency.Evaluate(alternatorRpm))
            + ShaftLoadRule.AlternatorDragKilowatts(AlternatorRatedAmps(), volts, rules.alternatorDragShare);
        return total;
    }

    /// <summary>What the standing loads draw with the engine running, whatever is switched on now, in amps: the figure the nominal cruise
    /// counts (Framework 0.243.0).</summary>
    float BaselineLoadAmpsRunning(SystemContext context)
    {
        float amps = 0f;
        foreach (ElectricalLoadSpec load in Loads)
        {
            if (!load.baseline || (load.available != null && !load.available.Holds(context))) continue;
            amps += AmpsNow(load, context);
        }
        return amps;
    }

    /// <summary>
    /// An electric vehicle's converter (Framework 0.243.0; the report of 23 Nov 2026 found its 12 V loads cost the drive battery nothing): while
    /// it is switched on, what the converter gives the lights, winch, outlet and 12 V battery comes out of the drive battery, at the converter's
    /// efficiency, as a share of the pack an hour. Its heaters and air conditioner draw on the pack already, in Systems_Electric.xml.
    /// </summary>
    void ApplyConverterDraw(double hours)
    {
        if (!EngineRunning || hours <= 0 || Vehicle.CompFueledTravel is not { Props.electricPowered: true } fuel || fuel.EmptyTank) return;
        PhobosVehicleRulesDef rules = Rules;
        double amps = ShaftLoadRule.AlternatorOutputAmps(AlternatorSupplyAmps(), loadAmps + CampLoadAmps, ChargingAmps());
        double share = ShaftLoadRule.ConverterPackSharePerHour(amps, ElectricalVolts() * rules.chargingVoltsFactor, rules.converterEfficiency,
            rules.tractionPackKilowattHours);
        if (share <= 0) return;
        VehicleFuel.Burn(Vehicle, (float)(share * fuel.FuelCapacity * hours), FuelUse.Electrics, this);
        if (Seen.Wants("vehicles.converter-draw", Vehicle)) Seen.Note("vehicles.converter-draw", Vehicle, amps.ToString("0") + " A");
    }

    /// <summary>What the alternator can give now, in amps: its rating, by its own and its belt's condition and its speed.</summary>
    double AlternatorSupplyAmps() => AlternatorRatedAmps() * AlternatorOutputShare();

    /// <summary>The alternator's rating in amps, by its own and its belt's condition; 0 without one.</summary>
    double AlternatorRatedAmps()
    {
        if (states == null) return 0;
        foreach (SystemState state in states)
        {
            if (!Enabled(state) || !state.system.parameters.ContainsKey(AlternatorAmpsParameter)) continue;
            return new SystemContext(this, state).Parameter(AlternatorAmpsParameter) * PartEfficiency(AlternatorRole, 1f) * PartEfficiency(BeltRole, 1f);
        }
        return 0;
    }

    /// <summary>
    /// What an aircraft's generator takes in a cruise for the loads switched on now, kW (Framework 0.132.0): the steady loads alone,
    /// with the generator at full speed and no battery to refill, because the figure is held for a whole flight between tiles, where
    /// nothing ticks. 0 with the player's "accessories load the engine" setting off.
    /// </summary>
    public double CruiseGeneratorKw()
    {
        if (!VehicleSystemsMod.Settings.accessoriesLoadEngine || states == null || states.Count == 0) return 0;
        double amps = System.Math.Min(loadAmps + CampLoadAmps, AlternatorRatedAmps());
        double volts = ElectricalVolts() * Rules.chargingVoltsFactor;
        return ShaftLoadRule.AlternatorKilowatts(amps, volts, Rules.alternatorEfficiency.Evaluate(Rules.generatorRatedRpm))
            + ShaftLoadRule.AlternatorDragKilowatts(AlternatorRatedAmps(), volts, Rules.alternatorDragShare);
    }

    const string AlternatorAmpsParameter = "alternatorAmps";

    /// <summary>What the batteries were taking from the alternator at the last update, in amps: the flows into the starting battery and
    /// the house battery, which with the engine running are the alternator's alone (shore charging runs only with it stopped).</summary>
    double ChargingAmps() => IntoAmps("charge", "batteryAmpHours") + IntoAmps(HouseChargeQuantity, "houseAmpHours");

    double IntoAmps(string quantity, string ampHoursParameter)
    {
        if (states == null) return 0;
        foreach (SystemState state in states)
        {
            int q = state.Def.QuantityIndex(quantity);
            if (q < 0 || !Enabled(state)) continue;
            double perHour = 0;
            foreach (FlowSample sample in state.samples)
            {
                if (sample.Quantity == q && sample.Kind == FlowKind.Into) perHour += sample.Rate;
            }
            return perHour * new SystemContext(this, state).Parameter(ampHoursParameter);
        }
        return 0;
    }

    /// <summary>A role's working share for what it drives (Framework 0.131.0): its parts' condition, and for the drive belt what a slipping
    /// belt still passes. The alternator's supply and the water pump's flow read this, so a loaded, worn belt underdrives both.</summary>
    public float DriveShare(string role, float missing = 1f) => PartEfficiency(role, missing) * (role == BeltRole ? BeltGrip : 1f);

    void NoticeBeltSlip(float grip, float beltCondition)
    {
        if (grip >= BeltSlipNoticed || beltCondition <= 0f || Vehicle?.Faction != Faction.OfPlayer) return;
        if (Seen.Wants("vehicles.belt-slipping", Vehicle)) Seen.Note("vehicles.belt-slipping", Vehicle, grip.ToStringPercent());
        if (!MayNotify("beltSlip", Rules.messageCooldownHours)) return;
        VehicleNotices.Send("PhobosVehicleSystems_MsgBeltSlipping".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), Vehicle,
            MessageTypeDefOf.NegativeEvent);
    }

    /// <summary>A belt passing less than this share of its load is worth telling the player about (authored).</summary>
    const float BeltSlipNoticed = 0.9f;

    /// <summary>The belt's condition at the last look, to hear it snap (Framework 0.270.0, S111): below nothing until first read, and never
    /// saved, so a save loaded with the belt already snapped stays quiet.</summary>
    float lastBeltCondition = -1f;

    /// <summary>A belt that was turning and has just snapped squeals and slaps loose (S111, the owner's pick of 23 Nov 2026).</summary>
    void NoticeBeltSnap(float beltCondition)
    {
        if (lastBeltCondition > 0f && beltCondition == 0f && Vehicle is { Spawned: true })
        {
            VehicleSystemSounds.PlayOnce(this, VehicleSystemSoundDefOf.PhobosVehicleSystems_BeltSnap);
            if (Seen.Wants("vehicles.sound.belt-snap", Vehicle)) Seen.Note("vehicles.sound.belt-snap", Vehicle);
        }
        lastBeltCondition = beltCondition;
    }

    /// <summary>How fast the alternator turns now, in rpm: the engine's speed times its Def's alternator ratio (check C4). A turbine's
    /// generator sits on its accessory gearbox, geared to its own speed (gap T-4, Framework 0.132.0): the rules Def's rated speed at full
    /// spool, not the spool's tens of thousands of rpm times a pulley ratio. Derived.</summary>
    public float AlternatorRpm()
    {
        Powertrain? train = Powertrain;
        if (train?.Turbine != null) return EngineRunning ? (float)(train.TurbineNow.SpeedPercent / 100) * Rules.generatorRatedRpm : 0f;
        return EngineRpm(AlternatorFallbackIdleRpm, AlternatorFallbackCruiseRpm) * (float)(train?.AlternatorRatio ?? EngineXml.DefaultAlternatorRatio);
    }

    /// <summary>The engine speeds assumed without a drivetrain model, as the engine-speed input's defaults have always been (authored).</summary>
    const float AlternatorFallbackIdleRpm = 800f, AlternatorFallbackCruiseRpm = 2000f;

    /// <summary>The alternator's (or a turbine's generator's) output as a share of its rating now (step S5): the rules Def's one curve.</summary>
    public float AlternatorOutputShare()
    {
        Powertrain? train = Powertrain;
        if (train?.Turbine != null)
        {
            (int working, int engines) = train.Engines();
            return (float)ElectricsRule.GeneratorShare(Rules.generatorSpoolOutput.Evaluate((float)train.TurbineNow.SpeedPercent), working, engines);
        }
        return Rules.alternatorOutput.Evaluate(AlternatorRpm());
    }

    /// <summary>The electrical system's voltage: its <c>systemVolts</c> figure (12, or 24 on heavy vehicles), 12 when no system has one.</summary>
    internal float ElectricalVolts()
    {
        const float DefaultVolts = 12f;
        if (states == null) return DefaultVolts;
        foreach (SystemState state in states)
        {
            if (Enabled(state) && state.system.parameters.ContainsKey("systemVolts")) return new SystemContext(this, state).Parameter("systemVolts");
        }
        return DefaultVolts;
    }

    float SumLoads(SystemContext context)
    {
        float amps = 0f;
        float camp = 0f;
        float baseline = 0f;
        switchedLoadsOn.Clear();
        shedLoads.Clear();
        deadLoads.Clear();
        keptForStartLoads.Clear();
        foreach (ElectricalLoadSpec load in Loads)
        {
            if (load.toggle && !IsOn(load)) continue;
            if (!AutoHolds(load)) continue;
            if (load.available != null && !load.available.Holds(context)) continue;
            if (load.when != null && !load.when.Holds(context)) continue;
            if (IsShorted(load)) continue;
            // A camp load with a house battery fitted draws on that battery alone, and stops at its cut-off rather than by the shedding order
            // (Framework 0.129.0, vehicle accessories).
            if (OnHouseBattery(load))
            {
                if (!HouseBatteryRuns) continue;
                camp += AmpsNow(load, context);
                if (load.toggle) switchedLoadsOn.Add(load.label);
                continue;
            }
            // Framework 0.276.3: a load that keeps the start back draws nothing below its line, and is named for the notice.
            if (KeptForStart(load))
            {
                keptForStartLoads.Add(load.label);
                continue;
            }
            if (ElectricsRule.IsShed(load.shedOrder, ShedDepth))
            {
                shedLoads.Add(load.label);
                continue;
            }
            // Check C1 (Framework 0.129.1): on a dead battery a load draws nothing, but a switch left on is still named, so the player
            // knows what will come back on.
            if (!LoadsPowered)
            {
                if (load.toggle) deadLoads.Add(load.label);
                continue;
            }
            float now = AmpsNow(load, context);
            amps += now;
            if (load.baseline) baseline += now;
            if (load.toggle) switchedLoadsOn.Add(load.label);
        }
        CampLoadAmps = camp;
        BaselineLoadAmps = baseline;
        return amps;
    }

    /// <summary>How hard the power steering works now, 0 to 1 (Framework 0.249.0), and the heading it was read against. Derived, never saved.</summary>
    public float SteeringWork { get; private set; }
    double lastHeading = double.NaN;

    /// <summary>
    /// How bright lamps on the starting battery burn now, against full (Framework 0.249.0): full while the engine runs and the alternator
    /// carries every load; otherwise at the battery's voltage by its charge (HeadlightRule.LampShare); nothing on a dead battery.
    /// </summary>
    public float LampShare
    {
        get
        {
            if (!LoadsPowered) return 0f;
            bool carries = EngineRunning && AlternatorSupplyAmps() >= loadAmps;
            float charge = TryGetQuantity("charge", out float c) ? c : 1f;
            return (float)HeadlightRule.LampShare(carries, charge);
        }
    }

    /// <summary>What the standing loads of a running engine draw now (Framework 0.243.0: its electronics), in amps: part of the loads' sum.
    /// Derived, never saved.</summary>
    public float BaselineLoadAmps { get; private set; }

    /// <summary>What a load draws now: its amps, times its inputs where it has any (a fridge by the heat, an outlet by what the camp asks).</summary>
    public float AmpsNow(ElectricalLoadSpec load, SystemContext context)
    {
        float amps = load.amps;
        for (int i = 0; i < load.ampsInputs.Count; i++) amps *= load.ampsInputs[i].Evaluate(context);
        return amps;
    }

    /// <summary>What a load draws now, for the tabs and its switch.</summary>
    public float AmpsNow(ElectricalLoadSpec load) => states == null || states.Count == 0 ? load.amps : AmpsNow(load, new SystemContext(this, states[0]));

    // ---- Vehicle accessories (Framework 0.129.0; docs/development/vehicle-accessories.md, the power budget) ----

    /// <summary>The ability a house battery fitting grants, and the house battery system's quantity.</summary>
    public const string HouseBatteryGrant = "HouseBattery";
    const string HouseChargeQuantity = "houseCharge";

    /// <summary>What the camp loads draw from the house battery now, in amps (0 without one). Derived, never saved.</summary>
    public float CampLoadAmps { get; private set; }

    /// <summary>Whether a house battery is fitted on a standing part.</summary>
    public bool HouseBatteryFitted => Fittings.Grants(HouseBatteryGrant);

    /// <summary>Whether this load draws on the house battery: a camp load, with one fitted.</summary>
    bool OnHouseBattery(ElectricalLoadSpec load) => load.campLoad && HouseBatteryFitted;

    /// <summary>Whether the house battery is above its low-voltage cut-off (true while its reading is unknown).</summary>
    bool HouseBatteryRuns => !TryGetQuantity(HouseChargeQuantity, out float house) || CampRule.HouseBatteryRuns(house, Rules.houseBatteryCutoff);

    /// <summary>The watts the power outlet gives the camp now (Framework 0.129.0, Y1). Derived, never saved.</summary>
    public float OutletWatts { get; private set; }

    /// <summary>Whether the outlet turned the meter round at the last look, so the grid neither carries the loads nor charges the battery.</summary>
    public bool OutletGiving { get; private set; }

    /// <summary>Whether a load runs now: fitted, switched on, not shed, and its battery above flat (the Systems tab's "running").</summary>
    bool LoadRuns(string name) => LoadNamed(name) is { } load && StateOf(load) == LoadState.Running;

    /// <summary>Whether a load of this name runs now, for what depends on it beyond this vehicle (a cold box in tow).</summary>
    public bool LoadRunning(string name) => LoadRuns(name);

    /// <summary>Whether a power outlet is fitted and switched on: its conduit stays down while the engine idles.</summary>
    public bool OutletWanted
    {
        get
        {
            if (!Fittings.Grants(CampOutlet.Grant)) return false;
            ElectricalLoadSpec? load = LoadNamed(CampOutlet.LoadName);
            return load != null && IsOn(load);
        }
    }

    /// <summary>The cargo food the fridge keeps cold now, by its rule (Framework 0.129.0, Y7). Derived at each look, never saved.</summary>
    readonly HashSet<Thing> chilled = new HashSet<Thing>();
    bool fridgeHadPower;

    /// <summary>The temperature the fridge keeps this cargo thing at, or null when it keeps it at nothing.</summary>
    public float? ChilledTemperature(Thing thing) =>
        chilled.Count > 0 && chilled.Contains(thing) ? (float)CampRule.FridgeTemperature(Rules.fridgeDegrees, Ambient) : null;

    /// <summary>Chooses the food the fridge keeps cold while it runs, and says once when it loses power with food aboard.</summary>
    void UpdateFridge()
    {
        bool fitted = Fittings.Grants(CampFridge.Grant);
        bool running = fitted && LoadRuns(CampFridge.LoadName);
        if (fitted && fridgeHadPower && !running && chilled.Count > 0 && Vehicle.Faction == Faction.OfPlayer && SwitchOn(CampFridge.LoadName))
        {
            Messages.Message("PhobosVehicleSystems_MsgFridgeNoPower".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), Vehicle, MessageTypeDefOf.CautionInput);
        }
        fridgeHadPower = running;
        chilled.Clear();
        if (!running) return;
        CampFridge.Choose(Vehicle, Rules.fridgeKilograms, chilled);
        if (chilled.Count > 0 && Seen.Wants("vehicles.fridge-chilling", Vehicle)) Seen.Note("vehicles.fridge-chilling", Vehicle, chilled.Count + " stacks");
    }

    /// <summary>The labels of the switched loads running now (lights, heaters), from the last sum. Derived, never saved.</summary>
    readonly List<string> switchedLoadsOn = new List<string>();
    /// <summary>The labels of the loads switched on but shed to save the battery, from the last sum. Derived, never saved.</summary>
    readonly List<string> shedLoads = new List<string>();
    /// <summary>The labels of the loads switched on but dead on a flat battery (check C1, Framework 0.129.1), from the last sum. Derived, never saved.</summary>
    readonly List<string> deadLoads = new List<string>();

    /// <summary>
    /// How many ranks of loads are shed to save the battery now (cross-links G16 and avionics A6, Framework 0.106.0;
    /// ElectricsRule.ShedDepth): 0 while the charging keeps up. Derived at each update from the generator's parts, the charge and the
    /// battery's trend at the last look, never saved.
    /// </summary>
    public int ShedDepth { get; private set; }
    int shedDepthNotified;
    bool shedLookedOnce;
    int? shedRanks;

    void UpdateShedding()
    {
        shedRanks ??= Loads.Count == 0 ? 0 : Loads.Max(l => l.shedOrder);
        if (shedRanks.Value == 0 || !TryGetQuantity("charge", out float charge))
        {
            ShedDepth = 0;
            return;
        }
        ShedDepth = ElectricsRule.ShedDepth(EngineRunning, GeneratorShare(), charge, Electrics.Trend == ElectricalTrend.RunningDown, ShedDepth,
            shedRanks.Value, Rules.shedGeneratorLost, Rules.shedLowCharge, Rules.shedRecoveredCharge);
    }

    /// <summary>The generator's working share: the alternator's efficiency times its drive belt's, 1 when sound or not modelled.</summary>
    float GeneratorShare() => PartEfficiency(AlternatorRole, 1f) * DriveShare(BeltRole);

    /// <summary>Whether the vehicle's loads have power now (check C1, Framework 0.129.1; <see cref="ElectricsRule.LoadsPowered"/>): the one
    /// answer every load, switch, weapon drive and fitting reads. Derived at each update, never saved; true until the first.</summary>
    public bool LoadsPowered { get; private set; } = true;
    bool powerWasOn = true;
    bool powerLookedOnce;

    void UpdatePower()
    {
        bool hasBattery = TryGetQuantity("charge", out float charge);
        LoadsPowered = ElectricsRule.LoadsPowered(EngineRunning, GeneratorShare(), GridCarriesLoads, hasBattery, charge,
            PhobosRulesDef.Current<PhobosKnockOutRulesDef>().batteryFlatBelow, LoadsPowered, Rules.shedGeneratorLost, Rules.flatRecoveryFactor);
    }

    /// <summary>One message when a flat battery puts the switched-on loads out, for the player's vehicles; none at the first look after a load.</summary>
    void NotifyPower()
    {
        bool was = powerWasOn;
        powerWasOn = LoadsPowered;
        if (!powerLookedOnce)
        {
            powerLookedOnce = true;
            return;
        }
        if (was == LoadsPowered || LoadsPowered || deadLoads.Count == 0 || Vehicle?.Faction != Faction.OfPlayer) return;
        string loads = deadLoads.ToCommaList(useAnd: true);
        Seen.Note("vehicles.battery-dead", Vehicle, loads);
        VehicleNotices.Send("PhobosVehicleSystems_MsgBatteryDead".Translate(Vehicle.LabelShort.Named("VEHICLE"), loads.Named("LOADS")).Resolve(),
            Vehicle, MessageTypeDefOf.NegativeEvent);
    }

    // ---- Keeping the start (Framework 0.276.3; the owner's card of 24 Nov 2026, "House battery, plus a floor") ----

    /// <summary>The readings (a starting power) held below their line now, so the loads that keep them clear are out. Derived at each
    /// update from the reading, its stages and the battery's trend at the last look, never saved.</summary>
    readonly HashSet<string> startHeld = new();
    /// <summary>Each reading's line, read once from its stages (<see cref="StartReserveRule.Line"/>); NaN where none refuses a start.</summary>
    readonly Dictionary<string, double> startLines = new();
    /// <summary>The labels of the wanted loads out to keep the start, from the last sum. Derived, never saved.</summary>
    readonly List<string> keptForStartLoads = new();
    bool keptForStartBefore;
    bool keptForStartLookedOnce;

    /// <summary>Whether this load is out now to keep enough in the starting battery to start the engine: it keeps a reading clear, draws
    /// on the starting battery (not a house battery), and that reading is held below its line.</summary>
    bool KeptForStart(ElectricalLoadSpec load) => load.startReserve != null && !OnHouseBattery(load) && startHeld.Contains(load.startReserve);

    /// <summary>The line a load keeping this reading clear goes out at: from the reading's own stages in the system that has it, never a
    /// figure of its own (the electrical system's starting power: the top of "barely enough to start", 0.2).</summary>
    double StartLine(string quantity)
    {
        if (startLines.TryGetValue(quantity, out double known)) return known;
        double line = double.NaN;
        if (states != null)
        {
            foreach (SystemState state in states)
            {
                int index = state.Def.QuantityIndex(quantity);
                if (index < 0) continue;
                List<StageSpec> stages = state.Def.quantities[index].stages;
                var read = new StartReserveRule.Stage[stages.Count];
                for (int i = 0; i < stages.Count; i++)
                {
                    read[i] = new StartReserveRule.Stage(stages[i].from, stages[i].to, stages[i].effects.Any(e => e is Effect_RefuseDraft));
                }
                line = StartReserveRule.Line(read);
                break;
            }
        }
        startLines[quantity] = line;
        return line;
    }

    void UpdateStartReserve()
    {
        bool charging = Electrics.Trend is ElectricalTrend.Charging or ElectricalTrend.ChargingFromGrid;
        IReadOnlyList<ElectricalLoadSpec> loads = Loads;
        for (int i = 0; i < loads.Count; i++)
        {
            if (loads[i].startReserve is not { } quantity) continue;
            bool held = TryGetQuantity(quantity, out float reading)
                && StartReserveRule.Held(OnHouseBattery(loads[i]), reading, StartLine(quantity), charging, startHeld.Contains(quantity));
            if (held) startHeld.Add(quantity);
            else startHeld.Remove(quantity);
        }
    }

    /// <summary>One message when wanted loads go out to keep the start, for the player's vehicles; none at the first look after a load.</summary>
    void NotifyStartReserve()
    {
        bool was = keptForStartBefore;
        keptForStartBefore = keptForStartLoads.Count > 0;
        if (!keptForStartLookedOnce)
        {
            keptForStartLookedOnce = true;
            return;
        }
        if (was || !keptForStartBefore || Vehicle?.Faction != Faction.OfPlayer) return;
        string loads = keptForStartLoads.ToCommaList(useAnd: true);
        Seen.Note("vehicles.kept-for-start", Vehicle, loads);
        VehicleNotices.Send("PhobosVehicleSystems_MsgKeptForStart".Translate(Vehicle.LabelShort.Named("VEHICLE"), loads.Named("LOADS")).Resolve(),
            Vehicle, MessageTypeDefOf.CautionInput);
    }

    /// <summary>Why this vehicle's load of this name is out to keep the start, in the player's words, or null when it is not (for a camp
    /// light's inspect line).</summary>
    public string? KeptForStartWhy(string name) =>
        LoadNamed(name) is { } load && StateOf(load) == LoadState.KeptForStart ? "PhobosVehicleSystems_KeptForStartWhy".Translate().Resolve() : null;

    /// <summary>One message when shedding starts and one when it ends, for the player's vehicles; none at the first look after a load.</summary>
    void NotifyShedding()
    {
        bool shedding = ShedDepth > 0;
        bool was = shedDepthNotified > 0;
        shedDepthNotified = ShedDepth;
        if (!shedLookedOnce)
        {
            shedLookedOnce = true;
            return;
        }
        if (shedding == was || Vehicle?.Faction != Faction.OfPlayer) return;
        if (shedding && shedLoads.Count > 0)
        {
            VehicleNotices.Send("PhobosVehicleSystems_MsgLoadsShed".Translate(Vehicle.LabelShort.Named("VEHICLE"),
                shedLoads.ToCommaList(useAnd: true).Named("LOADS")).Resolve(), Vehicle, MessageTypeDefOf.NegativeEvent);
        }
        else if (!shedding)
        {
            VehicleNotices.Send("PhobosVehicleSystems_MsgLoadsRestored".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), Vehicle,
                MessageTypeDefOf.PositiveEvent);
            // Seen in play (Framework 0.298.0, row 77): every shed load back on once charging works again.
            Seen.Note("vehicles.loads-restored", Vehicle);
        }
    }

    /// <summary>Why this aircraft may not take off now (Framework 0.106.0: Effect_RefuseLaunch, such as flight controls too damaged to
    /// fly), from the stages at the last update; null when it may. Only the player's aircraft are refused.</summary>
    public string? LaunchRefusal()
    {
        if (!Usable || states == null || Vehicle.Faction != Faction.OfPlayer) return null;
        // Framework 0.252.0: the engines still lighting one after another.
        if (StartSequence.Refusal() is { } starting) return starting;
        foreach ((SystemContext context, StageSpec stage, string _) in StagesInForce())
        {
            foreach (VehicleSystemEffect effect in stage.effects)
            {
                if (!effect.Active(context)) continue;
                string? reason = effect.LaunchRefusal(context);
                if (reason != null) return reason;
            }
        }
        return null;
    }

    /// <summary>At take-off, the warnings of refusals the player set to warn only (Framework 0.110.0: blind flying), once each.</summary>
    void WarnAtLaunch()
    {
        if (!Usable || states == null || Vehicle.Faction != Faction.OfPlayer) return;
        foreach ((SystemContext context, StageSpec stage, string _) in StagesInForce())
        {
            foreach (VehicleSystemEffect effect in stage.effects)
            {
                if (effect.Active(context) && effect.LaunchWarning(context) is { } warning)
                {
                    Messages.Message(warning, Vehicle, MessageTypeDefOf.CautionInput, historical: false);
                }
            }
        }
    }

    /// <summary>The battery's state in words for the inspect line (Framework 0.104.0; owner, 12 Nov 2026: "in words", in every display
    /// mode), worked out at each update so the inspect line only reads it. Derived, never saved.</summary>
    public ElectricalStatus Electrics { get; private set; }

    /// <summary>A change of charge smaller than this a game hour reads as steady (authored: about a 45 Ah battery's 0.1 A).</summary>
    const double SteadyChargePerHour = 0.002;
    /// <summary>Above this charge a battery still taking a little reads as full, not charging (authored).</summary>
    const float NearlyFull = 0.98f;

    ElectricalStatus ReadElectrics()
    {
        if (states == null || Vehicle?.Faction != Faction.OfPlayer) return default;
        // Check C1 (Framework 0.129.1): nothing draws on a dead battery, so it reads as flat, naming any switches left on.
        if (!LoadsPowered) return new ElectricalStatus(ElectricalTrend.Flat, 0f, deadLoads.Count == 0 ? "" : deadLoads.ToCommaList(useAnd: true));
        foreach (SystemState state in states)
        {
            int q = state.Def.QuantityIndex("charge");
            if (q < 0 || !Enabled(state)) continue;
            double net = 0;
            foreach (FlowSample sample in state.samples)
            {
                if (sample.Quantity != q) continue;
                if (sample.Kind == FlowKind.Into) net += sample.Rate;
                else if (sample.Kind == FlowKind.From) net -= sample.Rate;
            }
            float ampHours = new SystemContext(this, state).Parameter("batteryAmpHours");
            float amps = (float)(System.Math.Abs(net) * ampHours);
            string loads = switchedLoadsOn.Count == 0 ? "" : switchedLoadsOn.ToCommaList(useAnd: true);
            string shed = shedLoads.Count == 0 ? "" : shedLoads.ToCommaList(useAnd: true);
            if (net < -SteadyChargePerHour)
            {
                // The battery clock (avionics A6, Framework 0.106.0): how long the charge lasts at this drain, in game hours.
                float hours = (float)ElectricsRule.HoursLeft(state.values[q], ampHours, amps);
                return new ElectricalStatus(ElectricalTrend.RunningDown, amps, loads, shed, hours);
            }
            if (net > SteadyChargePerHour && state.values[q] < NearlyFull)
            {
                return new ElectricalStatus(EngineRunning ? ElectricalTrend.Charging : ElectricalTrend.ChargingFromGrid, amps, loads, shed);
            }
            return shed.Length > 0 ? new ElectricalStatus(ElectricalTrend.None, amps, loads, shed) : default;
        }
        return default;
    }

    int StageOf(SystemState state, int quantity)
    {
        List<StageSpec> stages = state.Def.quantities[quantity].stages;
        if (stages.Count == 0 || !Enabled(state)) return -1;
        StageBounds bounds = StageBounds.Of(state.Def.quantities[quantity]);
        return StageRule.StageIndex(state.values[quantity], bounds.from, bounds.to);
    }

    internal void EvaluateStages(SystemState state, float hours)
    {
        var context = new SystemContext(this, state);
        for (int q = 0; q < state.values.Length; q++)
        {
            int index = StageOf(state, q);
            if (index != state.stage[q])
            {
                StageSpec? old = state.CurrentStage(q);
                if (old != null)
                {
                    context.StageKey = StageKey(state, q);
                    foreach (VehicleSystemEffect effect in old.effects)
                    {
                        if (effect.Active(context)) effect.OnExit(context);
                    }
                }
                state.stage[q] = index;
                StageSpec? entered = state.CurrentStage(q);
                if (VehicleSystemsMod.StagesLog.DebugEnabled)
                {
                    VehicleSystemsMod.StagesLog.Debug(Vehicle.LabelShort + " (" + Vehicle.ThingID + ") " + state.Def.defName + "." + state.Def.quantities[q].name
                        + " = " + state.values[q].ToString("0.###") + ": " + (old?.label ?? "no stage") + " -> " + (entered?.label ?? "no stage"));
                }
                if (entered != null)
                {
                    context.StageKey = StageKey(state, q);
                    foreach (VehicleSystemEffect effect in entered.effects)
                    {
                        if (effect.Active(context)) effect.OnEnter(context);
                    }
                    // A red lamp lighting: one soft chime (the dashboard shows the same rule).
                    if (entered.notable && entered.Severe && state.Def.quantities[q].telltale != null) VehicleSystemSounds.Chime(this);
                    // A stage worth a look marks the graphs; not the stages a load works out again (they are not saved).
                    // A logged stage (Framework 0.290.0) is kept too, for the test list's saved histories.
                    if (entered.Recorded && state.staged) RecordEvent(VehicleEventKind.Stage, state.Def.defName, state.Def.quantities[q].name, index);
                    // Seen in play (Framework 0.123.0): the key is built from the quantity's name; config/test-evidence.json lists it as a prefix.
                    // Framework 0.138.0: the stage is the note's case, so a warning and the refusal after it are both noted, per vehicle.
                    if (entered.Recorded && state.staged && Seen.On)
                    {
                        string key = "vehicles.stage." + state.Def.quantities[q].name;
                        // A refusal straight from a warning that stood unnoted (entered at load, which notes nothing, or in an earlier
                        // session): the warning is noted first, as it really stood before the refusal and its message fired when it was
                        // entered (message cooldowns are not saved, so a load shows it). Without this the test list's order check read a
                        // session's first refusal as one with no warning (the Abrams, 19 Nov 2026, row 69).
                        if (entered.Severe && old is { notable: true, Severe: false } && Seen.Wants(key, Vehicle, old.label)) Seen.Note(key, Vehicle, old.label, old.label);
                        Seen.Note(key, Vehicle, entered.label, entered.label);
                    }
                }
            }
            StageSpec? current = state.CurrentStage(q);
            if (current == null || hours <= 0f) continue;
            context.StageKey = StageKey(state, q);
            foreach (VehicleSystemEffect effect in current.effects)
            {
                if (effect.Active(context)) effect.Apply(context, hours);
            }
        }
        state.staged = true;
    }

    static string StageKey(SystemState state, int quantity) => state.Def.defName + "/" + state.Def.quantities[quantity].name + "/" + state.stage[quantity];

    void OnVehicleEvent(VehicleEventDef eventDef)
    {
        if (Tier != DetailTier.Full)
        {
            // Another faction's headlights come on as it sets off and go out as it stops, without its electrics.
            if (Tier == DetailTier.Lite) Beams.Update();
            return;
        }
        if (!Usable) return;
        if (eventDef == VehicleEventDefOf.MoveStart) VehiclePipes.Leave(this);
        else if (eventDef == VehicleEventDefOf.MoveStop) VehiclePipes.Update(this);
        else if (eventDef == VehicleEventDefOf.IgnitionOn || eventDef == VehicleEventDefOf.IgnitionOff)
        {
            // Pipes and conduits go when the engine starts and come back when it stops (owner, 13 Nov 2026): laid once per stop, nothing each tick.
            ignitionHint = eventDef == VehicleEventDefOf.IgnitionOn;
            try
            {
                VehiclePipes.Update(this);
            }
            finally
            {
                ignitionHint = null;
            }
        }
        UpdateNow();
        Beams.Update();
        // The work lamps go out as the vehicle sets off and light as it stops, not at the next rare tick (Framework 0.171.0).
        if (eventDef == VehicleEventDefOf.MoveStart || eventDef == VehicleEventDefOf.MoveStop) Lamps.Update();
        if (states == null) return;
        foreach (SystemState state in states)
        {
            if (!Enabled(state)) continue;
            var context = new SystemContext(this, state) { StageKey = state.Def.defName + "/event/" + eventDef.defName };
            foreach (EventRule rule in state.Def.onEvents)
            {
                if (rule.eventDef != eventDef) continue;
                foreach (VehicleSystemEffect effect in rule.effects)
                {
                    if (effect.Active(context)) effect.OnEnter(context);
                }
            }
        }
        // Seen in play (Framework 0.301.0, row 73): a tank run dry, under a running engine (the fuel pump's damage) or drained while parked.
        if (eventDef.defName == "OutOfFuel") Seen.Note("vehicles.ran-dry", Vehicle, null, RanDry ? "running" : "parked");
    }

    /// <summary>The health of each bound part when the systems last looked, by part key; derived, never saved.</summary>
    readonly Dictionary<string, float> lastPartHealth = new();

    /// <summary>
    /// Called by the reactor when a bound part is hit, and by each update for every bound part: the health the part has lost since
    /// the systems last saw it is what the part-hit rules count (a holed radiator loses coolant for the hole, not for the shot).
    /// A rise (a repair) only resets the memory; wear and breakdowns the systems cause store the health themselves, so they never count.
    /// </summary>
    public void Notify_PartSeen(string role, VehicleComponent part, DamageDef? damage = null)
    {
        using PhobosTiming.Scope timing = PhobosTiming.Measure("vehicle hit");
        if (!Usable || part?.props?.key == null) return;
        string key = part.props.key;
        if (damage != null) recentDamage[key] = damage;
        float health = part.Health;
        if (!lastPartHealth.TryGetValue(key, out float last))
        {
            lastPartHealth[key] = health;
            return;
        }
        lastPartHealth[key] = health;
        float lost = last - health;
        if (lost < 0f)
        {
            // A repair: conditions that clear on repair go once the part is full again.
            recentDamage.Remove(key);
            if (conditions != null && conditions.Count > 0)
            {
                conditions.OnHealthRose(part, last, health);
                AfterConditionsChanged();
            }
            return;
        }
        if (lost == 0f) return;
        recentDamage.TryGetValue(key, out DamageDef? kind);
        recentDamage.Remove(key);
        Notify_PartHit(part, lost, kind);
    }

    /// <summary>Remembers a part's health after the systems changed it themselves, so the change is not counted as a hit.</summary>
    /// <summary>Keeps every part's health as it is now for the next load (Framework 0.200.0: a depot's weather wears parts off any map).</summary>
    public void RememberAllPartHealths()
    {
        if (Vehicle?.statHandler == null) return;
        foreach (VehicleComponent part in Vehicle.statHandler.components) RememberPartHealth(part);
    }

    void RememberPartHealth(VehicleComponent part)
    {
        if (part?.props?.key != null) lastPartHealth[part.props.key] = part.Health;
    }

    /// <summary>Looks at every bound part for health lost since the last look (a hit the reactor saw before the health was written).</summary>
    void SweepPartHealth()
    {
        foreach (KeyValuePair<string, List<string>> pair in Props.roleKeys)
        {
            foreach (VehicleComponent part in PartsOf(pair.Key)) Notify_PartSeen(pair.Key, part);
        }
        fittings?.Sweep();
    }

    /// <summary>A fitting or a tune came or went: the drivetrain is built again with them, and the stats and tabs follow.</summary>
    internal void Notify_FittingsChanged()
    {
        powertrain = null;
        powertrainBuilt = false;
        loadsCache = null;
        shedRanks = null;
        Vehicle.statHandler?.MarkAllDirty();
        Repairs.Invalidate();
        HealthViewVersion++;
        // A newly fitted extinguisher arrives full (Framework 0.241.0, FR10).
        FireFighting.FittingsChanged(this);
        // A turbocharger fitted is a new one, and one taken off leaves its part at nothing (Framework 0.320.0).
        Turbochargers.FittingsChanged(this);
    }

    /// <summary>The part-health memory beside each part's health now, for the dev action.</summary>
    public string DebugPartMemory()
    {
        var text = new System.Text.StringBuilder();
        foreach (KeyValuePair<string, List<string>> pair in Props.roleKeys)
        {
            foreach (VehicleComponent part in PartsOf(pair.Key))
            {
                string key = part.props.key;
                text.Append(pair.Key).Append('/').Append(key).Append(": health ").Append(part.Health.ToString("0.#"))
                    .Append(", last seen ").Append(lastPartHealth.TryGetValue(key, out float last) ? last.ToString("0.#") : "never").Append('\n');
            }
        }
        return text.ToString();
    }

    /// <summary>
    /// Health a bound part lost to a hit, and the kind of damage when the reactor saw it: each part-hit rule of the role that the
    /// damage matches moves its quantity by that much, or gives the part its condition.
    /// </summary>
    void Notify_PartHit(VehicleComponent part, float healthLost, DamageDef? damage)
    {
        if (!Usable || healthLost <= 0f) return;
        // The cause of a fire that catches soon after: its zone (Framework 0.241.0, FR10).
        FireFighting.NoteHit(this, part);
        // Another faction's vehicle counts its hits but does not evolve (the light tier).
        if (Tier == DetailTier.Full) UpdateNow();
        else EnsureStates();
        if (states == null) return;
        bool conditionGiven = false;
        foreach (SystemState state in states)
        {
            if (!Enabled(state)) continue;
            foreach (PartHitRule rule in state.Def.onPartHit)
            {
                // Every role the part plays: the health lost is counted once, whichever role's reactor saw it first.
                if (!PartPlays(part, rule.role) || !rule.Matches(damage) || !rule.AppliesTo(part)) continue;
                float amount = rule.Amount(healthLost, part.MaxHealth);
                if (!string.IsNullOrEmpty(rule.quantity)) AddToQuantity(rule.quantity, amount);
                if (rule.condition != null)
                {
                    Conditions.Add(rule.condition, part, rule.role, state.Def.defName, (float)FittingRule.FaultSeverity(System.Math.Max(0f, amount), Fittings.FaultFactor(part.props.key)),
                        Phobos.Framework.Persistence.CauseNote.Hit(damage, Vehicle.Tile));
                    conditionGiven = true;
                }
            }
        }
        if (conditionGiven) AfterConditionsChanged();
        VehicleStory.Hit(this);
    }

    internal bool PartPlays(VehicleComponent part, string role) => Props.roleKeys.TryGetValue(role, out List<string>? keys) && keys.Contains(part.props.key);

    readonly ProgressWatch freeRepairWatch = new();
    int repairsPausedUntil = -1;

    /// <summary>The watchdog has stood mechanics down from this vehicle for now. Derived, never saved: a load simply tries again.</summary>
    public bool RepairsPaused
    {
        get
        {
            if (repairsPausedUntil < 0) return false;
            int now = Find.TickManager.TicksGame;
            return now < repairsPausedUntil && repairsPausedUntil - now <= PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>().repairPauseTicks;
        }
    }

    /// <summary>A step of Vehicle Framework's free repair finished: true when the steps have stopped raising the vehicle's health.</summary>
    public bool Notify_FreeRepairStep(Pawn mechanic)
    {
        PhobosVehicleRulesDef rules = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();
        float health = Vehicle.statHandler.components.Sum(part => part.Health);
        if (!freeRepairWatch.Note(Find.TickManager.TicksGame, health, rules.repairStallTicks, rules.repairStepGapTicks)) return false;
        PauseRepairs(mechanic);
        return true;
    }

    /// <summary>Stands repairs to this vehicle down for a while and says so, instead of a mechanic working for ever at nothing.</summary>
    public void PauseRepairs(Pawn? mechanic)
    {
        // Framework 0.139.1: already stood down, it is not said again (six reports of one stall, 18 Nov 2026).
        if (RepairsPaused) return;
        repairsPausedUntil = Find.TickManager.TicksGame + PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>().repairPauseTicks;
        freeRepairWatch.Reset();
        VehicleSystemsMod.Logger.Warn("Repairs to " + Vehicle.LabelShort + " (" + Vehicle.def.defName + ") got nowhere for " + (mechanic?.LabelShort ?? "a mechanic") + "; standing down for now.");
        if (Vehicle.Faction == Faction.OfPlayer)
        {
            Messages.Message("PhobosVehicleSystems_RepairStalled".Translate(Vehicle.LabelShort.Named("VEHICLE")), Vehicle, MessageTypeDefOf.NegativeEvent);
        }
    }

    /// <summary>A part mended away from a map (a garage), where no update will see its health rise: what a repair clears, clears now.</summary>
    public void Notify_RepairedAway(VehicleComponent part, float healthBefore)
    {
        if (healthBefore <= 0f) fittings?.LoseWith(part);
        RememberPartHealth(part);
        if (conditions == null || conditions.Count == 0) return;
        conditions.OnHealthRose(part, healthBefore, part.Health);
        AfterConditionsChanged();
    }

    /// <summary>Gives a part of the role the condition with a cause already written (Framework 0.187.0: a surge from the grid).</summary>
    public void AddCondition(string role, PhobosVehicleConditionDef def, float amount, string system, Phobos.Framework.Persistence.CauseNote cause)
    {
        if (!Usable || amount <= 0f) return;
        VehicleComponent? chosen = PartsOf(role).FirstOrDefault();
        if (chosen == null) return;
        Conditions.Add(def, chosen, role, system, amount, cause);
        AfterConditionsChanged();
    }

    /// <summary>Gives a part of the role the condition, or adds to it where a part already has it (Effect_AddCondition). The stage that
    /// gave it is kept as its cause (Framework 0.181.0, EM24).</summary>
    public void AddCondition(string role, PhobosVehicleConditionDef def, float amount, string system, string? causeKey = null)
    {
        if (!Usable || amount <= 0f) return;
        VehicleComponent? chosen = null;
        foreach (VehicleComponent part in PartsOf(role))
        {
            chosen ??= part;
            if (conditions != null && conditions.Holds(def, part.props.key))
            {
                chosen = part;
                break;
            }
        }
        if (chosen != null) Conditions.Add(def, chosen, role, system, amount, causeKey == null ? null : Phobos.Framework.Persistence.CauseNote.Strain(causeKey, Vehicle.Tile));
    }

    /// <summary>Uses up so many of a thing from the vehicle's work store, then its cargo; in a convoy away from any map, the whole convoy's
    /// (Framework 0.181.0, EM1). The store first (Framework 0.272.0), so supplies an older save carried in the hold are still used.</summary>
    // A building named as a supply is used up packed (PackedThings, Framework 0.265.0).
    internal void TakeFromCargo(ThingDef def, int count)
    {
        int fromStore = WorkStore?.Take(def, count) ?? 0;
        if (fromStore < count) Phobos.Framework.Items.PackedThings.Take(CargoNow(), def, count - fromStore);
    }

    /// <summary>Where haulers lay out the supplies for this vehicle's own work (Framework 0.272.0): no cargo room, no weight.</summary>
    public CompPhobosWorkStore? WorkStore => workStore ??= Vehicle?.GetComp<CompPhobosWorkStore>();
    CompPhobosWorkStore? workStore;

    /// <summary>A destroyed part replaced: back to a share of its full health, from which the ordinary free repair continues.</summary>
    internal void RestorePart(VehicleComponent part, float healthShare)
    {
        part.SetHealth(Mathf.Max(1f, part.MaxHealth * healthShare));
        RememberPartHealth(part);
        Vehicle.statHandler.MarkAllDirty();
    }

    /// <summary>A repair finished: what depends on the parts and conditions follows now.</summary>
    internal void AfterRepair()
    {
        UpdateNow();
        AfterConditionsChanged();
    }

    /// <summary>For the dev action: sets the condition on the role's first part to a severity (0 removes it) and says what it did.</summary>
    public string DebugSetCondition(string role, PhobosVehicleConditionDef def, float severity)
    {
        UpdateNow();
        VehicleComponent? part = PartsOf(role).FirstOrDefault();
        if (part == null || states == null) return Vehicle.LabelShort + " has no part playing " + role + ".";
        Conditions.Set(def, part, role, states.Count > 0 ? states[0].Def.defName : "", severity);
        AfterConditionsChanged();
        return Vehicle.LabelShort + ": " + def.defName + " on " + part.props.key + " at " + severity.ToString("0.##") + ".";
    }

    /// <summary>A condition came, grew or went outside an update: its stages, the stat factors, the refusals and the tab follow now.</summary>
    void AfterConditionsChanged()
    {
        if (states == null) return;
        Conditions.EvaluateAll();
        RecacheStatFactors();
        MoveRefusal = FirstRefusal(draft: false);
        WorkTripRefusal = FirstWorkTripRefusal();
        HealthViewVersion++;
        // A hit that shorts the wiring (G12, Framework 0.123.0) puts its circuits out at once, not at the next update; a repair brings them back.
        if (Tier != DetailTier.Full || states.Count == 0) return;
        RefreshShortedLoads();
        loadAmps = SumLoads(new SystemContext(this, states[0]));
        Beams.Update();
    }

    /// <summary>Every stage in force, with the context its effects run in: the systems' quantities, then the parts' conditions. The
    /// one list the refusals, the stat factors and their reasons read, so a condition acts exactly as a system's stage does.</summary>
    IEnumerable<(SystemContext context, StageSpec stage, string owner)> StagesInForce()
    {
        if (states == null) yield break;
        foreach (SystemState state in states)
        {
            if (!Enabled(state)) continue;
            var context = new SystemContext(this, state);
            for (int q = 0; q < state.values.Length; q++)
            {
                StageSpec? stage = state.CurrentStage(q);
                if (stage != null) yield return (context, stage, state.Def.quantities[q].label);
            }
        }
        if (conditions == null) yield break;
        foreach (VehicleCondition condition in conditions.All)
        {
            StageSpec? stage = condition.CurrentStage;
            if (stage == null) continue;
            SystemContext? context = conditions.ContextFor(condition);
            if (context != null) yield return (context, stage, condition.def!.label);
        }
    }

    // ---- Quantities ----

    /// <summary>A quantity of any system running on this vehicle. A system switched off in the settings, or a cab system on an open
    /// vehicle, reads as absent: its last value is frozen, and other systems must not go on reading it (the crew kept feeling a
    /// switched-off cab's last temperature, and a cab heater would have read a switched-off cooling system's frozen coolant).</summary>
    public bool TryGetQuantity(string name, out float value)
    {
        if (states != null)
        {
            foreach (SystemState state in states)
            {
                if (!Enabled(state)) continue;
                int index = state.Def.QuantityIndex(name);
                if (index >= 0)
                {
                    value = (float)state.values[index];
                    return true;
                }
            }
        }
        value = 0f;
        return false;
    }

    public void SetQuantity(string name, float value) => ChangeQuantity(name, _ => value);

    public void AddToQuantity(string name, float amount) => ChangeQuantity(name, current => current + amount);

    void ChangeQuantity(string name, System.Func<double, double> change)
    {
        EnsureStates();
        if (states == null) return;
        foreach (SystemState state in states)
        {
            int index = state.Def.QuantityIndex(name);
            if (index < 0 || state.Def.quantities[index].Derived) continue;
            state.values[index] = SystemIntegrator.Clamp(change(state.values[index]), state.min[index], state.max[index]);
            state.Def.Worker.UpdateDerived(new SystemContext(this, state));
            return;
        }
    }

    // ---- Parts ----

    internal IEnumerable<VehicleComponent> PartsOf(string role)
    {
        if (Vehicle?.statHandler == null || !Props.roleKeys.TryGetValue(role, out List<string>? keys)) yield break;
        foreach (string key in keys)
        {
            VehicleComponent? part = Vehicle.statHandler.GetComponent(key);
            if (part != null) yield return part;
        }
    }

    /// <summary>The bound parts' efficiency, averaged; <paramref name="missing"/> when the role has no part.</summary>
    public float PartEfficiency(string role, float missing)
    {
        float sum = 0f;
        int count = 0;
        foreach (VehicleComponent part in PartsOf(role))
        {
            sum += part.Efficiency;
            count++;
        }
        return count == 0 ? missing : sum / count;
    }

    /// <summary>The average share of full health of the role's parts (how worn they are, where efficiency is what they still do),
    /// or <paramref name="missing"/> when the vehicle has none.</summary>
    public float PartHealthShare(string role, float missing)
    {
        float sum = 0f;
        int count = 0;
        foreach (VehicleComponent part in PartsOf(role))
        {
            sum += part.HealthPercent;
            count++;
        }
        return count == 0 ? missing : sum / count;
    }

    IntVec3 groundReadCell = IntVec3.Invalid;
    Map? groundReadMap;
    float groundReadCost;

    /// <summary>The world map's ordinary ground (open soil) when the caravan's own ground has not been read.</summary>
    const float OpenSoilPathCost = 2f;

    /// <summary>
    /// The path cost of the ground under the vehicle (crosslink 6): on a map, the cell's terrain plus its snow, read again only when the
    /// vehicle has moved to another cell; on the world map, the caravan's tile as the drivetrain reads it (open soil without one). A road
    /// or a floor is 0. 0 for running gear with its own surface (rails) and for an aircraft in flight. Derived, never saved.
    /// </summary>
    public float GroundPathCost
    {
        get
        {
            VehiclePawn vehicle = Vehicle;
            Powertrain? train = Powertrain;
            if (train != null && train.OwnSurface) return 0f;
            if (vehicle.CompVehicleLauncher?.inFlight ?? false) return 0f;
            if (!vehicle.Spawned) return train != null ? (float)train.WorldPathCost : OpenSoilPathCost;
            if (vehicle.Position != groundReadCell || vehicle.Map != groundReadMap)
            {
                groundReadCell = vehicle.Position;
                groundReadMap = vehicle.Map;
                Map map = vehicle.Map;
                groundReadCost = (map.terrainGrid.TerrainAt(groundReadCell)?.pathCost ?? 0)
                                 + WeatherBuildupUtility.MovementTicksAddOn(map.snowGrid.GetCategory(groundReadCell));
            }
            return groundReadCost;
        }
    }

    int fireShareTick = -1;
    float fireShare;

    /// <summary>
    /// How fiercely the vehicle burns now: its biggest attached fire as a share of the largest a fire grows (0 to 1). Only a fire on
    /// the vehicle's own map counts (Vehicle Framework can leave one behind when the vehicle leaves), and only the biggest, since a
    /// vehicle can carry duplicate fires the game never puts out (docs/development/temporary-patches.md, J3). A hull under water, or a
    /// vehicle off the map, does not burn. Read at most once a tick; derived, never saved.
    /// </summary>
    public float FireShare
    {
        get
        {
            int now = Find.TickManager.TicksGame;
            if (now != fireShareTick)
            {
                fireShareTick = now;
                fireShare = ReadFireShare();
            }
            return fireShare;
        }
    }

    float ReadFireShare()
    {
        VehiclePawn vehicle = Vehicle;
        if (vehicle == null || !vehicle.Spawned) return 0f;
        if (Submersibles.CompOf(vehicle)?.Hidden == true) return 0f;
        List<AttachableThing>? attachments = vehicle.GetComp<CompAttachBase>()?.attachments;
        if (attachments == null) return 0f;
        float biggest = 0f;
        foreach (AttachableThing attachment in attachments)
        {
            if (attachment is Fire fire && fire.Spawned && fire.Map == vehicle.Map && fire.fireSize > biggest) biggest = fire.fireSize;
        }
        return Mathf.Clamp01(biggest / Fire.MaxFireSize);
    }

    /// <summary>Slow damage to one living part of the role, without armour or reactions (wear, heat). With <paramref name="neverBelowShare"/>
    /// above 0 it never takes a part below that share of its full health, and leaves alone a part already at or under it: wear that
    /// stops short of putting the part out (the landing-surface rule's dust).</summary>
    public void WearRole(string role, float amount, float shareOfHealth = 0f, float neverBelowShare = 0f)
    {
        if (amount <= 0f && shareOfHealth <= 0f) return;
        VehicleComponent? part = PartsOf(role).Where(p => p.Health > Mathf.Max(0f, neverBelowShare) * p.MaxHealth).RandomElementWithFallback();
        if (part == null) return;
        float health = part.Health - amount - shareOfHealth * part.MaxHealth;
        part.SetHealth(neverBelowShare > 0f ? Mathf.Max(neverBelowShare * part.MaxHealth, health) : health);
        RememberPartHealth(part);
        Vehicle.statHandler.MarkAllDirty();
        Repairs.NotifyRepairable();    // Vehicle Framework's list of vehicles to repair is fed only by real hits
    }

    /// <summary>Takes one part to nothing at once, outside any armour step, as wear does (Framework 0.245.0: a burst tyre, FR11).</summary>
    internal void BreakPart(VehicleComponent part)
    {
        part.SetHealth(0f);
        RememberPartHealth(part);
        Vehicle.statHandler.MarkAllDirty();
        Repairs.NotifyRepairable();
    }

    /// <summary>The role a loadout binds an aircraft's wheels, skids or landing legs to (Framework 0.140.0).</summary>
    public const string LandingGearRole = "LandingGear";

    /// <summary>A hard landing's wear (Framework 0.140.0, the rolling landing of LandingCheck): one landing-gear part loses a share of its
    /// full health, or, where the loadout binds none, one of the vehicle's outer parts, its airframe. Never below one health: wear, not
    /// a break.</summary>
    public void WearLanding(float shareOfHealth)
    {
        if (shareOfHealth <= 0f || Vehicle?.statHandler == null) return;
        // A practised pilot puts it down more gently (Framework 0.190.0, EM11).
        shareOfHealth *= VehiclePractice.LandingFactor(Vehicle);
        VehicleComponent? part = PartsOf(LandingGearRole).Where(p => p.Health > 1f).RandomElementWithFallback()
            ?? Vehicle.statHandler.components.Where(p => p.props.depth == VehicleComponent.VehiclePartDepth.External && p.Health > 1f).RandomElementWithFallback();
        if (part == null) return;
        part.SetHealth(Mathf.Max(1f, part.Health - shareOfHealth * part.MaxHealth));
        RememberPartHealth(part);
        Vehicle.statHandler.MarkAllDirty();
        Repairs.NotifyRepairable();
    }

    /// <summary>A sudden failure: one living part of the role loses a share of its full health. Returns the part, or null.</summary>
    public VehicleComponent? BreakRole(string role, float fraction)
    {
        VehicleComponent? part = PartsOf(role).Where(p => p.Health > 0f).RandomElementWithFallback();
        if (part == null) return null;
        BreakPart(part, fraction);
        return part;
    }

    /// <summary>Takes a share of a part's health at once, as a breakdown does.</summary>
    internal void BreakPart(VehicleComponent part, float fraction)
    {
        part.SetHealth(part.Health - fraction * part.MaxHealth);
        RememberPartHealth(part);
        Vehicle.statHandler.MarkAllDirty();
        Repairs.NotifyRepairable();
    }

    // ---- What the game asks ----

    public float ActiveLoadAmps() => loadAmps;

    /// <summary>Engine speed in rpm: the model's when the vehicle has a drivetrain or turbine, else idle or a cruise figure.</summary>
    public float EngineRpm(float idle, float cruise)
    {
        Powertrain? train = Powertrain;
        if (train != null && VehicleSystemsMod.Settings.drivetrainMode != DrivetrainMode.Off && EngineRunning)
        {
            // Turning for work with nobody driving: the drivetrain has not sampled it, so the speed follows the work, from idle to rated.
            if (!EngineRunningRaw || WorkTurningEngine) return Mathf.Lerp((float)train.Spec.IdleRpm, (float)train.Spec.RatedRpm, EngineLoad);
            return (float)train.Result.Rpm;
        }
        return EngineRunning ? (Moving ? cruise : idle) : 0f;
    }

    /// <summary>Engine heat as a share of full-load heat: the model's when the player chose Full, else the old load figure.</summary>
    public float EngineHeatShare()
    {
        // The drivetrain has sampled nothing for work done standing still: without this, overdrive made no heat.
        if (WorkTurningEngine) return EngineLoad;
        Powertrain? train = Powertrain;
        // The model's heat is for the drive alone; the loads' share is added, as the alternator's power is burned like any other.
        if (train != null && train.Full && train.Ground) return EngineRunning ? (float)train.HeatShare + AccessoryShare : 0f;
        return EngineLoad;
    }

    /// <summary>Power at the wheels in kW, or 0 without a drivetrain.</summary>
    public float DrivePowerKw() => (float)(Powertrain?.Result.DrivePowerKw ?? 0);

    /// <summary>The share of a role's parts below a health fraction (flat tyres, for rolling resistance).</summary>
    public float RoleShareBelow(string role, float fraction)
    {
        int below = 0, count = 0;
        foreach (VehicleComponent part in PartsOf(role))
        {
            count++;
            if (part.HealthPercent < fraction) below++;
        }
        return count == 0 ? 0f : below / (float)count;
    }

    /// <summary>
    /// Whether the grid carries this parked vehicle's loads now (Framework 0.120.0; owner, 17 Nov 2026): its hook-up conduit lies under it, is
    /// on a grid with power to give, and the grid has not cut it off. Read at most once per update.
    /// </summary>
    public bool GridCarriesLoads
    {
        get
        {
            if (gridCarries.HasValue) return gridCarries.Value;
            // Not while the engine runs (Framework 0.120.5, fault-read G4): the alternator carries the loads then, and the engine burns fuel for
            // them, so the grid paying too was paying twice.
            gridCarries = !EngineRunning && !OutletGiving && GridHookedUp();
            return gridCarries.Value;
        }
    }

    bool? gridCarries;

    /// <summary>The watts the grid gives this vehicle now (0 when it is not hooked up), noted when seen in play. Derived, never saved.</summary>
    public float GridWatts { get; private set; }

    /// <summary>
    /// Sets the hook-up's draw on the colony's grid (Framework 0.120.0): the loads the grid carries and the battery's charge, at the vehicle's
    /// voltage through the supply's efficiency (HookUpRule). The other pieces under the vehicle draw nothing. Off the grid, nothing.
    /// Framework 0.120.5 (fault-read G3 and G4): the draw is asked of the grid whenever the meter's own net has power to give, so the grid
    /// can turn the meter on or off by its own rules; nothing is asked while the engine runs, and no charge while a service lead gives it.
    /// </summary>
    void UpdateHookUp()
    {
        CompPowerTrader? meter = VehicleConduits.Meter(this);
        GridWatts = 0f;
        OutletWatts = 0f;
        OutletGiving = false;
        if (meter == null) return;
        // The power outlet (Framework 0.129.0, Y1): switched on, it turns the same meter round, giving the camp's devices what they ask, up to
        // its rating. The vehicle then draws nothing from that grid.
        if (OutletWanted && LoadRuns(CampOutlet.LoadName))
        {
            OutletGiving = true;
            OutletWatts = (float)CampRule.OutletWatts(CampOutlet.CampAsks(meter), CampRule.OutletRating(Rules.outletWattsAt12Volts, ElectricalVolts()));
            meter.PowerOn = true;
            meter.PowerOutput = OutletWatts;
            if (OutletWatts > 0f && Seen.Wants("vehicles.outlet-giving", Vehicle)) Seen.Note("vehicles.outlet-giving", Vehicle, OutletWatts.ToString("0") + " W");
            return;
        }
        double carried = 0, charging = 0;
        if (!EngineRunning && NetOffers(meter))
        {
            carried = HookUpRule.CarriedAmps(loadAmps, Rules.hookUpRatingAmps);
            double chargerAmps = 0, charge = 1;
            if (!ServiceLeads.Feeding(Vehicle))
            {
                foreach (SystemState state in states!)
                {
                    int q = state.Def.QuantityIndex("charge");
                    if (q < 0 || !Enabled(state)) continue;
                    charge = state.values[q];
                    chargerAmps = new SystemContext(this, state).Parameter("shoreChargeAmps");
                    break;
                }
            }
            charging = HookUpRule.ChargeAmps(chargerAmps, charge);
            GridWatts = (float)HookUpRule.GridWatts(carried, charging, ElectricalVolts(), Rules.hookUpEfficiency);
        }
        meter.PowerOutput = -GridWatts;
        // Framework 0.137.3: noted only for a real draw. A full battery on the grid settles where the charger's last trickle matches the
        // battery's self-discharge, a float charge of a few milliamps (a tenth of a watt), which noted every parked vehicle as "0 W".
        // Framework 0.138.0: its figures as numbers, and carrying loads or only charging as its case, so each is noted once a session.
        if (Seen.On && GridWatts >= SeenGridWatts)
        {
            Seen.Note("vehicles.grid-hook-up", Vehicle, GridWatts.ToString("0") + " W: loads " + carried.ToString("0.0") + " A, charging " + charging.ToString("0.0") + " A",
                carried >= SeenLoadsAmps ? "carrying loads" : "charging only",
                new[] { Figure("W", GridWatts), Figure("loadsA", carried), Figure("chargingA", charging) });
        }
    }

    /// <summary>The least grid draw the seen-in-play note counts as the hook-up at work, in the game's watts (testing only, never play).</summary>
    const float SeenGridWatts = 1f;

    /// <summary>The loads, in amps, from which the grid note's case is "carrying loads" rather than "charging only" (testing only).</summary>
    const double SeenLoadsAmps = 1;

    static KeyValuePair<string, double> Figure(string name, double value) => new(name, System.Math.Round(value, 3));

    /// <summary>Whether the meter's own net has power to give beyond what the meter itself draws: stored energy, or a surplus.</summary>
    static bool NetOffers(CompPowerTrader meter)
    {
        PowerNet? net = meter.PowerNet;
        if (net == null) return false;
        if (net.CurrentStoredEnergy() > 1f) return true;
        float ours = meter.PowerOn ? meter.PowerOutput * CompPower.WattsToWattDaysPerTick : 0f;
        return net.CurrentEnergyGainRate() - ours > 0f;
    }

    /// <summary>Whether the grid hook-up delivers now: the meter beneath the vehicle is switched on by its grid, and that grid has power.</summary>
    bool GridHookedUp()
    {
        CompPowerTrader? meter = VehicleConduits.Meter(this);
        return meter != null && meter.PowerOn && NetOffers(meter);
    }

    /// <summary>
    /// Whether the parked vehicle has shore power: a generator set's service lead feeding it, or its grid hook-up delivering. Read at most once
    /// per update. Framework 0.120.5 (fault-read G3): it was any powered net within a cell, the meter on or not, so the battery charged
    /// with nothing billed beside a powered appliance, on a grid short of surplus, or in a brown-out. A vehicle standing where every cell
    /// already has a conduit has no meter of its own, and so no shore power from that grid.
    /// </summary>
    public bool ShorePowered()
    {
        if (shorePowered.HasValue) return shorePowered.Value;
        shorePowered = false;
        VehiclePawn vehicle = Vehicle;
        if (!vehicle.Spawned || Moving) return false;
        shorePowered = ServiceLeads.Feeding(vehicle) || (!OutletGiving && GridHookedUp());
        return shorePowered.Value;
    }

    /// <summary>Starts or stops the engine, saying which, or why it would not start. The one switch Vehicle Framework has for this is the draft.</summary>
    public void SetIgnition(bool on)
    {
        if (on)
        {
            AcceptanceReport report = Vehicle.CanDraft();
            if (!report.Accepted)
            {
                Effect_RefuseDraft? refusing = RefusingStart();
                VehicleSystemSounds.PlayOnce(this, refusing == null ? null : VehicleSystemSounds.ForEngine(this, refusing.sound, refusing.soundPetrol));
                // Framework 0.321.0: and seen, where the refusal names a cough (gelled fuel cranked through white).
                VehicleFx.Crank(Vehicle, refusing);
                Messages.Message(report.Reason, Vehicle, MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
        }
        Vehicle.ignition.Drafted = on;
        Messages.Message((on ? "PhobosVehicleSystems_DashEngineStarted" : "PhobosVehicleSystems_DashEngineStopped").Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(),
            Vehicle, MessageTypeDefOf.SilentInput, historical: false);
    }

    const int RareTicksPerPipeLook = 8;
    int pipeLooks;
    Dictionary<string, Phobos.Framework.Persistence.PipeUnderlay> pipeUnderlays = new();

    /// <summary>The pipes this vehicle has laid beneath it for one network, made the first time they are asked for.</summary>
    public Phobos.Framework.Persistence.PipeUnderlay Underlay(string key)
    {
        if (!pipeUnderlays.TryGetValue(key, out Phobos.Framework.Persistence.PipeUnderlay? underlay))
        {
            underlay = new Phobos.Framework.Persistence.PipeUnderlay();
            pipeUnderlays[key] = underlay;
        }
        return underlay;
    }

    /// <summary>The networks it holds pipes for whose keys start like this, for a link to take up what it no longer needs.</summary>
    public List<KeyValuePair<string, Phobos.Framework.Persistence.PipeUnderlay>> UnderlaysStartingWith(string prefix) =>
        pipeUnderlays.Where(pair => pair.Key.StartsWith(prefix, System.StringComparison.Ordinal)).ToList();

    /// <summary>What a fuel line touching the parked vehicle is doing for its tank, in words, for the inspect pane; set by the pipe link
    /// at each look, never saved.</summary>
    public string? PipeFuelLine { get; set; }

    /// <summary>
    /// Whether its pipes and conduits lie beneath it now (Framework 0.108.0; owner, 13 Nov 2026, UnderlayRule): the player's, standing on a map, not moving,
    /// with its engine off, or set up for work (its work runs through them while the engine turns it). Framework 0.120.5 (fault-read H5): a
    /// vehicle on the recovery tow counts as moving, as a towed trailer does. On a trailer this host lays none (Framework 0.133.0): the trailer's
    /// own comp (CompPhobosTrailer) lays its pipes.
    /// </summary>
    public bool ParkedForPipes => !IsTrailer && Phobos.Framework.Rules.UnderlayRule.Lays(Vehicle.Spawned, Vehicle.Faction == Faction.OfPlayer,
        Vehicle.vehiclePather is { Moving: true } || Hitches.Towing(Vehicle) != null, ignitionHint ?? Vehicle.Drafted,
        // A switched-on power outlet keeps them down while the engine idles to carry the camp (Framework 0.129.0, Y1), as set-up work does.
        Vehicle.GetComps<CompPhobosSetUp>().Any(setUp => setUp.Deployed) || OutletWanted);

    /// <summary>The engine's state an ignition event is about, while that event is handled: the event may come before the switch itself turns. Never saved.</summary>
    bool? ignitionHint;

    /// <summary>The health of the engine's worst part as a share, or 1 if it has none.</summary>
    public float EngineHealthShare() => PartsOf("Engine").Select(p => p.HealthPercent).DefaultIfEmpty(1f).Min();

    public override AcceptanceReport CanDraft()
    {
        if (!Usable) return true;
        UpdateNow();
        string? reason = FirstRefusal(draft: true);
        return reason == null ? true : new AcceptanceReport(reason);
    }

    public override AcceptanceReport CanMove(FloatMenuContext context)
    {
        if (!Usable) return true;
        UpdateNow();
        string? reason = FirstRefusal(draft: false);
        return reason == null ? true : new AcceptanceReport(reason);
    }

    /// <summary>What turning the key meets now: the first refusing stage's refusal that has a sound or a cough; null if none says.</summary>
    Effect_RefuseDraft? RefusingStart()
    {
        if (states == null || Vehicle.Faction != Faction.OfPlayer) return null;
        foreach ((SystemContext context, StageSpec stage, string _) in StagesInForce())
        {
            foreach (VehicleSystemEffect effect in stage.effects)
            {
                if (effect is not Effect_RefuseDraft refusal || !refusal.Active(context) || refusal.DraftRefusal(context) == null) continue;
                if (VehicleSystemSounds.ForEngine(this, refusal.sound, refusal.soundPetrol) != null || refusal.crankPuffs > 0) return refusal;
            }
        }
        return null;
    }

    string? FirstRefusal(bool draft)
    {
        // Only the player's vehicles refuse: another faction's raid or caravan must never be left stranded by a flat battery.
        if (states == null || Vehicle.Faction != Faction.OfPlayer) return null;
        foreach ((SystemContext context, StageSpec stage, string _) in StagesInForce())
        {
            foreach (VehicleSystemEffect effect in stage.effects)
            {
                if (!effect.Active(context)) continue;
                string? reason = draft ? effect.DraftRefusal(context) : effect.MoveRefusal(context);
                if (reason != null) return reason;
            }
        }
        return null;
    }

    string? FirstWorkTripRefusal()
    {
        // As FirstRefusal: only the player's vehicles, so another faction's are never held back.
        if (states == null || Vehicle.Faction != Faction.OfPlayer) return null;
        foreach ((SystemContext context, StageSpec stage, string _) in StagesInForce())
        {
            foreach (VehicleSystemEffect effect in stage.effects)
            {
                if (effect is Effect_KeepFromWorkTrips keep && keep.Active(context)) return keep.WorkTripRefusal(context);
            }
        }
        return null;
    }

    /// <summary>The factor the current stages put on a stat; read by the stat part on every Vehicle Framework stat.</summary>
    public float StatFactor(VehicleStatDef stat) => (statFactors.TryGetValue(stat, out float factor) ? factor : 1f) * Fittings.StatFactor(stat);

    /// <summary>How fast an electric vehicle charges from the grid now, against Vehicle Framework's own rate (derived, never saved).</summary>
    public float ChargeFactor { get; private set; } = 1f;

    /// <summary>The factor on the fuel this aircraft burns on the world map now (Framework 0.110.0, avionics A2; derived, never saved).</summary>
    public float FlightFuelFactor { get; private set; } = 1f;

    /// <summary>The factor the stages put on the fuel the engine burns now (Framework 0.123.0: a clogged air filter; derived, never saved).</summary>
    public float EngineFuelFactor { get; private set; } = 1f;

    /// <summary>The share of the engine's power the stages leave it now (Framework 0.130.0, step S4: a choked air filter, a warped head, a
    /// starved fuel feed); derived, never saved. The drivetrain model reads it where <see cref="PowerLossInModel"/>; otherwise it is folded
    /// into the move speed factor.</summary>
    public float EnginePowerFactor { get; private set; } = 1f;

    /// <summary>Whether lost engine power is carried by the drivetrain model (full mode, a ground drivetrain) rather than as lost speed.</summary>
    public bool PowerLossInModel => Powertrain is { Full: true, Ground: true };

    /// <summary>The stage labels behind a stat's factor, for the stat card's explanation.</summary>
    public IEnumerable<string> StatFactorReasons(VehicleStatDef stat)
    {
        foreach ((SystemContext context, StageSpec stage, string owner) in StagesInForce())
        {
            foreach (VehicleSystemEffect effect in stage.effects)
            {
                if (!effect.Active(context)) continue;
                // Step S4 (Framework 0.130.0): lost engine power shows on move speed where it is taken as speed (no full drivetrain model).
                float factor = effect.StatFactor(stat);
                if (stat == VehicleStatDefOf.MoveSpeed && !PowerLossInModel) factor *= effect.EnginePowerFactor;
                if (factor != 1f) yield return owner + ": " + stage.label + " x" + factor.ToStringByStyle(ToStringStyle.FloatTwo);
            }
        }
        foreach (string reason in Fittings.StatFactorReasons(stat)) yield return reason;
    }

    /// <summary>Kept between updates so working the factors out allocates nothing (Framework 0.130.0, step S8). Derived, never saved.</summary>
    readonly Dictionary<VehicleStatDef, float> freshFactors = new();
    readonly HashSet<VehicleStatDef> changedStats = new();

    void RecacheStatFactors()
    {
        Dictionary<VehicleStatDef, float> fresh = freshFactors;
        fresh.Clear();
        float charge = 1f;
        float flightFuel = 1f;
        float engineFuel = 1f;
        float enginePower = 1f;
        foreach ((SystemContext context, StageSpec stage, string _) in StagesInForce())
        {
            foreach (VehicleSystemEffect effect in stage.effects)
            {
                if (!effect.Active(context)) continue;
                charge *= effect.ChargeFactor;
                flightFuel *= effect.FlightFuelFactor;
                engineFuel *= effect.EngineFuelFactor;
                enginePower *= effect.EnginePowerFactor;
                // Step S8: the effect names its one stat, where every stat in the game was asked in turn before.
                if (effect.FactorStat is not { } stat) continue;
                float factor = effect.StatFactor(stat);
                if (factor != 1f) fresh[stat] = (fresh.TryGetValue(stat, out float before) ? before : 1f) * factor;
            }
        }
        ChargeFactor = charge;
        // Gap T-3 (Framework 0.132.0): flying between tiles follows the engines too, through the same factor the stages use.
        FlightFuelFactor = flightFuel * (float)(Powertrain?.WorldFlightFuelShare() ?? 1);
        EngineFuelFactor = engineFuel;
        // Step S4 (Framework 0.130.0): lost power is lost power where the drivetrain model can carry it; elsewhere it is lost speed, as before.
        EnginePowerFactor = enginePower;
        if (enginePower < 1f && !PowerLossInModel && VehicleStatDefOf.MoveSpeed is { } moveSpeed)
        {
            fresh[moveSpeed] = (fresh.TryGetValue(moveSpeed, out float before) ? before : 1f) * enginePower;
        }
        HashSet<VehicleStatDef> changed = changedStats;
        changed.Clear();
        changed.UnionWith(fresh.Keys);
        changed.SymmetricExceptWith(statFactors.Keys);
        foreach (KeyValuePair<VehicleStatDef, float> pair in fresh)
        {
            if (statFactors.TryGetValue(pair.Key, out float old) && Mathf.Abs(old - pair.Value) > 0.0001f) changed.Add(pair.Key);
        }
        statFactors.Clear();
        foreach (KeyValuePair<VehicleStatDef, float> pair in fresh) statFactors[pair.Key] = pair.Value;
        if (Vehicle?.statHandler == null) return;
        foreach (VehicleStatDef stat in changed) Vehicle.statHandler.MarkStatDirty(stat);
    }

    /// <summary>Whether the comp may send the notice of this key now, and if so marks it sent.</summary>
    public bool MayNotify(string key, float cooldownHours)
    {
        int now = Find.TickManager.TicksGame;
        if (lastNotice.TryGetValue(key, out int last) && now - last < cooldownHours * ElapsedRule.TicksPerHour) return false;
        lastNotice[key] = now;
        return true;
    }

    // ---- Loads ----

    /// <summary>The loads a hit to the wiring has shorted now, by name (cross-links G12, Framework 0.123.0): worked out from the conditions in
    /// force at each update and whenever a condition changes. Derived, never saved: the same parts give the same circuits (WiringRule).</summary>
    readonly HashSet<string> shortedLoads = new();
    readonly List<string> shortedBefore = new();
    bool shortsLookedOnce;

    /// <summary>Whether a hit to the wiring has shorted this load.</summary>
    public bool IsShorted(ElectricalLoadSpec load) => shortedLoads.Count > 0 && shortedLoads.Contains(load.name);

    void RefreshShortedLoads()
    {
        shortedBefore.Clear();
        shortedBefore.AddRange(shortedLoads);
        shortedLoads.Clear();
        PhobosVehicleLoadoutDef? loadout = Props.loadout;
        if (conditions != null && conditions.Count > 0 && loadout != null && states != null && states.Count > 0)
        {
            List<ElectricalLoadSpec>? candidates = null;
            foreach (VehicleCondition condition in conditions.All)
            {
                StageSpec? stage = condition.CurrentStage;
                if (stage == null) continue;
                SystemContext? context = conditions.ContextFor(condition);
                if (context == null) continue;
                int howMany = 0;
                foreach (VehicleSystemEffect effect in stage.effects)
                {
                    if (effect.Active(context)) howMany += effect.ShortedLoads;
                }
                if (howMany <= 0) continue;
                candidates ??= Loads.Where(l => l.toggle && LoadAvailable(l)).ToList();
                int seed = Gen.HashCombineInt(Vehicle.thingIDNumber, GenText.StableStringHash(condition.partKey));
                foreach (int index in WiringRule.Pick(seed, candidates.Count, howMany)) shortedLoads.Add(candidates[index].name);
            }
        }
        if (!shortsLookedOnce)
        {
            // The first look after a load: the circuits a save left shorted are no news.
            shortsLookedOnce = true;
            return;
        }
        if (Vehicle.Faction != Faction.OfPlayer || loadout == null) return;
        List<string> fresh = Loads.Where(l => shortedLoads.Contains(l.name) && !shortedBefore.Contains(l.name)).Select(l => l.label).ToList();
        if (fresh.Count == 0) return;
        Messages.Message("PhobosVehicleSystems_MsgWiringShorted".Translate(Vehicle.LabelShort.Named("VEHICLE"), fresh.ToCommaList(useAnd: true).Named("LOADS")),
            Vehicle, MessageTypeDefOf.NegativeEvent);
        if (Seen.Wants("vehicles.wiring-shorted", Vehicle)) Seen.Note("vehicles.wiring-shorted", Vehicle, string.Join(", ", fresh));
    }

    /// <summary>
    /// Where the load's switch stands, as its button shows it. For a load that switches itself (<see cref="Automatic"/>), whether it may
    /// switch itself on at all on this vehicle (on unless the player switched it off); otherwise whether it is switched on.
    /// </summary>
    public bool IsOn(ElectricalLoadSpec load)
    {
        if (Automatic(load)) return !loadSwitches.TryGetValue(AutoKey(load), out bool allowed) || allowed;
        return loadSwitches.TryGetValue(load.name, out bool on) ? on : load.defaultOn;
    }

    /// <summary>Whether a load of this name is fitted and switched on (or has no switch), whether or not it runs now (Framework 0.289.0).</summary>
    public bool LoadSwitchedOn(string name) => LoadNamed(name) is { } load && LoadAvailable(load) && (!load.toggle || IsOn(load));

    /// <summary>
    /// A reading kept clear by start-reserve loads, and the line it is kept above (from its own stages; NaN where none refuses a start), for
    /// what else keeps clear of it (a visitor's radio, Framework 0.289.0). False when the vehicle has no such reading.
    /// </summary>
    public bool TryStartLine(string quantity, out float reading, out double line)
    {
        line = double.NaN;
        if (!TryGetQuantity(quantity, out reading)) return false;
        line = StartLine(quantity);
        return true;
    }

    /// <summary>Whether this load switches itself now (Framework 0.171.0): it has an automatic rule and the player's setting is on.</summary>
    public bool Automatic(ElectricalLoadSpec load) => load.autoOn != null && VehicleSystemsMod.Settings.autoLights;

    /// <summary>The saved key of an automatic load's "may switch itself on" button: kept apart from the hand switch, so a lamp switched off by
    /// hand before the setting came is not kept dark by it.</summary>
    static string AutoKey(ElectricalLoadSpec load) => "auto:" + load.name;

    /// <summary>Whether a switched-on load is also wanted by its automatic rule (always, for a load switched by hand). Read from the cache
    /// <see cref="RefreshAutoSwitches"/> fills once per look, so buttons and tabs never compute it.</summary>
    bool AutoHolds(ElectricalLoadSpec load) => !Automatic(load) || autoWanted.Contains(load.name);

    /// <summary>The automatic loads whose rule holds at the last look. Derived, never saved.</summary>
    readonly HashSet<string> autoWanted = new();

    /// <summary>The tick the work lamps' rule last saw someone of the colony within their light (<see cref="When_PeopleNear"/>), so they
    /// stay lit a while after the last leaves. Derived, never saved: after a load they wait for the next person.</summary>
    internal int PeopleNearTick = int.MinValue / 2;

    /// <summary>Works out which automatic loads their rules want on now (Framework 0.171.0). Once per look, from <see cref="RefreshCaches"/>.</summary>
    void RefreshAutoSwitches()
    {
        autoWantedBefore.Clear();
        autoWantedBefore.UnionWith(autoWanted);
        autoWanted.Clear();
        if (!VehicleSystemsMod.Settings.autoLights || states == null || states.Count == 0 || Tier != DetailTier.Full) return;
        var context = new SystemContext(this, states[0]);
        foreach (ElectricalLoadSpec load in Loads)
        {
            if (load.autoOn == null || !load.autoOn.Holds(context)) continue;
            autoWanted.Add(load.name);
            if (!autoWantedBefore.Contains(load.name) && IsOn(load) && SeenInPlay.On) Seen.Note("vehicles.lights-auto", Vehicle, load.name, variant: load.name);
        }
    }

    readonly HashSet<string> autoWantedBefore = new();

    public void SetSwitch(ElectricalLoadSpec load, bool on)
    {
        loadSwitches[Automatic(load) ? AutoKey(load) : load.name] = on;
        if (states != null && states.Count > 0) loadAmps = SumLoads(new SystemContext(this, states[0]));
        // The light follows the switch at once, not at the next rare tick (Framework 0.171.0).
        if (load.beam != null) Beams.Update();
        if (load.name == WorkLamps.LoadName) Lamps.Update();
    }

    // ---- Service ----

    public bool ServiceRequested => serviceRequested;

    public void SetServiceRequested(bool requested)
    {
        serviceRequested = requested;
        servicedThisRequest.Clear();
    }

    bool Due(SystemState state)
    {
        ServiceSpec? service = state.Def.service;
        if (service == null || !Enabled(state)) return false;
        if (serviceRequested && !servicedThisRequest.Contains(state.Def.defName)) return true;
        if (conditions != null && conditions.AnyClearedBy(service)) return true;
        int index = state.Def.QuantityIndex(service.dueQuantity);
        if (index >= 0 && ThresholdRule.Passes(state.values[index], service.dueAbove, service.dueBelow)) return true;
        foreach (ServiceDueRule rule in service.alsoDue)
        {
            if (rule.setting != null && !VehicleSystemsMod.Settings.FlagOn(rule.setting)) continue;
            if (TryGetQuantity(rule.quantity, out float value) && ThresholdRule.Passes(value, rule.above, rule.below))
            {
                // Seen in play (Framework 0.301.0, row 28): a service called by a player's setting, such as low oil calling one.
                if (rule.setting != null) Seen.Note("vehicles.setting-calls-service", Vehicle, rule.quantity, rule.setting);
                return true;
            }
        }
        return false;
    }

    /// <summary>The first system whose service is due, or null.</summary>
    public SystemState? DueService()
    {
        foreach (SystemState state in DueServices()) return state;
        return null;
    }

    /// <summary>Every system whose service is due, in the loadout's order.</summary>
    public IEnumerable<SystemState> DueServices()
    {
        if (!Usable) yield break;
        EnsureStates();
        if (states == null) yield break;
        foreach (SystemState state in states)
        {
            if (Due(state)) yield return state;
        }
    }

    public IEnumerable<ServiceSpec> AllServices() => Props.systems.Select(s => s.def.service).OfType<ServiceSpec>();

    /// <summary>How many of a supply the work store and the cargo hold between them (the store since Framework 0.272.0); a building named
    /// as a supply is counted packed (PackedThings, Framework 0.265.0).</summary>
    internal int CountInCargo(ThingDef def) =>
        Phobos.Framework.Items.PackedThings.Count(CargoNow(), def) + (WorkStore is { Empty: false } store ? Phobos.Framework.Items.PackedThings.Count(store.Held, def) : 0);

    /// <summary>The convoy this vehicle travels in away from any map, whose cargo its repairs and services may use; null on a map, where
    /// haulers bring supplies to the vehicle itself.</summary>
    Caravan? ConvoyCaravan() => Vehicle != null && !Vehicle.Spawned ? Vehicle.GetCaravan() : null;

    IEnumerable<Thing>? CargoNow() => ConvoyCaravan() is { } caravan ? Stranding.ConvoyCargo(caravan) : Vehicle.inventory?.innerContainer;

    /// <summary>Everything every due service still lacks in the vehicle's cargo, added up by kind (the core's SupplyRules), so the
    /// haulers fetch it all and the vehicle says it all.</summary>
    public List<(ThingDef def, int count)> MissingSupplies() => SupplyRules.Shortfall(SupplyNeeds(), CountInCargo);

    /// <summary>What the due services and the repairs nothing aboard can pay for each ask, one list each.</summary>
    internal IEnumerable<IEnumerable<(ThingDef thingDef, int count)>> SupplyNeeds() =>
        DueServices().Where(s => s.Def.service != null)
            .Select(s => s.Def.service!.SuppliesNow.Select(supply => (supply.thingDef, supply.count))).Concat(Repairs.UnmetPayments());

    /// <summary>What all the waiting work could use, met or not (Framework 0.286.2): the due services' supplies and each repair's payment, so the
    /// work store keeps that much and sets down the rest. Always covers <see cref="SupplyNeeds"/>, so nothing set down is fetched again.</summary>
    internal IEnumerable<IEnumerable<(ThingDef thingDef, int count)>> WorkSupplyNeeds() =>
        DueServices().Where(s => s.Def.service != null)
            .Select(s => s.Def.service!.SuppliesNow.Select(supply => (supply.thingDef, supply.count))).Concat(Repairs.Payments());

    /// <summary>The first supply the due services still lack, for a hauler to fetch.</summary>
    public bool MissingSupply(out ThingDef? def, out int count)
    {
        def = null;
        count = 0;
        List<(ThingDef def, int count)> missing = MissingSupplies();
        if (missing.Count == 0) return false;
        (def, count) = missing[0];
        return true;
    }

    /// <summary>Whether one service's own supplies are all aboard.</summary>
    bool SuppliesAboard(SystemState state) =>
        state.Def.service == null || state.Def.service.SuppliesNow.All(supply => CountInCargo(supply.thingDef) >= supply.count);

    /// <summary>Whether the due service's water can be had now (always, unless a water source such as plumbing is required and none
    /// is beside the vehicle).</summary>
    public bool WaterReady(out string? reason) => WaterReady(DueService(), out reason);

    /// <summary>Whether this service's water can be had now.</summary>
    public bool WaterReady(SystemState? state, out string? reason)
    {
        reason = null;
        float litres = ServiceWater(state);
        if (litres <= 0f) return true;
        return VehicleWater.CanDraw(Vehicle, litres, out reason);
    }

    /// <summary>The litres a due service draws: its fixed figure or its parameter, and only the shortfall when it says so.</summary>
    public float ServiceWater(SystemState? state)
    {
        ServiceSpec? service = state?.Def.service;
        if (state == null || service == null) return 0f;
        float litres = service.waterLitres;
        string? parameter = service.scaledWaterLitresParameter != null && !VehicleSystemsMod.Settings.FlagOn(service.realisticWaterSetting)
            ? service.scaledWaterLitresParameter : service.waterLitresParameter;
        if (parameter != null && state.system.parameters.TryGetValue(parameter, out double fromParameter))
        {
            litres = (float)fromParameter;
        }
        // Framework 0.300.0: a compressor wash rinses each engine, so a four-engined aircraft takes four times a single's water.
        if (service.waterPerEngine) litres *= Powertrain.EngineRolesOf(this).Length;
        if (litres > 0f && service.waterForShortfall)
        {
            QuantityRestore? restore = service.restores.FirstOrDefault(r => r.quantity == service.dueQuantity);
            if (restore != null && restore.value > 0f && TryGetQuantity(service.dueQuantity, out float now))
            {
                litres *= Mathf.Clamp01((restore.value - now) / restore.value);
            }
        }
        return litres;
    }

    /// <summary>The first due service whose own supplies are all aboard, whose water can be had, and which needs no rig it is away from,
    /// or null: a mechanic need not wait for another service's supplies.</summary>
    public SystemState? ReadyService()
    {
        foreach (SystemState state in DueServices())
        {
            if (state.Def.service != null && SuppliesAboard(state) && WaterReady(state, out _) && !WaitsForRig(state)) return state;
        }
        return null;
    }

    /// <summary>Whether a due service is serious work this vehicle must be at a tuning rig for and is not (cross-links S3, Framework 0.124.0).</summary>
    public bool WaitsForRig(SystemState state) => state.Def.service is { needsRig: true } && !Fittings.AtRig();

    /// <summary>The first due service that waits only for a tuning rig, for the inspect line; null when none does.</summary>
    public SystemState? ServiceWaitingForRig() => DueServices().FirstOrDefault(WaitsForRig);

    /// <summary>Uses the supplies from the cargo, restores the quantities, and says so.</summary>
    public bool CompleteService(Pawn mechanic)
    {
        SystemState? state = ReadyService();
        ServiceSpec? service = state?.Def.service;
        if (state == null || service == null) return false;
        float water = ServiceWater(state);
        if (water > 0f && !VehicleWater.TryDraw(Vehicle, water)) return false;
        ThingOwner cargo = Vehicle.inventory.innerContainer;
        foreach (ThingDefCountClass supply in service.SuppliesNow) TakeFromCargo(supply.thingDef, supply.count);
        // Seen in play (Framework 0.301.0, row 37): a service that takes kits or raw materials, done with whichever the colony's choice says.
        if (service.rawSupplies.Count > 0) Seen.Note("vehicles.service-kit-used", Vehicle, service.label, GameComponent_PhobosServiceKits.RawAccepted ? "raw" : "kit");
        foreach (QuantityRestore restore in service.restores)
        {
            // Framework 0.247.0, generic since 0.248.0: a quantity kept per part is restored on the most worn part (or its axle) alone.
            if (PartFigures.RestoreMostWorn(restore.quantity, restore.value) is { } parts)
            {
                if (Seen.Wants("vehicles.part-restored", Vehicle)) Seen.Note("vehicles.part-restored", Vehicle, restore.quantity + ": " + parts);
                continue;
            }
            SetQuantity(restore.quantity, restore.value);
        }
        Conditions.ClearBy(service);
        servicedThisRequest.Add(state.Def.defName);
        RecordEvent(VehicleEventKind.Service, state.Def.defName);
        VehicleStory.Repaired(this, mechanic, service.label, service: true);
        if (service.needsRig) Seen.Note("vehicles.rig-service-done", Vehicle, service.label);
        if (serviceRequested && !Props.systems.Any(s => s.def.service != null && !servicedThisRequest.Contains(s.def.defName)
                                                         && !VehicleSystemsMod.Settings.disabledSystems.Contains(s.def.defName)))
        {
            serviceRequested = false;
            servicedThisRequest.Clear();
        }
        UpdateNow();
        if (Vehicle.Faction == Faction.OfPlayer)
        {
            Messages.Message("PhobosVehicleSystems_Serviced".Translate(mechanic.LabelShort.Named("MECHANIC"), Vehicle.LabelShort.Named("VEHICLE"),
                service.label.Named("SERVICE")), Vehicle, MessageTypeDefOf.TaskCompletion);
        }
        return true;
    }

    // ---- Jump-starting ----

    static int JumpQuantity(SystemState state) => state.Def.jumpStart == null ? -1 : state.Def.QuantityIndex(state.Def.jumpStart.quantity);

    /// <summary>The first system whose jump-start quantity is below the level a jump-start raises it to, or null.</summary>
    public SystemState? JumpStartNeed()
    {
        if (!Usable) return null;
        UpdateNow();
        if (states == null) return null;
        foreach (SystemState state in states)
        {
            int index = JumpQuantity(state);
            if (index >= 0 && Enabled(state) && state.values[index] < state.Def.jumpStart!.raiseTo) return state;
        }
        return null;
    }

    /// <summary>Whether this vehicle can give a jump-start for that system: it has it, and enough charge or a running engine.</summary>
    public bool CanDonate(PhobosVehicleSystemDef def)
    {
        if (!Usable) return false;
        UpdateNow();
        SystemState? state = states?.FirstOrDefault(s => s.Def == def && Enabled(s));
        if (state == null) return false;
        int index = JumpQuantity(state);
        return index >= 0 && (EngineRunning || state.values[index] >= def.jumpStart!.donorMinimum);
    }

    /// <summary>
    /// Raises this vehicle's quantity to the jump-start level from the donor's, which loses the same ampere-hours unless its engine
    /// runs; with no donor, from a crafted booster pack (which spends a start) or, when the player assumes every vehicle carries one,
    /// from nowhere. Says so. Returns false when the vehicle, the donor or the pack can no longer take part.
    /// </summary>
    public bool ReceiveJumpStart(CompPhobosVehicleSystems? donor, Pawn helper, Thing? pack = null)
    {
        SystemState? need = JumpStartNeed();
        JumpStartSpec? spec = need?.Def.jumpStart;
        if (need == null || spec == null) return false;
        CompPhobosCharges? charges = donor == null ? pack?.TryGetComp<CompPhobosCharges>() : null;
        if (donor == null && charges == null && !VehicleSystemsMod.Settings.FlagOn("assumeBoosterPack")) return false;
        if (donor != null && !donor.CanDonate(need.Def)) return false;
        if (charges != null && !charges.TryUse()) return false;
        SystemState? donorState = donor?.states!.First(s => s.Def == need.Def);
        double missing = RaiseForStart(need, spec);
        if (donor != null && donorState != null)
        {
            double capacity = new SystemContext(this, need).Parameter(spec.capacityParameter);
            double donorCapacity = new SystemContext(donor, donorState).Parameter(spec.capacityParameter);
            if (!donor.EngineRunning && donorCapacity > 0) donor.AddToQuantity(spec.quantity, -(float)(missing * capacity / donorCapacity));
            donor.EvaluateStages(donorState, 0f);
        }
        AfterStart(need);
        if (Vehicle.Faction == Faction.OfPlayer)
        {
            NamedArgument vehicle = Vehicle.LabelShort.Named("VEHICLE");
            NamedArgument who = helper.LabelShort.Named("HELPER");
            Messages.Message(donor != null
                ? "PhobosVehicleSystems_JumpStarted".Translate(who, vehicle, donor.Vehicle.LabelShort.Named("DONOR"))
                : charges != null
                    ? "PhobosVehicleSystems_JumpStartedPack".Translate(who, vehicle, charges.WholeUses.Named("COUNT"))
                    : "PhobosVehicleSystems_JumpStartedBooster".Translate(who, vehicle), Vehicle, MessageTypeDefOf.TaskCompletion);
        }
        return true;
    }

    /// <summary>
    /// A start from a generator set's service lead (Framework 0.101.0): the set is the charger, so no donor loses charge and no kit or pack is
    /// spent. The same raise as any jump-start. False when nothing needs a start.
    /// </summary>
    public bool ReceiveLeadStart(VehiclePawn source)
    {
        SystemState? need = JumpStartNeed();
        JumpStartSpec? spec = need?.Def.jumpStart;
        if (need == null || spec == null) return false;
        RaiseForStart(need, spec);
        AfterStart(need);
        // Seen in play (Framework 0.295.0, row 58): a start from a generator set's service lead.
        Seen.Note("vehicles.service-lead-start", Vehicle, source.LabelShort);
        if (Vehicle.Faction == Faction.OfPlayer)
        {
            Messages.Message("PhobosVehicleSystems_LeadStarted".Translate(source.LabelShort.Named("SOURCE"), Vehicle.LabelShort.Named("VEHICLE")),
                new LookTargets(Vehicle, source), MessageTypeDefOf.TaskCompletion);
        }
        return true;
    }

    /// <summary>Raises the quantity to the jump-start level; returns how much it was short, for a donor's loss.</summary>
    double RaiseForStart(SystemState need, JumpStartSpec spec)
    {
        int index = JumpQuantity(need);
        double missing = spec.raiseTo - need.values[index];
        need.values[index] = SystemIntegrator.Clamp(spec.raiseTo, need.min[index], need.max[index]);
        need.Def.Worker.UpdateDerived(new SystemContext(this, need));
        return missing;
    }

    void AfterStart(SystemState need)
    {
        EvaluateStages(need, 0f);
        RecordEvent(VehicleEventKind.JumpStart, need.Def.defName);
        RecacheStatFactors();
        MoveRefusal = FirstRefusal(draft: false);
        WorkTripRefusal = FirstWorkTripRefusal();
    }

    /// <summary>Whether this vehicle has a battery a service lead can charge: any system with a jump-start quantity.</summary>
    public bool HasLeadBattery()
    {
        if (!Usable) return false;
        UpdateNow();
        return states?.Any(s => JumpQuantity(s) >= 0 && Enabled(s)) == true;
    }

    // ---- Sweep ----

    /// <summary>Forgets every quantity, switch and saved health, for the uninstall sweep.</summary>
    public void ClearForRemoval()
    {
        states = null;
        savedValues.Clear();
        loadSwitches.Clear();
        savedPartHealth.Clear();
        events?.Clear();
        statFactors.Clear();
        serviceRequested = false;
        servicedThisRequest.Clear();
        conditions?.Clear();
        recentDamage.Clear();
        fittings?.Clear();
        repairs?.Invalidate();
    }

    // ---- Presentation (reads only) ----

    /// <summary>Tells the player once when damage stops one of their vehicles (Vehicle Framework undrafts it without a word), and why.</summary>
    void NoticeStopped()
    {
        if (!Vehicle.Spawned || Vehicle.Faction != Faction.OfPlayer) { wasStopped = null; return; }
        bool stopped = Mobility.StoppedByDamage(Vehicle, Props);
        bool first = wasStopped == null;
        bool news = stopped && wasStopped == false;
        wasStopped = stopped;
        if (first || !news) return;
        string? why = Mobility.Line(Vehicle, Props);
        Messages.Message("PhobosVehicleSystems_MsgCannotMove".Translate(Vehicle.LabelShort.Named("VEHICLE"), (why ?? "").Named("WHY")),
            Vehicle, MessageTypeDefOf.NegativeEvent);
        Seen.Note("vehicles.stopped-by-damage", Vehicle, why);
        if (Mobility.ThrownTrack(Vehicle, Props) is { } track) Seen.Note("vehicles.track-thrown", Vehicle, track.props.label);
    }

    public override string? CompInspectStringExtra()
    {
        if (!Usable) return null;
        EnsureStates();
        var lines = new List<string>();
        // Knocked out, not destroyed (Framework 0.115.0): why it cannot move, and which weapons are slowed and why.
        if (Vehicle.Spawned && Mobility.Line(Vehicle, Props) is { } stopped) lines.Add(stopped);
        if (Vehicle.Spawned && TurretPace.Line(Vehicle) is { } slowed) lines.Add(slowed);
        if (InspectText.For(this) is { } text) lines.Add(text);
        return lines.Count == 0 ? null : string.Join("\n", lines);
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (!Usable || Vehicle.Faction != Faction.OfPlayer) yield break;
        if (homeBay is { parent.Destroyed: false }) yield return BayGizmos.GoButton(this, homeBay);
        // Framework 0.252.0: a twin's fuel cross-feed.
        if (SideTanks.Applies) yield return SideTanks.Button();
        // A vehicle that won't start: the button sends the nearest colonist with a way to jump-start it (Framework 0.242.0).
        if (Vehicle.Spawned && JumpStartNeed() != null)
        {
            yield return new Command_Action
            {
                defaultLabel = "PhobosVehicleSystems_JumpStartButton".Translate(),
                defaultDesc = "PhobosVehicleSystems_JumpStartButtonDesc".Translate(),
                icon = SystemIcons.For("Phobos/VehicleSystems/JumpStart"),
                action = () =>
                {
                    if (JumpStartSend.OrderNearest(Vehicle) is { } why) Messages.Message(why, Vehicle, MessageTypeDefOf.RejectInput, historical: false);
                },
            };
        }
        // The cab's windows (Framework 0.233.0, hotboxing): a closed cab only; an open one is always open to the air.
        if (ClosedCab && Vehicle.VehicleDef.type == VehicleType.Land)
        {
            var windows = new Command_Toggle
            {
                defaultLabel = "PhobosLifeAboard_Windows".Translate(),
                defaultDesc = "PhobosLifeAboard_WindowsDesc".Translate(),
                icon = SystemIcons.For("Phobos/VehicleSystems/Windows"),
                isActive = () => WindowsDown,
                toggleAction = () => SetWindowsDown(!WindowsDown),
            };
            // With the glass gone there is nothing to wind up (Framework 0.238.0, VG4).
            if (Glass.Gone(Vehicle)) windows.Disable("PhobosVehicleSystems_GlassGoneWindows".Translate());
            yield return windows;
        }
        // People use parked vehicles (Framework 0.289.0): one switch keeps this vehicle's stores for the road. The game's own forbid picture.
        if (VehicleVisits.Enabled && Vehicle.Spawned)
        {
            yield return new Command_Toggle
            {
                defaultLabel = "PhobosVisit_OpenGizmo".Translate(),
                defaultDesc = "PhobosVisit_OpenGizmoDesc".Translate(Vehicle.LabelShort.Named("VEHICLE")),
                icon = OpenToColony ? TexCommand.ForbidOff : TexCommand.ForbidOn,
                isActive = () => OpenToColony,
                toggleAction = () => VehicleVisits.SetOpen(this, !OpenToColony),
            };
        }
        if (Props.loadout != null)
        {
            foreach (ElectricalLoadSpec load in Loads)
            {
                if (!load.toggle || !LoadAvailable(load)) continue;
                ElectricalLoadSpec captured = load;
                yield return new Command_Toggle
                {
                    defaultLabel = captured.label,
                    defaultDesc = captured.description + "\n\n" + "PhobosVehicleSystems_LoadDraw".Translate(AmpsNow(captured).ToString("0.#").Named("AMPS"))
                        + (Automatic(captured) ? "\n\n" + "PhobosVehicleSystems_LoadAuto".Translate(captured.autoDescription.Named("WHEN")).Resolve() : ""),
                    icon = SystemIcons.For(captured.iconPath),
                    isActive = () => IsOn(captured),
                    toggleAction = () => SetSwitch(captured, !IsOn(captured)),
                }.AsPhysicalSwitch();
            }
        }
        if (Vehicle.Spawned && CargoRefuel.AircraftToggle(Vehicle, this) is { } refuel)
        {
            // Framework 0.139.0: set before a journey, an aircraft sets down on the way to refuel from the fuel it carries aboard.
            yield return refuel;
        }
        if (Vehicle.Spawned && AirCombat.HasMapSalvo(Vehicle))
        {
            // Avionics A16 (Framework 0.119.0; owner, 17 Nov 2026): a salvo of chaff and flares on a map, for a few seconds' extra dodge.
            // Its icon is the cartridges' own picture: the button spends them.
            string? refusal = AirCombat.MapSalvoRefusal(Vehicle, When_Airborne.Airborne(this, live: true));
            var salvo = new Command_Action
            {
                defaultLabel = "PhobosAirCombat_SalvoGizmo".Translate(),
                defaultDesc = "PhobosAirCombat_SalvoGizmoDesc".Translate(AirCombat.CartridgesPerSalvo.Named("COUNT"), AirCombat.Cartridges(Vehicle).Named("LEFT"),
                    AirCombat.SalvoSeconds.Named("SECONDS")),
                icon = AirCombat.CartridgeIcon,
                action = () => AirCombat.MapSalvoNow(Vehicle, When_Airborne.Airborne(this, live: true)),
            };
            if (refusal != null) salvo.Disable(refusal);
            yield return salvo;
        }
        if (Repairs.AnyFieldFixable)
        {
            yield return new Command_Toggle
            {
                defaultLabel = "PhobosVehicleSystems_RequestFieldFix".Translate(),
                defaultDesc = "PhobosVehicleSystems_RequestFieldFixDesc".Translate(),
                icon = SystemIcons.For("Phobos/VehicleSystems/FieldFix"),
                isActive = () => Repairs.FieldFixRequested,
                toggleAction = () => Repairs.SetFieldFixRequested(!Repairs.FieldFixRequested),
            };
        }
        if (Fittings.AnythingToFit && Fittings.AtRigShown)
        {
            // Fittings and tunes on the command bar while a dyno bay is in reach (always, with the bay requirement off), so the window
            // is found where tuning can happen (owner, 11 Nov 2026: tuning was too hard to find); the Dashboard's button stays too.
            yield return new Command_Action
            {
                defaultLabel = "PhobosVehicleSystems_FitGizmo".Translate(),
                defaultDesc = "PhobosVehicleSystems_FitGizmoDesc".Translate(),
                icon = SystemIcons.Fittings,
                action = () => Find.WindowStack.Add(new Dialog_PhobosVehicleFittings(this)),
            };
        }
        if (VehicleFittings.RigReach > 0)
        {
            // Any vehicle can be diagnosed once the player has a tuning rig to do it at; the button says what it waits for.
            string? waiting = Diagnostics.Requested ? Diagnostics.WaitingFor() : null;
            yield return new Command_Toggle
            {
                defaultLabel = "PhobosVehicleSystems_Diagnose".Translate(),
                defaultDesc = "PhobosVehicleSystems_DiagnoseDesc".Translate() + (waiting != null ? "\n\n" + waiting : ""),
                icon = SystemIcons.For("Phobos/VehicleSystems/Diagnose"),
                isActive = () => Diagnostics.Requested,
                toggleAction = () => Diagnostics.SetRequested(!Diagnostics.Requested),
            };
        }
        if (AllServices().Any())
        {
            yield return new Command_Toggle
            {
                defaultLabel = "PhobosVehicleSystems_RequestService".Translate(),
                defaultDesc = "PhobosVehicleSystems_RequestServiceDesc".Translate(),
                icon = SystemIcons.Service,
                isActive = () => serviceRequested,
                toggleAction = () => SetServiceRequested(!serviceRequested),
            };
        }
    }

    /// <summary>Everything the comp holds, for the dev action that logs it.</summary>
    public string DebugDescription()
    {
        EnsureStates();
        var text = new StringBuilder();
        text.AppendLine(Vehicle.LabelShort + " (" + Vehicle.def.defName + "), loadout " + Props.loadout?.defName + ", last update " + lastUpdateTick
                        + ", engine " + (EngineRunning ? "running" : "off") + ", load " + EngineLoad + ", moving " + Moving + ", ambient " + Ambient.ToString("0.0")
                        + ", loads " + loadAmps + " A, shore power " + ShorePowered());
        foreach (KeyValuePair<string, List<string>> pair in Props.roleKeys)
        {
            text.AppendLine("  role " + pair.Key + ": " + string.Join(", ", pair.Value) + " (efficiency " + PartEfficiency(pair.Key, -1f).ToString("0.00") + ")");
        }
        if (states != null)
        {
            foreach (SystemState state in states)
            {
                text.AppendLine("  " + state.Def.defName + (Enabled(state) ? "" : " (switched off)") + (Due(state) ? ", service due" : ""));
                for (int i = 0; i < state.values.Length; i++)
                {
                    text.AppendLine("    " + state.Def.quantities[i].name + " = " + state.values[i].ToString("0.###") + " stage " + (state.CurrentStage(i)?.label ?? "-"));
                }
            }
        }
        if (conditions != null)
        {
            foreach (VehicleCondition condition in conditions.All)
            {
                text.AppendLine("  condition " + condition.def!.defName + " on " + condition.partKey + " (" + condition.role + ", from " + condition.system
                                + ") severity " + condition.severity.ToString("0.###") + " stage " + (condition.CurrentStage?.label ?? "-"));
            }
        }
        foreach (KeyValuePair<VehicleStatDef, float> pair in statFactors) text.AppendLine("  factor " + pair.Key.defName + " x" + pair.Value.ToString("0.###"));
        return text.ToString().TrimEnd();
    }
}

/// <summary>Each quantity's stage bounds as arrays, built once.</summary>
sealed class StageBounds
{
    static readonly Dictionary<QuantitySpec, StageBounds> cache = new();
    public readonly double[] from;
    public readonly double[] to;

    StageBounds(QuantitySpec quantity)
    {
        from = quantity.stages.Select(s => (double)s.from).ToArray();
        to = quantity.stages.Select(s => (double)s.to).ToArray();
    }

    public static StageBounds Of(QuantitySpec quantity)
    {
        if (!cache.TryGetValue(quantity, out StageBounds? bounds)) cache[quantity] = bounds = new StageBounds(quantity);
        return bounds;
    }
}
