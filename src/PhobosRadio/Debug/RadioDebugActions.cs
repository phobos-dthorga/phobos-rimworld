using System.Text;
using LudeonTK;
using Phobos.Radio.Services;
using Phobos.Radio.UI;
using RimWorld;
using Verse;

namespace Phobos.Radio;

/// <summary>Read-only readouts in the dev menu. Nothing here changes saved state.</summary>
public static class RadioDebugActions
{
    private const string Category = "Phobos Radio";

    [DebugAction(Category, "Log link budgets of selected pawns", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void LogSelectedLinks()
    {
        var map = Find.CurrentMap;
        if (map == null) return;
        var net = RadioNet.For(map);
        var sb = new StringBuilder();
        foreach (var selected in Find.Selector.SelectedObjects)
        {
            if (selected is not Pawn pawn) continue;
            sb.AppendLine(pawn.LabelShortCap);
            if (net.TryGetBest(pawn, out var link) && link != null)
            {
                sb.AppendLine(LinkText.Describe(link, DisplayMode.Engineer));
            }
            else
            {
                sb.AppendLine("  " + "PhobosRadio_NoStation".Translate());
            }
        }
        Log.Message(sb.Length == 0 ? "[PhobosRadio] No pawn selected." : "[PhobosRadio]\n" + sb);
    }

    [DebugAction(Category, "Log noise at cell", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void LogNoiseAtCell()
    {
        var map = Find.CurrentMap;
        if (map == null) return;
        var cell = Verse.UI.MouseCell();
        var noise = RadioNet.For(map).NoiseAt(cell);
        var roof = ObstructionService.RoofLossDb(map, cell, RadioRulesDef.Current);
        Log.Message($"[PhobosRadio] {cell}: local noise {(double.IsNegativeInfinity(noise) ? "none" : noise.ToString("0.0") + " dBm")}, roof loss {roof:0} dB");
    }

    /// <summary>
    /// The coverage overlay's cost on this colony (Radio 0.2.0): one whole pass for each selected station or antenna, timed. Performance is
    /// the mod's first concern, so this is how its rules' budget is tuned. It pauses the game for as long as the pass takes.
    /// </summary>
    [DebugAction(Category, "Time coverage of selected station or antenna", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void TimeCoverage()
    {
        var map = Find.CurrentMap;
        if (map == null) return;
        var coverage = RadioCoverage.For(map);
        var sb = new StringBuilder();
        foreach (var selected in Find.Selector.SelectedObjects)
        {
            if (selected is not Thing thing || (thing.TryGetComp<CompRadio>()?.Props.isStation != true && thing.TryGetComp<CompAntenna>() == null)) continue;
            var (milliseconds, cells, ends) = coverage.TimeFullPass(thing);
            var budget = RadioRulesDef.Current.coverageBudgetMsPerFrame;
            sb.AppendLine($"  {thing.LabelCap}: {cells} cells through {ends} antenna(s) in {milliseconds:0.0} ms " +
                          $"({(cells == 0 ? 0 : milliseconds * 1000 / cells):0.0} µs a cell); about {milliseconds / budget:0} frames at {budget} ms a frame");
        }
        Log.Message(sb.Length == 0
            ? "[PhobosRadio] Select a base station or an antenna first."
            : $"[PhobosRadio] Coverage, one whole pass ({map.Size.x} x {map.Size.z} map):\n{sb}");
    }

    [DebugAction(Category, "Recompute radio net now", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void RecomputeNow()
    {
        var map = Find.CurrentMap;
        if (map == null) return;
        RadioNet.For(map).MarkDirty();
        Log.Message("[PhobosRadio] Net marked for recompute.");
    }
}
