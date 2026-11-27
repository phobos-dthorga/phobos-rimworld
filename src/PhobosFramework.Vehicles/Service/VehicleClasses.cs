using System.Linq;
using Phobos.Framework.Vehicles.Rules;
using Vehicles;

namespace Phobos.VehicleSystems;

/// <summary>
/// What kind of vehicle a design is (Framework 0.369.0; the owner's cards of 28 Nov 2026), read from the Def alone: its loadout's rating,
/// the guns on its design, and the guns its upgrade tree can mount (<see cref="VehicleClassRule"/>). One place for every reader: the EMP
/// rating, the factory radios and the civilian bays read <see cref="CountsAsMilitary"/>; the Systems tab and the bindings report show the class.
/// Worked out once per design at start-up and kept on its systems' properties.
/// </summary>
public static class VehicleClasses
{
    /// <summary>A gun on the design as built: its own turrets list holds at least one.</summary>
    public static bool ArmedAsBuilt(VehicleDef def) =>
        def.comps?.Any(c => c is CompProperties_VehicleTurrets turrets && turrets.turrets is { Count: > 0 }) == true;

    /// <summary>A gun its upgrade tree can mount (a turret upgrade with at least one turret), as WeaponMounts reads the same tree.</summary>
    public static bool ArmedByUpgrade(VehicleDef def)
    {
        UpgradeTreeDef? tree = def.GetCompProperties<CompProperties_UpgradeTree>()?.def;
        return tree?.nodes?.Any(node => node?.upgrades?.Any(u => u is TurretUpgrade { turrets: { Count: > 0 } }) == true) == true;
    }

    /// <summary>The design's class.</summary>
    public static VehicleClass Of(VehicleDef def, PhobosVehicleLoadoutDef loadout) =>
        VehicleClassRule.Classify(loadout.empHardening, ArmedAsBuilt(def), ArmedByUpgrade(def));

    /// <summary>Military as EMP hardening judges it: armed as built, or a loadout rated military. A gun only by upgrade is not (the owner's
    /// card of 28 Nov 2026: civilian by design, military through upgrades, is a class of its own).</summary>
    public static bool CountsAsMilitary(VehicleDef def, PhobosVehicleLoadoutDef loadout) =>
        EmpRule.ClassFor(loadout.empHardening, ArmedAsBuilt(def)) == EmpHardening.Military;
}
