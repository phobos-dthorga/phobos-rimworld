using System.Text;
using LudeonTK;
using Phobos.Framework.Debug;
using Phobos.Triage.Services;
using Verse;

namespace Phobos.Triage.Debug;

/// <summary>Dev-mode reads and one forced planning pass. Nothing here changes game state beyond what the planner does on its own.</summary>
public static class TriageDebugActions
{
    [DebugAction(DebugCategory.Name, "Triage: log the operations indexed", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Entry | AllowedGameStates.Playing)]
    public static void LogIndex()
    {
        var text = new StringBuilder(TriageMod.LogPrefix);
        foreach (OperationInfo operation in OperationIndex.All)
        {
            text.Append('\n').Append(operation.Recipe.defName).Append(": removes ");
            for (int i = 0; i < operation.Treats.Count; i++) text.Append(i > 0 ? ", " : "").Append(operation.Treats[i].defName);
            text.Append("; skill ").Append(operation.Recipe.skillRequirements?.Count > 0 ? operation.Recipe.skillRequirements[0].minLevel.ToString() : "none")
                .Append(", success x").Append(operation.Recipe.surgerySuccessChanceFactor.ToString("0.00"))
                .Append(", death on failure ").Append((operation.Recipe.deathOnFailedSurgeryChance * 100f).ToString("0.0")).Append('%')
                .Append(", cost ").Append(operation.IngredientCost.ToString("0"))
                .Append(operation.NeedsMedicine ? ", medicine" : "").Append(operation.NeedsRareTool ? ", rare tool" : "");
        }
        Log.Message(text.ToString());
    }

    [DebugAction(DebugCategory.Name, "Triage: log triage of selected pawns", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void LogSelected()
    {
        var text = new StringBuilder(TriageMod.LogPrefix);
        foreach (object selected in Find.Selector.SelectedObjects)
        {
            if (selected is not Pawn pawn) continue;
            TriageEntry? entry = MapComponent_PhobosTriage.For(pawn.Map)?.EntryOf(pawn);
            text.Append('\n').Append(pawn.LabelShort).Append(": ");
            if (entry == null) { text.Append("not a patient of the doctors"); continue; }
            text.Append(entry.Category).Append(" (score ").Append(entry.Score.ToString("0")).Append("), worst ")
                .Append(entry.Worst?.LabelBase ?? "nothing").Append(" class ").Append(entry.WorstClass)
                .Append(", hours to death ").Append(float.IsInfinity(entry.HoursToDeath) ? "none" : entry.HoursToDeath.ToString("0.0"))
                .Append(", queued ").Append(entry.Queued?.defName ?? "nothing").Append(", refusal ").Append(entry.Refusal)
                .Append("; bills on the patient ").Append(pawn.BillStack?.Count ?? 0).Append(", ours in the ledger ").Append(TriageBillLedger.Current?.Count ?? 0);
        }
        Log.Message(text.ToString());
    }

    [DebugAction(DebugCategory.Name, "Triage: run the planner now on this map", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void RunNow()
    {
        MapComponent_PhobosTriage? triage = MapComponent_PhobosTriage.For(Find.CurrentMap);
        if (triage == null) { Log.Message(TriageMod.LogPrefix + " no triage component on this map."); return; }
        triage.Run();
        Log.Message(TriageMod.LogPrefix + " planned: " + triage.Entries.Count + " patients read, " + triage.PatientsWithBills.Count + " in bed with bills, "
            + (TriageBillLedger.Current?.Count ?? 0) + " bills of ours in the ledger.");
    }
}
