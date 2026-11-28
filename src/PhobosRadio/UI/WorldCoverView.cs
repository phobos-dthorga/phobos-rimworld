using System.Collections;
using System.Collections.Generic;
using Phobos.Framework.Interface;
using Phobos.Radio.Physics;
using Phobos.Radio.Services;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Phobos.Radio.UI;

/// <summary>
/// The cover picture on the globe (Radio 0.42.0; docs/development/radio-world-coverage.md): each tile <see cref="WorldCover"/> has worked
/// out, painted in the radio's own colours, brighter with more signal to spare and dark red where a contact fails inside the horizon.
/// A draw layer of the planet's surface, added to its Def by patch as other mods add theirs. Presentation only.
/// <para>Cost: nothing while there is no picture to show (it is not visible, so the game neither rebuilds nor draws it). With one, the
/// mesh is rebuilt when the picture changes: once a second while it is being worked out and once at its end, a few vertices a tile.</para>
/// </summary>
public class WorldDrawLayer_PhobosRadioCover : WorldDrawLayer
{
    /// <summary>How far above the globe the picture sits, so it does not flicker against the ground (authored: just over the pollution layer's).</summary>
    const float Lift = 0.03f;
    /// <summary>How many shades the ramp is cut into for the mesh's colours.</summary>
    const int Shades = 64;

    readonly List<Vector3> verts = new();
    readonly Color32[] ramp = new Color32[Shades + 1];
    Material? material;
    int drawn = -1;

    public override bool VisibleWhenLayerNotSelected => false;
    public override bool VisibleInBackground => false;
    public override bool Visible => base.Visible && WorldCover.Showing(planetLayer);
    public override bool ShouldRegenerate => Visible && (dirty || drawn != WorldCover.Version);

    public override IEnumerable Regenerate()
    {
        // The game's own material for tiles coloured by vertex, as its grid and noise layers use.
        material ??= WorldMaterials.VertexColorTransparent;
        foreach (object item in base.Regenerate()) yield return item;
        drawn = WorldCover.Version;
        if (!WorldCover.Showing(planetLayer)) yield break;
        RadioRulesDef rules = RadioRulesDef.Current;
        byte alpha = (byte)Mathf.Clamp(Mathf.RoundToInt(rules.worldCoverOpacity * 255f), 0, 255);
        for (int i = 0; i <= Shades; i++) ramp[i] = Tint(RadioPalette.At(i / (float)Shades), alpha);
        Color32 noContact = Tint(RadioPalette.NoContact, alpha);
        float fullScale = Mathf.Max(1f, rules.worldCoverFullScaleDb);
        WorldGrid grid = Find.WorldGrid;
        IReadOnlyList<int> painted = WorldCover.Painted;
        for (int p = 0; p < painted.Count; p++)
        {
            int id = painted[p];
            float margin = WorldCover.MarginOf(id);
            Color32 colour = margin < 0f ? noContact : ramp[Mathf.Clamp(Mathf.RoundToInt((float)CoverRule.Shade(margin, fullScale) * Shades), 0, Shades)];
            LayerSubMesh subMesh = GetSubMesh(material);
            grid.GetTileVertices(new PlanetTile(id, planetLayer), verts);
            int first = subMesh.verts.Count;
            for (int v = 0, count = verts.Count; v < count; v++)
            {
                subMesh.verts.Add(verts[v] + verts[v].normalized * Lift);
                subMesh.colors.Add(colour);
                if (v >= count - 2) continue;
                subMesh.tris.Add(first + v + 2);
                subMesh.tris.Add(first + v + 1);
                subMesh.tris.Add(first);
            }
        }
        // Radio 0.48.0: a relay left out because every channel there is in use is marked on its own tile, over the picture.
        if (WorldCover.Network)
        {
            foreach ((string _, int tileId) in WorldCover.FullRelays)
            {
                LayerSubMesh subMesh = GetSubMesh(material);
                grid.GetTileVertices(new PlanetTile(tileId, planetLayer), verts);
                int first = subMesh.verts.Count;
                for (int v = 0, count = verts.Count; v < count; v++)
                {
                    subMesh.verts.Add(verts[v] + verts[v].normalized * (Lift * 1.5f));
                    subMesh.colors.Add(FullRelayMark);
                    if (v >= count - 2) continue;
                    subMesh.tris.Add(first + v + 2);
                    subMesh.tris.Add(first + v + 1);
                    subMesh.tris.Add(first);
                }
            }
        }
        FinalizeMesh(MeshParts.All);
    }

    /// <summary>A full relay's mark: a strong magenta nothing on the signal ramp uses (authored).</summary>
    static readonly Color32 FullRelayMark = new(255, 60, 200, 235);

    static Color32 Tint(Color colour, byte alpha)
    {
        Color32 tint = colour;
        tint.a = alpha;
        return tint;
    }
}

/// <summary>
/// The cover picture's button and its line in the pane, for a place on the world map with a radio (Radio 0.42.0). The button opens a menu
/// of the far-end radios the picture can be drawn for (<see cref="RadioCoverKindDef"/>) and Off; the choice is the session's, never saved,
/// and the picture then follows whichever place with a radio is selected. Presentation only: it asks <see cref="WorldCover"/>.
/// </summary>
public static class WorldCoverGizmo
{
    const string GuardKey = "The radio coverage line on the world map";

    public static IEnumerable<Gizmo> For(WorldObject place)
    {
        if (!WorldCover.MayHaveRadio(place)) yield break;
        RadioCoverKindDef? kind = WorldCover.Kind;
        yield return new Command_Action
        {
            defaultLabel = kind == null
                ? "PhobosRadio_CoverGizmoOff".Translate()
                : WorldCover.Network
                    ? kind.caller
                        ? "PhobosRadio_CoverGizmoNetwork".Translate(kind.label.Named("KIND"))
                        : "PhobosRadio_CoverGizmoPlanner".Translate(kind.label.Named("KIND"))
                    : "PhobosRadio_CoverGizmo".Translate(kind.label.Named("KIND")),
            defaultDesc = "PhobosRadio_CoverGizmoDesc".Translate(),
            // The signal picture stands in until the button's own icon is drawn.
            icon = RadioTextures.Signal,
            action = () => Find.WindowStack.Add(new FloatMenu(Options(place))),
        };
    }

    static List<FloatMenuOption> Options(WorldObject place)
    {
        var options = new List<FloatMenuOption>
        {
            new("PhobosRadio_CoverMenuOff".Translate(), () => WorldCover.Pick(null)),
        };
        WorldCoverSite? site = WorldCover.SiteOf(place);
        foreach (RadioCoverKindDef kind in RadioCoverKindDef.InOrder)
        {
            if (site == null)
            {
                options.Add(new FloatMenuOption("PhobosRadio_CoverRefuseNoRadio".Translate(kind.LabelCap.Named("KIND")), null));
                continue;
            }
            switch (WorldCover.Refusal(site, kind))
            {
                case WorldCoverRefusal.NoMorseKey:
                    options.Add(new FloatMenuOption("PhobosRadio_CoverRefuseNoKey".Translate(kind.LabelCap.Named("KIND")), null));
                    break;
                case WorldCoverRefusal.RelayPassesSpeechOnly:
                    options.Add(new FloatMenuOption("PhobosRadio_CoverRefuseRelayMorse".Translate(kind.LabelCap.Named("KIND")), null));
                    break;
                default:
                    RadioCoverKindDef picked = kind;
                    options.Add(new FloatMenuOption("PhobosRadio_CoverMenuPlace".Translate(kind.label.Named("KIND")), () => WorldCover.Pick(picked)));
                    break;
            }
        }
        // Radio 0.43.0: the whole network, home through relays, for a radio that calls. It does not depend on the place selected.
        foreach (RadioCoverKindDef kind in RadioCoverKindDef.InOrder)
        {
            if (!WorldCover.NetworkOffered(kind)) continue;
            RadioCoverKindDef picked = kind;
            // Radio 0.48.0: for a relay as the far end it is the planner: where a new relay of that kind would reach home, and so join in.
            options.Add(new FloatMenuOption(kind.caller
                ? "PhobosRadio_CoverMenuNetwork".Translate(kind.label.Named("KIND"))
                : "PhobosRadio_CoverMenuPlanner".Translate(kind.label.Named("KIND")), () => WorldCover.Pick(picked, wholeNetwork: true)));
        }
        return options;
    }

    /// <summary>The place's line about the picture, or null when the picture is off or is another place's.</summary>
    public static string? InspectLine(WorldObject place)
    {
        // Guarded: the game asks for this every frame while the place is selected (UiGuard).
        if (UiGuard.Resting(GuardKey)) return UiGuard.FallbackText;
        try
        {
            if (WorldCover.Kind is not { } kind) return null;
            NamedArgument name = kind.label.Named("KIND");
            if (WorldCover.Network)
            {
                if (WorldCover.Working) return "PhobosRadio_CoverNetworkWorking".Translate(name, WorldCover.Progress.ToStringPercent().Named("PERCENT")).Resolve();
                if (WorldCover.Painted.Count == 0) return "PhobosRadio_CoverNetworkNone".Translate(name).Resolve();
                string shown = kind.caller
                    ? "PhobosRadio_CoverNetworkShown".Translate(name, WorldCover.Reached.Named("TILES"), WorldCover.Places.Named("PLACES")).Resolve()
                    : "PhobosRadio_CoverPlannerShown".Translate(name, WorldCover.Reached.Named("TILES"), WorldCover.Places.Named("PLACES")).Resolve();
                // Radio 0.48.0: relays left out with every channel in use, by name; each is marked on the globe.
                if (WorldCover.FullRelays.Count > 0)
                {
                    var names = new List<string>();
                    foreach ((string label, int _) in WorldCover.FullRelays) names.Add(label);
                    shown += "\n" + "PhobosRadio_CoverNetworkFull".Translate(names.ToCommaList(useAnd: true).Named("RELAYS")).Resolve();
                }
                // Radio 0.49.1: relays on the move are not drawn, by name.
                if (WorldCover.MovingRelays.Count > 0)
                    shown += "\n" + "PhobosRadio_CoverNetworkMoving".Translate(WorldCover.MovingRelays.ToCommaList(useAnd: true).Named("RELAYS")).Resolve();
                return shown;
            }
            if (WorldCover.Site is not { } site || site.Place != place) return null;
            switch (WorldCover.Refusal(site, kind))
            {
                case WorldCoverRefusal.NoMorseKey: return "PhobosRadio_CoverRefuseNoKey".Translate(kind.LabelCap.Named("KIND")).Resolve();
                case WorldCoverRefusal.RelayPassesSpeechOnly: return "PhobosRadio_CoverRefuseRelayMorse".Translate(kind.LabelCap.Named("KIND")).Resolve();
            }
            return WorldCover.Working
                ? "PhobosRadio_CoverWorking".Translate(name, WorldCover.Progress.ToStringPercent().Named("PERCENT")).Resolve()
                : "PhobosRadio_CoverShown".Translate(name, WorldCover.Reached.Named("TILES")).Resolve();
        }
        catch (System.Exception e) { UiGuard.Report(GuardKey, e); return null; }
    }
}

/// <summary>Puts the cover picture's button and line on a world object (Radio 0.42.0). It saves nothing and does nothing each tick.</summary>
public class WorldObjectComp_PhobosRadioCover : WorldObjectComp
{
    public override IEnumerable<Gizmo> GetGizmos() => WorldCoverGizmo.For(parent);

    public override string? CompInspectStringExtra() => WorldCoverGizmo.InspectLine(parent);
}

public class WorldObjectCompProperties_PhobosRadioCover : WorldObjectCompProperties
{
    public WorldObjectCompProperties_PhobosRadioCover() => compClass = typeof(WorldObjectComp_PhobosRadioCover);
}

/// <summary>
/// Gives every kind of place that can hold a map the cover picture's button (Radio 0.42.0), so a colony of the player's shows it whatever
/// mod its kind of place comes from; the relay mast names the comp in its own Def. Done here rather than by patch because the kinds are
/// other mods' as well as the game's. A place with no radio of the player's shows nothing.
/// <para>Cost: once at start-up, one pass over the world object Defs. In play, one comp a place, which does nothing each tick.</para>
/// </summary>
[StaticConstructorOnStartup]
public static class WorldCoverStartup
{
    static WorldCoverStartup()
    {
        foreach (WorldObjectDef def in DefDatabase<WorldObjectDef>.AllDefsListForReading)
        {
            if (def.worldObjectClass == null || !typeof(MapParent).IsAssignableFrom(def.worldObjectClass)) continue;
            if (def.comps.Exists(comp => comp is WorldObjectCompProperties_PhobosRadioCover)) continue;
            def.comps.Add(new WorldObjectCompProperties_PhobosRadioCover());
        }
    }
}

/// <summary>
/// Works the cover picture out a little each frame the world map is shown, and draws the reach ring of a selected colony, relay mast or
/// relay post (Radio 0.42.0; the ring moved here from the vehicles tie-in, which still draws a convoy's). Saves nothing.
/// <para>Cost: one check a frame while the world map is hidden. While it is shown with such a place selected, the place's radios are read
/// every <see cref="WorldCover.LookFrames"/> frames and a ring is drawn each frame.</para>
/// </summary>
public sealed class WorldComponent_PhobosWorldCover : WorldComponent
{
    public WorldComponent_PhobosWorldCover(World world) : base(world) { }

    public override void WorldComponentUpdate()
    {
        if (!WorldRendererUtility.WorldSelected)
        {
            WorldCover.Hidden();
            return;
        }
        WorldCover.Update(Find.WorldSelector.FirstSelectedObject);
        // A convoy's ring is drawn with its chain home, by the vehicles tie-in.
        if (WorldCover.Site is { Caller: false } site && WorldCover.RingTiles > 0) GenDraw.DrawWorldRadiusRing(site.Tile, WorldCover.RingTiles);
        // Radio 0.54.0: a colony's contact arcs, to each faction settlement its stations reach.
        if (WorldCover.Site is { Place: MapParent { HasMap: true } home } && home.Map.IsPlayerHome)
        {
            WorldArcs.DrawContacts(home.Map);
            // Radio 0.62.0: HF's band rose, in dev mode only until skywave is built.
            HfRose.Draw(home.Map);
            // Radio 0.71.0: the grey line and the skip-zone rings, each off until picked.
            HfPictures.Draw(home.Map);
        }
    }
}
