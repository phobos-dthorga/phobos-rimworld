using HarmonyLib;
using PipeSystem;
using Verse;

namespace Phobos.Factory;

/// <summary>
/// A hopper-only factory bill reads an input cell only when Pipe System finds a factory hopper there (it checks
/// a Def flag, about every 100 ticks per machine). This postfix also accepts switched-on storage, so a shelf
/// that holds several stacks on one cell can feed a machine directly. Pipe System then reads every item on the
/// cell, as it does for a hopper. A postfix that only widens the answer.
/// </summary>
[HarmonyPatch(typeof(Process), nameof(Process.InputFactoryHopperDetected))]
public static class InputFactoryHopperDetectedPatch
{
    public static void Postfix(IntVec3 inputTile, ThingWithComps ___parent, ref bool __result)
    {
        if (!__result) __result = BeltAccess.FeedsInputCell(inputTile, ___parent?.Map);
    }
}
