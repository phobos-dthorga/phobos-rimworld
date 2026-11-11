using System.Collections.Generic;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// What a fitting or a tune changes, in one line (Framework 0.87.0; the presentation record's P3), from the Def's own figures, never typed:
/// "engine power ×1.2, fuel burned ×1.1", or in Plain mode "engine power up, fuel burned up". A figure without a player's label is left
/// out. Reads Defs; changes nothing.
/// </summary>
public static class FittingText
{
    public static string? Effects(PhobosVehicleFittingDef fitting, bool plain)
    {
        var parts = new List<string>();
        Add(parts, "PhobosVehicleSystems_TuneFigurePower".Translate(), fitting.powerFactor, plain);
        Add(parts, "PhobosVehicleSystems_TuneFigureFuel".Translate(), fitting.fuelFactor, plain);
        foreach (StatFactorSpec spec in fitting.statFactors)
        {
            if (spec.stat != null) Add(parts, spec.stat.label, spec.factor, plain);
        }
        foreach (ParameterFactorSpec spec in fitting.parameterFactors) Add(parts, spec.label, spec.factor, plain);
        return Join(parts);
    }

    public static string? Effects(PhobosVehicleTuneDef tune, bool plain)
    {
        if (tune.isDefault) return "PhobosVehicleSystems_EffectAsBuilt".Translate().Resolve();
        var parts = new List<string>();
        Add(parts, "PhobosVehicleSystems_TuneFigurePower".Translate(), tune.powerFactor, plain);
        Add(parts, "PhobosVehicleSystems_TuneFigureFuel".Translate(), VehicleFittings.TuneFuelFactor(tune), plain);
        Add(parts, "PhobosVehicleSystems_EffectFinalDrive".Translate(), tune.finalDriveFactor, plain);
        foreach (ParameterFactorSpec spec in tune.parameterFactors) Add(parts, spec.label, spec.factor, plain);
        return Join(parts);
    }

    static void Add(List<string> parts, string label, float factor, bool plain)
    {
        // A factor of 0 switches something off (the block heater's "absent"): its description says so better than a figure.
        if (label.NullOrEmpty() || factor == 1f || factor <= 0f) return;
        parts.Add(!plain
            ? "PhobosVehicleSystems_TuneFigure".Translate(label.Named("LABEL"), factor.ToString("0.###").Named("FACTOR")).Resolve()
            : factor > 1f
                ? "PhobosVehicleSystems_EffectUp".Translate(label.Named("LABEL")).Resolve()
                : "PhobosVehicleSystems_EffectDown".Translate(label.Named("LABEL")).Resolve());
    }

    static string? Join(List<string> parts) => parts.Count == 0 ? null : string.Join(", ", parts).CapitalizeFirst();
}
