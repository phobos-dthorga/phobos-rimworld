using RimWorld;
using Verse;

namespace Phobos.Framework.Mood;

/// <summary>
/// Talk between two people the game's own chat cannot reach (Framework 0.379.0; docs/development/radio-life-on-the-air.md, RN7). The game
/// lets two people chat only when both stand on one map a few cells apart (<c>Pawn_InteractionsTracker.CanInteractNowWith</c>), so riders
/// aboard a vehicle and colonists talking by radio never did. This gives both the interaction's own memories and writes the game's own line
/// in the social log, and nothing else: no worker runs, so an insult here never starts a fight. First used by the vehicles' chat aboard
/// (Framework 0.292.0), then by the radio's talk on the air.
/// </summary>
public static class TalkApart
{
    /// <summary>Two people talk: <paramref name="first"/> starts it, <paramref name="second"/> answers.</summary>
    public static void Interact(Pawn first, Pawn second, InteractionDef interaction)
    {
        if (interaction.initiatorThought != null) first.needs?.mood?.thoughts?.memories?.TryGainMemory(interaction.initiatorThought, second);
        if (interaction.recipientThought != null) second.needs?.mood?.thoughts?.memories?.TryGainMemory(interaction.recipientThought, first);
        Find.PlayLog.Add(new PlayLogEntry_Interaction(interaction, first, second, null));
    }
}
