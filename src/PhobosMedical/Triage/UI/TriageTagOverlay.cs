using HarmonyLib;
using Phobos.Framework;
using Phobos.Triage.Rules;
using UnityEngine;
using Verse;

namespace Phobos.Triage.UI;

/// <summary>
/// Draws the triage tag's icon just right of a pawn's name on the map, for players without Useful Marks. With Useful Marks active, its
/// own row of marks by the name carries the tag instead (Mods/UsefulMarks, a provider through its public RegisterProvider), and this
/// draws nothing, so a tag never shows twice. A postfix on PawnUIOverlay.DrawPawnGUIOverlay, where the game draws the name; it changes
/// nothing the game draws. Presentation only: it reads the tag the planner already set.
/// </summary>
[HarmonyPatch(typeof(PawnUIOverlay), nameof(PawnUIOverlay.DrawPawnGUIOverlay))]
public static class Patch_PawnUIOverlay_TriageTag
{
    public const string UsefulMarksPackageId = "Andromeda.UsefulMarks";
    const float IconSize = 12f;
    const float Gap = 2f;

    public static void Postfix(Pawn ___pawn)
    {
        Pawn pawn = ___pawn;
        if (!pawn.Spawned || pawn.Map.fogGrid.IsFogged(pawn.Position) || OptionalMods.IsActive(UsefulMarksPackageId)) return;
        if (TriageTagStyle.TagOf(pawn) is not TriageCategory tag) return;
        Vector2 labelCentre = GenMapUI.LabelDrawPosFor(pawn, -0.6f);
        GameFont font = Text.Font;
        Text.Font = GameFont.Tiny;
        float nameWidth = Text.CalcSize(pawn.LabelShortCap).x;
        Text.Font = font;
        float padding = Prefs.DisableTinyText ? 6f : 4f;
        float height = Prefs.DisableTinyText ? 16f : 12f;
        var rect = new Rect(labelCentre.x + nameWidth / 2f + padding + Gap, labelCentre.y + (height - IconSize) / 2f, IconSize, IconSize);
        Color colour = GUI.color;
        GUI.color = TriageTagStyle.ColourOf(tag);
        GUI.DrawTexture(rect, TriageTagStyle.Icon);
        if (TriageTagStyle.Outline != null)
        {
            GUI.color = Color.white;
            GUI.DrawTexture(rect, TriageTagStyle.Outline);
        }
        GUI.color = colour;
    }
}
