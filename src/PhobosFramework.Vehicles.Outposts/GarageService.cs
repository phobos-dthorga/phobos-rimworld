using System;
using System.Collections.Generic;
using System.Linq;
using Outposts;
using Phobos.Framework.Rules;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems.Garage;

/// <summary>
/// Every rule of the garage: who may found one, parking, taking vehicles out, repairs and what happens when one closes. The commands and
/// windows only read from here and call it. Parking and taking out follow Vehicle Framework's own stash, so crew and cargo move as they do
/// when a player stashes a vehicle.
/// </summary>
public static class GarageService
{
    static WorldObjectDef? garageDef;

    /// <summary>The first outpost Def whose class is a garage, found by class rather than by name so other authors can add their own.</summary>
    public static WorldObjectDef? GarageDef => garageDef ??= DefDatabase<WorldObjectDef>.AllDefs.FirstOrDefault(
        d => d.worldObjectClass != null && typeof(Outpost_PhobosGarage).IsAssignableFrom(d.worldObjectClass));

    public static bool GarageAt(PlanetTile tile) => Find.WorldObjects.AllWorldObjects.OfType<Outpost_PhobosGarage>().Any(g => g.Tile == tile);

    /// <summary>Why this convoy cannot leave its vehicles anywhere at the moment, or null.</summary>
    public static string? ConvoyRefusal(VehicleCaravan convoy)
    {
        if (convoy.VehiclesListForReading.Count == 0) return "PhobosGarage_RefusedNoVehicles".Translate();
        if (convoy.vehiclePather.Moving) return "PhobosGarage_RefusedMoving".Translate();
        if (convoy.AerialVehicle || convoy.VehiclesListForReading.Any(v => !v.VehicleDef.canCaravan)) return "PhobosGarage_RefusedAircraft".Translate();
        return null;
    }

    public static IEnumerable<Pawn> PeopleOf(VehicleCaravan convoy) => convoy.AllPawnsAndVehiclePassengers.Where(p => p.IsFreeColonist);

    /// <summary>Why a garage cannot be founded here with these people staying, or null. The same checks as the outposts code's own window.</summary>
    public static string? FoundRefusal(VehicleCaravan convoy, WorldObjectDef def, List<Pawn> staying)
    {
        string? refusal = ConvoyRefusal(convoy);
        if (refusal != null) return refusal;
        if (staying.Count == 0) return "PhobosGarage_RefusedNobodyStays".Translate();
        var figures = def.GetModExtension<PhobosGarageExtension>();
        if (figures == null) return "PhobosGarage_RefusedNoVehicles".Translate();
        refusal = figures.CanSpawnOnWithExt(convoy.Tile, staying);
        if (refusal != null) return refusal;
        int bays = GarageRule.Bays(staying.Count, figures.baysPerPerson, figures.maxBays);
        int vehicles = convoy.VehiclesListForReading.Count;
        if (vehicles > bays) return "PhobosGarage_RefusedTooManyVehicles".Translate(vehicles.Named("COUNT"), bays.Named("BAYS"));
        return null;
    }

    public static Outpost_PhobosGarage? Found(VehicleCaravan convoy, WorldObjectDef def, List<Pawn> staying)
    {
        var figures = def.GetModExtension<PhobosGarageExtension>();
        PayCost(convoy, figures);
        Faction faction = convoy.Faction;
        PlanetTile tile = convoy.Tile;
        StashedVehicle stash = StashedVehicle.Create(convoy, out Caravan crew);
        if (stash == null) return null;
        var garage = (Outpost_PhobosGarage)WorldObjectMaker.MakeWorldObject(def);
        garage.Name = NameGenerator.GenerateName(faction.def.settlementNameMaker,
            Find.WorldObjects.AllWorldObjects.OfType<Outpost>().Select(o => o.Name), false);
        garage.Tile = tile;
        garage.SetFaction(faction);
        Find.WorldObjects.Add(garage);
        foreach (Pawn person in staying) garage.AddPawn(person);
        MoveInto(stash, garage);
        CloseIfEmpty(crew);
        return garage;
    }

    public static string? ParkRefusal(Outpost_PhobosGarage garage, VehicleCaravan convoy)
    {
        string? refusal = ConvoyRefusal(convoy);
        if (refusal != null) return refusal;
        int bays = Bays(garage);
        int total = garage.ParkedCount + convoy.VehiclesListForReading.Count;
        return total > bays ? "PhobosGarage_RefusedTooManyVehicles".Translate(total.Named("COUNT"), bays.Named("BAYS")) : null;
    }

    public static void Park(Outpost_PhobosGarage garage, VehicleCaravan convoy)
    {
        int count = convoy.VehiclesListForReading.Count;
        StashedVehicle stash = StashedVehicle.Create(convoy, out Caravan crew);
        if (stash == null) return;
        MoveInto(stash, garage);
        CloseIfEmpty(crew);
        Messages.Message("PhobosGarage_ParkDone".Translate(count.Named("COUNT"), garage.Name.Named("NAME")), garage, MessageTypeDefOf.TaskCompletion, false);
    }

    public static string? TakeOutRefusal(Caravan caravan, List<Pawn> chosen)
    {
        if (chosen.Count == 0) return "PhobosGarage_RefusedNoneChosen".Translate();
        if (caravan is VehicleCaravan { AerialVehicle: true }) return "PhobosGarage_RefusedAircraft".Translate();
        IEnumerable<Pawn> people = caravan is VehicleCaravan convoy ? convoy.AllPawnsAndVehiclePassengers : caravan.PawnsListForReading;
        return people.Any(p => p.IsFreeColonist) ? null : "PhobosGarage_RefusedNoDriver".Translate();
    }

    /// <summary>Hands the chosen vehicles to a caravan at the garage, in the order of steps Vehicle Framework uses to collect a stash.</summary>
    public static void TakeOut(Outpost_PhobosGarage garage, Caravan caravan, List<VehiclePawn> vehicles)
    {
        var pawns = caravan.pawns.InnerListForReading.ToList();
        caravan.RemoveAllPawns();
        foreach (VehiclePawn vehicle in vehicles) garage.Parked.Remove(vehicle);
        RoleHelper.Distribute(in vehicles, pawns);
        pawns.AddRange(vehicles);
        VehicleCaravan convoy = CaravanHelper.MakeVehicleCaravan(pawns, caravan.Faction, caravan.Tile, addToWorldPawnsIfNotAlready: true);
        convoy.RecacheVehicles();
        caravan.Destroy();
        Messages.Message("PhobosGarage_TakeOutDone".Translate(vehicles.Count.Named("COUNT"), garage.Name.Named("NAME")), convoy, MessageTypeDefOf.TaskCompletion, false);
    }

    public static int Bays(Outpost_PhobosGarage garage) => GarageRule.Bays(garage.PawnCount, garage.Figures.baysPerPerson, garage.Figures.maxBays);

    static float Damage(Outpost_PhobosGarage garage) =>
        garage.HeldVehicles.Sum(v => v.statHandler.ComponentsPrioritized.Sum(c => Math.Max(c.MaxHealth - c.Health, 0f)));

    static float PointsPerCycle(Outpost_PhobosGarage garage) =>
        GarageRule.PointsPerCycle(garage.TotalSkill(SkillDefOf.Construction), garage.Figures.repairPointsPerSkillLevel);

    static int Held(Outpost_PhobosGarage garage, ThingDef def) => garage.Things.Where(t => t.def == def).Sum(t => t.stackCount);

    static GarageRepairBudget Budget(Outpost_PhobosGarage garage, float damage)
    {
        PhobosGarageExtension figures = garage.Figures;
        return GarageRule.Spend(damage, PointsPerCycle(garage), Held(garage, ThingDefOf.Steel), Held(garage, ThingDefOf.ComponentIndustrial),
            figures.repairPointsPerSteel, figures.repairPointsPerComponent);
    }

    /// <summary>One repair cycle, from the outposts code's production timer. Vehicle Framework's part health stays the one authority.</summary>
    public static void RepairCycle(Outpost_PhobosGarage garage)
    {
        float damage = Damage(garage);
        if (damage <= 0f) return;
        GarageRepairBudget budget = Budget(garage, damage);
        if (budget.Points <= 0f) return;
        Spend(garage, ThingDefOf.Steel, budget.Steel);
        Spend(garage, ThingDefOf.ComponentIndustrial, budget.Components);
        float left = budget.Points;
        foreach (VehiclePawn vehicle in garage.HeldVehicles)
        {
            foreach (VehicleComponent part in vehicle.statHandler.ComponentsPrioritized)
            {
                if (left <= 0f) return;
                float missing = part.MaxHealth - part.Health;
                if (missing <= 0f) continue;
                float heal = Math.Min(missing, left);
                float before = part.Health;
                part.HealComponent(heal);
                vehicle.GetComp<CompPhobosVehicleSystems>()?.Notify_RepairedAway(part, before);
                left -= heal;
            }
        }
    }

    /// <summary>The garage's inspect line: its bays, and the one thing its repairs are waiting for.</summary>
    public static string StatusLine(Outpost_PhobosGarage garage)
    {
        string bays = "PhobosGarage_InspectBays".Translate(garage.ParkedCount.Named("USED"), Bays(garage).Named("BAYS"));
        float damage = Damage(garage);
        if (damage <= 0f) return bays;
        GarageRepairBudget budget = Budget(garage, damage);
        string repairs = PointsPerCycle(garage) <= 0f ? "PhobosGarage_WaitingSkill".Translate()
            : budget.Limit == GarageRepairLimit.Steel && budget.Points <= 0f ? "PhobosGarage_WaitingSteel".Translate()
            : budget.Limit == GarageRepairLimit.Components && budget.Points <= 0f ? "PhobosGarage_WaitingComponents".Translate()
            : "PhobosGarage_RepairNext".Translate(garage.TimeTillProduction.Named("TIME"));
        return bays + "\n" + repairs;
    }

    /// <summary>A garage that is closing leaves its vehicles behind as an ordinary parked stash, with a letter. They are never deleted here.</summary>
    public static void ReleaseAll(Outpost_PhobosGarage garage)
    {
        int count = garage.ParkedCount;
        if (garage.Parked.Count == 0) return;
        var stash = (StashedVehicle)WorldObjectMaker.MakeWorldObject(WorldObjectDefOfVehicles.StashedVehicle);
        stash.Tile = garage.Tile;
        garage.Parked.TryTransferAllToContainer(stash.GetDirectlyHeldThings(), false);
        int days = garage.Figures.releasedVehicleDays;
        stash.GetComponent<TimeoutComp>().StartTimeout(days * GenDate.TicksPerDay);
        Find.WorldObjects.Add(stash);
        Find.LetterStack.ReceiveLetter("PhobosGarage_ReleasedLabel".Translate(), "PhobosGarage_ReleasedText".Translate(count.Named("COUNT"),
            garage.Name.Named("NAME"), days.Named("DAYS")), LetterDefOf.NegativeEvent, stash);
    }

    static void MoveInto(StashedVehicle stash, Outpost_PhobosGarage garage)
    {
        stash.GetDirectlyHeldThings().TryTransferAllToContainer(garage.Parked, false);
        stash.Destroy();
    }

    static void CloseIfEmpty(Caravan crew)
    {
        if (!crew.Destroyed && crew.PawnsListForReading.Count == 0) crew.Destroy();
    }

    static void PayCost(VehicleCaravan convoy, PhobosGarageExtension figures)
    {
        if (figures.CostToMake == null) return;
        foreach (ThingDefCountClass cost in figures.CostToMake)
        {
            int left = cost.count;
            foreach (Thing thing in CaravanInventoryUtility.AllInventoryItems(convoy).Where(t => t.def == cost.thingDef).ToList())
            {
                if (left <= 0) break;
                left -= TakeAndDestroy(thing, left);
            }
        }
    }

    // One stack at a time: the outposts code's TakeItems goes wrong when a request spans several stacks.
    static void Spend(Outpost_PhobosGarage garage, ThingDef def, int count)
    {
        foreach (Thing thing in garage.Things.Where(t => t.def == def).ToList())
        {
            if (count <= 0) return;
            if (thing.stackCount <= count) garage.TakeItem(thing);
            count -= TakeAndDestroy(thing, count);
        }
    }

    static int TakeAndDestroy(Thing thing, int wanted)
    {
        int taken = Math.Min(thing.stackCount, wanted);
        if (taken == thing.stackCount) thing.Destroy();
        else thing.SplitOff(taken).Destroy();
        return taken;
    }
}
