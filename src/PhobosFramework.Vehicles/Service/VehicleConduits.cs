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

    /// <summary>
    /// Our hidden hook-up conduit (Framework 0.120.0: a hidden conduit that is also a power consumer, so the grid pays for what the vehicle takes;
    /// PhobosVehicleHookUp), else the game's hidden conduit, else the ordinary one. The hidden conduits earlier versions laid are swapped for it
    /// at the next look (Renew).
    /// </summary>
    public static ThingDef Conduit => conduit ??= DefDatabase<ThingDef>.GetNamedSilentFail(HookUpDefName)
        ?? DefDatabase<ThingDef>.GetNamedSilentFail("HiddenConduit") ?? ThingDefOf.PowerConduit;

    public const string HookUpDefName = "PhobosVehicleHookUp";

    /// <summary>The power consumer that meters the grid's draw for this vehicle: the first piece standing under it, while its conduits lie there.</summary>
    public static CompPowerTrader? Meter(VehiclePipes.IHost host)
    {
        if (!host.ParkedForPipes) return null;
        return host.Underlay(Key).FirstStanding?.TryGetComp<CompPowerTrader>();
    }

    public static Thing MakeConduit() => ThingMaker.MakeThing(Conduit);

    /// <summary>
    /// A 12 or 24 volt battery of our electrics, or a traction battery: anything that charges from shore power. Framework 0.283.0: or a tank
    /// trailer with its electric pump fitted, which runs on the grid through the same hook-up while it is parked.
    /// </summary>
    static bool HasBattery(VehiclePipes.IHost comp) =>
        (comp is CompPhobosVehicleSystems systems && systems.Props.systems.Any(system => system.def.quantities.Any(quantity => quantity.name == "charge")))
        || comp.Vehicle.CompFueledTravel is { Props.electricPowered: true }
        || (comp is CompPhobosTrailer && comp.Vehicle.GetComp<CompPhobosTank>() is { Props.needsPumpPower: true } tank
            && (tank.ElectricPump || WaterTreatment.Fitted(tank)));

    static bool CanLay(IntVec3 cell, Map map)
    {
        TerrainDef terrain = cell.GetTerrain(map);
        if (terrain.IsWater) return false;
        TerrainAffordanceDef? needed = Conduit.terrainAffordanceNeeded;
        return needed == null || terrain.affordances.Contains(needed);
    }

    public void Update(VehiclePipes.IHost comp)
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

    public void Leave(VehiclePipes.IHost comp) => comp.Underlay(Key).Clear();

    /// <summary>Visible conduits laid by 0.107.0 or 0.108.0 become hidden ones after a load.</summary>
    public void AfterLoad(VehiclePipes.IHost comp)
    {
        if (!comp.Vehicle.Spawned) return;
        comp.Underlay(Key).Renew(Conduit, MakeConduit);
    }
}
