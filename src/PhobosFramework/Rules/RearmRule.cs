namespace Phobos.Framework.Rules;

/// <summary>
/// Which ammunition a magazine may take when it can hold several kinds. Swapping a kind empties the magazine, so a kind is only changed on purpose: rounds already loaded decide what is
/// brought, and with an empty magazine the first kind on its way does. <typeparamref name="T"/> is the game's ammunition Def; generic so these can be tested without the game.
/// </summary>
public static class RearmRule
{
    /// <summary>The kind in the magazine that must be kept, or null if nothing has to be kept: empty, or the player has switched that kind off.</summary>
    public static T? KindToKeep<T>(T? loaded, int rounds, bool loadedAllowed) where T : class => loaded != null && rounds > 0 && loadedAllowed ? loaded : null;

    /// <summary>The only kind a colonist may fetch now, or null for any.</summary>
    public static T? KindToBring<T>(T? keep, T? alreadyOnItsWay) where T : class => keep ?? alreadyOnItsWay;

    /// <summary>Whether what has arrived may go in. A different kind goes in only when it is the one the player chose, which is what asks for a swap.</summary>
    public static bool MayLoad<T>(T? keep, T incoming, T? chosen) where T : class => keep == null || ReferenceEquals(keep, incoming) || ReferenceEquals(chosen, incoming);
}
