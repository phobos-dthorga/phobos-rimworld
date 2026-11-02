using HarmonyLib;
using PipeSystem;
using Verse;

namespace Phobos.Factory;

/// <summary>
/// Takes over Pipe System's output step (Process.TrySpawnAtCell) in the two cases where it goes wrong. Skipping the
/// original is the reason for a prefix: there is no other point between choosing where a batch goes and losing or
/// repeating it, and a transpiler would break with any change to that method. Owner's go-ahead, 29 Oct 2026.
/// <list type="bullet">
/// <item>A batch that won't fit on the stack it would join. The step adds it anyway, ignores whether the merge fit, and
/// never places the rest. VFE Factory's limits mostly equal the stack limit, and the game counts two identical guns as
/// stackable, so a full output hopper swallowed every later batch (read 29 Oct 2026). The machine now does what Pipe
/// System does for a hopper that is too full: it says so and tries again next tick. A bill with no output limit (0)
/// gets Pipe System's own fallback instead, which drops the batch on a free cell beside the machine.</item>
/// <item>A batch bigger than one stack (<see cref="OutputGuard.PlaceInParts"/>). The game's direct placement puts down
/// what the cell can hold and reports failure, the step throws the rest away and reports failure too, and the machine
/// tries the same batch again: one big corpse makes meat for ever (read in the owner's save, 2 Nov 2026).</item>
/// </list>
/// </summary>
[HarmonyPatch(typeof(Process), "TrySpawnAtCell",
    new[] { typeof(ProcessDef.Result), typeof(IntVec3), typeof(Map), typeof(Thing) },
    new[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out })]
public static class TrySpawnAtCellPatch
{
    public static bool Prefix(Process __instance, ProcessDef.Result result, IntVec3 cell, Map map, ref Thing? outThing, ref bool __result)
    {
        var step = OutputGuard.NextStep(__instance, result, cell, map, out var output, out int count);
        if (step == OutputStep.PipeSystemPlaces)
        {
            // Pipe System clears this only when it merges into a stack, so a machine went on saying "too full" after
            // placing into an emptied hopper. It sets the flag again itself if the stack is above the limit.
            __instance.outputFactoryHopperTooFull = false;
            return true;
        }
        outThing = null;
        __result = step == OutputStep.PlaceInParts && OutputGuard.PlaceInParts(__instance, output!, count, cell, map, out outThing);
        if (__result)
        {
            __instance.outputFactoryHopperTooFull = false;
        }
        else if (__instance.Def.maxOutputCount != 0f)
        {
            __instance.outputFactoryHopperTooFull = true;
            __instance.tickLeft = 1;
        }
        return false;
    }
}
