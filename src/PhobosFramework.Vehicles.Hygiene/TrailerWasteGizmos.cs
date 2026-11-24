using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.VehicleSystems.Hygiene;

/// <summary>
/// The waste tank's buttons on a trailer (Framework 0.283.0; the owner's cards of 25 Nov 2026): how full it gets before colonists empty it,
/// and its dump valve. Its stand-in tank lies hidden and cannot be selected, so the trailer carries them. Presentation only: each reads the
/// comp and calls one of its service methods.
/// </summary>
public static class TrailerWasteGizmos
{
    /// <summary>The levels offered: preset buttons, not a slider (the owner's way, 10 Nov 2026); 1 means never.</summary>
    static readonly float[] Levels = { 0.1f, 0.25f, 0.5f, 0.75f, 1f };

    /// <summary>Dubs Bad Hygiene's sewage outlet, the dump valve's picture, with ours behind it.</summary>
    const string DumpIcon = "DBH/UI/outlet";

    /// <summary>The game's own "set target fuel level" picture, which Dubs' septic tanks use for the same setting.</summary>
    const string LevelIcon = "UI/Commands/SetTargetFuelLevel";

    public static IEnumerable<Gizmo> For(CompPhobosTrailerPlumbing plumbing)
    {
        yield return EmptyAtButton(plumbing);
        if (VehicleSystemsMod.Settings.FlagOn("trailerWasteDump")) yield return DumpButton(plumbing);
    }

    static Gizmo EmptyAtButton(CompPhobosTrailerPlumbing plumbing) => new Command_Action
    {
        defaultLabel = (plumbing.EmptyAt >= 1f
            ? "PhobosTrailerPlumbing_GizmoEmptyAtNever".Translate()
            : "PhobosTrailerPlumbing_GizmoEmptyAt".Translate(plumbing.EmptyAt.ToStringPercent().Named("PERCENT"))).Resolve(),
        defaultDesc = "PhobosTrailerPlumbing_GizmoEmptyAtDesc".Translate().Resolve(),
        icon = SystemIcons.IfExistsCached(LevelIcon) ?? SystemIcons.For("Phobos/VehicleSystems/Tank/Hold"),
        action = () => Find.WindowStack.Add(new FloatMenu(EmptyAtOptions(plumbing))),
    };

    static List<FloatMenuOption> EmptyAtOptions(CompPhobosTrailerPlumbing plumbing)
    {
        var options = new List<FloatMenuOption>();
        foreach (float level in Levels)
        {
            float captured = level;
            string label = (level >= 1f
                ? "PhobosTrailerPlumbing_OptionEmptyNever".Translate()
                : "PhobosTrailerPlumbing_OptionEmptyAt".Translate(level.ToStringPercent().Named("PERCENT"))).Resolve();
            options.Add(new FloatMenuOption(label, () => plumbing.SetEmptyAt(captured)));
        }
        return options;
    }

    static Gizmo DumpButton(CompPhobosTrailerPlumbing plumbing)
    {
        bool open = plumbing.Dumping;
        var command = new Command_Action
        {
            defaultLabel = open ? "PhobosTrailerPlumbing_GizmoDumpClose".Translate().Resolve() : "PhobosTrailerPlumbing_GizmoDump".Translate().Resolve(),
            defaultDesc = "PhobosTrailerPlumbing_GizmoDumpDesc".Translate().Resolve(),
            icon = SystemIcons.IfExistsCached(DumpIcon) ?? SystemIcons.For("Phobos/VehicleSystems/Tank/Unload"),
            action = () => plumbing.SetDumping(!open),
        };
        if (!open && plumbing.DumpRefusal() is { } refused) command.Disable(refused);
        return command;
    }
}
