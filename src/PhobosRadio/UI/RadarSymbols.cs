using System.Collections.Generic;
using Phobos.Radio.Services;
using UnityEngine;
using Verse;

namespace Phobos.Radio.UI;

/// <summary>
/// The radar's symbols, drawn in code (Radio 0.74.0): a scope icon for its buttons (a ring with a sweep), and a diamond blip a colour for
/// each identity, for the world map and the scope. A radar contact is a symbol, not a picture, so these are the real thing, not stand-ins.
/// Made once, on the main thread, when first asked.
/// </summary>
[StaticConstructorOnStartup]
public static class RadarSymbols
{
    private const int IconSize = 64;
    private const int BlipSize = 32;

    private static Texture2D? scopeIcon;
    private static readonly Dictionary<RadarIdentity, Texture2D> blips = new();
    private static readonly Dictionary<RadarIdentity, Material> blipMats = new();

    /// <summary>The scope's phosphor green.</summary>
    public static readonly Color Phosphor = new(0.35f, 1f, 0.45f);

    public static Color ColourOf(RadarIdentity identity) => identity switch
    {
        RadarIdentity.Friend => new Color(0.35f, 0.8f, 1f),
        RadarIdentity.Neutral => new Color(0.9f, 0.9f, 0.9f),
        RadarIdentity.Hostile => new Color(1f, 0.3f, 0.25f),
        _ => new Color(1f, 0.85f, 0.2f),
    };

    /// <summary>The scope button's icon: a dark disc, two range rings, a sweep line and its fading wedge.</summary>
    public static Texture2D ScopeIcon => scopeIcon ??= MakeScopeIcon();

    private static Texture2D? scopeDisc;

    /// <summary>The scope's dark face, a disc a little brighter towards its rim, drawn once.</summary>
    public static Texture2D ScopeDisc => scopeDisc ??= MakeDisc();

    public static Texture2D Blip(RadarIdentity identity)
    {
        if (!blips.TryGetValue(identity, out var tex)) blips[identity] = tex = MakeBlip(ColourOf(identity));
        return tex;
    }

    public static Material BlipMaterial(RadarIdentity identity)
    {
        if (!blipMats.TryGetValue(identity, out var mat))
            blipMats[identity] = mat = MaterialPool.MatFrom(Blip(identity), ShaderDatabase.WorldOverlayTransparentLit, Color.white);
        return mat;
    }

    private static Texture2D MakeScopeIcon()
    {
        var tex = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, name = "PhobosRadarScopeIcon" };
        var pixels = new Color32[IconSize * IconSize];
        float centre = (IconSize - 1) / 2f;
        float radius = IconSize / 2f - 2f;
        const float sweepDegrees = 40f;
        for (int y = 0; y < IconSize; y++)
        {
            for (int x = 0; x < IconSize; x++)
            {
                float dx = x - centre, dy = y - centre;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                Color c = Color.clear;
                if (r <= radius)
                {
                    c = new Color(0.04f, 0.12f, 0.06f, 1f);
                    float angle = (Mathf.Atan2(dx, dy) * Mathf.Rad2Deg + 360f) % 360f;
                    // The wedge behind the sweep line, fading back from it (the line at 40°).
                    if (angle <= sweepDegrees) c = Color.Lerp(c, Phosphor, 0.6f * angle / sweepDegrees);
                    if (Mathf.Abs(r - radius * 0.5f) < 1.2f || Mathf.Abs(r - radius) < 1.6f) c = Color.Lerp(c, Phosphor, 0.8f);
                    if (Mathf.Abs(angle - sweepDegrees) < 3f && r > 1f) c = Phosphor;
                }
                pixels[y * IconSize + x] = c;
            }
        }
        // Two blips.
        Dot(pixels, IconSize, (int)(centre + radius * 0.3f), (int)(centre + radius * 0.6f), 3, ColourOf(RadarIdentity.Unknown));
        Dot(pixels, IconSize, (int)(centre - radius * 0.55f), (int)(centre - radius * 0.2f), 3, ColourOf(RadarIdentity.Hostile));
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        return tex;
    }

    private static Texture2D MakeDisc()
    {
        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, name = "PhobosRadarScopeDisc" };
        var pixels = new Color32[size * size];
        float centre = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float r = Mathf.Sqrt((x - centre) * (x - centre) + (y - centre) * (y - centre)) / centre;
                pixels[y * size + x] = r > 1f ? Color.clear : Color.Lerp(new Color(0.02f, 0.07f, 0.04f), new Color(0.05f, 0.14f, 0.07f), r * r);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        return tex;
    }

    private static Texture2D MakeBlip(Color colour)
    {
        var tex = new Texture2D(BlipSize, BlipSize, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, name = "PhobosRadarBlip" };
        var pixels = new Color32[BlipSize * BlipSize];
        float centre = (BlipSize - 1) / 2f;
        for (int y = 0; y < BlipSize; y++)
        {
            for (int x = 0; x < BlipSize; x++)
            {
                float d = Mathf.Abs(x - centre) + Mathf.Abs(y - centre);
                Color c = Color.clear;
                if (d <= centre - 1f) c = d >= centre - 4f ? Color.black : colour;
                pixels[y * BlipSize + x] = c;
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        return tex;
    }

    private static void Dot(Color32[] pixels, int size, int cx, int cy, int radius, Color colour)
    {
        for (int y = cy - radius; y <= cy + radius; y++)
            for (int x = cx - radius; x <= cx + radius; x++)
                if (x >= 0 && y >= 0 && x < size && y < size && (x - cx) * (x - cx) + (y - cy) * (y - cy) <= radius * radius) pixels[y * size + x] = colour;
    }
}
