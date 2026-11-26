using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// A water bore drilled at a halt on the world map, game-free (docs/development/vehicle-road-work.md, EM34). The tile's yearly rainfall
/// stands in for the ground water under it: no hydrogeology is modelled. Every figure is the agent's, for the owner to revise.
/// </summary>
public static class BoreRule
{
    /// <summary>Below this yearly rainfall the hole is dry (a desert's line, as the game's own biomes draw it).</summary>
    public const float DryBelowMillimetres = 250f;
    /// <summary>Litres an hour gained for each millimetre of yearly rain above the dry line.</summary>
    public const float LitresPerHourPerMillimetre = 0.8f;
    /// <summary>The most a small rig's bore pump gives, however wet the ground: about a quarter of a litre a second.</summary>
    public const float MostLitresPerHour = 1000f;
    /// <summary>Hours of drilling, at the plain pace, before the first water comes up.</summary>
    public const float DrillHours = 8f;

    /// <summary>Whether a bore here finds no water.</summary>
    public static bool Dry(float rainfallMillimetres) => rainfallMillimetres < DryBelowMillimetres;

    /// <summary>Litres an hour the bore gives once drilled, by the tile's yearly rainfall.</summary>
    public static float LitresPerHour(float rainfallMillimetres) =>
        Dry(rainfallMillimetres) ? 0f : Math.Min(MostLitresPerHour, (rainfallMillimetres - DryBelowMillimetres) * LitresPerHourPerMillimetre);

    /// <summary>Hours drilled in one step, at the hand's pace; the rest of the step, once drilled, pumps.</summary>
    public static float Drilled(float drilledSoFar, float hours, float pace) => Math.Min(DrillHours, drilledSoFar + Math.Max(0f, hours * pace));
}
