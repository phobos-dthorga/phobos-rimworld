using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// The dose from fallout lying on a map (Framework 0.364.0; docs/development/radiation-bursts.md; the owner's choice of 28 Nov 2026, "our own
/// fallout, squeezed"). The dose rate falls by the 7:10 rule, as the age to the power -1.2 from the first hour (Glasstone and Dolan, The
/// Effects of Nuclear Weapons, 1977, 9.16), and is held at the first hour's rate before it (authored: the fallout is still coming down).
/// Shelter cuts it by protection factors (authored, after the same tables' order: a frame house halves it, a stone building cuts it to a
/// tenth, deep rock to almost nothing). Game-free.
/// </summary>
public static class FalloutRule
{
    const double Exponent = 1.2;

    /// <summary>The dose rate in the open, grays an hour, <paramref name="hours"/> after the blast.</summary>
    public static double RateAt(double hours, double graysPerHourAtFirstHour) =>
        graysPerHourAtFirstHour <= 0 ? 0 : hours <= 1 ? graysPerHourAtFirstHour : graysPerHourAtFirstHour * Math.Pow(hours, -Exponent);

    /// <summary>The dose in the open between two ages, in hours: the rate integrated exactly, flat to the first hour, then t^-1.2.</summary>
    public static double DoseBetween(double fromHours, double toHours, double graysPerHourAtFirstHour)
    {
        if (graysPerHourAtFirstHour <= 0 || toHours <= fromHours) return 0;
        double dose = 0;
        if (fromHours < 1)
        {
            double flatEnd = Math.Min(toHours, 1);
            dose += graysPerHourAtFirstHour * (flatEnd - Math.Max(fromHours, 0));
            fromHours = 1;
            if (toHours <= 1) return dose;
        }
        // The integral of r1 t^-1.2 is -5 r1 t^-0.2.
        dose += graysPerHourAtFirstHour / (Exponent - 1) * (Math.Pow(fromHours, 1 - Exponent) - Math.Pow(toHours, 1 - Exponent));
        return dose;
    }

    /// <summary>The share of fallout's outside dose that reaches someone under this cover (authored protection factors): a frame house 0.5,
    /// a stone building 0.1, a thin rock roof 0.05, deep rock 0.003.</summary>
    public static double Pass(ShelterCover cover) => cover switch
    {
        ShelterCover.LightBuilding => 0.5,
        ShelterCover.StoneBuilding => 0.1,
        ShelterCover.ThinRock => 0.05,
        ShelterCover.ThickRock => 0.003,
        _ => 1,
    };
}
