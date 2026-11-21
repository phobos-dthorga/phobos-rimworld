using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Cooking at a field kitchen in a caravan (docs/development/vehicle-field-kitchen.md): which way a caravan cooks tonight, and how many meals
/// its cooks, food and fuel allow. Game-free and tested; every figure is the kitchen Def's, authored.
/// </summary>
public static class CaravanCookRule
{
    /// <summary>The way a caravan cooks.</summary>
    public enum Way
    {
        /// <summary>It does not cook now.</summary>
        None,
        /// <summary>Resting for the night: every cook at the whole kitchen.</summary>
        Camp,
        /// <summary>Driving through the night: the kettle simmers on the move, tended by riders who are not driving.</summary>
        Simmer,
        /// <summary>Driving through the night, with meal halts chosen: it stops to cook as at camp, then drives on.</summary>
        Halt,
    }

    /// <summary>
    /// Which way a caravan cooks now. A caravan at its night's rest cooks as at camp whatever was chosen; one driving on through the night simmers,
    /// or halts where the player chose halts; by day, or standing for any other reason, it does not cook.
    /// </summary>
    public static Way WayFor(bool nightResting, bool drivingOnAtNight, bool wantsHalts)
    {
        if (nightResting) return Way.Camp;
        if (!drivingOnAtNight) return Way.None;
        return wantsHalts ? Way.Halt : Way.Simmer;
    }

    /// <summary>The most meals one cook makes: the kitchen's figure, times the share the way of cooking allows (1 at camp).</summary>
    public static int PerCook(int mealsPerCook, double share) =>
        Math.Max(0, (int)Math.Floor(mealsPerCook * Math.Clamp(share, 0.0, 1.0) + 1e-6));

    /// <summary>
    /// The meals made: the least of what the cooks can make, what the raw food gives, what the fuel allows and what the water allows. Fuel
    /// or water is not counted where a meal takes none (a kitchen asks for water only where something gives water a meaning).
    /// </summary>
    public static int Meals(int cooks, int perCook, double nutrition, double nutritionPerMeal, double fuel, double fuelPerMeal, double water = 0, double waterPerMeal = 0)
    {
        if (cooks <= 0 || perCook <= 0 || nutritionPerMeal <= 0) return 0;
        int byCooks = cooks * perCook;
        int byFood = (int)Math.Floor(nutrition / nutritionPerMeal + 0.0001);
        int byFuel = fuelPerMeal > 0 ? (int)Math.Floor(fuel / fuelPerMeal + 1e-6) : int.MaxValue;
        int byWater = waterPerMeal > 0 ? (int)Math.Floor(water / waterPerMeal + 1e-6) : int.MaxValue;
        return Math.Max(0, Math.Min(Math.Min(byCooks, byFood), Math.Min(byFuel, byWater)));
    }
}
