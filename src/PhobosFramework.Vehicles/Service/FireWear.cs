using System.Collections.Generic;
using Phobos.Framework;
using Phobos.Framework.Defs;
using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Fire wearing a vehicle's parts (Framework 0.221.0; docs/development/vehicle-fire.md, FR2, FR3, FR5, FR6; owner, 22 Nov 2026: "all
/// three", and fireproof vehicles' "soft parts only"). One step on the rare tick for every vehicle, full and lite tier alike, in place of
/// the flat-rate wear stages the systems' Defs had for the player's vehicles only:
/// - each role in the rules wears by the size of the fire on the vehicle (FR2), more roles than before (FR3);
/// - the soft parts of a vehicle that cannot catch fire wear from a ground fire under it (FR5, a setting, on);
/// - other factions' vehicles burn as well (FR6, a setting, on);
/// - a fire shorts the wiring, as a hit on the battery does, on the full tier, where conditions are kept.
/// Wear goes through <see cref="CompPhobosVehicleSystems.WearRole"/>, outside any armour step. Reads nothing unless the vehicle burns or
/// stands in fire.
/// </summary>
public static class FireWear
{
    static readonly float RareTickHours = GenTicks.TickRareInterval / (float)ElapsedRule.TicksPerHour;
    const string ElectricalSystem = "PhobosVehicleSystem_Electrical";

    static PhobosFireWearRulesDef Rules => PhobosRulesDef.Current<PhobosFireWearRulesDef>();

    /// <summary>Called on the vehicle's rare tick; <paramref name="lite"/> for another faction's vehicle on the lite tier.</summary>
    public static void RareTick(CompPhobosVehicleSystems comp, bool lite)
    {
        VehicleSystemsSettings settings = VehicleSystemsMod.Settings;
        VehiclePawn vehicle = comp.Vehicle;
        if (vehicle == null || !vehicle.Spawned || settings.showOnly || (lite && !settings.fireWearOthers)) return;
        float onVehicle = comp.FireShare;
        bool canCatch = vehicle.FlammableNow;
        float under = !canCatch && settings.fireWearUnderFireproof ? GroundFireUnder(vehicle) : 0f;
        if (onVehicle <= 0f && under <= 0f) return;

        PhobosFireWearRulesDef rules = Rules;
        foreach (PhobosFireWearRole role in rules.roles)
        {
            double felt = FireWearRule.FireFelt(onVehicle, under, role.soft, canCatch, settings.fireWearUnderFireproof);
            double share = FireWearRule.SharePerHour(role.sharePerHour, felt, rules.leastShareOfRate) * RareTickHours;
            if (share > 0) comp.WearRole(role.role, 0f, (float)share, shedsMetal: false);
        }
        if (!lite && onVehicle > 0f && rules.wiringCondition != null && rules.wiringPerHour > 0f)
        {
            float shorted = (float)FireWearRule.SharePerHour(rules.wiringPerHour, onVehicle, rules.leastShareOfRate) * RareTickHours;
            comp.AddCondition(rules.wiringRole, rules.wiringCondition, shorted, ElectricalSystem,
                Phobos.Framework.Persistence.CauseNote.Words("PhobosVehicleSystems_CauseFire", vehicle.Tile));
        }
        if (Seen.Wants("vehicles.fire-wear", vehicle, Variant(lite, onVehicle)))
        {
            Seen.Note("vehicles.fire-wear", vehicle, "fire " + onVehicle.ToStringPercent() + ", under " + under.ToStringPercent(), Variant(lite, onVehicle),
                new[] { new KeyValuePair<string, double>("fire", onVehicle), new KeyValuePair<string, double>("under", under) });
        }
    }

    static string Variant(bool lite, float onVehicle) => onVehicle <= 0f ? "fireproof" : lite ? "other-faction" : "own";

    /// <summary>The largest ground fire on any cell the vehicle stands on, as a share of the largest a fire grows.</summary>
    static float GroundFireUnder(VehiclePawn vehicle)
    {
        Map map = vehicle.Map;
        float biggest = 0f;
        foreach (IntVec3 cell in vehicle.OccupiedRect())
        {
            if (!cell.InBounds(map)) continue;
            List<Thing> things = map.thingGrid.ThingsListAtFast(cell);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is Fire { parent: null } fire && fire.fireSize > biggest) biggest = fire.fireSize;
            }
        }
        return Mathf.Clamp01(biggest / Fire.MaxFireSize);
    }
}
