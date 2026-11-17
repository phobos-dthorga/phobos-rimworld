using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// One "any one of" link on an upgrade tree: <see cref="Node"/> can be fitted once any one of <see cref="AnyOf"/> is fitted. With
/// <see cref="HoldsParent"/>, the fitted parent cannot come off while the node is fitted and no other of its parents is (the tanker's
/// compartments are inside its tank); without it, the parents swap freely and the node stays (a hose reel is on the truck, not the tank).
/// </summary>
public sealed record UpgradeAnyOfLink(string Node, IReadOnlyList<string> AnyOf, bool HoldsParent);

/// <summary>
/// Vehicle Framework's prerequisites all have to be fitted; this rule adds "any one of" (owner, 17 Nov 2026: the tanker's tree should
/// branch, and its tanks are alternatives). Game-free.
/// </summary>
public static class UpgradeAnyOfRule
{
    /// <summary>The first link of <paramref name="node"/> that none of its parents meets, or null when every link is met (or it has none).</summary>
    public static UpgradeAnyOfLink? Unmet(string node, IEnumerable<UpgradeAnyOfLink> links, Func<string, bool> fitted) =>
        links.FirstOrDefault(link => link.Node == node && !link.AnyOf.Any(fitted));

    /// <summary>The node that holds <paramref name="parent"/> on: fitted, holding its parent, and with no other of its parents fitted. Null when
    /// the parent may come off.</summary>
    public static string? HeldBy(string parent, IEnumerable<UpgradeAnyOfLink> links, Func<string, bool> fitted)
    {
        if (!fitted(parent)) return null;
        foreach (UpgradeAnyOfLink link in links)
        {
            if (!link.HoldsParent || !fitted(link.Node) || !link.AnyOf.Contains(parent)) continue;
            if (!link.AnyOf.Any(other => other != parent && fitted(other))) return link.Node;
        }
        return null;
    }
}
