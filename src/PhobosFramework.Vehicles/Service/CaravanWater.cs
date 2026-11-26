using RimWorld.Planet;

namespace Phobos.VehicleSystems;

/// <summary>
/// The water a caravan carries in tanks (Framework 0.216.0; docs/development/vehicle-trailers.md, "Water on the road"): a bowser or a shower
/// trailer in tow, a field kitchen's own tank, a tanker in the line. Water only means something with a mod that gives it meaning, so the
/// source is registered by that mod's tie-in (Dubs Bad Hygiene's); without one nothing here holds or takes any, and nothing asks for water.
/// </summary>
public static class CaravanWater
{
    /// <summary>What a tie-in provides: how much water a caravan's tanks hold, and taking some.</summary>
    public interface ISource
    {
        float Litres(Caravan caravan);

        /// <summary>Takes up to this much, from the kitchen's own tank first or last. Returns what came.</summary>
        float Take(Caravan caravan, float litres, bool kitchenFirst);

        /// <summary>Puts up to this much untreated water from the ground into the caravan's water tanks (Framework 0.317.0: a water bore). Returns
        /// what went in.</summary>
        float PutUntreated(Caravan caravan, float litres);

        /// <summary>Room left in the caravan's water tanks, in litres.</summary>
        float Room(Caravan caravan);
    }

    static ISource? source;

    /// <summary>Whether anything gives water a meaning: without a source, a field kitchen's cooks ask for none.</summary>
    public static bool HasSource => source != null;

    public static void Register(ISource waterSource) => source = waterSource;

    public static float Litres(Caravan caravan) => source?.Litres(caravan) ?? 0f;

    public static float Take(Caravan caravan, float litres, bool kitchenFirst) => litres > 0f ? source?.Take(caravan, litres, kitchenFirst) ?? 0f : 0f;

    public static float PutUntreated(Caravan caravan, float litres) => litres > 0f ? source?.PutUntreated(caravan, litres) ?? 0f : 0f;

    public static float Room(Caravan caravan) => source?.Room(caravan) ?? 0f;
}

/// <summary>
/// A vehicle's own store of water (Framework 0.232.0: the crew water tank, with Dubs Bad Hygiene), read by the Fleet board for a tank running
/// low. Litres and the store's size; a store not fitted holds 0.
/// </summary>
public interface IVehicleWaterStore
{
    float Litres { get; }

    float Capacity { get; }
}
