using Phobos.Radio.Physics;
using Phobos.Radio.Services;
using UnityEngine;
using Verse;

namespace Phobos.Radio.UI;

[StaticConstructorOnStartup]
public static class RadioTextures
{
    public static readonly Texture2D Signal = ContentFinder<Texture2D>.Get("Phobos/Radio/UI/Signal");

    // One icon per outcome for the Radio button (Radio 0.13.0; drawn by scripts/art/draw_signal_bars.py, the owner's pick of 25 Nov 2026).
    private static readonly Texture2D SignalNone = ContentFinder<Texture2D>.Get("Phobos/Radio/UI/SignalNone");
    private static readonly Texture2D SignalMarginal = ContentFinder<Texture2D>.Get("Phobos/Radio/UI/SignalMarginal");
    private static readonly Texture2D SignalWorkable = ContentFinder<Texture2D>.Get("Phobos/Radio/UI/SignalWorkable");
    private static readonly Texture2D SignalClean = ContentFinder<Texture2D>.Get("Phobos/Radio/UI/SignalClean");

    /// <summary>The bars for a link's outcome; no link is no contact.</summary>
    public static Texture2D SignalFor(LinkOutcome? outcome) => outcome switch
    {
        LinkOutcome.Clean => SignalClean,
        LinkOutcome.Workable => SignalWorkable,
        LinkOutcome.Marginal => SignalMarginal,
        _ => SignalNone,
    };
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
                    : "PhobosRadio_GizmoLabel".Translate(LinkText.OutcomeLabel(link!.Outcome).ToString().Named("OUTCOME"));
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
            icon = RadioTextures.SignalFor(link?.Outcome),
            // Keying the set up to listen, with its squelch tail (Radio 0.16.0, S103; agent's choice of when, for the owner to revise).
            activateSound = RadioSoundDefOf.PhobosRadio_HandheldKeyUp,
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
