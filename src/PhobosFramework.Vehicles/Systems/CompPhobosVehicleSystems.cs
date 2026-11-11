using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Phobos.Framework;
using Phobos.Framework.Debug;
using Phobos.Framework.Items;
using Phobos.Framework.Rules;
using Phobos.Framework.Sounds;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
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
    /// <summary>How many parts the design had before the service appended any; the uninstall sweep cuts saved vehicles back to it.</summary>
    public int originalPartCount;
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
public class CompPhobosVehicleSystems : VehicleComp
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

    /// <summary>What the hydraulic fluid can give a vehicle's hydraulic work, 0 to 1; 1 for a vehicle with no hydraulics (T1, Framework 0.73.0).</summary>
    public float HydraulicSupply => TryGetQuantity("hydraulicSupply", out float supply) ? supply : 1f;

    /// <summary>Why hydraulic work cannot go on now, or null: too little fluid left to work it.</summary>
    public string? HydraulicRefusal() => HydraulicSupply < Rules.hydraulicWorkFloor ? "PhobosVehicleSystems_HydraulicsDry".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve() : null;

    VehicleDiagnostics? diagnostics;

    /// <summary>A diagnosis at a tuning rig: asked for, worked by a mechanic, reported in a letter (Framework 0.73.0).</summary>
    public VehicleDiagnostics Diagnostics => diagnostics ??= new VehicleDiagnostics(this);

    /// <summary>The conditions on this vehicle's parts (slice 2 of the vehicle health design).</summary>
    public VehicleConditions Conditions => conditions ??= new VehicleConditions(this);

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

    /// <summary>The share of its speed the vehicle may use this tick (the path follower's patch reads it).</summary>
    public float SpeedCap => Usable ? Powertrain?.SpeedCap ?? 1f : 1f;
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

    /// <summary>What a job's work is multiplied by: less on a vehicle standing in its own bay.</summary>
    public float BayWorkFactor => homeBay != null && homeBay.Holds(Vehicle) ? homeBay.Props.workFactor : 1f;

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
    public float EngineLoad { get; private set; }
    public float Ambient { get; private set; } = 21f;
    float loadAmps;
    bool? shorePowered;

    public CompProperties_PhobosVehicleSystems Props => (CompProperties_PhobosVehicleSystems)props;

    /// <summary>Whether the service runs for this vehicle: it has systems and the player has not swept them out.</summary>
    public bool Usable => Vehicle != null && Props.systems.Count > 0 && !VehicleSystems.Swept;

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

    bool Enabled(SystemState state) => !VehicleSystemsMod.Settings.disabledSystems.Contains(state.Def.defName) && (!state.Def.needsClosedCab || ClosedCab);

    /// <summary>Whether a system runs on this vehicle now (not switched off in the settings, and its cab is closed if it needs one).</summary>
    public bool IsEnabled(SystemState state) => Enabled(state);

    /// <summary>Counts up whenever something the Health tab shows may have changed (a hit, a repair, an update), so the tab rebuilds
    /// its rows then and only then. Derived, never saved.</summary>
    public int HealthViewVersion { get; private set; }

    /// <summary>The last day of each shown quantity, for the Systems tab's graphs.</summary>
    public QuantityHistory History { get; } = new();

    bool? closedCab;

    /// <summary>Whether this vehicle's cab shelters everyone aboard: the vehicle's own extension, else its loadout's cabin block.</summary>
    public bool ClosedCab
    {
        get
        {
            closedCab ??= Vehicle?.VehicleDef?.GetModExtension<PhobosVehicleCabinExtension>()?.closed ?? Props.loadout?.cabin.closed ?? true;
            return closedCab.Value;
        }
    }

    /// <summary>Why the vehicle will not move now, cached on each update for the caravan's inspect text and Vehicle Framework's
    /// world-map checks; null when it may.</summary>
    public string? MoveRefusal { get; private set; }

    /// <summary>Whether a load of the loadout is on: switched on by the player when it has a switch, and its own condition holds.</summary>
    public bool SwitchOn(string name)
    {
        PhobosVehicleLoadoutDef? loadout = Props.loadout;
        ElectricalLoadSpec? load = loadout?.loads.FirstOrDefault(l => l.name == name);
        return load != null && IsOn(load);
    }

    /// <summary>The least a system's parameter may be, set by what is fitted (a large battery). Derived, never saved.</summary>
    public void SetParameterFloor(string name, float floor) => (ParameterFloors ??= new Dictionary<string, float>())[name] = floor;

    internal Dictionary<string, float>? ParameterFloors { get; private set; }

    public bool LoadActive(string name)
    {
        PhobosVehicleLoadoutDef? loadout = Props.loadout;
        if (loadout == null || states == null || states.Count == 0) return false;
        foreach (ElectricalLoadSpec load in loadout.loads)
        {
            if (load.name != name) continue;
            if (load.toggle && !IsOn(load)) return false;
            var context = new SystemContext(this, states[0]);
            if (load.available != null && !load.available.Holds(context)) return false;
            return load.when == null || load.when.Holds(context);
        }
        return false;
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

    // ---- Saving ----

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
        Scribe_Collections.Look(ref loadSwitches, "PhobosVehicleSystems_Loads", LookMode.Value, LookMode.Value);
        // The graphs' history: the owner's exception to "derived state is never saved" (8 Nov 2026), a few kilobytes per vehicle.
        History.ExposeData(Props.systems);
        Scribe_Collections.Look(ref savedPartHealth, "PhobosVehicleSystems_PartHealth", LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref servicedSaved, "PhobosVehicleSystems_Serviced", LookMode.Value);
        Scribe_Collections.Look(ref pipeUnderlays, "PhobosVehicleSystems_PipeUnderlays", LookMode.Value, LookMode.Deep);
        pipeUnderlays ??= new Dictionary<string, Phobos.Framework.Persistence.PipeUnderlay>();
        Scribe_Values.Look(ref lastUpdateTick, "PhobosVehicleSystems_LastUpdate", -1);
        Scribe_Values.Look(ref serviceRequested, "PhobosVehicleSystems_ServiceRequested", false);
        Conditions.ExposeData();
        Repairs.ExposeData();
        Fittings.ExposeData();
        Diagnostics.ExposeData();
        Events.ExposeData();
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
        RestorePartHealths();
        if (Usable && !respawningAfterLoad) Fittings.RollEnemySet();
        powertrain?.ForgetGround();
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        // A gravship lifts its pipes along with it.
        if (mode != DestroyMode.WillReplace) VehiclePipes.Leave(this);
        VehicleSystemSounds.EndAll(this);
        engineSound?.End();
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
        if (train.Full && EngineRunning && !WorkTurningEngine) EngineLoad = Mathf.Min(1f, (float)train.Result.Load + AccessoryShare);
    }

    /// <summary>The engine sound that follows rpm and load, for a vehicle whose Def names one (derived, never saved).</summary>
    EngineSound? engineSound;

    /// <summary>Every tick, only for a vehicle with an engine sound of ours: a few assignments that keep its pitch and blend gliding.</summary>
    public override void CompTick()
    {
        base.CompTick();
        engineSound?.Tick(this);
    }

    public override void CompTickRare()
    {
        base.CompTickRare();
        VehicleCostMeasure.CheckDone();
        RefreshBay();
        DetailTier was = Tier;
        Tier = ReadTier();
        // A vehicle that has just become the player's (captured) starts its clock now: the hours it spent as another faction's,
        // when nothing evolved, are not caught up at once. From here it runs on the player's own settings.
        if (Tier == DetailTier.Full && was != DetailTier.Full && Current.Game != null) lastUpdateTick = Find.TickManager.TicksGame;
        switch (Tier)
        {
            case DetailTier.Full:
                using (PhobosTiming.Measure("vehicle rare tick / full")) UpdateNow();
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
                }
                break;
        }
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
            float health = (float)PartHealthRule.Restored(saved, saveHeldParts, part.Health, part.MaxHealth);
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
    public void UpdateNow()
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
            if (hours <= 0) return;
            lastUpdateTick = now;
            SweepPartHealth();
            RefreshCaches();
            Powertrain?.ApplyFuel(hours);
            if (EngineRunning) ChargeCarriedPacks(hours);
            foreach (SystemState state in states)
            {
                if (Enabled(state)) state.Def.Worker.Advance(new SystemContext(this, state), hours);
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
            MoveRefusal = FirstRefusal(draft: false);
            HealthViewVersion++;
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
        bool caravanMoving = caravan != null && caravan.vehiclePather != null && caravan.vehiclePather.MovingNow;
        bool drafted = vehicle.Spawned && vehicle.Drafted;
        bool movingOnMap = vehicle.Spawned && vehicle.vehiclePather != null && vehicle.vehiclePather.Moving;
        Moving = movingOnMap || caravanMoving || flying;
        EngineRunning = hasFuel && (drafted || caravanMoving || flying);
        EngineRunningRaw = EngineRunning;
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
        // A set-up vehicle's work turns the engine with nobody driving. EngineRunningRaw stays as it was, so the drivetrain's fuel sum adds nothing: the work burns its own.
        engineDemands ??= vehicle.AllComps.OfType<IVehicleEngineDemand>().ToList();
        WorkTurningEngine = false;
        foreach (IVehicleEngineDemand demand in engineDemands)
        {
            if (!demand.WantsEngine || !hasFuel) continue;
            EngineRunning = true;
            WorkTurningEngine = true;
            EngineLoad = Mathf.Max(EngineLoad, demand.EngineDemand);
        }
        shorePowered = null;
        loadAmps = 0f;
        if (states != null && states.Count > 0) loadAmps = SumLoads(new SystemContext(this, states[0]));
        // Crosslink 5: the alternator takes shaft power for what the loads draw, so the engine works a little harder for them.
        AccessoryShare = AccessoryLoad();
        if (AccessoryShare > 0f) EngineLoad = Mathf.Min(1f, EngineLoad + AccessoryShare);
    }

    /// <summary>The share of the engine's rated power the switched-on loads take through the alternator now (crosslink 5): 0 with the
    /// player's setting off, the engine stopped, no rated power known, or an electric vehicle (its loads draw on the traction battery).
    /// Derived on each update, never saved.</summary>
    public float AccessoryShare { get; private set; }

    float AccessoryLoad()
    {
        if (!VehicleSystemsMod.Settings.accessoriesLoadEngine || !EngineRunning || loadAmps <= 0f) return 0f;
        if (Vehicle.CompFueledTravel?.Props.electricPowered == true) return 0f;
        double rated = Powertrain?.Spec.RatedPowerKw ?? 0;
        return (float)AccessoryRules.EngineShare(loadAmps, ElectricalVolts(), rated);
    }

    /// <summary>The electrical system's voltage: its <c>systemVolts</c> figure (12, or 24 on heavy vehicles), 12 when no system has one.</summary>
    float ElectricalVolts()
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
        PhobosVehicleLoadoutDef? loadout = Props.loadout;
        if (loadout == null) return 0f;
        foreach (ElectricalLoadSpec load in loadout.loads)
        {
            if (load.toggle && !IsOn(load)) continue;
            if (load.available != null && !load.available.Holds(context)) continue;
            if (load.when != null && !load.when.Holds(context)) continue;
            amps += load.amps;
        }
        return amps;
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
                    if (entered.notable && state.staged) RecordEvent(VehicleEventKind.Stage, state.Def.defName, state.Def.quantities[q].name, index);
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
        if (Tier != DetailTier.Full) return;
        if (!Usable) return;
        if (eventDef == VehicleEventDefOf.MoveStart) VehiclePipes.Leave(this);
        else if (eventDef == VehicleEventDefOf.MoveStop) VehiclePipes.Update(this);
        UpdateNow();
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
        Vehicle.statHandler?.MarkAllDirty();
        Repairs.Invalidate();
        HealthViewVersion++;
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
                if (!PartPlays(part, rule.role) || !rule.Matches(damage)) continue;
                float amount = rule.Amount(healthLost, part.MaxHealth);
                if (!string.IsNullOrEmpty(rule.quantity)) AddToQuantity(rule.quantity, amount);
                if (rule.condition != null)
                {
                    Conditions.Add(rule.condition, part, rule.role, state.Def.defName, (float)FittingRule.FaultSeverity(System.Math.Max(0f, amount), Fittings.FaultFactor(part.props.key)));
                    conditionGiven = true;
                }
            }
        }
        if (conditionGiven) AfterConditionsChanged();
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

    /// <summary>Gives a part of the role the condition, or adds to it where a part already has it (Effect_AddCondition).</summary>
    public void AddCondition(string role, PhobosVehicleConditionDef def, float amount, string system)
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
        if (chosen != null) Conditions.Add(def, chosen, role, system, amount);
    }

    /// <summary>Uses up so many of a thing from the vehicle's cargo.</summary>
    internal void TakeFromCargo(ThingDef def, int count)
    {
        ThingOwner? cargo = Vehicle.inventory?.innerContainer;
        if (cargo == null) return;
        int remaining = count;
        foreach (Thing thing in cargo.Where(t => t.def == def).ToList())
        {
            if (remaining <= 0) break;
            int take = Mathf.Min(remaining, thing.stackCount);
            thing.SplitOff(take).Destroy();
            remaining -= take;
        }
    }

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
        HealthViewVersion++;
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

    IEnumerable<VehicleComponent> PartsOf(string role)
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

    /// <summary>Slow damage to one living part of the role, without armour or reactions (wear, heat).</summary>
    public void WearRole(string role, float amount, float shareOfHealth = 0f)
    {
        if (amount <= 0f && shareOfHealth <= 0f) return;
        VehicleComponent? part = PartsOf(role).Where(p => p.Health > 0f).RandomElementWithFallback();
        if (part == null) return;
        part.SetHealth(part.Health - amount - shareOfHealth * part.MaxHealth);
        RememberPartHealth(part);
        Vehicle.statHandler.MarkAllDirty();
        Repairs.NotifyRepairable();    // Vehicle Framework's list of vehicles to repair is fed only by real hits
    }

    /// <summary>A sudden failure: one living part of the role loses a share of its full health. Returns the part, or null.</summary>
    public VehicleComponent? BreakRole(string role, float fraction)
    {
        VehicleComponent? part = PartsOf(role).Where(p => p.Health > 0f).RandomElementWithFallback();
        if (part == null) return null;
        part.SetHealth(part.Health - fraction * part.MaxHealth);
        RememberPartHealth(part);
        Vehicle.statHandler.MarkAllDirty();
        Repairs.NotifyRepairable();
        return part;
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

    /// <summary>Whether the parked vehicle stands beside a building on a grid with power to give. Read at most once per update.</summary>
    public bool ShorePowered()
    {
        if (shorePowered.HasValue) return shorePowered.Value;
        shorePowered = false;
        VehiclePawn vehicle = Vehicle;
        if (!vehicle.Spawned || Moving) return false;
        Map map = vehicle.Map;
        foreach (IntVec3 cell in vehicle.OccupiedRect().ExpandedBy(Rules.shorePowerReach))
        {
            if (!cell.InBounds(map)) continue;
            foreach (Thing thing in cell.GetThingList(map))
            {
                PowerNet? net = (thing as ThingWithComps)?.GetComp<CompPower>()?.PowerNet;
                if (net != null && (net.CurrentStoredEnergy() > 1f || net.CurrentEnergyGainRate() > 0f))
                {
                    shorePowered = true;
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>Starts or stops the engine, saying which, or why it would not start. The one switch Vehicle Framework has for this is the draft.</summary>
    public void SetIgnition(bool on)
    {
        if (on)
        {
            AcceptanceReport report = Vehicle.CanDraft();
            if (!report.Accepted)
            {
                VehicleSystemSounds.PlayOnce(this, RefusedStartSound());
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

    /// <summary>The player's, standing on a map and not moving.</summary>
    public bool ParkedForPipes => Vehicle.Spawned && Vehicle.Faction == Faction.OfPlayer && Vehicle.vehiclePather is not { Moving: true };

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

    /// <summary>What turning the key sounds like now: the first refusing stage's sound, for this engine's fuel; null if none says.</summary>
    SoundDef? RefusedStartSound()
    {
        if (states == null || Vehicle.Faction != Faction.OfPlayer) return null;
        foreach ((SystemContext context, StageSpec stage, string _) in StagesInForce())
        {
            foreach (VehicleSystemEffect effect in stage.effects)
            {
                if (effect is not Effect_RefuseDraft refusal || !refusal.Active(context) || refusal.DraftRefusal(context) == null) continue;
                SoundDef? sound = VehicleSystemSounds.ForEngine(this, refusal.sound, refusal.soundPetrol);
                if (sound != null) return sound;
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

    /// <summary>The factor the current stages put on a stat; read by the stat part on every Vehicle Framework stat.</summary>
    public float StatFactor(VehicleStatDef stat) => (statFactors.TryGetValue(stat, out float factor) ? factor : 1f) * Fittings.StatFactor(stat);

    /// <summary>How fast an electric vehicle charges from the grid now, against Vehicle Framework's own rate (derived, never saved).</summary>
    public float ChargeFactor { get; private set; } = 1f;

    /// <summary>The stage labels behind a stat's factor, for the stat card's explanation.</summary>
    public IEnumerable<string> StatFactorReasons(VehicleStatDef stat)
    {
        foreach ((SystemContext context, StageSpec stage, string owner) in StagesInForce())
        {
            foreach (VehicleSystemEffect effect in stage.effects)
            {
                if (effect.Active(context) && effect.StatFactor(stat) != 1f)
                {
                    yield return owner + ": " + stage.label + " x" + effect.StatFactor(stat).ToStringByStyle(ToStringStyle.FloatTwo);
                }
            }
        }
        foreach (string reason in Fittings.StatFactorReasons(stat)) yield return reason;
    }

    void RecacheStatFactors()
    {
        var fresh = new Dictionary<VehicleStatDef, float>();
        float charge = 1f;
        foreach ((SystemContext context, StageSpec stage, string _) in StagesInForce())
        {
            foreach (VehicleSystemEffect effect in stage.effects)
            {
                if (!effect.Active(context)) continue;
                charge *= effect.ChargeFactor;
                foreach (VehicleStatDef stat in DefDatabase<VehicleStatDef>.AllDefsListForReading)
                {
                    float factor = effect.StatFactor(stat);
                    if (factor != 1f) fresh[stat] = (fresh.TryGetValue(stat, out float before) ? before : 1f) * factor;
                }
            }
        }
        ChargeFactor = charge;
        var changed = new HashSet<VehicleStatDef>(fresh.Keys);
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

    public bool IsOn(ElectricalLoadSpec load) => loadSwitches.TryGetValue(load.name, out bool on) ? on : load.defaultOn;

    public void SetSwitch(ElectricalLoadSpec load, bool on)
    {
        loadSwitches[load.name] = on;
        if (states != null && states.Count > 0) loadAmps = SumLoads(new SystemContext(this, states[0]));
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
            if (TryGetQuantity(rule.quantity, out float value) && ThresholdRule.Passes(value, rule.above, rule.below)) return true;
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

    internal int CountInCargo(ThingDef def)
    {
        ThingOwner? cargo = Vehicle.inventory?.innerContainer;
        return cargo == null ? 0 : cargo.TotalStackCountOfDef(def);
    }

    /// <summary>Everything every due service still lacks in the vehicle's cargo, added up by kind (the core's SupplyRules), so the
    /// haulers fetch it all and the vehicle says it all.</summary>
    public List<(ThingDef def, int count)> MissingSupplies() =>
        SupplyRules.Shortfall(DueServices().Where(s => s.Def.service != null)
            .Select(s => s.Def.service!.SuppliesNow.Select(supply => (supply.thingDef, supply.count))).Concat(Repairs.UnmetPayments()), CountInCargo);

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

    /// <summary>The first due service whose own supplies are all aboard and whose water can be had, or null: a mechanic need not wait
    /// for another service's supplies.</summary>
    public SystemState? ReadyService()
    {
        foreach (SystemState state in DueServices())
        {
            if (state.Def.service != null && SuppliesAboard(state) && WaterReady(state, out _)) return state;
        }
        return null;
    }

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
        foreach (QuantityRestore restore in service.restores) SetQuantity(restore.quantity, restore.value);
        Conditions.ClearBy(service);
        servicedThisRequest.Add(state.Def.defName);
        RecordEvent(VehicleEventKind.Service, state.Def.defName);
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
        int index = JumpQuantity(need);
        double missing = spec.raiseTo - need.values[index];
        need.values[index] = SystemIntegrator.Clamp(spec.raiseTo, need.min[index], need.max[index]);
        need.Def.Worker.UpdateDerived(new SystemContext(this, need));
        if (donor != null && donorState != null)
        {
            double capacity = new SystemContext(this, need).Parameter(spec.capacityParameter);
            double donorCapacity = new SystemContext(donor, donorState).Parameter(spec.capacityParameter);
            if (!donor.EngineRunning && donorCapacity > 0) donor.AddToQuantity(spec.quantity, -(float)(missing * capacity / donorCapacity));
            donor.EvaluateStages(donorState, 0f);
        }
        EvaluateStages(need, 0f);
        RecordEvent(VehicleEventKind.JumpStart, need.Def.defName);
        RecacheStatFactors();
        MoveRefusal = FirstRefusal(draft: false);
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

    public override string? CompInspectStringExtra()
    {
        if (!Usable) return null;
        EnsureStates();
        return InspectText.For(this);
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (!Usable || Vehicle.Faction != Faction.OfPlayer) yield break;
        if (homeBay is { parent.Destroyed: false }) yield return BayGizmos.GoButton(this, homeBay);
        PhobosVehicleLoadoutDef? loadout = Props.loadout;
        if (loadout != null)
        {
            foreach (ElectricalLoadSpec load in loadout.loads)
            {
                if (!load.toggle || !LoadAvailable(load)) continue;
                ElectricalLoadSpec captured = load;
                yield return new Command_Toggle
                {
                    defaultLabel = captured.label,
                    defaultDesc = captured.description + "\n\n" + "PhobosVehicleSystems_LoadDraw".Translate(captured.amps.ToString("0").Named("AMPS")),
                    icon = SystemIcons.For(captured.iconPath),
                    isActive = () => IsOn(captured),
                    toggleAction = () => SetSwitch(captured, !IsOn(captured)),
                }.AsPhysicalSwitch();
            }
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
