using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// A vehicle in another's hold (D'thorga Framework 0.288.0; docs/development/airdrop.md, "The hold"; the read of 18 Nov 2026 in
/// docs/development/modern-military-vehicles.md, decision 5): whether it fits by its cells either way round, and by weight. Game-free.
/// </summary>
public static class HoldRule
{
    /// <summary>Whether a footprint of <paramref name="width"/> by <paramref name="length"/> cells fits a hold of the given size, either way round.</summary>
    public static bool Fits(int width, int length, int holdWidth, int holdLength) =>
        (width <= holdWidth && length <= holdLength) || (width <= holdLength && length <= holdWidth);

    /// <summary>Whether a vehicle of <paramref name="kilograms"/> may join what is held already within the hold's limit.</summary>
    public static bool Takes(double kilograms, double heldKilograms, double limitKilograms) =>
        kilograms >= 0 && heldKilograms + kilograms <= limitKilograms;
}
