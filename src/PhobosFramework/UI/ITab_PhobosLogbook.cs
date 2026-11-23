using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Persistence;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Phobos.Framework.Interface;

/// <summary>
/// The logbook tab (Framework 0.181.0; docs/development/vehicle-emergent-play.md, EM23): any thing whose comp keeps a logbook shows it
/// here, newest first. Since Framework 0.267.0 (the owner's choice of 23 Nov 2026, after the Systems tab's cards became rows) each day is
/// written once as a heading, and each entry under it shows its hour beside its words, where every row repeated the date. Presentation
/// only: it reads the logbook and changes nothing.
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

    /// <summary>The hour's column, and the gap after it.</summary>
    const float HourWidth = 40f;
    const float HourGap = 6f;
    /// <summary>How far an entry sits in under its day's heading.</summary>
    const float EntryIndent = 8f;
    const float DayHeight = 26f;
    const float EntryLeast = 22f;
    /// <summary>The space left above every day's heading but the first.</summary>
    const float DayGap = 6f;

    /// <summary>One line of the laid-out logbook: a day's heading, or an entry with its hour.</summary>
    sealed class Line
    {
        public bool Day;
        public string Text = "";
        public string Hour = "";
        public bool Bad;
        public float Height;
    }

    /// <summary>The last book laid out, kept until it changes or the width does, so nothing is measured or translated every frame. Two
    /// open tabs on different books (the map's and the world's) lay out in turn; each is a list rebuild, not a frame's work.</summary>
    static Logbook? laidOutBook;
    static int laidOutVersion = -1;
    static float laidOutWidth = -1f;
    static float laidOutHeight;
    static readonly List<Line> lines = new();

    /// <summary>Draws a logbook in a rect, newest first; shared with the world map's tabs.</summary>
    public static void Draw(Rect rect, Logbook? book, ref Vector2 scroll)
    {
        if (book == null || book.Entries.Count == 0)
        {
            using (new TextBlock(GameFont.Small, TextAnchor.UpperLeft)) Widgets.Label(rect, "PhobosLogbook_Empty".Translate());
            return;
        }
        float width = rect.width - 16f;
        float textWidth = width - EntryIndent - HourWidth - HourGap;
        if (book != laidOutBook || book.Version != laidOutVersion || width != laidOutWidth) LayOut(book, width, textWidth);

        var view = new Rect(0f, 0f, width, laidOutHeight);
        Widgets.BeginScrollView(rect, ref scroll, view);
        float y = 0f;
        bool shaded = false;
        for (int i = 0; i < lines.Count; i++)
        {
            Line line = lines[i];
            if (line.Day)
            {
                if (i > 0) y += DayGap;
                var heading = new Rect(0f, y, width, DayHeight);
                Widgets.DrawLightHighlight(heading);
                using (new TextBlock(GameFont.Small, TextAnchor.MiddleLeft)) Widgets.Label(new Rect(6f, y, width - 12f, DayHeight), line.Text);
                y += DayHeight;
                shaded = false;
                continue;
            }
            var row = new Rect(EntryIndent, y, width - EntryIndent, line.Height);
            if (shaded) Widgets.DrawAltRect(row);
            shaded = !shaded;
            using (new TextBlock(GameFont.Tiny, TextAnchor.UpperRight, ColoredText.SubtleGrayColor))
            {
                Widgets.Label(new Rect(EntryIndent, y + 3f, HourWidth, line.Height), line.Hour);
            }
            using (new TextBlock(GameFont.Small, TextAnchor.UpperLeft, line.Bad ? ColorLibrary.RedReadable : Color.white))
            {
                Widgets.Label(new Rect(EntryIndent + HourWidth + HourGap, y, textWidth, line.Height), line.Text);
            }
            y += line.Height + 2f;
        }
        Widgets.EndScrollView();
    }

    /// <summary>The book as lines, newest first: a heading wherever the day changes (as it was where each entry was written), then its
    /// entries with their hours.</summary>
    static void LayOut(Logbook book, float width, float textWidth)
    {
        laidOutBook = book;
        laidOutVersion = book.Version;
        laidOutWidth = width;
        lines.Clear();
        float height = 0f;
        string? day = null;
        using (new TextBlock(GameFont.Small))
        {
            for (int i = book.Entries.Count - 1; i >= 0; i--)
            {
                PhobosLogEntry entry = book.Entries[i];
                string entryDay = entry.DayText();
                if (entryDay != day)
                {
                    if (day != null) height += DayGap;
                    day = entryDay;
                    lines.Add(new Line { Day = true, Text = entryDay, Height = DayHeight });
                    height += DayHeight;
                }
                string text = entry.Text();
                float rowHeight = Mathf.Max(Text.CalcHeight(text, textWidth), EntryLeast);
                lines.Add(new Line { Text = text, Hour = entry.HourText(), Bad = entry.def?.bad == true, Height = rowHeight });
                height += rowHeight + 2f;
            }
        }
        laidOutHeight = height;
        Seen.Note("framework.logbook.day-headings");
    }
}
