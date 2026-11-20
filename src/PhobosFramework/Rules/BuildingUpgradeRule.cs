using System;
using System.Collections.Generic;

namespace Phobos.Framework.Rules;

/// <summary>Why an upgrade cannot be ordered on a building now. Saved nowhere: shown only.</summary>
public enum UpgradeRefusal
{
    None,
    /// <summary>It is fitted already.</summary>
    Fitted,
    /// <summary>The upgrade it builds on is not fitted.</summary>
    NeedsFirst,
    /// <summary>Its research is not done.</summary>
    Research,
    /// <summary>Another upgrade is being fitted or taken off: one piece of work at a time on a building.</summary>
    Busy,
}

/// <summary>Why a fitted upgrade cannot be taken off now.</summary>
public enum UpgradeRemoveRefusal
{
    None,
    NotFitted,
    /// <summary>Another fitted upgrade builds on it: that one comes off first.</summary>
    Needed,
    Busy,
}

/// <summary>
/// Building upgrades, game-free (D'thorga Framework 0.210.0; docs/development/vehicle-service-yard.md, "Building upgrades"; owner, 20 Nov
/// 2026: charging "as an upgrade", through a system in the Framework core other buildings use too): whether one can be ordered or taken
/// off, what is still to be brought, what taking one off gives back, and what a figure reads when several fitted upgrades set it.
/// </summary>
public static class BuildingUpgradeRule
{
    /// <summary>Whether an upgrade can be ordered: the first refusal that applies, in the order a player would want to hear them.</summary>
    public static UpgradeRefusal CanOrder(bool fitted, bool prerequisiteFitted, bool researched, bool anotherOrder)
    {
        if (fitted) return UpgradeRefusal.Fitted;
        if (anotherOrder) return UpgradeRefusal.Busy;
        if (!prerequisiteFitted) return UpgradeRefusal.NeedsFirst;
        if (!researched) return UpgradeRefusal.Research;
        return UpgradeRefusal.None;
    }

    /// <summary>Whether a fitted upgrade can be taken off.</summary>
    public static UpgradeRemoveRefusal CanRemove(bool fitted, bool anotherFittedNeedsIt, bool anotherOrder)
    {
        if (!fitted) return UpgradeRemoveRefusal.NotFitted;
        if (anotherOrder) return UpgradeRemoveRefusal.Busy;
        if (anotherFittedNeedsIt) return UpgradeRemoveRefusal.Needed;
        return UpgradeRemoveRefusal.None;
    }

    /// <summary>What is still to be brought for each line of a cost, given what has been delivered; lines met are left out.</summary>
    public static List<(T what, int count)> Missing<T>(IEnumerable<(T what, int count)> cost, Func<T, int> delivered)
    {
        var missing = new List<(T, int)>();
        foreach ((T what, int count) in cost)
        {
            int still = count - delivered(what);
            if (still > 0) missing.Add((what, still));
        }
        return missing;
    }

    /// <summary>How many of a material taking an upgrade off gives back: a share of what it cost, rounded down, never below nothing.</summary>
    public static int Refund(int count, float share) => count <= 0 || share <= 0f ? 0 : (int)Math.Floor(count * Math.Min(1f, share));

    /// <summary>The work taking an upgrade off takes: a share of fitting it (authored by the upgrade).</summary>
    public static float RemoveWork(float fitWork, float share) => Math.Max(0f, fitWork) * Math.Max(0f, share);

    /// <summary>
    /// What a figure reads with these fitted upgrades setting it: the largest, so two upgrades that each raise a figure never add up past
    /// either (an authored simplification); the building's own value when none sets it.
    /// </summary>
    public static float Figure(IEnumerable<float> setBy, float fallback)
    {
        bool any = false;
        float best = 0f;
        foreach (float value in setBy)
        {
            if (!any || value > best) best = value;
            any = true;
        }
        return any ? best : fallback;
    }

    /// <summary>How far along a piece of work is, from nothing to one.</summary>
    public static float Progress(float done, float total) => total <= 0f ? 1f : Math.Clamp(done / total, 0f, 1f);
}
