using System;

namespace Phobos.Radio.Physics;

/// <summary>
/// Life on the air (Radio 0.63.0; docs/development/radio-life-on-the-air.md, slice 1, the owner's cards of 28 Nov 2026): when colonists talk
/// on the radio by themselves, whom they call, and whose turn it is to speak. Game-free; compiled into the tests. Every figure it is given is
/// the rules Def's, authored.
/// </summary>
public static class TalkRule
{
    /// <summary>
    /// The chance that one look starts a chat, for someone whose chats come <paramref name="gapHours"/> apart on average: the one named roll of
    /// the talk pass (the owner's rule: rolls are a last resort, and named). Zero with no gap.
    /// </summary>
    public static double ChancePerLook(int lookTicks, double gapHours, int ticksPerHour)
    {
        if (gapHours <= 0 || lookTicks <= 0 || ticksPerHour <= 0) return 0;
        return 1 - Math.Exp(-lookTicks / (gapHours * ticksPerHour));
    }

    /// <summary>Whether an hour of the day (0 to 23) falls in [<paramref name="from"/>, <paramref name="to"/>), the span crossing midnight when
    /// <paramref name="from"/> is the later hour (the night).</summary>
    public static bool InHours(int hour, int from, int to)
    {
        if (from == to) return false;
        return from < to ? hour >= from && hour < to : hour >= from || hour < to;
    }

    /// <summary>How close two people are, for an evening call home: the closest tie decides. Lower is closer.</summary>
    public enum Tie
    {
        Partner = 0,
        Family = 1,
        Friend = 2,
        None = 3,
    }

    /// <summary>The tie between two people: a partner, family by blood, a friend by opinion, or none.</summary>
    public static Tie TieOf(bool partner, bool family, int opinion, int friendOpinion)
    {
        if (partner) return Tie.Partner;
        if (family) return Tie.Family;
        return opinion >= friendOpinion ? Tie.Friend : Tie.None;
    }

    /// <summary>
    /// How likely a colonist is to call someone for a chat, by what each thinks of the other: nobody calls a person either can't stand; a liked
    /// one is called more often. A weight of zero is never chosen.
    /// </summary>
    public static double ChatWeight(int opinionOf, int opinionFrom, int floorOpinion)
    {
        if (opinionOf < floorOpinion || opinionFrom < floorOpinion) return 0;
        // 1 for someone barely tolerated, rising to 3 for someone much liked (opinion 100).
        return 1 + 2 * Math.Clamp((opinionOf + opinionFrom) / 200.0, 0, 1);
    }

    /// <summary>
    /// Whether the one who started an over is speaking now (true) or the other is answering (false): they take turns of
    /// <paramref name="turnTicks"/>, from <paramref name="sinceStart"/> ticks into the over.
    /// </summary>
    public static bool StarterSpeaks(int sinceStart, int turnTicks)
    {
        if (turnTicks <= 0) return true;
        return Math.Max(0, sinceStart) / turnTicks % 2 == 0;
    }

    /// <summary>Whether enough time has passed since a thing last happened (<paramref name="lastTick"/>, or never when negative).</summary>
    public static bool Since(int lastTick, int now, int gapTicks) => lastTick < 0 || now - lastTick >= gapTicks;
}
