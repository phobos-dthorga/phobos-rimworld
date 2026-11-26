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
