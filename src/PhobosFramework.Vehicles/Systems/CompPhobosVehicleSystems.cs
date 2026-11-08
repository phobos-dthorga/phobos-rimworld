using System.Collections.Generic;
using System.Linq;
using System.Text;
using Phobos.Framework;
using Phobos.Framework.Items;
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

    /// <summary>Spawned vehicles with this comp, for the per-tick speed cap (one dictionary read instead of a comp search).</summary>
    static readonly Dictionary<VehiclePawn, CompPhobosVehicleSystems> spawned = new();

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

    /// <summary>Engine running by Vehicle Framework's own signs (drafted, caravan moving, in flight), before any flight phase.</summary>
    public bool EngineRunningRaw { get; private set; }

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
    public bool LoadActive(string name)
    {
        PhobosVehicleLoadoutDef? loadout = Props.loadout;
        if (loadout == null || states == null || states.Count == 0) return false;
        foreach (ElectricalLoadSpec load in loadout.loads)
        {
            if (load.name != name) continue;
            if (load.toggle && !IsOn(load)) return false;
            return load.when == null || load.when.Holds(new SystemContext(this, states[0]));
        }
        return false;
    }

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
        // The graphs' history: the owner's exception to "derived state is never saved" (9 Nov 2026), a few kilobytes per vehicle.
        History.ExposeData(Props.systems);
        Scribe_Collections.Look(ref savedPartHealth, "PhobosVehicleSystems_PartHealth", LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref servicedSaved, "PhobosVehicleSystems_Serviced", LookMode.Value);
        Scribe_Values.Look(ref lastUpdateTick, "PhobosVehicleSystems_LastUpdate", -1);
        Scribe_Values.Look(ref serviceRequested, "PhobosVehicleSystems_ServiceRequested", false);
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
        if (Usable) spawned[Vehicle] = this;
        RestorePartHealths();
        powertrain?.ForgetGround();
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
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
        if (train == null || !Usable) return;
        bool active = train.IsAircraft ? Vehicle.Drafted || train.AircraftRunning : Vehicle.Spawned && Vehicle.vehiclePather != null && Vehicle.vehiclePather.Moving;
        if (!active && train.Cap >= 1f && train.Result.Rpm <= 0) return;
        train.Sample();
    }

    public override void CompTickRare()
    {
        base.CompTickRare();
        UpdateNow();
    }

    /// <summary>Gives the appended parts back the healths saved by key, in case their order moved since the save.</summary>
    void RestorePartHealths()
    {
        if (healthsRestored || Vehicle?.statHandler == null) return;
        healthsRestored = true;
        if (savedPartHealth.Count == 0) return;
        foreach (KeyValuePair<string, float> pair in savedPartHealth)
        {
            VehicleComponent? part = Vehicle.statHandler.GetComponent(pair.Key);
            if (part != null && Mathf.Abs(part.Health - pair.Value) > 0.01f) part.SetHealth(pair.Value);
            if (part != null) RememberPartHealth(part);
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
                EvaluateStages(state, (float)hours);
            }
            RecacheStatFactors();
            History.Sample(states, now);
            MoveRefusal = FirstRefusal(draft: false);
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
        shorePowered = null;
        loadAmps = 0f;
        if (states != null && states.Count > 0) loadAmps = SumLoads(new SystemContext(this, states[0]));
    }

    float SumLoads(SystemContext context)
    {
        float amps = 0f;
        PhobosVehicleLoadoutDef? loadout = Props.loadout;
        if (loadout == null) return 0f;
        foreach (ElectricalLoadSpec load in loadout.loads)
        {
            if (load.toggle && !IsOn(load)) continue;
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
    }

    static string StageKey(SystemState state, int quantity) => state.Def.defName + "/" + state.Def.quantities[quantity].name + "/" + state.stage[quantity];

    void OnVehicleEvent(VehicleEventDef eventDef)
    {
        if (!Usable) return;
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
    public void Notify_PartSeen(string role, VehicleComponent part)
    {
        if (!Usable || part?.props?.key == null) return;
        string key = part.props.key;
        float health = part.Health;
        if (!lastPartHealth.TryGetValue(key, out float last))
        {
            lastPartHealth[key] = health;
            return;
        }
        lastPartHealth[key] = health;
        float lost = last - health;
        if (lost <= 0f) return;
        Notify_PartHit(role, lost);
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

    /// <summary>Health a bound part lost to a hit: each part-hit rule of the role moves its quantity by that much.</summary>
    void Notify_PartHit(string role, float healthLost)
    {
        if (!Usable || healthLost <= 0f) return;
        UpdateNow();
        if (states == null) return;
        foreach (SystemState state in states)
        {
            if (!Enabled(state)) continue;
            foreach (PartHitRule rule in state.Def.onPartHit)
            {
                if (rule.role == role) AddToQuantity(rule.quantity, rule.perHealthLost * healthLost);
            }
        }
    }

    // ---- Quantities ----

    public bool TryGetQuantity(string name, out float value)
    {
        if (states != null)
        {
            foreach (SystemState state in states)
            {
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

    /// <summary>Slow damage to one living part of the role, without armour or reactions (wear, heat).</summary>
    public void WearRole(string role, float amount)
    {
        if (amount <= 0f) return;
        VehicleComponent? part = PartsOf(role).Where(p => p.Health > 0f).RandomElementWithFallback();
        if (part == null) return;
        part.SetHealth(part.Health - amount);
        RememberPartHealth(part);
        Vehicle.statHandler.MarkAllDirty();
    }

    /// <summary>A sudden failure: one living part of the role loses a share of its full health. Returns the part, or null.</summary>
    public VehicleComponent? BreakRole(string role, float fraction)
    {
        VehicleComponent? part = PartsOf(role).Where(p => p.Health > 0f).RandomElementWithFallback();
        if (part == null) return null;
        part.SetHealth(part.Health - fraction * part.MaxHealth);
        RememberPartHealth(part);
        Vehicle.statHandler.MarkAllDirty();
        return part;
    }

    // ---- What the game asks ----

    public float ActiveLoadAmps() => loadAmps;

    /// <summary>Engine speed in rpm: the model's when the vehicle has a drivetrain or turbine, else idle or a cruise figure.</summary>
    public float EngineRpm(float idle, float cruise)
    {
        Powertrain? train = Powertrain;
        if (train != null && VehicleSystemsMod.Settings.drivetrainMode != DrivetrainMode.Off && EngineRunning) return (float)train.Result.Rpm;
        return EngineRunning ? (Moving ? cruise : idle) : 0f;
    }

    /// <summary>Engine heat as a share of full-load heat: the model's when the player chose Full, else the old load figure.</summary>
    public float EngineHeatShare()
    {
        Powertrain? train = Powertrain;
        if (train != null && train.Full && train.Ground) return EngineRunning ? (float)train.HeatShare : 0f;
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

    string? FirstRefusal(bool draft)
    {
        // Only the player's vehicles refuse: another faction's raid or caravan must never be left stranded by a flat battery.
        if (states == null || Vehicle.Faction != Faction.OfPlayer) return null;
        foreach (SystemState state in states)
        {
            if (!Enabled(state)) continue;
            var context = new SystemContext(this, state);
            for (int q = 0; q < state.values.Length; q++)
            {
                StageSpec? stage = state.CurrentStage(q);
                if (stage == null) continue;
                foreach (VehicleSystemEffect effect in stage.effects)
                {
                    if (!effect.Active(context)) continue;
                    string? reason = draft ? effect.DraftRefusal(context) : effect.MoveRefusal(context);
                    if (reason != null) return reason;
                }
            }
        }
        return null;
    }

    /// <summary>The factor the current stages put on a stat; read by the stat part on every Vehicle Framework stat.</summary>
    public float StatFactor(VehicleStatDef stat) => statFactors.TryGetValue(stat, out float factor) ? factor : 1f;

    /// <summary>The stage labels behind a stat's factor, for the stat card's explanation.</summary>
    public IEnumerable<string> StatFactorReasons(VehicleStatDef stat)
    {
        if (states == null) yield break;
        foreach (SystemState state in states)
        {
            if (!Enabled(state)) continue;
            var context = new SystemContext(this, state);
            for (int q = 0; q < state.values.Length; q++)
            {
                StageSpec? stage = state.CurrentStage(q);
                if (stage == null) continue;
                foreach (VehicleSystemEffect effect in stage.effects)
                {
                    if (effect.Active(context) && effect.StatFactor(stat) != 1f)
                    {
                        yield return state.Def.quantities[q].label + ": " + stage.label + " x" + effect.StatFactor(stat).ToStringByStyle(ToStringStyle.FloatTwo);
                    }
                }
            }
        }
    }

    void RecacheStatFactors()
    {
        var fresh = new Dictionary<VehicleStatDef, float>();
        if (states != null)
        {
            foreach (SystemState state in states)
            {
                if (!Enabled(state)) continue;
                var context = new SystemContext(this, state);
                for (int q = 0; q < state.values.Length; q++)
                {
                    StageSpec? stage = state.CurrentStage(q);
                    if (stage == null) continue;
                    foreach (VehicleSystemEffect effect in stage.effects)
                    {
                        if (!effect.Active(context)) continue;
                        foreach (VehicleStatDef stat in DefDatabase<VehicleStatDef>.AllDefsListForReading)
                        {
                            float factor = effect.StatFactor(stat);
                            if (factor != 1f) fresh[stat] = (fresh.TryGetValue(stat, out float before) ? before : 1f) * factor;
                        }
                    }
                }
            }
        }
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
        int index = state.Def.QuantityIndex(service.dueQuantity);
        if (index < 0) return false;
        double value = state.values[index];
        return (!float.IsNaN(service.dueAbove) && value >= service.dueAbove) || (!float.IsNaN(service.dueBelow) && value <= service.dueBelow);
    }

    /// <summary>The first system whose service is due, or null.</summary>
    public SystemState? DueService()
    {
        if (!Usable) return null;
        EnsureStates();
        if (states == null) return null;
        foreach (SystemState state in states)
        {
            if (Due(state)) return state;
        }
        return null;
    }

    public IEnumerable<ServiceSpec> AllServices() => Props.systems.Select(s => s.def.service).OfType<ServiceSpec>();

    int CountInCargo(ThingDef def)
    {
        ThingOwner? cargo = Vehicle.inventory?.innerContainer;
        return cargo == null ? 0 : cargo.TotalStackCountOfDef(def);
    }

    /// <summary>The first supply the due service still lacks in the vehicle's cargo.</summary>
    public bool MissingSupply(out ThingDef? def, out int count)
    {
        def = null;
        count = 0;
        SystemState? state = DueService();
        if (state?.Def.service == null) return false;
        foreach (ThingDefCountClass supply in state.Def.service.supplies)
        {
            int short_ = supply.count - CountInCargo(supply.thingDef);
            if (short_ > 0)
            {
                def = supply.thingDef;
                count = short_;
                return true;
            }
        }
        return false;
    }

    /// <summary>Whether the due service's water can be had now (always, unless a water source such as plumbing is required and none
    /// is beside the vehicle).</summary>
    public bool WaterReady(out string? reason)
    {
        reason = null;
        SystemState? state = DueService();
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

    /// <summary>The due service whose supplies are all aboard and whose water can be had, or null.</summary>
    public SystemState? ReadyService()
    {
        SystemState? state = DueService();
        if (state?.Def.service == null || MissingSupply(out _, out _) || !WaterReady(out _)) return null;
        return state;
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
        foreach (ThingDefCountClass supply in service.supplies)
        {
            int remaining = supply.count;
            foreach (Thing thing in cargo.Where(t => t.def == supply.thingDef).ToList())
            {
                if (remaining <= 0) break;
                int take = Mathf.Min(remaining, thing.stackCount);
                thing.SplitOff(take).Destroy();
                remaining -= take;
            }
        }
        foreach (QuantityRestore restore in service.restores) SetQuantity(restore.quantity, restore.value);
        servicedThisRequest.Add(state.Def.defName);
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
        statFactors.Clear();
        serviceRequested = false;
        servicedThisRequest.Clear();
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
        PhobosVehicleLoadoutDef? loadout = Props.loadout;
        if (loadout != null)
        {
            foreach (ElectricalLoadSpec load in loadout.loads)
            {
                if (!load.toggle) continue;
                ElectricalLoadSpec captured = load;
                yield return new Command_Toggle
                {
                    defaultLabel = captured.label,
                    defaultDesc = captured.description + "\n\n" + "PhobosVehicleSystems_LoadDraw".Translate(captured.amps.ToString("0").Named("AMPS")),
                    icon = SystemIcons.For(captured.iconPath),
                    isActive = () => IsOn(captured),
                    toggleAction = () => SetSwitch(captured, !IsOn(captured)),
                };
            }
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
