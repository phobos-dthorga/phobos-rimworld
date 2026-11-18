using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Debug;
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
    /// <summary>The engine power and speed at the nominal cruise (step S6: what the world map's range assumes). Derived, never saved.</summary>
    readonly double cruiseEngineKw, cruiseRpm;
    readonly double fuelKgFull;
    readonly double waterTankKg;
    readonly double waterKgPerSecond;
    readonly double transmissionLimit;
    readonly double fixedRolling;
    readonly double fixedGrip;
    public readonly double KerbKg;
    /// <summary>How many times faster than the crankshaft the alternator turns (check C4, Framework 0.129.1), from the engine's Def.</summary>
    public readonly double AlternatorRatio;
    /// <summary>Whether air is forced into a piston engine (Framework 0.132.0): turbocharged as built, or with a fitting that grants
    /// <see cref="ForcedInductionGrant"/>. It then holds its rated power in thin air up to <see cref="HoldsPowerToMetres"/>.</summary>
    public readonly bool ForcedInduction;
    public readonly double HoldsPowerToMetres;
    /// <summary>What a fitting grants that forces air into the engine (the turbocharger's boost).</summary>
    public const string ForcedInductionGrant = "Boost";
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
        AlternatorRatio = Get("alternatorRatio", engine.alternatorRatio);
        // Fittings and tunes move only piston engines' figures (their Defs say so); a change builds the powertrain again.
        VehicleFittings fittings = comp.Fittings;
        ForcedInduction = engine.turbocharged || fittings.Grants(ForcedInductionGrant);
        float holds = Get("holdsPowerToMetres", engine.holdsPowerToMetres);
        HoldsPowerToMetres = !float.IsNaN(holds) && holds > 0 ? holds : PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>().turbochargerHoldsToMetres;
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
            DriveResult atCruise = DrivetrainModel.Evaluate(Spec, cruise);
            cruiseEngineKw = atCruise.EnginePowerKw;
            cruiseRpm = atCruise.Rpm;
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
    /// <summary>The heaviest the aircraft is built to be, kilograms: empty, full cargo, full fuel (the hover rule's and the cruising
    /// height's reference weight).</summary>
    public double MaxGrossKg() => KerbKg + comp.Vehicle.GetStatValue(VehicleStatDefOf.CargoCapacity) + fuelKgFull;

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
                Name = r.name, Label = r.label.NullOrEmpty() ? GenText.SplitCamelCase(r.name).ToLowerInvariant() : r.label, Share = r.share, SpeedPercent = r.speedPercent, TemperatureLimit = r.temperatureLimit, Minutes = r.minutes,
                ThrustKn = r.thrustKilonewtons, Wet = r.wet,
            });
        }
        if (spec.Ratings.Count == 0) spec.Ratings.Add(new TurbineRating { Name = "maxContinuous", Label = "PhobosVehicleSystems_RatingMaxContinuous".Translate().Resolve() });
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

    /// <summary>Whether the engine breathes the air around it (Framework 0.132.0): a piston engine or a gas turbine. An electric motor
    /// and a thruster do not, and thin air takes nothing from them.</summary>
    public bool BreathesAir => Spec.Engine is EngineKind.Piston or EngineKind.Turboshaft or EngineKind.Turbofan;

    /// <summary>The share of its rated power thin air leaves this engine at a height in metres (AirRule): a naturally aspirated piston
    /// engine loses it from the sea up, a turbocharged one holds it to its limit, a turbine follows the air's pressure.</summary>
    public double AirPowerShare(double elevationMetres) => Spec.Engine switch
    {
        EngineKind.Piston => AirRule.PistonPower(elevationMetres, ForcedInduction, HoldsPowerToMetres),
        EngineKind.Turboshaft or EngineKind.Turbofan => AirRule.PressureRatio(elevationMetres),
        _ => 1,
    };
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

    string? ratingHeld;
    int ratingSinceTick;
    bool ratingLimitTold;

    /// <summary>Whether the turbine has held a rating above maximum continuous for longer than its time limit (Framework 0.110.0, avionics
    /// L1): the hot section then wears faster. Derived, never saved: after a load the count starts again.</summary>
    public bool OverRatingLimit { get; private set; }

    /// <summary>Times how long the turbine has held its present rating, and says once when it passes the rating's time limit.</summary>
    void TrackRating(VehiclePawn vehicle, int now)
    {
        string? name = TurbineNow.Rating;
        if (name != ratingHeld)
        {
            ratingHeld = name;
            ratingSinceTick = now;
            ratingLimitTold = false;
        }
        TurbineRating? rating = Turbine?.Ratings.FirstOrDefault(r => r.Name == name) ?? dryTurbine?.Ratings.FirstOrDefault(r => r.Name == name);
        bool running = comp.EngineRunning;
        OverRatingLimit = running && rating != null && RatingLimitRule.Over(now - ratingSinceTick, rating.Minutes, GenDate.TicksPerHour / 60.0);
        if (OverRatingLimit && !ratingLimitTold && vehicle.Faction == Faction.OfPlayer)
        {
            ratingLimitTold = true;
            Messages.Message("PhobosVehicleSystems_MsgRatingOverLimit".Translate(vehicle.LabelShort.Named("VEHICLE"), RatingLabel.Named("RATING"),
                rating!.Minutes.ToString("0.#").Named("MINUTES")), vehicle, MessageTypeDefOf.CautionInput, historical: false);
        }
    }

    /// <summary>The rating the turbine runs at now, in the player's words.</summary>
    public string RatingLabel
    {
        get
        {
            string? name = TurbineNow.Rating;
            TurbineRating? rating = Turbine?.Ratings.FirstOrDefault(r => r.Name == name) ?? dryTurbine?.Ratings.FirstOrDefault(r => r.Name == name);
            return rating != null && !rating.Label.NullOrEmpty() ? rating.Label : name ?? "";
        }
    }

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
        // The ticks this sample stands for, for the fuel an aircraft owes (Framework 0.132.0); none after a long gap, such as a flight.
        int gap = lastTick < 0 ? 0 : now - lastTick;
        if (IsAircraft) SampleAircraft(vehicle, seconds, gap > MaxFuelGapTicks ? 0 : gap);
        else SampleGround(vehicle, seconds);
        if (Turbine != null) TrackRating(vehicle, now);
        lastTick = now;
    }

    void SampleGround(VehiclePawn vehicle, double seconds)
    {
        bool running = comp.EngineRunningRaw;
        var conditions = new DriveConditions
        {
            EngineRunning = running,
            MassKg = MassKg(),
            // A trailer behind the hitch (Framework 0.101.0): rolled, speeded up and lifted with the vehicle, no grip.
            TowedMassKg = comp.Hitch?.TowedKilograms ?? 0,
            CurrentGear = Result.Gear,
            UsableGears = UsableGears(),
            ClutchCapacity = ClutchCapacity(),
            DrivenWheelsWorking = comp.PartEfficiency("Wheels", 1f),
            // Step S4 (Framework 0.130.0): what a choked filter, a warped head or a starved feed takes from the engine; and since
            // 0.132.0 what thin air at height takes.
            PowerLoss = 1 - comp.EnginePowerFactor * comp.AirPowerFactor,
            // The engine-load ledger (Framework 0.131.0): what the engine was giving to other things at the last look.
            AccessoryKw = running ? comp.LedgerExtraKw : 0,
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
            LastMovingSpeed = measured;
            ReadGround(vehicle, out conditions.RollingResistance, out conditions.Grip);
            conditions.RollingResistance *= 1 + 2 * comp.RoleShareBelow("Wheels", 0.2f);
            conditions.Grip *= TreadGrip(conditions.Grip) * WeatherGrip(vehicle);
        }
        else if (caravan != null && caravan.vehiclePather != null && caravan.vehiclePather.MovingNow)
        {
            conditions.Speed = Spec.DesignSpeed;
            WorldGround(LegTile(caravan), out conditions.RollingResistance, out conditions.Grade, out conditions.Grip);
            conditions.Grip *= TreadGrip(conditions.Grip);
        }
        else
        {
            lastSpeed = 0;
            if (vehicle.Spawned) lastPosition = vehicle.Position.ToVector3Shifted();
        }
        Result = DrivetrainModel.Evaluate(Spec, conditions);
        Cap = conditions.Speed > 0 ? DrivetrainModel.SmoothCap(Cap, Result.Cap) : 1;
        if (VehicleSystemsMod.DriveLog.DebugEnabled) LogDrive(vehicle, conditions);
        if (Turbine != null)
        {
            (int working, int engines) = Engines();
            // Gap T-1 (Framework 0.132.0): what the engine gives to other things heats and wears the turbine as driving does.
            double demand = Spec.RatedPowerKw > 0 ? (Result.EnginePowerKw + (running ? comp.LedgerExtraKw : 0)) / Spec.RatedPowerKw : 0;
            TurbineNow = TurbineModel.Evaluate(Turbine, demand, comp.Ambient, Condition(), working, engines, running, comp.AirPressure);
        }
    }

    /// <summary>The flight phase as the aircraft's own framework reports it now, read by name; null for an aircraft without a profile.</summary>
    public string? PhaseNow(VehiclePawn vehicle) => Profile?.reader?.Read(vehicle);

    void SampleAircraft(VehiclePawn vehicle, double seconds, int ticks)
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
        ReactionBleedShare = running ? ReactionBleed(vehicle, hovering) : 0;
        CabinBleedShare = running && CabinBleedOn() ? Profile.cabinBleed : 0;
        double air = comp.AirPressure;
        // What flying asks of the engines, then what else they give (Framework 0.132.0, gap T-2 and step S7): bleed air, and the
        // generator at the vehicle's last look. All of it heats and wears the turbine and burns fuel; the ledger lists it.
        // Framework 0.141.0: a phase flown on the rotors that is not a hover follows the hover figure from its authored one.
        double flying = PhaseSpec == null ? 0 : PhaseSpec.hover ? HoverDemand(usable)
            : PhaseSpec.followsHover ? TurbineModel.FollowHover(PhaseSpec.demand, HoverDemand(usable), HoverDemand(usable, MaxGrossKg(), StandardAmbient, 1))
            // Framework 0.147.0: our own flight's forward phase, the hover figure times the forward-flight share.
            : PhaseSpec.forwardFlight ? HoverDemand(usable) * Profile.forwardFlightShare
            : PhaseSpec.demand;
        if (running && PhaseSpec is { followsHover: true } follows && Seen.On)
        {
            string side = flying >= follows.demand ? "above" : "below";
            if (Seen.Wants("aircraft.follows-hover", vehicle, side))
            {
                Seen.Note("aircraft.follows-hover", vehicle, Phase, side, new[]
                {
                    new KeyValuePair<string, double>("demand", flying),
                    new KeyValuePair<string, double>("authored", follows.demand),
                });
            }
        }
        double shaft = running ? ShaftShare(comp.GeneratorKw) : 0;
        double demand = BleedRule.Demand(flying, BleedShare) + shaft;
        TurbineNow = TurbineModel.Evaluate(usable, demand, comp.Ambient, Condition(), working, engines, running, air);
        bool beyond = hovering && Full && TurbineNow.OverTop > 0;
        if (beyond && VehicleSystemsMod.Settings.hoverLimit == VehicleSystemsSettings.HoverLimit.WarnOnly)
        {
            // Warn only (owner's setting, 8 Nov 2026): the engine is held at its top rating, so nothing wears past it.
            demand = TurbineNow.Available * usable.Top.Share;
            flying = Math.Max(0, demand - shaft) / (1 + BleedShare);
            TurbineNow = TurbineModel.Evaluate(usable, demand, comp.Ambient, Condition(), working, engines, running, air);
        }
        FlightDemand = running ? flying : 0;
        if (beyond && !hoverWarned && vehicle.Faction == Faction.OfPlayer)
        {
            hoverWarned = true;
            string key = VehicleSystemsMod.Settings.hoverLimit == VehicleSystemsSettings.HoverLimit.WarnOnly ? "PhobosVehicleSystems_MsgHoverBeyond" : "PhobosVehicleSystems_MsgHoverStrained";
            Messages.Message(key.Translate(vehicle.LabelShort.Named("VEHICLE")), vehicle, MessageTypeDefOf.CautionInput);
        }
        if (!hovering) hoverWarned = false;
        HoverWet = usable.Ratings.Any(r => r.Wet && r.Name == TurbineNow.Rating);
        if (running && HoverWet && waterTankKg > 0) comp.AddToQuantity("injectionWater", (float)(-waterKgPerSecond * seconds / waterTankKg));
        // The load here is flying's alone since Framework 0.132.0; the bleed air and the generator are the ledger's, added by the engine's
        // one picture (EngineState), so nothing is counted twice.
        Result = new DriveResult
        {
            Load = Math.Min(1, flying),
            Rpm = TurbineNow.SpeedPercent / 100 * Spec.RatedRpm,
            Cap = 1,
        };
        Cap = 1;
        OweFuel(vehicle, running, demand, ticks);
    }

    /// <summary>Sea-level air's standard temperature, °C: where an authored flight demand holds.</summary>
    const double StandardAmbient = 15;

    /// <summary>The share of the engine's work bled off now: reaction control in a hover (more in wind) and cabin heat (derived).</summary>
    public double BleedShare => ReactionBleedShare + CabinBleedShare;

    /// <summary>The reaction controls' share of it (the puffer jets, in a hover), and the cabin's (heat or air conditioning).</summary>
    public double ReactionBleedShare { get; private set; }
    public double CabinBleedShare { get; private set; }

    /// <summary>What flying asks of an aircraft's engines now, as a share of maximum continuous: its phase's demand, or its hover's; 0
    /// with them stopped. The bleed air and the generator come on top (the ledger). Derived.</summary>
    public double FlightDemand { get; private set; }

    /// <summary>A shaft load as a share of the engines' rated output.</summary>
    double ShaftShare(double kilowatts) => Spec.RatedPowerKw > 0 ? Math.Max(0, kilowatts) / Spec.RatedPowerKw : 0;

    bool CabinBleedOn() => Profile != null && Profile.cabinBleed > 0 && (comp.LoadActive("cabHeater") || comp.LoadActive("climateControl"));

    double ReactionBleed(VehiclePawn vehicle, bool hovering)
    {
        if (Profile == null) return 0;
        float wind = vehicle.Spawned && vehicle.Map != null ? vehicle.Map.windManager.WindSpeed : 0f;
        return BleedRule.Share(hovering, Profile.reactionControlBleed, Profile.gustBleedFactor, wind, false, 0);
    }

    double Bleed(VehiclePawn vehicle, bool hovering) =>
        Profile == null ? 0 : ReactionBleed(vehicle, hovering) + (CabinBleedOn() ? Profile.cabinBleed : 0);

    /// <summary>Whether this hover's over-limit message has been sent (once per hover; derived, never saved).</summary>
    bool hoverWarned;

    /// <summary>Whether this engine can judge a hover: an aircraft whose flight profile has hover phases, or checks its lift at launch.</summary>
    public bool JudgesHover => Profile != null && (Profile.liftCheckAtLaunch || Profile.phases.Any(phase => phase.hover));

    /// <summary>Whether lift-off through Vehicle Framework's own launch is checked against the engines (stock rotorcraft and pods).</summary>
    public bool ChecksLiftAtLaunch => Profile != null && Profile.liftCheckAtLaunch;

    bool HasWater() => waterTankKg > 0 && comp.TryGetQuantity("injectionWater", out float water) && water > 0.001f;

    /// <summary>The demand a hover asks, as a share of maximum continuous: weight against thrust for jets, momentum theory for rotors.</summary>
    double HoverDemand(TurbineSpec spec) => HoverDemand(spec, MassKg(), comp.Ambient, comp.AirPressure);

    double HoverDemand(TurbineSpec spec, double mass, double ambient, double pressure)
    {
        if (spec.Continuous.ThrustKn > 0) return mass * DrivetrainModel.Gravity / 1000 / spec.Continuous.ThrustKn;
        return TurbineModel.HoverPowerNeeded(Profile!.hoverPowerAtMaxGross, mass / Math.Max(1, MaxGrossKg()), ambient, pressure);
    }

    /// <summary>The share of its engines still working, 0 to 1.</summary>
    public double WorkingEngineShare => EngineShare();

    /// <summary>Whether the aircraft can hover now: dry, only with water, or not at all; and the best margin (1 means just enough).</summary>
    public (bool dry, bool wet, double margin) HoverCheck() => HoverCheck(MassKg(), comp.Ambient, comp.AirPressure);

    /// <summary>The kilograms of fuel one unit of the tank weighs (Framework 0.140.0), so a journey's burn can be taken off the weight.</summary>
    public double FuelKgPerUnit => comp.Vehicle.CompFueledTravel is { FuelCapacity: > 0 } fuel ? fuelKgFull / fuel.FuelCapacity : 0;

    /// <summary>The same check at another weight and in another place's air (Framework 0.140.0: the landing check at a journey's end):
    /// the destination tile's temperature and, where the engine breathes air, its pressure.</summary>
    public (bool dry, bool wet, double margin) HoverCheck(double massKg, double ambient, double pressure)
    {
        TurbineSpec spec = Turbine ?? new TurbineSpec();
        if (spec.Continuous.ThrustKn > 0 || spec.Ratings.Any(r => r.ThrustKn > 0))
        {
            (double dry, double wet) = TurbineModel.HoverMargin(spec, massKg, ambient, Condition() * EngineShare(), pressure);
            // The same thrust asks more of the engine with bleed air taken (BleedRule), so the margin shrinks by as much.
            double bled = BleedRule.Demand(1, Profile != null ? Bleed(comp.Vehicle, hovering: true) : 0);
            dry /= bled;
            wet /= bled;
            if (!HasWater()) wet = dry;
            return (dry >= 1, wet >= 1, Math.Max(dry, wet));
        }
        ApplyDriveLimit(spec);
        double needed = BleedRule.Demand(HoverDemand(spec, massKg, ambient, pressure), Profile != null ? Bleed(comp.Vehicle, hovering: true) : 0);
        double available = TurbineModel.LiftPowerAvailable(spec, ambient, Condition(), EngineShare(), pressure);
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

    /// <summary>The speed the vehicle was last seen moving at on a map, in metres a second, kept after it stops: what a stop's brake work reads
    /// (cross-links T4, Framework 0.123.0). Derived, never saved; 0 until it has moved.</summary>
    public double LastMovingSpeed { get; private set; }

    /// <summary>What the clutch can hold: a damaged part slips under high load (drivetrain design), and since Framework 0.123.0 (cross-links T4)
    /// a worn one too, by its wear (BrakeWearRule.ClutchHolds); the worse of the two shares multiplies.</summary>
    double ClutchCapacity()
    {
        float efficiency = comp.PartEfficiency("Clutch", 1f);
        double part = efficiency >= 0.5f ? 1 : Math.Max(0.1, efficiency / 0.5f);
        double worn = comp.TryGetQuantity("clutchWear", out float wear) ? BrakeWearRule.ClutchHolds(wear) : 1;
        return Math.Max(0.1, part * worn);
    }

    static Vector3 SubCellPosition(VehiclePawn vehicle)
    {
        VehiclePathFollower pather = vehicle.vehiclePather;
        Vector3 here = vehicle.Position.ToVector3Shifted();
        if (!pather.nextCell.IsValid || pather.nextCellCostTotal <= 0) return here;
        float progress = 1f - Mathf.Clamp01(pather.nextCellCostLeft / pather.nextCellCostTotal);
        return here + (pather.nextCell.ToVector3Shifted() - here) * progress;
    }

    /// <summary>What the tread does to the ground's grip: worn tread leaves less of it (T3, Framework 0.73.0), and tyres built for soft ground
    /// add to it the softer the going (all-terrain tyres, Framework 0.103.0). Rails, and a vehicle with no tyres or tracks system, keep it all.</summary>
    double TreadGrip(double groundGrip)
    {
        if (OwnSurface || !comp.TryGetQuantity("treadWear", out float wear)) return 1;
        return TreadRules.GripFactor(wear) * TreadRules.SoftGroundFactor(groundGrip, comp.Fittings.ParameterFactor(RunningGearSystem, SoftGroundGripParameter));
    }

    /// <summary>What rain, falling snow and ice under the wheels do to grip on a map (cross-links G5, Framework 0.113.0; WeatherGripRule): the
    /// game's own rain and snow rates, eased between weathers as the game eases them, none under a roof. Rails, and the player's "weather takes
    /// grip" setting switched off, keep it all. On the world map the tile's winter already counts in its path cost.</summary>
    double WeatherGrip(VehiclePawn vehicle)
    {
        if (OwnSurface || !VehicleSystemsMod.Settings.weatherGrip || !vehicle.Spawned || vehicle.Map == null) return 1;
        Map map = vehicle.Map;
        IntVec3 cell = vehicle.Position;
        bool open = !cell.Roofed(map);
        PhobosVehicleRulesDef rules = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();
        double factor = WeatherGripRule.Factor(open ? map.weatherManager.RainRate : 0, open ? map.weatherManager.SnowRate : 0,
            map.terrainGrid.TerrainAt(cell)?.IsIce == true, rules.weatherGripRain, rules.weatherGripSnow, rules.weatherGripIce);
        if (VehicleSystemsMod.StagesLog.DebugEnabled && System.Math.Abs(factor - loggedWeatherGrip) >= 0.05)
        {
            // Verbose only, and only when it moves by a twentieth: weather on grip had said nothing (the owner's verbose log, 16 Nov 2026).
            VehicleSystemsMod.StagesLog.Debug(vehicle.LabelShort + " (" + vehicle.ThingID + ") grip from the weather x" + factor.ToString("0.##") + " (rain "
                + map.weatherManager.RainRate.ToString("0.##") + ", snow " + map.weatherManager.SnowRate.ToString("0.##") + (open ? "" : ", under a roof")
                + (map.terrainGrid.TerrainAt(cell)?.IsIce == true ? ", on ice" : "") + ")");
        }
        loggedWeatherGrip = factor;
        return factor;
    }

    /// <summary>What the drive log last said (never saved): it speaks only when what holds the vehicle back changes, the cap moves by a
    /// tenth, or the clutch starts or stops slipping, so a steady drive writes nothing.</summary>
    DriveLimit loggedLimit = DriveLimit.None;
    double loggedCap = 1;
    bool loggedSlip;

    void LogDrive(VehiclePawn vehicle, DriveConditions conditions)
    {
        if (Result.Limit == loggedLimit && System.Math.Abs(Cap - loggedCap) < 0.1 && Result.ClutchSlipping == loggedSlip) return;
        loggedLimit = Result.Limit;
        loggedCap = Cap;
        loggedSlip = Result.ClutchSlipping;
        VehicleSystemsMod.DriveLog.Debug(vehicle.LabelShort + " (" + vehicle.ThingID + ") " + (conditions.Speed > 0 ? "driving" : "standing")
            + ": held by " + (Result.Limit == DriveLimit.None ? "nothing" : Result.Limit == DriveLimit.Traction ? "grip" : "power")
            + ", speed cap x" + Cap.ToString("0.##") + " (this moment x" + Result.Cap.ToString("0.##") + ")" + (Result.ClutchSlipping ? ", clutch slipping" : "")
            + "; gear " + Result.Gear + " of " + conditions.UsableGears + ", " + Result.Rpm.ToString("0") + " rpm, load " + Result.Load.ToString("0.##")
            + "; " + conditions.MassKg.ToString("0") + " kg" + (conditions.TowedMassKg > 0 ? " towing " + conditions.TowedMassKg.ToString("0") + " kg" : "")
            + ", grip " + conditions.Grip.ToString("0.##") + ", rolling " + conditions.RollingResistance.ToString("0.###") + ", grade " + conditions.Grade.ToString("0.###")
            + ", driven wheels " + conditions.DrivenWheelsWorking.ToString("0.##") + (vehicle.Spawned ? "" : " (world map)"));
    }

    /// <summary>The weather's grip factor last reported to the verbose log (never saved).</summary>
    double loggedWeatherGrip = 1;

    /// <summary>The running gear system and its soft-ground grip parameter, which a fitting moves (all-terrain tyres).</summary>
    const string RunningGearSystem = "PhobosVehicleSystem_RunningGear";
    const string SoftGroundGripParameter = "softGroundGrip";

    /// <summary>Running gear with its own surface (steel wheels on rails): the ground beneath does not touch it.</summary>
    public bool OwnSurface => !double.IsNaN(fixedRolling) && !double.IsNaN(fixedGrip);

    /// <summary>A map cell's going as a path cost: its terrain's and its snow's, the figure the ground curves read (the drive, the winch).</summary>
    public static double PathCostAt(Map map, IntVec3 cell)
    {
        double cost = map.terrainGrid.TerrainAt(cell)?.pathCost ?? 0;
        return cost + WeatherBuildupUtility.MovementTicksAddOn(map.snowGrid.GetCategory(cell));
    }

    /// <summary>
    /// "Can this pull that?" before a tow (hitch slice 3, Framework 0.104.0): the speed share this vehicle would hold at its design speed with
    /// <paramref name="towedKg"/> behind it, on the ground it stands on, from the same model and the same ground, tread and gearbox reads the
    /// drive uses (TowRule.Modelled). A forecast: hills and softer going on the way change it. The vehicle must be on a map.
    /// </summary>
    public TowVerdict ForecastTow(double towedKg)
    {
        VehiclePawn vehicle = comp.Vehicle;
        ReadGround(vehicle, out double rolling, out double grip);
        rolling *= 1 + 2 * comp.RoleShareBelow("Wheels", 0.2f);
        grip *= TreadGrip(grip) * WeatherGrip(vehicle);
        TowVerdict verdict = TowRule.Modelled(Spec, MassKg(), towedKg, rolling, grip, comp.PartEfficiency("Wheels", 1f), UsableGears());
        if (VehicleSystemsMod.HitchLog.DebugEnabled)
        {
            VehicleSystemsMod.HitchLog.Debug(vehicle.LabelShort + " (" + vehicle.ThingID + ") tow forecast for " + towedKg.ToString("0") + " kg behind "
                + MassKg().ToString("0") + " kg: " + verdict.Outcome + ", speed x" + verdict.SpeedShare.ToString("0.##") + ", held by " + verdict.Limit
                + " (grip " + grip.ToString("0.##") + ", rolling " + rolling.ToString("0.###") + ")");
        }
        return verdict;
    }

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
            double cost = PathCostAt(vehicle.Map, groundCell);
            groundRolling = DrivetrainModel.RollingResistanceForPathCost(cost);
            groundGrip = DrivetrainModel.GripForPathCost(cost);
        }
        rolling = groundRolling;
        grip = groundGrip;
    }

    /// <summary>
    /// The tile a moving caravan's ground is read from (Framework 0.131.0, gap D5): the one it is driving into. Vehicle Framework sets a
    /// caravan's own tile only when a leg ends, so reading that had it climb the hill it had just come off. A caravan turned back
    /// mid-leg heads for its own tile, which is then the answer.
    /// </summary>
    static PlanetTile LegTile(Vehicles.World.VehicleCaravan caravan)
    {
        PlanetTile next = caravan.vehiclePather.NextTile;
        return next.Valid ? next : caravan.Tile;
    }

    /// <summary>
    /// A caravan's ground: the tile's biome (its typical terrain and difficulty), road and snow as a path cost through the same curves
    /// a map cell uses, and the grade by hilliness (WorldGroundRules; docs/development/vehicle-world-map-design.md). Rails and
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
                // Framework 0.131.0 (gaps D3 and D4): a road keeps part of the country's grade, a railway less.
                PhobosVehicleRulesDef rules = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();
                bool rails = !double.IsNaN(fixedRolling) && !double.IsNaN(fixedGrip);
                worldGrade = WorldGroundRules.Grade((int)ground.Hilliness, ground.Road, rails, rules.roadGradeFactor, rules.railGradeFactor);
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

    /// <summary>The shaft power the engine gives now, kW (Framework 0.130.0, step S1): driving's, or standing work's when work turns the
    /// engine (the drive sample knows nothing of that), and the ledger's.</summary>
    public double ShaftKwNow(EngineState engine, bool withWork = true) =>
        (engine.Why == EngineWhy.Working ? (withWork ? comp.WorkPowerShare * Spec.RatedPowerKw : 0) : Math.Max(0, Result.EnginePowerKw))
        + Math.Max(0, engine.AccessoryShare) * Spec.RatedPowerKw;

    /// <summary>The fuel power the engine burns now for that shaft power, kW: the one figure the fuel burn and the Engineer line both read
    /// (step S1). The loads cost only what they add on the Willans line, a kilowatt over the indicated efficiency, with friction counted
    /// once; before 0.130.0 they were priced at rated-power fuel, friction and all, about 15 % too much.</summary>
    /// <param name="withWork">Whether standing work's own power is counted: not in the fuel sum for work that burns a rate of its own.</param>
    public double FuelKwNow(EngineState engine, bool withWork = true) =>
        DrivetrainModel.FuelPowerKw(Spec, ShaftKwNow(engine, withWork), engine.Why == EngineWhy.Working ? engine.Rpm : Result.Rpm);

    /// <summary>The fuel power at the nominal cruise, kW, that the author's rate stands for (Framework 0.131.0): the cruise's own, with the
    /// standing loads a rated figure already includes (a water pump), so that a cruise with nothing extra on burns the author's rate.</summary>
    public double NominalCruiseFuelKw =>
        Math.Max(0.1, DrivetrainModel.FuelPowerKw(Spec, cruiseEngineKw + comp.BaselineShaftKwAt(CruiseRpm), CruiseRpm));

    double CruiseRpm => Math.Max(cruiseRpm, Spec.IdleRpm);

    /// <summary>The fuel at the nominal cruise with these extra loads on, as a share of the nominal cruise (Framework 0.130.0, step S6): what
    /// the world map's range puts on Vehicle Framework's rate when the fuel follows the model. 1 with nothing extra on.</summary>
    public double CruiseFuelShare(double extraShare) =>
        DrivetrainModel.FuelPowerKw(Spec, cruiseEngineKw + comp.BaselineShaftKwAt(CruiseRpm) + Math.Max(0, extraShare) * Spec.RatedPowerKw, CruiseRpm)
        / NominalCruiseFuelKw;

    /// <summary>When the player lets fuel follow the model, or a fitting or tune changes what the engine burns: the difference between
    /// that and Vehicle Framework's own use (full rate moving, half idling), burned or given back over the hours that passed. Normalised
    /// so a nominal cruise matches.</summary>
    public void ApplyFuel(double hours)
    {
        if (IsAircraft)
        {
            ApplyAircraftFuel();
            return;
        }
        bool followsModel = VehicleFittings.FuelFollowsModel;
        // A stage's fuel factor joins the fittings' (Framework 0.123.0: a clogged air filter, cross-links G8).
        double tuned = comp.Fittings.TunedFuelFactor * comp.EngineFuelFactor;
        if ((!followsModel && tuned == 1) || !Full || !Ground || hours <= 0) return;
        CompFueledTravel? fuel = comp.Vehicle.CompFueledTravel;
        // Standing work turns the engine with nobody driving (Framework 0.131.0): the model burns for it too, where the work says it may
        // and the fuel follows the model; otherwise the work burns its own fixed rate, as before.
        bool working = comp.WorkTurningEngine && followsModel && comp.WorkFuelFromModel;
        if (fuel == null || fuel.Props.electricPowered || !(comp.EngineRunningRaw || working)) return;
        // On a map Vehicle Framework burns its daily rate while moving and half of it idling; on the world map its own per-tick world
        // rate (with the author's world multiplier) while the caravan moves, and nothing while it stands. With nobody at the controls
        // it burns nothing.
        bool onMap = comp.Vehicle.Spawned;
        double vfPerHour = onMap ? fuel.FuelEfficiency / 24.0 : FuelRules.WorldPerHour(fuel.ConsumptionRateWorldPerTick, ElapsedRule.TicksPerHour);
        double vfShare = !comp.EngineRunningRaw ? 0 : comp.Moving ? 1 : onMap ? FuelRateRule.FrameworkIdleShare : 0;
        // The ledger's shaft power is burned with the drive's, on the same Willans line (step S1).
        double modelShare = FittingRule.TunedFuelShare(followsModel ? FuelKwNow(comp.Engine, comp.WorkFuelFromModel) / NominalCruiseFuelKw : vfShare, tuned);
        double perHour = FuelRules.ExtraPerHour(vfPerHour, vfShare, modelShare);
        // Step S2 (Framework 0.130.0): through the one fuel service, so the vehicle can say what it burns and why.
        if (perHour > 0) VehicleFuel.Burn(comp.Vehicle, (float)(perHour * hours), FuelUse.Engine, comp);
        else if (perHour < 0) VehicleFuel.GiveBack(comp.Vehicle, (float)(-perHour * hours), FuelUse.Engine, comp);
    }

    // ---- Aircraft fuel (Framework 0.132.0, gap T-3; docs/development/vehicle-altitude-and-turbines.md) ----

    /// <summary>A sample older than this many ticks is stale: nothing is owed for the gap, and the tank's loss across it is not read (an
    /// aircraft back from a flight between tiles, a load).</summary>
    const int MaxFuelGapTicks = 600;

    /// <summary>What the tank may lose to everything else beyond its frameworks' own rates before it stops counting as engine fuel
    /// (authored: a twentieth over, for rounding).</summary>
    const double SeenFuelMargin = 1.05;

    /// <summary>Whether this aircraft's fuel follows its engines: the player's own, with the setting on and the full model.</summary>
    public bool AircraftFuelFollows => IsAircraft && Full && VehicleSystemsMod.Settings.aircraftFuel && comp.Vehicle.Faction == Faction.OfPlayer
        && comp.Vehicle.CompFueledTravel is { } fuel && !fuel.Props.electricPowered;

    /// <summary>Whether the last look set this aircraft's fuel on a map to what its engines owed (derived; the fuel line reads it).</summary>
    public bool MapFuelFollowed { get; private set; }

    double fuelOwed, owedTicks, othersAtMost;
    float fuelSeen = -1f;
    int fuelSeenTick = -1;

    double MaxFuelDemand => Turbine?.Top.Share ?? 1;

    double WearFuel => Turbine == null ? 1
        : TurbineModel.WearFuelFactor(Condition(), PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>().turbineWearFuelPenalty);

    /// <summary>
    /// What flying between tiles burns against the author's rate: 1 with the engines in good order and nothing switched on, more with a
    /// worn hot section, cabin air and what the generator carries. Vehicle Framework reads the figure this goes into both to burn the
    /// fuel and to say how far the aircraft can reach, so the two always agree.
    /// </summary>
    public double WorldFlightFuelShare()
    {
        if (!AircraftFuelFollows || Profile!.Phase(Profile.worldFuelPhase) is not { demand: > 0 } cruise) return 1;
        double demand = BleedRule.Demand(cruise.demand, CabinBleedOn() ? Profile.cabinBleed : 0) + ShaftShare(comp.CruiseGeneratorKw());
        return TurbineModel.FuelShare(Spec.IdleFuelShare, demand, cruise.demand, MaxFuelDemand, WearFuel);
    }

    /// <summary>The fuel the flight framework itself burns each tick while flying on a map (the author's on-map rate), or Vehicle
    /// Framework's own for an aircraft with none; NaN when the profile names a place for it that cannot be read.</summary>
    double MapFuelPerTick(VehiclePawn vehicle, CompFueledTravel fuel)
    {
        if (Profile!.mapFuelRateMember.NullOrEmpty()) return fuel.ConsumptionRatePerTickRaw;
        float own = PhaseReader_Members.Number(vehicle, Profile.mapFuelRateMember!);
        return float.IsNaN(own) || own <= 0f ? double.NaN : own;
    }

    /// <summary>Adds what the engines owe for the ticks this sample stands for, on a map: the author's on-map rate holds while flying
    /// about in good order, and a hover, a climb, an idle, the loads and a worn engine move it from there.</summary>
    void OweFuel(VehiclePawn vehicle, bool running, double demand, int ticks)
    {
        if (!running || ticks <= 0 || !vehicle.Spawned || !AircraftFuelFollows) return;
        CompFueledTravel fuel = vehicle.CompFueledTravel;
        double perTick = MapFuelPerTick(vehicle, fuel);
        FlightPhaseSpec? reference = Profile!.Phase(Profile.mapFuelPhase.NullOrEmpty() ? Profile.worldFuelPhase : Profile.mapFuelPhase);
        if (double.IsNaN(perTick) || reference == null || reference.demand <= 0) return;
        fuelOwed += perTick * ticks * TurbineModel.FuelShare(Spec.IdleFuelShare, demand, reference.demand, MaxFuelDemand, WearFuel);
        owedTicks += ticks;
        // The most everything else can have burned meanwhile: the flight framework's rate and Vehicle Framework's own.
        othersAtMost += ticks * SeenFuelMargin * ((Profile.mapFuelRateMember.NullOrEmpty() ? 0 : perTick) + fuel.ConsumptionRatePerTickRaw);
    }

    /// <summary>
    /// Settles an aircraft's fuel on a map: what its engines owed since the last look, less what the tank is seen to have lost to
    /// everything else meanwhile (Vehicle Framework's burn, the flight framework's), burned or handed back. Reading the tank rather
    /// than each framework's rules means a framework that changes when it burns is never charged twice. Our own burns are told to
    /// <see cref="NoteOwnFuel"/>, so they never count as someone else's.
    /// </summary>
    void ApplyAircraftFuel()
    {
        VehiclePawn vehicle = comp.Vehicle;
        CompFueledTravel? fuel = vehicle.CompFueledTravel;
        double owed = fuelOwed, ticks = owedTicks, atMost = othersAtMost;
        fuelOwed = 0;
        owedTicks = 0;
        othersAtMost = 0;
        MapFuelFollowed = false;
        if (fuel == null) return;
        int now = Find.TickManager.TicksGame;
        bool fresh = fuelSeenTick >= 0 && now - fuelSeenTick <= MaxFuelGapTicks;
        float before = fuelSeen;
        fuelSeen = fuel.Fuel;
        fuelSeenTick = now;
        if (!fresh || ticks <= 0 || !vehicle.Spawned || !AircraftFuelFollows) return;
        bool leaking = fuel.FuelLeaking;
        double seen = Math.Min(Math.Max(0, before - fuel.Fuel), atMost);
        double extra = TurbineModel.FuelCorrection(owed, seen, leaking);
        // What the frameworks burned counts as the engine's on the fuel line, with our correction to it.
        comp.FuelLog.Note(FuelUse.Engine, leaking ? Math.Min(seen, owed) : seen);
        if (extra > 0) VehicleFuel.Burn(vehicle, (float)extra, FuelUse.Engine, comp);
        else if (extra < 0) VehicleFuel.GiveBack(vehicle, (float)-extra, FuelUse.Engine, comp);
        MapFuelFollowed = true;
        if (SeenInPlay.On && Math.Abs(extra) > 0.0005) Seen.Note("vehicles.aircraft-fuel", vehicle, (Phase ?? "?")
            // Amounts, not a ratio (Framework 0.136.1): at lift-off nothing else had burned yet, and the ratio read "x48944".
            + ": engines owed " + owed.ToString("0.###") + ", others burned " + seen.ToString("0.###"));
    }

    /// <summary>Our own code burned this much of the tank (negative: gave it back), so it is not something the tank lost to anyone else.</summary>
    public void NoteOwnFuel(float burned)
    {
        if (fuelSeenTick >= 0) fuelSeen -= burned;
    }
}
