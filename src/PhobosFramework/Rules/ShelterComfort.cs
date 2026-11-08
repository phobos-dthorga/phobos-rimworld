namespace Phobos.Framework.Rules;

/// <summary>How a pawn feels about a shelter's temperature (a vehicle's cab, a pod, a tent), against their own comfort range.</summary>
public enum ShelterStage
{
    /// <summary>Nothing worth a thought: comfortable inside, and outside is comfortable too.</summary>
    None = -1,
    /// <summary>Comfortable inside while it is too cold outside.</summary>
    ShelteredFromCold = 0,
    /// <summary>Comfortable inside while it is too hot outside.</summary>
    ShelteredFromHeat = 1,
    /// <summary>Below the comfort range inside.</summary>
    Chilly = 2,
    /// <summary>Far below the comfort range inside.</summary>
    Freezing = 3,
    /// <summary>Above the comfort range inside.</summary>
    Hot = 4,
    /// <summary>Far above the comfort range inside.</summary>
    Stifling = 5,
}

/// <summary>
/// The rule behind shelter thoughts, game-free: a pawn compares the temperature inside with their comfort range (the game's
/// ComfyTemperatureMin and ComfyTemperatureMax stats, so genes, apparel and traits count), and is glad of a shelter only when it
/// spares them weather outside that range. Thresholds are authored (docs/development/vehicle-crew-and-ground-design.md). The stage
/// numbers match the order of a ThoughtDef's stages.
/// </summary>
public static class ShelterComfort
{
    /// <summary>Degrees beyond the comfort range at which chilly turns to freezing, and hot to stifling (authored).</summary>
    public const double SevereMargin = 10;

    public static ShelterStage Stage(double inside, double outside, double comfyMin, double comfyMax, double severeMargin = SevereMargin)
    {
        if (comfyMax < comfyMin) (comfyMin, comfyMax) = (comfyMax, comfyMin);
        if (inside < comfyMin - severeMargin) return ShelterStage.Freezing;
        if (inside < comfyMin) return ShelterStage.Chilly;
        if (inside > comfyMax + severeMargin) return ShelterStage.Stifling;
        if (inside > comfyMax) return ShelterStage.Hot;
        if (outside < comfyMin) return ShelterStage.ShelteredFromCold;
        if (outside > comfyMax) return ShelterStage.ShelteredFromHeat;
        return ShelterStage.None;
    }
}
