using System.Collections.Generic;
using Phobos.Radio.Physics;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>What an over is: a chat between two colonists, a call to one away in the evening, or company through the night.</summary>
public enum OverKind
{
    Chat,
    EveningCall,
    Company,
}

/// <summary>
/// One over on the air (Radio 0.63.0; docs/development/radio-life-on-the-air.md, RN1): who started it, who answers (a colonist on the map, one
/// away in a caravan, or nobody of ours: a radio friend, named), what it is, when it ends, whether the link was faint, and the stations that
/// carry it. Never saved: an over lasts minutes, and a load simply starts quiet.
/// </summary>
public sealed class Over
{
    public Over(Pawn starter, Pawn? other, string? otherName, OverKind kind, int start, int end, bool faint, IReadOnlyList<Thing> stations)
    {
        Starter = starter;
        Other = other;
        OtherName = otherName;
        Kind = kind;
        Start = start;
        End = end;
        Faint = faint;
        Stations = stations;
    }

    public Pawn Starter { get; }
    public Pawn? Other { get; }
    public string? OtherName { get; }
    public OverKind Kind { get; }
    public int Start { get; }
    public int End { get; set; }
    public bool Faint { get; }
    public IReadOnlyList<Thing> Stations { get; }

    /// <summary>The one who answers, by name: a colonist's short name, or the radio friend's.</summary>
    public string OtherLabel => Other?.LabelShort ?? OtherName ?? "?";
}

/// <summary>
/// Who is talking on the radio now (Radio 0.63.0; RN1): the overs the talk pass starts (<see cref="RadioTalk"/>), and the radio jobs that were
/// already calls (a radio friend's chat, a neighbour's call, a call to a faction, a lesson, advice). The net asks it whether a colonist's
/// hand-held is keyed, so the lamp, the needle and the waterfall show real speech.
/// <para>Cost: a short list of overs live now, pruned as each ends; a look at one colonist walks it once (a handful at most) and reads their
/// current job. Nothing is saved, nothing ticks.</para>
/// </summary>
public static class RadioAir
{
    private static readonly List<Over> live = new();

    /// <summary>Whether colonists talk on the air by themselves (the setting): with it off, nothing here runs and the lamp works as before.</summary>
    public static bool Live => RadioMod.Settings.talkOnAir;

    public static void Clear() => live.Clear();

    /// <summary>An over starts.</summary>
    public static void Begin(Over over)
    {
        Prune(Find.TickManager.TicksGame);
        live.Add(over);
    }

    /// <summary>An over ends early (a call hung up): it stops being live now.</summary>
    public static void End(Pawn starter)
    {
        int now = Find.TickManager.TicksGame;
        foreach (Over over in live) if (over.Starter == starter && over.End > now) over.End = now;
        Prune(now);
    }

    /// <summary>The over this colonist is in now, starting it or answering, or null.</summary>
    public static Over? Of(Pawn pawn)
    {
        int now = Find.TickManager.TicksGame;
        Prune(now);
        for (int i = live.Count - 1; i >= 0; i--)
        {
            Over over = live[i];
            if (over.Starter == pawn || over.Other == pawn) return over;
        }
        return null;
    }

    /// <summary>The overs a station carries now, for its inspect text.</summary>
    public static IEnumerable<Over> On(Thing station)
    {
        Prune(Find.TickManager.TicksGame);
        foreach (Over over in live)
        {
            foreach (Thing carrier in over.Stations)
            {
                if (carrier != station) continue;
                yield return over;
                break;
            }
        }
    }

    /// <summary>
    /// Whether this colonist is talking on the radio now: in an over, or doing a radio job that is a call (every job that makes a call at a
    /// desk is one, on a hand-held too), or giving or taking advice.
    /// </summary>
    public static bool OnAir(Pawn pawn) => Of(pawn) != null || OnAirByJob(pawn);

    private static bool OnAirByJob(Pawn pawn) =>
        pawn.jobs?.curDriver is IRadioDeskCall || RadioAdvice.CallOf(pawn) != null || RadioAdvice.CallBy(pawn) != null;

    /// <summary>
    /// Whether this colonist is speaking now, rather than listening: in an over the two take turns; on a call to someone off the map, the
    /// colonist speaks every other turn.
    /// </summary>
    public static bool Speaking(Pawn pawn)
    {
        int now = Find.TickManager.TicksGame;
        int turn = RadioRulesDef.Current.talkTurnTicks;
        if (Of(pawn) is { } over)
        {
            bool starterSpeaks = TalkRule.StarterSpeaks(now - over.Start, turn);
            return over.Starter == pawn ? starterSpeaks : !starterSpeaks;
        }
        // Each colonist's turns start at their own point, so two calls are not keyed in step.
        return OnAirByJob(pawn) && TalkRule.StarterSpeaks(now + pawn.thingIDNumber * 37 % System.Math.Max(1, turn * 2), turn);
    }

    private static void Prune(int now)
    {
        for (int i = live.Count - 1; i >= 0; i--)
        {
            Over over = live[i];
            if (over.End <= now || over.Starter.Destroyed) live.RemoveAt(i);
        }
    }
}
