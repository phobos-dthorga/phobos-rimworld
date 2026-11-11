namespace Phobos.Framework.Rules;

/// <summary>
/// A value worked out once and kept for a short while, game-free (Framework 0.94.0, the performance pass): for an answer a screen asks for
/// on every frame that is dear to work out and changes rarely (what a vehicle can be fitted with, which vehicles stand by a dyno bay). The
/// caller gives the time, so it works on real time while the game is paused; <see cref="Drop"/> forgets it at once when something it
/// depends on changes.
/// </summary>
public struct Expiring<T>
{
    T value;
    double until;
    bool held;

    /// <summary>Whether a value is held and still good at this time; the value when it is.</summary>
    public readonly bool TryGet(double now, out T found)
    {
        found = value;
        return held && now < until;
    }

    /// <summary>Holds a value for this long from now, and gives it back.</summary>
    public T Set(T fresh, double now, double seconds)
    {
        value = fresh;
        until = now + seconds;
        held = true;
        return fresh;
    }

    /// <summary>Forgets the value, so the next asking works it out again.</summary>
    public void Drop() => held = false;
}
