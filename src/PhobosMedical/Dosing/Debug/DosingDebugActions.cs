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
    /// <summary>Logs every drug indexed for pain relief, with its relief, its costs to consciousness and breathing, the bleeding it
    /// adds, its addictiveness and the conditions it gives. Reads only.</summary>
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

    /// <summary>Logs every drug indexed for fighting disease, with the immunity it adds. Reads only.</summary>
    [DebugAction(DebugCategory.Name, "Dosing: log drugs for fighting disease", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Entry | AllowedGameStates.Playing)]
    public static void LogImmunityIndex()
    {
        var text = new StringBuilder(DosingMod.LogPrefix).Append(" drugs for fighting disease:");
        foreach (DoseOption option in DoseIndex.ImmunityOptions)
        {
            text.Append('\n').Append(option.Def.defName).Append(" (").Append(option.Def.label).Append("): immunity gain +").Append(option.Effect.ImmunityGain.ToString("0.00"));
        }
        Log.Message(text.ToString());
    }

    /// <summary>Logs the indexed cures (what each removes), preventives (the outbreak diseases each stops) and helps to breathing, with
    /// each one's costs and worth. Reads only.</summary>
    [DebugAction(DebugCategory.Name, "Dosing: log cures and preventives", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Entry | AllowedGameStates.Playing)]
    public static void LogCareIndex()
    {
        var text = new StringBuilder(DosingMod.LogPrefix).Append(" cures (what each removes):");
        foreach (DoseOption option in DoseIndex.CureOptions) AppendCare(text, option, option.Cures);
        text.Append('\n').Append(DosingMod.LogPrefix).Append(" preventives (the outbreak diseases each stops):");
        foreach (DoseOption option in DoseIndex.PreventionOptions) AppendCare(text, option, option.Prevents);
        text.Append('\n').Append(DosingMod.LogPrefix).Append(" helps to breathing:");
        foreach (DoseOption option in DoseIndex.BreathingOptions)
        {
            AppendCare(text, option, System.Array.Empty<HediffDef>());
            text.Append(", breathing +").Append(option.Effect.BreathingGain.ToString("0.00"));
        }
        Log.Message(text.ToString());
    }

    /// <summary>
    /// Every list the care needs choose from, with what each item is worth by the game's own price and whether it is kept for saving a
    /// life at the player's rare mark (Medical 0.32.0): the price of an item whose Def states none is worked out by the game, so this is
    /// the one place it can be read.
    /// </summary>
    [DebugAction(DebugCategory.Name, "Dosing: log every care list, with worth", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Entry | AllowedGameStates.Playing)]
    public static void LogEveryList()
    {
        float mark = DosingMod.Settings.rareItemMarketValue;
        var text = new StringBuilder(DosingMod.LogPrefix).Append(" every care list; the rare mark is ").Append(mark.ToString("0")).Append(" silver:");
        AppendList(text, "pain relief", DoseIndex.Options, mark);
        AppendList(text, "fighting disease", DoseIndex.ImmunityOptions, mark);
        AppendList(text, "cures", DoseIndex.CureOptions, mark);
        AppendList(text, "prevention", DoseIndex.PreventionOptions, mark);
        AppendList(text, "breathing", DoseIndex.BreathingOptions, mark);
        AppendList(text, "toxins", DoseIndex.ToxicOptions, mark);
        AppendList(text, "vacuum", DoseIndex.VacuumOptions, mark);
        AppendList(text, "close to a major break", DoseIndex.StrainOptions, mark);
        AppendList(text, "addiction", DoseIndex.AddictionOptions, mark);
        AppendList(text, "old wounds", DoseIndex.OldWoundOptions, mark);
        AppendList(text, "missing parts", DoseIndex.RegrowthOptions, mark);
        AppendList(text, "reservoirs", DoseIndex.ReservoirOptions, mark);
        Log.Message(text.ToString());
    }

    static void AppendList(StringBuilder text, string name, System.Collections.Generic.IReadOnlyList<DoseOption> list, float mark)
    {
        text.Append("\n").Append(name).Append(" (").Append(list.Count).Append("):");
        foreach (DoseOption option in list)
        {
            text.Append("\n  ").Append(option.Def.defName).Append(" (").Append(option.Def.label).Append("): worth ").Append(option.MarketValue.ToString("0"))
                .Append(CareSafetyRule.Rare(option.MarketValue, mark) ? ", rare: only to save a life" : "");
            foreach (DoseHediff dose in option.Hediffs)
            {
                text.Append("; gives ").Append(dose.Def.defName).Append(' ').Append(dose.Severity.ToString("0.00")).Append(dose.ForRedose ? "" : " (a side harm)");
            }
        }
    }

    static void AppendCare(StringBuilder text, DoseOption option, System.Collections.Generic.IReadOnlyList<HediffDef> conditions)
    {
        DoseEffect e = option.Effect;
        text.Append('\n').Append(option.Def.defName).Append(" (").Append(option.Def.label).Append("):");
        foreach (HediffDef condition in conditions) text.Append(' ').Append(condition.defName);
        text.Append("; consciousness -").Append(e.ConsciousnessCost.ToString("0.00")).Append(", breathing -").Append(e.BreathingCost.ToString("0.00"))
            .Append(", vomits a day ").Append(e.VomitsPerDay.ToString("0.00")).Append(", addictiveness ").Append(e.Addictiveness.ToString("0.00"))
            .Append(", worth ").Append(option.MarketValue.ToString("0")).Append(option.LethalDependency != null ? ", begins " + option.LethalDependency.defName : "");
    }

    /// <summary>Select people: logs what each would take for their own pain, what blocks it, and the score and verdict for every
    /// option. Reads only.</summary>
    [DebugAction(DebugCategory.Name, "Dosing: log self-medication for selected pawns", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void LogSelfMedication()
    {
        var text = new StringBuilder(DosingMod.LogPrefix);
        foreach (object selected in Find.Selector.SelectedObjects)
        {
            if (selected is not Pawn pawn) continue;
            DoseDecision decision = DoseService.SelfDecide(pawn, everyItem: true);
            text.Append('\n').Append(pawn.LabelShort).Append(": pain ").Append(pawn.health.hediffSet.PainTotal.ToString("0.00"))
                .Append(", blocked by ").Append(DoseService.SelfBlockFor(pawn, DosingMod.Settings, Defs.PhobosDosingRulesDef.Current))
                .Append(", would take ").Append(decision.Chosen?.Option.Def.defName ?? "nothing");
            foreach (DoseVerdict verdict in decision.Verdicts)
            {
                text.Append("\n  ").Append(verdict.Option.Def.defName).Append(": score ").Append(verdict.Score.ToString("0.00"))
                    .Append(!verdict.Present ? ", none here" : verdict.Refusal != DoseRefusal.None ? ", " + verdict.Refusal : verdict.Item == null ? ", out of reach" : ", allowed");
            }
        }
        Log.Message(text.ToString());
    }

    /// <summary>Select people: logs what a doctor would give each as a patient (their pain, bleeding, rest, disease and need), and the
    /// score and verdict for every option. Reads only.</summary>
    [DebugAction(DebugCategory.Name, "Dosing: log the decision for selected pawns as patients", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void LogDecision()
    {
        var text = new StringBuilder(DosingMod.LogPrefix);
        foreach (object selected in Find.Selector.SelectedObjects)
        {
            if (selected is not Pawn pawn) continue;
            DoseDecision decision = DoseService.Decide(null, pawn, everyItem: true);
            text.Append('\n').Append(pawn.LabelShort).Append(": pain ").Append(pawn.health.hediffSet.PainTotal.ToString("0.00"))
                .Append(", bleeding ").Append(pawn.health.hediffSet.BleedRateTotal.ToString("0.00"))
                .Append(", resting ").Append(pawn.Downed || pawn.InBed() ? "yes" : "no")
                .Append(", losing to a disease ").Append(DoseService.LosingToDisease(pawn, Defs.PhobosDosingRulesDef.Current) ? "yes" : "no").Append(", need ").Append(decision.Need)
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
