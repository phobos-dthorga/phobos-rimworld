using Phobos.Framework.Defs;

namespace Phobos.VehicleSystems;

/// <summary>
/// What the ground under an aircraft on our own flight does to it as it lifts off or touches down (Framework 0.158.0; the owner's decision of
/// 18 Nov 2026, docs/development/vehicle-osprey.md, "What it lands on"; LandingSurfaceRule). The first loaded instance wins; another mod may
/// patch it in XML. Every figure is authored.
/// </summary>
public class PhobosLandingSurfaceRulesDef : PhobosRulesDef
{
    /// <summary>The share of each engine's full health one lift-off or touch-down over wholly loose, dry ground costs.</summary>
    public float engineWearPerEvent = 0.005f;
    /// <summary>Dust never wears an engine below this share of its full health. Above the 20% at which the engine model counts an engine
    /// out, so dust alone grounds nothing: a badly worn engine gives less power until it is repaired.</summary>
    public float engineWearFloor = 0.3f;
    /// <summary>The factor on that wear when rolling on or off: less dust is thrown up ahead of the intakes.</summary>
    public float rollingFactor = 0.5f;
    /// <summary>The share of the dust an inlet barrier keeps out.</summary>
    public float barrierStops = 0.8f;
    /// <summary>The chance one lift-off or touch-down sets dry growth alight, with dry growth under the whole aircraft.</summary>
    public float fireChancePerEvent = 0.04f;
    /// <summary>The factor on that chance in a heat wave.</summary>
    public float heatWaveFactor = 3f;
    /// <summary>The size of the fire it starts (the game's own scale, 0.1 to 1.75).</summary>
    public float fireSize = 0.3f;
    /// <summary>Ground or growth under more snow than this is neither dusty nor dry.</summary>
    public float snowAtMost = 0.2f;
    /// <summary>Ground wetter than this (0 to 1, D'thorga Framework's ground wetness) is neither dusty nor dry, though the rain has stopped
    /// (Framework 0.204.0, LM2).</summary>
    public float wetAbove = 0.25f;

    // ---- Strip debris (Framework 0.250.0, per-engine realism round two, step 10; the owner's card of 23 Nov 2026: jets and helicopters) ----

    /// <summary>Strikes an hour, for a fully exposed intake on wholly rough ground, in a flight phase weighing 1 (a take-off or landing roll).</summary>
    public float debrisPerHour = 0.5f;
    /// <summary>The chance of a strike at one lift-off or touch-down of an aircraft on our own flight, fully exposed, on wholly rough ground.</summary>
    public float debrisPerEvent = 0.04f;
    /// <summary>The share of an engine's full health one strike costs; never below <see cref="engineWearFloor"/>.</summary>
    public float debrisDamage = 0.15f;
    /// <summary>The flight phases that throw debris, by the phase names our flight profiles use, with how much each throws.</summary>
    public System.Collections.Generic.List<DebrisPhase> debrisPhases = new();
}

/// <summary>A flight phase that throws stones and grit at the intakes, and how much against a take-off roll's 1.</summary>
public class DebrisPhase
{
    public string phase = "";
    public float weight = 1f;
    /// <summary>The phase throws grit only while the aircraft moves over the ground (taxiing; Framework 0.254.0): some flight profiles call
    /// an aircraft standing with its engines on by the same name.</summary>
    public bool movingOnly;
}
