using System;
using System.Collections.Generic;

namespace Phobos.Framework.Rules;

/// <summary>One line of a cost: a thing, by its name, and how many.</summary>
public readonly record struct BillLine(string Thing, int Count);

/// <summary>Another mod's part offered for a role, present only while its mod is active: how many stand for one of the role, and the
/// sizes of built thing it suits (NaN leaves that end open).</summary>
public readonly record struct BillSubstitute(string Thing, int Count, double MinSize = double.NaN, double MaxSize = double.NaN);

/// <summary>What can fill one role: ours (an item, or materials) and the other mods' parts on offer, in order of preference.</summary>
public sealed class BillOffer
{
    /// <summary>Our own part for the role, or null when ours is materials.</summary>
    public string? OwnItem;
    public int OwnItemCount = 1;
    /// <summary>Ours as materials, used when there is no item of ours.</summary>
    public IReadOnlyList<BillLine> OwnMaterials = Array.Empty<BillLine>();
    /// <summary>Ours is a kit that covers the role for the whole build, however many of the role the bill asks for.</summary>
    public bool OwnCoversWholeBuild;
    public IReadOnlyList<BillSubstitute> Substitutes = Array.Empty<BillSubstitute>();
}

/// <summary>Where a role's parts came from, for a report.</summary>
public enum BillSource
{
    /// <summary>Our own item or materials.</summary>
    Ours,
    /// <summary>Another mod's part.</summary>
    Substitute,
}

/// <summary>
/// The parts bill (Framework 0.95.0; owner, 11 Nov 2026: every vehicle's build cost by role, generic so a later mod's placed buildings
/// use it too), game-free. A built thing lists the roles it needs and how many of each; each role is filled by another mod's part where
/// one is on offer and suits the thing's size, and by ours otherwise; what comes out is merged into the thing's own shell cost.
/// </summary>
public static class BuildBillRule
{
    /// <summary>Whether a size sits in a window. An open end is NaN; an unknown size suits only a window open at both ends.</summary>
    public static bool Suits(double size, double minSize, double maxSize) =>
        (double.IsNaN(minSize) || size >= minSize) && (double.IsNaN(maxSize) || size < maxSize);

    /// <summary>
    /// How many of our own item stand for one of a role on a thing of this size: the count of the last step the size reaches, else the
    /// plain count. A bigger thing takes more of the one part we make, where another mod makes a bigger part. Steps are in rising order;
    /// an unknown size takes the plain count.
    /// </summary>
    public static int CountAt(double size, int plainCount, IReadOnlyList<(double MinSize, int Count)> steps)
    {
        int count = plainCount;
        if (double.IsNaN(size)) return count;
        foreach ((double minSize, int stepCount) in steps)
        {
            if (size >= minSize) count = stepCount;
        }
        return count;
    }

    /// <summary>
    /// What <paramref name="count"/> of a role costs: the first substitute that suits <paramref name="size"/> when
    /// <paramref name="othersAllowed"/>, else ours. An offer with nothing of ours and no substitute that suits gives nothing.
    /// </summary>
    public static List<BillLine> Fill(BillOffer offer, int count, double size, bool othersAllowed, out BillSource source)
    {
        var lines = new List<BillLine>();
        source = BillSource.Ours;
        if (count <= 0) return lines;
        if (othersAllowed)
        {
            foreach (BillSubstitute substitute in offer.Substitutes)
            {
                if (substitute.Count <= 0 || !Suits(size, substitute.MinSize, substitute.MaxSize)) continue;
                source = BillSource.Substitute;
                lines.Add(new BillLine(substitute.Thing, substitute.Count * count));
                return lines;
            }
        }
        int times = offer.OwnCoversWholeBuild ? 1 : count;
        if (offer.OwnItem != null)
        {
            if (offer.OwnItemCount > 0) lines.Add(new BillLine(offer.OwnItem, offer.OwnItemCount * times));
            return lines;
        }
        foreach (BillLine material in offer.OwnMaterials)
        {
            if (material.Count > 0) lines.Add(new BillLine(material.Thing, material.Count * times));
        }
        return lines;
    }

    /// <summary>
    /// The shell's cost with the parts added: the shell's lines keep their order (its first stays first, which some readers of a cost
    /// rely on), a part the shell already names is added to that line, and new things follow in the order they came.
    /// </summary>
    public static List<BillLine> Merge(IReadOnlyList<BillLine> shell, IEnumerable<BillLine> parts)
    {
        var merged = new List<BillLine>(shell.Count + 8);
        var index = new Dictionary<string, int>(StringComparer.Ordinal);
        void Add(BillLine line)
        {
            if (line.Count <= 0) return;
            if (index.TryGetValue(line.Thing, out int at)) merged[at] = new BillLine(line.Thing, merged[at].Count + line.Count);
            else
            {
                index[line.Thing] = merged.Count;
                merged.Add(line);
            }
        }
        foreach (BillLine line in shell) Add(line);
        foreach (BillLine line in parts) Add(line);
        return merged;
    }
}
