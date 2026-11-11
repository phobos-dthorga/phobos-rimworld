namespace Phobos.Framework.Rules;

/// <summary>What to say in the log about a fault that has just happened again.</summary>
public enum FaultSay
{
    /// <summary>Nothing: it has been said.</summary>
    Nothing,
    /// <summary>The first time: the whole fault.</summary>
    Full,
    /// <summary>A short reminder of how often it has happened.</summary>
    Reminder,
}

/// <summary>
/// One thing that can fail over and over (a screen drawn sixty times a second), game-free (Framework 0.94.0, the screen guard): after a
/// fault it rests for a while, so it is not tried again every frame, and its fault is logged in full once and then only as a reminder
/// every so many times. The game keeps only 10,000 log messages a session, and a screen failing every frame would use them in minutes.
/// </summary>
public sealed class FaultLatch
{
    double restUntil = double.MinValue;

    /// <summary>How many times it has failed.</summary>
    public int Count { get; private set; }

    /// <summary>Whether it is resting at this time, and should not be tried.</summary>
    public bool Resting(double now) => now < restUntil;

    /// <summary>Notes a fault at this time: it rests for <paramref name="restSeconds"/>, and the answer says what to log.</summary>
    public FaultSay Note(double now, double restSeconds, int remindEvery)
    {
        restUntil = now + restSeconds;
        Count++;
        if (Count == 1) return FaultSay.Full;
        return remindEvery > 0 && Count % remindEvery == 0 ? FaultSay.Reminder : FaultSay.Nothing;
    }
}
