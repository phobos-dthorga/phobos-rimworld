using System.Collections.Generic;
using Phobos.Radio.Services;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Radio.UI;

/// <summary>
/// The station's HF band panel (Radio 0.62.0; the owner's cards 13 and 18 of 28 Nov 2026): VOACAP's forecast for each HF band the colony's
/// stations work, from <see cref="HfForecast"/>. <b>Dev mode only</b> until HF's skywave is built, so players never read a forecast for
/// reach that does not work yet. Presentation only.
/// </summary>
public class ITab_RadioHfBands : ITab
{
    public ITab_RadioHfBands()
    {
        size = new Vector2(460f, 420f);
        labelKey = "PhobosRadioHf_Tab";
    }

    public override bool IsVisible =>
        Prefs.DevMode && SelThing?.TryGetComp<CompRadio>() is { Props.isStation: true } radio && System.Linq.Enumerable.Any(radio.Bands, b => b.path == BandPath.Hf);

    protected override void FillTab()
    {
        if (SelThing is not { Spawned: true } station) return;
        Rect outer = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
        var lines = new List<string>
        {
            "PhobosRadioHf_DevOnly".Translate().Resolve(),
            "PhobosRadioHf_Sun".Translate(HfService.SunspotsNow().ToString("0").Named("SUNSPOTS"),
                RadioRulesDef.Current.hfSolarCycleYears.ToString("0.#").Named("YEARS")).Resolve(),
        };
        IReadOnlyList<HfForecast.Outlook> outlooks = HfForecast.Outlooks(station.Map);
        if (outlooks.Count == 0) lines.Add("PhobosRadioHf_NoTables".Translate().Resolve());
        foreach (HfForecast.Outlook outlook in outlooks)
        {
            lines.Add("");
            lines.Add("PhobosRadioHf_BandNoise".Translate(outlook.Band.LabelCap.Named("BAND"), outlook.NoiseDbm.ToString("0").Named("NOISE"),
                (outlook.BandwidthHz / 1000.0).ToString("0.#").Named("KHZ")).Resolve());
            foreach (HfForecast.Reach reach in outlook.ByDistance)
                lines.Add(reach.HasPath
                    ? "PhobosRadioHf_Reach".Translate(reach.Km.Named("KM"), (reach.OpenShare * 100).ToString("0").Named("PERCENT"),
                        reach.MufMHz.ToString("0.0").Named("MUF")).Resolve()
                    : "PhobosRadioHf_NoPath".Translate(reach.Km.Named("KM")).Resolve());
        }
        Text.Font = GameFont.Small;
        Widgets.Label(outer, string.Join("\n", lines));
        if (outlooks.Count > 0 && Phobos.Radio.Seen.Wants("radio.hf-band-panel", station))
            Phobos.Radio.Seen.Note("radio.hf-band-panel", station, outlooks.Count + " bands, sunspots " + HfService.SunspotsNow().ToString("0"));
    }
}

/// <summary>
/// The world map's HF band rose (Radio 0.62.0): around the colony, a petal along each of 16 bearings as far as each HF band is open on most
/// days at this hour, 40 m and 20 m in their own colours. Dev mode only, as the panel. Presentation only; the figures are
/// <see cref="HfForecast"/>'s.
/// <para>Cost: while dev mode is on and the colony is selected on the world map, 32 short arcs a frame; the forecast once a game hour.</para>
/// </summary>
public static class HfRose
{
    static readonly Dictionary<int, Material> colours = new();

    /// <summary>Each band's colour, by its place in the band list (agent's choice: warm for the lower band, cool for the higher).</summary>
    static Material ColourOf(int index)
    {
        if (colours.TryGetValue(index, out Material? held) && held != null) return held;
        Color c = index == 0 ? new Color(1f, 0.68f, 0.22f, 0.85f) : new Color(0.35f, 0.85f, 1f, 0.85f);
        return colours[index] = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.WorldOverlayTransparent, c, 3590);
    }

    public static void Draw(Map map)
    {
        if (!Prefs.DevMode) return;
        IReadOnlyList<HfForecast.Rose> roses = HfForecast.Roses(map);
        if (roses.Count == 0) return;
        Vector3 home = Find.WorldGrid.GetTileCenter(map.Tile);
        for (int band = 0; band < roses.Count; band++)
        {
            // Each band a little to one side, so both show along a bearing.
            float offset = (band - (roses.Count - 1) / 2f) * 4f;
            IReadOnlyList<int> farthest = roses[band].FarthestKm;
            for (int b = 0; b < farthest.Count; b++)
            {
                if (farthest[b] <= 0) continue;
                float bearing = b * 360f / farthest.Count + offset;
                WorldArcs.Arc(home, HfForecast.WorldPointAlong(map.Tile, bearing, farthest[b]), ColourOf(band), 0.6f);
            }
        }
        if (Phobos.Radio.Seen.Wants("radio.hf-band-rose", null)) Phobos.Radio.Seen.Note("radio.hf-band-rose", null, roses.Count + " bands");
    }
}
