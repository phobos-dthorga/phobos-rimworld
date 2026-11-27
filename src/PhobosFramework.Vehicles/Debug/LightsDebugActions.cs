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
    static readonly AccessTools.FieldRef<GlowGrid, HashSet<CompGlower>> LitGlowers = AccessTools.FieldRefAccess<GlowGrid, HashSet<CompGlower>>("litGlowers");
    static readonly AccessTools.FieldRef<GlowGrid, HashSet<int>> LitCustom = AccessTools.FieldRefAccess<GlowGrid, HashSet<int>>("litCustom");

    static readonly System.Reflection.FieldInfo? LightsField = AccessTools.Field(typeof(GlowGrid), "lights");

    /// <summary>
    /// The light grid's own records, copied out (Framework 0.120.2): where each light really is, its reach and what lit it. A thing's light
    /// is recorded at the cell the thing stood on when it was lit, and the game does not move it when the thing moves, so a light can stand
    /// where its thing no longer is. Read by name through Unity's list (AsArray, then ToArray); null if the game's shape has changed.
    /// </summary>
    static Verse.Glow.GlowLight[]? Records(GlowGrid grid, out string? problem)
    {
        problem = null;
        try
        {
            object? list = LightsField?.GetValue(grid);
            if (list == null) { problem = "the light grid has no list of lights"; return null; }
            object? array = list.GetType().GetMethod("AsArray", System.Type.EmptyTypes)?.Invoke(list, null);
            object? copy = array?.GetType().GetMethod("ToArray", System.Type.EmptyTypes)?.Invoke(array, null);
            if (copy is Verse.Glow.GlowLight[] records) return records;
            problem = "the light grid's list could not be copied";
        }
        catch (System.Exception error)
        {
            problem = "reading the light grid failed: " + error.GetType().Name + ": " + error.Message;
        }
        return null;
    }

    /// <summary>Click a cell on the map: logs every light reaching it (things with a glower, our vehicle lights with their vehicle,
    /// lights with no thing behind them) and the sky's share, and flags lights left behind by a thing that moved. Reads only.</summary>
    [DebugAction(DebugCategory.Name, "Lights: what lights this place?", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void WhatLightsThis()
    {
        Map map = Find.CurrentMap;
        IntVec3 at = UI.MouseCell();
        if (map == null || !at.InBounds(map)) return;
        var text = new StringBuilder();
        text.AppendLine("Lights reaching " + at + " (the game calls it " + map.glowGrid.GroundGlowAt(at).ToStringPercent() + " lit):");
        // Framework 0.120.4 (the owner's glare of 17 Nov 2026 was daylight on an open courtyard ringed by overhead mountain): the sky lights
        // every cell with no roof, and lamps alone bring a cell to at most 50 % (the game's own cap) unless it is overlit, at exactly 100 %.
        RoofDef? roof = at.GetRoof(map);
        float sky = map.skyManager.CurSkyGlow;
        text.AppendLine(roof == null
            ? "  The sky: no roof here, so the sky lights it at " + sky.ToStringPercent() + " now. Lamps alone give at most 50 %; anything between that and 100 % is the sky."
            : "  The sky: roofed (" + roof.LabelCap + "), so the sky gives nothing here.");

        // Who lit each thing's light, by thing ID: the glowers the grid holds lit.
        var byId = new Dictionary<int, CompGlower>();
        foreach (CompGlower glower in LitGlowers(map.glowGrid))
        {
            if (glower?.parent != null) byId[glower.parent.thingIDNumber] = glower;
        }
        var ours = VehicleLight.AllLit.Where(l => l.Map == map).ToDictionary(l => l.Handle);

        Verse.Glow.GlowLight[]? records = Records(map.glowGrid, out string? problem);
        int reaching = 0, strays = 0;
        if (records == null)
        {
            text.AppendLine("  The game's own records could not be read (" + problem + "); only the lit things' own places can be listed below.");
        }
        else
        {
            foreach (Verse.Glow.GlowLight light in records.OrderBy(l => l.position.DistanceTo(at)))
            {
                float reach = UnityEngine.Mathf.Max(light.glowRadius, light.overlightRadius);
                float distance = light.position.DistanceTo(at);
                if (distance > reach + 0.5f) continue;
                reaching++;
                string what;
                if (light.isTerrain) what = "glowing ground: " + (light.position.GetTerrain(map)?.LabelCap.Resolve() ?? "?");
                else if (light.isCustom) what = ours.TryGetValue(light.id, out VehicleLight? mine)
                    ? "our " + mine.Kind + " of " + (mine.Owner?.LabelShort ?? "no vehicle") + (mine.Orphaned ? "  << ITS VEHICLE HAS GONE" : "")
                    : "a light with no thing behind it, from another mod (handle " + light.id + ")";
                else if (byId.TryGetValue(light.id, out CompGlower? glower))
                {
                    Thing thing = glower.parent;
                    string from = thing.def.modContentPack?.Name ?? "the game";
                    string where = thing.Destroyed ? "DESTROYED" : !thing.Spawned ? "NOT ON THE MAP" : thing.Map != map ? "ON ANOTHER MAP"
                        : thing.Position == light.position ? "still there" : "MOVED: now at " + thing.Position;
                    bool stray = where != "still there";
                    if (stray) strays++;
                    what = thing.LabelCap + " (" + thing.def.defName + ", " + thing.ThingID + ", from " + from + "): " + where + (stray ? "  << ITS LIGHT STAYED BEHIND" : "");
                }
                else
                {
                    strays++;
                    what = "a thing's light whose thing the grid no longer holds lit (thing ID " + light.id + ")  << LEFT BEHIND";
                }
                text.AppendLine("  at " + light.position + ", " + distance.ToString("0") + " cells away, radius " + light.glowRadius.ToString("0.#")
                    + (light.overlightRadius > 0f ? ", overlit to " + light.overlightRadius.ToString("0.#") : "") + ", colour " + light.glowColor + ": " + what);
            }
            if (reaching == 0) text.AppendLine("  No light of any thing, ground or mod reaches this cell (" + records.Length + " lights on the map): what lights it is the sky, or something drawn.");
        }

        Log.Message(VehicleSystemsMod.LogPrefix + " " + text.ToString().TrimEnd());
        Messages.Message("The lights reaching " + at + " are listed in the log (" + reaching + " lights" + (strays > 0 ? ", " + strays + " left behind by a thing that moved or went" : "") + ").",
            strays > 0 ? MessageTypeDefOf.CautionInput : MessageTypeDefOf.NeutralEvent, historical: false);
    }

    /// <summary>Puts out every vehicle light of ours whose vehicle no longer stands on its map, and logs which, so the way it was left
    /// behind can be found. Needs a map; the lights are not saved, so this only clears the running game.</summary>
    [DebugAction(DebugCategory.Name, "Lights: put out vehicle lights left behind", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void PutOutOrphans()
    {
        int count = VehicleLight.PutOutOrphans();
        string text = count == 0 ? "No vehicle light of ours was left behind." : count + " vehicle light(s) left behind were put out; the log names each.";
        Log.Message(VehicleSystemsMod.LogPrefix + " " + text);
        Messages.Message(text, MessageTypeDefOf.NeutralEvent, historical: false);
    }
}
