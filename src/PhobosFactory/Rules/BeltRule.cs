namespace Phobos.Factory.Rules;

/// <summary>
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
}
