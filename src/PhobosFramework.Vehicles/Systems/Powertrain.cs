using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;
using Phobos.Framework.WorldMap;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>Whether the drivetrain runs, only shows its figures, or also feeds load, heat and the speed cap.</summary>
public enum DrivetrainMode
{
    Off,
    DisplayOnly,
    Full,
}

/// <summary>
/// One vehicle's drivetrain or turbine, as the designs describe (vehicle-drivetrain-design.md, vehicle-turbine-design.md): sampled
/// on Vehicle Framework's ScanShort (every 60 ticks) while the vehicle moves or flies, worked out by the game-free models, and read by
/// the systems (load, rpm, heat), the speed cap and the inspect line. Nothing here is saved: it is rebuilt from the vehicle after a
/// load.
/// </summary>
public sealed class Powertrain
{
    /// <summary>Metres per cell for speed (the action clock: 60 ticks are a second, a cell is a metre; map scale research).</summary>
    public const double MetresPerCell = 1.0;
    const int TicksPerSecond = 60;

    readonly CompPhobosVehicleSystems comp;
    public readonly PhobosVehicleDrivetrainDef? Def;
    public readonly PhobosVehicleFlightProfileDef? Profile;
    public readonly DrivetrainSpec Spec;
    public readonly TurbineSpec? Turbine;
    readonly TurbineSpec? dryTurbine;
    readonly double fullLoadHeatKw;
    readonly double nominalCruiseFuelKw;
    readonly double fuelKgFull;
    readonly double waterTankKg;
    readonly double waterKgPerSecond;
    readonly double transmissionLimit;
    readonly double fixedRolling;
    readonly double fixedGrip;
    public readonly double KerbKg;
    /// <summary>The roles that count as engines: Engine1, Engine2 and so on when a loadout binds them, else the one Engine.</summary>
    readonly string[] engineRoles;

    public DriveResult Result;
    public TurbineState TurbineNow;
    public string? Phase;
    public FlightPhaseSpec? PhaseSpec;
    public double Cap = 1;
    public bool HoverWet;
    double massKg = -1;
    int massTick = -1;
    Vector3 lastPosition;
    int lastTick = -1;
    double lastSpeed;
    IntVec3 groundCell = IntVec3.Invalid;
    Map? groundMap;
    double groundRolling = 0.012;
    double groundGrip = 0.8;

    Powertrain(CompPhobosVehicleSystems comp, PhobosVehicleDrivetrainDef? def, ParameterSet? overrides, PhobosVehicleFlightProfileDef? profile)
    {
        this.comp = comp;
        Def = def;
        Profile = profile;
        VehicleDef vehicleDef = comp.Vehicle.VehicleDef;
        float Get(string name, float fallback) => overrides != null && overrides.values.TryGetValue(name, out float v) ? v : fallback;
        MassXml mass = def?.mass ?? new MassXml();
        KerbKg = Tuning.Value(vehicleDef, Tuning.Drivetrain, Tuning.EmptyKilograms, KerbFor(vehicleDef, def, overrides));
        fuelKgFull = Get("fuelKilogramsFull", mass.fuelKilogramsFull);
        EngineXml engine = def?.engine ?? new EngineXml();
        // Fittings and tunes move only piston engines' figures (their Defs say so); a change builds the powertrain again.
        VehicleFittings fittings = comp.Fittings;
        Spec = new DrivetrainSpec
        {
            Engine = engine.kind,
            Transmission = def?.transmission.kind ?? TransmissionKind.None,
            IdleRpm = engine.idleRpm,
            LaunchRpm = engine.launchRpm,
            RatedRpm = engine.ratedRpm,
            RatedPowerKw = fittings.PowerFactor * (vehicleDef.GetModExtension<PhobosVehicleFiguresExtension>() is { } own && PhobosVehicleFiguresExtension.Given(own.ratedKilowatts)
                ? own.ratedKilowatts
                : Get("ratedKilowatts", engine.ratedKilowatts) is float rated && !float.IsNaN(rated) && rated > 0 ? rated
                : Get("kilowattsPerTonne", engine.kilowattsPerTonne) * KerbKg / 1000),
            FrictionConstant = engine.frictionConstant,
            FrictionLinear = engine.frictionLinear,
            FrictionQuadratic = engine.frictionQuadratic,
            IndicatedEfficiency = engine.indicatedEfficiency,
            CoolantHeatShare = engine.coolantHeatShare,
            IdleFuelShare = Get("idleFuelShare", engine.idleFuelShare),
            FinalDrive = (def?.transmission.finalDrive ?? 1) * fittings.FinalDriveFactor,
            UpshiftRpm = def?.transmission.upshiftRpm ?? 2400,
            DownshiftRpm = def?.transmission.downshiftRpm ?? 1300,
            Efficiency = Get("efficiency", def?.transmission.efficiency ?? 0.9f),
            WheelRadius = Get("wheelRadius", def?.running.wheelRadius ?? 0.38f),
            DrivenShare = Get("drivenShare", def?.running.drivenShare ?? 0.5f),
            DragArea = Get("dragArea", def?.running.dragArea ?? 2.4f),
            DesignSpeed = LoadoutSelector.BaseStat(vehicleDef, DefDatabase<VehicleStatDef>.GetNamedSilentFail("MoveSpeed")) * MetresPerCell,
            MaxTractiveEffortN = (def?.transmission.maxTractiveEffortKilonewtons ?? 0) * 1000,
            ContinuousTractiveEffortN = (def?.transmission.continuousTractiveEffortKilonewtons ?? 0) * 1000,
        };
        fixedRolling = def?.running.fixedRollingResistance ?? float.NaN;
        fixedGrip = def?.running.fixedGrip ?? float.NaN;
        engineRoles = Enumerable.Range(1, MaxEngines).Select(i => "Engine" + i).Where(role => comp.Props.roleKeys.ContainsKey(role)).ToArray();
        if (engineRoles.Length == 0) engineRoles = new[] { "Engine" };
        if (def != null && def.transmission.gears.Count > 0) Spec.Gears = def.transmission.gears.Select(g => (double)g).ToArray();
        if (engine.torqueCurve != null && engine.torqueCurve.Points.Count >= 2)
        {
            Spec.TorqueX = engine.torqueCurve.Points.Select(p => (double)p.x).ToArray();
            Spec.TorqueY = engine.torqueCurve.Points.Select(p => (double)p.y).ToArray();
        }
        fullLoadHeatKw = Math.Max(0.1, DrivetrainModel.FuelPowerKw(Spec, Spec.RatedPowerKw, Spec.RatedRpm) * Spec.CoolantHeatShare);
        if (Spec.Transmission != TransmissionKind.None)
        {
            var cruise = new DriveConditions
            {
                Speed = Spec.DesignSpeed, MassKg = KerbKg * 1.15, RollingResistance = 0.012, Grip = 0.8, EngineRunning = true,
                ClutchCapacity = 1, DrivenWheelsWorking = 1,
            };
            // Measured on the engine as built, or a tune's saving at a cruise would cancel itself out.
            double tunedPower = Spec.RatedPowerKw, tunedDrive = Spec.FinalDrive;
            Spec.RatedPowerKw = tunedPower / fittings.PowerFactor;
            Spec.FinalDrive = tunedDrive / fittings.FinalDriveFactor;
            nominalCruiseFuelKw = Math.Max(0.1, DrivetrainModel.Evaluate(Spec, cruise).FuelPowerKw);
            Spec.RatedPowerKw = tunedPower;
            Spec.FinalDrive = tunedDrive;
        }
        transmissionLimit = double.PositiveInfinity;
        if (engine.turbine is { } t)
        {
            Turbine = MakeTurbine(t, includeWet: true);
            dryTurbine = MakeTurbine(t, includeWet: false);
            waterTankKg = t.waterInjection?.tankKilograms ?? 0;
            waterKgPerSecond = t.waterInjection?.kilogramsPerSecond ?? 0;
            float limit = Get("transmissionLimit", t.transmissionLimit);
            if (!float.IsNaN(limit) && limit > 0) transmissionLimit = limit;
            float liftToWeight = Get("liftToWeightAtMaxGross", t.liftToWeightAtMaxGross);
            if (!float.IsNaN(liftToWeight) && liftToWeight > 0)
            {
                // Thrust-lifted craft: every rating's thrust follows from the authored lift-to-weight at maximum gross weight.
                double thrust = TurbineModel.ThrustForLiftToWeight(liftToWeight, MaxGrossKg());
                foreach (TurbineSpec turbine in new[] { Turbine, dryTurbine })
                {
                    double top = turbine.Top.Share;
                    foreach (TurbineRating rating in turbine.Ratings) rating.ThrustKn = thrust * rating.Share / Math.Max(0.01, top);
                }
            }
        }
    }

    /// <summary>The lightest kerb mass the per-Mass rule gives, kilograms (authored floor).</summary>
    public const double MinimumKerbKg = 50;

    /// <summary>
    /// A design's empty mass before the player's tuning, most specific first: the vehicle's own PhobosVehicleFiguresExtension; the
    /// loadout's or drivetrain's emptyKilograms; else Vehicle Framework's Mass times kilograms per Mass (12 by default), never below
    /// <see cref="MinimumKerbKg"/>.
    /// </summary>
    public static double KerbFor(VehicleDef vehicleDef, PhobosVehicleDrivetrainDef? def, ParameterSet? overrides)
    {
        float Get(string name, float fallback) => overrides != null && overrides.values.TryGetValue(name, out float v) ? v : fallback;
        PhobosVehicleFiguresExtension? own = vehicleDef.GetModExtension<PhobosVehicleFiguresExtension>();
        if (own != null && PhobosVehicleFiguresExtension.Given(own.emptyKilograms)) return own.emptyKilograms;
        MassXml mass = def?.mass ?? new MassXml();
        float empty = Get("emptyKilograms", mass.emptyKilograms);
        if (!float.IsNaN(empty) && empty > 0) return empty;
        return Math.Max(MinimumKerbKg, LoadoutSelector.BaseStat(vehicleDef, DefDatabase<VehicleStatDef>.GetNamedSilentFail("Mass"))
            * Get("kilogramsPerVehicleMass", mass.kilogramsPerVehicleMass));
    }

    /// <summary>The most engines a loadout may bind (Engine1 to Engine8).</summary>
    public const int MaxEngines = 8;

    /// <summary>Empty mass, a full cargo hold and full fuel: the weight a craft is designed to lift.</summary>
    double MaxGrossKg() => KerbKg + comp.Vehicle.GetStatValue(VehicleStatDefOf.CargoCapacity) + fuelKgFull;

    static TurbineSpec MakeTurbine(TurbineXml xml, bool includeWet)
    {
        var spec = new TurbineSpec
        {
            IdleSpeedPercent = xml.idleSpeedPercent,
            IdleTemperatureAboveAmbient = xml.idleTemperatureAboveAmbient,
            DeratePerDegree = xml.deratePerDegree,
            StartSpeedPercent = xml.startSpeedPercent,
        };
        spec.Ratings.Clear();
        foreach (TurbineRatingXml r in xml.ratings)
        {
            if (r.wet && !includeWet) continue;
            spec.Ratings.Add(new TurbineRating
            {
                Name = r.name, Share = r.share, SpeedPercent = r.speedPercent, TemperatureLimit = r.temperatureLimit, Minutes = r.minutes,
                ThrustKn = r.thrustKilonewtons, Wet = r.wet,
            });
        }
        if (spec.Ratings.Count == 0) spec.Ratings.Add(new TurbineRating { Name = "maxContinuous" });
        return spec;
    }

    public static Powertrain? For(CompPhobosVehicleSystems comp)
    {
        CompProperties_PhobosVehicleSystems props = comp.Props;
        if (props.drivetrain?.def == null && props.flightProfile == null) return null;
        return new Powertrain(comp, props.drivetrain?.def, props.drivetrain?.set, props.flightProfile);
    }

    static DrivetrainMode Mode => VehicleSystemsMod.Settings.drivetrainMode;

    public bool IsAircraft => Profile != null;
    public bool Ground => Spec.Transmission != TransmissionKind.None;
    public bool Full => Mode == DrivetrainMode.Full;

    /// <summary>The speed cap the path follower applies: 1 unless the model holds the vehicle back and the player chose Full.</summary>
    public float SpeedCap => DebugCapOverride is double forced ? (float)forced : Full && Ground ? (float)Cap : 1f;

    /// <summary>A cap forced by a dev action, to check the caravan slowing; null in play.</summary>
    public double? DebugCapOverride;

    /// <summary>Forgets the terrain under the vehicle, on arrival at a map.</summary>
    public void ForgetGround()
    {
        groundCell = IntVec3.Invalid;
        groundMap = null;
    }

    /// <summary>Heat as a share of full-load heat, the unit the cooling system's flows were calibrated in.</summary>
    public double HeatShare => Result.HeatKw / fullLoadHeatKw;

    public void MarkMassDirty() => massTick = -1;

    /// <summary>
    /// The turbine's temperature to show: the gas turbine system's quantity, which follows the model while the engine runs and cools
    /// over about an hour after it stops; the model's own figure while that system is switched off. Limits and wear use the model's.
    /// </summary>
    public float ShownTurbineTemperature
    {
        get
        {
            IReadOnlyList<SystemState> states = comp.States;
            for (int i = 0; i < states.Count; i++)
            {
                int index = states[i].Def.QuantityIndex("turbineTemperature");
                if (index >= 0 && comp.IsEnabled(states[i])) return (float)states[i].values[index];
            }
            return (float)TurbineNow.Temperature;
        }
    }

    /// <summary>Whether the engines run, for an aircraft: its phase says so.</summary>
    public bool AircraftRunning => PhaseSpec?.running ?? false;

    public bool AircraftMoving => PhaseSpec != null && PhaseSpec.running && PhaseSpec.moving;

    /// <summary>The working engines and how many there are: a loadout binds Engine1, Engine2 and so on (twin turboshafts, a pod's
    /// thrusters), else the one Engine counts. An engine below 20% health is out.</summary>
    public (int working, int engines) Engines()
    {
        int working = 0;
        foreach (string role in engineRoles)
        {
            if (comp.PartEfficiency(role, 1f) > 0.2f) working++;
        }
        return (working, engineRoles.Length);
    }

    /// <summary>Sets what the rotor drive accepts now: its rated share times the RotorDrive parts' condition (infinite without a limit).</summary>
    void ApplyDriveLimit(TurbineSpec spec)
    {
        spec.DriveLimitShare = double.IsInfinity(transmissionLimit) ? double.PositiveInfinity
            : transmissionLimit * Math.Max(0.05, comp.PartEfficiency("RotorDrive", 1f));
    }

    /// <summary>The traction motors' load now: pull ÷ their continuous rating (0 without a rating).</summary>
    public double TractionMotorLoad => comp.EngineRunningRaw ? DrivetrainModel.TractionMotorLoad(Spec, Result.TractiveForceN) : 0;

    double Condition()
    {
        float life = comp.TryGetQuantity("hotSectionLife", out float used) ? used : 0f;
        return life <= 1f ? 1 - 0.1 * life : Math.Max(0.6, 0.9 - 0.15 * (life - 1f));
    }

    /// <summary>Total mass now in kilograms: empty mass, cargo, passengers, ordnance in the turrets and fuel. Recomputed when cargo or
    /// passengers change, and at most every 250 ticks otherwise (fuel burns down).</summary>
    public double MassKg()
    {
        int now = Find.TickManager.TicksGame;
        if (massTick >= 0 && now - massTick < 250) return massKg;
        massTick = now;
        VehiclePawn vehicle = comp.Vehicle;
        double total = KerbKg + MassUtility.InventoryMass(vehicle) + (vehicle.GetComp<CompPhobosTank>()?.LoadKilograms ?? 0f);
        foreach (Pawn pawn in vehicle.AllPawnsAboard) total += pawn.GetStatValue(StatDefOf.Mass);
        if (vehicle.CompVehicleTurrets?.Turrets != null)
        {
            foreach (VehicleTurret turret in vehicle.CompVehicleTurrets.Turrets)
            {
                // shellCount counts shots; each item aboard gives chargePerAmmoCount of them (one, under Combat Extended's ammo).
                if (turret.loadedAmmo != null && turret.shellCount > 0) total += turret.loadedAmmo.BaseMass * turret.shellCount * Math.Max(0.01f, turret.def.chargePerAmmoCount);
            }
        }
        if (vehicle.CompFueledTravel != null) total += vehicle.CompFueledTravel.FuelPercent * fuelKgFull;
        massKg = total;
        return massKg;
    }

    /// <summary>Takes a sample now: once a second while the vehicle moves or flies, and on starting and stopping.</summary>
    public void Sample()
    {
        if (Mode == DrivetrainMode.Off)
        {
            Result = default;
            Cap = 1;
            return;
        }
        VehiclePawn vehicle = comp.Vehicle;
        int now = Find.TickManager.TicksGame;
        double seconds = lastTick < 0 ? 1 : Math.Max(0.01, (now - lastTick) / (double)TicksPerSecond);
        if (IsAircraft) SampleAircraft(vehicle, seconds);
        else SampleGround(vehicle, seconds);
        lastTick = now;
    }

    void SampleGround(VehiclePawn vehicle, double seconds)
    {
        bool running = comp.EngineRunningRaw;
        var conditions = new DriveConditions
        {
            EngineRunning = running,
            MassKg = MassKg(),
            CurrentGear = Result.Gear,
            UsableGears = UsableGears(),
            ClutchCapacity = ClutchCapacity(),
            DrivenWheelsWorking = comp.PartEfficiency("Wheels", 1f),
        };
        Vehicles.World.VehicleCaravan? caravan = vehicle.GetVehicleCaravan();
        if (vehicle.Spawned && vehicle.vehiclePather != null && vehicle.vehiclePather.Moving)
        {
            Vector3 position = SubCellPosition(vehicle);
            double measured = lastTick < 0 || seconds > 5 ? Spec.DesignSpeed : (position - lastPosition).magnitude * MetresPerCell / seconds;
            lastPosition = position;
            double intended = measured / Math.Max(0.05, Cap);
            conditions.Speed = intended;
            conditions.Acceleration = Mathf.Clamp((float)((intended - lastSpeed) / seconds), -3f, 3f);
            lastSpeed = intended;
            ReadGround(vehicle, out conditions.RollingResistance, out conditions.Grip);
            conditions.RollingResistance *= 1 + 2 * comp.RoleShareBelow("Wheels", 0.2f);
            conditions.Grip *= TreadGrip();
        }
        else if (caravan != null && caravan.vehiclePather != null && caravan.vehiclePather.MovingNow)
        {
            conditions.Speed = Spec.DesignSpeed;
            WorldGround(caravan.Tile, out conditions.RollingResistance, out conditions.Grade, out conditions.Grip);
            conditions.Grip *= TreadGrip();
        }
        else
        {
            lastSpeed = 0;
            if (vehicle.Spawned) lastPosition = vehicle.Position.ToVector3Shifted();
        }
        Result = DrivetrainModel.Evaluate(Spec, conditions);
        Cap = conditions.Speed > 0 ? DrivetrainModel.SmoothCap(Cap, Result.Cap) : 1;
        if (Turbine != null)
        {
            (int working, int engines) = Engines();
            double demand = Spec.RatedPowerKw > 0 ? Result.EnginePowerKw / Spec.RatedPowerKw : 0;
            TurbineNow = TurbineModel.Evaluate(Turbine, demand, comp.Ambient, Condition(), working, engines, running);
        }
    }

    void SampleAircraft(VehiclePawn vehicle, double seconds)
    {
        Phase = Profile!.reader?.Read(vehicle);
        PhaseSpec = Profile.Phase(Phase);
        bool running = PhaseSpec?.running ?? false;
        (int working, int engines) = Engines();
        TurbineSpec spec = Turbine ?? new TurbineSpec();
        bool water = HasWater();
        TurbineSpec usable = water ? spec : dryTurbine ?? spec;
        ApplyDriveLimit(usable);
        bool hovering = running && PhaseSpec != null && PhaseSpec.hover;
        BleedShare = running ? Bleed(vehicle, hovering) : 0;
        double demand = PhaseSpec == null ? 0 : PhaseSpec.hover ? HoverDemand(usable) : PhaseSpec.demand;
        demand = BleedRule.Demand(demand, BleedShare);
        TurbineNow = TurbineModel.Evaluate(usable, demand, comp.Ambient, Condition(), working, engines, running);
        bool beyond = hovering && Full && TurbineNow.OverTop > 0;
        if (beyond && VehicleSystemsMod.Settings.hoverLimit == VehicleSystemsSettings.HoverLimit.WarnOnly)
        {
            // Warn only (owner's setting, 8 Nov 2026): the engine is held at its top rating, so nothing wears past it.
            demand = TurbineNow.Available * usable.Top.Share;
            TurbineNow = TurbineModel.Evaluate(usable, demand, comp.Ambient, Condition(), working, engines, running);
        }
        if (beyond && !hoverWarned)
        {
            hoverWarned = true;
            string key = VehicleSystemsMod.Settings.hoverLimit == VehicleSystemsSettings.HoverLimit.WarnOnly ? "PhobosVehicleSystems_MsgHoverBeyond" : "PhobosVehicleSystems_MsgHoverStrained";
            Messages.Message(key.Translate(vehicle.LabelShort.Named("VEHICLE")), vehicle, MessageTypeDefOf.CautionInput);
        }
        if (!hovering) hoverWarned = false;
        HoverWet = usable.Ratings.Any(r => r.Wet && r.Name == TurbineNow.Rating);
        if (running && HoverWet && waterTankKg > 0) comp.AddToQuantity("injectionWater", (float)(-waterKgPerSecond * seconds / waterTankKg));
        Result = new DriveResult
        {
            Load = Math.Min(1, demand),
            Rpm = TurbineNow.SpeedPercent / 100 * Spec.RatedRpm,
            Cap = 1,
        };
        Cap = 1;
    }

    /// <summary>The share of the engine's work bled off now: reaction control in a hover (more in wind) and cabin heat (derived).</summary>
    public double BleedShare { get; private set; }

    double Bleed(VehiclePawn vehicle, bool hovering)
    {
        if (Profile == null) return 0;
        float wind = vehicle.Spawned && vehicle.Map != null ? vehicle.Map.windManager.WindSpeed : 0f;
        bool cabin = Profile.cabinBleed > 0 && (comp.LoadActive("cabHeater") || comp.LoadActive("climateControl"));
        return BleedRule.Share(hovering, Profile.reactionControlBleed, Profile.gustBleedFactor, wind, cabin, Profile.cabinBleed);
    }

    /// <summary>Whether this hover's over-limit message has been sent (once per hover; derived, never saved).</summary>
    bool hoverWarned;

    /// <summary>Whether this engine can judge a hover: an aircraft whose flight profile has hover phases, or checks its lift at launch.</summary>
    public bool JudgesHover => Profile != null && (Profile.liftCheckAtLaunch || Profile.phases.Any(phase => phase.hover));

    /// <summary>Whether lift-off through Vehicle Framework's own launch is checked against the engines (stock rotorcraft and pods).</summary>
    public bool ChecksLiftAtLaunch => Profile != null && Profile.liftCheckAtLaunch;

    bool HasWater() => waterTankKg > 0 && comp.TryGetQuantity("injectionWater", out float water) && water > 0.001f;

    /// <summary>The demand a hover asks, as a share of maximum continuous: weight against thrust for jets, momentum theory for rotors.</summary>
    double HoverDemand(TurbineSpec spec)
    {
        double mass = MassKg();
        if (spec.Continuous.ThrustKn > 0) return mass * DrivetrainModel.Gravity / 1000 / spec.Continuous.ThrustKn;
        double maxGross = KerbKg + comp.Vehicle.GetStatValue(VehicleStatDefOf.CargoCapacity) + fuelKgFull;
        return TurbineModel.HoverPowerNeeded(Profile!.hoverPowerAtMaxGross, mass / Math.Max(1, maxGross), comp.Ambient);
    }

    /// <summary>Whether the aircraft can hover now: dry, only with water, or not at all; and the best margin (1 means just enough).</summary>
    public (bool dry, bool wet, double margin) HoverCheck()
    {
        TurbineSpec spec = Turbine ?? new TurbineSpec();
        if (spec.Continuous.ThrustKn > 0 || spec.Ratings.Any(r => r.ThrustKn > 0))
        {
            (double dry, double wet) = TurbineModel.HoverMargin(spec, MassKg(), comp.Ambient, Condition() * EngineShare());
            // The same thrust asks more of the engine with bleed air taken (BleedRule), so the margin shrinks by as much.
            double bled = BleedRule.Demand(1, Profile != null ? Bleed(comp.Vehicle, hovering: true) : 0);
            dry /= bled;
            wet /= bled;
            if (!HasWater()) wet = dry;
            return (dry >= 1, wet >= 1, Math.Max(dry, wet));
        }
        ApplyDriveLimit(spec);
        double needed = BleedRule.Demand(HoverDemand(spec), Profile != null ? Bleed(comp.Vehicle, hovering: true) : 0);
        double available = TurbineModel.LiftPowerAvailable(spec, comp.Ambient, Condition(), EngineShare());
        return (available >= needed, available >= needed, available / Math.Max(0.01, needed));
    }

    double EngineShare()
    {
        (int working, int engines) = Engines();
        return (double)working / Math.Max(1, engines);
    }

    int UsableGears()
    {
        int gears = Spec.Gears.Length;
        float efficiency = comp.PartEfficiency("Transmission", 1f);
        if (efficiency < 0.2f) return Math.Min(2, gears);
        if (efficiency < 0.4f) return Math.Max(1, gears - 1);
        return gears;
    }

    double ClutchCapacity()
    {
        float efficiency = comp.PartEfficiency("Clutch", 1f);
        return efficiency >= 0.5f ? 1 : Math.Max(0.1, efficiency / 0.5f);
    }

    static Vector3 SubCellPosition(VehiclePawn vehicle)
    {
        VehiclePathFollower pather = vehicle.vehiclePather;
        Vector3 here = vehicle.Position.ToVector3Shifted();
        if (!pather.nextCell.IsValid || pather.nextCellCostTotal <= 0) return here;
        float progress = 1f - Mathf.Clamp01(pather.nextCellCostLeft / pather.nextCellCostTotal);
        return here + (pather.nextCell.ToVector3Shifted() - here) * progress;
    }

    /// <summary>What worn tread leaves of the ground's grip (T3, Framework 0.73.0): from the running gear's tread wear; rails, and a vehicle
    /// with no tyres or tracks system, keep it all.</summary>
    double TreadGrip() => OwnSurface || !comp.TryGetQuantity("treadWear", out float wear) ? 1 : TreadRules.GripFactor(wear);

    /// <summary>Running gear with its own surface (steel wheels on rails): the ground beneath does not touch it.</summary>
    public bool OwnSurface => !double.IsNaN(fixedRolling) && !double.IsNaN(fixedGrip);

    void ReadGround(VehiclePawn vehicle, out double rolling, out double grip)
    {
        if (!double.IsNaN(fixedRolling) && !double.IsNaN(fixedGrip))
        {
            // Running gear with its own surface (steel wheels on rails): the ground beneath doesn't matter.
            rolling = fixedRolling;
            grip = fixedGrip;
            return;
        }
        if (vehicle.Position != groundCell || vehicle.Map != groundMap)
        {
            groundCell = vehicle.Position;
            groundMap = vehicle.Map;
            Map map = vehicle.Map;
            double cost = map.terrainGrid.TerrainAt(groundCell)?.pathCost ?? 0;
            cost += WeatherBuildupUtility.MovementTicksAddOn(map.snowGrid.GetCategory(groundCell));
            groundRolling = DrivetrainModel.RollingResistanceForPathCost(cost);
            groundGrip = DrivetrainModel.GripForPathCost(cost);
        }
        rolling = groundRolling;
        grip = groundGrip;
    }

    /// <summary>
    /// A caravan's ground: the tile's biome (its typical terrain and difficulty), road and snow as a path cost through the same curves
    /// a map cell uses, and the grade by hilliness (WorldGroundRules; docs/development/vehicle-crew-and-ground-design.md). Rails and
    /// other running gear with their own surface keep it. Read once per tile and in-game hour (derived, never saved).
    /// </summary>
    void WorldGround(PlanetTile tile, out double rolling, out double grade, out double grip)
    {
        int now = Find.TickManager.TicksGame;
        if (tile != worldTile || now - worldTileTick >= ElapsedRule.TicksPerHour)
        {
            worldTile = tile;
            worldTileTick = now;
            if (BiomeGround.TryRead(tile, out TileGround ground))
            {
                WorldPathCost = WorldGroundRules.PathCost(ground.TerrainPathCost, ground.BiomeDifficulty, ground.Road, ground.WinterOffset);
                worldGrade = WorldGroundRules.Grade((int)ground.Hilliness);
                WorldGroundRead = ground;
            }
            else
            {
                WorldPathCost = 2;
                worldGrade = 0;
                WorldGroundRead = null;
            }
        }
        rolling = DrivetrainModel.RollingResistanceForPathCost(WorldPathCost);
        grip = DrivetrainModel.GripForPathCost(WorldPathCost);
        grade = worldGrade;
        if (!double.IsNaN(fixedRolling) && !double.IsNaN(fixedGrip))
        {
            rolling = fixedRolling;
            grip = fixedGrip;
        }
    }

    PlanetTile worldTile = PlanetTile.Invalid;
    int worldTileTick = -1;
    double worldGrade;

    /// <summary>The caravan tile's path cost as last read, for the dev log and the Systems tab.</summary>
    public double WorldPathCost { get; private set; } = 2;

    /// <summary>The caravan tile's ground as last read (null off the world map or before the first read), for the dev log.</summary>
    public TileGround? WorldGroundRead { get; private set; }

    /// <summary>When the player lets fuel follow the model, or a fitting or tune changes what the engine burns: the difference between
    /// that and Vehicle Framework's own use (full rate moving, half idling), burned or given back over the hours that passed. Normalised
    /// so a nominal cruise matches.</summary>
    public void ApplyFuel(double hours)
    {
        bool followsModel = VehicleFittings.FuelFollowsModel;
        double tuned = comp.Fittings.FuelFactor * (followsModel ? 1f : comp.Fittings.FlatFuelFactor);
        if ((!followsModel && tuned == 1) || !Full || !Ground || hours <= 0) return;
        CompFueledTravel? fuel = comp.Vehicle.CompFueledTravel;
        if (fuel == null || fuel.Props.electricPowered || !comp.EngineRunningRaw) return;
        // On a map Vehicle Framework burns its daily rate while moving and half of it idling; on the world map its own per-tick world
        // rate (with the author's world multiplier) while the caravan moves, and nothing while it stands.
        bool onMap = comp.Vehicle.Spawned;
        double vfPerHour = onMap ? fuel.FuelEfficiency / 24.0 : FuelRules.WorldPerHour(fuel.ConsumptionRateWorldPerTick, ElapsedRule.TicksPerHour);
        double vfShare = comp.Moving ? 1 : onMap ? 0.5 : 0;
        // Crosslink 5: the loads' share of rated power is burned at the engine's rated-power efficiency, added to the drive's fuel.
        double accessoryFuelKw = comp.AccessoryShare * DrivetrainModel.FuelPowerKw(Spec, Spec.RatedPowerKw, Spec.RatedRpm);
        double modelShare = FittingRule.TunedFuelShare(followsModel ? (Result.FuelPowerKw + accessoryFuelKw) / Math.Max(0.1, nominalCruiseFuelKw) : vfShare, tuned);
        double perHour = FuelRules.ExtraPerHour(vfPerHour, vfShare, modelShare);
        if (perHour > 0 && !fuel.EmptyTank) fuel.ConsumeFuel((float)(perHour * hours));
        else if (perHour < 0) fuel.Refuel((float)(-perHour * hours));
    }
}
