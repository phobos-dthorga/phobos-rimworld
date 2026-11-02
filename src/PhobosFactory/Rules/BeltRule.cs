namespace Phobos.Factory.Rules;

/// <summary>
/// The rules for VFE Factory's belts that need no game types. The switch, "Belts may take from here", is on every
/// storage building and means one thing to the player, but it acts in two ways. Ordinary storage starts off, and
/// switching it on adds it to what belts take from. Hoppers, and storage flagged as one, start on, because VFE
/// Factory feeds belts from them by itself, and switching one off takes it away.
///
/// When a VFE Factory belt is stuck for good. Building_Conveyor.Tick returns at once while the belt is empty and
/// Waiting, and the belt pulls from a hopper only while it is not Waiting, so nothing on the belt itself can move
/// it again: only an item pushed in by another belt does. VFE Factory sets Waiting only while a belt holds
/// something, so the state means something outside the belt emptied it. Read from VFE Factory's decompiled
/// assembly, 30 Oct 2026. No game types: this file is compiled into the unit tests too.
/// </summary>
public static class BeltRule
{
    /// <summary>Whether a belt is stuck: Waiting while it holds nothing.</summary>
    public static bool IsStuck(bool waiting, int heldStacks) => waiting && heldStacks == 0;

    /// <summary>Whether this is ordinary storage the player has switched on, which this mod feeds belts from.</summary>
    public static bool IsSwitchedOn(bool mayTake, bool startsOn) => mayTake && !startsOn;

    /// <summary>Whether this is a hopper the player has switched off, which belts must leave alone.</summary>
    public static bool RefusesBelts(bool mayTake, bool startsOn) => !mayTake && startsOn;

    /// <summary>Whether inspect text mentions the switch: only when it differs from where it started.</summary>
    public static bool ShowsInInspect(bool mayTake, bool startsOn) => mayTake != startsOn;
}
