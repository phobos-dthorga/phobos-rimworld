namespace Phobos.Framework.Rules;

/// <summary>
/// When a vehicle lays its network pieces (pipes, conduits) beneath it, game-free (Framework 0.108.0; owner, 13 Nov 2026: "only appear when the vehicle is
/// powered off ... performant that way", and "should appear though regardless, so players are not confused";
/// docs/development/network-underlays.md). A vehicle of the player's, standing on a map and not moving, lays them across its whole footprint while its
/// engine is off, whether or not a run touches it, and takes them up the moment its engine starts or it moves. A vehicle set up for work keeps the pieces
/// its work runs through while its engine turns it (a tanker's pump, a rig's outlet, the generator set's conduits).
/// </summary>
public static class UnderlayRule
{
    /// <summary>Whether the vehicle's pieces should lie beneath it now.</summary>
    public static bool Lays(bool spawned, bool players, bool moving, bool engineOn, bool setUp) =>
        spawned && players && !moving && (!engineOn || setUp);

    /// <summary>
    /// Whether a piece found on a thing's own footprint is taken back as the thing's own, once, after a load: only a piece of the old hidden kind earlier
    /// versions laid, on a cell the thing holds nothing on, by an underlay saved before Framework 0.108.0 (one that never adopted). A loading fault in
    /// Framework 0.107.0 lost track of such pieces; they stood orphaned under the things that laid them.
    /// </summary>
    public static bool Adopts(bool adoptedBefore, bool oldHiddenKind, bool cellAlreadyHeld) => !adoptedBefore && oldHiddenKind && !cellAlreadyHeld;
}
