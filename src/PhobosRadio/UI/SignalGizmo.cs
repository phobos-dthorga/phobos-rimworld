using Phobos.Radio.Services;
using UnityEngine;
using Verse;

namespace Phobos.Radio.UI;

[StaticConstructorOnStartup]
public static class RadioTextures
{
    public static readonly Texture2D Signal = ContentFinder<Texture2D>.Get("Phobos/Radio/UI/Signal");
}

/// <summary>The signal-bars gizmo on a worn hand-held or a station. Reads the net; delegates nothing yet.</summary>
public static class SignalGizmo
{
    public static Gizmo ForHandheld(CompRadio radio, LinkEvaluation? link)
    {
        var mode = RadioMod.Settings.displayMode;
        // The wearer of a hand-held, or the vehicle a radio is built into (Radio 0.10.0).
        var wearer = radio.Holder;
        var unpowered = link == null && wearer?.Map != null && RadioNet.For(wearer.Map).HasUnpoweredStation;
        // In touch, but every channel that can hear it is taken (Radio 0.7.0).
        var busy = link != null && wearer?.Map != null && RadioNet.For(wearer.Map).IsBusy(wearer);
        var standingBy = wearer?.Map != null && RadioNet.For(wearer.Map).IsStandingBy(wearer);
        // A vehicle on a channel that nobody at its station drives yet (Radio 0.12.0).
        var waiting = radio.parent is Pawn && wearer?.Map != null && RadioNet.For(wearer.Map).WaitsForOperator(wearer);
        var label = unpowered
            ? "PhobosRadio_GizmoNoPower".Translate()
            : link == null
                ? "PhobosRadio_GizmoNoStation".Translate()
                : busy
                    ? "PhobosRadio_GizmoBusy".Translate()
                    : "PhobosRadio_GizmoLabel".Translate(LinkText.Bars(link!.Outcome).ToString().Named("BARS"));
        var description = unpowered
            ? "PhobosRadio_StationNoPower".Translate().Resolve()
            : (busy ? "PhobosRadio_NetBusy".Translate() + "\n" : "") + (standingBy ? "PhobosRadio_StandingBy".Translate() + "\n" : "") +
              (waiting ? "PhobosRadio_WaitingOperator".Translate() + "\n" : "") +
              LinkText.Describe(link, mode);
        // A built-in radio's own label is the vehicle's: name the radio's kind instead.
        var title = radio.parent is Pawn ? radio.Kind.LabelCap.ToString() : radio.parent.LabelCap.ToString();
        return new Command_Action
        {
            defaultLabel = label,
            defaultDesc = title + "\n" + description,
            icon = RadioTextures.Signal,
            action = () =>
            {
                if (wearer == null) return;
                var map = wearer.Map;
                Find.WindowStack.Add(new Dialog_LinkBudget(wearer.LabelShortCap + ": " + title, () =>
                    wearer.Spawned && wearer.Map == map && RadioNet.For(map).TryGetBest(wearer, out var current) ? current : null));
            },
        };
    }

    public static Gizmo ForStation(CompRadio station, RadioNet net)
    {
        var pairs = net.PairsFor(station.parent);
        var inTouch = net.InTouchCount(station.parent);
        var lines = new System.Text.StringBuilder();
        lines.AppendLine(pairs.Count == 0
            ? "PhobosRadio_StationNoRadios".Translate()
            : "PhobosRadio_StationInTouch".Translate(inTouch.Named("INTOUCH"), pairs.Count.Named("TOTAL")));
        lines.AppendLine(LinkText.ChannelsLine(net.ChannelsOf(station.parent)));
        foreach (var pair in pairs)
        {
            var wearer = CompRadio.HolderOf(pair.A.Thing);
            lines.AppendLine($"  {LinkText.Bars(pair.Outcome)} {wearer?.LabelShortCap ?? pair.A.Thing.LabelCap}: {LinkText.OutcomeLabel(pair.Outcome)}");
        }
        return new Command_Action
        {
            defaultLabel = "PhobosRadio_GizmoStation".Translate(inTouch.Named("INTOUCH"), pairs.Count.Named("TOTAL")),
            defaultDesc = lines.ToString().TrimEnd(),
            icon = RadioTextures.Signal,
            action = () => RimWorld.InspectPaneUtility.OpenTab(typeof(ITab_RadioNet)),
        };
    }
}
