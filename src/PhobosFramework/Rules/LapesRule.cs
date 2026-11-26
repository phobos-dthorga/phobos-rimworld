using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// A low-altitude parachute extraction (LAPES; D'thorga Framework 0.323.0; docs/development/airdrop.md, "LAPES"; the owner's card of 26 Nov
/// 2026 for the C-27J: on by default). The aircraft flies a few feet over a clear strip and an extraction parachute pulls a vehicle on its
/// platform out of the ramp; the platform touches down at once and slides to a stop under ground friction and the chute's drag: no drift, no
/// canopy to wait for. Game-free.
/// </summary>
public static class LapesRule
{
    /// <summary>How high the aircraft flies over the strip as it lets go, feet: 5 to 10 ft in the US Army's airdrop reference and
    /// GlobalSecurity's account <b>[S]</b>; the middle taken.</summary>
    public const double HeightFeet = 8;

    /// <summary>
    /// How far the platform slides before it stops, metres. <b>Unconfirmed</b>: the one figure found, "typically 100 to 200 feet", is from an
    /// AI-written encyclopedia; no primary source was found (searched 26 Nov 2026). The middle, 150 ft, is taken. A slide's length hangs on the
    /// ground speed, the load, the chutes and the ground, so one figure is a simplification.
    /// </summary>
    public const double SlideMetres = 150 * ParachuteRule.MetresPerFoot;

    /// <summary>How long the slide takes on the map, seconds (authored: long enough to be seen, short against a parachute's descent).</summary>
    public const double SlideSeconds = 2.5;

    /// <summary>
    /// The chance the platform lands hard and its load is damaged (authored, the design's "more often than by parachute": five times the
    /// parachute's <see cref="ParachuteRule.HardLandingChance"/>; no LAPES damage rate was found).
    /// </summary>
    public const double HardLandingChance = 5 * ParachuteRule.HardLandingChance;

    /// <summary>
    /// The chance the aircraft strikes the ground as the load goes (authored: small; two fatal LAPES accidents are on record, a C-130 in 1982 and
    /// another in 1987, against deliveries the sources do not count).
    /// </summary>
    public const double GroundStrikeChance = 0.01;

    /// <summary>The share of each landing part's health a ground strike wears (authored).</summary>
    public const double StrikeWearShare = 0.25;

    /// <summary>The share of ground strikes that do more than the landing gear and belly: a fall's damage to the airframe (authored, "rarely
    /// more").</summary>
    public const double SevereStrikeShare = 0.1;

    /// <summary>
    /// The slide in cells on the map: the real slide at the share the map gives a parachute's drift (<see cref="ParachuteRule.MapDriftShare"/>),
    /// so the two read alike; never under one cell.
    /// </summary>
    public static int SlideCells(double metresPerCell) =>
        metresPerCell <= 0 ? 1 : Math.Max(1, (int)Math.Round(SlideMetres * ParachuteRule.MapDriftShare / metresPerCell));

    /// <summary>The clear strip it needs ahead of the touchdown point, cells: the slide, the platform's own length and one cell to spare.</summary>
    public static int StripCells(double metresPerCell, int platformLengthCells) =>
        SlideCells(metresPerCell) + Math.Max(1, platformLengthCells) + 1;

    /// <summary>Its height over the strip as a share of its usual flying height (for drawing it low); at most the whole.</summary>
    public static double HeightShare(double flyingHeightMetres) =>
        flyingHeightMetres <= 0 ? 1 : Math.Min(1, HeightFeet * ParachuteRule.MetresPerFoot / flyingHeightMetres);

    /// <summary>
    /// How far along the slide the platform is after this share of its time: an even deceleration from its exit speed to rest, so it covers
    /// most of the way early (s = 1 - (1 - t)^2).
    /// </summary>
    public static double SlidShare(double timeShare)
    {
        double t = Math.Max(0, Math.Min(1, timeShare));
        return 1 - (1 - t) * (1 - t);
    }

    /// <summary>Whether this load may go by LAPES: within the aircraft's limit and with nobody inside (crews never rode a LAPES load).</summary>
    public static bool MayExtract(double loadKg, double limitKg, bool crewInside) =>
        limitKg > 0 && loadKg > 0 && loadKg <= limitKg && !crewInside;
}
