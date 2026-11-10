using Verse;

namespace Phobos.VehicleSystems;

/// <summary>A share of health or efficiency in a word, for the Plain display mode: the Health tab and the rig's diagnosis say the same.</summary>
public static class HealthWords
{
    public static string Of(float share) =>
        (share >= 0.999f ? "PhobosVehicleSystems_HealthWordGood" : share >= 0.7f ? "PhobosVehicleSystems_HealthWordWorn"
            : share >= 0.4f ? "PhobosVehicleSystems_HealthWordPoor" : share > 0f ? "PhobosVehicleSystems_HealthWordBad" : "PhobosVehicleSystems_HealthDestroyed").Translate();
}
