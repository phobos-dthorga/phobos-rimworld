using System.Collections.Generic;
using System.Linq;
using Phobos.Radio.Physics;
using Phobos.Radio.Services;
using Phobos.VehicleSystems;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Vehicles.World;
using Verse;

namespace Phobos.Radio.Vehicles;

/// <summary>
/// Radio reach drawn on the world map (Radio 0.37.0; the owner's word of 27 Nov 2026, "1, 2, 3, and 4"): with a convoy of the player's, a
/// relay mast or a relay post selected, a ring for how far its radio reaches a home station's mast on level ground, and for a convoy its
/// chain home, hop by hop, the weakest hop in amber. Presentation only.
/// <para>Cost: nothing unless the world map shows such a thing selected; then, each frame, a ring and up to three arcs. The chain is the one
/// the convoy's own reach check last found (kept a tenth of a game hour, as that check is): drawing never searches and never takes a channel.</para>
/// </summary>
public static class ReachDrawing
{
    const float Lift = 0.06f;
    const float DegreesPerPiece = 1.5f;
    /// <summary>The far end the ring measures to: a home station's mast on level ground (authored: the mast antenna's height).</summary>
    const float HomeMastMetres = 10f;

    static readonly Dictionary<Caravan, List<(PlanetTile From, PlanetTile To, double MarginDb)>?> chains = new();
    static Material? hopMaterial, weakMaterial;
    static Material HopMaterial => hopMaterial ??= MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.WorldOverlayTransparent, new Color(0.45f, 0.9f, 0.55f, 0.75f), 3590);
    static Material WeakMaterial => weakMaterial ??= MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.WorldOverlayTransparent, new Color(1f, 0.7f, 0.25f, 0.9f), 3591);

    /// <summary>The chain a convoy's reach check found, or null when it found none. Called by that check only.</summary>
    internal static void Keep(Caravan convoy, List<(PlanetTile From, PlanetTile To, double MarginDb)>? hops)
    {
        if (chains.Count > 64) chains.Clear();
        chains[convoy] = hops;
    }

    /// <summary>Draws what is selected on the world map now.</summary>
    public static void Draw()
    {
        if (!WorldRendererUtility.WorldSelected) return;
        foreach (WorldObject selected in Find.WorldSelector.SelectedObjects)
        {
            switch (selected)
            {
                case VehicleCaravan convoy when convoy.Faction == Faction.OfPlayer:
                    if (ConvoyReachSource.ConvoyEnd(convoy, ConvoyRadio.Mast(convoy)) is not { } end) break;
                    Ring(convoy.Tile, end);
                    // Asks the convoy's reach as its line does (kept, so no new search most frames), then draws the chain it found.
                    _ = ConvoyRadio.Of(convoy);
                    if (chains.TryGetValue(convoy, out var hops) && hops is { Count: > 0 })
                    {
                        double weakest = hops.Min(h => h.MarginDb);
                        foreach (var hop in hops)
                            Arc(Find.WorldGrid.GetTileCenter(hop.From), Find.WorldGrid.GetTileCenter(hop.To), hop.MarginDb <= weakest ? WeakMaterial : HopMaterial,
                                hop.MarginDb <= weakest ? 1.2f : 0.8f);
                    }
                    break;
                case WorldObject_PhobosRelayMast or MapParent:
                    // A relay mast or a relay post: its own relay entry, as the chain search sees it (only while it is selected).
                    if (RelayNodes.All().FirstOrDefault(n => n.Owner == selected) is { } node) Ring(selected.Tile, node);
                    break;
            }
        }
    }

    /// <summary>A ring at the radio horizon between this end and a home station's mast on level ground, in world tiles.</summary>
    static void Ring(PlanetTile tile, RelayNode end)
    {
        double metres = Propagation.RadioHorizonMetres(end.End.AntennaHeightMetres + end.SiteMetres, HomeMastMetres);
        int tiles = Mathf.Clamp(Mathf.RoundToInt((float)(metres / RadioRulesDef.Current.metresPerWorldTile)), 1, 200);
        GenDraw.DrawWorldRadiusRing(tile, tiles);
    }

    /// <summary>A line along the globe's surface, in short straight pieces.</summary>
    static void Arc(Vector3 a, Vector3 b, Material material, float width)
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
}

/// <summary>Draws radio reach on the world map each frame it is shown (Radio 0.37.0). Saves nothing.</summary>
public sealed class WorldComponent_PhobosReachDrawing : WorldComponent
{
    public WorldComponent_PhobosReachDrawing(World world) : base(world) { }

    public override void WorldComponentUpdate()
    {
        if (Seen.Wants("radio.reach-drawn", null) && WorldRendererUtility.WorldSelected && Find.WorldSelector.SelectedObjects.Count > 0)
            Seen.Note("radio.reach-drawn", null, Find.WorldSelector.SelectedObjects[0].LabelCap);
        ReachDrawing.Draw();
    }
}
