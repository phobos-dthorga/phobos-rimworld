namespace Phobos.Framework.Rules;

/// <summary>
/// How enclosed a cell is, 0 (open air) to 1 (a roofed room with no gap in its roof). Consumers use it to infer effects
/// that need a confined space, such as carbon monoxide building up from smoke. Game-free; the framework feeds it the
/// room's figures.
/// </summary>
public static class EnclosureRule
{
    /// <summary>Enclosure of a roofed cell in a room that counts as outdoors (open to the sky or the map edge). Authored.</summary>
    public const float RoofedButOutdoors = 0.25f;

    public static float Compute(bool roofed, bool roomOutdoors, int openRoofCells, int roomCells)
    {
        if (!roofed) return 0f;
        if (roomOutdoors || roomCells <= 0) return RoofedButOutdoors;
        float openFraction = openRoofCells / (float)roomCells;
        if (openFraction < 0f) openFraction = 0f;
        if (openFraction > 1f) openFraction = 1f;
        return 1f - openFraction;
    }
}
