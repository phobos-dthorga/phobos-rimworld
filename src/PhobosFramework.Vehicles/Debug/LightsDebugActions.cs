using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using LudeonTK;
using Phobos.Framework.Debug;
using RimWorld;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Says what lights a place (Framework 0.120.1; the owner's report of 17 Nov 2026: a white glare over the market that never went out, and
/// nothing in the save to explain it). A custom glower has no thing behind it and is never saved, so only the running game can say what is
/// lit: the things with a glower the light grid holds lit, our own vehicle lights with the vehicle each belongs to, and how many lights
/// with no thing behind them are somebody else's. The grid's two lists are private and read by name; nothing is changed by reading them.
/// </summary>
public static class LightsDebugActions
{
    const float Reach = 20f;

    static readonly AccessTools.FieldRef<GlowGrid, HashSet<CompGlower>> LitGlowers = AccessTools.FieldRefAccess<GlowGrid, HashSet<CompGlower>>("litGlowers");
    static readonly AccessTools.FieldRef<GlowGrid, HashSet<int>> LitCustom = AccessTools.FieldRefAccess<GlowGrid, HashSet<int>>("litCustom");

    [DebugAction(DebugCategory.Name, "Lights: what lights this place?", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void WhatLightsThis()
    {
        Map map = Find.CurrentMap;
        IntVec3 at = UI.MouseCell();
        if (map == null || !at.InBounds(map)) return;
        var text = new StringBuilder();
        text.AppendLine("Lights within " + Reach.ToString("0") + " cells of " + at + " (the game calls it " + map.glowGrid.GroundGlowAt(at).ToStringPercent() + " lit):");

        text.AppendLine("Things that glow, nearest first (radius, colour):");
        var glowers = LitGlowers(map.glowGrid).Where(g => g?.parent is { Spawned: true } && g.parent.Position.InHorDistOf(at, Reach + g.GlowRadius))
            .OrderBy(g => g.parent.Position.DistanceTo(at)).ToList();
        foreach (CompGlower glower in glowers)
        {
            text.AppendLine("  " + glower.parent.LabelCap + " (" + glower.parent.def.defName + ", from " + (glower.parent.def.modContentPack?.Name ?? "the game")
                + ") at " + glower.parent.Position + ", " + glower.parent.Position.DistanceTo(at).ToString("0") + " cells away: radius "
                + glower.GlowRadius.ToString("0.#") + ", colour " + glower.GlowColor);
        }
        if (glowers.Count == 0) text.AppendLine("  none");

        text.AppendLine("Vehicle lights of ours on this map:");
        var ours = VehicleLight.AllLit.Where(l => l.Map == map).ToList();
        foreach (VehicleLight light in ours.OrderBy(l => l.Cell.DistanceTo(at)))
        {
            text.AppendLine("  " + light.Kind + " of " + (light.Owner?.LabelShort ?? "no vehicle") + " (" + (light.Owner?.ThingID ?? "?") + ") at " + light.Cell + ", "
                + light.Cell.DistanceTo(at).ToString("0") + " cells away: radius " + light.Radius.ToString("0.#") + (light.Orphaned ? "  << ITS VEHICLE HAS GONE" : ""));
        }
        if (ours.Count == 0) text.AppendLine("  none");

        HashSet<int> custom = LitCustom(map.glowGrid);
        var known = new HashSet<int>(ours.Select(l => l.Handle));
        int others = custom.Count(handle => !known.Contains(handle));
        text.AppendLine("Lights with no thing behind them on this map: " + custom.Count + " (" + ours.Count + " ours, " + others
            + " not in our list: another mod's, or one of ours lit by a version before 0.120.1 this session; where they are, the game does not say).");
        if (others > 0) text.AppendLine("  If the glare goes when you save and load, it was one of these: they are never saved.");

        Log.Message(VehicleSystemsMod.LogPrefix + " " + text.ToString().TrimEnd());
        Messages.Message("The lights near " + at + " are listed in the log (" + glowers.Count + " things, " + ours.Count + " vehicle lights, " + others + " other).",
            MessageTypeDefOf.NeutralEvent, historical: false);
    }

    [DebugAction(DebugCategory.Name, "Lights: put out vehicle lights left behind", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void PutOutOrphans()
    {
        int count = VehicleLight.PutOutOrphans();
        string text = count == 0 ? "No vehicle light of ours was left behind." : count + " vehicle light(s) left behind were put out; the log names each.";
        Log.Message(VehicleSystemsMod.LogPrefix + " " + text);
        Messages.Message(text, MessageTypeDefOf.NeutralEvent, historical: false);
    }
}
