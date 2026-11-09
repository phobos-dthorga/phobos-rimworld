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
        var wearer = radio.Wearer;
        var unpowered = link == null && wearer?.Map != null && RadioNet.For(wearer.Map).HasUnpoweredStation;
        var label = unpowered
            ? "PhobosRadio_GizmoNoPower".Translate()
            : link == null
                ? "PhobosRadio_GizmoNoStation".Translate()
                : "PhobosRadio_GizmoLabel".Translate(LinkText.Bars(link!.Outcome).ToString().Named("BARS"));
        var description = unpowered ? "PhobosRadio_StationNoPower".Translate().Resolve() : LinkText.Describe(link, mode);
        return new Command_Action
        {
            defaultLabel = label,
            defaultDesc = radio.parent.LabelCap + "\n" + description,
            icon = RadioTextures.Signal,
            action = () =>
            {
                if (wearer == null) return;
                var map = wearer.Map;
                Find.WindowStack.Add(new Dialog_LinkBudget(wearer.LabelShortCap + ": " + radio.parent.LabelCap, () =>
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
        foreach (var pair in pairs)
        {
            var wearer = (pair.A.Thing as RimWorld.Apparel)?.Wearer;
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
