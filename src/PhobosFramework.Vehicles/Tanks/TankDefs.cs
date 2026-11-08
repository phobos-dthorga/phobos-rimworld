using System.Collections.Generic;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>Whether a fluid needs an open liquid tank or a pressure vessel.</summary>
public enum TankPhase
{
    Liquid,
    Gas,
}

/// <summary>
/// A fluid a tanker can carry. Anyone can write one in XML: its phase decides the tank it needs, its limits decide how temperature
/// treats it, and its ports decide where it can be pumped from and to. Each ships in the folder of the mod that gives it meaning.
/// </summary>
public class PhobosTankContentsDef : Def
{
    public TankPhase phase = TankPhase.Liquid;
    /// <summary>What one unit is called in the tank's readout (litres, for instance).</summary>
    public string unitLabel = "units";
    /// <summary>The mass balance: what one unit adds to the vehicle.</summary>
    public float kilogramsPerUnit = 1f;
    /// <summary>Burns, and may explode when the tank is destroyed under it.</summary>
    public bool flammable;
    /// <summary>What a leak leaves on the ground. None for a gas.</summary>
    public ThingDef? spillFilth;
    /// <summary>Below this the contents are frozen and cannot be pumped. NaN never freezes.</summary>
    public float freezesBelow = float.NaN;
    /// <summary>Below this the pump starts to slow. NaN never thickens.</summary>
    public float thickensBelow = float.NaN;
    /// <summary>At or below this the pump is at thickFloor.</summary>
    public float thickestAt = float.NaN;
    public float thickFloor = 0.15f;
    /// <summary>A player's setting that must be on before the contents thicken (fuelGelling, for chemfuel: it is not diesel).</summary>
    public string? thickensOnlyWithSetting;
    /// <summary>Days above freezing before the whole load spoils. Zero never spoils.</summary>
    public float spoilsAfterDays;
    /// <summary>The relief valve opens at this outside temperature, or in a fire. NaN never vents.</summary>
    public float ventsAbove = float.NaN;
    /// <summary>Where it can be pumped from and to, tried in order.</summary>
    public List<TankPort> ports = new();

    public bool Vents => !float.IsNaN(ventsAbove);

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (kilogramsPerUnit < 0f) yield return "kilogramsPerUnit is negative";
        if (ports.Count == 0) yield return "no ports: it could never be loaded or unloaded";
        if (float.IsNaN(thickensBelow) != float.IsNaN(thickestAt)) yield return "thickensBelow and thickestAt must be given together";
    }
}

/// <summary>A tank a node of the vehicle's upgrade tree can fit, and what it means.</summary>
public class TankFitting
{
    /// <summary>The upgrade node's key, the only link between the tree and the tank.</summary>
    public string node = "";
    public TankPhase phase = TankPhase.Liquid;
    /// <summary>Hours for the contents to reach the outside temperature.</summary>
    public float hoursToOutside = 6f;
    /// <summary>How fast the spoilage clock runs in this tank (1 is a plain tank).</summary>
    public float spoilRate = 1f;
    /// <summary>How much a full tank of this kind weighs on the truck, for its heavy stages (1 is the general tank).</summary>
    public float loadWeight = 1f;
}

public class CompProperties_PhobosTank : VehicleCompProperties
{
    public List<TankFitting> fittings = new();
    /// <summary>The part whose loss bursts the tank.</summary>
    public string shellPart = "TankShell";
    /// <summary>The part holding the relief valve.</summary>
    public string valvePart = "TankFittings";
    /// <summary>Pump, hoses and controls below these shares of their full rating refuse to run (authored).</summary>
    public float pumpFloor = 0.35f;
    public float controlFloor = 0.35f;
    /// <summary>A shell below this will not take a load (authored).</summary>
    public float integrityFloor = 0.25f;
    /// <summary>The share of capacity leaking each hour at zero integrity (authored).</summary>
    public float leakFractionPerHour = 0.1f;
    /// <summary>The share of capacity a working relief valve vents each hour (authored).</summary>
    public float ventFractionPerHour = 0.5f;
    /// <summary>How far a hose reaches another vehicle, in cells from centre to centre (authored).</summary>
    public float hoseReach = 10f;
    /// <summary>The purge job: 4 in-game hours, and its water where plumbing rules apply (authored).</summary>
    public int purgeTicks = 10000;
    public float purgeLitres = 200f;
    /// <summary>The explosion the cube root scales from: Vanilla Chemfuel Expanded's fixed tank, radius up to 12 at 4,500 units.</summary>
    public float explosionReferenceUnits = 4500f;
    public float explosionReferenceRadius = 12f;
    public int explosionDamage = 40;
    /// <summary>The hose laid out while the tanker is set up.</summary>
    public GraphicDataOverlay? hosesOut;

    public CompProperties_PhobosTank() => compClass = typeof(CompPhobosTank);
}

[DefOf]
public static class TankDefOf
{
    public static VehicleStatDef Phobos_PumpRate = null!;
    public static VehicleStatDef Phobos_TankIntegrity = null!;
    public static VehicleStatDef Phobos_TankCapacity = null!;
    public static VehicleStatDef Phobos_RigControl = null!;
    public static JobDef PhobosVehicleSystems_PurgeTank = null!;

    static TankDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(TankDefOf));
}
