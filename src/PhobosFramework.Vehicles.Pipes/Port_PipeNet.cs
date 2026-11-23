using System;
using System.Collections.Generic;
using RimWorld;
using PipeSystem;
using Verse;

namespace Phobos.VehicleSystems.Pipes;

/// <summary>
/// A Vanilla Expanded Framework pipe network under or beside the tanker. The tanker never registers as a connector: the framework
/// registers those at spawn from their neighbours, and a thing that moves would leave a stale one. It reads the net at its cells and
/// moves units through the net's public storage calls, only while set up.
/// </summary>
public class Port_PipeNet : TankPort
{
    public PipeNetDef? net;

    public override float Draw(CompPhobosTank tank, float most, out string? reason)
    {
        reason = null;
        PipeNet? found = Find(tank);
        if (found == null)
        {
            reason = "PhobosVehicleSystems_TankNoPipe".Translate().Resolve();
            return 0f;
        }
        float taken = Math.Min(most, found.Stored);
        if (taken <= 0f)
        {
            reason = "PhobosVehicleSystems_TankPipeEmpty".Translate().Resolve();
            return 0f;
        }
        // The net's stored figure is refreshed only now and then, so what came out, not what was asked for.
        found.DrawAmongStorage(taken, out float drawn, null);
        if (drawn <= 0f) reason = "PhobosVehicleSystems_TankPipeEmpty".Translate().Resolve();
        // What the net's stores were made from joins the load (NP1: Vanilla Nutrient Paste Expanded's vats keep their paste's
        // ingredients, and its taps give every vat's to each meal). Read through the game's own ingredient comp; nets with none add nothing.
        else
        {
            tank.AddIngredients(IngredientsOf(found));
            if (tank.Ingredients.Count > 0 && Seen.Wants("vehicles.tank-ingredients", tank.Vehicle, "in"))
                Seen.Note("vehicles.tank-ingredients", tank.Vehicle, tank.Ingredients.Count + " ingredient(s) taken in", "in");
        }
        return drawn;
    }

    public override float Give(CompPhobosTank tank, float most, out string? reason)
    {
        reason = null;
        PipeNet? found = Find(tank);
        if (found == null)
        {
            reason = "PhobosVehicleSystems_TankNoPipe".Translate().Resolve();
            return 0f;
        }
        float given = Math.Min(most, found.AvailableCapacity);
        if (given <= 0f)
        {
            reason = "PhobosVehicleSystems_TankPipeFull".Translate().Resolve();
            return 0f;
        }
        // Its capacity counts tanks marked for transfer, which take nothing: only what was really stored leaves the tanker.
        found.DistributeAmongStorage(given, out float stored);
        if (stored <= 0f) reason = "PhobosVehicleSystems_TankPipeFull".Translate().Resolve();
        // The stores learn what the load was made from (NP1), as the net's taps read them all together.
        else
        {
            RegisterIngredients(found, tank.Ingredients);
            if (tank.Ingredients.Count > 0 && Seen.Wants("vehicles.tank-ingredients", tank.Vehicle, "out"))
                Seen.Note("vehicles.tank-ingredients", tank.Vehicle, tank.Ingredients.Count + " ingredient(s) handed on", "out");
        }
        return stored;
    }

    /// <summary>Every ingredient the net's stores keep, through the game's own ingredient comp on each store.</summary>
    static IEnumerable<ThingDef> IngredientsOf(PipeNet net)
    {
        foreach (CompResourceStorage storage in net.storages)
        {
            if (storage?.parent?.GetComp<CompIngredients>() is not { } kept) continue;
            foreach (ThingDef ingredient in kept.ingredients) yield return ingredient;
        }
    }

    /// <summary>Tells every store on the net that it now holds something made from these.</summary>
    static void RegisterIngredients(PipeNet net, IReadOnlyList<ThingDef> ingredients)
    {
        if (ingredients.Count == 0) return;
        foreach (CompResourceStorage storage in net.storages)
        {
            if (storage?.parent?.GetComp<CompIngredients>() is not { } kept) continue;
            for (int i = 0; i < ingredients.Count; i++) kept.RegisterIngredient(ingredients[i]);
        }
    }

    PipeNet? Find(CompPhobosTank tank)
    {
        if (net == null) return null;
        Map map = tank.Vehicle.Map;
        PipeNetManager? manager = map.GetComponent<PipeNetManager>();
        if (manager == null) return null;
        foreach (IntVec3 cell in tank.ContactCells())
        {
            if (!cell.InBounds(map)) continue;
            PipeNet? found = manager.GetPipeNetAt(cell, net);
            if (found != null) return found;
        }
        return null;
    }
}
