namespace Phobos.Framework.Rules;

/// <summary>Where a consumer should read a gas from.</summary>
public enum GasSource
{
    /// <summary>Nothing carries this gas right now: infer the effect, or do nothing.</summary>
    None,
    /// <summary>An effect or gas that already exists (the game's, or another mod's) carries it.</summary>
    Existing,
    /// <summary>The framework's own room gas carries it.</summary>
    Own,
}

/// <summary>The player's override on the owner's rule.</summary>
public enum GasSourceMode
{
    /// <summary>Existing effects where present, own gases where nothing exists (the owner's rule).</summary>
    Automatic,
    /// <summary>Every framework gas runs, even where an existing effect covers it.</summary>
    AlwaysOwn,
    /// <summary>Own gases off; consumers fall back to inference.</summary>
    ExistingOnly,
}

/// <summary>
/// The owner's rule (4 Nov 2026): reuse an existing gas or effect where one is present; add our own only where its value is
/// greater than inference could give or nothing exists to infer from; keep a manual override. Game-free.
/// </summary>
public static class SourceRule
{
    /// <param name="mode">The player's setting.</param>
    /// <param name="existingPresent">Whether an existing gas or effect covers this gas right now.</param>
    /// <param name="alwaysOwn">The gas Def's pin: this gas is always ours, whatever the mode.</param>
    public static GasSource Decide(GasSourceMode mode, bool existingPresent, bool alwaysOwn)
    {
        if (alwaysOwn) return GasSource.Own;
        switch (mode)
        {
            case GasSourceMode.AlwaysOwn:
                return GasSource.Own;
            case GasSourceMode.ExistingOnly:
                return existingPresent ? GasSource.Existing : GasSource.None;
            default:
                return existingPresent ? GasSource.Existing : GasSource.Own;
        }
    }
}
