namespace Phobos.Framework.Rules;

/// <summary>
/// The arithmetic behind the game's own figures for outside tools (Framework 0.380.0, <c>Debug/GameMeter</c>), game-free. Ticks are
/// counted as the game's tick count moves forward between two frames; a jump back (another save loaded) or a jump of more than
/// <see cref="MaxTicksPerFrame"/> (time advanced by a dev action, or a load) counts as none, so ticks per second stay the
/// simulation's own pace.
/// </summary>
public static class GameMeterRule
{
    /// <summary>
    /// The most ticks one frame can honestly simulate. RimWorld's fastest speed runs up to a few hundred ticks a frame on a small
    /// colony (agent's estimate); an in-game hour (2,500 ticks) in one frame is a jump, not play.
    /// </summary>
    public const int MaxTicksPerFrame = 2500;

    /// <summary>The ticks simulated between two readings of the game's tick count.</summary>
    public static int TicksAdvanced(int before, int now)
    {
        int step = now - before;
        return step > 0 && step <= MaxTicksPerFrame ? step : 0;
    }

    /// <summary>Milliseconds from a stopwatch's ticks at its frequency.</summary>
    public static double Milliseconds(long stopwatchTicks, long frequency) =>
        frequency > 0 ? stopwatchTicks * 1000.0 / frequency : 0.0;
}
