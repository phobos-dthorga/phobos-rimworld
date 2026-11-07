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

    /// <summary>Several fires add up; this caps the sum so a wildfire never shows a figure the game's display cannot hold.</summary>
    public static float Cap(float totalRise, float maxRise) => totalRise > maxRise ? maxRise : totalRise;
}
