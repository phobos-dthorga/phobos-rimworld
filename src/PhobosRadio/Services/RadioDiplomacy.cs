using System.Collections.Generic;
using Phobos.Radio.Physics;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>
/// Calling another faction on the radio from a station's desk (Radio 0.53.0; docs/development/radio-comms-console.md, shape S2; the owner's
/// cards of 27 Nov 2026). Only factions whose settlement the colony's stations reach, through relays, by the one link budget
/// (<see cref="FactionDoctors.ReachToTalk"/>), and only those who talk on the radio. The game's comms console is untouched and still reaches
/// every faction. What a radio call brings, the owner's picks: help from the settlement in reach comes sooner (B1); a clean line costs less
/// goodwill (B2); check-ins earn a little (B5); a QSL card for the first call (B6). What it costs: a seat and a channel; on a marginal line
/// one request in four is misheard, the goodwill spent; and a hostile settlement that hears a trader asked for may ambush it on its way.
/// The game's own cooldowns are shared, so a request by radio and one by console cannot both be made. Every figure is in the rules Def.
/// </summary>
public static class RadioDiplomacy
{
    /// <summary>What the game's console charges for these, in goodwill (FactionDialogMaker): the radio's price on any line but a clean one.</summary>
    public const int ConsoleTraderGoodwill = 15;
    public const int ConsoleAidGoodwill = 25;
    /// <summary>The game's own waits between requests to one faction, in ticks (FactionDialogMaker).</summary>
    const int TraderRequestWaitTicks = 240000;
    const int AidRequestWaitTicks = 60000;
    /// <summary>The game's fixed wait for a requested trader caravan, in ticks: the radio's is never longer.</summary>
    public const int ConsoleTraderArrivalTicks = 120000;
    /// <summary>The least a caravan takes to set out and arrive, however close the settlement (authored: an hour).</summary>
    const int LeastArrivalTicks = 2500;

    /// <summary>This faction's settlement the colony's stations reach, and how well, or null.</summary>
    public static FactionDoctorLink? Reach(Map map, Faction faction) =>
        FactionDoctors.Talks(faction) ? FactionDoctors.ReachToTalk(map, map.Tile, () => FactionDoctors.StationEnds(map), faction) : null;

    static readonly Dictionary<Map, (int tick, List<FactionDoctorLink> links)> inReach = new();

    /// <summary>
    /// The settlement each faction that talks on the radio has in reach of this map's stations, one a faction, worked out once a game hour
    /// (Radio 0.54.0): what the logbook's "Settlements in reach" and the world map's contact arcs both show.
    /// </summary>
    public static IReadOnlyList<FactionDoctorLink> InReach(Map map)
    {
        int now = Find.TickManager.TicksGame;
        if (inReach.TryGetValue(map, out var held) && now - held.tick >= 0 && now - held.tick < GenDate.TicksPerHour) return held.links;
        if (inReach.Count > 8) inReach.Clear();
        var links = new List<FactionDoctorLink>();
        foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
            if (!faction.IsPlayer && Reach(map, faction) is { } link) links.Add(link);
        inReach[map] = (now, links);
        return links;
    }

    /// <summary>How good a line is, by the link's margin.</summary>
    public static LinkOutcome Line(FactionDoctorLink link) => RadioRulesDef.Current.MarginBands.Classify(link.MarginDb);

    /// <summary>The goodwill a request costs over this line (B2): less on a clean one, the console's price otherwise.</summary>
    public static int TraderGoodwill(LinkOutcome line) => line == LinkOutcome.Clean ? Mathf.RoundToInt(RadioRulesDef.Current.radioTraderGoodwillClean) : ConsoleTraderGoodwill;
    public static int AidGoodwill(LinkOutcome line) => line == LinkOutcome.Clean ? Mathf.RoundToInt(RadioRulesDef.Current.radioAidGoodwillClean) : ConsoleAidGoodwill;

    // ---- What may be asked ----

    /// <summary>Why a trader caravan cannot be asked for now, in the player's words, or null: the game's own conditions.</summary>
    public static string? TraderRefusal(Map map, Faction faction)
    {
        if (faction.PlayerRelationKind != FactionRelationKind.Ally) return "MustBeAlly".Translate();
        if (!faction.def.allowedArrivalTemperatureRange.ExpandedBy(-4f).Includes(map.mapTemperature.SeasonalTemp)) return "BadTemperature".Translate();
        int wait = faction.lastTraderRequestTick + TraderRequestWaitTicks - Find.TickManager.TicksGame;
        return wait > 0 ? "WaitTime".Translate(wait.ToStringTicksToPeriod()) : null;
    }

    /// <summary>Why military aid cannot be asked for now, or null: the game's own conditions.</summary>
    public static string? AidRefusal(Map map, Faction faction)
    {
        if (faction.PlayerRelationKind != FactionRelationKind.Ally) return "MustBeAlly".Translate();
        if (!faction.def.allowedArrivalTemperatureRange.ExpandedBy(-4f).Includes(map.mapTemperature.SeasonalTemp)) return "BadTemperature".Translate();
        int wait = faction.lastMilitaryAidRequestTick + AidRequestWaitTicks - Find.TickManager.TicksGame;
        if (wait > 0) return "WaitTime".Translate(wait.ToStringTicksToPeriod());
        if (NeutralGroupIncidentUtility.AnyBlockingHostileLord(map, faction)) return "HostileVisitorsPresent".Translate();
        return (int)faction.def.techLevel < (int)TechLevel.Industrial ? "PhobosRadioCall_AidTooFar".Translate(faction.Name.Named("FACTION")).Resolve() : null;
    }

    /// <summary>Why a check-in would earn nothing now, or null (B5).</summary>
    public static string? CheckInRefusal(Faction faction)
    {
        RadioRulesDef rules = RadioRulesDef.Current;
        if (faction.PlayerGoodwill >= rules.radioCheckInGoodwillCap) return "PhobosRadioCall_CheckInFriends".Translate(faction.Name.Named("FACTION")).Resolve();
        int wait = GameComponent_PhobosRadioDiplomacy.Current?.CheckInWait(faction) ?? 0;
        return wait > 0 ? "WaitTime".Translate(wait.ToStringTicksToPeriod()) : null;
    }

    // ---- The requests ----

    /// <summary>
    /// A trader caravan asked for by radio: the goodwill paid at once; misheard on a marginal line now and then; otherwise sent from the
    /// settlement in reach, arriving after the travel from there (never later than the console's) unless a hostile listener waylays it.
    /// Returns what the faction says back.
    /// </summary>
    public static string RequestTrader(Pawn negotiator, Map map, Faction faction, TraderKindDef kind, FactionDoctorLink link)
    {
        LinkOutcome line = Line(link);
        Faction.OfPlayer.TryAffectGoodwillWith(faction, -TraderGoodwill(line), canSendMessage: false, canSendHostilityLetter: true, HistoryEventDefOf.RequestedTrader);
        Called(negotiator, faction, link, "trader");
        if (Misheard(line)) return MisheardReply(faction);
        faction.lastTraderRequestTick = Find.TickManager.TicksGame;
        int arrive = Find.TickManager.TicksGame + TravelTicks(link.Settlement.Tile, map.Tile);
        (int ambush, Faction? by) = Ambush(map);
        GameComponent_PhobosRadioDiplomacy.Current?.Order(new RadioTraderOrder(faction, map, kind, arrive, ambush, by, link.Settlement.LabelCap));
        return "PhobosRadioCall_TraderSent".Translate(faction.Name.Named("FACTION"), link.Settlement.LabelCap.Named("SETTLEMENT"),
            (arrive - Find.TickManager.TicksGame).ToStringTicksToPeriod().Named("TIME")).Resolve();
    }

    /// <summary>Military aid asked for by radio: as the console's call, at the line's price, misheard now and then on a marginal line.</summary>
    public static string RequestAid(Pawn negotiator, Map map, Faction faction, FactionDoctorLink link)
    {
        LinkOutcome line = Line(link);
        Faction.OfPlayer.TryAffectGoodwillWith(faction, -AidGoodwill(line), canSendMessage: false, canSendHostilityLetter: true, HistoryEventDefOf.RequestedMilitaryAid);
        Called(negotiator, faction, link, "aid");
        if (Misheard(line)) return MisheardReply(faction);
        faction.lastMilitaryAidRequestTick = Find.TickManager.TicksGame;
        var parms = new IncidentParms
        {
            target = map,
            faction = faction,
            raidArrivalModeForQuickMilitaryAid = true,
            points = DiplomacyTuning.RequestedMilitaryAidPointsRange.RandomInRange,
        };
        IncidentDefOf.RaidFriendly.Worker.TryExecute(parms);
        return "PhobosRadioCall_AidSent".Translate(faction.Name.Named("FACTION")).Resolve();
    }

    /// <summary>A check-in (B5): a little goodwill, and a little Social for the one who called.</summary>
    public static string CheckIn(Pawn negotiator, Faction faction, FactionDoctorLink link)
    {
        RadioRulesDef rules = RadioRulesDef.Current;
        int gain = Mathf.Min(rules.radioCheckInGoodwill, Mathf.Max(0, rules.radioCheckInGoodwillCap - faction.PlayerGoodwill));
        if (gain > 0) Faction.OfPlayer.TryAffectGoodwillWith(faction, gain, canSendMessage: false, canSendHostilityLetter: false, RadioDiplomacyDefOf.PhobosRadio_CheckedInByRadio);
        negotiator.skills?.Learn(SkillDefOf.Social, rules.radioCheckInSocialXp);
        GameComponent_PhobosRadioDiplomacy.Current?.CheckedIn(faction);
        Called(negotiator, faction, link, "check-in");
        return "PhobosRadioCall_CheckedIn".Translate(faction.Name.Named("FACTION"), gain.Named("GOODWILL")).Resolve();
    }

    // ---- The pieces ----

    /// <summary>A supply drop over this line (Radio 0.54.0, B2): cheaper on a clean one, the console's price otherwise.</summary>
    public static int SupplyGoodwill(LinkOutcome line, int consolePrice) =>
        line == LinkOutcome.Clean ? Mathf.Min(consolePrice, Mathf.RoundToInt(RadioRulesDef.Current.radioSupplyGoodwillClean)) : consolePrice;

    /// <summary>Whether a request over this line is misheard: only on a marginal line, at the rules' chance (one in four).</summary>
    public static bool Misheard(LinkOutcome line)
    {
        if (line != LinkOutcome.Marginal || !Rand.Chance(RadioRulesDef.Current.radioMishearChance)) return false;
        if (Seen.Wants("radio.call-misheard", null)) Seen.Note("radio.call-misheard");
        return true;
    }

    public static string MisheardReply(Faction faction) => "PhobosRadioCall_Misheard".Translate(faction.Name.Named("FACTION")).Resolve();

    /// <summary>What every call does besides its request: the logbook, a QSL card for a first call, and the note that it was seen.</summary>
    public static void Called(Pawn negotiator, Faction faction, FactionDoctorLink link, string what)
    {
        RadioLog.Add(RadioLogKind.Diplomacy, "PhobosRadioLog_FactionCall".Translate(negotiator.LabelShort.Named("COLONIST"), faction.Name.Named("FACTION"),
            link.Settlement.LabelCap.Named("SETTLEMENT")).Resolve());
        RadioQsl.Worked(link.Settlement, negotiator, link.MarginDb, negotiator.MapHeld?.Tile ?? PlanetTile.Invalid);
        if (Seen.Wants("radio.faction-call", negotiator, what)) Seen.Note("radio.faction-call", negotiator, faction.Name, what);
    }

    /// <summary>
    /// How long a caravan takes from the settlement (B1): its tiles at a walking caravan's pace, the rules' factor for the ground, never less
    /// than an hour and never longer than the console's two days. An approximation: the real path is not walked.
    /// </summary>
    public static int TravelTicks(PlanetTile from, PlanetTile to)
    {
        float tiles = Find.WorldGrid.ApproxDistanceInTiles(from, to);
        int ticks = Mathf.RoundToInt(tiles * CaravanTicksPerMoveUtility.DefaultTicksPerMove * RadioRulesDef.Current.radioTraderTravelFactor);
        return Mathf.Clamp(ticks, LeastArrivalTicks, ConsoleTraderArrivalTicks);
    }

    /// <summary>
    /// Whether a hostile listener waylays the caravan (the owner's card: ambush the trader): each hostile settlement that hears the colony's
    /// stations adds the rules' chance; ambushed, it is lost or arrives hurt, half and half (authored). 0 none, 1 hurt, 2 lost.
    /// </summary>
    static (int, Faction?) Ambush(Map map)
    {
        if (!RadioMod.Settings.listenersAmbushTraders) return (0, null);
        List<Faction> listeners = Listeners(map);
        if (listeners.Count == 0) return (0, null);
        float spared = Mathf.Pow(1f - RadioRulesDef.Current.radioListenerAmbushChance, listeners.Count);
        if (!Rand.Chance(1f - spared)) return (0, null);
        return (Rand.Chance(RadioRulesDef.Current.radioAmbushLostShare) ? 2 : 1, listeners.RandomElement());
    }

    /// <summary>
    /// The hostile factions with a settlement that hears one of the colony's stations directly, at least faintly (a marginal line), within the
    /// rules' search: who could overhear a call. Asked once a request.
    /// </summary>
    public static List<Faction> Listeners(Map map)
    {
        var heard = new List<Faction>();
        RadioRulesDef rules = RadioRulesDef.Current;
        List<RelayNode> stations = FactionDoctors.StationEnds(map);
        if (stations.Count == 0) return heard;
        foreach (Settlement settlement in Find.WorldObjects.Settlements)
        {
            if (settlement.Faction is not { } faction || heard.Contains(faction) || !faction.HostileTo(Faction.OfPlayer)) continue;
            if (Find.WorldGrid.ApproxDistanceInTiles(map.Tile, settlement.Tile) > rules.factionSearchTiles) continue;
            if (FactionDoctors.EndOf(settlement) is not { } ear) continue;
            foreach (RelayNode station in stations)
            {
                if (LinkBudgetService.EvaluateWorld(station, ear, rules) is not { } link || link.Outcome < LinkOutcome.Marginal) continue;
                heard.Add(faction);
                break;
            }
        }
        return heard;
    }
}

/// <summary>A trader caravan asked for by radio, on its way (Radio 0.53.0): saved until it arrives, or is lost to an ambush.</summary>
public sealed class RadioTraderOrder : IExposable
{
    public Faction? faction;
    public Map? map;
    public TraderKindDef? kind;
    public int arriveTick;
    /// <summary>0 none, 1 arrives hurt, 2 lost.</summary>
    public int ambush;
    public Faction? ambusher;
    public string from = "";

    public RadioTraderOrder() { }

    public RadioTraderOrder(Faction faction, Map map, TraderKindDef kind, int arriveTick, int ambush, Faction? ambusher, string from)
    {
        this.faction = faction;
        this.map = map;
        this.kind = kind;
        this.arriveTick = arriveTick;
        this.ambush = ambush;
        this.ambusher = ambusher;
        this.from = from;
    }

    public void ExposeData()
    {
        Scribe_References.Look(ref faction, "faction");
        Scribe_References.Look(ref map, "map");
        Scribe_Defs.Look(ref kind, "kind");
        Scribe_Values.Look(ref arriveTick, "arriveTick");
        Scribe_Values.Look(ref ambush, "ambush");
        Scribe_References.Look(ref ambusher, "ambusher");
        Scribe_Values.Look(ref from, "from", "");
    }
}

/// <summary>
/// The radio's calls to other factions that outlast the call (Radio 0.53.0): trader caravans on their way, and when each faction was last
/// checked in with. Saved. Cost: once every 250 ticks, a look at the caravans on their way (a handful at most).
/// </summary>
public class GameComponent_PhobosRadioDiplomacy : GameComponent
{
    const int LookTicks = 250;
    /// <summary>How long a caravan that cannot arrive yet keeps trying, as the game's queued trader does (a day).</summary>
    const int RetryTicks = 60000;

    List<RadioTraderOrder> orders = new();
    Dictionary<Faction, int> checkedIn = new();
    List<Faction>? checkInKeys;
    List<int>? checkInValues;

    public GameComponent_PhobosRadioDiplomacy(Game game) { }

    public static GameComponent_PhobosRadioDiplomacy? Current => Verse.Current.Game?.GetComponent<GameComponent_PhobosRadioDiplomacy>();

    public void Order(RadioTraderOrder order) => orders.Add(order);

    public void CheckedIn(Faction faction) => checkedIn[faction] = Find.TickManager.TicksGame;

    /// <summary>Ticks until this faction can be checked in with again; 0 or less when it can.</summary>
    public int CheckInWait(Faction faction) =>
        checkedIn.TryGetValue(faction, out int last) ? last + Mathf.RoundToInt(RadioRulesDef.Current.radioCheckInDays * GenDate.TicksPerDay) - Find.TickManager.TicksGame : 0;

    public override void GameComponentTick()
    {
        int now = Find.TickManager.TicksGame;
        if (now % LookTicks != 0 || orders.Count == 0) return;
        for (int i = orders.Count - 1; i >= 0; i--)
        {
            RadioTraderOrder order = orders[i];
            if (order.arriveTick > now) continue;
            if (order.faction == null || order.map == null || order.kind == null || !Find.Maps.Contains(order.map) || order.faction.HostileTo(Faction.OfPlayer))
            {
                orders.RemoveAt(i);
                continue;
            }
            if (order.ambush == 2)
            {
                orders.RemoveAt(i);
                Find.LetterStack.ReceiveLetter("PhobosRadioCall_AmbushLostLabel".Translate(), "PhobosRadioCall_AmbushLost".Translate(order.faction.Name.Named("FACTION"),
                    (order.ambusher?.Name ?? "?").Named("AMBUSHERS")).Resolve(), LetterDefOf.NegativeEvent, new LookTargets(order.map.Parent));
                if (Seen.Wants("radio.trader-ambushed", null, "lost")) Seen.Note("radio.trader-ambushed", null, order.faction.Name, "lost");
                continue;
            }
            if (Arrive(order))
            {
                orders.RemoveAt(i);
                continue;
            }
            if (now - order.arriveTick > RetryTicks) orders.RemoveAt(i);
        }
    }

    /// <summary>The caravan arrives as the game's requested trader does; an ambushed one comes hurt. False when it could not arrive now.</summary>
    static bool Arrive(RadioTraderOrder order)
    {
        Map map = order.map!;
        var before = new HashSet<Pawn>(map.mapPawns.SpawnedPawnsInFaction(order.faction!));
        var parms = new IncidentParms { target = map, faction = order.faction, traderKind = order.kind, forced = true };
        if (!IncidentDefOf.TraderCaravanArrival.Worker.TryExecute(parms)) return false;
        if (Seen.Wants("radio.trader-arrived", null)) Seen.Note("radio.trader-arrived", null, order.faction!.Name + " from " + order.from);
        if (order.ambush != 1) return true;
        foreach (Pawn pawn in map.mapPawns.SpawnedPawnsInFaction(order.faction!))
        {
            if (before.Contains(pawn) || !pawn.RaceProps.Humanlike || !Rand.Chance(0.5f)) continue;
            // Authored: a cut or two each, for the half of them that were hit.
            for (int hit = Rand.RangeInclusive(1, 2); hit > 0; hit--) pawn.TakeDamage(new DamageInfo(DamageDefOf.Cut, Rand.Range(5f, 10f)));
        }
        Find.LetterStack.ReceiveLetter("PhobosRadioCall_AmbushHurtLabel".Translate(), "PhobosRadioCall_AmbushHurt".Translate(order.faction!.Name.Named("FACTION"),
            (order.ambusher?.Name ?? "?").Named("AMBUSHERS")).Resolve(), LetterDefOf.NeutralEvent, new LookTargets(map.Parent));
        if (Seen.Wants("radio.trader-ambushed", null, "hurt")) Seen.Note("radio.trader-ambushed", null, order.faction.Name, "hurt");
        return true;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref orders, "PhobosRadio_TraderOrders", LookMode.Deep);
        Scribe_Collections.Look(ref checkedIn, "PhobosRadio_CheckedIn", LookMode.Reference, LookMode.Value, ref checkInKeys, ref checkInValues);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            orders ??= new List<RadioTraderOrder>();
            orders.RemoveAll(order => order.faction == null || order.map == null || order.kind == null);
            checkedIn ??= new Dictionary<Faction, int>();
            checkedIn.RemoveAll(entry => entry.Key == null);
        }
    }
}

[DefOf]
public static class RadioDiplomacyDefOf
{
    public static JobDef PhobosRadio_CallFaction = null!;
    public static HistoryEventDef PhobosRadio_CheckedInByRadio = null!;

    static RadioDiplomacyDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(RadioDiplomacyDefOf));
}
