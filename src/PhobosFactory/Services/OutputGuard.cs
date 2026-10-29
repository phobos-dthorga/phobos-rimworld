using System;
using HarmonyLib;
using Phobos.Factory.Rules;
using PipeSystem;
using Verse;

namespace Phobos.Factory;

/// <summary>
/// Whether Pipe System's output step is about to add a finished batch to a stack in the output cell that
/// cannot hold all of it (<see cref="OutputRule"/>), which would lose the rest. It mirrors that step:
/// the same output and count, a stand-in for the new item with the same quality and ingredients, and the
/// first thing on the cell that stands-in stacks with. Only hopper-only bills whose item is not made of a
/// material go through that merge; the others are placed as new things or checked by Pipe System itself.
/// </summary>
public static class OutputGuard
{
    /// <summary>Pipe System's own step that gives a new item its quality and ingredients, so the stand-in matches.</summary>
    private static readonly Action<Process, Thing>? ApplyIngredientsAndQuality = MakeApplyIngredientsAndQuality();

    public static bool WouldLoseBatch(Process process, ProcessDef.Result result, IntVec3 cell, Map map)
    {
        var def = process.Def;
        if (!def.onlyGrabAndOutputToFactoryHoppers || def.useFirstIngredientAsOutputStuff || !cell.Walkable(map)) return false;
        // Some result workers pick the item or the amount at random (VFE Factory's drill and fish farm), and
        // Pipe System asks them again right after this; saving and restoring the generator makes both answers
        // the same.
        Rand.PushState();
        try
        {
            var output = result.GetOutput(process);
            if (output == null || output.MadeFromStuff) return false;
            int batch = result.GetCount(process);
            var things = cell.GetThingList(map);
            bool crowded = false;
            foreach (var thing in things)
            {
                if (thing.def == output && !OutputRule.BatchFits(thing.stackCount, batch, output.stackLimit))
                {
                    crowded = true;
                    break;
                }
            }
            if (!crowded) return false;
            var standIn = ThingMaker.MakeThing(output);
            ApplyIngredientsAndQuality?.Invoke(process, standIn);
            foreach (var thing in things)
            {
                if (thing.def != output || !thing.CanStackWith(standIn)) continue;
                // Above the bill's limit Pipe System waits by itself; otherwise it merges into this stack.
                if (def.maxOutputCount > 0f && thing.stackCount > def.maxOutputCount) return false;
                return !OutputRule.BatchFits(thing.stackCount, batch, output.stackLimit);
            }
            return false;
        }
        finally
        {
            Rand.PopState();
        }
    }

    private static Action<Process, Thing>? MakeApplyIngredientsAndQuality()
    {
        var method = AccessTools.Method(typeof(Process), "ApplyIngredientsAndQuality", new[] { typeof(Thing) });
        if (method != null) return AccessTools.MethodDelegate<Action<Process, Thing>>(method);
        Log.Warning("[D'thorga Factory Additions] Pipe System's ApplyIngredientsAndQuality is gone; the output guard " +
                    "now compares new items without their quality, which matters only for bills that set one.");
        return null;
    }
}
