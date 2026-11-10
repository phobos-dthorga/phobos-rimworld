using System.Collections.Generic;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>Another mod's part accepted in place of ours, only for the vehicles it suits (a light engine does not go in a tank).</summary>
public class PartSupplySubstitute
{
    public ThingDef? thing;
    public int count = 1;
    public float minKilograms = float.NaN;
    public float maxKilograms = float.NaN;

    public bool Suits(double kilograms) =>
        thing != null && (float.IsNaN(minKilograms) || kilograms >= minKilograms) && (float.IsNaN(maxKilograms) || kilograms < maxKilograms);
}

/// <summary>
/// What replacing a destroyed part of a role costs (docs/development/vehicle-health-consequences.md). The first Def that fits the
/// part's role and the vehicle wins; one marked isDefault covers every part with no role of its own, by its size. Ours always exist;
/// another mod's parts are listed as substitutes, each only where it makes sense. Another mod adds roles and parts in XML.
/// </summary>
public class PhobosVehiclePartSupplyDef : Def
{
    public List<string> roles = new();
    /// <summary>Covers every part no other Def covers, at <see cref="steelPerHealth"/> and <see cref="componentsPerHealth"/>.</summary>
    public bool isDefault;
    /// <summary>Only vehicles with a gas turbine, or only those without.</summary>
    public Match turbine = Match.Any;
    /// <summary>Charged at the player's whole-part share (engines, transmissions) rather than the share for everything else.</summary>
    public bool wholePart;
    /// <summary>Our part, asked for whole at a whole-part share of 100%.</summary>
    public ThingDef? item;
    public int itemCount = 1;
    /// <summary>The item is a service kit (S4, Framework 0.74.0): while the colony still accepts raw materials, its own cost is paid instead.</summary>
    public bool kit;
    /// <summary>The full cost in materials, of which the player's share is charged. Empty: the item's own cost.</summary>
    public List<ThingDefCountClass> materials = new();
    public float steelPerHealth;
    public float componentsPerHealth;
    public List<PartSupplySubstitute> substitutes = new();
    /// <summary>Work in ticks for a mechanic of construction speed 1.</summary>
    public float work = 1500f;

    public bool Fits(string? role, bool hasTurbine)
    {
        if (role == null || !roles.Contains(role)) return false;
        return turbine == Match.Any || (turbine == Match.Yes) == hasTurbine;
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (!isDefault && roles.Count == 0) yield return "names no roles and is not the default";
        if (wholePart && item == null) yield return "a whole part needs an item";
        if (item == null && materials.Count == 0 && steelPerHealth <= 0f && componentsPerHealth <= 0f) yield return "costs nothing";
        if (work <= 0f) yield return "work must be above 0";
    }
}
