using System;
using System.Collections.Generic;

namespace Phobos.Radiation.Rules;

/// <summary>One worn garment, as the shielding rule sees it: its mass and the body part groups (by defName) it covers.</summary>
public readonly struct WornLayer
{
    public readonly double massKg;
    public readonly IReadOnlyList<string> groups;

    public WornLayer(double massKg, IReadOnlyList<string> groups)
    {
        this.massKg = massKg;
        this.groups = groups;
    }
}

/// <summary>
/// Worn mass shields a little against gamma rays from outside (D'thorga Medical 0.24.0; docs/development/radiation-model.md, RM17; the
/// owner's choice of 22 Nov 2026: by mass, weighted by the marrow it covers, not Combat Extended's armour ratings). Gamma rays are
/// stopped by mass per area, whatever the material: about 650 kg a square metre cuts a strike's gamma rays to a tenth (Glasstone and
/// Dolan, Table 8.41: 3.3 inches of steel, 11 of concrete, 16 of earth). Each garment's mass is spread over the area of the groups it
/// covers; layers on a group add up; the share that passes is weighted by the active bone marrow under each group, since the marrow's
/// dose decides the blood syndrome. Game-free.
/// </summary>
public static class ShieldingRule
{
    /// <summary>The mass per area that cuts gamma rays to a tenth, kg/m² (from Glasstone and Dolan's Table 8.41, at ordinary densities).</summary>
    public const double TenthValueKgPerM2 = 650;

    /// <summary>An adult's skin area by body part group, in m² (agent's estimates, about 1.8 m² in all; a gameplay simplification).</summary>
    public static readonly IReadOnlyDictionary<string, double> AreaM2 = new Dictionary<string, double>
    {
        ["Torso"] = 0.55, ["Shoulders"] = 0.1, ["Arms"] = 0.25, ["Hands"] = 0.09, ["Neck"] = 0.03, ["FullHead"] = 0.12,
        ["UpperHead"] = 0.06, ["Legs"] = 0.55, ["Feet"] = 0.12, ["Waist"] = 0.05,
    };

    /// <summary>
    /// The share of an adult's active bone marrow under each group (after ICRP Publication 70's table, from the agent's memory,
    /// unverified): the torso's ribs, spine, sternum and pelvis three quarters; the skull 8 %; the neck's vertebrae 4 %; shoulder
    /// blades, collarbones and upper arms 6 %; the upper thighs 7 %. A helmet over the upper head only covers half of the skull.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, double> MarrowShare = new Dictionary<string, double>
    {
        ["Torso"] = 0.75, ["FullHead"] = 0.08, ["Neck"] = 0.04, ["Shoulders"] = 0.04, ["Arms"] = 0.02, ["Legs"] = 0.07,
    };

    /// <summary>The share of gamma rays that pass a mass per area.</summary>
    public static double Transmission(double kgPerM2) => kgPerM2 <= 0 ? 1 : Math.Pow(10, -kgPerM2 / TenthValueKgPerM2);

    /// <summary>The share of an outside dose that reaches the marrow through what is worn: 1 for nothing worn.</summary>
    public static double Pass(IEnumerable<WornLayer> worn)
    {
        var load = new Dictionary<string, double>();
        foreach (WornLayer layer in worn)
        {
            if (layer.massKg <= 0 || layer.groups == null) continue;
            double area = 0;
            foreach (string group in layer.groups) area += AreaM2.TryGetValue(group, out double a) ? a : 0;
            if (area <= 0) continue;
            double perArea = layer.massKg / area;
            foreach (string group in layer.groups)
            {
                if (!AreaM2.ContainsKey(group)) continue;
                load[group] = (load.TryGetValue(group, out double before) ? before : 0) + perArea;
            }
        }
        double pass = 0;
        foreach (KeyValuePair<string, double> marrow in MarrowShare)
        {
            double kg = load.TryGetValue(marrow.Key, out double on) ? on : 0;
            if (marrow.Key == "FullHead" && load.TryGetValue("UpperHead", out double upper))
            {
                // A helmet on the upper head covers half the skull's marrow.
                pass += marrow.Value * (0.5 * Transmission(kg + upper) + 0.5 * Transmission(kg));
                continue;
            }
            pass += marrow.Value * Transmission(kg);
        }
        return pass;
    }
}
