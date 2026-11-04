using System.Collections.Generic;
using Verse;

namespace Phobos.Framework.Gases;

/// <summary>The carriers the framework knows. The game's is always there; a tie-in assembly may register another at startup.</summary>
public static class GasCarriers
{
    static readonly List<IGasCarrier> carriers = new() { new VanillaGasCarrier() };

    public static IReadOnlyList<IGasCarrier> All => carriers;

    public static void Register(IGasCarrier carrier)
    {
        if (!carriers.Contains(carrier)) carriers.Add(carrier);
    }

    /// <summary>The thickest reading any carrier gives, 0 to 1.</summary>
    public static float Density(IntVec3 cell, Map map, GasKind kind)
    {
        float best = 0f;
        for (int i = 0; i < carriers.Count; i++)
        {
            float density = carriers[i].Density(cell, map, kind);
            if (density > best) best = density;
        }
        return best;
    }

    /// <summary>Adds to every carrier that accepts the kind, in the same 0 to 1 unit.</summary>
    public static void Add(IntVec3 cell, Map map, GasKind kind, float fraction)
    {
        for (int i = 0; i < carriers.Count; i++)
        {
            if (carriers[i].CanWrite(kind)) carriers[i].Add(cell, map, kind, fraction);
        }
    }

    public static string Describe()
    {
        var names = new List<string>();
        foreach (IGasCarrier carrier in carriers) names.Add(carrier.Name);
        return string.Join(", ", names);
    }
}
