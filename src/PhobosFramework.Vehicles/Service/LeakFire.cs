using System.Collections.Generic;
using Phobos.Framework;
using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A leaking fuel tank feeds a fire on its vehicle (Framework 0.245.0; docs/development/vehicle-fire.md, FR4; the owner's card of 23 Nov 2026).
/// The fire already wears the fuel tank (FR3), and Vehicle Framework already leaks fuel from a tank worn below its own threshold; here, while
/// the vehicle burns and its tank leaks with fuel in it, the leak catches as a pool of fire on the ground beneath it, which burns there by
/// the game's own rules: it spreads, relights the vehicle after an extinguisher, and reaches whatever stands near. Each pool takes fuel from
/// the tank. Any faction's vehicle. A setting, off: risk high, the agent's grade (a spreading ground fire can take people and other vehicles
/// nearby). Reads nothing unless the vehicle burns.
/// </summary>
public static class LeakFire
{
    static readonly float RareTickHours = GenTicks.TickRareInterval / (float)ElapsedRule.TicksPerHour;

    /// <summary>When each vehicle's player was last told, so a burning truck sends one message, not one a pool. Derived, never saved.</summary>
    static readonly Dictionary<int, int> toldAt = new();
    const int TellEveryTicks = GenDate.TicksPerHour;

    /// <summary>Called on the vehicle's rare tick, on either tier.</summary>
    public static void RareTick(CompPhobosVehicleSystems comp)
    {
        VehicleSystemsSettings settings = VehicleSystemsMod.Settings;
        if (!settings.fuelFeedsFire || settings.showOnly) return;
        VehiclePawn vehicle = comp.Vehicle;
        if (vehicle == null || !vehicle.Spawned) return;
        float fire = comp.FireShare;
        if (fire <= 0f) return;
        CompFueledTravel? fuel = vehicle.CompFueledTravel;
        if (fuel == null || !fuel.FuelLeaking || fuel.EmptyTank) return;

        PhobosFireFightingRulesDef rules = PhobosFireFightingRulesDef.Rules;
        if (!Rand.Chance((float)FireFightingRule.ChanceIn(rules.leakCatchesPerHour * fire, RareTickHours))) return;
        Map map = vehicle.Map;
        IntVec3 cell = vehicle.OccupiedRect().RandomCell;
        if (!cell.InBounds(map) || !FireUtility.TryStartFireIn(cell, map, rules.leakFireSize, vehicle)) return;
        fuel.ConsumeFuel(Mathf.Min(rules.leakFuelPerPool, fuel.Fuel));

        if (vehicle.Faction == Faction.OfPlayer)
        {
            int now = Find.TickManager.TicksGame;
            if (!toldAt.TryGetValue(vehicle.thingIDNumber, out int told) || now - told >= TellEveryTicks)
            {
                toldAt[vehicle.thingIDNumber] = now;
                Messages.Message("PhobosFireFighting_LeakCaught".Translate(vehicle.LabelShort.Named("VEHICLE")), new TargetInfo(cell, map),
                    MessageTypeDefOf.NegativeEvent);
            }
        }
        if (Seen.Wants("vehicles.leak-fire", vehicle))
        {
            Seen.Note("vehicles.leak-fire", vehicle, "fire " + fire.ToStringPercent(), values: new[] { new KeyValuePair<string, double>("fuel", fuel.Fuel) });
        }
    }
}
