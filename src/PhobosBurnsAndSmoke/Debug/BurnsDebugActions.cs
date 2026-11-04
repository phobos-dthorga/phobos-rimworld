using System.Text;
using LudeonTK;
using Phobos.BurnsAndSmoke.Rules;
using Phobos.BurnsAndSmoke.Services;
using Phobos.Framework.Debug;
using Phobos.Framework.Uninstall;
using Verse;

namespace Phobos.BurnsAndSmoke.Debug;

/// <summary>Dev-mode reads, one forced airway burn for testing, and the uninstall sweep.</summary>
public static class BurnsDebugActions
{
    [DebugAction(DebugCategory.Name, "Burns: log burn load of selected pawns", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void LogBurnLoad()
    {
        var text = new StringBuilder(BurnsAndSmokeMod.LogPrefix);
        foreach (object selected in Find.Selector.SelectedObjects)
        {
            if (selected is not Pawn pawn) continue;
            BurnLoad load = BurnLoadService.Load(pawn);
            BurnShockReadout readout = BurnLoadService.Readout(pawn);
            text.Append('\n').Append(pawn.LabelShort)
                .Append(": burned ").Append((load.Tbsa * 100f).ToString("0.0")).Append("% of the body, ")
                .Append((load.TendedFraction * 100f).ToString("0")).Append("% of it tended; Baux ").Append(readout.Baux.ToString("0"))
                .Append(" (age ").Append(readout.AgeYears.ToString("0")).Append(readout.InhalationInjury ? ", inhalation injury" : "")
                .Append("), shock target ").Append((readout.Target * 100f).ToString("0")).Append('%')
                .Append(readout.InBed ? ", in bed" : "")
                .Append("; smoke breathed ").Append(Framework.Gases.PhobosGases.Breathed(pawn, Framework.Gases.GasKind.Smoke).ToString("0.00"))
                .Append(", carbon monoxide breathed ").Append(SmokeExposureService.BreathedCarbonMonoxide(pawn, Framework.Gases.PhobosGases.Breathed(pawn, Framework.Gases.GasKind.Smoke)).ToString("0.00"));
        }
        Log.Message(text.ToString());
    }

    [DebugAction(DebugCategory.Name, "Burns: start an airway burn on selected pawns", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void StartAirwayBurn()
    {
        foreach (object selected in Find.Selector.SelectedObjects)
        {
            if (selected is Pawn pawn && !pawn.Dead) AirwayService.Start(pawn, 1f);
        }
    }

    [DebugAction(DebugCategory.Name, "Burns: remove every D'thorga burn condition from every pawn", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
    public static void RemoveAll()
    {
        int removed = BurnsAndSmokeMod.Pack == null ? 0 : CleanupService.RemoveHediffsOf(BurnsAndSmokeMod.Pack);
        Log.Message(BurnsAndSmokeMod.LogPrefix + " removed " + removed + " condition" + (removed == 1 ? "" : "s") + ". Save now, then the mod can be switched off.");
    }
}
