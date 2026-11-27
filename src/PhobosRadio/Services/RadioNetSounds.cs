using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.Sound;

namespace Phobos.Radio.Services;

/// <summary>
/// The sounds of the net changing (Radio 0.30.0; the owner's cards of 27 Nov 2026): a radio's own squelch as it comes into touch with a
/// station (S163, its Def's engageSound), and a station's channel knob as it moves one of its radios to another channel (S169). Worked
/// out only after the net recomputes, by comparing who is served, and on which channel, with the recompute before, so it costs nothing
/// per tick. Nothing is saved: the first recompute after a load or a new map only remembers, so a loaded colony does not squelch at once.
/// </summary>
public sealed class RadioNetSounds
{
    /// <summary>
    /// The least time between two plays of one radio's squelch, or one station's knob (agent's choice, about two in-game hours), so a pawn
    /// walking along the edge of range does not chatter. Not physics: a sound's pace only.
    /// </summary>
    public const int CooldownTicks = 5000;

    private bool primed;
    private readonly Dictionary<int, (int station, int channel)> before = new();
    private readonly Dictionary<int, (int station, int channel)> now = new();
    private readonly Dictionary<int, int> lastPlayed = new();

    /// <summary>Called by the net at the end of each recompute, with each served radio and its station.</summary>
    public void Heard(RadioNet net, Map map, IReadOnlyDictionary<Thing, List<Thing>> servedOn)
    {
        now.Clear();
        int tick = Find.TickManager.TicksGame;
        foreach (var pair in servedOn)
        {
            Thing station = pair.Key;
            foreach (Thing radio in pair.Value)
            {
                int channel = net.ChannelOf(station, radio);
                now[radio.thingIDNumber] = (station.thingIDNumber, channel);
                if (!primed) continue;
                if (!before.TryGetValue(radio.thingIDNumber, out var was))
                {
                    // Newly in touch: the radio's own squelch where its holder stands.
                    if (radio.TryGetComp<CompRadio>()?.Props.engageSound is { } squelch && CompRadio.HolderOf(radio) is { Spawned: true } holder
                        && holder.Map == map && Due(radio.thingIDNumber, tick))
                    {
                        squelch.PlayOneShot(new TargetInfo(holder.Position, map));
                        string variant = radio is Pawn ? "vehicle" : "handheld";
                        if (Seen.Wants("radio.engage-squelch", holder, variant)) Seen.Note("radio.engage-squelch", holder, null, variant);
                    }
                }
                else if (was.station == station.thingIDNumber && was.channel != channel && channel >= 0 && Due(station.thingIDNumber, tick))
                {
                    // The same station moved it to another channel: its knob turned.
                    RadioSoundDefOf.PhobosRadio_ChannelKnob.PlayOneShot(new TargetInfo(station.Position, map));
                    Seen.Note("radio.channel-knob", station);
                }
            }
        }
        before.Clear();
        foreach (var entry in now) before[entry.Key] = entry.Value;
        primed = true;
    }

    /// <summary>Forgets everything, as when the map goes: the next recompute only remembers.</summary>
    public void Reset()
    {
        primed = false;
        before.Clear();
        lastPlayed.Clear();
    }

    private bool Due(int id, int tick)
    {
        if (lastPlayed.TryGetValue(id, out int last) && tick - last < CooldownTicks) return false;
        lastPlayed[id] = tick;
        return true;
    }
}
