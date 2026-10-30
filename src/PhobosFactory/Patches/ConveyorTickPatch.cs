using HarmonyLib;
using VanillaFurnitureExpandedFactory;

namespace Phobos.Factory;

/// <summary>
/// A VFE Factory belt's tick returns at once while the belt is empty and Waiting, so a belt caught in that state
/// never pulls or moves again. This prefix wakes such a belt just before that check (<see cref="StuckBelts"/>),
/// so the same tick runs normally. Underground entrances and exits tick through this method too. A prefix
/// because the check is the first thing the method does; it never skips the original. VFE Factory has no
/// extension point for a belt's state.
/// </summary>
[HarmonyPatch(typeof(Building_Conveyor), "Tick")]
public static class ConveyorTickPatch
{
    public static bool Prepare() => StuckBelts.FindReset();

    public static void Prefix(Building_Conveyor __instance) => StuckBelts.WakeIfStuck(__instance);
}
