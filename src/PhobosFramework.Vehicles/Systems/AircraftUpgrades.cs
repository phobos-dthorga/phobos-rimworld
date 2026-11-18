using RimWorld;
using Vehicles;

namespace Phobos.VehicleSystems;

/// <summary>Stats an upgrade node raises to switch on a system of ours on one vehicle (Framework 0.150.0; AircraftUpgrades.xml).</summary>
[DefOf]
public static class AircraftUpgradeDefOf
{
    public static VehicleStatDef Phobos_SlingKilograms = null!;
    public static VehicleStatDef Phobos_InfraredJammer = null!;
    public static VehicleStatDef Phobos_TerrainRadar = null!;
    public static VehicleStatDef Phobos_InletBarrierLoss = null!;
    public static VehicleStatDef Phobos_KitKilograms = null!;

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
