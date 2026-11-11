using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// What a fitting, a tune, or a part's fitting and tune together change, in one line (Framework 0.87.0, combined in 0.88.0; the
/// presentation record's P3), from the figures the fittings service works out (FitFigures), never typed: "engine power ×1.2, fuel burned
/// ×1.1", or in Plain mode "engine power up, fuel burned up". A figure without a player's label is left out. Reads; changes nothing.
/// </summary>
public static class FittingText
{
    public static string? Effects(PhobosVehicleFittingDef fitting, bool plain) =>
        Effects(FitFigures.Of(new[] { (fitting, 1) }, Enumerable.Empty<PhobosVehicleTuneDef>()), plain);

    public static string? Effects(PhobosVehicleTuneDef tune, bool plain) =>
        tune.isDefault ? "PhobosVehicleSystems_EffectAsBuilt".Translate().Resolve() : Effects(FitFigures.Of(Enumerable.Empty<(PhobosVehicleFittingDef, int)>(), new[] { tune }), plain);

    /// <summary>The figures as one line, or null when nothing a player can name changes.</summary>
    public static string? Effects(FitFigures figures, bool plain)
    {
        var parts = new List<string>();
        Add(parts, "PhobosVehicleSystems_TuneFigurePower".Translate(), figures.Power, plain);
        Add(parts, "PhobosVehicleSystems_TuneFigureFuel".Translate(), figures.TunedFuel, plain);
        Add(parts, "PhobosVehicleSystems_EffectFinalDrive".Translate(), figures.FinalDrive, plain);
        foreach (KeyValuePair<Vehicles.VehicleStatDef, float> stat in figures.Stats) Add(parts, stat.Key.label, stat.Value, plain);
        foreach (KeyValuePair<(string system, string name), float> parameter in figures.Parameters)
        {
            if (figures.ParameterLabels.TryGetValue(parameter.Key, out string? label)) Add(parts, label, parameter.Value, plain);
        }
        return parts.Count == 0 ? null : string.Join(", ", parts).CapitalizeFirst();
    }

    /// <summary>
    /// A part row's line in the fittings window (Framework 0.88.0): what its fitting and the tune landing on it give together, named by
    /// both ("Power tune + turbocharger: ..."), or "as built" for an engine or gearbox with neither; null for any other part with no fitting.
    /// </summary>
    public static string? RowLine(PhobosVehicleFittingDef? fitting, PhobosVehicleTuneDef? tune, FitFigures figures, bool plain)
    {
        var sources = new List<string>();
        if (tune != null && !tune.isDefault) sources.Add(tune.LabelCap);
        if (fitting != null) sources.Add(fitting.label);
        if (sources.Count == 0) return tune != null ? "PhobosVehicleSystems_EffectAsBuilt".Translate().Resolve() : null;
        string? effects = Effects(figures, plain);
        return effects == null ? null : "PhobosVehicleSystems_EffectSources".Translate(string.Join(" + ", sources).Named("SOURCES"), effects.Named("EFFECTS")).Resolve();
    }

    static void Add(List<string> parts, string? label, double factor, bool plain)
    {
        // A factor of 0 switches something off (the block heater's "absent"): its description says so better than a figure.
        if (label.NullOrEmpty() || System.Math.Abs(factor - 1) < 1e-4 || factor <= 0) return;
        parts.Add(!plain
            ? "PhobosVehicleSystems_TuneFigure".Translate(label.Named("LABEL"), factor.ToString("0.###").Named("FACTOR")).Resolve()
            : factor > 1
                ? "PhobosVehicleSystems_EffectUp".Translate(label.Named("LABEL")).Resolve()
                : "PhobosVehicleSystems_EffectDown".Translate(label.Named("LABEL")).Resolve());
    }
}
