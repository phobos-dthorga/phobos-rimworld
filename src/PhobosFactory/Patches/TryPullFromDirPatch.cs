using HarmonyLib;
using VanillaFurnitureExpandedFactory;
using Verse;

namespace Phobos.Factory;

/// <summary>
/// A VFE Factory belt pulls from one side at a time (behind, then either side) through this private method,
/// which accepts only factory hoppers. When it found nothing behind the belt, this postfix takes a stack from
/// switched-on storage there instead (<see cref="BeltAccess.TryTakeFrom"/>); storage at the belt's sides is left
/// alone, so belts can run past it. VFE Factory's order of sides is kept, so storage behind a belt goes before a
/// hopper at its side. A postfix, so VFE Factory's pull is never skipped.
/// </summary>
[HarmonyPatch(typeof(Building_Conveyor), "TryPullFromDir")]
public static class TryPullFromDirPatch
{
    public static void Postfix(Building_Conveyor __instance, Rot4 dir, ref bool __result)
    {
        if (!__result) __result = BeltAccess.TryTakeFrom(__instance, dir);
    }
}
