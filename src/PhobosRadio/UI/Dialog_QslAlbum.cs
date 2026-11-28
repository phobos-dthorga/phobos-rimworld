using System.Collections.Generic;
using System.Linq;
using Phobos.Radio.Services;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Radio.UI;

/// <summary>
/// The QSL album (Radio 0.71.0; docs/development/radio-qsl-cards.md, D's rest): every card the colony has, drawn as on the Log tab (a click
/// turns one over), sorted by date, faction or distance, and filtered to one kind. Presentation only.
/// <para>Cost: while open, the cards in view drawn each frame, each front painted once and kept (part B's).</para>
/// </summary>
public class Dialog_QslAlbum : Window
{
    enum Sort { Date, Faction, Distance }

    Sort sort = Sort.Date;
    Vector2 scroll;
    const float Gap = 8f;

    public override Vector2 InitialSize => new(3 * ITab_RadioLog.CardWidth + 4 * Gap + 40f, 720f);

    public Dialog_QslAlbum()
    {
        doCloseX = true;
        closeOnClickedOutside = true;
        absorbInputAroundWindow = true;
    }

    List<QslCard> Sorted() => sort switch
    {
        Sort.Faction => RadioQsl.Cards.OrderBy(c => c.Faction?.Name ?? "~").ThenBy(c => c.Tick).ToList(),
        Sort.Distance => RadioQsl.Cards.OrderByDescending(c => c.Tiles).ToList(),
        _ => RadioQsl.Cards.OrderByDescending(c => c.Tick).ToList(),
    };

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 34f), "PhobosRadioQsl_AlbumTitle".Translate(RadioQsl.Cards.Count.Named("COUNT")));
        Text.Font = GameFont.Small;
        float x = inRect.x;
        foreach (Sort each in new[] { Sort.Date, Sort.Faction, Sort.Distance })
        {
            var button = new Rect(x, inRect.y + 38f, 140f, 26f);
            if (Widgets.RadioButtonLabeled(button, ("PhobosRadioQsl_AlbumSort" + each).Translate(), sort == each)) sort = each;
            x += 150f;
        }
        List<QslCard> cards = Sorted();
        var outer = new Rect(inRect.x, inRect.y + 72f, inRect.width, inRect.height - 72f);
        int perRow = Mathf.Max(1, (int)((outer.width - 16f + Gap) / (ITab_RadioLog.CardWidth + Gap)));
        int rows = (cards.Count + perRow - 1) / perRow;
        var view = new Rect(0f, 0f, outer.width - 16f, rows * (ITab_RadioLog.CardHeight + Gap));
        Widgets.BeginScrollView(outer, ref scroll, view);
        for (int i = 0; i < cards.Count; i++)
        {
            var rect = new Rect(i % perRow * (ITab_RadioLog.CardWidth + Gap), i / perRow * (ITab_RadioLog.CardHeight + Gap), ITab_RadioLog.CardWidth,
                ITab_RadioLog.CardHeight);
            // Only the cards in view are drawn (their fronts are painted on first sight).
            if (rect.yMax < scroll.y || rect.y > scroll.y + outer.height) continue;
            ITab_RadioLog.DrawCard(rect, cards[i]);
        }
        Widgets.EndScrollView();
        if (Phobos.Radio.Seen.Wants("radio.qsl-album", null, sort.ToString())) Phobos.Radio.Seen.Note("radio.qsl-album", null, cards.Count + " cards", sort.ToString());
    }
}
