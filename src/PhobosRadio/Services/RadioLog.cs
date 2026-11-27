using System.Collections.Generic;
using System.Linq;
using Phobos.Radio.Physics;
using RimWorld;
using RimWorld.Planet;
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

    public void ExposeData()
    {
        Scribe_References.Look(ref Settlement, "PhobosQslSettlement");
        Scribe_References.Look(ref Faction, "PhobosQslFaction");
        Scribe_Values.Look(ref Place, "PhobosQslPlace", "");
        Scribe_Values.Look(ref Operator, "PhobosQslOperator", "");
        Scribe_Values.Look(ref Tick, "PhobosQslTick");
        Scribe_Values.Look(ref Report, "PhobosQslReport", "");
        Scribe_Values.Look(ref Tiles, "PhobosQslTiles");
    }
}

/// <summary>The awards a set of cards earns (agent defaults; the owner's card gave what an award brings).</summary>
public enum QslAward
{
    ThreeFactions,
    AllFactions,
    LongWay,
    TenSettlements,
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

    private static List<QslCard> cards = new();
    private static List<QslAward> awards = new();

    public static IReadOnlyList<QslCard> Cards => cards;
    public static IReadOnlyList<QslAward> Awards => awards;

    public static void Clear()
    {
        cards = new List<QslCard>();
        awards = new List<QslAward>();
    }

    public static void ExposeData()
    {
        Scribe_Collections.Look(ref cards, "PhobosRadioQslCards", LookMode.Deep);
        Scribe_Collections.Look(ref awards, "PhobosRadioQslAwards", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            cards ??= new List<QslCard>();
            awards ??= new List<QslAward>();
        }
    }

    /// <summary>
    /// The operators' signal report for a link, readability and strength (RS, 31 to 59; agent's mapping): readable 5 above a workable margin,
    /// 4 when marginal; strength one S-unit for each 3 dB of margin, at least 1. Approximate, and only for the card.
    /// </summary>
    public static string Report(double marginDb)
    {
        int readability = marginDb >= RadioRulesDef.Current.workableDb ? 5 : marginDb >= RadioRulesDef.Current.marginalDb ? 4 : 3;
        int strength = System.Math.Max(1, System.Math.Min(9, 1 + (int)System.Math.Floor(System.Math.Max(0.0, marginDb) / 3.0)));
        return readability.ToString() + strength.ToString();
    }

    /// <summary>A call with a settlement happened: the first earns its card, said in a message and in the log, and the awards are counted.</summary>
    public static void Worked(Settlement settlement, Pawn colonist, double marginDb, PlanetTile from)
    {
        if (settlement.Faction is not { } faction || cards.Any(card => card.Settlement == settlement)) return;
        int tiles = from.Valid ? (int)Find.WorldGrid.ApproxDistanceInTiles(from, settlement.Tile) : 0;
        var card = new QslCard
        {
            Settlement = settlement,
            Faction = faction,
            Place = settlement.LabelCap,
            Operator = colonist.LabelShort,
            Tick = Find.TickManager.TicksAbs,
            Report = Report(marginDb),
            Tiles = tiles,
        };
        cards.Add(card);
        string text = "PhobosRadioQsl_Earned".Translate(settlement.LabelCap.Named("PLACE"), faction.Name.Named("FACTION"), colonist.LabelShort.Named("OPERATOR"),
            card.Report.Named("REPORT")).Resolve();
        Messages.Message(text, MessageTypeDefOf.PositiveEvent, historical: false);
        RadioLog.Add(RadioLogKind.Qsl, text);
        if (Seen.Wants("radio.qsl-card", colonist)) Seen.Note("radio.qsl-card", colonist, settlement.LabelCap + ", " + card.Report);
        CountAwards();
    }

    private static void CountAwards()
    {
        var factions = cards.Select(card => card.Faction).Where(f => f != null).Distinct().ToList();
        var withRadio = Find.FactionManager.AllFactionsListForReading.Where(FactionDoctors.Talks).ToList();
        if (factions.Count >= ManyFactions) Give(QslAward.ThreeFactions);
        if (withRadio.Count > 0 && withRadio.All(factions.Contains)) Give(QslAward.AllFactions);
        if (cards.Any(card => card.Tiles >= LongWayTiles)) Give(QslAward.LongWay);
        if (cards.Count >= ManySettlements) Give(QslAward.TenSettlements);
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
        _ => "PhobosRadioQsl_AwardTenSettlements".Translate(ManySettlements.Named("COUNT")).Resolve(),
    };

    /// <summary>Whether a colonist works the radio, for the awards' mood: wearing a hand-held, or with a radio friend.</summary>
    public static bool WorksTheRadio(Pawn pawn) =>
        RadioFriends.Of(pawn) != null || pawn.apparel?.WornApparel.Any(worn => worn.TryGetComp<CompRadio>() != null) == true;
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
