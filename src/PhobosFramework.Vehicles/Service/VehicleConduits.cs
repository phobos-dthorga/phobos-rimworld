using System.Linq;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A vehicle with a battery lays the game's own hidden power conduit across its footprint while it stands with its engine off (Framework 0.108.0; owner,
/// 13 Nov 2026: "every vehicle with a battery", always, "so players are not confused"), so a conduit touching any side joins it to the grid, as a building on
/// a conduit run joins. Its battery then charges from that grid by the shore power the electrics already have: the conduits beneath it are things on a
/// powered net standing within the vehicle's reach (CompPhobosVehicleSystems.ShorePowered), so nothing is counted twice. Taken up when the engine starts or
/// it moves. The generator set lays its own conduits while it is set up to feed the grid through them (CompPhobosGeneratorSet), so this one stands down
/// while it is set up; packed up and parked with its engine off, it follows this rule like any vehicle.
/// <para>Framework 0.108.1 (owner, 13 Nov 2026: "You need to use the Hidden versions of them"): the conduit is the game's hidden conduit, which draws nothing on
/// the ground and shows in the power overlay; the visible conduits 0.107.0 and 0.108.0 laid are swapped for it at the next look. It refunds nothing when
/// taken apart, as the ordinary conduit does not.</para>
/// </summary>
[StaticConstructorOnStartup]
public sealed class VehicleConduits : VehiclePipes.ILink
{
    const string Key = "PowerConduits";

    static VehicleConduits() => VehiclePipes.Register(new VehicleConduits());

    static ThingDef? conduit;

    /// <summary>The game's hidden conduit (a power conduit that draws nothing; ludeon.rimworld's HiddenConduit), or the ordinary one if it is ever missing.</summary>
    public static ThingDef Conduit => conduit ??= DefDatabase<ThingDef>.GetNamedSilentFail("HiddenConduit") ?? ThingDefOf.PowerConduit;

    public static Thing MakeConduit() => ThingMaker.MakeThing(Conduit);

    /// <summary>A 12 or 24 volt battery of our electrics, or a traction battery: anything that charges from shore power.</summary>
    static bool HasBattery(CompPhobosVehicleSystems comp) =>
        comp.Props.systems.Any(system => system.def.quantities.Any(quantity => quantity.name == "charge"))
        || comp.Vehicle.CompFueledTravel is { Props.electricPowered: true };

    static bool CanLay(IntVec3 cell, Map map)
    {
        TerrainDef terrain = cell.GetTerrain(map);
        if (terrain.IsWater) return false;
        TerrainAffordanceDef? needed = Conduit.terrainAffordanceNeeded;
        return needed == null || terrain.affordances.Contains(needed);
    }

    public void Update(CompPhobosVehicleSystems comp)
    {
        var underlay = comp.Underlay(Key);
        bool generatorSetUp = comp.Vehicle.GetComp<CompPhobosGeneratorSet>() is { Deployed: true };
        if (!comp.ParkedForPipes || generatorSetUp || !HasBattery(comp))
        {
            underlay.Clear();
            return;
        }
        Map map = comp.Vehicle.Map;
        underlay.Renew(Conduit, MakeConduit);
        underlay.Fill(comp.Vehicle.OccupiedRect(), map, cell => cell.GetTransmitter(map) != null, MakeConduit, cell => CanLay(cell, map));
    }

    public void Leave(CompPhobosVehicleSystems comp) => comp.Underlay(Key).Clear();

    /// <summary>Visible conduits laid by 0.107.0 or 0.108.0 become hidden ones after a load.</summary>
    public void AfterLoad(CompPhobosVehicleSystems comp)
    {
        if (!comp.Vehicle.Spawned) return;
        comp.Underlay(Key).Renew(Conduit, MakeConduit);
    }
}
