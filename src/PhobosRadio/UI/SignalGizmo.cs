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
        var label = link == null
            ? "PhobosRadio_GizmoNoStation".Translate()
            : "PhobosRadio_GizmoLabel".Translate(LinkText.Bars(link.Outcome));
        var description = LinkText.Describe(link, mode);
        return new Command_Action
        {
            defaultLabel = label,
            defaultDesc = radio.parent.LabelCap + "\n" + description,
            icon = RadioTextures.Signal,
            action = () => Find.WindowStack.Add(new Dialog_MessageBox(radio.parent.LabelCap + "\n\n" + LinkText.Describe(link, DisplayMode.Engineer))),
        };
    }

    public static Gizmo ForStation(CompRadio station, RadioNet net)
    {
        var pairs = net.PairsFor(station.parent);
        var inTouch = net.InTouchCount(station.parent);
        var lines = new System.Text.StringBuilder();
        lines.AppendLine(pairs.Count == 0
            ? "PhobosRadio_StationNoRadios".Translate()
            : "PhobosRadio_StationInTouch".Translate(inTouch, pairs.Count));
        foreach (var pair in pairs)
        {
            var wearer = (pair.A.Thing as RimWorld.Apparel)?.Wearer;
            lines.AppendLine($"  {LinkText.Bars(pair.Outcome)} {wearer?.LabelShortCap ?? pair.A.Thing.LabelCap}: {LinkText.OutcomeLabel(pair.Outcome)}");
        }
        return new Command_Action
        {
            defaultLabel = "PhobosRadio_GizmoStation".Translate(inTouch, pairs.Count),
            defaultDesc = lines.ToString().TrimEnd(),
            icon = RadioTextures.Signal,
            action = () => Find.WindowStack.Add(new Dialog_MessageBox(station.parent.LabelCap + "\n\n" + lines)),
        };
    }
}
