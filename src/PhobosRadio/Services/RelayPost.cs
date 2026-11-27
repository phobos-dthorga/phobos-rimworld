using System.Collections.Generic;
using Outposts;
using Phobos.Radio.Physics;
using RimWorld.Planet;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>A relay post's figures, from its Def. All authored, for the owner to revise.</summary>
public class PhobosRelayPostExtension : OutpostExtension
{
    /// <summary>The post's relay radio: a ThingDef with a radio comp, never on a map.</summary>
    public ThingDef? radio;
    /// <summary>How many 25 kHz channels it relays on: two repeaters' worth.</summary>
    public int channels = 4;
}

/// <summary>
/// A radio relay post (Radio 0.24.0; the owner's card of 27 Nov 2026, "Our own relay post": a world outpost as one of the four relay sites):
/// an outpost of Vanilla Expanded Framework's, founded from its own "create outpost" window like any other and staffed by colonists, that
/// produces nothing and passes radio calls on from its tile. Its relay radio is made from its Def when first asked for and never saved,
/// since nothing about it changes (derived state). A post relays while anyone is there to keep it.
/// </summary>
public class Outpost_PhobosRelayPost : Outpost
{
    static readonly PhobosRelayPostExtension Defaults = new();
    Thing? radioUnit;

    public PhobosRelayPostExtension Figures => Ext as PhobosRelayPostExtension ?? Defaults;

    /// <summary>The post's relay radio, or null when its Def names none.</summary>
    public CompRadio? Radio
    {
        get
        {
            if (radioUnit == null && Figures.radio is { } def) radioUnit = ThingMaker.MakeThing(def);
            return radioUnit?.TryGetComp<CompRadio>();
        }
    }

    /// <summary>It relays only while someone keeps it.</summary>
    public bool Manned => PawnCount > 0;

    /// <summary>The post as a relay, while it is the player's and manned, or null: what the relay search and the world map's pictures both read.</summary>
    public RelayNode? Node() => !Destroyed && Manned && Faction is { IsPlayer: true } && Radio is { } radio
        ? new RelayNode(RadioEndpoint.BuiltIn(radio.parent, IntVec3.Zero, radio), Tile, LabelCap, this, Repeater: true,
            SiteMetres: RelayNode.SiteOf(Tile), Channels: Figures.channels)
        : null;

    public override void Produce() { }

    public override string ProductionString()
    {
        if (!Manned) return "PhobosRadio_RelayPostUnmanned".Translate().Resolve();
        string line = "PhobosRadio_RelayPostLine".Translate((Radio?.Props.antennaHeightMetres ?? 0f).ToString("0").Named("METRES"),
            Figures.channels.Named("CHANNELS")).Resolve();
        return RelayNodes.UseLine(this, Figures.channels) is { } use ? line + "\n" + use : line;
    }
}

/// <summary>Every manned relay post of the player's, as relays.</summary>
public sealed class RelayPostSource : IRelaySource
{
    public IEnumerable<RelayNode> Nodes()
    {
        if (Find.WorldObjects == null) yield break;
        foreach (WorldObject place in Find.WorldObjects.AllWorldObjects)
        {
            if (place is Outpost_PhobosRelayPost post && post.Node() is { } node) yield return node;
        }
    }
}
