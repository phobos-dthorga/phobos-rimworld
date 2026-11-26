using System;

namespace Phobos.Framework.Rules;

/// <summary>How a stuck vehicle is got out (EM43), fastest first.</summary>
public enum BogRecovery
{
    /// <summary>A load of stone under the wheels: at once, for the stone.</summary>
    Stone,
    /// <summary>A winch truck in the convoy pulls it out.</summary>
    Winch,
    /// <summary>Another vehicle with a hitch drags it out.</summary>
    Tow,
    /// <summary>Nothing to hand: wait for the ground to firm up.</summary>
    Wait,
}

/// <summary>
/// The world map's ground under heavy vehicles, game-free (docs/development/vehicle-road-work.md): bogging on soft, wet ground (EM43) and
/// grit on winter tiles (EM39). <b>State, never a roll</b> (the owner's rule on rolls): a heavy vehicle sinks a little on each soft tile it
/// crosses, and sticks when it has sunk a whole measure, so a player can see it coming from the ground and the load. The world map has no
/// mud of its own, so softness is read from the tile's swampiness and its yearly rain, firm when frozen. Every figure is the agent's.
/// </summary>
public static class GroundRule
{
    /// <summary>Swampiness at which a tile is soft whatever the rain.</summary>
    public const float SwampSoftFrom = 0.5f;
    /// <summary>Yearly rain at which flat or gently hilly ground starts to be soft, and at which it is fully soft.</summary>
    public const float WetFromMillimetres = 1400f;
    public const float SoakedAtMillimetres = 2800f;
    /// <summary>A vehicle under this laden mass does not sink: a light truck spreads its weight.</summary>
    public const float HeavyFromKilograms = 12000f;
    /// <summary>The extra mass over the heavy line that sinks a whole measure on one fully soft tile.</summary>
    public const float SinkMeasureKilograms = 24000f;
    /// <summary>Tracks spread a vehicle's weight: a tracked vehicle sinks a quarter as fast.</summary>
    public const float TrackedShare = 0.25f;
    /// <summary>Hours each way out takes; stone frees it at once.</summary>
    public const float WinchHours = 1.5f;
    public const float TowHours = 3f;
    public const float WaitHours = 24f;
    /// <summary>Stone chunks laid under the wheels to free a stuck vehicle.</summary>
    public const int StoneChunks = 10;
    /// <summary>Stone chunks spread as grit on a winter tile.</summary>
    public const int GritChunks = 2;
    /// <summary>Days grit eases a tile.</summary>
    public const float GritDays = 5f;
    /// <summary>The share of the winter's extra going grit takes away.</summary>
    public const float GritRelief = 0.75f;

    /// <summary>
    /// How soft a tile is, 0 (firm) to 1 (fully soft): a swamp by its swampiness, otherwise flat or gently hilly ground by its yearly
    /// rain. Frozen ground is firm; large hills and mountains drain and are firm.
    /// </summary>
    public static float Softness(float swampiness, float rainfallMillimetres, int hilliness, float temperature)
    {
        if (temperature <= 0f || hilliness >= 3) return 0f;
        float swamp = swampiness >= SwampSoftFrom ? Math.Clamp(swampiness, 0f, 1f) : 0f;
        float wet = Math.Clamp((rainfallMillimetres - WetFromMillimetres) / (SoakedAtMillimetres - WetFromMillimetres), 0f, 1f);
        return Math.Max(swamp, wet);
    }

    /// <summary>How far a vehicle sinks crossing one tile of this softness, in measures (1 sticks it).</summary>
    public static float Sink(float ladenKilograms, float softness, bool tracked)
    {
        float over = Math.Max(0f, ladenKilograms - HeavyFromKilograms) / SinkMeasureKilograms;
        return over * Math.Clamp(softness, 0f, 1f) * (tracked ? TrackedShare : 1f);
    }

    /// <summary>The fastest way out the convoy has: stone first when the player lays it, then a winch, a tow, or the wait.</summary>
    public static BogRecovery Best(bool winch, bool tow) => winch ? BogRecovery.Winch : tow ? BogRecovery.Tow : BogRecovery.Wait;

    /// <summary>Hours a way out takes.</summary>
    public static float Hours(BogRecovery way) => way switch
    {
        BogRecovery.Stone => 0f,
        BogRecovery.Winch => WinchHours,
        BogRecovery.Tow => TowHours,
        _ => WaitHours,
    };

    /// <summary>The winter's extra cost of a tile with grit on it.</summary>
    public static float Gritted(float winterCost) => winterCost * (1f - GritRelief);
}
