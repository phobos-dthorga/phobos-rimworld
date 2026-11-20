using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Bulk;
using Phobos.Framework.Rules;
using Phobos.VehicleSystems;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.Farming;

/// <summary>What an implement's draught and working width are, from its Def (docs/development/farming-batch-two.md, "The implements").</summary>
public abstract class CompProperties_PhobosImplement : VehicleCompProperties
{
    /// <summary>How many cells wide it works. Authored [A].</summary>
    public int widthCells = 3;
    /// <summary>Its draught per metre of working width, kN (Iowa State University's tables, medium soil). Authored from the source.</summary>
    public float kilonewtonsPerMetre = 4.4f;
    /// <summary>The metres a cell stands for, for draught (the agent's scale).</summary>
    public float metresPerCell = 1.5f;
    /// <summary>The order button's picture.</summary>
    public string orderIconPath = "Phobos/Farming/UI/Harvest";
    /// <summary>The upgrade node that unfolds its wings (the owner's width by upgrade, 20 Nov 2026), and its width with them.</summary>
    public string? wingNode;
    public int wingWidthCells;
}

/// <summary>
/// What every implement behind a tractor shares (docs/development/farming-batch-two.md): its strip is the line under its rear edge, it
/// drags its draught only while lowered (Implements.Lowered), and it works only paddocks. The tractor's field run drives; this works.
/// </summary>
public abstract class CompPhobosImplement : VehicleComp, IVehicleFieldWorker, IVehicleDraught, IVehicleHandLowered, IVehicleTowedMove
{
    protected Zone_Growing? area;
    protected int cellsWorked;
    bool handLowered;

    // ---- Lowered by hand (the owner's button, 20 Nov 2026) ----

    public bool HandLowered => handLowered;

    public void LiftByHand() => handLowered = false;

    /// <summary>Lowers or lifts it by hand, and says which. Refused, with the reason, while nothing holds it.</summary>
    public void ToggleByHand()
    {
        if (!handLowered && Implements.HeldBy(Vehicle) == null)
        {
            Messages.Message("PhobosFarming_ImplementNotHeld".Translate(Vehicle.LabelShort.Named("IMPLEMENT")).Resolve(), Vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        handLowered = !handLowered;
        Messages.Message((handLowered ? "PhobosFarming_LoweredByHand" : "PhobosFarming_LiftedByHand").Translate(Vehicle.LabelShort.Named("IMPLEMENT")).Resolve(),
            Vehicle, MessageTypeDefOf.SilentInput, historical: false);
        CarriedLoads.Changed(Implements.DrivenBy(Vehicle) ?? Vehicle);
        if (handLowered && Seen.Wants("farming.implement.hand-lowered", Vehicle)) Seen.Note("farming.implement.hand-lowered", Vehicle, Vehicle.def.defName);
    }

    /// <summary>
    /// The hitch has moved it a cell: lowered by hand and not working a run's row, it works the line under its rear edge (docs/development/
    /// farming-batch-two.md). Cheap: one line of cells.
    /// </summary>
    public void TowedMoved()
    {
        if (!handLowered || !Vehicle.Spawned || Implements.HeldBy(Vehicle) == null || Implements.RunLowers(Vehicle)) return;
        CellRect rect = Vehicle.OccupiedRect();
        IntVec3 facing = Vehicle.Rotation.FacingCell;
        StripLine line = StripRule.Front(rect.minX, rect.minZ, rect.maxX, rect.maxZ, facing.x, facing.z, StripWidth, StripAhead);
        Map map = Vehicle.Map;
        Zone_Growing? keep = area;
        try
        {
            for (int cross = line.CrossMin; cross <= line.CrossMax; cross++)
            {
                FieldCell at = line.Cell(cross);
                var cell = new IntVec3(at.X, 0, at.Z);
                if (!cell.InBounds(map)) continue;
                area = HandArea(cell, map);
                if (WorksByHand(area)) Work(cell);
            }
        }
        finally
        {
            area = keep;
        }
    }

    /// <summary>The area a cell worked by hand counts as: its growing zone, if any. The crop it grows is kept, as in a run.</summary>
    protected virtual Zone_Growing? HandArea(IntVec3 cell, Map map) => cell.GetZone(map) as Zone_Growing;

    /// <summary>Whether it works a cell of this area by hand. Tillage works anywhere; a drill only a paddock it could sow.</summary>
    protected virtual bool WorksByHand(Zone_Growing? zone) => true;

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (Vehicle.Faction != Faction.OfPlayer || !Vehicle.Spawned) yield break;
        var toggle = new Command_Action
        {
            defaultLabel = (handLowered ? "PhobosFarming_GizmoLift" : "PhobosFarming_GizmoLower").Translate(),
            defaultDesc = (handLowered ? "PhobosFarming_GizmoLiftDesc" : "PhobosFarming_GizmoLowerDesc").Translate(Vehicle.LabelShort.Named("IMPLEMENT")),
            // The Framework's own set-up and pack-up pictures (a required mod's icons, confirmed to exist): drawn ones of our own to come.
            icon = SystemIcons.For(handLowered ? "Phobos/VehicleSystems/Tank/PackUp" : "Phobos/VehicleSystems/Tank/SetUp"),
            action = ToggleByHand,
        };
        if (!handLowered && Implements.HeldBy(Vehicle) == null) toggle.Disable("PhobosFarming_ImplementNotHeld".Translate(Vehicle.LabelShort.Named("IMPLEMENT")).Resolve());
        yield return toggle;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref handLowered, "PhobosFarming_HandLowered");
    }

    public CompProperties_PhobosImplement ImplementProps => (CompProperties_PhobosImplement)props;

    public abstract string OrderLabel { get; }
    public abstract string OrderDescription { get; }
    public string OrderIconPath => ImplementProps.orderIconPath;

    /// <summary>Its working width: wider once its wing node is fitted (draught grows with it).</summary>
    public int StripWidth => System.Math.Max(1, WingsFitted && ImplementProps.wingWidthCells > 0 ? ImplementProps.wingWidthCells : ImplementProps.widthCells);

    /// <summary>Whether its wing node is fitted.</summary>
    protected bool WingsFitted => !ImplementProps.wingNode.NullOrEmpty() && VehicleUpgrades.Unlocked(Vehicle, ImplementProps.wingNode!);

    /// <summary>The line under the implement's own rear edge: its length back from its front line.</summary>
    public int StripAhead => -(System.Math.Max(Vehicle.def.Size.z, 1) - 1);

    public double DraughtNewtons =>
        FieldWorkRule.DraughtNewtons(Implements.Lowered(Vehicle), ImplementProps.kilonewtonsPerMetre, StripWidth, ImplementProps.metresPerCell);

    public bool Accepts(Zone zone) => zone is Zone_Growing && Paddocks.IsPaddock(zone);

    public virtual string? CannotWork(Zone zone)
    {
        if (Implements.HeldBy(Vehicle) == null) return "PhobosFarming_ImplementNotHeld".Translate(Vehicle.LabelShort.Named("IMPLEMENT")).Resolve();
        return null;
    }

    public abstract bool HasWork(IReadOnlyList<IntVec3> rowCells);

    public virtual void BeginRun(Zone zone)
    {
        area = zone as Zone_Growing;
        cellsWorked = 0;
    }

    public void WorkCells(List<IntVec3> cells)
    {
        if (area == null || !Vehicle.Spawned) return;
        for (int i = 0; i < cells.Count; i++)
        {
            if (Work(cells[i])) cellsWorked++;
        }
    }

    /// <summary>Works one cell of the area. Returns whether it did anything.</summary>
    protected abstract bool Work(IntVec3 cell);

    public virtual FieldWorkStop RowDone(out string? reason)
    {
        reason = null;
        return FieldWorkStop.None;
    }

    public abstract string EndRun(bool finished);

    /// <summary>Whoever drives the vehicle holding this implement, or null.</summary>
    protected Pawn? Driver()
    {
        // The vehicle that drives the chain (a drill behind a plough is driven by the tractor in front of both).
        VehiclePawn? tractor = Implements.DrivenBy(Vehicle);
        if (tractor == null || !tractor.PawnsByHandlingType.TryGetValue(HandlingType.Movement, out List<Pawn>? drivers) || drivers == null) return null;
        return drivers.FirstOrDefault(p => p != null && !p.Dead);
    }

    /// <summary>Whether this plant is a sown crop, which no implement ever destroys: any sown plant, not only the paddock's chosen crop,
    /// so a crop left growing after the paddock's crop is changed is spared too (owner, 20 Nov 2026; docs/development/farming-batch-two.md).</summary>
    protected static bool IsSownCrop(Plant plant) => plant.sown;
}

/// <summary>A plough or a cultivator, configured in its Def.</summary>
public class CompProperties_PhobosTillage : CompProperties_PhobosImplement
{
    /// <summary>"ploughed" or "cultivated": what the paddock is marked as once a run has worked most of it.</summary>
    public string mark = "ploughed";
    /// <summary>The share of a paddock's cells a run must work for the paddock to be marked. Authored [A].</summary>
    public float markShare = 0.9f;
    /// <summary>A reversible (turnover) plough (owner, 20 Nov 2026): it rolls over at each row's end, so every furrow is thrown the same way,
    /// and its picture shows the side that is down (the node look "turned").</summary>
    public bool turns;

    public CompProperties_PhobosTillage() => compClass = typeof(CompPhobosTillage);
}

/// <summary>
/// A plough or a cultivator on the linkage (docs/development/farming-batch-two.md): it clears weeds, stubble and wild plants from the cells
/// it passes over, never a sown crop and never a tree, and marks the paddock ploughed or cultivated once a run has worked most of it. The
/// mark is for the soil rules to come; until then it shows in the end message and does nothing else.
/// </summary>
public class CompPhobosTillage : CompPhobosImplement, IVehicleLookSwitch
{
    /// <summary>The node look a turnover plough shows while its other side is down.</summary>
    public const string TurnedLook = "turned";

    readonly List<Thing> cleared = new();
    bool turned;

    public CompProperties_PhobosTillage Props => (CompProperties_PhobosTillage)props;

    bool Ploughs => Props.mark != "cultivated";

    public override string OrderLabel => (Ploughs ? "PhobosFarming_GizmoPlough" : "PhobosFarming_GizmoCultivate").Translate().Resolve();

    public override string OrderDescription => (Ploughs ? "PhobosFarming_GizmoPloughDesc" : "PhobosFarming_GizmoCultivateDesc")
        .Translate(Vehicle.LabelShort.Named("IMPLEMENT")).Resolve();

    string MarkWords => (Ploughs ? "PhobosFarming_MarkPloughed" : "PhobosFarming_MarkCultivated").Translate().Resolve();

    /// <summary>Whether a plant here would be cleared: anything but a sown crop (of any kind) and trees.</summary>
    static bool Clears(Plant plant) => !IsSownCrop(plant) && !plant.def.plant.IsTree;

    public override bool HasWork(IReadOnlyList<IntVec3> rowCells)
    {
        Map map = Vehicle.Map;
        // Tillage works every cell but those holding the sown crop, cleared or not.
        for (int i = 0; i < rowCells.Count; i++)
        {
            if (!rowCells[i].InBounds(map)) continue;
            Plant? plant = rowCells[i].GetPlant(map);
            if (plant == null || Clears(plant)) return true;
        }
        return false;
    }

    protected override bool Work(IntVec3 cell)
    {
        Map map = Vehicle.Map;
        if (!cell.InBounds(map)) return false;
        cleared.Clear();
        List<Thing> things = cell.GetThingList(map);
        bool keptCrop = false;
        for (int i = 0; i < things.Count; i++)
        {
            if (things[i] is not Plant plant) continue;
            if (Clears(plant)) cleared.Add(plant);
            else keptCrop = true;
        }
        // A cell holding the sown crop is passed over whole: tillage there would tear the crop up.
        if (keptCrop) return false;
        foreach (Thing thing in cleared) thing.Destroy(DestroyMode.KillFinalize);
        return true;
    }

    /// <summary>A turnover plough rolls over at each row's end, as a real one does at the headland, and its picture follows.</summary>
    public override FieldWorkStop RowDone(out string? reason)
    {
        if (Props.turns)
        {
            turned = !turned;
            Vehicle.GetComp<CompPhobosNodeLooks>()?.Refresh();
            if (Seen.Wants("farming.plough.turned", Vehicle)) Seen.Note("farming.plough.turned", Vehicle, Vehicle.def.defName);
        }
        return base.RowDone(out reason);
    }

    /// <summary>The plough's looks: its turned side, and its wings on whichever side is down.</summary>
    public bool ShowsLook(string when) => when switch
    {
        TurnedLook => turned,
        "wings" => WingsFitted && !turned,
        "wings-turned" => WingsFitted && turned,
        _ => false,
    };

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref turned, "PhobosFarming_PloughTurned");
    }

    public override string EndRun(bool finished)
    {
        string words = "PhobosFarming_Tilled".Translate(cellsWorked.Named("CELLS"), MarkWords.Named("MARK")).Resolve();
        if (area != null && area.Cells.Count > 0 && cellsWorked >= Props.markShare * area.Cells.Count && Paddocks.Get(area) is { } state)
        {
            state.lastTilled = Props.mark;
            state.lastTilledTick = Find.TickManager.TicksGame;
            words += " " + "PhobosFarming_PaddockMarkedTilled".Translate(area.label.Named("ZONE"), MarkWords.Named("MARK")).Resolve();
        }
        if (Seen.Wants("farming.tillage.end", Vehicle, Props.mark))
        {
            Seen.Note("farming.tillage.end", Vehicle, area?.label, variant: Props.mark, values: new List<KeyValuePair<string, double>> { new("cells", cellsWorked) });
        }
        area = null;
        return words;
    }
}

/// <summary>A seed drill, configured in its Def.</summary>
public class CompProperties_PhobosSeedDrill : CompProperties_PhobosImplement
{
    /// <summary>The share of a cell's harvest, by weight, a cell takes as seed (about 3 %, from the record's sources). Authored [A].</summary>
    public float seedShare = SeedRates.SeedShare;
    /// <summary>What cleaning takes off a crop's own grain used as seed: it sows at its weight divided by what is left. Authored [A].</summary>
    public float cleaningLoss = SeedRates.CleaningLoss;
    /// <summary>The generic seed traders sell, which sows any crop.</summary>
    public ThingDef? genericSeed;

    public CompProperties_PhobosSeedDrill() => compClass = typeof(CompPhobosSeedDrill);
}

/// <summary>
/// A seed drill (docs/development/farming-batch-two.md): it sows the paddock's crop in the empty cells it passes over, by the game's own
/// checks (JobDriver_PlantSow and WorkGiver_GrowerSow.JobOnCell, read 20 Nov 2026), drawing seed from its own bulk hold: the crop's own
/// grain, cleaned as it goes, or the generic seed traders sell. It never cuts anything: a cell holding another plant is passed over, as a
/// hand would cut it first. Out of seed, the run ends and says so.
/// </summary>
public class CompPhobosSeedDrill : CompPhobosImplement
{
    double owedKilograms;
    int sown;
    int blocked;

    public CompProperties_PhobosSeedDrill Props => (CompProperties_PhobosSeedDrill)props;

    CompPhobosBulkHold? Hold => Vehicle.GetComp<CompPhobosBulkHold>();

    public override string OrderLabel => "PhobosFarming_GizmoSow".Translate().Resolve();

    public override string OrderDescription => "PhobosFarming_GizmoSowDesc".Translate(Vehicle.LabelShort.Named("IMPLEMENT")).Resolve();

    ThingDef? Crop => area?.GetPlantDefToGrow();

    Zone? handZone;
    int handZoneTick = -1;
    bool handZoneSows;

    /// <summary>Lowered by hand, a drill sows only a paddock, whose crop it knows.</summary>
    protected override Zone_Growing? HandArea(IntVec3 cell, Map map) => cell.GetZone(map) is Zone_Growing zone && Paddocks.IsPaddock(zone) ? zone : null;

    /// <summary>Lowered by hand, it sows a paddock only where a run could (season, research, the driver's skill, seed), asked once a tick per paddock.</summary>
    protected override bool WorksByHand(Zone_Growing? zone)
    {
        if (zone == null) return false;
        int now = Find.TickManager.TicksGame;
        if (zone != handZone || now != handZoneTick)
        {
            handZone = zone;
            handZoneTick = now;
            handZoneSows = CannotWork(zone) == null;
        }
        return handZoneSows;
    }

    /// <summary>The seed it sows this crop with: the crop's own grain if it holds some, else the generic seed. Null for none.</summary>
    ThingDef? SeedFor(ThingDef crop)
    {
        BulkContents? contents = Hold?.Contents;
        if (contents == null) return null;
        // Farming 0.6.0: only a crop whose harvest is its own seed sows from it (cotton is not sown with cloth).
        ThingDef? grain = Crops.OwnSeed(crop);
        if (grain != null && contents.CountOf(grain) > 0) return grain;
        if (Props.genericSeed != null && contents.CountOf(Props.genericSeed) > 0) return Props.genericSeed;
        return null;
    }

    public override string? CannotWork(Zone zone)
    {
        if (base.CannotWork(zone) is { } cannot) return cannot;
        NamedArgument name = Vehicle.LabelShort.Named("IMPLEMENT");
        if (zone is not Zone_Growing growing || growing.GetPlantDefToGrow() is not { } crop) return null;
        if (crop.plant.sowResearchPrerequisites is { } research && research.Any(r => !r.IsFinished))
        {
            return "PhobosFarming_SowNeedsResearch".Translate(crop.label.Named("CROP")).Resolve();
        }
        if (!PlantUtility.GrowthSeasonNow(growing.Map, crop)) return "PhobosFarming_SowOutOfSeason".Translate(crop.label.Named("CROP")).Resolve();
        if (crop.plant.sowMinSkill > 0 && Driver() is { skills: { } skills } && skills.GetSkill(SkillDefOf.Plants).Level < crop.plant.sowMinSkill)
        {
            return "PhobosFarming_SowDriverSkill".Translate(crop.label.Named("CROP"), crop.plant.sowMinSkill.Named("LEVEL")).Resolve();
        }
        if (SeedFor(crop) == null) return "PhobosFarming_NoSeed".Translate(name, crop.label.Named("CROP")).Resolve();
        return null;
    }

    public override void BeginRun(Zone zone)
    {
        base.BeginRun(zone);
        owedKilograms = 0;
        sown = 0;
        blocked = 0;
    }

    /// <summary>Whether this cell can be sown now, by the game's own checks for a hand, short of anything a hand would cut first.</summary>
    bool Sowable(IntVec3 cell, ThingDef crop, Map map)
    {
        if (!cell.InBounds(map) || !PlantUtility.GrowthSeasonNow(cell, map, crop)) return false;
        List<Thing> things = cell.GetThingList(map);
        for (int i = 0; i < things.Count; i++)
        {
            Thing thing = things[i];
            if (thing.def == crop || thing is Blueprint || thing is Frame || thing.def.BlocksPlanting()) return false;
        }
        if (crop.plant.diesToLight && (!cell.Roofed(map) || map.glowGrid.GroundGlowAt(cell, ignoreCavePlants: true) > 0f)) return false;
        if (crop.plant.interferesWithRoof && cell.Roofed(map)) return false;
        if (PlantUtility.AdjacentSowBlocker(crop, cell, map) != null) return false;
        return crop.CanEverPlantAt(cell, map);
    }

    public override bool HasWork(IReadOnlyList<IntVec3> rowCells)
    {
        if (Crop is not { } crop) return false;
        Map map = Vehicle.Map;
        for (int i = 0; i < rowCells.Count; i++)
        {
            if (Sowable(rowCells[i], crop, map)) return true;
        }
        return false;
    }

    protected override bool Work(IntVec3 cell)
    {
        if (Crop is not { } crop || Hold is not { } hold) return false;
        Map map = Vehicle.Map;
        if (!Sowable(cell, crop, map))
        {
            blocked++;
            return false;
        }
        if (SeedFor(crop) is not { } seed) return false;
        // The crop's own grain loses its cleaning share, so a cell takes more of it than of bought seed.
        // The crop's own grain loses its cleaning share (SeedRates, shared with hands); bought seed sows at the plain rate.
        double perCell = seed != Props.genericSeed
            ? SeedRates.OwnGrainKilogramsPerCell(crop, Props.seedShare, Props.cleaningLoss)
            : SeedRates.OwnGrainKilogramsPerCell(crop, Props.seedShare, 0f);
        (int units, double owed) = FieldWorkRule.DrawSeed(owedKilograms, perCell, seed.BaseMass);
        if (units > 0 && hold.Contents.Take(seed, units, out _) < units) return false;
        owedKilograms = owed;
        if (units > 0) hold.ContentsChanged();
        if (GenSpawn.Spawn(crop, cell, map) is not Plant plant) return false;
        plant.Growth = 0.0001f;
        plant.sown = true;
        map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.Things);
        if (Driver() is { } driver)
        {
            Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.SowedPlant, driver.Named(HistoryEventArgsNames.Doer)));
            if (crop.plant.humanFoodPlant) Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.SowedHumanFoodPlant, driver.Named(HistoryEventArgsNames.Doer)));
        }
        sown++;
        if (Seen.Wants("farming.sow.cell", Vehicle)) Seen.Note("farming.sow.cell", Vehicle, crop.label, variant: seed == Props.genericSeed ? "bought seed" : "own grain");
        return true;
    }

    public override FieldWorkStop RowDone(out string? reason)
    {
        reason = null;
        if (Crop is { } crop && SeedFor(crop) == null)
        {
            reason = "PhobosFarming_NoSeed".Translate(Vehicle.LabelShort.Named("IMPLEMENT"), crop.label.Named("CROP")).Resolve();
            return FieldWorkStop.OutOfSupply;
        }
        return FieldWorkStop.None;
    }

    public override string EndRun(bool finished)
    {
        string words = "PhobosFarming_Sowed".Translate(sown.Named("CELLS"), (Crop?.label ?? "").Named("CROP")).Resolve();
        if (Seen.Wants("farming.sow.end", Vehicle, finished ? "finished" : "stopped"))
        {
            Seen.Note("farming.sow.end", Vehicle, area?.label, variant: finished ? "finished" : "stopped",
                values: new List<KeyValuePair<string, double>> { new("sown", sown), new("passed over", blocked) });
        }
        area = null;
        return words;
    }
}
