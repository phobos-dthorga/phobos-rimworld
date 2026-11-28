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

    /// <summary>Select people: logs each one's best radio link, in engineer's figures, or that they reach no station. Reads
    /// only.</summary>
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

    /// <summary>Click a cell on the map: logs the local radio noise there and the loss its roof gives. Reads only.</summary>
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

    /// <summary>Marks the current map's radio net to be worked out again from its stations and antennas at its next use. Nothing saved
    /// changes.</summary>
    [DebugAction(Category, "Recompute radio net now", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void RecomputeNow()
    {
        var map = Find.CurrentMap;
        if (map == null) return;
        RadioNet.For(map).MarkDirty();
        Log.Message("[PhobosRadio] Net marked for recompute.");
    }

    /// <summary>Radio 0.36.0: a neighbour asks now, for testing, if any can (the letter, as by chance).</summary>
    [DebugAction(Category, "Neighbour asks for help now", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void NeighbourAsksNow() => RadioNeighbours.TryAsk(forced: true);

    /// <summary>Radio 0.73.0 (RN4): who on this map is listening for calls now, and who the selective call would wake. Reads only.</summary>
    [DebugAction(Category, "Radio watch: who is listening", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void WhoIsListening()
    {
        RadioWatchState state = RadioWatch.Of(Find.CurrentMap, fresh: true);
        Log.Message($"[PhobosRadio] Radio watch: station {state.HasStation}; at desks [{string.Join(", ", state.AtDesks.ConvertAll(p => p.LabelShort))}]; " +
            $"on hand-helds [{string.Join(", ", state.OnHandhelds.ConvertAll(p => p.LabelShort))}]; on watch [{string.Join(", ", state.Watchers.ConvertAll(p => p.LabelShort))}]; " +
            $"selective call {(state.SelectiveCallStation?.LabelShort ?? "none")}, would wake {(state.OnCall?.LabelShort ?? "nobody")}.");
    }

    /// <summary>Radio 0.73.0 (RN5): the standing order to ask a trader in, now rather than at its hour, for testing; says why when it does not.</summary>
    [DebugAction(Category, "Standing orders: ask a trader now", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void AskTraderNow()
    {
        if (RadioStandingOrders.TryAskTrader(Find.CurrentMap) is { } reason) Messages.Message(reason, MessageTypeDefOf.RejectInput, historical: false);
    }

    /// <summary>Radio 0.63.0: each selected colonist calls someone for a chat on the air now, the chance skipped, for testing.</summary>
    [DebugAction(Category, "Chat on the air now (selected)", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void ChatNow()
    {
        var sb = new StringBuilder();
        foreach (var selected in Find.Selector.SelectedObjects)
        {
            if (selected is not Pawn pawn) continue;
            sb.AppendLine($"  {pawn.LabelShortCap}: {(RadioTalk.ChatNow(pawn) ? "chatting" : "no chat (not in touch, busy, or nobody out of earshot to call)")}");
        }
        Log.Message(sb.Length == 0 ? "[PhobosRadio] Select a colonist first." : "[PhobosRadio] Chat on the air:\n" + sb);
    }

    /// <summary>Radio 0.63.0: the evening calls to colonists away in caravans, now, whatever the hour, for testing.</summary>
    [DebugAction(Category, "Evening calls now", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void EveningCallsNow()
    {
        if (Find.CurrentMap is { } map) RadioTalk.EveningNow(map);
    }
}
