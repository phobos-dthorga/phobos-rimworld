using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>What a vehicle's glass is (docs/development/vehicle-glass.md, VG2).</summary>
public enum GlassKind
{
    /// <summary>A laminated windscreen and toughened side glass: a civilian cab.</summary>
    Ordinary,
    /// <summary>Thick armoured glass: an armoured cab.</summary>
    Armoured,
    /// <summary>Small, tough vision blocks: a tank or armoured vehicle seen out of through blocks.</summary>
    VisionBlocks,
}

/// <summary>The glass's state, from its health.</summary>
public enum GlassState
{
    Sound,
    Cracked,
    Gone,
}

/// <summary>
/// A vehicle's glass (D'thorga Framework 0.238.0; docs/development/vehicle-glass.md, VG1 to VG4, VG7; the owner's answers of 22 Nov 2026).
/// Glass is one part, whose health is its share of whole glass. It cracks and breaks before anything else on a vehicle, as real glass
/// is the first thing a blast breaks; how readily depends on its kind. Game-free; every figure here is authored, a gameplay
/// simplification of the record's sources.
/// </summary>
public static class GlassRule
{
    /// <summary>Glass below this share of whole is cracked, and wants re-glazing; above it, chips only.</summary>
    public const double CrackedBelow = 0.7;

    public static GlassState StateOf(double share) => share <= 0 ? GlassState.Gone : share < CrackedBelow ? GlassState.Cracked : GlassState.Sound;

    /// <summary>The share of a hit on the cab's plate that reaches its glass: ordinary glass breaks easily, armoured glass and vision
    /// blocks are made to take it.</summary>
    public static double PlateHitShare(GlassKind kind) => kind switch
    {
        GlassKind.Armoured => 0.15,
        GlassKind.VisionBlocks => 0.1,
        _ => 0.6,
    };

    /// <summary>How far past an explosion's own radius its blast still breaks glass, as a multiple of the radius (real windows break
    /// far beyond where a blast harms anything else).</summary>
    public const double BlastReach = 2.5;

    /// <summary>
    /// The damage an explosion does to glass at a distance from its centre: none beyond the reach, falling in a straight line to it, and
    /// scaled by how readily the kind breaks.
    /// </summary>
    public static double BlastDamage(double amount, double distance, double radius, GlassKind kind)
    {
        double reach = radius * BlastReach;
        if (amount <= 0 || reach <= 0 || distance >= reach) return 0;
        double falloff = 1 - Math.Max(0, distance) / reach;
        double share = kind switch
        {
            GlassKind.Armoured => 0.25,
            GlassKind.VisionBlocks => 0.2,
            _ => 1.0,
        };
        return amount * falloff * share;
    }

    /// <summary>What re-glazing takes, by the share missing: an ordinary cab its windscreen (a laminated pane) once cracked, and side
    /// panes as more is missing, three for all of it; an armoured cab armoured panes, three for all; vision blocks, four for all.</summary>
    public static (int laminated, int panes, int armoured, int blocks) Goods(GlassKind kind, double missing)
    {
        if (missing <= 0) return (0, 0, 0, 0);
        double m = Math.Min(1, missing);
        return kind switch
        {
            GlassKind.Armoured => (0, 0, (int)Math.Ceiling(m * 3), 0),
            GlassKind.VisionBlocks => (0, 0, 0, (int)Math.Ceiling(m * 4)),
            _ => (1, (int)Math.Floor(m * 3), 0, 0),
        };
    }

    // ---- Splinters (VG5) ----

    /// <summary>
    /// How many splinters strike each person behind the glass as it breaks. Ordinary glass showers its side windows: one when it cracks
    /// badly (a third or more lost at once), two when it goes. Armoured glass and vision blocks throw none until they go, then one (their
    /// inner plastic layer holds most). Authored after the record's sources: flying glass is a leading injury of real blasts.
    /// </summary>
    public static int Splinters(GlassKind kind, double shareLost, bool gone)
    {
        if (shareLost <= 0) return 0;
        if (kind != GlassKind.Ordinary) return gone ? 1 : 0;
        if (gone) return 2;
        return shareLost >= 1.0 / 3 ? 1 : 0;
    }

    /// <summary>A splinter's damage, a small cut (authored).</summary>
    public const double SplinterDamage = 3;

    /// <summary>The chance a splinter finds an eye (authored; the sources name eye injuries among blast glass wounds).</summary>
    public const double SplinterEyeChance = 0.05;

    // ---- The driver's view (VG6) ----

    /// <summary>
    /// How fast the driver dares go for what they can see, as a share of the vehicle's speed: never a rolled crash, as the tired or stoned
    /// driver's rule. Cracked glass: slower in the dark or bad weather, a little slower otherwise. Glass gone: the weather in the driver's
    /// face, slower in rain, snow or fog only. Vision blocks: cracked, a little slower; gone, the driver drives head-out or crawls. Authored.
    /// </summary>
    public static double ViewFactor(GlassKind kind, GlassState state, bool dark, bool badWeather)
    {
        if (state == GlassState.Sound) return 1;
        if (kind == GlassKind.VisionBlocks) return state == GlassState.Gone ? 0.6 : 0.85;
        if (state == GlassState.Cracked) return dark || badWeather ? 0.75 : 0.9;
        return badWeather ? 0.8 : 1;
    }

    /// <summary>The work of re-glazing, by the share missing: a set amount for the job, and more for each share.</summary>
    public static double ReglazeWork(double missing, double baseWork, double workForAll) => baseWork + workForAll * Math.Min(1, Math.Max(0, missing));
}
