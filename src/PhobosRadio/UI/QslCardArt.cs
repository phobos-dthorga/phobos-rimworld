using System.Collections.Generic;
using Phobos.Framework.WorldMap;
using Phobos.Radio.Physics;
using Phobos.Radio.Services;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Phobos.Radio.UI;

/// <summary>
/// A QSL card's front picture (Radio 0.59.0; docs/development/radio-qsl-cards.md, part B): the station's home as a layered landscape, the
/// sky at the hour of the contact, ridges by its hills, the ground by its biome, the sea if it is coastal. The rules are game-free in
/// <see cref="QslArtRule"/>; this only reads the tile and paints.
/// <para>Cost: one picture a card (256 by 176 pixels, about 180 KB), painted the first time the card is shown and kept for the session, at
/// most <see cref="Kept"/> of them; nothing saved, nothing per frame but drawing the picture.</para>
/// </summary>
public static class QslCardArt
{
    public const int Width = 256;
    public const int Height = 176;
    /// <summary>Pictures kept at once; past it the oldest are let go and painted again when next shown (agent's figure).</summary>
    const int Kept = 48;
    /// <summary>The nearest ridge's trees: one each this many pixels (authored).</summary>
    const int TreeSpacing = 7;

    static readonly Dictionary<string, Texture2D> pictures = new();
    static readonly Queue<string> order = new();

    /// <summary>The card's front picture, painted on first use.</summary>
    public static Texture2D Front(QslCard card)
    {
        string key = (card.Settlement?.ID ?? -1) + ":" + card.Tick;
        if (pictures.TryGetValue(key, out Texture2D? known) && known != null) return known;
        Texture2D picture = Paint(card);
        pictures[key] = picture;
        order.Enqueue(key);
        while (order.Count > Kept && pictures.Remove(order.Dequeue(), out Texture2D? old) && old != null) Object.Destroy(old);
        return picture;
    }

    static readonly Color Rock = new(0.36f, 0.38f, 0.45f);

    static Color GroundColour(QslGround ground) => ground switch
    {
        QslGround.Trees => new Color(0.17f, 0.31f, 0.15f),
        QslGround.Dunes => new Color(0.78f, 0.65f, 0.42f),
        QslGround.Snow => new Color(0.86f, 0.89f, 0.93f),
        QslGround.Swamp => new Color(0.24f, 0.31f, 0.21f),
        _ => new Color(0.33f, 0.46f, 0.22f),
    };

    static Texture2D Paint(QslCard card)
    {
        int seed = card.Settlement?.ID ?? card.Tick;
        PlanetTile tile = card.Settlement?.Tile ?? PlanetTile.Invalid;
        int hills = 1;
        QslGround ground = QslGround.Grass;
        bool coast = false;
        float longitude = 0f;
        if (tile.Valid)
        {
            if (BiomeGround.TryRead(tile, out TileGround read))
            {
                hills = (int)read.Hilliness;
                ground = QslArtRule.Ground(read.Biome.defName);
            }
            longitude = Find.WorldGrid.LongLatOf(tile).x;
            coast = Find.World.CoastDirectionAt(tile).IsValid;
        }
        double hour = GenDate.HourFloat(card.Tick, longitude);
        (float[] top, float[] low) = QslArtRule.Sky(hour);
        var pixels = new Color[Width * Height];
        var skyTop = new Color(top[0], top[1], top[2]);
        var skyLow = new Color(low[0], low[1], low[2]);
        for (int y = 0; y < Height; y++)
        {
            Color c = Color.Lerp(skyLow, skyTop, (float)y / (Height - 1));
            for (int x = 0; x < Width; x++) pixels[y * Width + x] = c;
        }

        // The sun by day, the moon by night.
        (float lx, float ly) = QslArtRule.Light(hour);
        bool day = hour % 24 > 6 && hour % 24 < 18;
        Disc(pixels, (int)(lx * Width), (int)(ly * Height), 9, day ? new Color(1f, 0.93f, 0.65f) : new Color(0.85f, 0.87f, 0.92f));

        // Three ridges, far to near: the far ones in the haze of the sky, the nearest in the biome's own colour.
        Color groundColour = GroundColour(ground);
        float[]? nearest = null;
        for (int layer = 0; layer < 3; layer++)
        {
            float[] ridge = QslArtRule.Ridge(seed, hills, Width, layer);
            Color colour = layer switch
            {
                0 => Color.Lerp(skyLow, Rock, 0.55f),
                1 => Color.Lerp(Rock, groundColour, 0.45f) * 0.85f,
                _ => groundColour,
            };
            colour.a = 1f;
            for (int x = 0; x < Width; x++)
            {
                int top2 = (int)(ridge[x] * Height);
                for (int y = 0; y < top2 && y < Height; y++)
                {
                    Color c = colour;
                    // Snow on the far peaks of a cold place, and on the highest of any mountains.
                    if (layer == 0 && (ground == QslGround.Snow || hills >= 4) && y > top2 - 4) c = Color.white;
                    pixels[y * Width + x] = c;
                }
            }
            nearest = ridge;
        }

        if (ground == QslGround.Trees && nearest != null)
        {
            var tree = new Color(0.09f, 0.20f, 0.09f);
            for (int x = 3; x < Width - 3; x += TreeSpacing)
            {
                int foot = (int)(nearest[x] * Height);
                int tall = 6 + (int)(QslRule.Pick(seed + x, 5));
                for (int dy = 0; dy < tall; dy++)
                {
                    int half = (tall - dy) / 3;
                    for (int dx = -half; dx <= half; dx++) Set(pixels, x + dx, foot + dy, tree);
                }
            }
        }

        if (coast)
        {
            var sea = new Color(0.14f, 0.33f, 0.52f);
            int line = (int)(Height * 0.16f);
            for (int y = 0; y < line; y++)
                for (int x = 0; x < Width; x++)
                    pixels[y * Width + x] = (y + x / 9) % 11 == 0 ? Color.Lerp(sea, Color.white, 0.35f) : sea;
        }

        var picture = new Texture2D(Width, Height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        picture.SetPixels(pixels);
        picture.Apply(false, true);
        return picture;
    }

    static void Disc(Color[] pixels, int cx, int cy, int r, Color colour)
    {
        for (int y = -r; y <= r; y++)
            for (int x = -r; x <= r; x++)
                if (x * x + y * y <= r * r) Set(pixels, cx + x, cy + y, colour);
    }

    static void Set(Color[] pixels, int x, int y, Color colour)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height) pixels[y * Width + x] = colour;
    }
}
