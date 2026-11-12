using System.Linq;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A vehicle with a battery lays the game's own visible power conduit across its footprint while it stands with its engine off (Framework 0.108.0; owner,
/// 13 Nov 2026: "every vehicle with a battery", always, "so players are not confused"), so a conduit touching any side joins it to the grid, as a building on
/// a conduit run joins. Its battery then charges from that grid by the shore power the electrics already have: the conduits beneath it are things on a
/// powered net standing within the vehicle's reach (CompPhobosVehicleSystems.ShorePowered), so nothing is counted twice. Taken up when the engine starts or
/// it moves. The generator set lays its own conduits while it is set up to feed the grid through them (CompPhobosGeneratorSet), so this one stands down
/// while it is set up; packed up and parked with its engine off, it follows this rule like any vehicle.
/// </summary>
[StaticConstructorOnStartup]
public sealed class VehicleConduits : VehiclePipes.ILink
{
    const string Key = "PowerConduits";

    static VehicleConduits() => VehiclePipes.Register(new VehicleConduits());

    /// <summary>A 12 or 24 volt battery of our electrics, or a traction battery: anything that charges from shore power.</summary>
    static bool HasBattery(CompPhobosVehicleSystems comp) =>
        comp.Props.systems.Any(system => system.def.quantities.Any(quantity => quantity.name == "charge"))
        || comp.Vehicle.CompFueledTravel is { Props.electricPowered: true };

    static bool CanLay(IntVec3 cell, Map map)
    {
        TerrainDef terrain = cell.GetTerrain(map);
        if (terrain.IsWater) return false;
        TerrainAffordanceDef? needed = ThingDefOf.PowerConduit.terrainAffordanceNeeded;
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
        underlay.Fill(comp.Vehicle.OccupiedRect(), map, cell => cell.GetTransmitter(map) != null, () => ThingMaker.MakeThing(ThingDefOf.PowerConduit),
            cell => CanLay(cell, map));
    }

    public void Leave(CompPhobosVehicleSystems comp) => comp.Underlay(Key).Clear();

    /// <summary>Conduits were never laid hidden, so there is nothing to take back.</summary>
    public void AfterLoad(CompPhobosVehicleSystems comp)
    {
    }
}
