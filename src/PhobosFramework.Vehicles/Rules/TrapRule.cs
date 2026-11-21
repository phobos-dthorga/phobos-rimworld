namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Whether a trap may go off under a vehicle, game-free (Framework 0.213.0; docs/development/vehicle-traps.md). An aircraft in the air passes
/// over every trap (Framework 0.171.1). A vehicle of the player's passes over every trap unless it is on a drive the player ordered (owner,
/// 21 Nov 2026: vehicles driven without the player's orders kept setting off the colony's traps; a setting, on by default). Everything else,
/// a raider's vehicle above all, meets a trap as the game decides.
/// </summary>
public static class TrapRule
{
    public enum Outcome
    {
        /// <summary>The game rolls the trap's chance as for any pawn.</summary>
        AsTheGame,
        /// <summary>An aircraft in the air: nothing touches the ground.</summary>
        Overflown,
        /// <summary>The player's vehicle on a drive the player did not order: an errand, a tow, a push, a caravan setting off.</summary>
        PassedOver,
    }

    public static Outcome For(bool inAir, bool players, bool settingOn, bool onPlayersOrder)
    {
        if (inAir) return Outcome.Overflown;
        if (players && settingOn && !onPlayersOrder) return Outcome.PassedOver;
        return Outcome.AsTheGame;
    }
}
