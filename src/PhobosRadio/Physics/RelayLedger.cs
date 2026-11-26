using System;
using System.Collections.Generic;

namespace Phobos.Radio.Physics;

/// <summary>
/// The channels at relays between tiles (Radio 0.24.0; the relay design's rule 5, radio-advice-and-relays.md: "a relayed call takes
/// channels at each relay through the ledger; two at a relay between tiles, one in and one out"). Each relay is a small pool of channels,
/// counted by kind as a station's are (<see cref="Channels"/>): one use of a kind takes twice its channels at a relay, since a repeater
/// hears on one and sends on the other, and the radios on a use take turns. A call through a relay holds a <b>lease</b> there, renewed
/// each time its caller is asked about again and lapsing when it is not, so a convoy that stops asking frees its place by itself, and
/// nothing is saved. A kind that pre-empts (vehicle control) may take a place from the kinds served after it, as at a station. Nodes and
/// callers are any objects with equality; game-free, compiled into the tests.
/// </summary>
public sealed class RelayLedger
{
    readonly record struct Lease(object Caller, int Kind, int Until, int Since);

    readonly Dictionary<object, List<Lease>> byNode = new();

    /// <summary>A kind's shape at a relay: twice the channels of one use, one in and one out; the same turns.</summary>
    public static KindShape AtRelay(KindShape shape) => shape with { ChannelsPerUse = Math.Max(1, shape.ChannelsPerUse) * 2 };

    List<Lease> Live(object node, int now)
    {
        if (!byNode.TryGetValue(node, out var leases)) return new List<Lease>();
        leases.RemoveAll(l => l.Until < now);
        if (leases.Count == 0) byNode.Remove(node);
        return leases;
    }

    static int[] Count(List<Lease> leases, int kinds, object? except)
    {
        var counts = new int[kinds];
        foreach (var lease in leases)
        {
            if (except != null && Equals(lease.Caller, except)) continue;
            if (lease.Kind >= 0 && lease.Kind < kinds) counts[lease.Kind]++;
        }
        return counts;
    }

    static int Room(int kind, int[] counts, IReadOnlyList<KindShape> shapes, int channels)
    {
        var atRelay = new KindShape[shapes.Count];
        for (var k = 0; k < shapes.Count; k++) atRelay[k] = AtRelay(shapes[k]);
        return Channels.Room(kind, counts, atRelay, channels);
    }

    /// <summary>Whether this caller holds a place at this relay already.</summary>
    public bool Holds(object node, object caller, int now) => Live(node, now).Exists(l => Equals(l.Caller, caller));

    /// <summary>
    /// Whether this caller's call of this kind may pass through this relay now: it holds a place there already, there is room, or its kind
    /// pre-empts and a call of a kind served after it could make room.
    /// </summary>
    public bool HasRoom(object node, object caller, int kind, IReadOnlyList<KindShape> shapes, int channels, int now)
    {
        if (kind < 0 || kind >= shapes.Count || channels <= 0) return false;
        var leases = Live(node, now);
        if (leases.Exists(l => Equals(l.Caller, caller))) return true;
        var counts = Count(leases, shapes.Count, caller);
        if (Room(kind, counts, shapes, channels) > 0) return true;
        if (!shapes[kind].Preempts) return false;
        for (var k = shapes.Count - 1; k > kind; k--)
        {
            if (shapes[k].Preempts) continue;
            while (counts[k] > 0)
            {
                counts[k]--;
                if (Room(kind, counts, shapes, channels) > 0) return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Holds a place for this caller at each relay on its path until <paramref name="until"/>, and lets go of its places anywhere else. A
    /// relay with no room for it, where its kind pre-empts, loses the newest calls of the kinds served last until there is: those callers
    /// find no room at their next ask.
    /// </summary>
    public void Hold(object caller, int kind, IEnumerable<object> path, IReadOnlyList<KindShape> shapes, Func<object, int> channelsOf, int now, int until)
    {
        var on = new HashSet<object>(path);
        foreach (var node in new List<object>(byNode.Keys))
        {
            if (on.Contains(node)) continue;
            byNode[node].RemoveAll(l => Equals(l.Caller, caller));
            if (byNode[node].Count == 0) byNode.Remove(node);
        }
        foreach (var node in on)
        {
            var leases = Live(node, now);
            var mine = leases.FindIndex(l => Equals(l.Caller, caller));
            if (mine >= 0)
            {
                leases[mine] = leases[mine] with { Until = until, Kind = kind };
                continue;
            }
            var channels = channelsOf(node);
            var counts = Count(leases, shapes.Count, null);
            if (kind >= 0 && kind < shapes.Count && shapes[kind].Preempts)
            {
                for (var k = shapes.Count - 1; k > kind && Room(kind, counts, shapes, channels) <= 0; k--)
                {
                    if (shapes[k].Preempts) continue;
                    while (counts[k] > 0 && Room(kind, counts, shapes, channels) <= 0)
                    {
                        var newest = -1;
                        for (var i = 0; i < leases.Count; i++)
                        {
                            if (leases[i].Kind == k && (newest < 0 || leases[i].Since > leases[newest].Since)) newest = i;
                        }
                        if (newest < 0) break;
                        leases.RemoveAt(newest);
                        counts[k]--;
                    }
                }
            }
            leases.Add(new Lease(caller, kind, until, now));
            byNode[node] = leases;
        }
    }

    /// <summary>Lets go of every place this caller holds.</summary>
    public void Release(object caller)
    {
        foreach (var node in new List<object>(byNode.Keys))
        {
            byNode[node].RemoveAll(l => Equals(l.Caller, caller));
            if (byNode[node].Count == 0) byNode.Remove(node);
        }
    }

    /// <summary>How many calls pass through this relay now, and how many channels they hold there.</summary>
    public (int Calls, int Channels) Use(object node, IReadOnlyList<KindShape> shapes, int now)
    {
        var leases = Live(node, now);
        var counts = Count(leases, shapes.Count, null);
        var atRelay = new KindShape[shapes.Count];
        for (var k = 0; k < shapes.Count; k++) atRelay[k] = AtRelay(shapes[k]);
        return (leases.Count, Channels.ChannelsHeld(counts, atRelay));
    }

    /// <summary>Forgets everything: a new game.</summary>
    public void Clear() => byNode.Clear();
}
