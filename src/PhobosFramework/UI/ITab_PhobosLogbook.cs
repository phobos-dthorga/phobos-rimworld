using System.Linq;
using Phobos.Framework.Persistence;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Phobos.Framework.Interface;

/// <summary>
/// The logbook tab (Framework 0.181.0; docs/development/vehicle-emergent-play.md, EM23): any thing whose comp keeps a logbook shows it
/// here, newest first, with its date, as the game's own log tabs do. Presentation only: it reads the logbook and changes nothing.
/// </summary>
public class ITab_PhobosLogbook : ITab
{
    Vector2 scroll;

    public ITab_PhobosLogbook()
    {
        size = new Vector2(480f, 480f);
        labelKey = "PhobosLogbook_Tab";
    }

    Logbook? Book => SelThing is ThingWithComps thing ? thing.AllComps.OfType<ILogbookHolder>().FirstOrDefault()?.Logbook : null;

    public override bool IsVisible => Book != null;

    protected override void FillTab()
    {
        if (UiGuard.Resting(GuardKey)) return;
        try
        {
            var rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
            Draw(rect, Book, ref scroll);
        }
        catch (System.Exception e) { UiGuard.Report(GuardKey, e); }
    }

    const string GuardKey = "The logbook tab";

    /// <summary>Draws a logbook in a rect, newest first; shared with the world map's tabs.</summary>
    public static void Draw(Rect rect, Logbook? book, ref Vector2 scroll)
    {
        if (book == null || book.Entries.Count == 0)
        {
            using (new TextBlock(GameFont.Small, TextAnchor.UpperLeft)) Widgets.Label(rect, "PhobosLogbook_Empty".Translate());
            return;
        }
        Text.Font = GameFont.Small;
        float dateWidth = 110f;
        float textWidth = rect.width - dateWidth - 16f - 6f;
        float height = 0f;
        foreach (PhobosLogEntry entry in book.Entries) height += Mathf.Max(Text.CalcHeight(entry.Text(), textWidth), 24f) + 2f;
        var view = new Rect(0f, 0f, rect.width - 16f, height);
        Widgets.BeginScrollView(rect, ref scroll, view);
        float y = 0f;
        for (int i = book.Entries.Count - 1; i >= 0; i--)
        {
            PhobosLogEntry entry = book.Entries[i];
            string text = entry.Text();
            float rowHeight = Mathf.Max(Text.CalcHeight(text, textWidth), 24f);
            var row = new Rect(0f, y, view.width, rowHeight);
            if (i % 2 == 0) Widgets.DrawLightHighlight(row);
            using (new TextBlock(GameFont.Tiny, TextAnchor.UpperLeft, ColoredText.SubtleGrayColor)) Widgets.Label(new Rect(2f, y + 3f, dateWidth, rowHeight), entry.DateText());
            using (new TextBlock(GameFont.Small, TextAnchor.UpperLeft, entry.def?.bad == true ? ColorLibrary.RedReadable : Color.white))
                Widgets.Label(new Rect(dateWidth + 6f, y, textWidth, rowHeight), text);
            y += rowHeight + 2f;
        }
        Widgets.EndScrollView();
    }
}
