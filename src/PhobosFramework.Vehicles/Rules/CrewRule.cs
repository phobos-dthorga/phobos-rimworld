namespace Phobos.Framework.Vehicles.Rules;

/// <summary>Why a crew would leave its vehicle.</summary>
public enum CrewDanger
{
    None,
    /// <summary>The vehicle is on fire.</summary>
    Burning,
    /// <summary>It can no longer move and has no weapon that can fire: a dead hull.</summary>
    KnockedOut,
}

/// <summary>
/// When a crew leaves its vehicle (owner, 16 Nov 2026; docs/development/vehicle-knocked-out-systems.md, C2). Game-free.
/// </summary>
public static class CrewRule
{
    /// <summary>
    /// The danger the crew is in, if any. A vehicle that was never meant to move (a towed gun, an emplacement) is not "knocked out" by
    /// standing still; a vehicle with no weapons at all is, once it cannot move. Fire comes first: a burning vehicle is left whatever else.
    /// </summary>
    public static CrewDanger Danger(bool crewAboard, bool burning, bool designMoves, bool canMove, bool anyWeaponWorks)
    {
        if (!crewAboard) return CrewDanger.None;
        if (burning) return CrewDanger.Burning;
        if (designMoves && !canMove && !anyWeaponWorks) return CrewDanger.KnockedOut;
        return CrewDanger.None;
    }
}
