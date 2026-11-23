using System;
using System.Collections.Generic;

namespace Phobos.Radio.Physics;

/// <summary>The shape of one kind of channel use (Radio 0.8.0): how many channels one use takes, and how many radios take turns on it.</summary>
public readonly record struct KindShape(int ChannelsPerUse, int RadiosPerUse);

/// <summary>One radio asking for a channel: its id, its kind (an index into the kinds), and the stations it reaches, best first.</summary>
public readonly record struct ChannelCandidate(int Radio, int Kind, IReadOnlyList<int> StationsBestFirst);

/// <summary>
/// The channel ledger (Radio 0.7.0; the owner, 24 Nov 2026, after D'thorga Mechanised Specialist Expansion's BVR channels: "go ahead
/// and build the channel ledger", and "a channel is 25 kHz each just like a trunked system would be"). A station's channels are a
/// pool, as on a trunked radio system: a mode takes as many channels as its bandwidth needs, and the radios on a use take turns, so a
/// mode keyed a fifth of the time lets five share one. Since 0.8.0 the ledger counts by kind, as the BVR system does (the owner:
/// "Start on Phase 2's ledger kinds"): each kind fills whole blocks of channels of its own, and kinds never share a block. No
/// frequencies, no tuning: a count. Game-free; compiled into the tests.
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

    /// <summary>The channels a kind's radios hold: whole uses, each its own block.</summary>
    public static int ChannelsHeld(int radios, KindShape shape) =>
        radios <= 0 ? 0 : (radios + Math.Max(1, shape.RadiosPerUse) - 1) / Math.Max(1, shape.RadiosPerUse) * Math.Max(1, shape.ChannelsPerUse);

    /// <summary>The channels a station's radios hold, kind by kind.</summary>
    public static int ChannelsHeld(IReadOnlyList<int> radiosByKind, IReadOnlyList<KindShape> kinds)
    {
        var total = 0;
        for (var k = 0; k < kinds.Count && k < radiosByKind.Count; k++) total += ChannelsHeld(radiosByKind[k], kinds[k]);
        return total;
    }

    /// <summary>
    /// How many more radios of one kind a station could serve, with the others as they are: the turns left in that kind's last
    /// block, and whole new blocks in the channels nobody holds.
    /// </summary>
    public static int Room(int kind, IReadOnlyList<int> radiosByKind, IReadOnlyList<KindShape> kinds, int channels)
    {
        if (kind < 0 || kind >= kinds.Count) return 0;
        var shape = kinds[kind];
        var perUse = Math.Max(1, shape.ChannelsPerUse);
        var radiosPerUse = Math.Max(1, shape.RadiosPerUse);
        var have = kind < radiosByKind.Count ? radiosByKind[kind] : 0;
        var slack = have <= 0 ? 0 : ChannelsHeld(have, shape) / perUse * radiosPerUse - have;
        var free = Math.Max(0, channels - ChannelsHeld(radiosByKind, kinds));
        return slack + free / perUse * radiosPerUse;
    }

    /// <summary>
    /// Who is served by which station. A radio that held a channel keeps it while it still reaches that station and the station has
    /// room, so two near the limit never swap back and forth. The rest are served in the order given (the caller puts the kinds that
    /// matter most first, then the strongest), each by the best station with room for one more of its kind; a radio no station has
    /// room for is left out (the net is busy for it).
    /// </summary>
    public static Dictionary<int, int> Allocate(IReadOnlyDictionary<int, int> channelsByStation, IReadOnlyList<KindShape> kinds,
        IReadOnlyList<ChannelCandidate> candidates, IReadOnlyDictionary<int, int> previous)
    {
        var counts = new Dictionary<int, int[]>();
        var served = new Dictionary<int, int>();
        int[] Counts(int station)
        {
            if (!counts.TryGetValue(station, out var c)) counts[station] = c = new int[kinds.Count];
            return c;
        }
        bool Room(int station, int kind)
        {
            if (!channelsByStation.TryGetValue(station, out var channels) || kind < 0 || kind >= kinds.Count) return false;
            var c = Counts(station);
            c[kind]++;
            var fits = ChannelsHeld(c, kinds) <= channels;
            c[kind]--;
            return fits;
        }
        void Take(int radio, int station, int kind)
        {
            served[radio] = station;
            Counts(station)[kind]++;
        }

        foreach (var candidate in candidates)
        {
            if (!previous.TryGetValue(candidate.Radio, out var held)) continue;
            if (!Contains(candidate.StationsBestFirst, held) || !Room(held, candidate.Kind)) continue;
            Take(candidate.Radio, held, candidate.Kind);
        }
        foreach (var candidate in candidates)
        {
            if (served.ContainsKey(candidate.Radio)) continue;
            foreach (var station in candidate.StationsBestFirst)
            {
                if (!Room(station, candidate.Kind)) continue;
                Take(candidate.Radio, station, candidate.Kind);
                break;
            }
        }
        return served;
    }

    /// <summary>
    /// Whether a radio is keyed now, taking its turn on its use: its place among the station's radios of its kind sets which use and
    /// which turn; each turn lasts the mode's keyed share of the cycle.
    /// </summary>
    public static bool KeyedNow(int place, int radiosPerUse, int ticks, int cycleTicks, double dutyCycle)
    {
        if (place < 0 || cycleTicks <= 0) return false;
        var turn = place % Math.Max(1, radiosPerUse);
        var turnTicks = Math.Max(1, (int)(cycleTicks * Math.Clamp(dutyCycle, 0.0, 1.0)));
        var at = ((ticks % cycleTicks) + cycleTicks) % cycleTicks;
        return at >= turn * turnTicks && at < (turn + 1) * turnTicks;
    }

    /// <summary>Which channel a radio's use starts on, counting from 0: its kind's block starts at <paramref name="kindStart"/>.</summary>
    public static int ChannelOf(int place, KindShape shape, int kindStart) =>
        place < 0 ? -1 : kindStart + place / Math.Max(1, shape.RadiosPerUse) * Math.Max(1, shape.ChannelsPerUse);

    private static bool Contains(IReadOnlyList<int> list, int value)
    {
        for (var i = 0; i < list.Count; i++) if (list[i] == value) return true;
        return false;
    }
}
