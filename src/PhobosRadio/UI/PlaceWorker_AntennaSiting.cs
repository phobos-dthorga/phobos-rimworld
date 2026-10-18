using Phobos.Radio.Services;
using UnityEngine;
using Verse;

namespace Phobos.Radio.UI;

/// <summary>
/// The siting preview while placing an antenna or station: its height, the roof over the cell and the
/// local noise. It warns under thick roof and never blocks placement. No coverage ring: distance never
/// decides a contact on one map.
/// </summary>
public class PlaceWorker_AntennaSiting : PlaceWorker
{
    public override void DrawPlaceMouseAttachments(float curX, ref float curY, BuildableDef def, IntVec3 center, Rot4 rot)
    {
        var map = Find.CurrentMap;
        if (map == null || !center.InBounds(map) || def is not ThingDef thingDef) return;
        var rules = RadioRulesDef.Current;
        var height = Height(thingDef);
        var roof = center.GetRoof(map);
        var roofLoss = ObstructionService.RoofLossDb(map, center, rules);
        var noise = RadioNet.For(map).NoiseAt(center);

        Text.Font = GameFont.Small;
        curY = Line(curX, curY, "PhobosRadio_SitingHeight".Translate(height.ToString("0.#")), Color.white);
        if (roof == null)
        {
            curY = Line(curX, curY, "PhobosRadio_SitingOpenSky".Translate(), Color.white);
        }
        else
        {
            var colour = roof.isThickRoof ? Color.red : roof.isNatural ? Color.yellow : Color.white;
            curY = Line(curX, curY, "PhobosRadio_SitingRoof".Translate(roof.LabelCap, roofLoss.ToString("0")), colour);
        }
        curY = double.IsNegativeInfinity(noise)
            ? Line(curX, curY, "PhobosRadio_SitingQuiet".Translate(), Color.white)
            : Line(curX, curY, "PhobosRadio_SitingNoise".Translate(noise.ToString("0")), noise > -100 ? Color.yellow : Color.white);
    }

    private static float Height(ThingDef def)
    {
        var antenna = def.GetCompProperties<CompProperties_Antenna>();
        if (antenna != null) return antenna.heightMetres;
        var radio = def.GetCompProperties<CompProperties_Radio>();
        return radio?.antennaHeightMetres ?? 0f;
    }

    private static float Line(float x, float y, string text, Color colour)
    {
        var previous = GUI.color;
        GUI.color = colour;
        Widgets.Label(new Rect(x, y, 360f, 29f), text);
        GUI.color = previous;
        return y + 24f;
    }
}
