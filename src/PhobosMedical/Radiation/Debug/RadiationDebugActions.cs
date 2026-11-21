using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using LudeonTK;
using Phobos.Framework.Debug;
using Phobos.Medical;
using Phobos.Radiation.Defs;
using Phobos.Radiation.Rules;
using Phobos.Radiation.Services;
using RimWorld;
using Verse;

namespace Phobos.Radiation.Debug;

/// <summary>
/// Dev-mode radiation for testing (owner, 22 Nov 2026: "add debug methods to remove and add radiation"): a dose from preset buttons, every
/// radiation condition removed, and a read of the dose. Each works on the selected people.
/// </summary>
public static class RadiationDebugActions
{
    /// <summary>The preset doses, in grays: from below the course to well past the lethal range, so each phase and syndrome can be reached.</summary>
    static readonly float[] Doses = { 0.25f, 0.5f, 1f, 2f, 3f, 4f, 5f, 6f, 8f, 10f, 15f, 20f, 50f };

    [DebugAction(DebugCategory.Name, "Radiation: give a dose to selected pawns", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static List<DebugActionNode> GiveDose()
    {
        var nodes = new List<DebugActionNode>();
        foreach (float grays in Doses)
        {
            float captured = grays;
            nodes.Add(new DebugActionNode(captured.ToString("0.##", CultureInfo.InvariantCulture) + " Gy", DebugActionType.Action, () =>
            {
                foreach (Pawn pawn in DebugSelection.SelectedThings<Pawn>().Where(p => !p.Dead).ToList())
                {
                    RadiationService.Receive(pawn, captured, "debug");
                    Log.Message(MedicalMod.LogPrefix + " " + pawn.LabelShort + ": gave " + captured.ToString("0.##") + " Gy; " + Line(pawn)
                        + (RadiationPart.On ? "" : " The radiation model is off in the settings, so no course runs."));
                }
            }));
        }
        return nodes;
    }

    [DebugAction(DebugCategory.Name, "Radiation: remove all radiation from selected pawns", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void RemoveAll()
    {
        foreach (Pawn pawn in DebugSelection.SelectedThings<Pawn>().ToList())
        {
            int removed = Remove(pawn);
            Log.Message(MedicalMod.LogPrefix + " " + pawn.LabelShort + ": removed " + removed + " radiation condition" + (removed == 1 ? "" : "s") + ".");
        }
    }

    [DebugAction(DebugCategory.Name, "Radiation: log the dose of selected pawns", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void LogDose()
    {
        var text = new StringBuilder(MedicalMod.LogPrefix);
        foreach (Pawn pawn in DebugSelection.SelectedThings<Pawn>()) text.Append('\n').Append(pawn.LabelShort).Append(": ").Append(Line(pawn));
        Log.Message(text.ToString());
    }

    /// <summary>Removes our three radiation conditions and any of Rimatomics' (so none are converted back), and returns how many went.</summary>
    static int Remove(Pawn pawn)
    {
        if (pawn.health?.hediffSet == null) return 0;
        var gone = new List<Hediff>();
        foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
        {
            if (hediff.def == RadiationDefOf.PhobosRadiation_Dose || hediff.def == RadiationDefOf.PhobosRadiation_EarlySickness
                || hediff.def == RadiationDefOf.PhobosRadiation_Illness || RimatomicsConversion.IsRimatomics(hediff.def))
            {
                gone.Add(hediff);
            }
        }
        foreach (Hediff hediff in gone) pawn.health.RemoveHediff(hediff);
        return gone.Count;
    }

    /// <summary>The dose, the course and what decides it, in one line.</summary>
    static string Line(Pawn pawn)
    {
        if (RadiationService.DoseOf(pawn, create: false) is not { } dose) return "no dose.";
        var line = new StringBuilder()
            .Append("effective ").Append(dose.Effective.ToString("0.00")).Append(" Gy, lifetime ").Append(dose.lifetime.ToString("0.00"))
            .Append(" Gy (category ").Append(RadiationRule.ExposureCategory(dose.lifetime)).Append("), beyond repair ").Append(dose.beyondRepair.ToString("0.00"))
            .Append(" Gy, last from ").Append(dose.lastSource ?? "unknown");
        if (!dose.InCourse) return line.Append("; no course.").ToString();
        double days = (Find.TickManager.TicksGame - dose.courseStart) / (double)GenDate.TicksPerDay;
        double care = RadiationService.Care(pawn, PhobosRadiationRulesDef.Current);
        double lethal = RadiationRule.LethalDose(RadiationPart.Preset, care, RadiationService.Sensitivity(pawn, 2));
        return line.Append("; course day ").Append(days.ToString("0.0")).Append(", peak ").Append(dose.peak.ToString("0.00"))
            .Append(" Gy, ").Append(dose.willVomit ? (dose.firstVomit >= 0 ? "vomited" : "will vomit at " + RadiationRule.HoursToVomit(dose.peak).ToString("0.0") + " h") : "no vomiting")
            .Append(", syndrome ").Append(RadiationRule.Syndrome(dose.peak)).Append(", care ").Append(care.ToString("0.00"))
            .Append(", lethal at ").Append(lethal.ToString("0.0")).Append(" Gy for this person.").ToString();
    }
}
