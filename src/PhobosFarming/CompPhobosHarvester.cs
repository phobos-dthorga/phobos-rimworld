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

    // Saved since the fault-read of 20 Nov 2026: plants are destroyed as they are cut, so what a row has cut and not yet poured into the
    // tank must survive a save, a load and the combine leaving the map, or it is lost.
    Dictionary<ThingDef, int> rowTally = new();
    Dictionary<ThingDef, int> secondTally = new();
    Dictionary<ThingDef, int> runTotals = new();
    Zone_Growing? area;
    int plants;
    int failed;

    public CompProperties_PhobosHarvester Props => (CompProperties_PhobosHarvester)props;

    CompPhobosBulkHold? Hold => Vehicle.GetComp<CompPhobosBulkHold>();

    PhobosHarvestFront? frontNow;
    int frontTick = -1;

    /// <summary>The front fitted now, or null. Asked of the upgrades once a tick: a row asks it for every plant.</summary>
    public PhobosHarvestFront? Front
    {
        get
        {
            int now = Find.TickManager.TicksGame;
            if (now != frontTick)
            {
                frontTick = now;
                frontNow = Props.fronts.FirstOrDefault(f => VehicleUpgrades.Unlocked(Vehicle, f.node));
            }
            return frontNow;
        }
    }

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
        if (Hold is not { } hold) return "PhobosFarming_NoTank".Translate(name).Resolve();
        if (zone is not Zone_Growing growing) return null;
        ThingDef? crop = growing.GetPlantDefToGrow();
        if (crop != null && !Takes(front, crop))
        {
            return "PhobosFarming_FrontDoesNotSuit".Translate(front.label.Named("FRONT"), crop.label.Named("CROP")).Resolve();
        }
        // A tank that keeps one product and still holds another takes none of this crop (the fault-read of 21 Nov 2026, F4: it drove the
        // whole paddock cutting nothing, and said nothing).
        if (crop?.plant.harvestedThingDef is { } product && !hold.Contents.Takes(product, hold.MixesProducts))
        {
            return "PhobosFarming_TankHoldsOther".Translate(name, hold.Contents.Summary.Named("WHAT"), crop.label.Named("CROP")).Resolve();
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
        // What an earlier run cut and could not pour is kept: it goes into the tank at this run's first row end.
        runTotals.Clear();
        plants = 0;
        failed = 0;
    }

    /// <summary>
    /// Pours what the row has cut into the tank, as far as it fits: at a row's end, and when a run ends or leaves the map partway along a
    /// row, so nothing cut is left behind while the tank has room. What does not fit stays in the tally.
    /// </summary>
    public void Settle()
    {
        CompPhobosBulkHold? hold = Hold;
        if (hold == null) return;
        foreach (ThingDef product in rowTally.Keys.ToList())
        {
            int count = rowTally[product];
            int added = hold.Receive(product, count);
            if (added > 0) Add(runTotals, product, added);
            if (added >= count) rowTally.Remove(product);
            else rowTally[product] = count - added;
        }
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
                        harvested.Destroy();
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
        Settle();
        LeaveSecondProducts();
        if (rowTally.Count > 0 || !hold.HasRoom) return FieldWorkStop.Full;
        return FieldWorkStop.None;
    }

    /// <summary>
    /// Straw and other second products are left lying in the field where the row ended, or chopped into it with a straw chopper. What finds
    /// no room on the ground stays in the tally for the next row's end, never lost.
    /// </summary>
    void LeaveSecondProducts()
    {
        if (secondTally.Count == 0) return;
        bool chops = ChopsStraw;
        if (chops)
        {
            // Only straw goes through the chopper (the fault-read of 21 Nov 2026, F6): a second product a person can eat, such as oats'
            // grain, is never destroyed.
            foreach (ThingDef straw in secondTally.Keys.Where(IsStraw).ToList()) secondTally.Remove(straw);
            if (secondTally.Count == 0) return;
        }
        if (!Vehicle.Spawned) return;
        foreach (ThingDef def in secondTally.Keys.ToList())
        {
            int count = secondTally[def];
            if (Seen.Wants("farming.harvest.second-product", Vehicle)) Seen.Note("farming.harvest.second-product", Vehicle, def.label,
                values: new List<KeyValuePair<string, double>> { new("count", count) });
            int left = Drop(def, count);
            if (left > 0) secondTally[def] = left;
            else secondTally.Remove(def);
        }
    }

    /// <summary>Whether a second product is straw, to a combine: what a person cannot eat (hay, stalks, fibre). The game's hay stands for
    /// baled straw. Read from what the product is, never its name.</summary>
    static bool IsStraw(ThingDef def) => def.ingestible == null || !def.ingestible.HumanEdible;

    /// <summary>Lays stacks on the ground beside the combine. Returns how many units found no room (the fault-read: they had been lost).</summary>
    int Drop(ThingDef def, int count)
    {
        while (count > 0)
        {
            Thing thing = ThingMaker.MakeThing(def);
            int made = System.Math.Min(count, def.stackLimit);
            thing.stackCount = made;
            if (!GenPlace.TryPlaceThing(thing, Vehicle.Position, Vehicle.Map, ThingPlaceMode.Near, null, c => !Vehicle.OccupiedRect().Contains(c)))
            {
                // The game may have merged part of the stack into a heap before giving up (the fault-read of 21 Nov 2026, B8): only what is
                // still in hand found no room.
                int inHand = thing.Destroyed ? 0 : thing.stackCount;
                if (!thing.Destroyed) thing.Destroy();
                return count - (made - inHand);
            }
            count -= made;
        }
        return 0;
    }

    public string EndRun(bool finished)
    {
        // What was cut goes into the tank first; only what does not fit is left beside the combine, and what finds no room even there stays
        // in the tally for the next run (the fault-read of 20 Nov 2026: it had all been dropped, even with room in the tank).
        Settle();
        if (Vehicle.Spawned)
        {
            foreach (ThingDef left in rowTally.Keys.ToList())
            {
                int lost = Drop(left, rowTally[left]);
                if (lost > 0) rowTally[left] = lost;
                else rowTally.Remove(left);
            }
            LeaveSecondProducts();
        }
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

    /// <summary>
    /// Leaving the map partway along a row: what the row has cut goes into the tank, as far as it fits, and the paddock is let go (the
    /// fault-read of 21 Nov 2026, F14: it had stayed in the tally, unseen and weightless, until the next run).
    /// </summary>
    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        Settle();
        area = null;
        base.PostDeSpawn(map, mode);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        bool saving = Scribe.mode == LoadSaveMode.Saving;
        if (saving && area != null && (area.Map == null || !area.Map.zoneManager.AllZones.Contains(area))) area = null;
        if (!saving || rowTally.Count > 0) Scribe_Collections.Look(ref rowTally, "PhobosHarvestRow", LookMode.Def, LookMode.Value);
        if (!saving || secondTally.Count > 0) Scribe_Collections.Look(ref secondTally, "PhobosHarvestSecond", LookMode.Def, LookMode.Value);
        if (!saving || runTotals.Count > 0) Scribe_Collections.Look(ref runTotals, "PhobosHarvestTotals", LookMode.Def, LookMode.Value);
        if (!saving || area != null) Scribe_References.Look(ref area, "PhobosHarvestArea");
        Scribe_Values.Look(ref plants, "PhobosHarvestPlants");
        Scribe_Values.Look(ref failed, "PhobosHarvestFailed");
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            rowTally = Clean(rowTally);
            secondTally = Clean(secondTally);
            runTotals = Clean(runTotals);
        }
    }

    /// <summary>A loaded tally without the products whose Def is gone (a mod removed) or with nothing counted.</summary>
    static Dictionary<ThingDef, int> Clean(Dictionary<ThingDef, int>? tally)
    {
        var clean = new Dictionary<ThingDef, int>();
        if (tally == null) return clean;
        foreach (KeyValuePair<ThingDef, int> pair in tally)
        {
            if (pair.Key != null && pair.Value > 0) clean[pair.Key] = pair.Value;
        }
        return clean;
    }

    public override string? CompInspectStringExtra() => Front is { } front
        ? "PhobosFarming_LineFront".Translate(front.label.Named("FRONT"), front.width.Named("CELLS")).Resolve()
        : "PhobosFarming_LineNoFront".Translate().Resolve();
}
