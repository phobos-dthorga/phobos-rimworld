namespace Phobos.Framework.Rules;

/// <summary>
/// The game-free half of keeping the game's log alive (docs/development/logging-framework-design.md): how many times a session it
/// may be turned back on. Message signatures and their tally are in the shared logging core.
/// </summary>
public static class GameLogRule
{
    /// <summary>Times a session the log is turned back on after the game's 10,000-message limit; then it stays off, to spare the disk.
    /// Agent default (owner may revise): 25 times is about a quarter of a million messages.</summary>
    public const int MaxRevivals = 25;

    public static bool MayRevive(int revivalsSoFar) => revivalsSoFar < MaxRevivals;
}
