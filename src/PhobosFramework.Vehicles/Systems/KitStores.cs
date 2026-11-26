using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Something on a vehicle that holds what a fitting's kit carries with it, by the name in the fitting's <c>kitCarries</c> (Framework 0.334.0:
/// the extinguisher's charges, which live on the fire-fighting comp rather than in a system's quantities). Any comp on the vehicle may take
/// part, another mod's too. The share is always nothing to whole, so a kit's own <c>CompPhobosCharges</c> can hold it.
/// </summary>
public interface IPhobosKitStore
{
    /// <summary>What the store of this name holds now, nothing to whole; false when this comp keeps no such store.</summary>
    bool TryGetKitShare(string store, out float share);

    /// <summary>Sets the store of this name from a kit just fitted; false when this comp keeps no such store.</summary>
    bool TrySetKitShare(string store, float share);
}

/// <summary>
/// Where a fitting's carried share is read and written (Framework 0.334.0): first any comp on the vehicle that keeps a store of that name,
/// then the vehicle's system quantity of that name (the house battery's houseCharge, Framework 0.333.0).
/// </summary>
public static class KitStores
{
    public static bool TryGet(CompPhobosVehicleSystems comp, string store, out float share)
    {
        foreach (ThingComp other in comp.Vehicle.AllComps)
        {
            if (other is IPhobosKitStore kit && kit.TryGetKitShare(store, out share)) return true;
        }
        return comp.TryGetQuantity(store, out share);
    }

    public static void Set(CompPhobosVehicleSystems comp, string store, float share)
    {
        foreach (ThingComp other in comp.Vehicle.AllComps)
        {
            if (other is IPhobosKitStore kit && kit.TrySetKitShare(store, share)) return;
        }
        comp.SetQuantity(store, share);
    }
}
