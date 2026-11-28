using System.Collections.Generic;
using System.Linq;
using Phobos.Radio.Services;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Radio.UI;

/// <summary>
/// The station's Log tab (Radio 0.38.0; docs/development/radio-logbook-qsl-sstv-morse.md): the awards, the QSL cards drawn in code, the
/// colony's radio friends, the settlements in reach, and the log, newest first. It costs nothing until it is open; the settlements in reach
/// are worked out once an hour while it is.
/// </summary>
public class ITab_RadioLog : ITab
{
    // Radio 0.58.0: room for the callsign, the locator, the time and the note.
    internal const float CardWidth = 268f;
    internal const float CardHeight = 184f;
    private const float Gap = 8f;

    private Vector2 scroll;
    private float viewHeight = 600f;
    private static int reachTick = -1;
    private static Map? reachMap;
    private static List<(Faction faction, string place, string area)> reachable = new();

    public ITab_RadioLog()
    {
        size = new Vector2(500f, 520f);
        labelKey = "PhobosRadioLog_Tab";
    }

    public override bool IsVisible => SelThing?.TryGetComp<CompRadio>()?.Props.isStation == true;

    protected override void FillTab()
    {
        if (SelThing is not { Spawned: true } station) return;
        var outer = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
        var view = new Rect(0f, 0f, outer.width - 16f, viewHeight);
        Widgets.BeginScrollView(outer, ref scroll, view);
        float y = 0f;
        y = Section(view, y, "PhobosRadioLog_Awards".Translate());
        if (RadioQsl.Awards.Count == 0) y = Line(view, y, "PhobosRadioLog_NoAwards".Translate(), dim: true);
        foreach (QslAward award in RadioQsl.Awards) y = Certificate(view, y, award);

        y = Section(view, y, "PhobosRadioLog_Cards".Translate(RadioQsl.Cards.Count.Named("COUNT")));
        // Radio 0.71.0: the album, every card sorted as the player likes.
        if (RadioQsl.Cards.Count > 0 && Widgets.ButtonText(new Rect(view.width - 140f, y - 26f, 140f, 24f), "PhobosRadioQsl_OpenAlbum".Translate()))
            Find.WindowStack.Add(new Dialog_QslAlbum());
        if (RadioQsl.Cards.Count == 0) y = Line(view, y, "PhobosRadioLog_NoCards".Translate(), dim: true);
        int perRow = Mathf.Max(1, (int)((view.width + Gap) / (CardWidth + Gap)));
        for (int i = 0; i < RadioQsl.Cards.Count; i++)
        {
            int col = i % perRow;
            if (i > 0 && col == 0) y += CardHeight + Gap;
            DrawCard(new Rect(col * (CardWidth + Gap), y, CardWidth, CardHeight), RadioQsl.Cards[i]);
        }
        if (RadioQsl.Cards.Count > 0) y += CardHeight + Gap;

        y = Section(view, y, "PhobosRadioLog_Friends".Translate());
        bool anyFriend = false;
        foreach (Pawn colonist in station.Map.mapPawns.FreeColonists)
        {
            if (RadioFriends.Of(colonist) is not { Friend: { } friend } held) continue;
            anyFriend = true;
            string stage = ("PhobosRadioLog_FriendStage" + RadioFriends.Stage(held.Hours)).Translate();
            y = Line(view, y, "PhobosRadioLog_FriendLine".Translate(colonist.LabelShort.Named("COLONIST"), friend.LabelShort.Named("FRIEND"),
                (friend.Faction?.Name ?? "?").Named("FACTION"), stage.Named("STAGE"), held.Hours.ToString("0").Named("HOURS")));
        }
        if (!anyFriend) y = Line(view, y, "PhobosRadioLog_NoFriends".Translate(), dim: true);

        y = Section(view, y, "PhobosRadioLog_InReach".Translate());
        Reachable(station.Map);
        if (reachable.Count == 0) y = Line(view, y, "PhobosRadioLog_NoneInReach".Translate(), dim: true);
        foreach ((Faction faction, string place, string area) in reachable)
            y = Line(view, y, "PhobosRadioLog_InReachLine".Translate(faction.Name.Named("FACTION"), place.Named("PLACE"), AreaText(area).Named("AREA"),
                ("PhobosRadioLog_Standing" + FactionDoctors.StandingOf(faction)).Translate().Named("STANDING")));

        y = Section(view, y, "PhobosRadioLog_Entries".Translate());
        if (RadioLog.Entries.Count == 0) y = Line(view, y, "PhobosRadioLog_NoEntries".Translate(), dim: true);
        for (int i = RadioLog.Entries.Count - 1; i >= 0; i--)
        {
            RadioLogEntry entry = RadioLog.Entries[i];
            string when = GenDate.DateFullStringAt(GenDate.TickGameToAbs(entry.Tick), Find.WorldGrid.LongLatOf(station.Map.Tile));
            y = Line(view, y, when + ": " + entry.Text);
        }
        Widgets.EndScrollView();
        viewHeight = y + 10f;
    }

    private static float Section(Rect view, float y, string title)
    {
        y += 6f;
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(0f, y, view.width, 30f), title);
        Text.Font = GameFont.Small;
        Widgets.DrawLineHorizontal(0f, y + 30f, view.width);
        return y + 36f;
    }

    private static float Line(Rect view, float y, string text, bool dim = false)
    {
        float height = Text.CalcHeight(text, view.width);
        GUI.color = dim ? Color.gray : Color.white;
        Widgets.Label(new Rect(0f, y, view.width, height), text);
        GUI.color = Color.white;
        return y + height + 2f;
    }

    private static float Certificate(Rect view, float y, QslAward award)
    {
        var rect = new Rect(0f, y, view.width, 30f);
        Widgets.DrawBoxSolid(rect, new Color(0.35f, 0.28f, 0.12f, 0.6f));
        Widgets.DrawBox(rect);
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(rect, RadioQsl.AwardLabel(award));
        Text.Anchor = TextAnchor.UpperLeft;
        return y + 34f;
    }

    /// <summary>Cards turned over to their back this session (Radio 0.59.0); a card shows its front until clicked.</summary>
    private static readonly HashSet<string> turned = new();

    /// <summary>A QSL card (Radio 0.59.0, part B): its front, the station's home painted with the callsign across it, or its back, what the
    /// contact was, on the stock of the sender's tech level; a click turns it over.</summary>
    internal static void DrawCard(Rect rect, QslCard card)
    {
        card.FillIn();
        string key = (card.Settlement?.ID ?? -1) + ":" + card.Tick;
        bool back = turned.Contains(key);
        Stock(card, out Color paper, out Color ink, out Color edge, out int edgeWidth);
        if (back) DrawBack(rect, card, paper, ink);
        else DrawFront(rect, card);
        GUI.color = edge;
        Widgets.DrawBox(rect, edgeWidth);
        GUI.color = Color.white;
        TooltipHandler.TipRegion(rect, "PhobosRadioQsl_TurnOver".Translate());
        if (Widgets.ButtonInvisible(rect))
        {
            if (!turned.Add(key)) turned.Remove(key);
            Verse.Sound.SoundStarter.PlayOneShotOnCamera(SoundDefOf.Click);
        }
    }

    /// <summary>The stock by the sender's tech level (Phobos.Radio.Physics.QslArtRule.Stock); a gilt edge for a faction with royal titles.</summary>
    private static void Stock(QslCard card, out Color paper, out Color ink, out Color edge, out int edgeWidth)
    {
        Color colour = card.Faction?.Color ?? Color.gray;
        edgeWidth = 2;
        switch (Phobos.Radio.Physics.QslArtRule.Stock((int)(card.Faction?.def.techLevel ?? TechLevel.Industrial)))
        {
            case Phobos.Radio.Physics.QslStock.Hide:
                paper = new Color(0.36f, 0.25f, 0.16f); ink = new Color(0.95f, 0.90f, 0.80f); edge = new Color(0.22f, 0.14f, 0.08f); edgeWidth = 3;
                break;
            case Phobos.Radio.Physics.QslStock.Parchment:
                paper = new Color(0.86f, 0.79f, 0.62f); ink = new Color(0.20f, 0.15f, 0.10f); edge = new Color(0.55f, 0.42f, 0.25f);
                break;
            case Phobos.Radio.Physics.QslStock.Glossy:
                paper = new Color(0.08f, 0.12f, 0.16f); ink = new Color(0.85f, 0.95f, 1f); edge = new Color(0.20f, 0.80f, 0.90f);
                break;
            default:
                paper = new Color(0.93f, 0.93f, 0.90f); ink = new Color(0.12f, 0.12f, 0.14f); edge = colour;
                break;
        }
        if (card.Faction?.def.HasRoyalTitles == true)
        {
            edge = new Color(0.86f, 0.68f, 0.20f);
            edgeWidth = 3;
        }
    }

    private static void DrawFront(Rect rect, QslCard card)
    {
        GUI.DrawTexture(rect, QslCardArt.Front(card), ScaleMode.ScaleAndCrop);
        Text.Font = GameFont.Medium;
        string call = card.Call.Length > 0 ? card.Call : card.Place;
        Shadowed(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 60f, 30f), call, Color.white);
        Text.Font = GameFont.Tiny;
        Shadowed(new Rect(rect.x + 10f, rect.y + 36f, rect.width - 60f, 18f), card.Place, Color.white);
        if (card.Grid.Length > 0) Shadowed(new Rect(rect.x + 10f, rect.yMax - 22f, 120f, 18f), card.Grid, Color.white);
        // The faction's icon as a stamp, and with Ideology its primary ideology's symbol beside it.
        var stamp = new Rect(rect.xMax - 46f, rect.y + 8f, 36f, 36f);
        Widgets.DrawBoxSolid(stamp, new Color(0.95f, 0.93f, 0.86f, 0.9f));
        if (card.Faction?.def.FactionIcon is { } icon)
        {
            GUI.color = card.Faction.Color;
            GUI.DrawTexture(stamp.ContractedBy(4f), icon);
        }
        if (ModsConfig.IdeologyActive && card.Faction?.ideos?.PrimaryIdeo is { } ideo && ideo.Icon != null)
        {
            GUI.color = ideo.Color;
            GUI.DrawTexture(new Rect(stamp.x - 30f, stamp.y + 8f, 24f, 24f), ideo.Icon);
        }
        GUI.color = Color.white;
        Text.Font = GameFont.Small;
        if (card.Call.Length > 0 && Seen.Wants("radio.qsl-card-front", variant: Phobos.Radio.Physics.QslArtRule.Stock((int)(card.Faction?.def.techLevel ?? TechLevel.Industrial)).ToString()))
            Seen.Note("radio.qsl-card-front", null, card.Call + ", " + card.Place,
                Phobos.Radio.Physics.QslArtRule.Stock((int)(card.Faction?.def.techLevel ?? TechLevel.Industrial)).ToString());
    }

    private static void Shadowed(Rect rect, string text, Color colour)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        Widgets.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text.Truncate(rect.width));
        GUI.color = colour;
        Widgets.Label(rect, text.Truncate(rect.width));
        GUI.color = Color.white;
    }

    /// <summary>The back: the callsign, then what the contact was, in the stock's ink, and the operator's signature.</summary>
    private static void DrawBack(Rect rect, QslCard card, Color paper, Color ink)
    {
        Widgets.DrawBoxSolid(rect, paper);
        Color colour = card.Faction?.Color ?? Color.gray;
        Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 6f, rect.height), colour);
        GUI.color = ink;
        Text.Font = GameFont.Small;
        Widgets.Label(new Rect(rect.x + 14f, rect.y + 6f, rect.width - 22f, 24f), (card.Call.Length > 0 ? card.Call + "  " + card.Place : card.Place).Truncate(rect.width - 22f));
        // Radio 0.65.0: the named place the callsign's prefix comes from, under it.
        if (card.Area.Length > 0)
        {
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x + 14f, rect.y + 24f, rect.width - 22f, 16f), AreaText(card.Area).Truncate(rect.width - 22f));
            if (Seen.Wants("radio.qsl-card-area")) Seen.Note("radio.qsl-card-area", null, card.Call + ", " + card.Area);
        }
        Text.Font = GameFont.Tiny;
        // Radio 0.57.1: dated where the card was sent from, as the log dates its lines where the station is (the owner's game, 28 Nov
        // 2026: a card dated at longitude 0 read a day before the contact); the colony's home if the settlement is gone.
        RimWorld.Planet.PlanetTile? at = card.Settlement?.Tile ?? Find.AnyPlayerHomeMap?.Tile;
        string date = GenDate.DateFullStringAt(card.Tick, at is { Valid: true } tile ? Find.WorldGrid.LongLatOf(tile) : Vector2.zero);
        // Radio 0.58.0: the time in UTC as the operators log it (longitude 0, the game's own day), the locator and a note for what the
        // contact was.
        float utc = GenDate.HourFloat(card.Tick, 0f);
        string time = ((int)utc).ToString("00") + ":" + ((int)(utc % 1f * 60f)).ToString("00");
        string note = QslNote(card);
        // Radio 0.62.0: the band and mode, the relays, and the player's own callsign when one is set, on every card, old ones too.
        string worked = card.Band != null && card.Mode != null
            ? "PhobosRadioQsl_WorkedOn".Translate(card.Band.LabelCap.Named("BAND"), card.Mode.label.Named("MODE")).Resolve()
            : "";
        if (card.Relays == 1) worked += "PhobosRadioQsl_ViaOne".Translate().Resolve();
        else if (card.Relays > 1) worked += "PhobosRadioQsl_ViaMany".Translate(card.Relays.Named("COUNT")).Resolve();
        string own = Phobos.Radio.Physics.QslRule.CleanCallsign(RadioMod.Settings.ownCallsign);
        string ourCall = own.Length > 0 ? own : card.OurCall.Length > 0 ? card.OurCall : "?";
        string lines = "PhobosRadioQsl_CardText".Translate(card.Operator.Named("OPERATOR"), card.Report.Named("REPORT"), card.Tiles.Named("TILES"),
            date.Named("DATE"), (card.Faction?.Name ?? "?").Named("FACTION"), ourCall.Named("OURCALL"),
            time.Named("UTC"), (card.Grid.Length > 0 ? card.Grid : "?").Named("GRID"), note.Named("NOTE"), worked.Named("WORKED")).Resolve();
        if (card.Band != null && Seen.Wants("radio.qsl-card-band", variant: card.Mode?.defName))
            Seen.Note("radio.qsl-card-band", null, card.Call + ": " + worked + card.Report, card.Mode?.defName);
        GUI.color = ink;
        Widgets.Label(new Rect(rect.x + 14f, rect.y + 30f, rect.width - 22f, rect.height - 34f), lines);
        Text.Font = GameFont.Small;
        // The operator's signature, a pen squiggle seeded by the name (QslArtRule.Signature).
        var box = new Rect(rect.xMax - 96f, rect.yMax - 30f, 86f, 22f);
        List<(float X, float Y)> pen = Phobos.Radio.Physics.QslArtRule.Signature(card.Operator);
        var inkLine = new Color(ink.r, ink.g, ink.b, 0.85f);
        for (int i = 1; i < pen.Count; i++)
            Widgets.DrawLine(new Vector2(box.x + pen[i - 1].X * box.width, box.yMax - pen[i - 1].Y * box.height),
                new Vector2(box.x + pen[i].X * box.width, box.yMax - pen[i].Y * box.height), inkLine, 1.5f);
        GUI.color = Color.white;
        if (card.Call.Length > 0 && Seen.Wants("radio.qsl-card-drawn", variant: card.Kind.ToString()))
            Seen.Note("radio.qsl-card-drawn", null, card.Call + " " + card.Grid, card.Kind.ToString());
    }

    /// <summary>Notes a card may carry for its kind of contact (Radio 0.58.0): authored lines in the language files, three of each.</summary>
    private const int NotesPerKind = 3;

    /// <summary>The card's note: one of its kind's lines, the same each time for the same card.</summary>
    private static string QslNote(QslCard card) =>
        ("PhobosRadioQsl_Note" + card.Kind + Phobos.Radio.Physics.QslRule.Pick((card.Settlement?.ID ?? 0) * 31 + card.Tick, NotesPerKind)).Translate().Resolve();

    /// <summary>The settlements of factions that talk that this map reaches, one for each faction, worked out once a game hour.</summary>
    private static void Reachable(Map map)
    {
        int now = Find.TickManager.TicksGame;
        if (reachMap == map && reachTick >= 0 && now - reachTick < GenDate.TicksPerHour) return;
        reachMap = map;
        reachTick = now;
        reachable = new List<(Faction, string, string)>();
        // Radio 0.54.0: the same list the world map's contact arcs draw; since 0.65.0 with the named place its prefix comes from.
        foreach (FactionDoctorLink link in RadioDiplomacy.InReach(map))
            reachable.Add((link.Faction, link.Settlement.LabelCap, QslCalls.AreaOf(link.Settlement.Tile)));
    }

    /// <summary>A place's area as the logbook and the card name it (Radio 0.65.0), or nothing where none is near.</summary>
    private static string AreaText(string area) =>
        area.Length > 0 ? "PhobosRadioQsl_Area".Translate(area.Named("AREA")).Resolve() : "";
}
