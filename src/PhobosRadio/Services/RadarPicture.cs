using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Phobos.Radio.Physics;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Phobos.Radio.Services;

/// <summary>What a radar contact is (Radio 0.74.0).</summary>
public enum RadarContactKind
{
    Aircraft,
    Raid,
    Missile,
    Shell,
    Falling,
}

/// <summary>
/// Friend or foe (Radio 0.74.0, row RD9; the owner's card of 28 Nov 2026, "unknown, then identified"): unknown until a transponder answers
/// or the watch identifies it; then friend, neutral or hostile.
/// </summary>
public enum RadarIdentity
{
    Unknown,
    Friend,
    Neutral,
    Hostile,
}

/// <summary>
/// Something in the sky between world tiles, as a source reports it: where, how high, how big to a radar, whose. <c>Inbound</c> marks a
/// missile or shell heading for one of the player's places: the first radar to see it sends a letter.
/// </summary>
public readonly record struct RadarTarget(object Key, PlanetTile Tile, double HeightMetres, double SquareMetres, Faction? Faction, string Label,
    RadarContactKind Kind, bool Transponder, bool Inbound = false);

/// <summary>Another mod's radar on a colony's map, listed on its console beside ours (Radio 0.74.0): what it is, whether it works, a note.</summary>
public readonly record struct OtherRadar(Thing Thing, bool Working, string Note);

/// <summary>Another mod's radars on a map (Radio 0.74.0): Dubs Rimatomics' dish, Fortified Features Framework's radars.</summary>
public interface IOtherRadarSource
{
    void Collect(Map map, List<OtherRadar> into);
}

/// <summary>Something in the sky over a map, as a source reports it.</summary>
public readonly record struct MapRadarTarget(Thing Thing, double HeightMetres, double SquareMetres, Faction? Faction, RadarContactKind Kind,
    bool Transponder);

/// <summary>Things in flight between world tiles (Radio 0.74.0): aircraft from the vehicles tie-in; missiles and shells from their mods'.</summary>
public interface IRadarTargetSource
{
    void Collect(List<RadarTarget> into);
}

/// <summary>Things in the air over one map (Radio 0.74.0): aircraft from the vehicles tie-in.</summary>
public interface IMapRadarTargetSource
{
    void Collect(Map map, List<MapRadarTarget> into);
}

/// <summary>One contact on a colony's scope: what, whose, where from the colony and how high.</summary>
public sealed class RadarContact
{
    public object Key = null!;
    public RadarContactKind Kind;
    public RadarIdentity Identity;
    public string Label = "";
    public Faction? Faction;
    /// <summary>Bearing from the colony, degrees clockwise from north.</summary>
    public float BearingDegrees;
    public double RangeMetres;
    public double HeightMetres;
    /// <summary>Seen only by a radar of another colony, through the data link.</summary>
    public bool Remote;
    /// <summary>A held raid: the blurred count, "7 to 13".</summary>
    public string? Count;
}

/// <summary>
/// The seams other parts feed the radar picture through (Radio 0.74.0), registered once at start-up as the radio's other seams are. A source
/// that throws is dropped with one warning, so another mod's change never stops the picture.
/// </summary>
public static class RadarTargets
{
    private static readonly List<IRadarTargetSource> sources = new();
    private static readonly List<IMapRadarTargetSource> mapSources = new();
    private static readonly List<IOtherRadarSource> otherSources = new();
    private static readonly List<RadarTarget> collected = new();
    private static int collectedTick = -1;

    public static void Register(IRadarTargetSource source)
    {
        if (!sources.Contains(source)) sources.Add(source);
    }

    public static void Register(IMapRadarTargetSource source)
    {
        if (!mapSources.Contains(source)) mapSources.Add(source);
    }

    public static void Register(IOtherRadarSource source)
    {
        if (!otherSources.Contains(source)) otherSources.Add(source);
    }

    /// <summary>Other mods' radars on a map, for its console.</summary>
    public static void Others(Map map, List<OtherRadar> into)
    {
        for (int i = otherSources.Count - 1; i >= 0; i--)
        {
            try
            {
                otherSources[i].Collect(map, into);
            }
            catch (Exception error)
            {
                Log.Warning($"[PhobosRadio] Another mod's radar list ({otherSources[i].GetType().Name}) failed and is switched off: {error}");
                otherSources.RemoveAt(i);
            }
        }
    }

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<object, StrongBox<PlanetTile>> under = new();
    private static readonly List<PlanetTile> steps = new();

    /// <summary>
    /// The tile under something moving over the world, for a source with no tile of its own (a missile, a shell): walked from where it was
    /// last time (or from <paramref name="start"/>) towards its place, a neighbour at a time (D'thorga Framework's TileAir.StepToward), so
    /// each look takes a few steps.
    /// </summary>
    public static PlanetTile TileUnder(object key, Vector3 position, PlanetTile start)
    {
        WorldGrid grid = Find.WorldGrid;
        StrongBox<PlanetTile> box = under.GetValue(key, _ => new StrongBox<PlanetTile>(start));
        PlanetTile here = box.Value.Valid ? box.Value : start;
        if (!here.Valid) return here;
        for (int i = 0; i < 4000; i++)
        {
            PlanetTile next = Phobos.Framework.WorldMap.TileAir.StepToward(grid, here, position, steps);
            if (next == here) break;
            here = next;
        }
        box.Value = here;
        return here;
    }

    /// <summary>Whether anything in flight is heading for one of the player's places (the warnings' background look asks it).</summary>
    public static bool AnyInbound()
    {
        foreach (RadarTarget target in InFlight())
            if (target.Inbound) return true;
        return false;
    }

    /// <summary>Everything in flight between tiles, collected at most once a tick for every map that asks.</summary>
    public static List<RadarTarget> InFlight()
    {
        int now = Find.TickManager.TicksGame;
        if (collectedTick == now) return collected;
        collectedTick = now;
        collected.Clear();
        for (int i = sources.Count - 1; i >= 0; i--)
        {
            try
            {
                sources[i].Collect(collected);
            }
            catch (Exception error)
            {
                Log.Warning($"[PhobosRadio] A radar target source ({sources[i].GetType().Name}) failed and is switched off: {error}");
                sources.RemoveAt(i);
            }
        }
        return collected;
    }

    public static void OverMap(Map map, List<MapRadarTarget> into)
    {
        for (int i = mapSources.Count - 1; i >= 0; i--)
        {
            try
            {
                mapSources[i].Collect(map, into);
            }
            catch (Exception error)
            {
                Log.Warning($"[PhobosRadio] A radar target source over the map ({mapSources[i].GetType().Name}) failed and is switched off: {error}");
                mapSources.RemoveAt(i);
            }
        }
    }
}

/// <summary>
/// A colony's radar picture (Radio 0.74.0; docs/development/radar-and-air-defence.md, rows RD3, RD6, RD8 and RD9): its radars and consoles,
/// the radars of other colonies its data link reaches, who keeps watch, and the contacts they see.
/// <para>Cost: nothing while nobody looks. The contacts are worked out only when the scope or the world map asks, at most once every
/// <see cref="RadioRulesDef.radarLookTicks"/> (30); each is one radar sum and, between tiles, one terrain walk a radar. The data link's
/// reach is asked at most once a game hour.</para>
/// </summary>
public class MapComponent_PhobosRadar : MapComponent
{
    private readonly List<CompRadar> radars = new();
    private readonly List<CompRadarConsole> consoles = new();
    private readonly List<RadarContact> contacts = new();
    private readonly List<RadarContact> overMap = new();
    private readonly Dictionary<object, int> firstSeen = new();
    private readonly List<MapRadarTarget> mapTargets = new();
    private readonly List<CompRadar> watching = new();
    private int lookedTick = -999999;
    private int lookedOverMapTick = -999999;
    private int linkTick = -999999;
    private readonly List<CompRadar> linked = new();
    private int watchTick = -999999;
    private int watchSkill;

    private static List<ThingDef>? fallingDefs;

    public MapComponent_PhobosRadar(Map map) : base(map) { }

    public static MapComponent_PhobosRadar For(Map map)
    {
        var picture = map.GetComponent<MapComponent_PhobosRadar>();
        if (picture == null)
        {
            picture = new MapComponent_PhobosRadar(map);
            map.components.Add(picture);
        }
        return picture;
    }

    public void Add(CompRadar radar)
    {
        if (!radars.Contains(radar)) radars.Add(radar);
        MarkDirty();
    }

    public void Remove(CompRadar radar)
    {
        radars.Remove(radar);
        MarkDirty();
    }

    public void Add(CompRadarConsole console)
    {
        if (!consoles.Contains(console)) consoles.Add(console);
    }

    public void Remove(CompRadarConsole console) => consoles.Remove(console);

    /// <summary>Something changed: a radar built, lost, roofed or upgraded. The next look works it all out again.</summary>
    public void MarkDirty()
    {
        lookedTick = -999999;
        lookedOverMapTick = -999999;
        linkTick = -999999;
        foreach (Map other in Find.Maps)
        {
            if (other != map && other.GetComponent<MapComponent_PhobosRadar>() is { } picture) picture.linkTick = -999999;
        }
    }

    public IReadOnlyList<CompRadar> Radars => radars;

    public int WorkingRadarCount => radars.Count(r => r.Working);

    public int LinkedRadarCount => Linked().Count;

    public bool HasWorkingConsole => consoles.Any(c => c.Working);

    /// <summary>The radars that watch this colony's sky: its own that work, and those of other colonies its data link reaches.</summary>
    public List<CompRadar> Watching()
    {
        watching.Clear();
        foreach (CompRadar radar in radars)
            if (radar.Working) watching.Add(radar);
        watching.AddRange(Linked());
        return watching;
    }

    /// <summary>
    /// The radars of the colony's other maps that share their picture with it (RD6's data link; the owner's card of 28 Nov 2026: "involving
    /// the rest of the colony's radio network (plus relays/repeaters too?)"): working, with the data link fitted, and on a map whose
    /// stations reach this map's stations by radio, relays and repeaters included (<see cref="RelayNodes.BestChain"/>, the search the
    /// faction calls use). Asked at most once a game hour.
    /// </summary>
    public List<CompRadar> Linked()
    {
        int now = Find.TickManager.TicksGame;
        if (now - linkTick < RadioRulesDef.Current.radarDataLinkKeepTicks && now >= linkTick) return linked;
        linkTick = now;
        linked.Clear();
        List<RelayNode>? here = null;
        List<RelayNode>? relays = null;
        foreach (Map other in Find.Maps)
        {
            if (other == map || !other.IsPlayerHome || other.GetComponent<MapComponent_PhobosRadar>() is not { } picture) continue;
            List<CompRadar> sharing = picture.radars.Where(r => r.Working && r.HasDataLink).ToList();
            if (sharing.Count == 0) continue;
            here ??= FactionDoctors.StationEnds(map);
            if (here.Count == 0) return linked;
            relays ??= RelayNodes.All().ToList();
            bool reached = false;
            foreach (RelayNode from in FactionDoctors.StationEnds(other))
            {
                if (RelayNodes.BestChain(from, here, relays, leaseHolder: (this, "PhobosRadarDataLink")) != null)
                {
                    reached = true;
                    break;
                }
            }
            if (!reached) continue;
            linked.AddRange(sharing);
            if (Seen.Wants("radio.radar-datalink")) Seen.Note("radio.radar-datalink", sharing[0].parent, "to " + map.Parent.LabelCap);
        }
        return linked;
    }

    /// <summary>A colonist is on watch at a console now, with this Intellectual skill.</summary>
    public void NoteWatch(Pawn pawn)
    {
        int skill = pawn.skills?.GetSkill(SkillDefOf.Intellectual)?.Level ?? 0;
        if (Find.TickManager.TicksGame - watchTick > RadioRulesDef.Current.radarWatchKeepTicks) watchSkill = skill;
        else watchSkill = Math.Max(watchSkill, skill);
        watchTick = Find.TickManager.TicksGame;
    }

    /// <summary>
    /// The skill this colony's picture is identified with: the watch's if someone kept watch lately, the plot extractor's if a watching
    /// radar has one, the better of the two; null with nobody and nothing.
    /// </summary>
    public int? WatchSkill()
    {
        var rules = RadioRulesDef.Current;
        int? skill = Find.TickManager.TicksGame - watchTick <= rules.radarWatchKeepTicks ? watchSkill : null;
        if (Watching().Any(r => r.HasPlotExtractor)) skill = Math.Max(skill ?? 0, rules.radarPlotExtractorSkill);
        return skill;
    }

    /// <summary>How far through a warning, or a contact's first look, it is identified: the rules' share, sooner with a better watch.</summary>
    public static float IdentifyShare(int? skill)
    {
        var rules = RadioRulesDef.Current;
        if (skill == null) return rules.radarIdentifyShareUnmanned;
        return Mathf.Max(rules.radarIdentifyShareBest, rules.radarIdentifyShareUnmanned - rules.radarIdentifySharePerSkill * skill.Value);
    }

    /// <summary>Whether a colonist is wanted on watch: a console works, a radar watches, and something is still unknown.</summary>
    public bool WantsWatch()
    {
        if (!HasWorkingConsole || Watching().Count == 0) return false;
        if (GameComponent_PhobosRadarWarnings.Current?.AnyUnknownFor(map) == true) return true;
        // The last picture worked out, not a fresh one: colonists ask this whenever they look for work, and it must not make the radar
        // look on their account.
        return contacts.Any(c => c.Identity == RadarIdentity.Unknown);
    }

    public CompRadarConsole? FreeConsole(Pawn pawn) =>
        consoles.FirstOrDefault(c => c.Working && !c.parent.IsForbidden(pawn) && pawn.CanReserve(c.parent) && RadioSeats.CanSit(pawn, c.parent));

    public IEnumerable<Thing> ConsoleThings => consoles.Where(c => c.Working).Select(c => (Thing)c.parent);

    /// <summary>The contacts between world tiles the colony's radars see now, worked out at most once every look interval.</summary>
    public List<RadarContact> Contacts()
    {
        int now = Find.TickManager.TicksGame;
        var rules = RadioRulesDef.Current;
        if (now - lookedTick < rules.radarLookTicks && now >= lookedTick) return contacts;
        lookedTick = now;
        contacts.Clear();
        List<CompRadar> eyes = Watching();
        if (eyes.Count == 0) return contacts;
        int? skill = WatchSkill();
        PlanetTile home = map.Tile;
        foreach (RadarTarget target in RadarTargets.InFlight())
        {
            if (!target.Tile.Valid) continue;
            bool seen = false;
            bool remote = true;
            foreach (CompRadar radar in eyes)
            {
                if (!Sees(radar, target.Tile, target.HeightMetres, target.SquareMetres, rules)) continue;
                seen = true;
                if (radar.parent.Map == map) remote = false;
                if (!remote) break;
            }
            if (!seen) continue;
            if (!firstSeen.ContainsKey(target.Key))
            {
                firstSeen[target.Key] = now;
                if (target.Inbound) InboundLetter(target, home, rules);
            }
            contacts.Add(new RadarContact
            {
                Key = target.Key,
                Kind = target.Kind,
                Identity = Identify(target.Faction, target.Transponder, now - firstSeen[target.Key], skill, rules),
                Label = target.Label,
                Faction = target.Faction,
                BearingDegrees = home == target.Tile ? 0f : Find.WorldGrid.GetHeadingFromTo(home, target.Tile),
                RangeMetres = Find.WorldGrid.ApproxDistanceInTiles(home, target.Tile) * rules.radarMetresPerWorldTile,
                HeightMetres = target.HeightMetres,
                Remote = remote,
            });
        }
        GameComponent_PhobosRadarWarnings.Current?.AddContacts(map, contacts);
        if (firstSeen.Count > 256) firstSeen.Clear();
        if (contacts.Count > 0 && Seen.Wants("radio.radar-contact")) Seen.Note("radio.radar-contact", null, contacts.Count + " in sight of " + map.Parent.LabelCap);
        return contacts;
    }

    /// <summary>
    /// The contacts over the map itself (the scope's second scale): aircraft over it, and anything falling. Seen while any radar of this map
    /// works; on a map, range barely matters (the record's first gotcha), so the sum is not asked.
    /// </summary>
    public List<RadarContact> OverMap()
    {
        int now = Find.TickManager.TicksGame;
        var rules = RadioRulesDef.Current;
        if (now - lookedOverMapTick < rules.radarLookTicks && now >= lookedOverMapTick) return overMap;
        lookedOverMapTick = now;
        overMap.Clear();
        if (!radars.Any(r => r.Working)) return overMap;
        int? skill = WatchSkill();
        mapTargets.Clear();
        RadarTargets.OverMap(map, mapTargets);
        foreach (ThingDef def in FallingDefs())
        {
            foreach (Thing thing in map.listerThings.ThingsOfDef(def))
                mapTargets.Add(new MapRadarTarget(thing, 0, rules.radarDropPodSquareMetres, null, RadarContactKind.Falling, false));
        }
        IntVec3 centre = map.Center;
        float metresPerCell = RadioMod.Settings.metresPerCell;
        foreach (MapRadarTarget target in mapTargets)
        {
            if (!target.Thing.Spawned) continue;
            if (!firstSeen.ContainsKey(target.Thing)) firstSeen[target.Thing] = now;
            Vector3 offset = (target.Thing.Position - centre).ToVector3();
            overMap.Add(new RadarContact
            {
                Key = target.Thing,
                Kind = target.Kind,
                Identity = target.Kind == RadarContactKind.Falling ? RadarIdentity.Unknown
                    : Identify(target.Faction, target.Transponder, now - firstSeen[target.Thing], skill, rules),
                Label = target.Thing.LabelShortCap,
                Faction = target.Faction,
                BearingDegrees = (Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg + 360f) % 360f,
                RangeMetres = offset.magnitude * metresPerCell,
                HeightMetres = target.HeightMetres,
            });
        }
        return overMap;
    }

    /// <summary>
    /// A missile or shell heading for one of the player's places, seen for the first time by this colony's radars: a letter, once a thing a
    /// colony (Radio 0.74.0, the owner's card of 28 Nov 2026 for the IRBM and Combat Extended tie-ins). Unknown until identified, as any.
    /// </summary>
    private void InboundLetter(RadarTarget target, PlanetTile home, RadioRulesDef rules)
    {
        double km = Find.WorldGrid.ApproxDistanceInTiles(home, target.Tile) * rules.radarMetresPerWorldTile / 1000.0;
        string what = (target.Kind == RadarContactKind.Shell ? "PhobosRadar_InboundShell" : "PhobosRadar_InboundMissile").Translate().Resolve();
        string text = "PhobosRadar_InboundText".Translate(map.Parent.LabelCap.Named("COLONY"), what.Named("WHAT"),
            RadarWarning.CompassOf(Find.WorldGrid.GetHeadingFromTo(home, target.Tile)).Named("BEARING"), km.ToString("0").Named("KM")).Resolve();
        Find.LetterStack.ReceiveLetter("PhobosRadar_InboundLabel".Translate(what.Named("WHAT")), text, LetterDefOf.ThreatBig,
            target.Key is WorldObject place ? new LookTargets(new GlobalTargetInfo(place)) : new LookTargets(map.Parent));
        if (Seen.Wants("radio.radar-inbound", null, target.Kind.ToString()))
            Seen.Note("radio.radar-inbound", null, what + ", " + km.ToString("0") + " km", target.Kind.ToString());
    }

    /// <summary>Whether a radar sees a target on a tile at this height and size: the sum, the horizon, and the high ground between (RD3).</summary>
    public static bool Sees(CompRadar radar, PlanetTile tile, double heightMetres, double squareMetres, RadioRulesDef rules)
    {
        PlanetTile from = radar.parent.Map.Tile;
        double metres = Find.WorldGrid.ApproxDistanceInTiles(from, tile) * rules.radarMetresPerWorldTile;
        if (metres > radar.SeesMetres(squareMetres, heightMetres)) return false;
        return !Shadowed(from, tile, radar.HeightMetres, heightMetres, metres, rules);
    }

    /// <summary>Whether high ground stands over the line between a radar and a target: a radar needs to see it, so any edge over the line hides it.</summary>
    public static bool Shadowed(PlanetTile from, PlanetTile to, double radarMetres, double targetMetres, double metres, RadioRulesDef rules)
    {
        if (from == to) return false;
        WorldTerrain.Blocking blocking = WorldTerrain.Between(from, to, radarMetres, targetMetres, metres, rules);
        return blocking.Any && blocking.OverLineMetres > 0.0;
    }

    /// <summary>Friend or foe: a transponder of ours or an ally's answers at once; anything else is unknown until the watch has had its time.</summary>
    public static RadarIdentity Identify(Faction? faction, bool transponder, int ticksSeen, int? skill, RadioRulesDef rules)
    {
        if (transponder && faction != null && (faction.IsPlayer || faction.PlayerRelationKind == FactionRelationKind.Ally)) return RadarIdentity.Friend;
        if (ticksSeen < IdentifyShare(skill) * rules.radarIdentifyBaseTicks) return RadarIdentity.Unknown;
        return Standing(faction);
    }

    /// <summary>What an identified contact is, by its faction.</summary>
    public static RadarIdentity Standing(Faction? faction)
    {
        if (faction == null) return RadarIdentity.Neutral;
        if (faction.IsPlayer || faction.PlayerRelationKind == FactionRelationKind.Ally) return RadarIdentity.Friend;
        return faction.HostileTo(Faction.OfPlayer) ? RadarIdentity.Hostile : RadarIdentity.Neutral;
    }

    private static List<ThingDef> FallingDefs() =>
        fallingDefs ??= DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.thingClass != null && typeof(Skyfaller).IsAssignableFrom(d.thingClass)
            && d.skyfaller != null && !d.skyfaller.reversed).ToList();
}
