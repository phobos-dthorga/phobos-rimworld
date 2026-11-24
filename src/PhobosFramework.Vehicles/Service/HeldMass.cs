using System.Collections.Generic;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// What another mod's own hold carries, as weight our engine model counts (Framework 0.254.0; the owner's card of 23 Nov 2026: the C-17
/// Globemaster's hold keeps the vehicles loaded into it in a comp of its own, outside the cargo, so a laden C-17 flew as if empty). A loadout
/// names such comps by their full type names (<see cref="PhobosVehicleLoadoutDef.heldMassComps"/>); every thing each holds counts: a
/// vehicle by its own mass stat (cargo included, as the C-17's own sum takes it), a pawn by its mass, anything else by its mass times its
/// count. Matched by name, so no reference to the other mod's assembly is needed, and a comp missing from a later version weighs nothing.
/// </summary>
public static class HeldMass
{
    public static double Kilograms(VehiclePawn vehicle, PhobosVehicleLoadoutDef? loadout)
    {
        if (loadout == null || loadout.heldMassComps.Count == 0) return 0;
        double total = 0;
        List<ThingComp> comps = vehicle.AllComps;
        for (int i = 0; i < comps.Count; i++)
        {
            if (comps[i] is not IThingHolder holder || !loadout.heldMassComps.Contains(comps[i].GetType().FullName ?? "")) continue;
            ThingOwner? held = holder.GetDirectlyHeldThings();
            if (held == null) continue;
            for (int j = 0; j < held.Count; j++) total += Of(held[j]);
        }
        if (total > 0 && Seen.Wants("vehicles.held-mass", vehicle)) Seen.Note("vehicles.held-mass", vehicle, null, null,
            new[] { new KeyValuePair<string, double>("kilograms", total) });
        return total;
    }

    static double Of(Thing thing) => thing switch
    {
        // A vehicle in the hold weighs its kilograms (fault-read 25 Nov 2026, E3): Vehicle Framework's Mass is on each author's own scale.
        VehiclePawn held => held.GetComp<CompPhobosVehicleSystems>()?.Powertrain?.MassKg() ?? Powertrain.KerbFor(held.VehicleDef, null, null),
        Pawn pawn => pawn.GetStatValue(StatDefOf.Mass),
        _ => thing.GetStatValue(StatDefOf.Mass) * thing.stackCount,
    };
}
