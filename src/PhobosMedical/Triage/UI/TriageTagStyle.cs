using Phobos.Triage.Rules;
using Phobos.Triage.Services;
using UnityEngine;
using Verse;

namespace Phobos.Triage.UI;

/// <summary>
/// How a triage tag looks beside a pawn's name: one white icon tinted by the tag's colour, with a white outline so a black tag reads on
/// a dark map. The colours are the field triage colours as the owner chose them (7 Nov 2026): red immediate, yellow urgent, green
/// delayed, white minor, black expectant. Shared by the map overlay and the Useful Marks tie-in. Presentation only.
/// </summary>
[StaticConstructorOnStartup]
public static class TriageTagStyle
{
    public const string IconPath = "Phobos/Triage/UI/TriageTag";
    public const string OutlinePath = "Phobos/Triage/UI/TriageTagOutline";

    /// <summary>The tag's icon; a missing texture falls back to a plain white square of the game's, never the red X.</summary>
    public static readonly Texture2D Icon = ContentFinder<Texture2D>.Get(IconPath, false) ?? BaseContent.WhiteTex;
    /// <summary>The outline drawn over the tinted icon in white; null when missing, and then nothing is drawn.</summary>
    public static readonly Texture2D? Outline = ContentFinder<Texture2D>.Get(OutlinePath, false);

    static readonly Color Red = new(0.86f, 0.16f, 0.16f);
    static readonly Color Yellow = new(0.95f, 0.8f, 0.15f);
    static readonly Color Green = new(0.25f, 0.72f, 0.3f);
    static readonly Color White = new(0.95f, 0.95f, 0.95f);
    static readonly Color Black = new(0.08f, 0.08f, 0.08f);

    public static Color ColourOf(TriageCategory category) => category switch
    {
        TriageCategory.Immediate => Red,
        TriageCategory.Urgent => Yellow,
        TriageCategory.Delayed => Green,
        TriageCategory.Minor => White,
        _ => Black,
    };

    /// <summary>The tag's own word, as the inspect line names it.</summary>
    public static string LabelOf(TriageCategory category) => ("PhobosTriage_Category" + category).Translate();

    /// <summary>The pawn's tag, when it has one worth showing: a patient with a condition the triage classed.</summary>
    public static TriageCategory? TagOf(Pawn pawn)
    {
        TriageEntry? entry = MapComponent_PhobosTriage.For(pawn.Map)?.EntryOf(pawn);
        return entry?.Worst == null ? null : entry.Category;
    }
}
