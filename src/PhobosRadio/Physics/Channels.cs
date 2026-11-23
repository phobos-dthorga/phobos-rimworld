using System;
using System.Collections.Generic;

namespace Phobos.Radio.Physics;

/// <summary>
/// The shape of one kind of channel use (Radio 0.8.0): how many channels one use takes, and how many radios take turns on it. A kind
/// that <paramref name="Preempts"/> (0.9.0) may take channels from the kinds served after it when a station is full.
/// </summary>
public readonly record struct KindShape(int ChannelsPerUse, int RadiosPerUse, bool Preempts = false);

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
    /// <para>
    /// Pre-emption (0.9.0; the autonomous vehicles record, an agent default): a radio of a kind that pre-empts, finding no station with
    /// room, takes channels at the best station where it can, from the radios of kinds served after it that do not pre-empt, the last
    /// in the order given first (the weakest of the kind served last), and only as many as it needs. A radio so put off tries the
    /// other stations it reaches; with no room there either, the net is busy for it. Nothing is taken where even all of them would
    /// not make room.
    /// </para>
    /// </summary>
    public static Dictionary<int, int> Allocate(IReadOnlyDictionary<int, int> channelsByStation, IReadOnlyList<KindShape> kinds,
        IReadOnlyList<ChannelCandidate> candidates, IReadOnlyDictionary<int, int> previous)
    {
        var counts = new Dictionary<int, int[]>();
        var served = new Dictionary<int, int>();
        var servedAt = new Dictionary<int, List<int>>();   // each station's radios, as indices into the candidates
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
        void Take(int at, int station)
        {
            var candidate = candidates[at];
            served[candidate.Radio] = station;
            Counts(station)[candidate.Kind]++;
            if (!servedAt.TryGetValue(station, out var list)) servedAt[station] = list = new List<int>();
            list.Add(at);
        }
        bool TakeAnyWithRoom(int at)
        {
            foreach (var station in candidates[at].StationsBestFirst)
            {
                if (!Room(station, candidates[at].Kind)) continue;
                Take(at, station);
                return true;
            }
            return false;
        }
        bool Preempt(int at, List<int> putOff)
        {
            var kind = candidates[at].Kind;
            foreach (var station in candidates[at].StationsBestFirst)
            {
                if (!channelsByStation.TryGetValue(station, out var channels) || !servedAt.TryGetValue(station, out var list)) continue;
                var victims = new List<int>();
                foreach (var other in list)
                {
                    var otherKind = candidates[other].Kind;
                    if (otherKind > kind && !kinds[otherKind].Preempts) victims.Add(other);
                }
                if (victims.Count == 0) continue;
                // Would putting them all off make room? If not, take nothing here.
                var c = (int[])Counts(station).Clone();
                foreach (var victim in victims) c[candidates[victim].Kind]--;
                c[kind]++;
                if (ChannelsHeld(c, kinds) > channels) continue;
                victims.Sort((a, b) => b.CompareTo(a));   // the last in the order first
                foreach (var victim in victims)
                {
                    if (Room(station, kind)) break;
                    served.Remove(candidates[victim].Radio);
                    Counts(station)[candidates[victim].Kind]--;
                    list.Remove(victim);
                    putOff.Add(victim);
                }
                Take(at, station);
                return true;
            }
            return false;
        }

        for (var at = 0; at < candidates.Count; at++)
        {
            var candidate = candidates[at];
            if (!previous.TryGetValue(candidate.Radio, out var held)) continue;
            if (!Contains(candidate.StationsBestFirst, held) || !Room(held, candidate.Kind)) continue;
            Take(at, held);
        }
        List<int>? putOffAll = null;
        for (var at = 0; at < candidates.Count; at++)
        {
            var candidate = candidates[at];
            if (served.ContainsKey(candidate.Radio) || TakeAnyWithRoom(at)) continue;
            if (candidate.Kind < 0 || candidate.Kind >= kinds.Count || !kinds[candidate.Kind].Preempts) continue;
            Preempt(at, putOffAll ??= new List<int>());
        }
        if (putOffAll != null)
        {
            putOffAll.Sort();
            foreach (var at in putOffAll) if (!served.ContainsKey(candidates[at].Radio)) TakeAnyWithRoom(at);
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
