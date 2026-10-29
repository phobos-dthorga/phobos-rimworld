using HarmonyLib;
using VanillaFurnitureExpandedFactory;

namespace Phobos.Factory;

/// <summary>
/// A VFE Factory belt tries to pull only while it has a factory hopper beside it, which it works out about once
/// a second in this private method. This postfix also counts switched-on storage directly behind it, so the
/// belt starts asking <see cref="TryPullFromDirPatch"/>. VFE Factory has no extension point for what a
/// belt may pull from: it checks a Def flag, which cannot be set for one building.
/// </summary>
[HarmonyPatch(typeof(Building_Conveyor), "PeriodicCacheCheck")]
public static class PeriodicCacheCheckPatch
{
    public static void Postfix(Building_Conveyor __instance, ref bool ___hasAdjacentHopper)
    {
        if (!___hasAdjacentHopper) ___hasAdjacentHopper = BeltAccess.HasStorageToTakeFrom(__instance);
    }
}
