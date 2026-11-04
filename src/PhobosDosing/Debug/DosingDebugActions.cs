using System.Text;
using LudeonTK;
using Phobos.Dosing.Rules;
using Phobos.Dosing.Services;
using Phobos.Framework.Debug;
using RimWorld;
using Verse;

namespace Phobos.Dosing.Debug;

/// <summary>Dev-mode reads: the index, and the full decision table for a selected pawn as the patient.</summary>
public static class DosingDebugActions
{
    [DebugAction(DebugCategory.Name, "Dosing: log the pain-relief index", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Entry | AllowedGameStates.Playing)]
    public static void LogIndex()
    {
        var text = new StringBuilder(DosingMod.LogPrefix).Append(" indexed ingestibles (relief, consciousness cost, ceiling, breathing cost, bleed increase, addictiveness):");
        foreach (DoseOption option in DoseIndex.Options)
        {
            DoseEffect e = option.Effect;
            text.Append('\n').Append(option.Def.defName).Append(" (").Append(option.Def.label).Append("): relief ").Append(e.PainRelief.ToString("0.00"))
                .Append(", consciousness -").Append(e.ConsciousnessCost.ToString("0.00")).Append(" cap ").Append(e.ConsciousnessCap.ToString("0.00"))
                .Append(", breathing -").Append(e.BreathingCost.ToString("0.00")).Append(", bleed +").Append(e.BleedIncrease.ToString("0.00"))
                .Append(", addictiveness ").Append(e.Addictiveness.ToString("0.00")).Append(e.Sedative ? ", sedative" : "").Append(e.Recreational ? ", recreational" : "");
            foreach (DoseHediff dose in option.Hediffs)
            {
                text.Append("; gives ").Append(dose.Def.defName).Append(' ').Append(dose.Severity.ToString("0.00"))
                    .Append(dose.DangerSeverity < float.MaxValue ? " (dangerous from " + dose.DangerSeverity.ToString("0.00") + ")" : "");
            }
        }
        Log.Message(text.ToString());
    }

    [DebugAction(DebugCategory.Name, "Dosing: log the decision for selected pawns as patients", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void LogDecision()
    {
        var text = new StringBuilder(DosingMod.LogPrefix);
        foreach (object selected in Find.Selector.SelectedObjects)
        {
            if (selected is not Pawn pawn) continue;
            DoseDecision decision = DoseService.Decide(null, pawn);
            text.Append('\n').Append(pawn.LabelShort).Append(": pain ").Append(pawn.health.hediffSet.PainTotal.ToString("0.00"))
                .Append(", resting ").Append(pawn.Downed || pawn.InBed() ? "yes" : "no").Append(", need ").Append(decision.Need)
                .Append(", chosen ").Append(decision.Chosen?.Option.Def.defName ?? "nothing");
            foreach (DoseVerdict verdict in decision.Verdicts)
            {
                text.Append("\n  ").Append(verdict.Option.Def.defName).Append(": score ").Append(verdict.Score.ToString("0.00"))
                    .Append(!verdict.Present ? ", none on the map" : verdict.Refusal != DoseRefusal.None ? ", " + verdict.Refusal : verdict.Item == null ? ", out of reach" : ", allowed");
            }
        }
        Log.Message(text.ToString());
    }
}
