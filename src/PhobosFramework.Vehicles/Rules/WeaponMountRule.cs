using System;
using System.Collections.Generic;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Which weapons are tied to a mount, and which part is the mount (owner, 16 Nov 2026; docs/development/vehicle-knocked-out-systems.md,
/// T1, T9, T10). Game-free.
/// </summary>
public static class WeaponMountRule
{
    /// <summary>
    /// Whether a weapon gets our mount requirement: only where its author set none (an author's own rule always stands), never on a
    /// weapon mounted on another (it already stops with its parent), and never on a fixed launcher bolted to the hull (smoke dischargers).
    /// </summary>
    public static bool NeedsMount(bool authorRequirement, bool attachedToAnother, bool fixedMount) =>
        !authorRequirement && !attachedToAnother && !fixedMount;

    /// <summary>
    /// The index in <paramref name="partKeys"/> of the vehicle's own part to use as the mount: the first of
    /// <paramref name="mountKeys"/>, in that order, that the vehicle has (case does not matter); -1 when it has none, and one is added.
    /// </summary>
    public static int AuthorMount(IReadOnlyList<string> partKeys, IReadOnlyList<string> mountKeys)
    {
        foreach (string wanted in mountKeys)
        {
            for (int i = 0; i < partKeys.Count; i++)
            {
                if (string.Equals(partKeys[i], wanted, StringComparison.OrdinalIgnoreCase)) return i;
            }
        }
        return -1;
    }
}
