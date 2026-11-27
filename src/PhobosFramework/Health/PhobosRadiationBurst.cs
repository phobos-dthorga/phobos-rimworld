using Verse;

namespace Phobos.Framework.Health;

/// <summary>What a burst's radiation comes from, each with its own setting.</summary>
public enum RadiationBurstKind
{
    /// <summary>A fission or fusion weapon: a prompt dose, and fallout where the weapon says so.</summary>
    Nuclear,
    /// <summary>Antimatter: a prompt dose of gamma rays from annihilation, and no fallout (a sci-fi extrapolation).</summary>
    Antimatter,
}

/// <summary>
/// Marks a weapon whose blast gives a prompt radiation dose through D'thorga Medical's model (Framework 0.358.0;
/// docs/development/radiation-bursts.md; owner, 27 Nov 2026). Added by XML to a ThingDef that explodes (a projectile, or anything with the
/// game's explosive comp), to an IRBM warhead, or to a DMS Legion support type; another modder adds it to theirs the same way. Every figure
/// is authored; the shape of the fall-off is the real one (<see cref="Rules.RadiationBurstRule"/>).
/// </summary>
public class PhobosRadiationBurst : DefModExtension
{
    public RadiationBurstKind kind = RadiationBurstKind.Nuclear;
    /// <summary>The edge of the burst, in cells, inside which everyone in the open takes <see cref="graysAtEdge"/>. 0: the thing's own
    /// explosion radius.</summary>
    public float edgeRadius;
    /// <summary>The dose in the open at the edge, in grays.</summary>
    public float graysAtEdge = 10f;
    /// <summary>How far, in cells, air takes to cut the dose to about a third, beyond the inverse square.</summary>
    public float airLength = 60f;
    /// <summary>The same dose everywhere on the map, with no centre: a burst from orbit.</summary>
    public bool wholeMap;
    /// <summary>Whether built roofs and walls shield (false for rays that pass through them); rock always does.</summary>
    public bool buildingsShield = true;
    /// <summary>Days of Dubs Rimatomics' fallout on the map, when it is active; zero for none.</summary>
    public FloatRange falloutDays = FloatRange.Zero;
    /// <summary>Only while the thing's refuelable store holds at least one unit: a boiler that holds its nuclear charges as fuel gives no
    /// dose when it blows up empty.</summary>
    public bool onlyWithFuel;

    /// <summary>Whether this thing holds what the burst needs (its fuel, where <see cref="onlyWithFuel"/> asks).</summary>
    public bool Armed(Thing? thing) => !onlyWithFuel || (thing?.TryGetComp<RimWorld.CompRefuelable>() is { } fuel && fuel.Fuel >= 1f);
}
