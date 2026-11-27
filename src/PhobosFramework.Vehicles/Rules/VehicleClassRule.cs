namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// What kind of vehicle a design is (Framework 0.369.0; the owner's cards of 28 Nov 2026: "civilian by design, but military through upgrades",
/// shown on the vehicle). Game-free: the service reads the design and calls this.
/// </summary>
public enum VehicleClass
{
    /// <summary>Built for civilian work, with no gun as built and none an upgrade can add.</summary>
    Civilian,
    /// <summary>Civilian by design, military through upgrades: no gun as built, but its upgrade tree can mount one (our service trucks' gun
    /// kits, and by the owner's card the L-ATV, the MaxxPro and the Humvee, whose guns come only by upgrade). Treated as civilian
    /// everywhere today (bays, factory radio, EMP); the class is recorded and shown for future use.</summary>
    ArmedByUpgrade,
    /// <summary>Armed as built, or a loadout rated military.</summary>
    Military,
    /// <summary>Nothing electronic keeps its engine running (a loadout rated mechanical: the steam locomotives).</summary>
    Mechanical,
}

public static class VehicleClassRule
{
    /// <summary>
    /// The class: a loadout's own rating first (mechanical or military); then armed as built makes it military; then a gun an upgrade can
    /// mount makes it armed by upgrade; else civilian. "Military" here is exactly what the EMP rating, the factory radios and the civilian bays
    /// read (<see cref="EmpRule.ClassFor"/>), so the two never disagree.
    /// </summary>
    public static VehicleClass Classify(EmpHardening chosen, bool armedAsBuilt, bool armedByUpgrade)
    {
        if (chosen == EmpHardening.Mechanical) return VehicleClass.Mechanical;
        if (EmpRule.ClassFor(chosen, armedAsBuilt) == EmpHardening.Military) return VehicleClass.Military;
        if (chosen == EmpHardening.Civilian) return VehicleClass.Civilian;
        return armedByUpgrade ? VehicleClass.ArmedByUpgrade : VehicleClass.Civilian;
    }
}
