using System.Collections.Generic;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>How a hitch and a trailer join, or that they cannot (Framework 0.182.0).</summary>
public enum CouplingMatch
{
    /// <summary>They cannot join: the trailer's coupling is not one the hitch has, and no adapter fitted turns one into it.</summary>
    None,
    /// <summary>One side names no coupling, so it joins anything: everything built before couplings existed (owner, 20 Nov 2026).</summary>
    Universal,
    /// <summary>The hitch has the trailer's coupling.</summary>
    Direct,
    /// <summary>An adapter fitted to the hitch takes the trailer's coupling.</summary>
    ByAdapter,
}

/// <summary>
/// Whether a hitch and a trailer can couple (Framework 0.182.0; docs/development/farming-machines.md, "Coupling types"). The owner's
/// decisions of 20 Nov 2026: couplings are enforced, an adapter fitted to the hitch fixes a mismatch, and a hitch or trailer that names
/// no coupling (every one built before) joins anything, so no coupled pair in a save is ever broken. Couplings are named by their Defs'
/// names; game-free, so any author's hitch and trailer share it.
/// </summary>
public static class CouplingRule
{
    /// <summary>
    /// How this trailer's coupling joins this hitch: universal when either side names none, direct when the hitch has it, by an adapter
    /// when one fitted takes it, and none otherwise.
    /// </summary>
    /// <param name="permanent">
    /// The couplings that join for good (an articulated vehicle's pivot). Such a coupling joins only a hitch that names it: never a hitch
    /// that names none, and never by an adapter. And a hitch that names nothing but such couplings takes nothing else, not even a trailer
    /// that names none. The fault-read of 21 Nov 2026 (H1, H2): any older truck could take an orphaned rear half and never let it go, and
    /// any older trailer on a front half's pivot stopped its own rear half ever being made.
    /// </param>
    public static CouplingMatch Match(IReadOnlyCollection<string>? hitchCouplings, string? trailerCoupling, IReadOnlyCollection<string>? adaptersTake,
        IReadOnlyCollection<string>? permanent = null)
    {
        bool trailerPermanent = !string.IsNullOrEmpty(trailerCoupling) && Has(permanent, trailerCoupling!);
        if (trailerPermanent) return Has(hitchCouplings, trailerCoupling!) ? CouplingMatch.Direct : CouplingMatch.None;
        if (hitchCouplings != null && hitchCouplings.Count > 0 && permanent != null && permanent.Count > 0 && All(hitchCouplings, permanent))
        {
            return CouplingMatch.None;
        }
        if (string.IsNullOrEmpty(trailerCoupling) || hitchCouplings == null || hitchCouplings.Count == 0) return CouplingMatch.Universal;
        foreach (string coupling in hitchCouplings)
        {
            if (coupling == trailerCoupling) return CouplingMatch.Direct;
        }
        if (adaptersTake != null)
        {
            foreach (string coupling in adaptersTake)
            {
                if (coupling == trailerCoupling) return CouplingMatch.ByAdapter;
            }
        }
        return CouplingMatch.None;
    }

    static bool Has(IReadOnlyCollection<string>? names, string name)
    {
        if (names == null) return false;
        foreach (string each in names)
        {
            if (each == name) return true;
        }
        return false;
    }

    static bool All(IReadOnlyCollection<string> names, IReadOnlyCollection<string> within)
    {
        foreach (string each in names)
        {
            if (!Has(within, each)) return false;
        }
        return true;
    }

    /// <summary>Whether a match lets them couple.</summary>
    public static bool Joins(CouplingMatch match) => match != CouplingMatch.None;
}
