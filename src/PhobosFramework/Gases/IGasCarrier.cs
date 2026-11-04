using Verse;

namespace Phobos.Framework.Gases;

/// <summary>Something that already carries a gas: the game's gas grid, or another mod's. The framework reads all of them
/// and writes to those that accept it.</summary>
public interface IGasCarrier
{
    string Name { get; }

    /// <summary>Density of a gas at a cell, 0 (none) to 1 (as thick as the carrier allows).</summary>
    float Density(IntVec3 cell, Map map, GasKind kind);

    bool CanWrite(GasKind kind);

    /// <summary>Adds gas at a cell, in the same 0 to 1 unit; the carrier spreads it as it would its own.</summary>
    void Add(IntVec3 cell, Map map, GasKind kind, float fraction);
}
