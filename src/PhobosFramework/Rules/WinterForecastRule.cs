using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// When the snow comes or goes at a place, game-free (Framework 0.188.0; docs/development/vehicle-emergent-play.md, EM16: roads that
/// close, told in advance). The game gives each world tile a winter figure for any date, 0 in the warm months to 2 in deep winter; this
/// finds the first day ahead on which it crosses the line that counts as snow on the ground, either way. For anything that plans by the
/// season: a convoy's route, a harvest, a camp.
/// </summary>
public static class WinterForecastRule
{
    /// <summary>The winter figure that counts as snow on the ground: half of a full winter. Authored.</summary>
    public const double SnowLine = 1.0;

    /// <summary>How many days ahead a forecast looks. Authored: half a season of the game's fifteen-day quadrums twice over.</summary>
    public const int HorizonDays = 30;

    /// <summary>
    /// Days until the snow comes (where there is none today) or goes (where there is), or null if nothing changes within the horizon.
    /// <paramref name="winterOnDay"/> gives the winter figure so many days from now; day 0 is today.
    /// </summary>
    public static int? DaysUntilChange(Func<int, double> winterOnDay, out bool snowNow, double snowLine = SnowLine, int horizonDays = HorizonDays)
    {
        snowNow = winterOnDay(0) >= snowLine;
        for (int day = 1; day <= horizonDays; day++)
        {
            if (winterOnDay(day) >= snowLine != snowNow) return day;
        }
        return null;
    }
}
