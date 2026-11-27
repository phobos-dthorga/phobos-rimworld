using Phobos.Framework.Vehicles.Rules;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// How dirty the fuel in a vehicle's tank is (Framework 0.365.0; docs/development/vehicle-fuel-filter.md, FF5; the owner's answer of 28 Nov
/// 2026: stale depot fuel is dirty fuel). The share is the fuel system's <see cref="Quantity"/>, saved with the vehicle's other figures; this
/// keeps it in step with the tank. Fuel from a stale depot brings dirt (<see cref="FilledFromDepot"/>); any other fuel that arrives is clean
/// and thins it, seen at each update as the tank rising. A dirtier tank clogs the fuel filter faster.
/// </summary>
public sealed class FuelDirt
{
    public const string Quantity = "fuelDirt";
    /// <summary>A rise in the tank smaller than this is not a refuel (rounding, a dev action's nudge).</summary>
    const float RefuelAbove = 0.01f;

    readonly CompPhobosVehicleSystems comp;
    /// <summary>The tank as last seen; NaN until the first look this session. Derived, never saved.</summary>
    float lastFuel = float.NaN;

    public FuelDirt(CompPhobosVehicleSystems comp) => this.comp = comp;

    bool Kept(out CompFueledTravel? tank, out float dirt)
    {
        tank = comp.Vehicle?.CompFueledTravel;
        dirt = 0f;
        return tank != null && VehicleSystemsMod.Settings.fuelFilters && comp.TryGetQuantity(Quantity, out dirt);
    }

    /// <summary>At each update: fuel that arrived since the last look by any way but a depot is clean, and thins what is there.</summary>
    public void Update()
    {
        if (!Kept(out CompFueledTravel? tank, out float dirt)) return;
        float now = tank!.Fuel;
        if (!float.IsNaN(lastFuel) && now > lastFuel + RefuelAbove && dirt > 0f)
        {
            comp.SetQuantity(Quantity, (float)FuelFilterRule.Mix(lastFuel, dirt, now - lastFuel, 0));
        }
        lastFuel = now;
    }

    /// <summary>Called by the depot code once it has poured <paramref name="added"/> units of fuel <paramref name="ageDays"/> days old into
    /// the tank: the depot's dirt joins the tank's, weighed by amount.</summary>
    public void FilledFromDepot(float added, double ageDays)
    {
        if (added <= 0f || !Kept(out CompFueledTravel? tank, out float dirt)) return;
        float before = tank!.Fuel - added;
        // Anything else poured in since the last look is settled first, as clean fuel.
        if (!float.IsNaN(lastFuel) && before > lastFuel + RefuelAbove) dirt = (float)FuelFilterRule.Mix(lastFuel, dirt, before - lastFuel, 0);
        double depotDirt = FuelFilterRule.DepotDirt(ageDays, DepotPressureRule.FreshDays);
        float mixed = (float)FuelFilterRule.Mix(before, dirt, added, depotDirt);
        comp.SetQuantity(Quantity, mixed);
        lastFuel = tank.Fuel;
        if (depotDirt > 0 && Seen.Wants("vehicles.fuel-dirt", comp.Vehicle)) Seen.Note("vehicles.fuel-dirt", comp.Vehicle, mixed.ToStringPercent());
    }
}
