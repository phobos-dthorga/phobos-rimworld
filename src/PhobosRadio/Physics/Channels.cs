using System;
using System.Collections.Generic;

namespace Phobos.Radio.Physics;

/// <summary>One radio asking for a channel: its id, and the stations it is in touch with, best first.</summary>
public readonly record struct ChannelCandidate(int Radio, IReadOnlyList<int> StationsBestFirst);

/// <summary>
/// The channel ledger (Radio 0.7.0; the owner, 24 Nov 2026, after D'thorga Mechanised Specialist Expansion's BVR channels: "go ahead
/// and build the channel ledger", and "a channel is 25 kHz each just like a trunked system would be"). A station's channels are a
/// pool, as on a trunked radio system: a mode takes as many channels as its bandwidth needs, and the radios on a channel take turns,
/// so a mode keyed a fifth of the time lets five share one. No frequencies, no tuning: a count. Game-free; compiled into the tests.
/// </summary>
public static class Channels
{
    /// <summary>How many channels one use of a mode takes: its bandwidth over the channel width, rounded up, at least one.</summary>
    public static int PerUse(double bandwidthHz, double channelWidthHz)
    {
        if (channelWidthHz <= 0) throw new ArgumentOutOfRangeException(nameof(channelWidthHz), "A channel must have a width.");
        return Math.Max(1, (int)Math.Ceiling(bandwidthHz / channelWidthHz - 1e-9));
    }

    /// <summary>How many radios can take turns on one use's channels: one over the share of the time each is keyed, at least one.</summary>
    public static int RadiosPerUse(double dutyCycle) => dutyCycle <= 0 ? 1 : Math.Max(1, (int)Math.Floor(1.0 / dutyCycle + 1e-9));

    /// <summary>How many radios a station's channels can serve at once in a mode.</summary>
    public static int Slots(int channels, int channelsPerUse, int radiosPerUse) =>
        channelsPerUse <= 0 ? 0 : Math.Max(0, channels) / channelsPerUse * radiosPerUse;

    /// <summary>
    /// Who is served by which station. A radio that held a channel keeps it while still in touch with that station and the station has
    /// room, so two near the limit never swap back and forth. The rest are served in the order given, each by the best station that has
    /// room; a radio no station has room for is left out (the net is busy for it).
    /// </summary>
    public static Dictionary<int, int> Allocate(IReadOnlyDictionary<int, int> slotsByStation, IReadOnlyList<ChannelCandidate> candidates,
        IReadOnlyDictionary<int, int> previous)
    {
        var used = new Dictionary<int, int>();
        var served = new Dictionary<int, int>();
        bool Room(int station) => slotsByStation.TryGetValue(station, out var slots) && (used.TryGetValue(station, out var n) ? n : 0) < slots;
        void Take(int radio, int station)
        {
            served[radio] = station;
            used[station] = (used.TryGetValue(station, out var n) ? n : 0) + 1;
        }

        foreach (var candidate in candidates)
        {
            if (!previous.TryGetValue(candidate.Radio, out var held)) continue;
            if (!Contains(candidate.StationsBestFirst, held) || !Room(held)) continue;
            Take(candidate.Radio, held);
        }
        foreach (var candidate in candidates)
        {
            if (served.ContainsKey(candidate.Radio)) continue;
            foreach (var station in candidate.StationsBestFirst)
            {
                if (!Room(station)) continue;
                Take(candidate.Radio, station);
                break;
            }
        }
        return served;
    }

    /// <summary>
    /// Whether a radio is keyed now, taking its turn on its channel: its place among the station's served radios sets which channel
    /// and which turn; each turn lasts the mode's keyed share of the cycle.
    /// </summary>
    public static bool KeyedNow(int place, int radiosPerUse, int ticks, int cycleTicks, double dutyCycle)
    {
        if (place < 0 || cycleTicks <= 0) return false;
        var turn = place % Math.Max(1, radiosPerUse);
        var turnTicks = Math.Max(1, (int)(cycleTicks * Math.Clamp(dutyCycle, 0.0, 1.0)));
        var at = ((ticks % cycleTicks) + cycleTicks) % cycleTicks;
        return at >= turn * turnTicks && at < (turn + 1) * turnTicks;
    }

    /// <summary>Which channel a radio's turn is on, counting from 0: each use takes a block of channels.</summary>
    public static int ChannelOf(int place, int radiosPerUse, int channelsPerUse) =>
        place < 0 ? -1 : place / Math.Max(1, radiosPerUse) * Math.Max(1, channelsPerUse);

    private static bool Contains(IReadOnlyList<int> list, int value)
    {
        for (var i = 0; i < list.Count; i++) if (list[i] == value) return true;
        return false;
    }
}
