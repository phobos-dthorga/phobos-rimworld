using System;
using HarmonyLib;
using Phobos.Factory.Rules;
using PipeSystem;
using RimWorld;
using Verse;

namespace Phobos.Factory;

/// <summary>What should happen to a finished batch at its output cell.</summary>
public enum OutputStep
{
    /// <summary>Pipe System's own output step is safe here.</summary>
    PipeSystemPlaces,

    /// <summary>The batch would lose items on the stack it would join, so the machine waits.</summary>
    Wait,

    /// <summary>The batch is bigger than one stack, so it goes out through <see cref="OutputGuard.PlaceInParts"/>.</summary>
    PlaceInParts,
}

/// <summary>
/// Decides what happens to a finished batch just before Pipe System's output step, which loses items when a batch
/// won't fit on the stack it joins (<see cref="OutputRule"/>) and repeats for ever when a batch is bigger than one stack.
/// Only hopper-only bills whose item is not made of a material go through that merge; the others are placed as new
/// things or checked by Pipe System itself.
/// </summary>
public static class OutputGuard
{
    /// <summary>Pipe System's own step that gives a new item its quality and ingredients, so the stand-in matches.</summary>
    private static readonly Action<Process, Thing>? ApplyIngredientsAndQuality = MakeApplyIngredientsAndQuality();

    public static OutputStep NextStep(Process process, ProcessDef.Result result, IntVec3 cell, Map map, out ThingDef? output, out int count)
    {
        output = null;
        count = 0;
        var def = process.Def;
        if (!def.onlyGrabAndOutputToFactoryHoppers || def.useFirstIngredientAsOutputStuff || !cell.Walkable(map)) return OutputStep.PipeSystemPlaces;
        if (BatchInParts.TryGet(process, out output, out count) && output != null) return OutputStep.PlaceInParts;
        // Some result workers pick the item or the amount at random (VFE Factory's drill and fish farm), and
        // Pipe System asks them again right after this; saving and restoring the generator makes both answers
        // the same.
        Rand.PushState();
        try
        {
            output = result.GetOutput(process);
            if (output == null || output.MadeFromStuff) return OutputStep.PipeSystemPlaces;
            count = result.GetCount(process);
            if (OutputRule.IsOversized(count, output.stackLimit)) return OutputStep.PlaceInParts;
            return WouldLoseBatch(process, output, count, cell, map) ? OutputStep.Wait : OutputStep.PipeSystemPlaces;
        }
        finally
        {
            Rand.PopState();
        }
    }

    /// <summary>
    /// Puts down as much of a batch as the cell will take and remembers the rest, so the batch comes out exactly once.
    /// True when none is left. The game's direct placement fills the stacks on the cell, starts new ones up to the cell's
    /// item limit, and leaves what didn't fit on the thing it was given. Asking it to go past that limit isn't an option:
    /// on 1.7 spawning into a full cell throws one of the items already there onto a nearby cell, which on a factory
    /// floor can be a belt carrying something else (GenSpawn.Spawn, read 2 Nov 2026).
    /// </summary>
    public static bool PlaceInParts(Process process, ThingDef output, int count, IntVec3 cell, Map map, out Thing? placed)
    {
        var batch = ThingMaker.MakeThing(output);
        batch.stackCount = count;
        ApplyIngredientsAndQuality?.Invoke(process, batch);
        if (!GenPlace.TryPlaceThing(batch, cell, map, ThingPlaceMode.Direct, out placed))
        {
            BatchInParts.Remember(process, output, batch.stackCount);
            return false;
        }
        BatchInParts.Forget(process);
        // The one public way to clear the batch's ingredients and forced quality. It also applies them to the item it
        // is given, so it gets a throwaway one rather than the placed stack, which could be rolled for dirty water twice.
        process.HandleIngredientsAndQuality(ThingMaker.MakeThing(output));
        return true;
    }

    private static bool WouldLoseBatch(Process process, ThingDef output, int batch, IntVec3 cell, Map map)
    {
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
            if (process.Def.maxOutputCount > 0f && thing.stackCount > process.Def.maxOutputCount) return false;
            return !OutputRule.BatchFits(thing.stackCount, batch, output.stackLimit);
        }
        return false;
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
