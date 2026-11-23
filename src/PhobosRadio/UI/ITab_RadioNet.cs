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
            pairs.Count == 0 ? "PhobosRadio_StationNoRadios".Translate() : "PhobosRadio_StationInTouch".Translate(net.InTouchCount(station).Named("INTOUCH"), pairs.Count.Named("TOTAL")));
        Widgets.Label(new Rect(outer.x, outer.y + 24f, outer.width, 24f), "PhobosRadio_StationAntenna".Translate(net.AntennaLabelFor(station).ToString().Named("ANTENNA")));
        // The channels, a line for each kind in use (Radio 0.8.0), as tall as it needs.
        var channelsText = LinkText.ChannelsLine(net.ChannelsOf(station));
        var channelsHeight = Text.CalcHeight(channelsText, outer.width);
        Widgets.Label(new Rect(outer.x, outer.y + 48f, outer.width, channelsHeight), channelsText);

        var top = 54f + channelsHeight;
        var list = new Rect(outer.x, outer.y + top, outer.width, outer.height - top);
        const float rowHeight = 52f;
        var view = new Rect(0f, 0f, list.width - 16f, pairs.Count * rowHeight);
        Widgets.BeginScrollView(list, ref scroll, view);
        var y = 0f;
        foreach (var pair in pairs)
        {
            var row = new Rect(0f, y, view.width, rowHeight);
            if (Mouse.IsOver(row)) Widgets.DrawHighlight(row);
            // A hand-held's wearer, or the vehicle a radio is built into (Radio 0.10.0).
            var wearer = CompRadio.HolderOf(pair.A.Thing);
            var who = wearer?.LabelShortCap ?? pair.A.Thing.LabelCap.ToString();
            var isBest = wearer != null && net.TryGetBest(wearer, out var best) && best != null && best.B.Thing == station;
            // Which channel serves them here, that the net is busy for them (Radio 0.7.0), or that a vehicle stands by (0.10.0).
            var channel = net.ChannelOf(station, pair.A.Thing);
            var where = channel >= 0
                ? " " + "PhobosRadio_TabOnChannel".Translate((channel + 1).Named("CHANNEL"))
                : wearer != null && net.IsBusy(wearer) ? " " + "PhobosRadio_TabBusy".Translate()
                : wearer != null && net.IsStandingBy(wearer) ? " " + "PhobosRadio_TabStandingBy".Translate() : "";
            var head = $"{LinkText.Bars(pair.Outcome)} {who}: {LinkText.OutcomeLabel(pair.Outcome)}" +
                       (isBest ? " " + "PhobosRadio_TabBestStation".Translate() : "") + where;
            Widgets.Label(new Rect(row.x + 4f, row.y + 2f, row.width - 8f, 24f), head);
            var detail = mode == DisplayMode.Plain
                ? (pair.InTouch ? "" : "PhobosRadio_PlainReason".Translate(LinkText.PlainReason(pair).ToString().Named("REASON")).ToString())
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
