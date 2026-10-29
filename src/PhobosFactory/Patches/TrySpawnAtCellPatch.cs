using HarmonyLib;
using PipeSystem;
using Verse;

namespace Phobos.Factory;

/// <summary>
/// Stops Pipe System from destroying finished factory batches. Its output step (Process.TrySpawnAtCell)
/// adds a batch to a matching stack in the output cell whenever that stack holds no more than the bill's
/// output limit, ignores whether the merge fit, and never places the rest. VFE Factory's limits mostly equal
/// the stack limit, and the game counts two identical guns as stackable, so a full output hopper swallowed
/// every later batch (most of VFE Factory's own bills, read 29 Oct 2026). This prefix skips the step only when
/// <see cref="OutputGuard"/> finds the batch would not fit, and then does what Pipe System does for a hopper
/// that is too full: it says so and tries again next tick. A bill with no output limit (0) gets Pipe System's
/// own fallback instead, which drops the batch on a free cell beside the machine.
/// Skipping the original is the reason for a prefix: there is no other point between choosing the stack and
/// losing the items, and a transpiler would break with any change to that method. Owner's go-ahead, 29 Oct 2026.
/// </summary>
[HarmonyPatch(typeof(Process), "TrySpawnAtCell",
    new[] { typeof(ProcessDef.Result), typeof(IntVec3), typeof(Map), typeof(Thing) },
    new[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out })]
public static class TrySpawnAtCellPatch
{
    public static bool Prefix(Process __instance, ProcessDef.Result result, IntVec3 cell, Map map, ref Thing? outThing, ref bool __result)
    {
        if (!OutputGuard.WouldLoseBatch(__instance, result, cell, map)) return true;
        if (__instance.Def.maxOutputCount != 0f)
        {
            __instance.outputFactoryHopperTooFull = true;
            __instance.tickLeft = 1;
        }
        outThing = null;
        __result = false;
        return false;
    }
}
