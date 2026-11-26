using Phobos.Framework.Defs;
using UnityEngine;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>What a puff of a vehicle's is, for its colour (Framework 0.321.0): smoke's colour says what burns (docs/development/vehicle-effects.md).</summary>
public enum FxTint
{
    /// <summary>The fleck's own colour.</summary>
    Plain,
    /// <summary>Blue-grey: oil burning (a worn turbo or engine).</summary>
    OilSmoke,
    /// <summary>Black: more fuel than air (high boost, a clogged filter, a cold diesel, a seizing engine).</summary>
    RichSmoke,
    /// <summary>White: water (boiling coolant, the boiler, cold-morning condensation, unburnt fuel cranked through).</summary>
    Steam,
    /// <summary>Pale grey: hot brake linings.</summary>
    BrakeSmoke,
    /// <summary>Brownish grey: a slipping clutch's lining.</summary>
    Acrid,
    /// <summary>Dark: fuel dripping from a leak.</summary>
    Fuel,
    /// <summary>Light grey: tyre rubber.</summary>
    TyreSmoke,
    /// <summary>Earthy: bits thrown from a burst tyre.</summary>
    Debris,
}

/// <summary>Where on a vehicle a puff comes from, worked out from its size and heading (Framework 0.321.0).</summary>
public enum FxSpot
{
    /// <summary>Anywhere over the body.</summary>
    Body,
    /// <summary>The back, a little to one side.</summary>
    Exhaust,
    /// <summary>The engine bay, toward the front.</summary>
    Engine,
    /// <summary>The front (the radiator).</summary>
    Front,
    /// <summary>Around the wheels, low.</summary>
    Wheels,
    /// <summary>Under the middle.</summary>
    Under,
}

/// <summary>
/// The figures and colours of vehicles' visual effects (Framework 0.321.0; docs/development/vehicle-effects.md; the owner's cards of 26 Nov
/// 2026). Every figure is authored, a look to tune by eye; the colours are the smoke chemistry's (blue oil, black fuel, white water).
/// </summary>
public class PhobosVehicleFxRulesDef : PhobosRulesDef
{
    public static PhobosVehicleFxRulesDef Rules => Current<PhobosVehicleFxRulesDef>();

    public Color oilSmoke = new(0.55f, 0.64f, 0.85f, 0.75f);
    public Color richSmoke = new(0.12f, 0.12f, 0.12f, 0.8f);
    public Color steam = new(1f, 1f, 1f, 0.55f);
    public Color brakeSmoke = new(0.85f, 0.85f, 0.85f, 0.6f);
    public Color acrid = new(0.6f, 0.55f, 0.45f, 0.7f);
    public Color fuel = new(0.15f, 0.12f, 0.08f, 0.7f);
    public Color tyreSmoke = new(0.9f, 0.9f, 0.9f, 0.7f);
    public Color debris = new(0.35f, 0.3f, 0.25f, 0.9f);

    /// <summary>The share of what moving vehicles throw up drawn at the Light setting; a fault's own sign is never thinned.</summary>
    public float lightShare = 0.4f;

    /// <summary>Exhaust: nothing at or below the idle load; <see cref="exhaustCruisePerHour"/> from a light load, rising to
    /// <see cref="exhaustHardPerHour"/> at full load. A diesel's is darker than a petrol's, and a cold engine's white in cold air.</summary>
    public float exhaustIdleLoad = 0.15f;
    public float exhaustCruisePerHour = 25f;
    public float exhaustHardPerHour = 260f;
    public Color exhaustDiesel = new(0.3f, 0.3f, 0.3f, 0.45f);
    public Color exhaustPetrol = new(0.75f, 0.75f, 0.75f, 0.2f);
    /// <summary>A cold engine's exhaust shows white below this air temperature, until its coolant passes <see cref="exhaustWarmCoolant"/>.</summary>
    public float exhaustCondensesBelow = 5f;
    public float exhaustWarmCoolant = 50f;

    /// <summary>What the wheels throw up, an hour at <see cref="groundFullSpeed"/> cells a second and above, scaled by the place's dust.</summary>
    public float groundPerHour = 320f;
    public float groundFullSpeed = 4f;
    /// <summary>Slower than this, cells a second, nothing is thrown up.</summary>
    public float groundMinSpeed = 0.3f;
    /// <summary>Snow deeper than this shows as spray and white ruts.</summary>
    public float snowFromDepth = 0.15f;
    /// <summary>Ground wetter than this is mud.</summary>
    public float mudFromWetness = 0.5f;
    public Color dust = new(0.72f, 0.62f, 0.46f, 0.6f);
    public Color sand = new(0.86f, 0.78f, 0.58f, 0.6f);
    public Color mud = new(0.33f, 0.25f, 0.17f, 0.85f);
    public Color snow = new(1f, 1f, 1f, 0.85f);

    /// <summary>Ruts: a pair every <see cref="rutEveryCells"/> cells on soft ground or snow, lasting <see cref="rutSeconds"/> seconds before
    /// they fade (<see cref="rutSecondsLight"/> at the Light setting).</summary>
    public float rutEveryCells = 0.6f;
    public float rutSeconds = 40f;
    public float rutSecondsLight = 15f;
    public float rutWidth = 0.3f;
    public Color rutSoil = new(0.25f, 0.2f, 0.15f, 0.35f);
    public Color rutSnow = new(0.55f, 0.6f, 0.72f, 0.4f);

    /// <summary>Rotor downwash: puffs an hour at full strength, reaching the ground up to <see cref="downwashReachMetres"/> below the rotor.</summary>
    public float downwashPerHour = 900f;
    public float downwashReachMetres = 30f;

    /// <summary>A rough landing's burst of tyre smoke and dust.</summary>
    public int touchdownPuffs = 10;

    // ---- Slice 3 (Framework 0.326.0): lamps, heat shimmer, flares ----

    /// <summary>Brake lamps: lit while a land vehicle that was moving at <see cref="groundMinSpeed"/> or more loses
    /// <see cref="brakeDropShare"/> of its speed between looks (stopping included), and held <see cref="brakeHoldSeconds"/> after; drawn at
    /// full brightness and <see cref="brakeGlowScale"/> times the lamp's size.</summary>
    public float brakeDropShare = 0.25f;
    public float brakeHoldSeconds = 1.2f;
    public float brakeGlowScale = 1.4f;

    /// <summary>Heat shimmer behind a running turbine or jet: an hour's shimmers per engine at full output, a third of that at idle.</summary>
    public float shimmerPerHour = 900f;
    public float shimmerIdleShare = 0.35f;
    public float shimmerScale = 1.1f;
    /// <summary>Where the jet pipe is, as a share of half the drawn length ahead of the middle (behind, negative): a jet's at its tail, a
    /// turboshaft's (a helicopter, the Osprey's nacelles, a turbine truck) a little behind the middle, thrusters at the back.</summary>
    public float shimmerTurbofanForward = -0.85f;
    public float shimmerTurboshaftForward = -0.2f;
    public float shimmerThrusterForward = -0.75f;
    /// <summary>How far out across the drawn width the outermost engine of several sits, as a share of half the width.</summary>
    public float shimmerSpreadShare = 0.5f;

    /// <summary>A salvo's flares: how many, thrown out behind and to both sides at <see cref="flareSpeed"/> cells a second, slowed by the
    /// air (<see cref="flareDragSeconds"/>), burning <see cref="flareSeconds"/> seconds with a trail of white smoke.</summary>
    public int flaresPerSalvo = 6;
    public float flareSeconds = 4f;
    public float flareSpeed = 4f;
    public float flareDragSeconds = 1.2f;
    public float flareSize = 1.3f;
    public Color flareGlow = new(1f, 0.95f, 0.8f, 1f);
    public Color flareSmoke = new(1f, 1f, 1f, 0.6f);
    /// <summary>A salvo's chaff: a glitter of this many sparks behind the aircraft.</summary>
    public int chaffSparks = 14;
    public Color chaff = new(0.85f, 0.88f, 0.95f, 0.9f);
}

/// <summary>How a lamp burns (Framework 0.326.0).</summary>
public enum LampPattern
{
    /// <summary>Always while lit.</summary>
    Steady,
    /// <summary>A short flash once a period (a beacon, a strobe).</summary>
    Flash,
}

/// <summary>
/// A lamp of an electrical load (Framework 0.326.0, slice 3; docs/development/vehicle-effects.md): a glow drawn on the vehicle while its load
/// runs (headlights, navigation lights, a recovery beacon), turning with it. Its place is in shares of half the vehicle's length (ahead of
/// the middle) and half its width (to its right), of its footprint, or of its drawn picture where <see cref="drawn"/> (an aircraft's
/// wingtips reach past its footprint). The glow is the game's own fire glow, tinted; no picture of ours. Every figure is authored.
/// </summary>
public class LampSpec
{
    public float forward;
    public float right;
    public Color colour = Color.white;
    /// <summary>The glow's size across, cells.</summary>
    public float size = 0.6f;
    public LampPattern pattern = LampPattern.Steady;
    public float periodSeconds = 1f;
    /// <summary>How late in the period its flash comes, 0 to 1 (two lamps half a period apart take turns).</summary>
    public float phase;
    /// <summary>The share of each period a flash lasts.</summary>
    public float litShare = 0.2f;
    /// <summary>Places on the drawn picture rather than the footprint.</summary>
    public bool drawn;
    /// <summary>A brake lamp: also lit, brighter and larger, while the vehicle slows, whether or not its load runs (a tail lamp).</summary>
    public bool brake;
}
