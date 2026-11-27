using System;
using System.Text;

namespace Phobos.Radio.Physics;

/// <summary>
/// What a QSL card carries that can be worked out rather than stored (Radio 0.58.0; docs/development/radio-qsl-cards.md, the owner's pick
/// of 28 Nov 2026): the Maidenhead grid locator of a place, a station's callsign, and the signal report. Game-free: the game passes
/// latitude, longitude and seeds, so the same place always gives the same card.
/// </summary>
public static class QslRule
{
    /// <summary>
    /// The Maidenhead locator to six characters (field, square, subsquare), as amateurs worldwide give their station's place: the
    /// published algorithm, unchanged (the IARU's; Munich, 48.147°N 11.608°E, is JN58td).
    /// </summary>
    public static string Locator(double latitude, double longitude)
    {
        double lon = Clamp(longitude + 180.0, 0, 359.999999);
        double lat = Clamp(latitude + 90.0, 0, 179.999999);
        var text = new StringBuilder(6);
        text.Append((char)('A' + (int)(lon / 20)));
        text.Append((char)('A' + (int)(lat / 10)));
        text.Append((char)('0' + (int)(lon % 20 / 2)));
        text.Append((char)('0' + (int)(lat % 10)));
        text.Append((char)('a' + (int)(lon % 2 * 12)));
        text.Append((char)('a' + (int)(lat % 1 * 24)));
        return text.ToString();
    }

    /// <summary>
    /// A callsign: the faction's two-letter prefix, a call-area digit from the station's latitude (0 in the far south to 9 in the far north,
    /// as a country numbers its call areas), and two or three letters of the station's own from its seed. Authored scheme (agent's).
    /// </summary>
    public static string Callsign(string prefix, double latitude, int stationSeed)
    {
        int area = Math.Min(9, Math.Max(0, (int)((latitude + 90.0) / 18.0)));
        uint seed = Mix((uint)stationSeed);
        int length = seed % 3 == 0 ? 2 : 3;
        var text = new StringBuilder(prefix.Length + 1 + length);
        text.Append(prefix).Append((char)('0' + area));
        for (int i = 0; i < length; i++)
        {
            seed = Mix(seed + (uint)i + 1u);
            text.Append((char)('A' + (int)(seed % 26)));
        }
        return text.ToString();
    }

    /// <summary>
    /// A faction's two-letter prefix: its name's first letter (A to Z, accents and marks stripped; otherwise one from the seed), then a
    /// letter from its seed, moved on by <paramref name="taken"/> so the game can step past a prefix another faction already has.
    /// </summary>
    public static string Prefix(string name, int factionSeed, int taken = 0)
    {
        char first = FirstLetter(name) ?? (char)('A' + (int)(Mix((uint)factionSeed) % 26));
        char second = (char)('A' + (int)((Mix((uint)factionSeed ^ 0x9E3779B9u) + (uint)taken) % 26));
        return new string(new[] { first, second });
    }

    /// <summary>The report: readability and strength (RS) for voice, with a tone figure for Morse (RST: a clean note is 9).</summary>
    public static string Report(int readability, int strength, bool morse) =>
        readability.ToString() + strength.ToString() + (morse ? "9" : "");

    /// <summary>Which of <paramref name="count"/> authored lines a card takes: the same for the same seed, spread across them.</summary>
    public static int Pick(int seed, int count) => count <= 0 ? 0 : (int)(Mix((uint)seed ^ 0x85EBCA6Bu) % (uint)count);

    static char? FirstLetter(string name)
    {
        foreach (char c in name.Normalize(NormalizationForm.FormD))
        {
            char upper = char.ToUpperInvariant(c);
            if (upper >= 'A' && upper <= 'Z') return upper;
        }
        return null;
    }

    /// <summary>A small integer hash (murmur3's finaliser): stable across runs and machines, unlike string hash codes.</summary>
    static uint Mix(uint x)
    {
        x ^= x >> 16;
        x *= 0x85EBCA6Bu;
        x ^= x >> 13;
        x *= 0xC2B2AE35u;
        x ^= x >> 16;
        return x;
    }

    static double Clamp(double v, double lo, double hi) => v < lo ? lo : v > hi ? hi : v;
}
