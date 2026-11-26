using System.Collections.Generic;
using RimWorld;
using Vehicles;

namespace Phobos.VehicleSystems;

/// <summary>Stats an upgrade node raises to switch on a system of ours on one vehicle (Framework 0.150.0; AircraftUpgrades.xml).</summary>
[DefOf]
public static class AircraftUpgradeDefOf
{
    public static VehicleStatDef Phobos_SlingKilograms = null!;
    public static VehicleStatDef Phobos_InfraredJammer = null!;
    /// <summary>Chaff and flare dispensers fitted by a node, where the loadout's come only that way (Framework 0.316.0).</summary>
    public static VehicleStatDef Phobos_Countermeasures = null!;
    public static VehicleStatDef Phobos_TerrainRadar = null!;
    public static VehicleStatDef Phobos_InletBarrierLoss = null!;
    public static VehicleStatDef Phobos_KitKilograms = null!;
    /// <summary>An ammunition locker's room, kg, for the vehicle's own rounds only (Framework 0.319.0; <see cref="AmmoLocker"/>).</summary>
    public static VehicleStatDef Phobos_AmmoLockerKilograms = null!;
    /// <summary>Grip on soft ground a node adds (Framework 0.197.0: a tractor's dual wheels), on top of the tyre fittings' factor.</summary>
    public static VehicleStatDef Phobos_SoftGroundGripBonus = null!;

    static AircraftUpgradeDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(AircraftUpgradeDefOf));
}

/// <summary>
/// Listed on an upgrade node that changes one of our systems (a cargo hook's rating, an inlet barrier's power loss, a kit's weight): when the
/// node is fitted or taken off, the vehicle's systems are built again with it, as they are when a fitting comes or goes. Vehicle Framework's
/// own stat change does not reach the drivetrain model, which reads its figures once when built. Saves nothing.
/// </summary>
public class PhobosSystemsUpgrade : Upgrade
{
    public override bool UnlockOnLoad => true;

    public override void Unlock(VehiclePawn vehicle, bool unlockingPostLoad) => Rebuild(vehicle, unlockingPostLoad ? "loaded" : "fitted");

    public override void Refund(VehiclePawn vehicle) => Rebuild(vehicle, "removed");

    void Rebuild(VehiclePawn vehicle, string how)
    {
        (CompPhobosVehicleSystems.Of(vehicle) ?? vehicle.GetComp<CompPhobosVehicleSystems>())?.Notify_FittingsChanged();
        string key = node?.key ?? "";
        if (Seen.Wants("vehicles.upgrade-rebuilds-systems", vehicle, key)) Seen.Note("vehicles.upgrade-rebuilds-systems", vehicle, how, key);
    }
}

/// <summary>
/// Listed on an upgrade node after the turret upgrade that adds a weapon no seat works (the Osprey's belly gun): switches the weapon's
/// automatic aim on as it is fitted (Framework 0.163.0; fault-read W2 of 19 Nov 2026). Vehicle Framework starts a player's weapon with it
/// off and switches it on only for other factions, so a gun described as firing by itself did nothing until its button was found. Only
/// at fitting, never after a load: the player's later choice, which Vehicle Framework saves, stands. Saves nothing.
/// </summary>
public class PhobosAutoTargetUpgrade : Upgrade
{
    /// <summary>The keys of the weapons to switch on, as their turret entries name them.</summary>
    public List<string> turretKeys = new();

    public override bool UnlockOnLoad => false;

    public override void Unlock(VehiclePawn vehicle, bool unlockingPostLoad)
    {
        if (unlockingPostLoad || vehicle.CompVehicleTurrets?.Turrets is not { } turrets) return;
        foreach (VehicleTurret turret in turrets)
        {
            if (turretKeys.Contains(turret.key)) turret.AutoTarget = true;
        }
    }

    public override void Refund(VehiclePawn vehicle)
    {
    }
}
