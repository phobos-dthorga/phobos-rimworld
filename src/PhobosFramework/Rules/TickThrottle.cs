namespace Phobos.Framework.Rules;

/// <summary>
/// "At most once per so many ticks", game-free: whether something last done at <c>lastTick</c> may be done again at <c>now</c>. A
/// negative last tick means never done; a last tick later than now (a different game was loaded) counts as never done too, so a
/// throttle kept in a static field does not stay shut after a load. Our mods had written this by hand three times (a log line once
/// an in-game hour per map, once a day per fault); the shared logger (docs/development/logging-framework-design.md) will build on it.
/// </summary>
public static class TickThrottle
{
    public const int Never = -1;

    public static bool Due(int now, int lastTick, int intervalTicks) =>
        lastTick < 0 || now < lastTick || now - lastTick >= intervalTicks;
}
