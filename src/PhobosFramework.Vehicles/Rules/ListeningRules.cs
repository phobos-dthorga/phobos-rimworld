using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>What a listening array can hear. Never saved.</summary>
public enum ContactKind
{
    /// <summary>Something moving in or on the water: a swimmer, a boat, someone wading.</summary>
    InWater,
    /// <summary>Something moving on land: footsteps and engines carry poorly into the water.</summary>
    OnLand,
    /// <summary>Another boat that is under, heard by its own noise whether it moves or not.</summary>
    HiddenBoat,
}

/// <summary>The eight points of the compass, clockwise from north, for saying where a contact was heard.</summary>
public enum Bearing
{
    North,
    NorthEast,
    East,
    SouthEast,
    South,
    SouthWest,
    West,
    NorthWest,
}

/// <summary>
/// The arithmetic of a listening array, free of the game (docs/development/vehicle-seabed-crawler.md, "The listening array as built"). A passive array only hears: it makes no
/// sound and gives nothing away. Every figure that feeds it is authored on the vehicle's Def and is a gameplay choice, not a measurement.
/// </summary>
public static class ListeningRule
{
    /// <summary>
    /// How far the array hears now, in cells: the fitted range, times the best listener's hearing aboard (the game's hearing capacity, 0 to 1), and less on the move, when the boat's own machinery
    /// and the water rushing past drown out what it listens for. Nothing at all without a listener or an array.
    /// </summary>
    public static float Range(float fittedRange, float bestHearing, bool moving, float movingFactor)
    {
        if (fittedRange <= 0f || bestHearing <= 0f) return 0f;
        return fittedRange * Math.Clamp(bestHearing, 0f, 1f) * (moving ? Math.Clamp(movingFactor, 0f, 1f) : 1f);
    }

    /// <summary>
    /// Whether something at this distance is heard. What moves in the water is heard across the whole range and what moves on land across a share of it; something standing still makes no
    /// sound to hear. A boat that is under is heard by its own noise (1 for an ordinary hull, less for a quiet or settled one), and less again while it keeps still.
    /// </summary>
    public static bool Hears(ContactKind kind, float distance, float range, bool contactMoving, float landShare, float boatNoise, float stillBoatShare)
    {
        if (range <= 0f || distance < 0f) return false;
        return kind switch
        {
            ContactKind.InWater => contactMoving && distance < range,
            ContactKind.OnLand => contactMoving && distance < range * Math.Clamp(landShare, 0f, 1f),
            _ => distance < range * Math.Max(0f, boatNoise) * (contactMoving ? 1f : Math.Clamp(stillBoatShare, 0f, 1f)),
        };
    }

    /// <summary>The compass point a contact lies on from the listener, from the cell offset between them: x to the east, z to the north, as the game's map has them.</summary>
    public static Bearing BearingOf(int dx, int dz)
    {
        if (dx == 0 && dz == 0) return Bearing.North;
        double degrees = Math.Atan2(dx, dz) * 180.0 / Math.PI;
        int point = (int)Math.Round(degrees / 45.0);
        return (Bearing)(((point % 8) + 8) % 8);
    }

    /// <summary>Whether the nearest contact is close enough to spot the boat, so the crew knows it is in danger before it is seen.</summary>
    public static bool WithinSpotting(float nearest, float spotRange) => spotRange > 0f && nearest >= 0f && nearest < spotRange;
}
