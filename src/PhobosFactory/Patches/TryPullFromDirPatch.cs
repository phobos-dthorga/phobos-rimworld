using HarmonyLib;
using VanillaFurnitureExpandedFactory;
using Verse;

namespace Phobos.Factory;

/// <summary>
/// A VFE Factory belt pulls from one side at a time (behind, then either side) through this private method,
/// which accepts only factory hoppers and takes the first item it finds there.
/// <list type="bullet">
/// <item>The prefix takes over for hopper-flagged storage whose Def carries <see cref="BeltFilterExtension"/>
/// (the corpse fridge), taking only what its filter allows (<see cref="BeltAccess.TakeFirstAllowed"/>). It skips
/// the original for that storage alone: VFE Factory chooses the item inside this method, so the only other way
/// would be to put a wrong item back after it was taken. Hoppers and other storage run the original unchanged.</item>
/// <item>The postfix, when nothing was taken, takes a stack from switched-on storage directly behind the belt
/// instead (<see cref="BeltAccess.TryTakeFrom"/>); storage at the belt's sides is left alone, so belts can run
/// past it. VFE Factory's order of sides is kept, so storage behind a belt goes before a hopper at its side.</item>
/// </list>
/// </summary>
[HarmonyPatch(typeof(Building_Conveyor), "TryPullFromDir")]
public static class TryPullFromDirPatch
{
    public static bool Prefix(Building_Conveyor __instance, Rot4 dir, ref bool __result)
    {
        var storage = BeltAccess.FilteredHopperAt(__instance, dir);
        if (storage == null) return true;
        __result = BeltAccess.TakeFirstAllowed(__instance, storage);
        return false;
    }

    public static void Postfix(Building_Conveyor __instance, Rot4 dir, ref bool __result)
    {
        if (!__result) __result = BeltAccess.TryTakeFrom(__instance, dir);
    }
}
