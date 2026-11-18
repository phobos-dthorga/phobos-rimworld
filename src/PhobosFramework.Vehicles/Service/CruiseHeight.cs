using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Phobos.Framework;
using Phobos.Framework.Rules;
using Phobos.Framework.WorldMap;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The height an aircraft flies a leg between tiles at, and what that does to it (Framework 0.141.0; docs/development/vehicle-flight-over-the-map.md,
/// "A cruising height between tiles"; the rule is the core's CruiseRule). Worked out per leg from the highest ground on the way, the ceiling
/// the engines reach at the aircraft's weight with the engines it has left, and whether anyone aboard lacks oxygen. Vehicle Framework reads the
/// leg's factors through the two figures we already patch: flight speed, once per leg, and the world-flight fuel multiplier, every tick. Nothing
/// is saved: a leg is worked out again when first asked after a load. The player's aircraft only, as the rest of the flight fuel work.
/// </summary>
public static class CruiseHeight
{
    static VehicleSystemsSettings Settings => VehicleSystemsMod.Settings;

    /// <summary>The cruising figures, systems and engine model that judge this aircraft, or null when the rule does not govern it: the
    /// setting is on, it is the player's, the full engine model is on, its engines breathe air, and it has cruising figures.</summary>
    public static CruiseSpec? Spec(VehiclePawn? vehicle, out Powertrain? train)
    {
        train = null;
        if (vehicle == null || !Settings.cruiseHeight || Settings.drivetrainMode != DrivetrainMode.Full) return null;
        if (vehicle.Faction != Faction.OfPlayer) return null;
        if (AirCombat.Systems(vehicle) is not { Usable: true } comp || comp.Powertrain is not { IsAircraft: true, BreathesAir: true } engines) return null;
        CruiseSpec? spec = comp.Props.loadout?.Cruise;
        if (spec == null) return null;
        train = engines;
        return spec;
    }

    /// <summary>Whether the rule governs this aircraft.</summary>
    public static bool Governs(VehiclePawn? vehicle) => Spec(vehicle, out _) != null;

    /// <summary>The ability the cabin oxygen fitting grants (Framework 0.142.0): masks at every seat, fed while the engine runs.</summary>
    public const string CabinOxygenAbility = "CabinOxygen";

    /// <summary>Whether the cabin oxygen fitting is fitted and working now: its electrical load could run and the engine is turning.</summary>
    public static bool CabinOxygen(VehiclePawn vehicle)
    {
        if (AirCombat.Systems(vehicle) is not { Usable: true } comp || !comp.Fittings.Grants(CabinOxygenAbility)) return false;
        return Feeds.Refusal(comp, Feeds.ForAbility(comp, CabinOxygenAbility), "PhobosVehicleSystems_CabinOxygenWhat".Translate().Resolve()) == null;
    }

    /// <summary>
    /// The height above which someone aboard has no oxygen, metres, or NaN when nobody does: a pressurised cabin, a working cabin oxygen
    /// fitting (Framework 0.142.0; owner, 19 Nov 2026), or every crew member on a working oxygen set with no passengers. Without the cabin
    /// fitting, passengers (seats that neither drive nor man a weapon) have no oxygen: the avionics' set is the crew's. Only flesh breathes.
    /// </summary>
    public static double OxygenLine(VehiclePawn vehicle, CruiseSpec spec)
    {
        if (spec.pressurised || CabinOxygen(vehicle)) return double.NaN;
        bool crewOxygen = AirCombat.Carries(vehicle, fit => fit.oxygen) && AirCombat.Works(vehicle, AirCombat.OxygenQuantity);
        List<Pawn>? passengers = vehicle.PawnsByHandlingType.TryGetValue(HandlingType.None, out List<Pawn>? list) ? list : null;
        foreach (Pawn pawn in vehicle.AllPawnsAboard)
        {
            if (pawn.Dead || pawn.RaceProps == null || !pawn.RaceProps.IsFlesh) continue;
            if (!crewOxygen || (passengers != null && passengers.Contains(pawn))) return spec.OxygenLine;
        }
        return double.NaN;
    }

    // ---- The ground on a leg, kept while the world stands ----

    static readonly Dictionary<(PlanetTile, PlanetTile), (float highest, float mountains, bool read)> ground = new();
    static World? groundWorld;
    const int MaxGroundEntries = 256;

    /// <summary>The height a leg must be flown at to clear its ground, metres; NaN where the leg cannot be walked (a tile off the surface).</summary>
    public static double Floor(PlanetTile from, PlanetTile to)
    {
        if (Find.World != groundWorld)
        {
            ground.Clear();
            groundWorld = Find.World;
        }
        if (!ground.TryGetValue((from, to), out var found))
        {
            if (ground.Count >= MaxGroundEntries) ground.Clear();
            bool read = TileAir.HighestOnLeg(from, to, out float highest, out float mountains);
            found = (highest, mountains, read);
            ground[(from, to)] = found;
        }
        return found.read ? CruiseRule.Floor(found.highest, found.mountains) : double.NaN;
    }

    /// <summary>One leg for this aircraft as it is now (weight, engines, people aboard), or null when the rule does not govern it or the leg
    /// cannot be walked.</summary>
    public static CruiseLeg? Leg(VehiclePawn vehicle, PlanetTile from, PlanetTile to, double fuelBurnedBefore = 0) =>
        LegOver(vehicle, Floor(from, to), fuelBurnedBefore);

    /// <summary>A leg over ground that asks this height (ground and clearance), for this aircraft as it is now; null when the rule does not
    /// govern it or the height is unknown.</summary>
    static CruiseLeg? LegOver(VehiclePawn vehicle, double floor, double fuelBurnedBefore = 0)
    {
        if (Spec(vehicle, out Powertrain? train) is not { } spec || train == null || double.IsNaN(floor)) return null;
        double mass = Math.Max(1, train.MassKg() - Math.Max(0, fuelBurnedBefore) * train.FuelKgPerUnit);
        double share = mass / Math.Max(1, train.MaxGrossKg());
        return CruiseRule.Leg(spec.lift, spec.designMetres, spec.ceilingMetres, share, train.WorkingEngineShare, OxygenLine(vehicle, spec), floor);
    }

    /// <summary>The kilograms this aircraft must shed to fly a refused leg, or NaN when no weight would do (it must route round).</summary>
    public static double KilogramsToShed(VehiclePawn vehicle, CruiseLeg leg)
    {
        if (leg.Flyable || Spec(vehicle, out Powertrain? train) is not { } spec || train == null) return 0;
        double allowed = CruiseRule.WeightShareFor(leg.FloorMetres, spec.ceilingMetres, train.WorkingEngineShare) * train.MaxGrossKg();
        // Below its own empty weight nothing can be thrown out: the way over is round.
        return allowed < train.KerbKg ? double.NaN : Math.Max(1, train.MassKg() - allowed);
    }

    // ---- The leg being flown ----

    sealed class Flying
    {
        public AerialVehicleInFlight? Aerial;
        public PlanetTile To = PlanetTile.Invalid;
        public CruiseLeg? Leg;
        /// <summary>The engines working when the leg was last judged, so one lost on the way is noticed (Framework 0.142.0).</summary>
        public int Engines = -1;
    }

    static readonly ConditionalWeakTable<VehiclePawn, Flying> flying = new();

    /// <summary>Set while the destination picker prices a journey leg by leg, so the figures it asks for are the author's, unscaled.</summary>
    public static bool Suspended;

    /// <summary>The leg this aircraft is flying now, worked out when first asked after it begins (or after a load); null on the ground,
    /// circling, on reconnaissance, or where the rule does not govern it.</summary>
    public static CruiseLeg? Current(VehiclePawn? vehicle)
    {
        if (vehicle?.ParentHolder is not AerialVehicleInFlight aerial || vehicle.CompVehicleLauncher is not { inFlight: true }) return null;
        FlightPath? path = aerial.flightPath;
        if (path == null || path.Empty || path.Circling || aerial.recon) return null;
        PlanetTile to = path.First.Tile;
        Flying state = flying.GetOrCreateValue(vehicle);
        if (state.Aerial != aerial || state.To != to)
        {
            state.Aerial = aerial;
            state.To = to;
            state.Leg = Leg(vehicle, aerial.Tile, to);
            state.Engines = WorkingEngines(vehicle);
            if (state.Leg is { } leg) Begun(vehicle, aerial, leg);
        }
        return state.Leg;
    }

    /// <summary>A leg has begun: noted for the test list, and the player told if it is beyond the engines (an engine lost, cargo taken on).</summary>
    static void Begun(VehiclePawn vehicle, AerialVehicleInFlight aerial, CruiseLeg leg)
    {
        string variant = leg.Limit.ToString();
        if (Seen.Wants("aircraft.cruise-height", vehicle, variant))
        {
            Seen.Note("aircraft.cruise-height", vehicle, aerial.Tile + " to " + aerial.flightPath.First.Tile, variant, new[]
            {
                new KeyValuePair<string, double>("metres", leg.HeightMetres),
                new KeyValuePair<string, double>("fuel", leg.FuelFactor),
                new KeyValuePair<string, double>("speed", leg.SpeedFactor),
            });
        }
        if (leg.Flyable) return;
        // A leg found beyond the engines as it begins (an engine lost at the last stop) is flown, struggling, at the height the ground asks, and
        // the player is told (owner, 19 Nov 2026: struggle across); the destination picker refuses such a leg before launch.
        Messages.Message("PhobosVehicleSystems_MsgCruiseBeyond".Translate(vehicle.LabelShort.Named("VEHICLE"), ((float)leg.ShortMetres).ToString("0").Named("METRES")),
            new GlobalTargetInfo(aerial), MessageTypeDefOf.NegativeEvent);
    }

    static int WorkingEngines(VehiclePawn vehicle) => AirCombat.Systems(vehicle)?.Powertrain?.Engines().working ?? -1;

    static readonly AccessTools.FieldRef<AerialVehicleInFlight, float>? Transition =
        AccessTools.Field("Vehicles.World.AerialVehicleInFlight:transition") is { } field
            ? AccessTools.FieldRefAccess<AerialVehicleInFlight, float>(field) : null;
    static readonly Action<AerialVehicleInFlight>? SetSpeed =
        AccessTools.Method("Vehicles.World.AerialVehicleInFlight:SetSpeed") is { } method
            ? AccessTools.MethodDelegate<Action<AerialVehicleInFlight>>(method) : null;
    static readonly List<PlanetTile> legTiles = new();

    /// <summary>
    /// The look at an aircraft of ours in flight, every 250 ticks (Framework 0.142.0; owner, 19 Nov 2026): when it has lost an engine since its
    /// leg was judged, the leg is judged again where it is. It flies on if the engines left still climb over the ground ahead; turns back to set
    /// down at the stop it left if they climb over the ground behind and it can land there; else struggles across. Speed follows at once.
    /// </summary>
    public static void FlightLook(AerialVehicleInFlight flight, VehiclePawn vehicle)
    {
        if (Current(vehicle) is null || !flying.TryGetValue(vehicle, out Flying? state)) return;
        int working = WorkingEngines(vehicle);
        if (working < 0 || working >= state.Engines)
        {
            state.Engines = Math.Max(state.Engines, working);
            return;
        }
        state.Engines = working;
        PlanetTile from = flight.Tile;
        if (!TileAir.WalkLeg(from, state.To, legTiles)) return;
        int here = CruiseRule.TileAlong(Transition != null ? Transition(flight) : 0f, legTiles.Count);
        TileAir.Highest(legTiles, here, legTiles.Count - 1, out float aheadHigh, out float aheadMountains);
        TileAir.Highest(legTiles, 0, here, out float backHigh, out float backMountains);
        CruiseLeg? ahead = LegOver(vehicle, CruiseRule.Floor(aheadHigh, aheadMountains));
        CruiseLeg? back = LegOver(vehicle, CruiseRule.Floor(backHigh, backMountains));
        if (ahead is not { } onward) return;
        bool canSetDown = CargoRefuel.NoLandingReason(from) == null;
        CruiseTurn turn = Settings.showOnly ? CruiseTurn.FlyOn : CruiseRule.AfterEngineLost(onward.Flyable, back?.Flyable == true, canSetDown);
        NamedArgument who = vehicle.LabelShort.Named("VEHICLE");
        string metres = ((float)onward.ShortMetres).ToString("0");
        switch (turn)
        {
            case CruiseTurn.TurnBack:
                // The leg back is judged here, over the ground already crossed; the flight's next leg is that one, so Current keeps it.
                state.To = from;
                state.Leg = back;
                flight.OrderFlyToTiles(new List<FlightNode> { new(from) }, new ArrivalAction_LandToCaravan(vehicle));
                Messages.Message("PhobosVehicleSystems_MsgCruiseTurnBack".Translate(who, metres.Named("METRES")), new GlobalTargetInfo(flight),
                    MessageTypeDefOf.NegativeEvent);
                break;
            case CruiseTurn.Struggle:
                state.Leg = onward;
                SetSpeed?.Invoke(flight);
                Messages.Message("PhobosVehicleSystems_MsgCruiseBeyond".Translate(who, metres.Named("METRES")), new GlobalTargetInfo(flight),
                    MessageTypeDefOf.NegativeEvent);
                break;
            default:
                state.Leg = onward;
                SetSpeed?.Invoke(flight);
                break;
        }
        string variant = "EngineLost" + turn;
        CruiseLeg flown = state.Leg ?? onward;
        if (Seen.Wants("aircraft.cruise-height", vehicle, variant))
        {
            Seen.Note("aircraft.cruise-height", vehicle, working + " engine(s) left", variant, new[]
            {
                new KeyValuePair<string, double>("metres", flown.HeightMetres),
                new KeyValuePair<string, double>("fuel", flown.FuelFactor),
                new KeyValuePair<string, double>("speed", flown.SpeedFactor),
            });
        }
    }

    /// <summary>The factor on the world-flight fuel multiplier for the leg being flown; 1 off a governed leg, while priced by the picker, or with
    /// the player's figures-only setting.</summary>
    public static float FuelFactor(VehiclePawn? vehicle) => Suspended || Settings.showOnly || Current(vehicle) is not { } leg ? 1f : (float)leg.FuelFactor;

    /// <summary>The factor on flight speed for the leg being flown, as <see cref="FuelFactor"/>.</summary>
    public static float SpeedFactor(VehiclePawn? vehicle) => Suspended || Settings.showOnly || Current(vehicle) is not { } leg ? 1f : (float)leg.SpeedFactor;

    // ---- Words ----

    /// <summary>What the leg comes to, in the player's display mode: a line for the destination picker, or null for a leg flown as built.</summary>
    public static string? Describe(VehiclePawn vehicle, CruiseLeg leg)
    {
        DisplayMode mode = Settings.displayMode;
        NamedArgument who = vehicle.LabelShort.Named("VEHICLE");
        if (!leg.Flyable)
        {
            double shed = KilogramsToShed(vehicle, leg);
            return double.IsNaN(shed)
                ? "PhobosVehicleSystems_CruiseRefusedRoute".Translate(who).Resolve()
                : "PhobosVehicleSystems_CruiseRefusedShed".Translate(who, ((float)shed).ToString("0").Named("KILOGRAMS")).Resolve();
        }
        if (leg.Limit == CruiseLimit.Design) return null;
        string why = leg.Limit switch
        {
            CruiseLimit.Ceiling => "PhobosVehicleSystems_CruiseWhyCeiling".Translate().Resolve(),
            CruiseLimit.Oxygen => "PhobosVehicleSystems_CruiseWhyOxygen".Translate().Resolve(),
            _ => "PhobosVehicleSystems_CruiseWhyGround".Translate().Resolve(),
        };
        if (mode == DisplayMode.Plain)
        {
            return leg.FuelPerDistance > 1.005
                ? "PhobosVehicleSystems_CruisePlainCostly".Translate(who, why.Named("WHY")).Resolve()
                : "PhobosVehicleSystems_CruisePlainFine".Translate(who, why.Named("WHY")).Resolve();
        }
        string line = "PhobosVehicleSystems_CruiseFigures".Translate(who, ((float)leg.HeightMetres).ToString("0").Named("METRES"), why.Named("WHY"),
            ((float)leg.FuelPerDistance).ToStringPercent().Named("FUEL"), ((float)leg.SpeedFactor).ToStringPercent().Named("SPEED")).Resolve();
        if (mode != DisplayMode.Engineer) return line;
        return line + "\n" + "PhobosVehicleSystems_CruiseEngineer".Translate(((float)leg.DesignMetres).ToString("0").Named("DESIGN"),
            ((float)leg.CeilingMetres).ToString("0").Named("CEILING"), ((float)leg.FloorMetres).ToString("0").Named("FLOOR"),
            ((float)leg.FuelFactor).ToString("0.00").Named("FUELTICK")).Resolve();
    }
}
