using System.Collections.Generic;
using Phobos.Radio.Physics;
using Phobos.Radio.Services;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Radio.UI;

/// <summary>
/// The waterfall (Radio 0.6.0, the radio visuals record's RV3), a tab on the radio terminal, as a software radio shows its band:
/// across is the band, down is time with the newest at the top, and brightness is strength at the antenna. Above it, the band as it
/// is now, with the line a voice signal needs.
/// <para>
/// <b>Honest where it can be:</b> each hand-held's streak is its real received level at the station (the forward direction of its
/// link), shown keyed for the mode's duty cycle; the speckle is the station's real noise floor, with random grain. <b>Each radio is
/// drawn on its own spot of the band to tell them apart: the radio has no channels yet</b>, and the tab says so.
/// </para>
/// <para>
/// <b>Cost:</b> only while the tab is open and the game runs: one new row a tenth of a second, a 256 by 128 picture sent to the
/// graphics card. The links are the net's, read, never worked out here. Untested assumption: a picture written this way works under
/// RimWorld 1.7's Unity.
/// </para>
/// </summary>
public class ITab_RadioWaterfall : ITab
{
    private const int Bins = 256;
    private const int Rows = 128;
    private const float SecondsPerRow = 0.1f;
    /// <summary>The span the colours cover above the noise floor, in dB (authored: a voice signal 12 dB up shows clearly).</summary>
    private const float SpanDb = 60f;
    private const float SpectrumHeight = 70f;
    private const int KeyCycleTicks = 600;
    private static readonly int[] SkirtDb = { 0, 3, 14, 28, 40, 50 };

    private Texture2D? picture;
    private Color32[] pixels = new Color32[Bins * Rows];
    private readonly float[] latest = new float[Bins];
    private readonly List<(string name, int bin, float receivedDbm, int seed)> radios = new();
    private Thing? showing;
    private float nextRow;
    private float floorDbm = -127f;
    private float neededDbm = -115f;

    public ITab_RadioWaterfall()
    {
        size = new Vector2(560f, 420f);
        labelKey = "PhobosRadio_TabWaterfall";
    }

    private CompRadio? Station => SelThing?.TryGetComp<CompRadio>() is { Props.isStation: true } radio ? radio : null;

    /// <summary>Always on a software radio's station; on any other only while the desk waterfall setting is on (Radio 0.6.0).</summary>
    public override bool IsVisible => Station is { } station && (station.Props.waterfallBuiltIn || RadioMod.Settings.deskWaterfall);

    protected override void FillTab()
    {
        var station = Station;
        if (station == null || !station.parent.Spawned) return;
        var rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
        if (showing != station.parent) Start(station);
        if (!Find.TickManager.Paused && RealTime.LastRealTime >= nextRow)
        {
            nextRow = RealTime.LastRealTime + SecondsPerRow;
            Read(station);
            AddRow();
        }

        Text.Font = GameFont.Small;
        Widgets.Label(new Rect(rect.x, rect.y, rect.width, 24f), "PhobosRadio_WaterfallTitle".Translate((station.Props.bands.Count > 0
            ? station.Props.bands[0].LabelCap.ToString() : string.Empty).Named("BAND")));
        var spectrum = new Rect(rect.x, rect.y + 26f, rect.width, SpectrumHeight);
        DrawSpectrum(spectrum);
        var fall = new Rect(rect.x, spectrum.yMax + 2f, rect.width, rect.height - SpectrumHeight - 26f - 64f);
        if (picture != null) GUI.DrawTexture(fall, picture, ScaleMode.StretchToFill);
        DrawNames(new Rect(rect.x, fall.yMax + 2f, rect.width, 20f));
        Text.Font = GameFont.Tiny;
        GUI.color = new Color(0.62f, 0.66f, 0.72f);
        Widgets.Label(new Rect(rect.x, fall.yMax + 24f, rect.width, 40f),
            radios.Count == 0 ? "PhobosRadio_WaterfallNobody".Translate() : "PhobosRadio_WaterfallNote".Translate());
        GUI.color = Color.white;
        Text.Font = GameFont.Small;
    }

    private void Start(CompRadio station)
    {
        showing = station.parent;
        picture ??= new Texture2D(Bins, Rows, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var dark = (Color32)RadioPalette.At(0f);
        for (var i = 0; i < pixels.Length; i++) pixels[i] = dark;
        nextRow = 0f;
    }

    /// <summary>The station's links as the net last worked them out: each hand-held's level arriving here, and the floor here.</summary>
    private void Read(CompRadio station)
    {
        radios.Clear();
        var pairs = RadioNet.For(station.parent.Map).PairsFor(station.parent);
        if (pairs.Count > 0)
        {
            var forward = pairs[0].Budget.Forward;
            floorDbm = (float)forward.Inputs.NoiseFloorDbm;
            neededDbm = floorDbm + (float)forward.Inputs.RequiredSnrDb;
        }
        else
        {
            var mode = station.Mode;
            floorDbm = (float)NoiseFloor.TotalDbm(mode.bandwidthHz, station.Props.noiseFigureDb, RadioNet.For(station.parent.Map).NoiseAt(station.parent.Position));
            neededDbm = floorDbm + mode.requiredSnrDb;
        }
        var ordered = new List<LinkEvaluation>(pairs);
        ordered.Sort((a, b) => a.A.Thing.thingIDNumber.CompareTo(b.A.Thing.thingIDNumber));
        for (var i = 0; i < ordered.Count; i++)
        {
            var link = ordered[i];
            var wearer = (link.A.Thing as Apparel)?.Wearer;
            var bin = Mathf.RoundToInt(Bins * (i + 0.5f) / ordered.Count);
            radios.Add((wearer?.LabelShort ?? link.A.Thing.LabelShort, bin, (float)link.Budget.Forward.ReceivedDbm, link.A.Thing.thingIDNumber * 37));
        }
    }

    /// <summary>One new row at the top: grain about the floor, and each keyed radio's streak with its skirts.</summary>
    private void AddRow()
    {
        System.Array.Copy(pixels, Bins, pixels, 0, Bins * (Rows - 1));
        for (var b = 0; b < Bins; b++)
        {
            // Noise power is exponentially spread; in dB its grain sits a few dB about the floor.
            latest[b] = floorDbm + 10f * Mathf.Log10(-Mathf.Log(1f - UnityEngine.Random.Range(0.001f, 0.999f)) + 0.05f);
        }
        var mode = Station?.Mode;
        var keyedTicks = KeyCycleTicks * Mathf.Clamp01(mode?.dutyCycle ?? 0.2f);
        foreach (var (_, bin, received, seed) in radios)
        {
            if ((Find.TickManager.TicksGame + seed) % KeyCycleTicks >= keyedTicks) continue;
            for (var offset = -(SkirtDb.Length - 1); offset < SkirtDb.Length; offset++)
            {
                var at = bin + offset;
                if (at < 0 || at >= Bins) continue;
                var level = received - SkirtDb[Mathf.Abs(offset)] + UnityEngine.Random.Range(-1f, 1f);
                if (level > latest[at]) latest[at] = level;
            }
        }
        var top = Bins * (Rows - 1);
        for (var b = 0; b < Bins; b++) pixels[top + b] = RadioPalette.At((latest[b] - (floorDbm - 5f)) / SpanDb);
        picture!.SetPixels32(pixels);
        picture.Apply(false);
    }

    private void DrawSpectrum(Rect rect)
    {
        Widgets.DrawBoxSolid(rect, new Color(0.03f, 0.04f, 0.06f));
        float Y(float dbm) => rect.yMax - Mathf.Clamp01((dbm - (floorDbm - 5f)) / SpanDb) * rect.height;
        var step = rect.width / Bins;
        for (var b = 1; b < Bins; b++)
        {
            Widgets.DrawLine(new Vector2(rect.x + (b - 1) * step, Y(latest[b - 1])), new Vector2(rect.x + b * step, Y(latest[b])),
                RadioPalette.At(0.85f), 1f);
        }
        var needed = Y(neededDbm);
        for (var x = rect.x; x < rect.xMax; x += 12f)
        {
            Widgets.DrawLine(new Vector2(x, needed), new Vector2(Mathf.Min(x + 6f, rect.xMax), needed), new Color(0.44f, 0.83f, 0.88f), 1f);
        }
        if (RadioMod.Settings.displayMode != DisplayMode.Plain)
        {
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperRight;
            GUI.color = new Color(0.44f, 0.83f, 0.88f);
            Widgets.Label(new Rect(rect.x, needed - 16f, rect.width - 4f, 16f), "PhobosRadio_WaterfallNeeded".Translate(neededDbm.ToString("0").Named("LEVEL")));
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }
    }

    private void DrawNames(Rect rect)
    {
        Text.Font = GameFont.Tiny;
        Text.Anchor = TextAnchor.UpperCenter;
        var keyedTicks = KeyCycleTicks * Mathf.Clamp01(Station?.Mode.dutyCycle ?? 0.2f);
        foreach (var (name, bin, _, seed) in radios)
        {
            var keyed = (Find.TickManager.TicksGame + seed) % KeyCycleTicks < keyedTicks;
            GUI.color = keyed ? Color.white : new Color(0.62f, 0.66f, 0.72f);
            var x = rect.x + rect.width * bin / Bins;
            Widgets.Label(new Rect(x - 50f, rect.y, 100f, rect.height), name);
        }
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;
    }
}
