using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using Phobos.VehicleSystems;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.Farming;

/// <summary>A harvesting front a combine can be fitted with, by upgrade: how wide it cuts and which crop classes it takes.</summary>
public class PhobosHarvestFront
{
    /// <summary>The upgrade node that fits it. Fronts are either/or: the first fitted one counts.</summary>
    public string node = "";
    /// <summary>How many cells wide its strip is (owner, 20 Nov 2026: 6 and 8 for the grain fronts, centred on the 4-cell body).</summary>
    public int width = 6;
    /// <summary>The crop classes it takes.</summary>
    public List<PhobosCropClassDef> takes = new();
    /// <summary>The front's name in the player's words, for messages.</summary>
    [MustTranslate] public string label = "";
}

/// <summary>A combine's harvester, configured in its Def (docs/development/farming-batch-one.md, "The harvest steps").</summary>
public class CompProperties_PhobosHarvester : VehicleCompProperties
{
    public List<PhobosHarvestFront> fronts = new();
    /// <summary>The share of a hand harvester's Plants experience a driver earns for a plant cut by machine. Authored [A].</summary>
    public float driverExperienceShare = 0.25f;
    /// <summary>The upgrade node that chops straw into the field instead of leaving it lying (the straw chopper), or empty.</summary>
    public string strawChopperNode = "";
    /// <summary>The order button's picture.</summary>
    public string orderIconPath = "Phobos/Farming/UI/Harvest";

    public CompProperties_PhobosHarvester() => compClass = typeof(CompPhobosHarvester);
}

/// <summary>
/// A combine's harvesting front (docs/development/farming-batch-one.md): the field run drives, and this cuts the cells its strip covers,
/// by the game's own harvest steps with the driver standing in for the colonist (JobDriver_PlantWork, read 20 Nov 2026): the driver's
/// yield stat, the failure roll, YieldNow, the quest notice, the comps' extra yield, then the plant's own harvest hook with the vehicle as
/// the harvester, so other mods' harvest hooks still fire. What it cuts is tallied by the row and settled into the grain tank once a row;
/// straw and other second products are left lying in the field, as a combine leaves them, unless a straw chopper is fitted. It never
/// destroys an unripe plant, and takes only the crop classes its front takes.
/// </summary>
public class CompPhobosHarvester : VehicleComp, IVehicleFieldWorker
{
    /// <summary>The game's Plants experience a tick for a colonist harvesting by hand (JobDriver_PlantHarvest.Init, read 20 Nov 2026).</summary>
    const float HandExperiencePerTick = 0.085f;

    // Derived during a run, never saved: a run that resumes after a load starts its tallies afresh.
    readonly Dictionary<ThingDef, int> rowTally = new();
    readonly Dictionary<ThingDef, int> secondTally = new();
    readonly Dictionary<ThingDef, int> runTotals = new();
    Zone_Growing? area;
    int plants;
    int failed;

    public CompProperties_PhobosHarvester Props => (CompProperties_PhobosHarvester)props;

    CompPhobosBulkHold? Hold => Vehicle.GetComp<CompPhobosBulkHold>();

    /// <summary>The front fitted now, or null.</summary>
    public PhobosHarvestFront? Front => Props.fronts.FirstOrDefault(f => VehicleUpgrades.Unlocked(Vehicle, f.node));

    bool ChopsStraw => !Props.strawChopperNode.NullOrEmpty() && VehicleUpgrades.Unlocked(Vehicle, Props.strawChopperNode);

    // ---- The order ----

    public string OrderLabel => "PhobosFarming_GizmoHarvest".Translate().Resolve();

    public string OrderDescription => "PhobosFarming_GizmoHarvestDesc".Translate(Vehicle.LabelShort.Named("VEHICLE"),
        (Front?.label ?? "PhobosFarming_NoFrontWords".Translate().Resolve()).Named("FRONT")).Resolve();

    public string OrderIconPath => Props.orderIconPath;

    public int StripWidth => Front?.width ?? 1;

    public int StripAhead => 1;

    public bool Accepts(Zone zone) => zone is Zone_Growing && Paddocks.IsPaddock(zone);

    public string? CannotWork(Zone zone)
    {
        NamedArgument name = Vehicle.LabelShort.Named("VEHICLE");
        if (Front is not { } front) return "PhobosFarming_NoFront".Translate(name).Resolve();
        if (Hold == null) return "PhobosFarming_NoTank".Translate(name).Resolve();
        if (zone is not Zone_Growing growing) return null;
        ThingDef? crop = growing.GetPlantDefToGrow();
        if (crop != null && !Takes(front, crop))
        {
            return "PhobosFarming_FrontDoesNotSuit".Translate(front.label.Named("FRONT"), crop.label.Named("CROP")).Resolve();
        }
        if (!growing.Cells.Any(c => Ripe(c, growing) != null)) return "PhobosFarming_NothingRipe".Translate(growing.label.Named("ZONE")).Resolve();
        return null;
    }

    static bool Takes(PhobosHarvestFront front, ThingDef plant) => Crops.ClassOf(plant) is { } cropClass && front.takes.Contains(cropClass);

    /// <summary>The plant in this cell this front would cut now, or null: ripe and mature, allowed to be cut, able to yield, and of a crop class it takes.</summary>
    Plant? Ripe(IntVec3 cell, Zone_Growing? zone)
    {
        Map map = Vehicle.Map;
        if (!cell.InBounds(map) || Front is not { } front) return null;
        Plant? plant = cell.GetPlant(map);
        if (plant == null || !plant.HarvestableNow || plant.LifeStage != PlantLifeStage.Mature) return null;
        if (plant.TryGetComp<CompPlantPreventCutting>(out CompPlantPreventCutting prevent) && prevent.PreventCutting) return null;
        if (!plant.CanYieldNow()) return null;
        // As the game's own harvest scan: a zone that does not allow cutting keeps everything but the crop it grows.
        if (zone is { allowCut: false } && plant.def != zone.GetPlantDefToGrow()) return null;
        return Takes(front, plant.def) ? plant : null;
    }

    public bool HasWork(IReadOnlyList<IntVec3> rowCells)
    {
        for (int i = 0; i < rowCells.Count; i++)
        {
            if (Ripe(rowCells[i], area) != null) return true;
        }
        return false;
    }

    // ---- A run ----

    public void BeginRun(Zone zone)
    {
        area = zone as Zone_Growing;
        rowTally.Clear();
        secondTally.Clear();
        runTotals.Clear();
        plants = 0;
        failed = 0;
    }

    /// <summary>Whether the tank still has room for this product beyond what the row has already cut into it.</summary>
    bool RoomFor(ThingDef product)
    {
        CompPhobosBulkHold? hold = Hold;
        if (hold == null) return false;
        if (!hold.Contents.Takes(product, hold.MixesProducts) && !rowTally.ContainsKey(product)) return false;
        rowTally.TryGetValue(product, out int pending);
        return hold.Contents.RoomFor(product, hold.CapacityKg) > pending;
    }

    public void WorkCells(List<IntVec3> cells)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            if (Ripe(cells[i], area) is { } plant) Cut(plant);
        }
    }

    /// <summary>One plant, by the game's own steps (JobDriver_PlantWork's cut toil), with the driver standing in for the colonist.</summary>
    void Cut(Plant plant)
    {
        ThingDef? product = plant.def.plant.harvestedThingDef;
        // A full tank leaves the plant standing, never destroys it.
        if (product != null && !RoomFor(product)) return;
        Pawn? driver = Driver();
        bool humanlike = driver?.RaceProps.Humanlike == true;
        if (product != null)
        {
            StatDef stat = product.IsDrug || plant.def.plant.drugForHarvestPurposes ? StatDefOf.DrugHarvestYield : StatDefOf.PlantHarvestYield;
            float yieldStat = driver?.GetStatValue(stat) ?? 1f;
            bool fails = FarmingMod.Settings.driverCanFail && HarvestRule.Fails(humanlike, plant.def.plant.harvestFailable, plant.Blighted, yieldStat, Rand.Value);
            if (fails)
            {
                failed++;
            }
            else
            {
                int yield = HarvestRule.Yield(plant.YieldNow(), yieldStat, Rand.Value);
                if (yield > 0)
                {
                    Add(rowTally, product, yield);
                    if (driver != null)
                    {
                        Thing harvested = ThingMaker.MakeThing(product);
                        harvested.stackCount = yield;
                        Find.QuestManager.Notify_PlantHarvested(driver, harvested);
                        driver.records.Increment(RecordDefOf.PlantsHarvested);
                    }
                }
                if (plant.HarvestableNow)
                {
                    foreach (ThingComp comp in plant.AllComps)
                    {
                        foreach (ThingDefCountClass extra in comp.GetAdditionalHarvestYield()) Add(secondTally, extra.thingDef, extra.count);
                    }
                }
            }
            DualCrops.Second(plant, yieldStat, secondTally);
        }
        driver?.skills?.Learn(SkillDefOf.Plants, HarvestRule.DriverExperience(HandExperiencePerTick, plant.def.plant.harvestWork, Props.driverExperienceShare));
        plants++;
        if (Seen.Wants("farming.harvest.cell", Vehicle, Front?.node))
        {
            Seen.Note("farming.harvest.cell", Vehicle, plant.def.label, variant: Front?.node);
        }
        // The plant's own harvest hook, with the vehicle as the harvester: other mods' hooks fire, as for a hand.
        plant.PlantCollected(Vehicle, PlantDestructionMode.Chop);
    }

    static void Add(Dictionary<ThingDef, int> tally, ThingDef def, int count)
    {
        tally.TryGetValue(def, out int had);
        tally[def] = had + count;
    }

    Pawn? Driver()
    {
        if (!Vehicle.PawnsByHandlingType.TryGetValue(HandlingType.Movement, out List<Pawn>? drivers) || drivers == null) return null;
        return drivers.FirstOrDefault(p => p != null && !p.Dead);
    }

    public FieldWorkStop RowDone(out string? reason)
    {
        reason = null;
        CompPhobosBulkHold? hold = Hold;
        if (hold == null) return FieldWorkStop.Fault;
        foreach (ThingDef product in rowTally.Keys.ToList())
        {
            int count = rowTally[product];
            int added = hold.Receive(product, count);
            if (added > 0) Add(runTotals, product, added);
            if (added >= count) rowTally.Remove(product);
            else rowTally[product] = count - added;
        }
        LeaveSecondProducts();
        if (rowTally.Count > 0 || !hold.HasRoom) return FieldWorkStop.Full;
        return FieldWorkStop.None;
    }

    /// <summary>Straw and other second products are left lying in the field where the row ended, or chopped into it with a straw chopper.</summary>
    void LeaveSecondProducts()
    {
        if (secondTally.Count == 0) return;
        if (!ChopsStraw && Vehicle.Spawned)
        {
            foreach (KeyValuePair<ThingDef, int> second in secondTally)
            {
                if (Seen.Wants("farming.harvest.second-product", Vehicle)) Seen.Note("farming.harvest.second-product", Vehicle, second.Key.label,
                    values: new List<KeyValuePair<string, double>> { new("count", second.Value) });
                Drop(second.Key, second.Value);
            }
        }
        secondTally.Clear();
    }

    void Drop(ThingDef def, int count)
    {
        while (count > 0)
        {
            Thing thing = ThingMaker.MakeThing(def);
            thing.stackCount = System.Math.Min(count, def.stackLimit);
            count -= thing.stackCount;
            if (!GenPlace.TryPlaceThing(thing, Vehicle.Position, Vehicle.Map, ThingPlaceMode.Near, null, c => !Vehicle.OccupiedRect().Contains(c))) break;
        }
    }

    public string EndRun(bool finished)
    {
        // What was cut but did not fit is never lost: it is left beside the combine.
        if (Vehicle.Spawned)
        {
            foreach (KeyValuePair<ThingDef, int> left in rowTally) Drop(left.Key, left.Value);
            LeaveSecondProducts();
        }
        rowTally.Clear();
        string words = runTotals.Count == 0
            ? "PhobosFarming_HarvestedNothing".Translate().Resolve()
            : "PhobosFarming_Harvested".Translate(plants.Named("PLANTS"), runTotals.Select(t => t.Value.ToString("N0") + " " + t.Key.label).ToCommaList().Named("WHAT")).Resolve();
        if (failed > 0) words += " " + "PhobosFarming_HarvestFailedSome".Translate(failed.Named("COUNT")).Resolve();
        if (Seen.Wants("farming.harvest-run.end", Vehicle, finished ? "finished" : "stopped"))
        {
            Seen.Note("farming.harvest-run.end", Vehicle, area?.label, variant: finished ? "finished" : "stopped",
                values: new List<KeyValuePair<string, double>> { new("plants", plants), new("failed", failed), new("kg", runTotals.Sum(t => t.Value * t.Key.BaseMass)) });
        }
        area = null;
        return words;
    }

    public override string? CompInspectStringExtra() => Front is { } front
        ? "PhobosFarming_LineFront".Translate(front.label.Named("FRONT"), front.width.Named("CELLS")).Resolve()
        : "PhobosFarming_LineNoFront".Translate().Resolve();
}
