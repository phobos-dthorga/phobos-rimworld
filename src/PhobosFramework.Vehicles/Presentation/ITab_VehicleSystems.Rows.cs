using Phobos.Framework.Interface;
using UnityEngine;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The row every card of the Systems tab shares where a thing has a state in words (Framework 0.262.0): its name, the word in its
/// colour, and a figure against the right edge where it has one. First drawn by the electrical loads' card (0.261.0). Presentation only.
/// </summary>
public partial class ITab_VehicleSystems
{
    /// <summary>Where the word begins on a row whose names are short (a weapon, a reading), as a share of the card's width: where a
    /// reading's bar begins in the cards above.</summary>
    internal const float WordAfterShortName = 0.38f;

    /// <summary>
    /// One row with a word between its name and its figure: the name to <paramref name="nameShare"/> of the width, the word in the tiny
    /// font and its colour, running to the edge where the row has no figure and stopping short of the figure where it has one, and cut
    /// short rather than wrapped. Returns the row's rect; <paramref name="cut"/> says whether the word was cut, so the caller's tooltip
    /// can give it in full (<see cref="WordTip"/>). <paramref name="x"/> is the row's left edge, for a caller that draws outside a card
    /// (the Dashboard).
    /// </summary>
    internal static Rect WordRow(ref float y, float width, string label, string word, Color wordColour, string figure, float nameShare, out bool cut,
        float x = 0f)
    {
        float nameEnd = width * nameShare;
        Rect row = PhobosWidgets.FigureRow(ref y, x, width, label, figure, nameEnd: nameEnd);
        float wordWidth = width - nameEnd - 6f - (figure.Length > 0 ? PhobosWidgets.FigureColumnWidth : 0f);
        using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleLeft))
        {
            string shown = word.Truncate(wordWidth);
            cut = shown != word;
            Widgets.Label(new Rect(x + nameEnd, row.y, wordWidth, row.height), shown.Colorize(wordColour));
        }
        return row;
    }

    /// <summary>A word row's tooltip, built only under the mouse: the word in full where it was cut short, then whatever else the row has
    /// to say. Nothing where there is neither.</summary>
    internal static void WordTip(Rect row, string label, string word, bool cut, string? more)
    {
        if (!Mouse.IsOver(row)) return;
        string tip = cut ? "PhobosVehicleSystems_SysWordTip".Translate(label.Named("NAME"), word.Named("WORD")).Resolve() : "";
        if (!more.NullOrEmpty()) tip += (tip.Length > 0 ? "\n\n" : "") + more;
        if (tip.Length > 0) TooltipHandler.TipRegion(row, tip);
    }
}
