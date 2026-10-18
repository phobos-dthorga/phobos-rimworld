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

    [DebugAction(Category, "Recompute radio net now", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void RecomputeNow()
    {
        var map = Find.CurrentMap;
        if (map == null) return;
        RadioNet.For(map).MarkDirty();
        Log.Message("[PhobosRadio] Net marked for recompute.");
    }
}
