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
    public static CouplingMatch Match(IReadOnlyCollection<string>? hitchCouplings, string? trailerCoupling, IReadOnlyCollection<string>? adaptersTake)
    {
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

    /// <summary>Whether a match lets them couple.</summary>
    public static bool Joins(CouplingMatch match) => match != CouplingMatch.None;
}
