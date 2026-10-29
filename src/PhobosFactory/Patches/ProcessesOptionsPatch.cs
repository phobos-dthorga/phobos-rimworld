using System.Collections.Generic;
using HarmonyLib;
using PipeSystem;
using Verse;

namespace Phobos.Factory;

/// <summary>
/// Pipe System's Add process menu checks only a bill's research list. A generated bill must also wait for
/// its bench recipe, which mods gate in their own ways (Dubs Rimatomics' research, Ideology memes), so this
/// removes the options of generated bills that <see cref="BillAvailability"/> says are not available yet.
/// Pipe System's ProcessDef.ShowProcess always returns true and cannot be overridden, so there is no other
/// hook. An option carries no link to its bill, so it is found by its label, which BenchBillGenerator keeps
/// unique per machine. A postfix that only removes options: the menu is otherwise Pipe System's.
/// </summary>
[HarmonyPatch(typeof(CompAdvancedResourceProcessor), nameof(CompAdvancedResourceProcessor.ProcessesOptions), MethodType.Getter)]
public static class ProcessesOptionsPatch
{
    public static void Postfix(CompAdvancedResourceProcessor __instance, List<FloatMenuOption> __result)
    {
        HashSet<string>? hidden = null;
        foreach (var process in __instance.Props.processes)
        {
            if (process != null && !BillAvailability.IsAvailable(process)) (hidden ??= new HashSet<string>()).Add(process.labelOverride);
        }
        if (hidden == null) return;
        __result.RemoveAll(option => hidden.Contains(option.Label));
        if (__result.Count == 0) __result.Add(new FloatMenuOption("PhobosFactory_NoBillsAvailable".Translate(), null));
    }
}
