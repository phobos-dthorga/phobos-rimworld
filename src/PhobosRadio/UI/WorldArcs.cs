using System.Collections.Generic;
using Phobos.Radio.Physics;
using Phobos.Radio.Services;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Phobos.Radio.UI;

/// <summary>
/// Lines along the globe for the radio's world-map pictures (Radio 0.54.0): an arc between two points in short straight pieces, and the
/// colony's contact arcs. Moved here from the vehicles tie-in's reach drawing, which uses it too. Presentation only.
/// </summary>
public static class WorldArcs
{
    /// <summary>How far above the globe a line sits, and how long each straight piece is, in degrees (authored).</summary>
    const float Lift = 0.06f;
    const float DegreesPerPiece = 1.5f;

    static readonly Dictionary<LinkOutcome, Material> byOutcome = new();

    /// <summary>The line material for a link of this outcome, in the colours the radio uses everywhere.</summary>
    public static Material For(LinkOutcome outcome)
    {
        if (byOutcome.TryGetValue(outcome, out Material? material) && material != null) return material;
        Color colour = RadioPathView.OutcomeColor(outcome);
        colour.a = 0.85f;
        return byOutcome[outcome] = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.WorldOverlayTransparent, colour, 3590);
    }

    /// <summary>A line along the globe's surface, in short straight pieces.</summary>
    public static void Arc(Vector3 a, Vector3 b, Material material, float width)
    {
        float radius = a.magnitude + Lift;
        float degrees = Vector3.Angle(a, b);
        int pieces = Mathf.Max(1, Mathf.CeilToInt(degrees / DegreesPerPiece));
        Vector3 previous = a.normalized * radius;
        for (int i = 1; i <= pieces; i++)
        {
            Vector3 point = Vector3.Slerp(a, b, i / (float)pieces).normalized * radius;
            GenDraw.DrawWorldLineBetween(previous, point, material, width);
            previous = point;
        }
    }

    /// <summary>
    /// The colony's contact arcs (Radio 0.54.0; the owner's pick of 27 Nov 2026): a line to each faction settlement its stations reach, in
    /// the colour of the line's outcome, as the logbook's "Settlements in reach" lists them. Straight to the settlement, though a call may
    /// pass through relays: its colour is the chain's weakest hop.
    /// <para>Cost: only while a colony with a station is selected on the world map; the settlements are found once a game hour
    /// (<see cref="RadioDiplomacy.InReach"/>), and a few arcs drawn a frame.</para>
    /// </summary>
    public static void DrawContacts(Map map)
    {
        IReadOnlyList<FactionDoctorLink> links = RadioDiplomacy.InReach(map);
        if (links.Count == 0) return;
        WorldGrid grid = Find.WorldGrid;
        Vector3 home = grid.GetTileCenter(map.Tile);
        foreach (FactionDoctorLink link in links)
        {
            LinkOutcome line = RadioDiplomacy.Line(link);
            Arc(home, grid.GetTileCenter(link.Settlement.Tile), For(line), line >= LinkOutcome.Workable ? 1f : 0.7f);
        }
        if (Seen.Wants("radio.contact-arcs", null)) Seen.Note("radio.contact-arcs", null, links.Count + " settlements in reach");
    }
}
