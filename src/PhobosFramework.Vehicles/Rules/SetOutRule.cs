namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Where a trailer's set-out pieces go (Framework 0.121.0; docs/development/vehicle-trailers.md, B0.4), game-free. A piece's cells are given in
/// the trailer's own frame: x counts out from the side the pieces go (0 is the column touching the trailer), z runs along the trailer towards its
/// front. Rotations are the game's: 0 north, 1 east, 2 south, 3 west. Facings: 0 towards the trailer, 1 away from it, 2 to its front, 3 to its back.
/// </summary>
public static class SetOutRule
{
    /// <summary>A cell beside a trailer facing north: <paramref name="side"/> is 1 for its left (west), -1 for its right.</summary>
    public static (int x, int z) NorthFrame(int trailerWidth, int side, int x, int z) => (-side * ((trailerWidth - 1) / 2 + 1 + x), z);

    /// <summary>Turns an offset from a north-facing frame to a rotation, as the game turns a cell offset.</summary>
    public static (int x, int z) Rotate(int x, int z, int rotation) => (((rotation % 4) + 4) % 4) switch
    {
        1 => (z, -x),
        2 => (-x, -z),
        3 => (-z, x),
        _ => (x, z),
    };

    /// <summary>
    /// The rotation a piece takes so its working side looks the way its facing says. The game works a building from the side opposite its rotation
    /// (a stove turned north is worked from the south: its interaction cell is (0, -1) turned by its rotation), so the rotation is the opposite of
    /// the direction the working side looks.
    /// </summary>
    public static int Rotation(int facing, int side, int trailerRotation)
    {
        int looks = facing switch
        {
            0 => side == 1 ? 1 : 3,
            1 => side == 1 ? 3 : 1,
            2 => 0,
            _ => 2,
        };
        return ((looks + 2) % 4 + trailerRotation % 4 + 4) % 4;
    }

    /// <summary>Where the game stands a building's worker, from the building, for a building of the game's usual interaction cell (0, -1).</summary>
    public static (int x, int z) WorkerOffset(int rotation) => Rotate(0, -1, rotation);
}
