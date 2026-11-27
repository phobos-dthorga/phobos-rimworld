using HarmonyLib;
using Phobos.Radio.Services;
using RimWorld;
using Verse;

namespace Phobos.Radio;

/// <summary>
/// A QSL card you can hold (Radio 0.66.0; docs/development/radio-qsl-in-play.md, part 1, the owner's words of 28 Nov 2026: "every stored
/// record is a truly realized book (or in this case a QSL card) that pawns can interact with, read, etc. for mood benefits"): the game's
/// own book, so colonists read it with the game's reading job, keep it on a shelf or a QSL wall, and the logbook card it confirms rides
/// with it. It arrives by the bureau (<see cref="RadioQsl.Bureau"/>).
/// </summary>
public class QslCardBook : Book
{
    static readonly AccessTools.FieldRef<Book, string> TitleRef = AccessTools.FieldRefAccess<Book, string>("title");
    static readonly AccessTools.FieldRef<Book, string> DescriptionRef = AccessTools.FieldRefAccess<Book, string>("description");

    /// <summary>The logbook card it confirms (kept whole with it, so it reads the same after its settlement is gone).</summary>
    public QslCard? Card;

    /// <summary>Makes the card's item for a logbook card, with its title and words.</summary>
    public static QslCardBook Make(QslCard card)
    {
        var book = (QslCardBook)ThingMaker.MakeThing(RadioQslDefOf.PhobosRadio_QslCard);
        book.Card = card;
        book.Write();
        return book;
    }

    /// <summary>A card's words come from its contact, not the game's book namer.</summary>
    public override void GenerateBook(Pawn? author = null, long? fixedDate = null) => Write();

    void Write()
    {
        if (Card == null) return;
        string call = Card.Call.Length > 0 ? Card.Call : Card.Place;
        TitleRef(this) = "PhobosRadioQsl_ItemTitle".Translate(call.Named("CALL"), Card.Place.Named("PLACE")).Resolve();
        DescriptionRef(this) = "PhobosRadioQsl_ItemDescription".Translate(Card.Operator.Named("OPERATOR"), call.Named("CALL"),
            Card.Place.Named("PLACE"), (Card.Faction?.Name ?? "?").Named("FACTION"), Card.Report.Named("REPORT"), Card.Tiles.Named("TILES")).Resolve();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref Card, "PhobosQslCard");
        if (Scribe.mode == LoadSaveMode.PostLoadInit) Write();
    }
}

/// <summary>Reading a QSL card leaves a small memory (Radio 0.66.0): more for a contact far away. Figures authored.</summary>
public class BookOutcomeProperties_QslMemory : BookOutcomeProperties
{
    public ThoughtDef? thought;
    /// <summary>A card from this far or further (tiles) leaves the memory's second stage.</summary>
    public int farTiles = 50;

    public override System.Type DoerClass => typeof(ReadingOutcomeDoerQslMemory);
}

public class ReadingOutcomeDoerQslMemory : BookOutcomeDoer
{
    public new BookOutcomeProperties_QslMemory Props => (BookOutcomeProperties_QslMemory)props;

    public override bool DoesProvidesOutcome(Pawn reader) => Props.thought != null;

    public override void OnReadingTick(Pawn reader, float factor)
    {
        if (Props.thought is not { } def || reader.needs?.mood?.thoughts?.memories is not { } memories) return;
        if (memories.GetFirstMemoryOfDef(def) != null) return;
        bool far = Book is QslCardBook { Card: { } card } && card.Tiles >= Props.farTiles;
        memories.TryGainMemory((Thought_Memory)ThoughtMaker.MakeThought(def, far ? 1 : 0));
        if (Seen.Wants("radio.qsl-card-read", reader)) Seen.Note("radio.qsl-card-read", reader, Book.Label, far ? "far" : "near");
    }

    public override string GetBenefitsString(Pawn? reader = null) => "PhobosRadioQsl_ReadBenefit".Translate().Resolve();
}

[DefOf]
public static class RadioQslDefOf
{
    public static ThingDef PhobosRadio_QslCard = null!;

    static RadioQslDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(RadioQslDefOf));
}
