using System.Collections.Generic;
using System.Linq;
using Phobos.Radio.Physics;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Phobos.Radio.Services;

// The station logbook and QSL cards (Radio 0.38.0; docs/development/radio-logbook-qsl-sstv-morse.md, the owner's cards of 27 Nov 2026).
// The log keeps what the radio did (advice, a faction's help, friends, lessons, neighbours' calls, medevacs); a QSL card confirms the first
// contact with each settlement, drawn in code from the faction's own colour and icon (the owner's "made procedurally"); a set of cards earns
// an award, a lasting mood for those who work the radio (the owner's card). All saved with the game.

/// <summary>What a log entry is about.</summary>
public enum RadioLogKind
{
    Advice,
    FactionHelp,
    Friend,
    Lesson,
    Neighbour,
    Medevac,
    Qsl,
    Award,
    /// <summary>A call to another faction, or an ally's warning (Radio 0.53.0).</summary>
    Diplomacy,
    /// <summary>Colonists talking by themselves: an evening call, company through the night (Radio 0.63.0).</summary>
    Talk,
    /// <summary>Radio watch: a call heard or missed, and what the standing orders did (Radio 0.73.0).</summary>
    Watch,
}

/// <summary>One line of the logbook: when, what kind, and the words as they were said then.</summary>
public sealed class RadioLogEntry : IExposable
{
    public int Tick;
    public RadioLogKind Kind;
    public string Text = "";

    public void ExposeData()
    {
        Scribe_Values.Look(ref Tick, "PhobosRadioLogTick");
        Scribe_Values.Look(ref Kind, "PhobosRadioLogKind");
        Scribe_Values.Look(ref Text, "PhobosRadioLogText", "");
    }
}

/// <summary>
/// The logbook: the last entries, newest last. <para>Cost: an entry is a list append when a call happens; nothing else runs.</para>
/// </summary>
public static class RadioLog
{
    /// <summary>How many entries are kept (agent default).</summary>
    public const int Keep = 200;

    private static List<RadioLogEntry> entries = new();

    public static IReadOnlyList<RadioLogEntry> Entries => entries;

    public static void Clear() => entries = new List<RadioLogEntry>();

    public static void ExposeData()
    {
        Scribe_Collections.Look(ref entries, "PhobosRadioLog", LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) entries ??= new List<RadioLogEntry>();
    }

    /// <summary>Writes a line in the logbook.</summary>
    public static void Add(RadioLogKind kind, string text)
    {
        if (Current.Game == null || text.NullOrEmpty()) return;
        entries.Add(new RadioLogEntry { Tick = Find.TickManager.TicksGame, Kind = kind, Text = text });
        if (entries.Count > Keep) entries.RemoveRange(0, entries.Count - Keep);
    }
}

/// <summary>A QSL card: the first contact with a settlement, as the operators' cards confirm one.</summary>
public sealed class QslCard : IExposable
{
    public Settlement? Settlement;
    public Faction? Faction;
    public string Place = "";
    public string Operator = "";
    public int Tick;
    public string Report = "";
    public int Tiles;
    /// <summary>What earned it (Radio 0.58.0): the card's note answers it. Cards from before say only "first contact".</summary>
    public QslContact Kind;
    /// <summary>The sender's callsign, its grid locator and our station's callsign (Radio 0.58.0), worked out when the card is earned and
    /// kept, so a card stays whole when its settlement is gone; a card from before 0.58.0 fills them in the first time it is drawn.</summary>
    public string Call = "";
    public string Grid = "";
    public string OurCall = "";
    /// <summary>The band and mode the contact was worked on, and the relays that carried it (Radio 0.62.0); null and 0 on a card from
    /// before, or where nobody worked them out.</summary>
    public BandDef? Band;
    public ModeDef? Mode;
    public int Relays;
    /// <summary>The named place the sender's prefix comes from (Radio 0.65.0), "Kasiko Mountains"; empty on a card from before, which takes
    /// it, and a callsign from it, the first time it is drawn while its settlement stands.</summary>
    public string Area = "";
    /// <summary>Whether its card has come by the bureau (Radio 0.66.0): the logbook shows a card at once; the card itself comes later with a
    /// friendly trader or visitor (<see cref="RadioQsl.Bureau"/>). Cards from before come by the bureau too.</summary>
    public bool Delivered;
    /// <summary>The sender's biome and tile (Radio 0.69.0), kept for the awards when the settlement is gone; filled in on older cards while
    /// their settlement stands.</summary>
    public string Biome = "";
    public int TileId = -1;

    public void ExposeData()
    {
        Scribe_Values.Look(ref Biome, "PhobosQslBiome", "");
        Scribe_Values.Look(ref TileId, "PhobosQslTile", -1);
        Scribe_Values.Look(ref Delivered, "PhobosQslDelivered");
        Scribe_Values.Look(ref Area, "PhobosQslArea", "");
        Scribe_Defs.Look(ref Band, "PhobosQslBand");
        Scribe_Defs.Look(ref Mode, "PhobosQslMode");
        Scribe_Values.Look(ref Relays, "PhobosQslRelays");
        Scribe_References.Look(ref Settlement, "PhobosQslSettlement");
        Scribe_References.Look(ref Faction, "PhobosQslFaction");
        Scribe_Values.Look(ref Place, "PhobosQslPlace", "");
        Scribe_Values.Look(ref Operator, "PhobosQslOperator", "");
        Scribe_Values.Look(ref Tick, "PhobosQslTick");
        Scribe_Values.Look(ref Report, "PhobosQslReport", "");
        Scribe_Values.Look(ref Tiles, "PhobosQslTiles");
        Scribe_Values.Look(ref Kind, "PhobosQslKind", QslContact.FirstContact);
        Scribe_Values.Look(ref Call, "PhobosQslCall", "");
        Scribe_Values.Look(ref Grid, "PhobosQslGrid", "");
        Scribe_Values.Look(ref OurCall, "PhobosQslOurCall", "");
    }

    /// <summary>Fills in what a card from before lacks, where its settlement still stands: callsigns and locator (0.58.0), and since 0.65.0
    /// the area and callsigns from the world's own places, replacing the faction-based ones.</summary>
    public void FillIn()
    {
        // Radio 0.69.0: the biome and tile, for the awards.
        if (TileId < 0 && Settlement != null)
        {
            TileId = Settlement.Tile.tileId;
            Biome = Find.WorldGrid[Settlement.Tile]?.PrimaryBiome?.defName ?? "";
        }
        if (Area.Length > 0 || Settlement == null) return;
        Call = QslCalls.Of(Settlement);
        Grid = QslCalls.Locator(Settlement.Tile);
        OurCall = QslCalls.Home(Find.AnyPlayerHomeMap?.Tile);
        Area = QslCalls.AreaOf(Settlement.Tile);
    }
}

/// <summary>What a contact was (Radio 0.58.0), for the note the card carries.</summary>
public enum QslContact
{
    FirstContact,
    /// <summary>Their doctor or mechanic advised ours.</summary>
    Advice,
    /// <summary>We called them: a trader, help, a check-in.</summary>
    FactionCall,
    /// <summary>A radio friend's chat.</summary>
    Friend,
    /// <summary>We helped them, at their asking.</summary>
    Neighbour,
    /// <summary>A rare station worked while it was on the air (Radio 0.70.0).</summary>
    Rare,
    /// <summary>Heard, not worked: their signal reached us, ours did not reach them (Radio 0.70.0). Counts toward no award.</summary>
    Listener,
}

/// <summary>
/// Callsigns and grid locators (Radio 0.58.0; docs/development/radio-qsl-cards.md). Since 0.65.0 (the owner's cards of 28 Nov 2026) a
/// station's prefix comes from the world's own named place it lies in or nearest to (a mountain range, a sea, a lake, a bay: the names
/// the game gives each rolled world), three letters, unique on the world; the call-area digit from its latitude. The rules are game-free
/// in <see cref="QslRule"/>.
/// <para>Cost: the places' prefixes once a session; a tile's nearest place found once (a short search over its neighbours, at most
/// <see cref="SearchTiles"/>) and kept; nothing per tick.</para>
/// </summary>
public static class QslCalls
{
    private static readonly Dictionary<WorldFeature, string> placePrefixes = new();
    private static readonly Dictionary<int, WorldFeature?> nearest = new();
    /// <summary>How many tiles the search for a tile's nearest named place looks at, at most (agent's figure).</summary>
    public const int SearchTiles = 4000;

    public static void Clear()
    {
        placePrefixes.Clear();
        nearest.Clear();
    }

    /// <summary>The named place a tile lies in, or the nearest by tiles; null on a world with none near.</summary>
    public static WorldFeature? PlaceOf(PlanetTile tile)
    {
        if (!tile.Valid) return null;
        if (nearest.TryGetValue(tile.tileId, out WorldFeature? known)) return known;
        WorldFeature? found = Find.WorldGrid[tile]?.feature;
        if (found == null)
        {
            var seen = new HashSet<PlanetTile> { tile };
            var frontier = new Queue<PlanetTile>();
            frontier.Enqueue(tile);
            var around = new List<PlanetTile>();
            while (frontier.Count > 0 && seen.Count < SearchTiles && found == null)
            {
                PlanetTile at = frontier.Dequeue();
                Find.WorldGrid.GetTileNeighbors(at, around);
                foreach (PlanetTile next in around)
                {
                    if (!seen.Add(next)) continue;
                    if (Find.WorldGrid[next]?.feature is { } feature)
                    {
                        found = feature;
                        break;
                    }
                    frontier.Enqueue(next);
                }
            }
        }
        if (nearest.Count > 2048) nearest.Clear();
        nearest[tile.tileId] = found;
        return found;
    }

    /// <summary>The name of a tile's place, or empty.</summary>
    public static string AreaOf(PlanetTile tile) => PlaceOf(tile)?.name ?? "";

    /// <summary>A tile's three-letter prefix: its place's, every place on the world stepping past one taken before it in the order the
    /// world made them, so a world's prefixes come out the same each time it loads; from the tile itself where no place is near.</summary>
    public static string PlacePrefixOf(PlanetTile tile)
    {
        if (PlaceOf(tile) is not { } place) return QslRule.PlacePrefix("", tile.tileId / 97);
        if (placePrefixes.TryGetValue(place, out string? known)) return known;
        placePrefixes.Clear();
        var taken = new HashSet<string>();
        foreach (WorldFeature each in Find.World.features.features.OrderBy(f => f.uniqueID))
        {
            int step = 0;
            string prefix;
            do prefix = QslRule.PlacePrefix(each.name ?? "", each.uniqueID, step++);
            while (!taken.Add(prefix) && step < 26);
            placePrefixes[each] = prefix;
        }
        return placePrefixes.TryGetValue(place, out known) ? known : QslRule.PlacePrefix(place.name ?? "", place.uniqueID);
    }

    public static string Of(Settlement settlement) =>
        settlement.Faction != null ? QslRule.Callsign(PlacePrefixOf(settlement.Tile), Find.WorldGrid.LongLatOf(settlement.Tile).y, settlement.ID) : "";

    /// <summary>Our station's callsign: the player's own from the settings (Radio 0.62.0), else worked out from the colony's tile; empty
    /// without either.</summary>
    public static string Home(PlanetTile? tile) =>
        QslRule.CleanCallsign(RadioMod.Settings.ownCallsign) is { Length: > 0 } own ? own
        : tile is { Valid: true } t ? QslRule.Callsign(PlacePrefixOf(t), Find.WorldGrid.LongLatOf(t).y, t.tileId) : "";

    public static string Locator(PlanetTile tile)
    {
        Vector2 longLat = Find.WorldGrid.LongLatOf(tile);
        return QslRule.Locator(longLat.y, longLat.x);
    }
}

/// <summary>
/// The world's landmasses (Radio 0.69.0, for the award "every landmass worked"): the land tiles joined by their neighbours, numbered once a
/// session by one fill over the world's surface; water is -1.
/// <para>Cost: one pass over the world's tiles, the first time an award is counted in a session.</para>
/// </summary>
public static class Landmasses
{
    static int[]? ids;
    static World? of;

    public static int Of(int tileId)
    {
        if (Find.World is not { } world) return -1;
        if (ids == null || of != world) Fill(world);
        return tileId >= 0 && tileId < ids!.Length ? ids[tileId] : -1;
    }

    static void Fill(World world)
    {
        of = world;
        PlanetLayer surface = Find.WorldGrid.Surface;
        int count = surface.TilesCount;
        ids = new int[count];
        for (int i = 0; i < count; i++) ids[i] = -2;
        var around = new List<PlanetTile>();
        var queue = new Queue<int>();
        int next = 0;
        for (int start = 0; start < count; start++)
        {
            if (ids[start] != -2) continue;
            if (surface[start].WaterCovered) { ids[start] = -1; continue; }
            ids[start] = next;
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                int at = queue.Dequeue();
                surface.GetTileNeighbors(new PlanetTile(at, surface), around);
                foreach (PlanetTile n in around)
                {
                    if (n.tileId < 0 || n.tileId >= count || ids[n.tileId] != -2) continue;
                    if (surface[n.tileId].WaterCovered) { ids[n.tileId] = -1; continue; }
                    ids[n.tileId] = next;
                    queue.Enqueue(n.tileId);
                }
            }
            next++;
        }
    }
}

/// <summary>The awards a set of cards earns (agent defaults; the owner's card gave what an award brings).</summary>
public enum QslAward
{
    ThreeFactions,
    AllFactions,
    LongWay,
    TenSettlements,
    /// <summary>Radio 0.69.0 (docs/development/radio-qsl-in-play.md, part 3): every biome a settlement with radio stands in.</summary>
    AllBiomes,
    TenGridSquares,
    TwentyFiveGridSquares,
    /// <summary>A contact of <see cref="RadioQsl.FarSideKm"/> or more, at HF's scale.</summary>
    FarSide,
    /// <summary>Every landmass a settlement with radio stands on.</summary>
    AllLandmasses,
    /// <summary>A rare station worked (Radio 0.70.0).</summary>
    RareStation,
}

/// <summary>
/// QSL cards and awards.
/// <para>Cost: when a call with a settlement happens, one look in the cards; when a card is earned, the awards are counted over the cards.</para>
/// </summary>
public static class RadioQsl
{
    /// <summary>A contact this far or further earns "a long way" (agent default), in tiles.</summary>
    public const int LongWayTiles = 50;
    public const int ManySettlements = 10;
    public const int ManyFactions = 3;
    /// <summary>Radio 0.69.0's awards (agent's figures): grid squares (the locator's first four characters), and the far side in km.</summary>
    public const int SomeGrids = 10;
    public const int ManyGrids = 25;
    public const int FarSideKm = 10000;

    private static List<QslCard> cards = new();
    private static List<QslAward> awards = new();
    /// <summary>The visiting groups the bureau has already posted by this session; never saved, since a card once posted stays posted.</summary>
    private static readonly HashSet<int> postedBy = new();
    /// <summary>The awards whose framed certificate the bureau has posted (Radio 0.68.0); awards from before are posted with the next visitors.</summary>
    private static List<QslAward> certificatesPosted = new();

    public static IReadOnlyList<QslCard> Cards => cards;
    public static IReadOnlyList<QslAward> Awards => awards;

    public static void Clear()
    {
        postedBy.Clear();
        RareStations.Clear();
        QslCalls.Clear();
        cards = new List<QslCard>();
        awards = new List<QslAward>();
        certificatesPosted = new List<QslAward>();
    }

    public static void ExposeData()
    {
        RareStations.ExposeData();
        Scribe_Collections.Look(ref cards, "PhobosRadioQslCards", LookMode.Deep);
        Scribe_Collections.Look(ref awards, "PhobosRadioQslAwards", LookMode.Value);
        Scribe_Collections.Look(ref certificatesPosted, "PhobosRadioQslCertificates", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            cards ??= new List<QslCard>();
            awards ??= new List<QslAward>();
            certificatesPosted ??= new List<QslAward>();
        }
    }

    /// <summary>
    /// The operators' signal report for a link, readability and strength (RS, 31 to 59; agent's mapping): readable 5 above a workable margin,
    /// 4 when marginal; strength one S-unit for each 3 dB of margin, at least 1. Approximate, and only for the card.
    /// </summary>
    public static string Report(double marginDb, bool keyed = false)
    {
        int readability = marginDb >= RadioRulesDef.Current.workableDb ? 5 : marginDb >= RadioRulesDef.Current.marginalDb ? 4 : 3;
        int strength = System.Math.Max(1, System.Math.Min(9, 1 + (int)System.Math.Floor(System.Math.Max(0.0, marginDb) / 3.0)));
        // Radio 0.62.0: Morse adds the tone, RST (QslRule.Report).
        return QslRule.Report(readability, strength, keyed);
    }

    /// <summary>A call with a settlement happened: the first earns its card, said in a message and in the log, and the awards are counted.
    /// The link (Radio 0.62.0) gives the card its band, mode and relays.</summary>
    public static void Worked(FactionDoctorLink link, Pawn colonist, PlanetTile from, QslContact kind = QslContact.FirstContact)
    {
        Settlement settlement = link.Settlement;
        double marginDb = link.MarginDb;
        // Radio 0.70.0: a listener's card for a settlement does not stop it earning its worked card.
        if (settlement.Faction is not { } faction || cards.Any(card => card.Settlement == settlement && card.Kind != QslContact.Listener)) return;
        int tiles = from.Valid ? (int)Find.WorldGrid.ApproxDistanceInTiles(from, settlement.Tile) : 0;
        var card = new QslCard
        {
            Settlement = settlement,
            Faction = faction,
            Place = settlement.LabelCap,
            Operator = colonist.LabelShort,
            Tick = Find.TickManager.TicksAbs,
            Report = Report(marginDb, link.Mode?.keyed == true),
            Tiles = tiles,
            Kind = kind,
            Band = link.Band,
            Mode = link.Mode,
            Relays = link.Relays,
            Call = QslCalls.Of(settlement),
            Area = QslCalls.AreaOf(settlement.Tile),
            TileId = settlement.Tile.tileId,
            Biome = Find.WorldGrid[settlement.Tile]?.PrimaryBiome?.defName ?? "",
            Grid = QslCalls.Locator(settlement.Tile),
            OurCall = QslCalls.Home(from.Valid ? from : Find.AnyPlayerHomeMap?.Tile),
        };
        cards.Add(card);
        string text = "PhobosRadioQsl_Earned".Translate(settlement.LabelCap.Named("PLACE"), faction.Name.Named("FACTION"), colonist.LabelShort.Named("OPERATOR"),
            card.Report.Named("REPORT")).Resolve();
        Messages.Message(text, MessageTypeDefOf.PositiveEvent, historical: false);
        RadioLog.Add(RadioLogKind.Qsl, text);
        if (Seen.Wants("radio.qsl-card", colonist)) Seen.Note("radio.qsl-card", colonist, settlement.LabelCap + ", " + card.Report);
        CountAwards();
    }

    /// <summary>
    /// The QSL bureau (Radio 0.66.0; docs/development/radio-qsl-in-play.md, part 1): when a friendly trader caravan or visiting group is on a
    /// home map, it brings every card still awaited, dropped beside one of them, as the old bureaux posted everyone's cards together.
    /// <para>Cost: once a game hour, a look over the home maps' groups while any card is awaited; nothing otherwise.</para>
    /// </summary>
    public static void Bureau()
    {
        if ((!cards.Any(card => !card.Delivered) && awards.All(certificatesPosted.Contains)) || Find.Maps == null) return;
        foreach (Map map in Find.Maps)
        {
            if (!map.IsPlayerHome) continue;
            foreach (Verse.AI.Group.Lord lord in map.lordManager.lords)
            {
                if (lord.faction is not { } faction || faction.IsPlayer || faction.HostileTo(Faction.OfPlayer) || postedBy.Contains(lord.loadID)) continue;
                if (lord.ownedPawns.FirstOrDefault(p => p.Spawned && p.RaceProps.Humanlike) is not { } carrier) continue;
                postedBy.Add(lord.loadID);
                Post(carrier, faction);
                return;
            }
        }
    }

    private static void Post(Pawn carrier, Faction faction)
    {
        var posted = new List<Thing>();
        foreach (QslCard card in cards)
        {
            if (card.Delivered) continue;
            QslCardBook book = QslCardBook.Make(card);
            if (GenPlace.TryPlaceThing(book, carrier.Position, carrier.Map, ThingPlaceMode.Near)) posted.Add(book);
            card.Delivered = true;
        }
        // Radio 0.68.0: each award's framed certificate, packed to be placed.
        foreach (QslAward award in awards)
        {
            if (certificatesPosted.Contains(award)) continue;
            Thing certificate = ThingMaker.MakeThing(RadioQslDefOf.PhobosRadio_QslCertificate);
            certificate.SetFaction(Faction.OfPlayer);
            certificate.TryGetComp<CompQslCertificate>()?.SetAward(AwardLabel(award));
            Thing packed = MinifyUtility.MakeMinified(certificate);
            if (GenPlace.TryPlaceThing(packed, carrier.Position, carrier.Map, ThingPlaceMode.Near)) posted.Add(packed);
            certificatesPosted.Add(award);
            if (Seen.Wants("radio.qsl-certificate", carrier, award.ToString())) Seen.Note("radio.qsl-certificate", carrier, AwardLabel(award), award.ToString());
        }
        if (posted.Count == 0) return;
        string text = "PhobosRadioQsl_BureauPost".Translate(posted.Count.Named("COUNT"), faction.Name.Named("FACTION")).Resolve();
        Messages.Message(text, new LookTargets(posted), MessageTypeDefOf.PositiveEvent, historical: false);
        RadioLog.Add(RadioLogKind.Qsl, text);
        if (Seen.Wants("radio.qsl-bureau", carrier)) Seen.Note("radio.qsl-bureau", carrier, posted.Count + " cards by " + faction.Name);
    }

    /// <summary>
    /// A card earned without a settlement's own contact (Radio 0.70.0): a rare station or a listener's card. Kept, said in a message and in
    /// the log, awaited by the bureau, and the awards counted.
    /// </summary>
    public static void Earn(QslCard card, Pawn? op)
    {
        cards.Add(card);
        string text = (card.Kind == QslContact.Rare ? "PhobosRadioQsl_EarnedRare" : "PhobosRadioQsl_EarnedListener").Translate(card.Call.Named("CALL"),
            card.Place.Named("PLACE"), (op?.LabelShort ?? "?").Named("OPERATOR"), card.Report.Named("REPORT")).Resolve();
        Messages.Message(text, MessageTypeDefOf.PositiveEvent, historical: false);
        RadioLog.Add(RadioLogKind.Qsl, text);
        CountAwards();
    }

    private static void CountAwards()
    {
        // Radio 0.70.0: a listener's card is heard, not worked, and counts toward no award.
        List<QslCard> cards = RadioQsl.cards.Where(card => card.Kind != QslContact.Listener).ToList();
        if (cards.Any(card => card.Kind == QslContact.Rare)) Give(QslAward.RareStation);
        var factions = cards.Select(card => card.Faction).Where(f => f != null).Distinct().ToList();
        var withRadio = Find.FactionManager.AllFactionsListForReading.Where(FactionDoctors.Talks).ToList();
        if (factions.Count >= ManyFactions) Give(QslAward.ThreeFactions);
        if (withRadio.Count > 0 && withRadio.All(factions.Contains)) Give(QslAward.AllFactions);
        if (cards.Any(card => card.Tiles >= LongWayTiles)) Give(QslAward.LongWay);
        if (cards.Count >= ManySettlements) Give(QslAward.TenSettlements);
        // Radio 0.69.0: the further awards.
        foreach (QslCard card in cards) card.FillIn();
        int grids = cards.Where(card => card.Grid.Length >= 4).Select(card => card.Grid.Substring(0, 4)).Distinct().Count();
        if (grids >= SomeGrids) Give(QslAward.TenGridSquares);
        if (grids >= ManyGrids) Give(QslAward.TwentyFiveGridSquares);
        double kmPerTile = RadioRulesDef.Current.hfMetresPerWorldTile / 1000.0;
        if (cards.Any(card => card.Tiles * kmPerTile >= FarSideKm)) Give(QslAward.FarSide);
        var talking = Find.WorldObjects.Settlements.Where(st => st.Faction is { } f && FactionDoctors.Talks(f)).ToList();
        if (talking.Count == 0) return;
        var biomes = new HashSet<string>(cards.Select(card => card.Biome).Where(b => b.Length > 0));
        if (talking.All(st => Find.WorldGrid[st.Tile]?.PrimaryBiome is not { } b || biomes.Contains(b.defName))) Give(QslAward.AllBiomes);
        var worked = new HashSet<int>(cards.Where(card => card.TileId >= 0).Select(card => Landmasses.Of(card.TileId)).Where(id => id >= 0));
        if (talking.All(st => Landmasses.Of(st.Tile.tileId) is var id && (id < 0 || worked.Contains(id)))) Give(QslAward.AllLandmasses);
    }

    private static void Give(QslAward award)
    {
        if (awards.Contains(award)) return;
        awards.Add(award);
        string text = "PhobosRadioQsl_Award".Translate(AwardLabel(award).Named("AWARD")).Resolve();
        Find.LetterStack.ReceiveLetter("PhobosRadioQsl_AwardLabel".Translate(), text, LetterDefOf.PositiveEvent);
        RadioLog.Add(RadioLogKind.Award, text);
        if (Seen.Wants("radio.qsl-award", null, award.ToString())) Seen.Note("radio.qsl-award", null, award.ToString(), award.ToString());
    }

    public static string AwardLabel(QslAward award) => award switch
    {
        QslAward.ThreeFactions => "PhobosRadioQsl_AwardThreeFactions".Translate(ManyFactions.Named("COUNT")).Resolve(),
        QslAward.AllFactions => "PhobosRadioQsl_AwardAllFactions".Translate().Resolve(),
        QslAward.LongWay => "PhobosRadioQsl_AwardLongWay".Translate(LongWayTiles.Named("TILES")).Resolve(),
        QslAward.AllBiomes => "PhobosRadioQsl_AwardAllBiomes".Translate().Resolve(),
        QslAward.TenGridSquares => "PhobosRadioQsl_AwardGridSquares".Translate(SomeGrids.Named("COUNT")).Resolve(),
        QslAward.TwentyFiveGridSquares => "PhobosRadioQsl_AwardGridSquares".Translate(ManyGrids.Named("COUNT")).Resolve(),
        QslAward.FarSide => "PhobosRadioQsl_AwardFarSide".Translate(FarSideKm.Named("KM")).Resolve(),
        QslAward.AllLandmasses => "PhobosRadioQsl_AwardAllLandmasses".Translate().Resolve(),
        QslAward.RareStation => "PhobosRadioQsl_AwardRareStation".Translate().Resolve(),
        _ => "PhobosRadioQsl_AwardTenSettlements".Translate(ManySettlements.Named("COUNT")).Resolve(),
    };

    /// <summary>Whether a colonist works the radio, for the awards' mood: a hand-held worn or carried (Radio 0.67.0), or a radio friend.</summary>
    public static bool WorksTheRadio(Pawn pawn) => RadioFriends.Of(pawn) != null || PawnHandhelds.First(pawn) != null;
}

/// <summary>"Our station is known far and wide": +2 an award, up to +6, for colonists who work the radio (Radio 0.38.0).</summary>
public class ThoughtWorker_PhobosRadioAwards : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        int count = RadioQsl.Awards.Count;
        if (count == 0 || !p.IsColonist || !RadioQsl.WorksTheRadio(p)) return ThoughtState.Inactive;
        return ThoughtState.ActiveAtStage(System.Math.Min(count, 3) - 1);
    }
}
