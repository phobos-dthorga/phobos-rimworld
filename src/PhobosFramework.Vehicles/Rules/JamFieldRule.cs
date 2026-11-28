using System.Collections.Generic;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Electronic jamming fields and our systems (Framework 0.225.0; docs/development/ea6b-prowler.md, step 2): whether a point lies inside a field,
/// and whether a capability is one a jammer blinds. Game-free and tested.
/// </summary>
public static class JamFieldRule
{
    /// <summary>Whether a point this far across and along from a field's centre lies inside it (on the ground plane; height is ignored).</summary>
    public static bool Inside(double dx, double dz, double radius) => radius > 0 && dx * dx + dz * dz <= radius * radius;

    /// <summary>
    /// How a jamming aircraft's power is shared between radar and radios (Framework 0.385.0; docs/development/electronic-warfare.md, EW4): the
    /// share of its pods' power on radar, the share on radios, and whether a separate communications jammer (the Prowler's ICAP III, the
    /// AN/USQ-113) runs. Radar alone: the pods on radar, the radio jammer silent. Radios alone: the pods on radios, and the radio jammer with
    /// them. Both: with a radio jammer, the pods stay on radar at full strength and the radio jammer takes the radios; without one, the pods'
    /// power is shared, <paramref name="bothRadarShare"/> on radar and the rest on radios. Neither: nothing.
    /// </summary>
    public static AircraftJamShare Share(bool radar, bool radios, bool commsJammer, float bothRadarShare)
    {
        if (radar && radios)
            return commsJammer ? new AircraftJamShare(1f, 0f, true) : new AircraftJamShare(bothRadarShare, 1f - bothRadarShare, false);
        if (radios) return new AircraftJamShare(0f, 1f, commsJammer);
        return radar ? new AircraftJamShare(1f, 0f, false) : new AircraftJamShare(0f, 0f, false);
    }

    /// <summary>A field's radar reach for a share of its power: as the square root, as a one-way field falls off (the Busbar's mast, EW11).</summary>
    public static double RadarReach(double fullReach, double radarShare) => radarShare <= 0 ? 0 : fullReach * System.Math.Sqrt(System.Math.Min(1.0, radarShare));

    /// <summary>Whether a jammer blinds this capability: only those the rules Def lists (a radar jammer floods radar frequencies, so a radar and
    /// a warning receiver; not an infrared night sight or an autopilot's gyros).</summary>
    public static bool Blinds(IReadOnlyCollection<string> jammed, string quantity)
    {
        foreach (string name in jammed)
        {
            if (name == quantity) return true;
        }
        return false;
    }
}

/// <summary>A jamming aircraft's split of its power (<see cref="JamFieldRule.Share"/>): its pods on radar and on radios, and whether its separate
/// radio jammer runs.</summary>
public readonly record struct AircraftJamShare(float PodsOnRadar, float PodsOnRadios, bool CommsJammer);
