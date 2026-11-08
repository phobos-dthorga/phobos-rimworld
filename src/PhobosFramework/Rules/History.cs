using System;
using System.Text;

namespace Phobos.Framework.Rules;

/// <summary>
/// A fixed-size ring of past samples, oldest first, for graphs: a vehicle quantity, a radio signal, a patient's vitals. Game-free.
/// </summary>
public sealed class HistoryRing
{
    readonly float[] samples;
    int start;
    int count;

    public HistoryRing(int capacity)
    {
        samples = new float[Math.Max(1, capacity)];
    }

    public int Capacity => samples.Length;
    public int Count => count;

    /// <summary>The sample at a position, 0 being the oldest kept and Count − 1 the newest.</summary>
    public float this[int oldestFirst] => samples[(start + oldestFirst) % Capacity];

    public float Newest => count == 0 ? 0f : this[count - 1];

    public void Push(float value)
    {
        if (count < Capacity)
        {
            samples[(start + count) % Capacity] = value;
            count++;
        }
        else
        {
            samples[start] = value;
            start = (start + 1) % Capacity;
        }
    }

    public void Clear()
    {
        start = 0;
        count = 0;
    }

    /// <summary>The samples, oldest first, into an array of at least Count.</summary>
    public void CopyTo(float[] into)
    {
        for (int i = 0; i < count && i < into.Length; i++) into[i] = this[i];
    }

    /// <summary>How many samples cover a span at one sample per stride (24 hours at one per 250 ticks: 240).</summary>
    public static int SamplesFor(int hours, int strideTicks, int ticksPerHour = 2500) => Math.Max(1, hours * ticksPerHour / Math.Max(1, strideTicks));
}

/// <summary>
/// A ring as text for a save: each sample scaled over min..max to a 16-bit value, little-endian, base64. About 480 bytes for 240
/// samples; a value outside the range clamps. Game-free.
/// </summary>
public static class HistoryCodec
{
    public static string Encode(HistoryRing ring, float min, float max)
    {
        var bytes = new byte[ring.Count * 2];
        float span = Math.Max(1e-6f, max - min);
        for (int i = 0; i < ring.Count; i++)
        {
            float share = Math.Clamp((ring[i] - min) / span, 0f, 1f);
            ushort scaled = (ushort)Math.Round(share * ushort.MaxValue);
            bytes[i * 2] = (byte)(scaled & 0xFF);
            bytes[i * 2 + 1] = (byte)(scaled >> 8);
        }
        return Convert.ToBase64String(bytes);
    }

    /// <summary>A ring of the given capacity from the text; malformed text gives an empty ring, never an error.</summary>
    public static HistoryRing Decode(string? text, int capacity, float min, float max)
    {
        var ring = new HistoryRing(capacity);
        if (string.IsNullOrEmpty(text)) return ring;
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(text);
        }
        catch (FormatException)
        {
            return ring;
        }
        float span = Math.Max(1e-6f, max - min);
        for (int i = 0; i + 1 < bytes.Length; i += 2)
        {
            ushort scaled = (ushort)(bytes[i] | (bytes[i + 1] << 8));
            ring.Push(min + span * scaled / ushort.MaxValue);
        }
        return ring;
    }
}
