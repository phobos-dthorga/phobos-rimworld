using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Time for what a vehicle holds where the game gives it none (Framework 0.216.0; docs/development/vehicle-field-kitchen.md). The game ages a
/// held thing only when its holder ticks it. Two holders of ours never did: a trailer riding in its hitch off the map has no ticks at all, so
/// its cargo stood still in time, and the pieces a trailer packs away were never ticked on a map or off it, so food in a packed cold box never
/// aged. Each now gets the game's own rare tick, once per rare tick of whoever carries it, so food there rots at the temperature it is kept
/// at, as it does in any other cargo.
/// </summary>
public static class HeldGoods
{
    /// <summary>One rare tick for everything a vehicle riding in a hitch carries: its cargo, and what its packed pieces hold.</summary>
    public static void AgeTowed(VehiclePawn towed)
    {
        Age(towed.inventory?.innerContainer, towed);
        AgePacked(towed);
    }

    /// <summary>One rare tick for what a vehicle's packed pieces hold (a packed cold box's food).</summary>
    public static void AgePacked(VehiclePawn vehicle)
    {
        if (vehicle.GetComp<CompPhobosSetOut>() is not { } setOut) return;
        foreach (Thing piece in setOut.PackedPieces)
        {
            if (piece is IThingHolder holder) Age(holder.GetDirectlyHeldThings(), vehicle);
            // A store's load has its own clock (Framework 0.347.0: a packed paste tap holds no items, so its paste never aged on the road).
            if (piece.TryGetComp<CompPhobosReservoir>() is { } store) store.CompTickRare();
        }
    }

    static void Age(ThingOwner? held, VehiclePawn carrier)
    {
        if (held == null) return;
        bool aged = false;
        for (int index = held.Count - 1; index >= 0; index--)
        {
            Thing thing = held[index];
            // What the game ticks rarely is what rots, spoils or decays; nothing else in a cargo needs its time.
            if (thing.def.tickerType != TickerType.Rare || thing.Destroyed) continue;
            thing.TickRare();
            aged = true;
            if (thing.Destroyed && index < held.Count && held[index] == thing) held.Remove(thing);
        }
        if (aged && Seen.Wants("trailers.held-goods-age", carrier)) Seen.Note("trailers.held-goods-age", carrier, carrier.Spawned ? "packed" : "in tow");
    }
}

/// <summary>
/// A field kitchen's cold box on the road (Framework 0.216.0; the owner, 22 Nov 2026: "cold box stays cold in tow"): packed for the road it
/// runs through its trailer's lead from the vehicle that tows it, as one more load on that vehicle's electrics, drawing the box's own working
/// watts while it holds food. Cold only while that load runs: switched on, not shed, the battery above flat. Unhitched and packed, it is only
/// a box.
/// </summary>
public static class ColdInTow
{
    /// <summary>The load's name in the rules Def's tow loads.</summary>
    public const string LoadName = "trailerColdBox";

    /// <summary>The watts the packed cold boxes behind this vehicle ask now: each one's own working watts, while it holds anything.</summary>
    public static float Watts(VehiclePawn tractor, bool onlyInUse)
    {
        float watts = 0f;
        foreach (VehiclePawn towed in CompPhobosHitch.Behind(tractor))
        {
            if (towed.GetComp<CompPhobosSetOut>() is not { } setOut) continue;
            foreach (Thing piece in setOut.PackedPieces)
            {
                if (piece is Building_PhobosColdBox box && (!onlyInUse || box.GetDirectlyHeldThings().Count > 0)) watts += box.WattsNow;
            }
        }
        return watts;
    }

    /// <summary>
    /// The towing vehicle's look found its cold box load running: every packed cold box behind it is told so, and stays cold until two looks
    /// pass without word. Told from the tractor's side, so it holds for a trailer riding in the hitch off the map, which no list of tows names
    /// after a game is loaded.
    /// </summary>
    public static void Chill(VehiclePawn tractor)
    {
        int now = Find.TickManager.TicksGame;
        foreach (VehiclePawn towed in CompPhobosHitch.Behind(tractor))
        {
            if (towed.GetComp<CompPhobosSetOut>() is not { } setOut) continue;
            foreach (Thing piece in setOut.PackedPieces)
            {
                if (piece is not Building_PhobosColdBox box) continue;
                box.ChilledInTow(now);
                if (box.GetDirectlyHeldThings().Count > 0 && Seen.Wants("trailers.cold-box-in-tow", towed)) Seen.Note("trailers.cold-box-in-tow", towed, box.GetDirectlyHeldThings().Count + " stacks");
            }
        }
    }
}

/// <summary>A packed cold box rides behind this vehicle (the tow load is fitted); with <c>inUse</c>, one that holds food (it draws).</summary>
public class When_TowedColdBox : VehicleSystemCondition
{
    public bool inUse;

    protected override bool HoldsRaw(SystemContext context) =>
        context.Comp.Hitch is { Trailer: not null } && ColdInTow.Watts(context.Vehicle, inUse) > 0f;
}

/// <summary>What the cold boxes in tow draw now, in amps at this vehicle's voltage: their own working watts, from their Defs.</summary>
public class Input_TowedColdBoxAmps : VehicleSystemInput
{
    protected override float Raw(SystemContext context)
    {
        float volts = context.Comp.ElectricalVolts();
        return volts > 0f ? ColdInTow.Watts(context.Vehicle, onlyInUse: true) / volts : 0f;
    }
}
