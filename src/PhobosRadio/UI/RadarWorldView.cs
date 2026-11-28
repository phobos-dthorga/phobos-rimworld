using Phobos.Radio.Services;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Phobos.Radio.UI;

/// <summary>
/// The radar on the world map (Radio 0.74.0, row RD8): while a colony with a working radar is selected, a ring for how far its best radar
/// sees a fighter at height, and a line to each contact it sees. Held raids are their own world objects
/// (<see cref="WorldObject_PhobosRadarContact"/>), drawn by the game.
/// <para>Cost: nothing unless the world map is shown with such a colony selected; then one ring and a line a contact a frame, and the
/// picture's contacts at most every 30 ticks.</para>
/// </summary>
public class WorldComponent_PhobosRadarView : WorldComponent
{
    private static Material? lineMat;

    public WorldComponent_PhobosRadarView(World world) : base(world) { }

    public override void WorldComponentUpdate()
    {
        if (!WorldRendererUtility.WorldSelected || Find.WorldSelector.FirstSelectedObject is not MapParent { HasMap: true } parent) return;
        if (!parent.Map.IsPlayerHome || parent.Map.GetComponent<MapComponent_PhobosRadar>() is not { } picture) return;
        var rules = RadioRulesDef.Current;
        double best = 0.0;
        foreach (CompRadar radar in picture.Radars)
            if (radar.Working) best = System.Math.Max(best, radar.SeesMetres(5.0, 3000.0));
        if (best <= 0.0) return;
        int tiles = Mathf.Max(1, Mathf.RoundToInt((float)(best / rules.radarMetresPerWorldTile)));
        GenDraw.DrawWorldRadiusRing(parent.Tile, tiles);
        lineMat ??= MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.WorldOverlayTransparent, new Color(0.35f, 1f, 0.45f, 0.6f));
        Vector3 home = Find.WorldGrid.GetTileCenter(parent.Tile);
        foreach (RadarContact contact in picture.Contacts())
        {
            if (contact.Key is WorldObject target && !target.Destroyed) GenDraw.DrawWorldLineBetween(home, target.DrawPos, lineMat, 0.5f);
        }
    }
}
