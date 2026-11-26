using System;

namespace Phobos.Radio.Physics;

/// <summary>
/// Advice by radio (Radio 0.23.0; docs/development/radio-advice-and-relays.md, the owner's cards of 26 Nov 2026): how much of the skill gap
/// between a carer and their adviser the call closes. No game types: compiled into the physics tests too.
/// <para>The owner's card: half the gap on a workable or clean link, a quarter on a marginal one. An aid at hand (a numbered medical chest)
/// adds its own share. The level is rounded down, as the game's skills are whole levels, and never passes the adviser's own.</para>
/// </summary>
public static class AdviceRule
{
    /// <summary>The share of the gap closed on this link: <paramref name="solid"/> on a workable or clean one, <paramref name="marginal"/> on a
    /// marginal one, nothing with no contact; <paramref name="aid"/> is added, and the whole is at most all of it.</summary>
    public static double Share(LinkOutcome outcome, double solid, double marginal, double aid)
    {
        double share = outcome switch
        {
            LinkOutcome.Clean or LinkOutcome.Workable => solid,
            LinkOutcome.Marginal => marginal,
            _ => 0.0,
        };
        return share <= 0.0 ? 0.0 : Math.Min(1.0, share + Math.Max(0.0, aid));
    }

    /// <summary>The level the carer works at, talked through by an adviser of <paramref name="adviser"/> closing <paramref name="share"/> of the
    /// gap; their own when the adviser knows no more.</summary>
    public static int EffectiveLevel(int carer, int adviser, double share)
    {
        if (adviser <= carer || share <= 0.0) return carer;
        int lifted = (int)Math.Floor(carer + share * (adviser - carer) + 1e-9);
        return Math.Min(adviser, Math.Max(carer, lifted));
    }

    /// <summary>A call through two links is as good as the worse of them, as a relayed call is as good as its worst hop.</summary>
    public static LinkOutcome Worse(LinkOutcome a, LinkOutcome b) => a < b ? a : b;
}

/// <summary>How another faction stands with the player, for its doctor's help by radio.</summary>
public enum FactionStanding
{
    Hostile,
    Neutral,
    Ally,
}

/// <summary>What a faction asks for its doctor's help: nothing (no help), free, or goodwill.</summary>
public enum FactionTerms
{
    Refused,
    /// <summary>A call already open: it goes on, at no new cost.</summary>
    Continuing,
    Free,
    Goodwill,
}

/// <summary>
/// Another faction's doctor by radio (Radio 0.26.0; docs/development/radio-advice-and-relays.md, the owner's card of 27 Nov 2026: "Allies
/// free, others goodwill"): whether a faction helps now, and on what terms. No game types: compiled into the physics tests too.
/// <para>A call stays open while it is used, and for <c>windowTicks</c> after its last use, so a convoy's tends an hour apart are one call.
/// An ally opens a new call at most once in <c>allyEveryTicks</c> (the owner's card: once a day), free. A neutral faction opens one for
/// <c>cost</c> goodwill, never taking goodwill below <c>floor</c> (agent default: 0), so the help can never turn a faction hostile.
/// A hostile faction never helps.</para>
/// </summary>
public static class FactionDoctorRule
{
    public static FactionTerms Terms(FactionStanding standing, int goodwill, int cost, int floor, int lastOpened, int lastUse, int now,
        int allyEveryTicks, int windowTicks)
    {
        if (standing == FactionStanding.Hostile) return FactionTerms.Refused;
        if (lastUse >= 0 && now - lastUse < windowTicks) return FactionTerms.Continuing;
        if (standing == FactionStanding.Ally) return lastOpened < 0 || now - lastOpened >= allyEveryTicks ? FactionTerms.Free : FactionTerms.Refused;
        return goodwill - cost >= floor ? FactionTerms.Goodwill : FactionTerms.Refused;
    }

    /// <summary>
    /// The Medicine of a faction's doctor by how advanced the faction is (agent default, authored): none before industry (no radio),
    /// <paramref name="industrial"/> for an industrial faction, <paramref name="spacer"/> for a spacer one, <paramref name="ultra"/> beyond.
    /// The levels of the game's tech: 0 undefined, 1 animal, 2 neolithic, 3 medieval, 4 industrial, 5 spacer, 6 ultra, 7 archotech.
    /// </summary>
    public static int DoctorLevel(int techLevel, int industrial, int spacer, int ultra) => techLevel switch
    {
        < 4 => 0,
        4 => industrial,
        5 => spacer,
        _ => ultra,
    };
}
