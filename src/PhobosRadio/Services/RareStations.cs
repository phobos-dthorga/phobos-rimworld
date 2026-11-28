using System.Collections.Generic;
using System.Linq;
using Phobos.Radio.Physics;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>A rare station on the air (Radio 0.70.0): where, what, its callsign and when it goes off the air.</summary>
public sealed class RareStation : IExposable
{
    public int TileId = -1;
    /// <summary>Its kind's key: Ship, Ice or Outpost.</summary>
    public string Kind = "Outpost";
    public string Call = "";
    public int EndsTick;

    public void ExposeData()
    {
        Scribe_Values.Look(ref TileId, "PhobosRareTile", -1);
        Scribe_Values.Look(ref Kind, "PhobosRareKind", "Outpost");
        Scribe_Values.Look(ref Call, "PhobosRareCall", "");
        Scribe_Values.Look(ref EndsTick, "PhobosRareEnds");
    }

    public PlanetTile Tile => new(TileId, Find.WorldGrid.Surface);
}

/// <summary>
/// Rare stations and listener cards (Radio 0.70.0; docs/development/radio-qsl-in-play.md, part 4, the owner's cards of 28 Nov 2026).
/// <para>Now and then a rare station comes on the air for a few hours from somewhere unusual (a ship at sea, an expedition on the ice, a
/// lone outpost), at least the rules' distance away; an HF station works it while a band is open, for a rare card. A settlement the
/// colony hears but cannot reach (the link's one direction) earns a listener's card, marked heard only.</para>
/// <para>Cost: once a game hour, one roll while no rare station is on the air, and a look at a few settlements for listener cards (at
/// most <see cref="ListenLooks"/> links weighed); nothing per tick.</para>
/// </summary>
public static class RareStations
{
    /// <summary>A rare station comes on the air about this often, in days (authored: about once a season).</summary>
    public const float MeanDays = 15f;
    /// <summary>At least this far away, in tiles (about 1,000 km at HF's scale; authored).</summary>
    public const int MinTiles = 34;
    public const int MinHours = 6;
    public const int MaxHours = 12;
    /// <summary>How many settlements the hourly listen weighs, at most (authored, for cost).</summary>
    public const int ListenLooks = 8;

    public static RareStation? OnAir;
    private static int listenFrom;

    public static void Clear()
    {
        OnAir = null;
        listenFrom = 0;
    }

    public static void ExposeData()
    {
        Scribe_Deep.Look(ref OnAir, "PhobosRadioRareStation");
    }

    /// <summary>Whether the colony has an HF station to hear one with.</summary>
    static Map? HfHome() =>
        Find.Maps?.FirstOrDefault(map => map.IsPlayerHome && RadioNet.For(map).Stations.Any(s => s.Bands.Any(b => b.path == BandPath.Hf)));

    public static void Hourly()
    {
        int now = Find.TickManager.TicksGame;
        if (OnAir != null && now >= OnAir.EndsTick) OnAir = null;
        if (HfHome() is not { } home) return;
        if (OnAir == null && Rand.MTBEventOccurs(MeanDays, GenDate.TicksPerDay, GenDate.TicksPerHour)) Start(home);
        Listen(home);
    }

    static void Start(Map home)
    {
        PlanetLayer surface = Find.WorldGrid.Surface;
        for (int attempt = 0; attempt < 30; attempt++)
        {
            int id = Rand.Range(0, surface.TilesCount);
            var tile = new PlanetTile(id, surface);
            if (Find.WorldGrid.ApproxDistanceInTiles(home.Tile, tile) < MinTiles) continue;
            string kind = surface[id].WaterCovered ? "Ship"
                : GroundWave.GroundOf(surface[id].PrimaryBiome?.defName ?? "") == HfGround.Ice ? "Ice" : "Outpost";
            string call = QslRule.Callsign(QslCalls.PlacePrefixOf(tile), Find.WorldGrid.LongLatOf(tile).y, id) + (kind == "Ship" ? "/MM" : "/P");
            OnAir = new RareStation { TileId = id, Kind = kind, Call = call, EndsTick = Find.TickManager.TicksGame + Rand.Range(MinHours, MaxHours + 1) * GenDate.TicksPerHour };
            string area = QslCalls.AreaOf(tile);
            Find.LetterStack.ReceiveLetter("PhobosRadioRare_Label".Translate(), "PhobosRadioRare_Text".Translate(call.Named("CALL"),
                ("PhobosRadioRare_Kind" + kind).Translate().Named("KIND"), (area.Length > 0 ? area : "?").Named("AREA")),
                LetterDefOf.PositiveEvent, new LookTargets(new GlobalTargetInfo(tile)));
            if (Seen.Wants("radio.rare-station", null, kind)) Seen.Note("radio.rare-station", null, call, kind);
            return;
        }
    }

    /// <summary>The best link from a station to the rare station now, or null.</summary>
    static LinkEvaluation? LinkTo(CompRadio station, RareStation rare)
    {
        if (FactionDoctors.RemoteEnd() is not { } far) return null;
        Map map = station.parent.Map;
        LinkEvaluation? best = null;
        foreach (RadioEndpoint end in RadioNet.StationEndpoints(station, RadioMod.Settings.metresPerCell))
        {
            LinkEvaluation? link = LinkBudgetService.EvaluateWorld(end, map.Tile, RelayNode.SiteOf(map.Tile), far, rare.Tile, 0, RadioRulesDef.Current, out _);
            if (link != null && (best == null || link.MarginDb > best.MarginDb)) best = link;
        }
        return best;
    }

    /// <summary>A station works the rare station on the air (from its button): a rare card while a band is open; otherwise why not.</summary>
    public static void Work(CompRadio station)
    {
        if (OnAir is not { } rare) return;
        if (RadioQsl.Cards.Any(card => card.Kind == QslContact.Rare && card.Call == rare.Call))
        {
            Messages.Message("PhobosRadioRare_HaveCard".Translate(rare.Call.Named("CALL")), MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        if (LinkTo(station, rare) is not { InTouch: true } link)
        {
            Messages.Message("PhobosRadioRare_NotOpen".Translate(rare.Call.Named("CALL")), MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        Pawn? op = Operator(station.parent.Map);
        RadioQsl.Earn(new QslCard
        {
            Place = ("PhobosRadioRare_Kind" + rare.Kind).Translate().CapitalizeFirst(),
            Operator = op?.LabelShort ?? "?",
            Tick = Find.TickManager.TicksAbs,
            Report = RadioQsl.Report(link.MarginDb, link.Mode.keyed),
            Tiles = (int)Find.WorldGrid.ApproxDistanceInTiles(station.parent.Map.Tile, rare.Tile),
            Kind = QslContact.Rare,
            Band = link.Band,
            Mode = link.Mode,
            Call = rare.Call,
            Area = QslCalls.AreaOf(rare.Tile),
            TileId = rare.TileId,
            Biome = Find.WorldGrid[rare.Tile]?.PrimaryBiome?.defName ?? "",
            Grid = QslCalls.Locator(rare.Tile),
            OurCall = QslCalls.Home(station.parent.Map.Tile),
        }, op);
        if (Seen.Wants("radio.rare-worked", station.parent, rare.Kind)) Seen.Note("radio.rare-worked", station.parent, rare.Call + ", " + link.Band.label, rare.Kind);
    }

    static Pawn? Operator(Map map) =>
        map.mapPawns.FreeColonists.FirstOrDefault(RadioQsl.WorksTheRadio) ?? map.mapPawns.FreeColonists.FirstOrDefault();

    /// <summary>A listener's card: a settlement heard at the colony (their signal reaches us) that our signal does not reach, a few weighed
    /// an hour in turn.</summary>
    static void Listen(Map home)
    {
        var settlements = Find.WorldObjects.Settlements
            .Where(st => st.Faction is { } f && FactionDoctors.Talks(f) && !RadioQsl.Cards.Any(card => card.Settlement == st))
            .Where(st => Find.WorldGrid.ApproxDistanceInTiles(home.Tile, st.Tile) <= RadioRulesDef.Current.factionSearchTiles)
            .OrderBy(st => st.ID).ToList();
        if (settlements.Count == 0) return;
        List<RelayNode> ends = FactionDoctors.StationEnds(home);
        if (ends.Count == 0) return;
        RelayNode ours = ends[0];
        for (int i = 0; i < System.Math.Min(ListenLooks, settlements.Count); i++)
        {
            Settlement st = settlements[(listenFrom + i) % settlements.Count];
            if (FactionDoctors.EndOf(st) is not { } theirs) continue;
            LinkEvaluation? link = LinkBudgetService.EvaluateWorld(ours, theirs, RadioRulesDef.Current);
            if (link == null || link.InTouch || link.Budget.Reverse.MarginDb < RadioRulesDef.Current.marginalDb) continue;
            Pawn? op = Operator(home);
            RadioQsl.Earn(new QslCard
            {
                Settlement = st,
                Faction = st.Faction,
                Place = st.LabelCap,
                Operator = op?.LabelShort ?? "?",
                Tick = Find.TickManager.TicksAbs,
                Report = RadioQsl.Report(link.Budget.Reverse.MarginDb, link.Mode.keyed),
                Tiles = (int)Find.WorldGrid.ApproxDistanceInTiles(home.Tile, st.Tile),
                Kind = QslContact.Listener,
                Band = link.Band,
                Mode = link.Mode,
                Call = QslCalls.Of(st),
                Area = QslCalls.AreaOf(st.Tile),
                TileId = st.Tile.tileId,
                Biome = Find.WorldGrid[st.Tile]?.PrimaryBiome?.defName ?? "",
                Grid = QslCalls.Locator(st.Tile),
                OurCall = QslCalls.Home(home.Tile),
            }, op);
            if (Seen.Wants("radio.listener-card", null, link.Band.defName)) Seen.Note("radio.listener-card", null, st.LabelCap + ", " + link.Band.label, link.Band.defName);
            break;
        }
        listenFrom += ListenLooks;
    }
}
