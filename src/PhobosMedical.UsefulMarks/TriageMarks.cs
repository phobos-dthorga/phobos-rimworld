using System.Collections.Generic;
using Phobos.Triage.Rules;
using Phobos.Triage.UI;
using UsefulMarks;
using Verse;

namespace Phobos.Triage.UsefulMarks;

/// <summary>
/// The triage tag as one of Useful Marks' marks by a pawn's name (owner, 7 Nov 2026: use that row). Useful Marks asks every registered
/// provider for extra marks whenever it works out a pawn's marks (PawnLabelCustomColors_WorldComponent.RegisterProvider and
/// IMarkerProvider, read from its 1.7 build). One mark per tag, built once, tinted with the tag's colour and outlined in white.
/// Presentation only: it reads the tag the planner set.
/// </summary>
[StaticConstructorOnStartup]
public static class TriageMarks
{
    static readonly Dictionary<TriageCategory, MarkerSettings> marks = new();

    static TriageMarks() => PawnLabelCustomColors_WorldComponent.RegisterProvider(new Provider());

    static MarkerSettings MarkFor(TriageCategory tag)
    {
        if (!marks.TryGetValue(tag, out MarkerSettings? mark))
        {
            mark = new MarkerSettings
            {
                icon = TriageTagStyle.Icon,
                iconAccent = TriageTagStyle.Outline,
                color = TriageTagStyle.ColourOf(tag),
                colorAccent = UnityEngine.Color.white,
                description = TriageTagStyle.LabelOf(tag),
                atSide = 1,
            };
            marks[tag] = mark;
        }
        return mark;
    }

    sealed class Provider : IMarkerProvider
    {
        public void Process(Pawn pawn, List<MarkerSettings> markers)
        {
            if (TriageTagStyle.TagOf(pawn) is TriageCategory tag) markers.Add(MarkFor(tag));
        }
    }
}
