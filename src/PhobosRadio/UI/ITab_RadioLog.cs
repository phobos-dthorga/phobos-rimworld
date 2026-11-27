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
    private const float CardWidth = 228f;
    private const float CardHeight = 132f;
    private const float Gap = 8f;

    private Vector2 scroll;
    private float viewHeight = 600f;
    private static int reachTick = -1;
    private static Map? reachMap;
    private static List<(Faction faction, string place)> reachable = new();

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
        foreach ((Faction faction, string place) in reachable)
            y = Line(view, y, "PhobosRadioLog_InReachLine".Translate(faction.Name.Named("FACTION"), place.Named("PLACE"),
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

    /// <summary>A QSL card, drawn in code: the faction's colour and icon, the place, the operator, the date, the report and the distance.</summary>
    private static void DrawCard(Rect rect, QslCard card)
    {
        Color colour = card.Faction?.Color ?? Color.gray;
        Widgets.DrawBoxSolid(rect, new Color(colour.r * 0.25f, colour.g * 0.25f, colour.b * 0.25f, 0.95f));
        GUI.color = colour;
        Widgets.DrawBox(rect, 2);
        var icon = new Rect(rect.x + 8f, rect.y + 8f, 40f, 40f);
        if (card.Faction?.def.FactionIcon is { } texture) GUI.DrawTexture(icon, texture);
        GUI.color = new Color(colour.r, colour.g, colour.b, 0.25f);
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.UpperRight;
        Widgets.Label(new Rect(rect.x, rect.y + 4f, rect.width - 8f, 30f), "QSL");
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
        Widgets.Label(new Rect(icon.xMax + 6f, rect.y + 8f, rect.width - icon.width - 22f, 30f), card.Place.Truncate(rect.width - icon.width - 22f));
        Text.Font = GameFont.Tiny;
        string date = GenDate.DateFullStringAt(card.Tick, Vector2.zero);
        string lines = "PhobosRadioQsl_CardText".Translate(card.Operator.Named("OPERATOR"), card.Report.Named("REPORT"), card.Tiles.Named("TILES"),
            date.Named("DATE"), (card.Faction?.Name ?? "?").Named("FACTION")).Resolve();
        Widgets.Label(new Rect(rect.x + 8f, rect.y + 52f, rect.width - 16f, rect.height - 56f), lines);
        Text.Font = GameFont.Small;
    }

    /// <summary>The settlements of factions that talk that this map reaches, one for each faction, worked out once a game hour.</summary>
    private static void Reachable(Map map)
    {
        int now = Find.TickManager.TicksGame;
        if (reachMap == map && reachTick >= 0 && now - reachTick < GenDate.TicksPerHour) return;
        reachMap = map;
        reachTick = now;
        reachable = new List<(Faction, string)>();
        foreach (Faction faction in Find.FactionManager.AllFactionsListForReading.Where(FactionDoctors.Talks))
        {
            if (FactionDoctors.ReachToTalk(map, map.Tile, () => FactionDoctors.StationEnds(map), faction) is { } link)
                reachable.Add((faction, link.Settlement.LabelCap));
        }
    }
}
