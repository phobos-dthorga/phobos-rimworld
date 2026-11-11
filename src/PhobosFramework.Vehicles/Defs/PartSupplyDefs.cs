using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Bills;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>Words a vehicle's parts bill carries for the role supplies to read.</summary>
public static class VehicleBillTags
{
    /// <summary>The vehicle runs on a gas turbine, so its engine role is the turbine's.</summary>
    public const string Turbine = "Turbine";
}

/// <summary>
/// What a vehicle's part of a role costs: to replace when destroyed (docs/development/vehicle-health-consequences.md), and, since
/// Framework 0.95.0, to build (docs/development/parts-bills.md). It is the core's role supply with a repair's own figures added, so
/// one table serves builds and repairs: the roles, our item or materials and the other mods' substitutes are the core's fields. The
/// first Def that fits the part's role and the vehicle wins; one marked isDefault covers every part with no role of its own, by its
/// size. Ours always exist; another mod's parts are listed as substitutes, each only where it makes sense.
/// </summary>
public class PhobosVehiclePartSupplyDef : PhobosRoleSupplyDef
{
    /// <summary>Covers every part no other Def covers, at <see cref="steelPerHealth"/> and <see cref="componentsPerHealth"/>.</summary>
    public bool isDefault;
    /// <summary>Only vehicles with a gas turbine, or only those without.</summary>
    public Match turbine = Match.Any;
    /// <summary>Charged at the player's whole-part share (engines, transmissions) rather than the share for everything else.</summary>
    public bool wholePart;
    /// <summary>The item is a service kit (S4, Framework 0.74.0): while the colony still accepts raw materials, its own cost is paid instead.</summary>
    public bool kit;
    public float steelPerHealth;
    public float componentsPerHealth;
    /// <summary>Work in ticks for a mechanic of construction speed 1.</summary>
    public float work = 1500f;

    public bool Fits(string? role, bool hasTurbine)
    {
        if (role == null || !roles.Contains(role)) return false;
        return turbine == Match.Any || (turbine == Match.Yes) == hasTurbine;
    }

    public override bool Fits(string role, IReadOnlyCollection<string> tags) => Fits(role, tags.Contains(VehicleBillTags.Turbine));

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (!isDefault && roles.Count == 0) yield return "names no roles and is not the default";
        if (wholePart && item == null) yield return "a whole part needs an item";
        if (item == null && materials.Count == 0 && steelPerHealth <= 0f && componentsPerHealth <= 0f) yield return "costs nothing";
        if (work <= 0f) yield return "work must be above 0";
    }
}
