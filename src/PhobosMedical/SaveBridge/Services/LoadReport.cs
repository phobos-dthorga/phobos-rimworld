using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Phobos.MedicalSaveBridge.Services;

/// <summary>
/// Read-only checks on every pawn after a load, written to the log. They change nothing; they tell the owner whether the
/// re-numbering held and what More Injuries' own fix-up would have moved or removed.
/// </summary>
public static class LoadReport
{
    const int MaxLinesPerCheck = 40;

    public static void Run()
    {
        List<Pawn> pawns = AllPawnsAliveOrDead();
        ReportHediffsOnNewParts(pawns);
        ReportAddedPartsOffTheirRecipe(pawns);
    }

    /// <summary>
    /// A hediff that sits on a part the save never knew is either a re-numbering that went wrong or a wound dealt during the load.
    /// In a freshly loaded save, neither should exist.
    /// </summary>
    static void ReportHediffsOnNewParts(List<Pawn> pawns)
    {
        var lines = new List<string>();
        foreach (Pawn pawn in pawns)
        {
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff.Part != null && BridgeState.IsNewRecord(hediff.Part))
                {
                    lines.Add(pawn.LabelShortCap + ": " + hediff.def.defName + " on " + hediff.Part.Label + ", age " + hediff.ageTicks + " ticks");
                }
            }
        }
        Emit("Hediffs sitting on parts newer than the save (a re-numbering fault if any show)", lines);
    }

    /// <summary>
    /// What More Injuries' FixMisplacedBionics pass would have acted on: an added part (prosthetic, bionic, implant) whose part is
    /// not one its installing recipe allows. More Injuries moves such a hediff to a random allowed part or removes it; the bridge
    /// only lists them. Recipes are unioned per hediff, so a hediff that two recipes install on different parts raises no alarm.
    /// </summary>
    static void ReportAddedPartsOffTheirRecipe(List<Pawn> pawns)
    {
        var allowed = new Dictionary<HediffDef, HashSet<BodyPartDef>>();
        foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefs)
        {
            if (recipe.addsHediff?.addedPartProps == null || recipe.appliedOnFixedBodyParts.NullOrEmpty()) continue;
            if (!allowed.TryGetValue(recipe.addsHediff, out HashSet<BodyPartDef>? parts))
            {
                parts = new HashSet<BodyPartDef>();
                allowed[recipe.addsHediff] = parts;
            }
            foreach (BodyPartDef part in recipe.appliedOnFixedBodyParts)
            {
                parts.Add(part);
            }
        }
        var lines = new List<string>();
        foreach (Pawn pawn in pawns)
        {
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff.Part?.def == null || !allowed.TryGetValue(hediff.def, out HashSet<BodyPartDef>? parts) || parts.Contains(hediff.Part.def)) continue;
                lines.Add(pawn.LabelShortCap + ": " + hediff.def.defName + " on " + hediff.Part.Label + " (its recipes install it on " + string.Join(", ", parts.Select(part => part.defName)) + ")");
            }
        }
        Emit("Added parts sitting off the parts their recipes allow (More Injuries' fix-up would have moved or removed these)", lines);
    }

    static void Emit(string heading, List<string> lines)
    {
        if (lines.Count == 0)
        {
            Log.Message(BridgeState.LogPrefix + " " + heading + ": none.");
            return;
        }
        IEnumerable<string> shown = lines.Take(MaxLinesPerCheck);
        string more = lines.Count > MaxLinesPerCheck ? "\n  ... and " + (lines.Count - MaxLinesPerCheck) + " more" : "";
        Log.Warning(BridgeState.LogPrefix + " " + heading + ": " + lines.Count + "\n  " + string.Join("\n  ", shown) + more);
    }

    static List<Pawn> AllPawnsAliveOrDead()
    {
        var seen = new HashSet<Pawn>();
        var pawns = new List<Pawn>();
        foreach (Pawn pawn in PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead)
        {
            if (pawn?.health?.hediffSet != null && seen.Add(pawn)) pawns.Add(pawn);
        }
        foreach (Map map in Find.Maps)
        {
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse))
            {
                Pawn? inner = (thing as Corpse)?.InnerPawn;
                if (inner?.health?.hediffSet != null && seen.Add(inner)) pawns.Add(inner);
            }
        }
        return pawns;
    }
}
