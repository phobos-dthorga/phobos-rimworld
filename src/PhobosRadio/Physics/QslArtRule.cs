using System;
using System.Collections.Generic;

namespace Phobos.Radio.Physics;

/// <summary>What a card is printed on, by the sending faction's tech level (Radio 0.59.0; agent's choices).</summary>
public enum QslStock
{
    Hide,
    Parchment,
    Card,
    Glossy,
}

/// <summary>The ground of a card's picture, by the station's biome (Radio 0.59.0).</summary>
public enum QslGround
{
    Grass,
    Trees,
    Dunes,
    Snow,
    Swamp,
}

/// <summary>
/// The QSL card's drawn picture, game-free (Radio 0.59.0; docs/development/radio-qsl-cards.md, part B): ridgelines from the tile's hills,
/// the sky from the hour of the contact, the stock from the faction's tech level and the operator's signature, each from a seed, so the
/// same card always draws the same picture. Heights and positions are fractions of the picture (0 at the bottom, 1 at the top).
/// </summary>
public static class QslArtRule
{
    /// <summary>
    /// One ridgeline across the picture: <paramref name="width"/> heights. Hills from 0 (flat) to 5 (impassable) set how high and how rough;
    /// <paramref name="layer"/> 0 is the farthest and highest, each nearer layer lower. Authored figures (agent's).
    /// </summary>
    public static float[] Ridge(int seed, int hills, int width, int layer)
    {
        hills = Math.Max(0, Math.Min(5, hills));
        float baseHeight = new[] { 0.30f, 0.34f, 0.40f, 0.50f, 0.62f, 0.72f }[hills] - layer * 0.12f;
        float rough = new[] { 0.02f, 0.03f, 0.06f, 0.12f, 0.20f, 0.24f }[hills] * (1f - layer * 0.25f);
        var heights = new float[width];
        // Three octaves of seeded sine waves: smooth, and the same for the same seed.
        uint s = Mix((uint)seed * 31u + (uint)layer);
        double p1 = s % 1000 / 1000.0 * Math.PI * 2, p2 = Mix(s) % 1000 / 1000.0 * Math.PI * 2, p3 = Mix(s + 7) % 1000 / 1000.0 * Math.PI * 2;
        for (int x = 0; x < width; x++)
        {
            double t = (double)x / Math.Max(1, width - 1);
            double wave = Math.Sin(t * Math.PI * 2 * 1.3 + p1) * 0.55 + Math.Sin(t * Math.PI * 2 * 3.1 + p2) * 0.3 + Math.Sin(t * Math.PI * 2 * 7.7 + p3) * 0.15;
            heights[x] = Clamp01(baseHeight + (float)wave * rough);
        }
        return heights;
    }

    /// <summary>The sky's colour at the top and at the horizon (RGB, 0 to 1) for the local hour: night, dawn and dusk, day.</summary>
    public static (float[] Top, float[] Horizon) Sky(double localHour)
    {
        double h = ((localHour % 24) + 24) % 24;
        float[] nightTop = { 0.04f, 0.05f, 0.12f }, nightLow = { 0.10f, 0.12f, 0.25f };
        float[] dayTop = { 0.25f, 0.50f, 0.85f }, dayLow = { 0.70f, 0.84f, 0.95f };
        float[] glowTop = { 0.30f, 0.25f, 0.50f }, glowLow = { 0.98f, 0.60f, 0.35f };
        // Dawn round 6, dusk round 18, an hour and a half either side (authored).
        double fromGlow = Math.Min(Math.Abs(h - 6), Math.Abs(h - 18));
        if (fromGlow < 1.5)
        {
            float g = (float)(1 - fromGlow / 1.5);
            bool day = h > 6 && h < 18;
            return (Mix3(day ? dayTop : nightTop, glowTop, g), Mix3(day ? dayLow : nightLow, glowLow, g));
        }
        return h > 6 && h < 18 ? (dayTop, dayLow) : (nightTop, nightLow);
    }

    /// <summary>Where the sun or moon sits (x, y as fractions), by the hour: up from the left at dawn, highest at noon or midnight.</summary>
    public static (float X, float Y) Light(double localHour)
    {
        double h = ((localHour % 24) + 24) % 24;
        double t = (h >= 6 && h < 18 ? h - 6 : (h + 6) % 24) / 12.0;
        return ((float)(0.1 + 0.8 * t), (float)(0.45 + 0.4 * Math.Sin(t * Math.PI)));
    }

    /// <summary>The stock for a tech level, as the game numbers them (Neolithic 2, Medieval 3, Industrial 4, Spacer 5, Ultra 6, Archotech 7).</summary>
    public static QslStock Stock(int techLevel) => techLevel <= 2 ? QslStock.Hide : techLevel == 3 ? QslStock.Parchment : techLevel == 4 ? QslStock.Card : QslStock.Glossy;

    /// <summary>The ground for a biome, by its name's words (mod biomes too); grass for anything else.</summary>
    public static QslGround Ground(string biomeName)
    {
        string n = biomeName.ToLowerInvariant();
        if (Has(n, "desert", "arid", "dune", "badland")) return QslGround.Dunes;
        if (Has(n, "ice", "tundra", "glacial", "snow", "frozen")) return QslGround.Snow;
        if (Has(n, "swamp", "marsh", "bog", "wetland")) return QslGround.Swamp;
        if (Has(n, "forest", "jungle", "taiga", "boreal", "wood")) return QslGround.Trees;
        return QslGround.Grass;
    }

    /// <summary>
    /// A signature: a pen squiggle as points across a box (x, y as fractions), seeded by the name, so each operator signs the same way.
    /// </summary>
    public static List<(float X, float Y)> Signature(string name, int points = 24)
    {
        uint s = 2166136261u;
        foreach (char c in name) s = (s ^ c) * 16777619u;
        var line = new List<(float, float)>(points);
        double phase = Mix(s) % 1000 / 1000.0 * Math.PI * 2, loops = 2 + Mix(s + 1) % 3;
        for (int i = 0; i < points; i++)
        {
            double t = (double)i / (points - 1);
            double y = 0.5 + 0.35 * Math.Sin(t * Math.PI * 2 * loops + phase) * (0.6 + 0.4 * Math.Sin(t * Math.PI));
            line.Add(((float)(0.05 + 0.9 * t), Clamp01((float)y)));
        }
        return line;
    }

    static bool Has(string text, params string[] words)
    {
        foreach (string w in words)
            if (text.Contains(w)) return true;
        return false;
    }

    static float[] Mix3(float[] a, float[] b, float t) => new[] { a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t };

    static float Clamp01(float v) => v < 0 ? 0 : v > 1 ? 1 : v;

    static uint Mix(uint x)
    {
        x ^= x >> 16;
        x *= 0x85EBCA6Bu;
        x ^= x >> 13;
        x *= 0xC2B2AE35u;
        x ^= x >> 16;
        return x;
    }
}
