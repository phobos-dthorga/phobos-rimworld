namespace Phobos.VisitSettlementsPatches.Rules;

/// <summary>Why a visited settlement's map stays open when Visit Settlements would close it. None lets it close.</summary>
public enum KeepReason
{
    None,
    /// <summary>A battle (a RimPacts defence or watched battle) is being fought on the map; RimPacts keeps such maps open itself.</summary>
    Battle,
    /// <summary>A free colonist on the map, on foot, is still conscious.</summary>
    ColonistUp,
    /// <summary>A free colonist inside a vehicle, shuttle or pod on the map is still conscious.</summary>
    CrewUp,
    /// <summary>One of the player's mechanoids on the map is still conscious.</summary>
    MechUp,
}

/// <summary>
/// Whether a visited settlement's map should stay open when a pawn on it goes down. Game-free.
///
/// Visit Settlements (by Nina.Goblin; read in its 1.7 build of 22 Oct 2026) closes the map of a settlement you visited the moment any
/// pawn on it goes down, if every spawned free colonist there is downed. It counts only colonists standing on the map, so with all
/// of them inside a vehicle the list is empty, "every one of none is downed" is true, and the first raider, guard or animal to fall
/// closes the map. The game then sends everyone left on it to the world as lost (owner's Barracuda's Stream defence, 8 Nov 2026: four
/// colonists in a tank lost with it, three mechs later taken by other factions, and RimPacts counted the defence lost).
///
/// The map stays open while the player can still act there, or while a battle is fought on it: agent choices of 8 Nov 2026 for the
/// owner. When nobody of the player's is left conscious and no battle is on, Visit Settlements' own rule stands.
/// </summary>
public static class VisitMapRule
{
    public static KeepReason Decide(bool battleRunning, int colonistsUpOnFoot, int crewUp, int mechsUp)
    {
        if (battleRunning) return KeepReason.Battle;
        if (colonistsUpOnFoot > 0) return KeepReason.ColonistUp;
        if (crewUp > 0) return KeepReason.CrewUp;
        if (mechsUp > 0) return KeepReason.MechUp;
        return KeepReason.None;
    }
}
