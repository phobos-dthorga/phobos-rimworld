using System.Collections.Generic;
using Phobos.Radio.Services;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Radio.UI;

/// <summary>
/// The station's HF band panel (Radio 0.62.0; the owner's cards 13 and 18 of 28 Nov 2026): VOACAP's forecast for each HF band the colony's
/// stations work, from <see cref="HfForecast"/>: dev mode only in Radio 0.62.0, shown to every player since 0.64.0, when skywave came
/// into play. Presentation only.
/// </summary>
public class ITab_RadioHfBands : ITab
{
    public ITab_RadioHfBands()
    {
        size = new Vector2(460f, 420f);
        labelKey = "PhobosRadioHf_Tab";
    }

    public override bool IsVisible =>
        SelThing?.TryGetComp<CompRadio>() is { Props.isStation: true } radio && System.Linq.Enumerable.Any(radio.Bands, b => b.path == BandPath.Hf);

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
/// days at this hour, 40 m and 20 m in their own colours (shown to every player since Radio 0.64.0). Presentation only; the figures are
/// <see cref="HfForecast"/>'s.
/// <para>Cost: while dev mode is on and the colony is selected on the world map, 32 short arcs a frame; the forecast once a game hour.</para>
/// </summary>
public static class HfRose
{
    static readonly Dictionary<int, Material> colours = new();

    /// <summary>Each band's colour, by its place in the band list (agent's choice: warm for the lower band, cool for the higher).</summary>
    public static Material ColourFor(int index) => ColourOf(index);

    static Material ColourOf(int index)
    {
        if (colours.TryGetValue(index, out Material? held) && held != null) return held;
        Color c = index == 0 ? new Color(1f, 0.68f, 0.22f, 0.85f) : new Color(0.35f, 0.85f, 1f, 0.85f);
        return colours[index] = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.WorldOverlayTransparent, c, 3590);
    }

    public static void Draw(Map map)
    {
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

/// <summary>
/// HF's world-map pictures (Radio 0.71.0; docs/development/radio-hf.md, the owner's card 14: a later slice, off until picked): the grey
/// line, the day and night edge where HF often opens at dawn and dusk; and the skip-zone rings, for each HF band the distance at which the
/// sky first brings it back down this hour. Each its own setting, off by default. Presentation only.
/// <para>Cost: only while its setting is on and the colony is selected on the world map: the grey line 72 short arcs a frame, the rings
/// 48 a band; the rings' distances are the forecast's, worked out once a game hour.</para>
/// </summary>
public static class HfPictures
{
    const int GreyLinePieces = 72;
    const int RingPieces = 48;
    static Material? greyLine;

    public static void Draw(Map map)
    {
        if (RadioMod.Settings.hfGreyLine) GreyLine();
        if (RadioMod.Settings.hfSkipRings) SkipRings(map);
    }

    static void GreyLine()
    {
        greyLine ??= MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.WorldOverlayTransparent, new Color(0.55f, 0.55f, 0.65f, 0.7f), 3590);
        Vector3 sun = GenCelestial.CurSunPositionInWorldSpace().normalized;
        Vector3 u = Vector3.Cross(sun, Mathf.Abs(sun.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
        Vector3 v = Vector3.Cross(sun, u).normalized;
        float radius = Find.WorldGrid.GetTileCenter(Find.AnyPlayerHomeMap?.Tile ?? new RimWorld.Planet.PlanetTile(0, Find.WorldGrid.Surface)).magnitude;
        Vector3 previous = u * radius;
        for (int i = 1; i <= GreyLinePieces; i++)
        {
            float a = i * 2f * Mathf.PI / GreyLinePieces;
            Vector3 point = (u * Mathf.Cos(a) + v * Mathf.Sin(a)) * radius;
            WorldArcs.Arc(previous, point, greyLine, 1.2f);
            previous = point;
        }
        if (Phobos.Radio.Seen.Wants("radio.hf-grey-line", null)) Phobos.Radio.Seen.Note("radio.hf-grey-line");
    }

    static void SkipRings(Map map)
    {
        IReadOnlyList<HfForecast.Outlook> outlooks = HfForecast.Outlooks(map);
        for (int band = 0; band < outlooks.Count; band++)
        {
            int skip = 0;
            foreach (HfForecast.Reach reach in outlooks[band].ByDistance)
                if (reach.HasPath && reach.OpenShare >= HfForecast.OpenShare) { skip = reach.Km; break; }
            if (skip <= 0) continue;
            Vector3 previous = HfForecast.WorldPointAlong(map.Tile, 0f, skip);
            for (int i = 1; i <= RingPieces; i++)
            {
                Vector3 point = HfForecast.WorldPointAlong(map.Tile, i * 360f / RingPieces, skip);
                WorldArcs.Arc(previous, point, HfRose.ColourFor(band), 0.8f);
                previous = point;
            }
        }
        if (outlooks.Count > 0 && Phobos.Radio.Seen.Wants("radio.hf-skip-rings", null)) Phobos.Radio.Seen.Note("radio.hf-skip-rings", null, outlooks.Count + " bands");
    }
}
