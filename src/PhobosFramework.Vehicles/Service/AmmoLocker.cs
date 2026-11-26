using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The ammunition locker (Framework 0.319.0; docs/development/vehicle-militarised-service-vehicles.md; the owner's card of 26 Nov 2026: rounds
/// only). A node raises <c>Phobos_AmmoLockerKilograms</c>; the cargo figure (<see cref="VehicleStatPart_PhobosSystems"/>) adds that room for
/// the rounds its own guns and dischargers fire, by <see cref="AmmoLockerRule"/>. A round is whatever a fitted turret's ammunition allows, as
/// Vehicle Framework itself decides what to bring a turret (with Combat Extended, its ammunition set's rounds).
/// <para>Cost: the cargo figure is worked out again only when cargo comes or goes on a vehicle with a locker, reading its inventory once.</para>
/// </summary>
public static class AmmoLocker
{
    /// <summary>Whether other cargo had spilled into the locker when the cargo figure was last worked out, by vehicle; derived, never saved.</summary>
    static readonly Dictionary<int, bool> spilledByVehicle = new();

    /// <summary>Whether other cargo had spilled into its locker when its cargo figure was last worked out (for the figure's explanation).</summary>
    public static bool SpilledWhenLastWorkedOut(VehiclePawn vehicle) => spilledByVehicle.TryGetValue(vehicle.thingIDNumber, out bool spilled) && spilled;

    /// <summary>The locker's room, kg; 0 without one.</summary>
    public static float Kilograms(VehiclePawn vehicle) => vehicle.GetStatValue(AircraftUpgradeDefOf.Phobos_AmmoLockerKilograms);

    /// <summary>Whether a thing aboard is a round for one of its fitted turrets.</summary>
    public static bool IsRound(VehiclePawn vehicle, ThingDef def)
    {
        var turrets = vehicle.CompVehicleTurrets?.Turrets;
        if (turrets == null) return false;
        foreach (VehicleTurret turret in turrets)
        {
            if (turret.def?.ammunition?.Allows(def) == true) return true;
        }
        return false;
    }

    /// <summary>What it carries, kg: its rounds, and everything else (people in its cargo too).</summary>
    public static void Split(VehiclePawn vehicle, out float rounds, out float other)
    {
        rounds = 0f;
        other = 0f;
        var things = vehicle.inventory?.innerContainer;
        if (things == null) return;
        foreach (Thing thing in things)
        {
            // As the game weighs an inventory (MassUtility.InventoryMass).
            float mass = thing.GetStatValue(StatDefOf.Mass) * thing.stackCount;
            if (thing is not Pawn && IsRound(vehicle, thing.def)) rounds += mass;
            else other += mass;
        }
    }

    /// <summary>
    /// The room the locker adds to a cargo figure of <paramref name="bedWithoutOffsets"/>, kg. Vehicle Framework adds an upgrade's cargo offset
    /// after every stat part, so the bed counts that offset here too.
    /// </summary>
    public static float Room(VehiclePawn vehicle, float bedWithoutOffsets, out bool spilled)
    {
        spilled = false;
        float locker = Kilograms(vehicle);
        if (locker <= 0f) return 0f;
        float bed = bedWithoutOffsets + vehicle.statHandler.GetStatOffset(VehicleStatDefOf.CargoCapacity);
        Split(vehicle, out float rounds, out float other);
        spilled = AmmoLockerRule.Spilled(locker, other, bed);
        spilledByVehicle[vehicle.thingIDNumber] = spilled;
        float room = (float)AmmoLockerRule.Room(locker, rounds, other, bed);
        if (spilled && Seen.Wants("vehicles.ammo-locker", vehicle, "spilled")) Seen.Note("vehicles.ammo-locker", vehicle, other.ToString("0") + " kg of other cargo", "spilled");
        else if (!spilled && rounds > 0f && Seen.Wants("vehicles.ammo-locker", vehicle, "rounds")) Seen.Note("vehicles.ammo-locker", vehicle, rounds.ToString("0") + " kg of rounds", "rounds");
        return room;
    }

    /// <summary>Cargo came or went: on a vehicle with a locker its cargo figure is worked out again.</summary>
    public static void CargoChanged(VehiclePawn vehicle)
    {
        if (Kilograms(vehicle) > 0f) vehicle.statHandler.MarkStatDirty(VehicleStatDefOf.CargoCapacity);
    }
}
