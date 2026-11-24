namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Remote guns (Framework 0.280.0; docs/development/autonomous-vehicles.md, step 6; the owner's cards of 24 and 25 Nov 2026). A remote gun is
/// a turret no seat works, as GTek's L-ATV has, on a vehicle that carries the remote control kit. Game-free and tested.
/// </summary>
public static class RemoteGunRule
{
    /// <summary>
    /// Whether a turret no seat works may fire. On a vehicle without the kit it fires as its author built it, with nobody aboard. With the
    /// kit, only while a station's operator has the vehicle's picture, or always when the setting lets remote guns fire on their own
    /// (owner: "Picture link, or the setting").
    /// </summary>
    public static bool MayFire(bool kitFitted, bool picture, bool fireAlone) => !kitFitted || picture || fireAlone;
}
