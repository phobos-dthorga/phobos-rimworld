using UnityEngine;
using Verse;

// Not "Phobos.Framework.UI": a namespace named UI would hide the game's Verse.UI class from every Phobos.Framework.* namespace.
namespace Phobos.Framework.Interface;

/// <summary>
/// The game's own work bar, the one over a pawn's job, for work that runs with no pawn's job to carry it (owner, 12 Nov 2026: "there needs to
/// be progress bars in many areas, where appropriate"), such as a tank filling or a load being tipped. Call it from a comp's PostDraw. The
/// game's bar is a mote that must be renewed every tick, which a comp that only runs every 250 ticks cannot do, so this draws the same bar
/// directly: the same size, colours and margin as <c>MoteProgressBar</c> and <c>SubEffecter_ProgressBar</c>, and, like it, only at the closest
/// zoom and never in screenshot mode. Presentation only.
/// </summary>
public static class MapProgressBar
{
    /// <summary>The game's bar: 0.68 by 0.12 cells (SubEffecter_ProgressBar).</summary>
    public const float Width = 0.68f;
    public const float Height = 0.12f;
    const float Margin = 0.12f;

    static Material? unfilled;
    static Material? filled;

    // The game's colours (MoteProgressBar), made on first use, on the main thread that draws.
    static Material Unfilled => unfilled ??= SolidColorMaterials.NewSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f, 0.65f), ShaderDatabase.MetaOverlay);
    static Material Filled => filled ??= SolidColorMaterials.NewSolidColorMaterial(new Color(0.9f, 0.85f, 0.2f, 0.65f), ShaderDatabase.MetaOverlay);

    /// <summary>Draws the bar over a thing, <paramref name="offsetZ"/> cells towards the top of the screen from its centre.</summary>
    public static void DrawOver(Thing thing, float fraction, float offsetZ = 0f)
    {
        if (!thing.Spawned) return;
        Vector3 centre = thing.DrawPos;
        centre.y = AltitudeLayer.MetaOverlays.AltitudeFor();
        centre.z += offsetZ;
        Draw(centre, fraction);
    }

    public static void Draw(Vector3 centre, float fraction)
    {
        if (Find.CameraDriver.CurrentZoom != CameraZoomRange.Closest || Find.ScreenshotModeHandler.Active) return;
        GenDraw.DrawFillableBar(new GenDraw.FillableBarRequest
        {
            center = centre,
            size = new Vector2(Width, Height),
            fillPercent = Mathf.Clamp01(fraction),
            filledMat = Filled,
            unfilledMat = Unfilled,
            margin = Margin,
        });
    }
}
