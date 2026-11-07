namespace Phobos.BurnsAndSmoke.Rules;

/// <summary>
/// How much hotter than the air a cell is for being near a fire in the open. Game-free. The game itself gives a fire's heat to
/// the whole room and throws it away outdoors; this rule is the local hot zone the room model cannot give. The shape is authored:
/// a peak at the flame for a full-size fire, scaled by the fire's size, falling with the square of (distance + 1) so one cell
/// away is a quarter of the peak and four cells away a twenty-fifth. The figures are starting values in the rules Def.
/// </summary>
public static class FireHeatRule
{
    /// <summary>Degrees above the air at <paramref name="distance"/> cells from one fire of <paramref name="fireSize"/>.</summary>
    public static float Rise(float fireSize, float fullFireSize, float distance, float peakRisePerFullFire, float radiusCells)
    {
        if (fireSize <= 0f || fullFireSize <= 0f || peakRisePerFullFire <= 0f || distance < 0f || distance > radiusCells) return 0f;
        float size = fireSize / fullFireSize;
        if (size > 1f) size = 1f;
        float spread = distance + 1f;
        return peakRisePerFullFire * size / (spread * spread);
    }

    /// <summary>
    /// The share of a fire's local heat a room adds on top of its own temperature. The game spreads a fire's heat evenly over its
    /// room, so a small room already shows it: nothing extra up to <paramref name="evenlyWarmedCells"/>, then the part of the room
    /// the even spread misses, approaching all of it in a hangar. Authored.
    /// </summary>
    public static float IndoorShare(int roomCells, float evenlyWarmedCells)
    {
        if (roomCells <= 0) return 0f;
        if (evenlyWarmedCells <= 0f) return 1f;
        if (roomCells <= evenlyWarmedCells) return 0f;
        return 1f - evenlyWarmedCells / roomCells;
    }

    /// <summary>The game burns a pawn when the air is this many degrees above the top of their comfortable range (HediffGiver_Heat).</summary>
    public const float GameBurnMarginAboveComfort = 150f;

    /// <summary>Whether air at <paramref name="cellTemperature"/> burns a pawn whose comfortable range tops out at <paramref name="comfortableMax"/>.</summary>
    public static bool BurnsAt(float cellTemperature, float comfortableMax) => cellTemperature > comfortableMax + GameBurnMarginAboveComfort;

    /// <summary>Several fires add up; this caps the sum so a wildfire never shows a figure the game's display cannot hold.</summary>
    public static float Cap(float totalRise, float maxRise) => totalRise > maxRise ? maxRise : totalRise;
}
