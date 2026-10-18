using Phobos.Radio.Services;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Radio.UI;

/// <summary>The station's Net tab: every worn hand-held on the map against this station, from the cache.</summary>
public class ITab_RadioNet : ITab
{
    private Vector2 scroll;

    public ITab_RadioNet()
    {
        size = new Vector2(500f, 420f);
        labelKey = "PhobosRadio_TabNet";
    }

    public override bool IsVisible => SelThing?.TryGetComp<CompRadio>()?.Props.isStation == true;

    protected override void FillTab()
    {
        var station = SelThing;
        if (station == null || !station.Spawned) return;
        var net = RadioNet.For(station.Map);
        var pairs = net.PairsFor(station);
        var mode = RadioMod.Settings.displayMode;

        var outer = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
        Text.Font = GameFont.Small;
        Widgets.Label(new Rect(outer.x, outer.y, outer.width, 24f),
            pairs.Count == 0 ? "PhobosRadio_StationNoRadios".Translate() : "PhobosRadio_StationInTouch".Translate(net.InTouchCount(station), pairs.Count));
        Widgets.Label(new Rect(outer.x, outer.y + 24f, outer.width, 24f), "PhobosRadio_StationAntenna".Translate(net.AntennaLabelFor(station)));

        var list = new Rect(outer.x, outer.y + 54f, outer.width, outer.height - 54f);
        const float rowHeight = 52f;
        var view = new Rect(0f, 0f, list.width - 16f, pairs.Count * rowHeight);
        Widgets.BeginScrollView(list, ref scroll, view);
        var y = 0f;
        foreach (var pair in pairs)
        {
            var row = new Rect(0f, y, view.width, rowHeight);
            if (Mouse.IsOver(row)) Widgets.DrawHighlight(row);
            var wearer = (pair.A.Thing as Apparel)?.Wearer;
            var who = wearer?.LabelShortCap ?? pair.A.Thing.LabelCap.ToString();
            var isBest = wearer != null && net.TryGetBest(wearer, out var best) && best != null && best.B.Thing == station;
            var head = $"{LinkText.Bars(pair.Outcome)} {who}: {LinkText.OutcomeLabel(pair.Outcome)}" +
                       (isBest ? " " + "PhobosRadio_TabBestStation".Translate() : "");
            Widgets.Label(new Rect(row.x + 4f, row.y + 2f, row.width - 8f, 24f), head);
            var detail = mode == DisplayMode.Plain
                ? (pair.InTouch ? "" : "PhobosRadio_PlainReason".Translate(LinkText.PlainReason(pair)).ToString())
                : LinkText.OperatorSummary(pair);
            Widgets.Label(new Rect(row.x + 4f, row.y + 26f, row.width - 8f, 24f), detail);
            if (Widgets.ButtonInvisible(row))
            {
                var pawn = wearer;
                var thing = pair.A.Thing;
                Find.WindowStack.Add(new Dialog_LinkBudget(who, () => FindPair(net, station, thing)));
            }
            y += rowHeight;
        }
        Widgets.EndScrollView();
    }

    private static LinkEvaluation? FindPair(RadioNet net, Thing station, Thing handheld)
    {
        foreach (var pair in net.PairsFor(station)) if (pair.A.Thing == handheld) return pair;
        return null;
    }
}
