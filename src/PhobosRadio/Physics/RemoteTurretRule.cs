namespace Phobos.Radio.Physics;

/// <summary>
/// The base turret's firing rule (Radio 0.18.0; docs/development/autonomous-vehicles.md, the base turret; the owner's card of 25 Nov 2026,
/// "Picture link, or the setting", one rule for every remote gun). Game-free and tested.
/// </summary>
public static class RemoteTurretRule
{
    /// <summary>It fires while a station's operator watches it (has its picture), or always when remote guns may fire on their own.</summary>
    public static bool MayFire(bool watched, bool fireAlone) => watched || fireAlone;

    /// <summary>
    /// Whether a turret on a station's feedline has one of its operator's hands (the owner's card of 26 Nov 2026: one each, as a driven
    /// vehicle has): the station's vehicles take theirs first, then its turrets in their order. <paramref name="place"/> counts the hands
    /// already taken before this turret.
    /// </summary>
    public static bool HasHand(bool operatorAtConsole, int place, int handsPerOperator) => operatorAtConsole && place >= 0 && place < handsPerOperator;
}
