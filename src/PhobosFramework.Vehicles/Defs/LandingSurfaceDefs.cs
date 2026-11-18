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
}
