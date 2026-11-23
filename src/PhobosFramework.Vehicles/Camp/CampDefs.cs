using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// On a building that packs up (a swag, a bedroll): the camp order sets it out from a vehicle's cargo and packs it back (Framework 0.212.0;
/// docs/development/vehicle-accessories.md, Y3). Other mods mark their own camp gear with it in XML.
/// </summary>
public class PhobosCampGearExtension : DefModExtension
{
    /// <summary>The work of laying one out or rolling it up, at construction speed (authored).</summary>
    public float work = 150f;
    /// <summary>A seat (Framework 0.219.0, the camp stool): set down beside a camp table where one stands, facing it, so people eat there.</summary>
    public bool besideTable;
    /// <summary>A bed's machine (Framework 0.265.0, a sleep accelerator; vehicle-crew-machines.md, CM1): set down against the head of a bed of
    /// the camp, facing it, as the game asks of its own sleep accelerator. One to a bed; with no bed free it stays in the cargo.</summary>
    public bool besideBedHead;
}

/// <summary>
/// On a bed: the memory a sleeper wakes with (Framework 0.212.0; the record's Y8 and B1). Given after the game's own bed memories, so it can
/// answer "slept outside" (−4) as the owner chose: +5 for a fair night in a swag under an open sky, +4 for the roof tent.
/// </summary>
public class PhobosBedMemoryExtension : DefModExtension
{
    public ThoughtDef? memory;
    /// <summary>Only with no roof over the bed: the stars, not an awning.</summary>
    public bool openSkyOnly;
    /// <summary>Only with no rain or snow falling on waking.</summary>
    public bool fairWeatherOnly;
    /// <summary>Only when the night was neither too cold nor too hot for the sleeper (the game's own "slept in the cold" and "in the heat").</summary>
    public bool comfortableOnly;
}

/// <summary>Gives a land vehicle its camp order (Framework 0.212.0). Added in code to every land vehicle with a loadout, trailers apart, and to a trailer
/// whose deck takes camp gear (Framework 0.216.0).</summary>
public class CompProperties_PhobosCamp : VehicleCompProperties
{
    public CompProperties_PhobosCamp() => compClass = typeof(CompPhobosCamp);
}

[DefOf]
public static class VehicleCampDefOf
{
    public static ThingDef PhobosCamp_RoofTent = null!;
    public static ThingDef PhobosCamp_AwningPole = null!;
    public static ThingDef PhobosCamp_Swag = null!;
    public static ThingDef PhobosCamp_CabBunk = null!;
    public static ThingDef PhobosCamp_CamperBed = null!;
    public static ThingDef PhobosCamp_CamperBedRoomy = null!;
    public static ThingDef Phobos_CanvasWall = null!;
    public static ThingDef Phobos_CanvasDoor = null!;
    public static RoofDef Phobos_CanvasRoof = null!;
    public static JobDef PhobosVehicleSystems_CampWork = null!;
    public static JobDef PhobosVehicleSystems_BunkSleep = null!;
    public static ThoughtDef PhobosSleptInCab = null!;

    static VehicleCampDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(VehicleCampDefOf));
}
