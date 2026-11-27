using Phobos.Framework;
using Phobos.Framework.Defs;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Rough landings and the autopilot (avionics A3 and A4; aircraft service-life upgrades, 21 Nov 2026; docs/development/vehicle-avionics.md).
/// An aircraft that lands where its crew cannot see to land (poor visibility with its instruments out), or with its flight controls shot about,
/// may land hard: its landing gear, or its airframe where it has none, takes wear through the hard-landing rule, and the player is told. A
/// working autopilot halves the chance. Every kind of landing is noticed on the aircraft's rare tick, never inside its spawning: one that flies
/// over the map by itself (our own flight, the fixed-wing and VTOL frameworks) is seen to go from the air to the ground, and one that arrives
/// from the world map (Vehicle Framework's own aircraft) is marked on the way and judged once it is down. Any faction's aircraft. Behind the
/// "Rough landings" setting (moderate risk, on). Saves only the arrival mark on the vehicle's systems.
/// </summary>
public static class RoughLandings
{
    static bool faulted;

    /// <summary>The aircraft's rare tick: notices a landing since the last look.</summary>
    public static void Watch(CompPhobosVehicleSystems comp)
    {
        if (faulted || comp.Props.loadout?.kind != "Aircraft") return;
        VehiclePawn vehicle = comp.Vehicle;
        if (!vehicle.Spawned)
        {
            comp.WasAirborneOnMap = false;
            return;
        }
        try
        {
            bool airborne = OnMapFlight.Airborne(vehicle);
            bool landed = false;
            if (comp.ArrivingFromWorld)
            {
                // Arrived in the air (a jet flying in from the map's edge): its touchdown comes later and is seen below.
                comp.ArrivingFromWorld = false;
                landed = !airborne;
            }
            else if (comp.WasAirborneOnMap && !airborne) landed = true;
            comp.WasAirborneOnMap = airborne;
            if (landed) Judge(comp);
        }
        catch (System.Exception error)
        {
            faulted = true;
            VehicleSystemsMod.Logger.Error("could not judge an aircraft's landing, so every landing is smooth until the game restarts: " + error);
        }
    }

    static void Judge(CompPhobosVehicleSystems comp)
    {
        VehiclePawn vehicle = comp.Vehicle;
        if (!VehicleSystemsMod.Settings.roughLandings || VehicleSystemsMod.Settings.showOnly || !comp.Usable) return;
        PhobosAirCombatRulesDef rules = PhobosRulesDef.Current<PhobosAirCombatRulesDef>();
        bool poor = When_PoorVisibility.Poor(vehicle);
        bool instruments = AirCombat.Works(vehicle, AirCombat.AttitudeQuantity);
        float control = vehicle.GetStatValue(VehicleStatDefOf.FlightControl);
        bool autopilot = AirCombat.Works(vehicle, AirCombat.AutopilotQuantity);
        bool darkUnlit = DarkUnlit(comp);
        double chance = RoughLandingRule.Chance(poor, instruments, control, autopilot, rules.roughLandingChance, rules.roughLandingControlBelow,
            rules.autopilotRoughLandingFactor, darkUnlit);
        if (chance <= 0) return;
        string variant = darkUnlit ? "dark" : autopilot ? "autopilot" : "manual";
        if (!Rand.Chance((float)chance))
        {
            if (Seen.Wants("aircraft.rough-landing", vehicle, "smooth")) Seen.Note("aircraft.rough-landing", vehicle, variant: "smooth");
            return;
        }
        comp.WearLanding(rules.roughLandingWear);
        // Framework 0.321.0: and seen: tyre smoke and dust at the wheels.
        VehicleMotionFx.Touchdown(vehicle);
        string why = darkUnlit ? "dark" : poor && !instruments ? "blind" : "controls";
        if (Seen.Wants("aircraft.rough-landing", vehicle, variant)) Seen.Note("aircraft.rough-landing", vehicle, why, variant);
        if (vehicle.Faction == Faction.OfPlayer)
        {
            string key = darkUnlit ? "PhobosAirCombat_RoughLandingDark"
                : poor && !instruments ? "PhobosAirCombat_RoughLandingBlind" : "PhobosAirCombat_RoughLandingControls";
            Messages.Message(key.Translate(vehicle.LabelShort.Named("VEHICLE")), vehicle, MessageTypeDefOf.NegativeEvent);
        }
    }

    /// <summary>
    /// Down in the dark on unlit ground (Framework 0.366.0, the vehicle standard's batch 4; the owner's card of 28 Nov 2026, "Dark and unlit =
    /// risky"): the sky is dark where it stands, no landing lights burn, and nothing lights the ground under it (a lamp, a lit pad, its own
    /// work lamps). The player's aircraft by its switch and power; another faction's crew is taken to land with its lights on, where it has
    /// them (the light tier keeps no switch).
    /// </summary>
    static bool DarkUnlit(CompPhobosVehicleSystems comp)
    {
        VehiclePawn vehicle = comp.Vehicle;
        if (!When_Dark.IsDark(vehicle) || vehicle.Map is not { } map) return false;
        bool lights = comp.Tier == Phobos.Framework.Rules.DetailTier.Full ? comp.Beams.On : comp.Beams.Fitted;
        if (lights) return false;
        return map.glowGrid.GroundGlowAt(vehicle.Position) < When_Dark.DefaultDarkBelow;
    }
}
