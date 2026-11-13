using System;
using System.Collections.Generic;
using System.Linq;
using Rimefeller;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems.Oil;

/// <summary>
/// The tanker on Rimefeller's own pipes (Framework 0.109.0; soft-dependency-crosslinks.md, SD4; owner, 13 Nov 2026): a parked tanker set to
/// carry crude or chemfuel lays Rimefeller's hidden oil pipeline beneath its whole footprint, as it lays plumbing and other pipes (owner's
/// rule: anything that takes piping has its own pipes beneath it), so a run touching any side joins it. Rimefeller keeps crude and chemfuel
/// in one network; the ports below pump each through it. Rimefeller's network is its own, not a Vanilla Expanded one, so PipeNetLink
/// cannot do this. The pieces are ours, taken up when the tanker leaves or stops carrying oil.
/// </summary>
[StaticConstructorOnStartup]
public sealed class RimefellerPipeLink : VehiclePipes.ILink
{
    const string Key = "Rimefeller:Oil";

    static RimefellerPipeLink() => VehiclePipes.Register(new RimefellerPipeLink());

    static ThingDef? HiddenPipe => DefDatabase<ThingDef>.GetNamedSilentFail("OilPipelineHidden");

    static Thing? Make() => HiddenPipe is { } def ? ThingMaker.MakeThing(def) : null;

    /// <summary>A Rimefeller pipe, tank or other piped building stands on the cell.</summary>
    internal static bool Piped(IntVec3 cell, Map map) => cell.GetThingList(map).Any(t => t.TryGetComp<CompPipe>() != null);

    static bool CarriesOil(VehiclePipes.IHost comp)
    {
        CompPhobosTank? tank = comp.Vehicle.GetComp<CompPhobosTank>();
        return tank != null && tank.Compartments.Any(c => c.contents?.ports.OfType<Port_RimefellerOil>().Any() == true);
    }

    public void Update(VehiclePipes.IHost comp)
    {
        var underlay = comp.Underlay(Key);
        if (!comp.ParkedForPipes || HiddenPipe == null || !CarriesOil(comp))
        {
            underlay.Clear();
            return;
        }
        Map map = comp.Vehicle.Map;
        underlay.Renew(HiddenPipe, Make);
        underlay.Fill(comp.Vehicle.OccupiedRect(), map, cell => Piped(cell, map), Make, cell => !cell.GetTerrain(map).IsWater);
    }

    public void AfterLoad(VehiclePipes.IHost comp)
    {
        if (comp.Vehicle.Spawned && HiddenPipe != null) comp.Underlay(Key).Renew(HiddenPipe, Make);
    }

    public void Leave(VehiclePipes.IHost comp) => comp.Underlay(Key).Clear();
}

/// <summary>
/// Crude or chemfuel through the Rimefeller pipeline beneath the parked tanker. Chemfuel: one unit is one chemfuel, as Rimefeller's own
/// pipes refuel buildings (read from its code, CompPipe). Crude: Rimefeller's code gives no rate between its oil barrels and its stored
/// crude, so one unit to one barrel is authored. Pumping in takes only what is really stored; pumping out stores only what fits.
/// </summary>
public class Port_RimefellerOil : TankPort
{
    /// <summary>Chemfuel when true, crude oil when false.</summary>
    public bool chemfuel;

    static PipelineNet? NetUnder(CompPhobosTank tank)
    {
        Map map = tank.Vehicle.Map;
        foreach (IntVec3 cell in tank.Vehicle.OccupiedRect())
        {
            if (!cell.InBounds(map)) continue;
            foreach (Thing thing in cell.GetThingList(map))
            {
                if (thing.TryGetComp<CompPipe>()?.pipeNet is { } net) return net;
            }
        }
        return null;
    }

    public override float Draw(CompPhobosTank tank, float most, out string? reason)
    {
        reason = null;
        PipelineNet? net = NetUnder(tank);
        if (net == null)
        {
            reason = "PhobosVehicleSystems_TankNoPipe".Translate().Resolve();
            return 0f;
        }
        double stored = chemfuel ? net.FuelStorage.Where(t => !t.DrainTank).Sum(t => (double)t.Storage) : net.OilStorage.Sum(t => (double)t.Storage);
        float taken = (float)Math.Min(Math.Min(most, tank.Space), stored);
        if (taken <= 0f || !(chemfuel ? net.PullFuel(taken) : net.PullOil(taken)))
        {
            reason = "PhobosVehicleSystems_TankPipeEmpty".Translate().Resolve();
            return 0f;
        }
        return taken;
    }

    public override float Give(CompPhobosTank tank, float most, out string? reason)
    {
        reason = null;
        PipelineNet? net = NetUnder(tank);
        if (net == null)
        {
            reason = "PhobosVehicleSystems_TankNoPipe".Translate().Resolve();
            return 0f;
        }
        // Rimefeller stores nothing when the whole amount does not fit, so offer only what the tanks have room for.
        IEnumerable<CompStorageTank> tanks = (chemfuel ? net.FuelStorage : net.OilStorage).Where(t => t.space > 0f && !t.DrainTank);
        float offered = Math.Min(most, tanks.Sum(t => t.space));
        if (offered <= 0f)
        {
            reason = "PhobosVehicleSystems_TankPipeFull".Translate().Resolve();
            return 0f;
        }
        double left = chemfuel ? net.PushFuel(offered) : net.PushCrude(offered);
        float stored = (float)Math.Max(0.0, offered - left);
        if (stored <= 0f) reason = "PhobosVehicleSystems_TankPipeFull".Translate().Resolve();
        return stored;
    }
}
