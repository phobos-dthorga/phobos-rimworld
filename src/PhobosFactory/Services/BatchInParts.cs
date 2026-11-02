using System.Runtime.CompilerServices;
using PipeSystem;
using Verse;

namespace Phobos.Factory;

/// <summary>
/// What a machine still has to put out of a batch too big for its output cell to take at once
/// (<see cref="Rules.OutputRule.IsOversized"/>). It belongs to the process and is saved with it, so a corpse gives
/// its meat once, whatever happens to the save in between. Saved as two values on the process, and only while a
/// batch is part-way out: the game ignores them if this mod is removed, so nothing new can complain on load.
/// </summary>
public static class BatchInParts
{
    private sealed class Left
    {
        public ThingDef? output;
        public int count;
    }

    private static readonly ConditionalWeakTable<Process, Left> Waiting = new();

    public static bool TryGet(Process process, out ThingDef? output, out int count)
    {
        output = null;
        count = 0;
        if (!Waiting.TryGetValue(process, out var left)) return false;
        output = left.output;
        count = left.count;
        return true;
    }

    public static void Remember(Process process, ThingDef output, int count)
    {
        var left = Waiting.GetOrCreateValue(process);
        left.output = output;
        left.count = count;
    }

    public static void Forget(Process process) => Waiting.Remove(process);

    public static void Expose(Process process)
    {
        Waiting.TryGetValue(process, out var left);
        if (Scribe.mode == LoadSaveMode.Saving && left == null) return;
        var output = left?.output;
        int count = left?.count ?? 0;
        Scribe_Defs.Look(ref output, "PhobosFactory_BatchLeftThing");
        Scribe_Values.Look(ref count, "PhobosFactory_BatchLeftCount");
        if (Scribe.mode == LoadSaveMode.LoadingVars && output != null && count > 0) Remember(process, output, count);
    }
}
