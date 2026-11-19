namespace Phobos.Framework.Rules;

/// <summary>How far a mod's unasked-for messages are quietened (the vehicle message level; owner's choice of 20 Nov 2026,
/// docs/development/settings-pages.md).</summary>
public enum MessageLevel
{
    /// <summary>Every message shows, as before.</summary>
    All,
    /// <summary>Only troubles show: breakdowns, fires, damage, losses and threats.</summary>
    TroublesOnly,
    /// <summary>No unasked-for message shows; letters still do.</summary>
    LettersOnly,
}

/// <summary>What a message is, by the game's message type.</summary>
public enum MessageKind
{
    /// <summary>An answer to the player's own order: a refusal saying why, a caution, a quiet confirmation. Never hidden.</summary>
    Reply,
    /// <summary>News nobody needs to act on: something finished, something neutral or good.</summary>
    Routine,
    /// <summary>Something wrong, or a threat. Also any message type the rule does not know.</summary>
    Trouble,
}

/// <summary>
/// Which messages a level hides. Game-free: the caller maps the game's message type to a kind (<see cref="KindOf"/>, by its defName)
/// and says whether the player's own click caused it. Whatever the player asked for, and any reply, always shows: a refusal must say
/// why (AGENTS.md), so hiding one would be a silent refusal.
/// </summary>
public static class MessageLevelRule
{
    public static MessageKind KindOf(string? messageTypeDefName) => messageTypeDefName switch
    {
        "RejectInput" or "SilentInput" or "CautionInput" => MessageKind.Reply,
        "NeutralEvent" or "PositiveEvent" or "TaskCompletion" => MessageKind.Routine,
        _ => MessageKind.Trouble,
    };

    /// <summary>Whether this message is hidden at this level. <paramref name="askedFor"/>: the player's own click or order caused it.</summary>
    public static bool Hides(MessageLevel level, MessageKind kind, bool askedFor)
    {
        if (askedFor || kind == MessageKind.Reply) return false;
        return level switch
        {
            MessageLevel.TroublesOnly => kind == MessageKind.Routine,
            MessageLevel.LettersOnly => true,
            _ => false,
        };
    }
}
