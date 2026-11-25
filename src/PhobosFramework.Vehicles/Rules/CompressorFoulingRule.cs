using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// A turbine's compressor fouling, and the wash that clears it (Framework 0.300.0; the owner's cards of 26 Nov 2026,
/// docs/development/compressor-wash.md). Dust, grit and salt drawn in with the air coat the compressor's blades, so it moves less air and
/// the turbine gives less for the same fuel and runs hotter; a water wash rinses it off. Fouling is a share from clean (0) to fully fouled
/// (1). It grows with the engine's running hours, faster on rough ground in step with the intake's exposure to grit (the loadout's
/// intakeDebris figure that step 10's grit rule reads), and stops at 1, so a turbine never washed is weaker but never stopped by it.
/// Game-free.
/// </summary>
public static class CompressorFoulingRule
{
    /// <summary>
    /// Fouling gained per running hour: <paramref name="perHour"/> on paving, times (1 + <paramref name="roughFactor"/> × the rough share of
    /// the ground under it × its intakes' exposure). With the shipped figures (0.01 an hour, a factor of 2, the aircraft's usual exposure of
    /// 0.5), a wash falls due at half fouled after about 50 hours on paving and 25 on rough ground (the owner's card: "about 50 engine hours").
    /// </summary>
    public static double PerHour(double perHour, double roughFactor, double roughShare, double exposure) =>
        Math.Max(0, perHour) * (1 + Math.Max(0, roughFactor) * Clamp01(roughShare) * Math.Max(0, exposure));

    /// <summary>
    /// What fouling leaves of the turbine's output, as a factor on its condition: 1 when clean, 1 − <paramref name="fullLoss"/> when fully
    /// fouled. The turbine model reads condition for its output, its temperature at a demand and its fuel, so this one figure gives the
    /// power lost, the hotter running and the extra fuel together (the owner's card: less power, runs hotter, and more fuel).
    /// </summary>
    public static double ConditionFactor(double fouling, double fullLoss) => 1 - Clamp01(fullLoss) * Clamp01(fouling);

    static double Clamp01(double value) => Math.Min(1, Math.Max(0, value));
}
