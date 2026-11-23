using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Where a vehicle's headlight beam falls, and when it is worth moving, game-free (Framework 0.104.0; owner, 12 Nov 2026: the headlights
/// switch lights the ground). The game's light is a round pool around one cell, so a beam is drawn as one pool whose centre stands a few
/// cells ahead of the vehicle's nose along its heading: from the nose to about twice that distance out is lit. A simplification, labelled:
/// a real low beam is a cone; the distances are authored.
/// </summary>
public static class HeadlightRule
{
    /// <summary>
    /// The cell the beam's pool is centred on: from the middle of the vehicle (<paramref name="centreX"/>, <paramref name="centreZ"/>, in
    /// map units, x east and z north), along <paramref name="heading"/> (degrees, 0 north, 90 east) past the nose
    /// (<paramref name="halfLength"/> from the middle) by <paramref name="ahead"/> cells.
    /// </summary>
    public static (int X, int Z) BeamCell(double centreX, double centreZ, double heading, double halfLength, double ahead)
    {
        double radians = heading * Math.PI / 180.0;
        double reach = Math.Max(0, halfLength) + Math.Max(0, ahead);
        double x = centreX + Math.Sin(radians) * reach;
        double z = centreZ + Math.Cos(radians) * reach;
        return ((int)Math.Floor(x), (int)Math.Floor(z));
    }

    /// <summary>
    /// Whether a lit beam should be looked at again this tick: only every <paramref name="interval"/> ticks, so a moving vehicle moves its
    /// light a few times a second at most (each move makes the game work out the light round it again).
    /// </summary>
    public static bool Due(int lastLookTick, int nowTick, int interval) => nowTick - lastLookTick >= Math.Max(1, interval);

    /// <summary>
    /// Whether another faction's vehicle, simulated on the light tier with no electrics, shows its headlights: at night, only while it
    /// moves (parked vehicles at a base stay dark, so a camp of them costs nothing). Its crew switch them on to drive, whatever the
    /// player's own vehicles default to.
    /// </summary>
    public static bool LiteBurns(bool night, bool moving) => night && moving;

    /// <summary>The charging system's voltage over the battery's nominal one: 14 V on 12 (general).</summary>
    public const double ChargingVolts = 14.0 / 12.0;

    /// <summary>A resting lead-acid battery's voltage over its nominal one, flat and full: about 11.8 V and 12.7 V on 12 (general).</summary>
    public const double FlatVolts = 11.8 / 12.0, FullVolts = 12.7 / 12.0;

    /// <summary>A halogen lamp's light against its voltage, to this power (Framework 0.249.0; general: a tungsten-halogen lamp's output goes
    /// with about the 3.4th power of its voltage). The vehicles' headlights are taken as halogen (authored).</summary>
    public const double LampVoltageExponent = 3.4;

    /// <summary>
    /// How bright the headlights burn against their full brightness (Framework 0.249.0; the owner's go-ahead of 23 Nov 2026, "lights dim
    /// when the alternator can't keep up"): full while the alternator carries every load at its charging voltage; otherwise at the
    /// battery's own voltage, by its charge, to <see cref="LampVoltageExponent"/>: about seven-tenths from a full battery, a little over
    /// half from a nearly flat one. 0 to 1.
    /// </summary>
    public static double LampShare(bool alternatorCarries, double charge)
    {
        if (alternatorCarries) return 1;
        double volts = FlatVolts + (FullVolts - FlatVolts) * Math.Max(0, Math.Min(1, charge));
        return Math.Pow(volts / ChargingVolts, LampVoltageExponent);
    }
}
