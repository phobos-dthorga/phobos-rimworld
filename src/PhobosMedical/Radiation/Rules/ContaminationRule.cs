using System;

namespace Phobos.Radiation.Rules;

/// <summary>
/// Radioactive contamination (D'thorga Medical 0.24.0; docs/development/radiation-model.md, RM6; the owner's choice of 22 Nov 2026: on
/// clothes and skin). Fallout dust settles on clothes and bare skin, and is breathed in without a mask; a suit keeps it off the skin and
/// a mask out of the lungs, but neither stops the gamma rays from the ground. Carried dust fades as fallout does and keeps giving a
/// small dose until washed off; dust breathed or swallowed gives its dose from inside over weeks; fallout's iodine goes to the thyroid
/// unless potassium iodide has filled it first. Game-free; the amounts are in the rules Def, authored.
/// </summary>
public static class ContaminationRule
{
    /// <summary>The share of dust left after so many days, by the 7:10 rule (Glasstone and Dolan, 9.16) from fresh fallout: all of it
    /// for the first day, then falling as the age to the power -1.2.</summary>
    public static double DustLeft(double days) => days <= 1 ? 1 : Math.Pow(days, -1.2);

    /// <summary>The share of an inside dose delivered over so many days, at a half-life in the body (caesium's about 110 days without
    /// treatment, Radiogardase label).</summary>
    public static double Delivered(double days, double halfLifeDays) => days <= 0 || halfLifeDays <= 0 ? 0 : 1 - Math.Pow(2, -days / halfLifeDays);

    /// <summary>
    /// The share of a body that a person's clothes leave bare, from the share of skin they cover (0 to 1): the dust that reaches the
    /// skin. The rest settles on the clothes.
    /// </summary>
    public static double Bare(double covered) => 1 - Clamp(covered, 0, 1);

    // ---- The thyroid (US FDA, 2001: potassium iodide "provides protection only for the thyroid from radioiodines") ----

    /// <summary>Thyroid cancer per gray to the thyroid: about 1 % for adults and 5 % for children (agent's figures, unverified; the
    /// sources agree only that children are far more at risk, as after Chernobyl).</summary>
    public static double ThyroidCancerChance(double thyroidGrays, double ageYears) =>
        Clamp(thyroidGrays * (ageYears < 18 ? 0.05 : 0.01), 0, 0.9);

    /// <summary>Real years until a thyroid cancer shows: 4 to 10 by the person's draw (after Chernobyl's children, from about 4).</summary>
    public static double ThyroidCancerYears(double draw) => 4 + 6 * Clamp(draw, 0, 1);

    static double Clamp(double value, double low, double high) => value < low ? low : value > high ? high : value;
}
