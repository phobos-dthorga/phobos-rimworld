using System;
using System.Collections.Generic;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Which of a slot's pictures a vehicle shows, by the upgrades fitted (owner, 12 Nov 2026: the generator set's stock fan and its heavy-duty
/// fan; also the tipper's plain box against its rock and bulk bodies, and the drill rig's folded mast against its tall one). Game-free.
/// A slot lists its pictures in order: the first whose node is fitted wins, and a picture with no node is the stock look, shown when none of
/// the others is fitted. A slot with no stock look shows nothing until one of its nodes is fitted.
/// </summary>
public static class NodeLookRule
{
    /// <summary>The index of the picture to show, or -1 for none. <paramref name="nodes"/> holds each picture's node key, null for stock.</summary>
    public static int Choose(IReadOnlyList<string?> nodes, Func<string, bool> fitted)
    {
        var met = new bool?[nodes.Count];
        for (int i = 0; i < nodes.Count; i++) met[i] = nodes[i] is string node ? fitted(node) : null;
        return Choose(met);
    }

    /// <summary>
    /// The index of the picture to show, or -1 for none, when each picture's condition is already known: true or false for a picture shown
    /// by something fitted (an upgrade node, or a fitting since Framework 0.105.0: the recovery winch, the auxiliary tank), null for the stock
    /// look.
    /// </summary>
    public static int Choose(IReadOnlyList<bool?> met)
    {
        int stock = -1;
        for (int i = 0; i < met.Count; i++)
        {
            if (met[i] is not bool fitted)
            {
                if (stock < 0) stock = i;
            }
            else if (fitted)
            {
                return i;
            }
        }
        return stock;
    }
}
