using System.Collections.Generic;
using System.Linq;
using Phobos.Factory.Rules;
using PipeSystem;
using Verse;

namespace Phobos.Factory;

/// <summary>
/// Gives every bill on a machine marked with <see cref="SafeOutputLimitsExtension"/> an output limit that
/// never loses a finished batch (<see cref="OutputCapRule"/>). Runs once at load, after VFE Factory has
/// generated its machining bay bills.
/// </summary>
public static class OutputLimitService
{
    public static void MakeMarkedMachinesSafe()
    {
        var changedOn = new List<string>();
        int changed = 0;
        foreach (var machine in DefDatabase<ThingDef>.AllDefsListForReading)
        {
            if (!machine.HasModExtension<SafeOutputLimitsExtension>()) continue;
            var processes = machine.GetCompProperties<CompProperties_AdvancedResourceProcessor>()?.processes;
            if (processes == null) continue;
            int before = changed;
            foreach (var process in processes)
                if (process != null && MakeSafe(process)) changed++;
            if (changed > before) changedOn.Add(machine.label);
        }
        if (changed > 0)
            Log.Message($"[D'thorga Factory Additions] Output limits made safe on {changed} bills ({string.Join(", ", changedOn)}): " +
                        "a finished item now waits for room in the output hopper instead of vanishing.");
    }

    /// <summary>
    /// Lowers one bill's output limit to the safe one; false when it was already safe or the rule does not
    /// apply. It applies only where Pipe System adds finished items to a stack (hopper-only bills whose item is
    /// not made of a material) and where the batch size is fixed (a result worker can change it at run time,
    /// as VFE Factory's mechanoid shredding does).
    /// </summary>
    public static bool MakeSafe(ProcessDef process)
    {
        if (!process.onlyGrabAndOutputToFactoryHoppers || process.useFirstIngredientAsOutputStuff || process.results == null) return false;
        var results = process.results.Where(r => r.thing != null).ToList();
        if (results.Count == 0 || results.Any(r => r.workerClass != typeof(ProcessDef.ResultWorker))) return false;
        float safe = results.Min(r => OutputCapRule.Clamp(process.maxOutputCount, r.thing.stackLimit, r.count));
        if (safe == process.maxOutputCount) return false;
        process.maxOutputCount = safe;
        return true;
    }
}
