using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Debug;
using Phobos.Framework.Rules;
using Phobos.Framework.Sounds;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using SmashTools;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

[DefOf]
public static class GeneratorDefOf
{
    public static VehicleStatDef Phobos_GeneratorOutput = null!;
    public static VehicleStatDef Phobos_GeneratorBuffer = null!;
    public static VehicleStatDef Phobos_CableReach = null!;
    public static VehicleStatDef Phobos_RigStability = null!;
    public static VehicleStatDef Phobos_RigControl = null!;
    public static VehicleStatDef Phobos_HeatRecovery = null!;
    public static VehicleStatDef Phobos_ServiceLeadReach = null!;

    static GeneratorDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(GeneratorDefOf));
}

public class CompProperties_PhobosGeneratorSet : VehicleCompProperties
{
    public ThingDef? cableEndDef;
    /// <summary>Authored; see docs/development/vehicle-generator-set.md for how these sit against the game's own generator.</summary>
    public float baseWatts = 1200f;
    public float outputFloorWatts = 240f;
    public float idleFuelPerDay = 1.5f;
    public float fullFuelPerDay = 9f;
    /// <summary>Heat per second for each watt recovered: the game's own heater gives 21 a second for 175 W.</summary>
    public float heatPerSecondPerWatt = 21f / 175f;
    /// <summary>Recovered heat stops when the room reaches this, the game's heater's own default target.</summary>
    public float heatTargetCelsius = 21f;
    public float stabilityFloor = 0.5f;
    public float controlFloor = 0.5f;
    /// <summary>Parts without which no power is made, whatever the average says.</summary>
    public List<string> vitalParts = new();
    /// <summary>Part without which the cable cannot be run out.</summary>
    public string reelPart = "CableReel";
    public GraphicDataOverlay? setUpOverlay;
    /// <summary>Loops at the truck while it is set up and its engine runs; null for silence.</summary>
    public SoundDef? runningSound;

    public CompProperties_PhobosGeneratorSet() => compClass = typeof(CompPhobosGeneratorSet);
}

/// <summary>The power comp of the cable end. Its output is set by the truck on the rare tick, so the building itself never ticks.</summary>
public sealed class CompPhobosGeneratorStub : CompPowerPlant
{
    public void SetOutput(float watts) => PowerOutput = watts;
}

/// <summary>
/// A generator truck (docs/development/vehicle-generator-set.md). The game's power code takes only buildings, so setting up spawns a cable end, a small building of ours on the
/// net, and the truck sets its output each rare tick from its fuel, its health and what the net draws. Gizmos read this and call its service methods.
/// </summary>
public class CompPhobosGeneratorSet : CompPhobosSetUp, IVehicleEngineDemand
{
    const float DaysPerRareTick = GenTicks.TickRareInterval / (float)GenDate.TicksPerDay;

    Building? cableEnd;
    float buffer;
    /// <summary>The vehicle the service lead runs to (Framework 0.101.0), or null.</summary>
    VehiclePawn? leadTo;

    // Derived on the rare tick, never saved.
    bool running;
    float load;
    float outputWatts;
    string? fault;
    HeatState heat;
    float leadWatts;

    enum HeatState { None, Warming, WarmEnough, Outdoors }
    readonly SustainedSound runningLoop = new();

    public CompProperties_PhobosGeneratorSet Props => (CompProperties_PhobosGeneratorSet)props;
    protected override string DeployedSaveKey => "PhobosGeneratorDeployed";
    protected override GraphicDataOverlay? SetUpLook => Props.setUpOverlay;
    protected override string SetUpRefusal => "PhobosVehicleSystems_GenSetUpRefusal".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();

    public bool WantsEngine => deployed && running;
    public float EngineDemand => load;
    public int CableReach => (int)Vehicle.GetStatValue(GeneratorDefOf.Phobos_CableReach);
    float BufferCapacity => Vehicle.GetStatValue(GeneratorDefOf.Phobos_GeneratorBuffer);

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_References.Look(ref cableEnd, "PhobosGeneratorCableEnd");
        Scribe_Values.Look(ref buffer, "PhobosGeneratorBuffer");
        Scribe_References.Look(ref leadTo, "PhobosServiceLeadTo");
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            if (deployed) ShowSetUp(true);
        });
        if (leadTo != null) ServiceLeads.Note(leadTo, this);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        // Leaving the map, for a caravan or a launch, or being destroyed, takes the cable with it.
        ClearSetUp();
        base.PostDeSpawn(map, mode);
    }

    public override void CompTickRare()
    {
        base.CompTickRare();
        if (deployed) UpdatePower();
        if (leadTo != null) KeepLead();
    }

    // ---- Services the gizmos call ----

    /// <summary>Why it cannot set up now, or null.</summary>
    public string? CannotDeploy()
    {
        if (!Vehicle.Spawned) return "PhobosVehicleSystems_GenNotOnMap".Translate().Resolve();
        if (StillMoving) return "PhobosVehicleSystems_GenStopFirst".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        if (Vehicle.GetStatValue(GeneratorDefOf.Phobos_RigStability) < Props.stabilityFloor) return "PhobosVehicleSystems_GenOutriggers".Translate().Resolve();
        if (Vehicle.GetStatValue(GeneratorDefOf.Phobos_RigControl) < Props.controlFloor) return "PhobosVehicleSystems_GenControls".Translate().Resolve();
        VehicleComponent? reel = Vehicle.statHandler.GetComponent(Props.reelPart);
        if (reel != null && reel.Health <= 0f) return "PhobosVehicleSystems_GenReelGone".Translate().Resolve();
        return null;
    }

    /// <summary>Whether the cable end can be put down on this cell.</summary>
    public bool CanPlaceCableAt(IntVec3 cell) => CellClearFor(cell, CableReach);

    /// <summary>Every cell the cable can reach, for the outline while choosing.</summary>
    public List<IntVec3> CableCells()
    {
        CellRect around = Vehicle.OccupiedRect().ExpandedBy(System.Math.Max(1, CableReach)).ClipInsideMap(Vehicle.Map);
        return around.Cells.Where(CanPlaceCableAt).ToList();
    }

    /// <summary>The best adjacent cell for a short cable: beside a conduit if one is there, or else invalid if none is clear.</summary>
    public IntVec3 BestCableCell() => BestAdjacentCell();

    public void SetUp(IntVec3 cell)
    {
        string? refused = CannotDeploy();
        if (refused == null && !CanPlaceCableAt(cell)) refused = "PhobosVehicleSystems_GenNoCell".Translate().Resolve();
        if (refused != null)
        {
            Tell(refused, MessageTypeDefOf.RejectInput);
            return;
        }
        Thing placed = ThingMaker.MakeThing(Props.cableEndDef);
        placed.SetFaction(Vehicle.Faction);
        cableEnd = (Building)GenSpawn.Spawn(placed, cell, Vehicle.Map);
        deployed = true;
        ShowSetUp(true);
        VehicleWorkSounds.Play(Vehicle, "SetUp");
        UpdatePower();
        Tell("PhobosVehicleSystems_GenSetUp".Translate(Vehicle.LabelShort.Named("VEHICLE")), MessageTypeDefOf.SilentInput);
    }

    public void PackUp()
    {
        ClearSetUp();
        VehicleWorkSounds.Play(Vehicle, "PackUp", "SetUp");
        Tell("PhobosVehicleSystems_GenPackedUp".Translate(Vehicle.LabelShort.Named("VEHICLE")), MessageTypeDefOf.SilentInput);
    }

    // ---- The work ----

    void ClearSetUp()
    {
        if (leadTo != null) TakeInLead("PhobosVehicleSystems_LeadPackedUp".Translate(Vehicle.LabelShort.Named("SOURCE")).Resolve());
        deployed = false;
        running = false;
        load = 0f;
        outputWatts = 0f;
        fault = null;
        runningLoop.End();
        ShowSetUp(false);
        if (cableEnd is { Destroyed: false }) cableEnd.Destroy(DestroyMode.Vanish);
        cableEnd = null;
    }

    /// <summary>What it can offer now, and if nothing, why.</summary>
    float Offered(out string? why)
    {
        why = null;
        foreach (string key in Props.vitalParts)
        {
            VehicleComponent? part = Vehicle.statHandler.GetComponent(key);
            if (part == null || part.Health > 0f) continue;
            why = "PhobosVehicleSystems_GenPartDead".Translate(part.props.label.Named("PART")).Resolve();
            return 0f;
        }
        if (Vehicle.CompFueledTravel is { EmptyTank: true })
        {
            why = "PhobosVehicleSystems_GenNoFuel".Translate().Resolve();
            return 0f;
        }
        float watts = Vehicle.GetStatValue(GeneratorDefOf.Phobos_GeneratorOutput);
        if (watts >= Props.outputFloorWatts) return watts;
        why = "PhobosVehicleSystems_GenTooDamaged".Translate().Resolve();
        return 0f;
    }

    void UpdatePower()
    {
        if (cableEnd is not { Destroyed: false, Spawned: true })
        {
            ClearSetUp();
            Tell("PhobosVehicleSystems_GenCableLost".Translate(Vehicle.LabelShort.Named("VEHICLE")), MessageTypeDefOf.NegativeEvent);
            return;
        }
        using (PhobosTiming.Measure("vehicle generator set"))
        {
            CompPhobosGeneratorStub? stub = cableEnd.GetComp<CompPhobosGeneratorStub>();
            if (stub == null) return;
            float offered = Offered(out fault);
            ReadNet(stub.PowerNet, stub, out float need, out bool batteriesHaveRoom);
            // Full-output fuel follows the watts on offer, so a bigger or a damaged generator keeps the same watts per chemfuel.
            float full = Props.fullFuelPerDay * offered / Props.baseWatts;
            // A service lead takes its watts off the top; the camp's grid gets the rest (Framework 0.101.0).
            LeadStep withLead = GeneratorRule.StepWithLead(offered, leadTo != null ? Rules.serviceLeadWatts : 0f, need, batteriesHaveRoom, buffer, BufferCapacity,
                DaysPerRareTick, Props.idleFuelPerDay, full);
            GeneratorStep step = withLead.Camp;
            leadWatts = withLead.LeadWatts;
            stub.SetOutput(step.OutputWatts);
            buffer = step.Buffer;
            load = step.Load;
            outputWatts = step.OutputWatts;
            running = offered > 0f;
            runningLoop.Set(Props.runningSound, running, Vehicle);
            float used = WorkFuel(step.FuelUsed, DaysPerRareTick);
            if (used > 0f) Vehicle.CompFueledTravel?.ConsumeFuel(used);
            RecoverHeat();
        }
    }

    /// <summary>With an exhaust heat exchanger fitted, warms the room the cable end stands in, in step with the watts being made, until
    /// the room reaches the game's heater's target. Outdoors, the heat goes to waste.</summary>
    void RecoverHeat()
    {
        heat = HeatState.None;
        float share = Vehicle.GetStatValue(GeneratorDefOf.Phobos_HeatRecovery);
        if (share <= 0f || cableEnd == null || !running) return;
        Room? room = cableEnd.Position.GetRoom(cableEnd.Map);
        if (room == null || room.UsesOutdoorTemperature)
        {
            heat = HeatState.Outdoors;
            return;
        }
        double perSecond = HeatRecoveryRule.HeatPerSecond(outputWatts, share, Props.heatPerSecondPerWatt, room.Temperature, Props.heatTargetCelsius);
        if (perSecond <= 0)
        {
            heat = HeatState.WarmEnough;
            return;
        }
        heat = HeatState.Warming;
        GenTemperature.PushHeat(cableEnd.Position, cableEnd.Map, (float)perSecond * GenTicks.TickRareInterval / GenTicks.TicksPerRealSecond);
    }

    static void ReadNet(PowerNet? net, CompPhobosGeneratorStub own, out float need, out bool batteriesHaveRoom)
    {
        need = 0f;
        batteriesHaveRoom = false;
        if (net == null) return;
        float draw = 0f, others = 0f;
        foreach (CompPowerTrader comp in net.powerComps)
        {
            if (comp == own || !comp.PowerOn) continue;
            if (comp.PowerOutput < 0f) draw -= comp.PowerOutput;
            else others += comp.PowerOutput;
        }
        need = System.Math.Max(0f, draw - others);
        foreach (CompPowerBattery battery in net.batteryComps)
        {
            if (battery.AmountCanAccept <= 0f) continue;
            batteriesHaveRoom = true;
            break;
        }
    }

    void Tell(string text, MessageTypeDef type) => Messages.Message(text, Vehicle, type, historical: false);

    // ---- The service lead (Framework 0.101.0; docs/development/vehicle-upgrades-round-two.md) ----

    static PhobosVehicleRulesDef Rules => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();

    public int LeadReach => (int)Vehicle.GetStatValue(GeneratorDefOf.Phobos_ServiceLeadReach);
    public VehiclePawn? LeadTo => leadTo;
    /// <summary>Whether the lead is in and the set is giving it power now: what makes the vehicle at its end count as on shore power.</summary>
    public bool LeadLive => leadTo != null && deployed && running && leadWatts > 0f;

    /// <summary>Why the set cannot run its lead now, or null.</summary>
    public string? LeadRefusal()
    {
        if (LeadReach <= 0) return "PhobosVehicleSystems_LeadNone".Translate().Resolve();
        if (!deployed) return "PhobosVehicleSystems_LeadNotSetUp".Translate(Vehicle.LabelShort.Named("SOURCE")).Resolve();
        if (!running) return fault ?? "PhobosVehicleSystems_LeadNotRunning".Translate(Vehicle.LabelShort.Named("SOURCE")).Resolve();
        return null;
    }

    bool InReach(VehiclePawn target) => Vehicle.OccupiedRect().ExpandedBy(LeadReach).Overlaps(target.OccupiedRect());

    /// <summary>Why the lead cannot run to this vehicle, or null.</summary>
    public string? LeadRefusalFor(VehiclePawn target)
    {
        NamedArgument name = target.LabelShort.Named("VEHICLE");
        if (target == Vehicle) return "PhobosVehicleSystems_LeadNotItself".Translate().Resolve();
        if (target.Faction != Faction.OfPlayer) return "PhobosVehicleSystems_LeadNotOurs".Translate(name).Resolve();
        if (!target.Spawned || target.Map != Vehicle.Map) return "PhobosVehicleSystems_LeadNotHere".Translate(name).Resolve();
        if (target.GetComp<CompPhobosVehicleSystems>()?.HasLeadBattery() != true) return "PhobosVehicleSystems_LeadNoBattery".Translate(name).Resolve();
        if (ServiceLeads.SourceFor(target) is { } other && other != this) return "PhobosVehicleSystems_LeadTaken".Translate(name, other.Vehicle.LabelShort.Named("SOURCE")).Resolve();
        if (target.vehiclePather != null && target.vehiclePather.Moving) return "PhobosVehicleSystems_LeadMoving".Translate(name).Resolve();
        if (!InReach(target)) return "PhobosVehicleSystems_LeadTooFar".Translate(name, LeadReach.Named("CELLS")).Resolve();
        return null;
    }

    /// <summary>Plugs the lead into a vehicle; a start follows at once if its battery is too low to start.</summary>
    public void RunLead(VehiclePawn target)
    {
        if ((LeadRefusal() ?? LeadRefusalFor(target)) is { } refused)
        {
            Tell(refused, MessageTypeDefOf.RejectInput);
            return;
        }
        if (leadTo != null && leadTo != target) TakeInLead(null);
        leadTo = target;
        ServiceLeads.Note(target, this);
        VehicleWorkSounds.Play(Vehicle, "SetUp");
        Messages.Message("PhobosVehicleSystems_LeadRun".Translate(Vehicle.LabelShort.Named("SOURCE"), target.LabelShort.Named("VEHICLE")),
            new LookTargets(Vehicle, target), MessageTypeDefOf.TaskCompletion, historical: false);
        UpdatePower();
        KeepLead();
    }

    /// <summary>Takes the lead in, adding why when <paramref name="why"/> is given (the player's own order needs no reason).</summary>
    public void TakeInLead(string? why)
    {
        VehiclePawn? target = leadTo;
        leadTo = null;
        leadWatts = 0f;
        if (target == null) return;
        ServiceLeads.Forget(target, this);
        string line = "PhobosVehicleSystems_LeadTakenIn".Translate(Vehicle.LabelShort.Named("SOURCE"), target.LabelShort.Named("VEHICLE")).Resolve();
        Messages.Message(why == null ? line : line + " " + why, new LookTargets(Vehicle, target),
            why == null ? MessageTypeDefOf.SilentInput : MessageTypeDefOf.CautionInput, historical: false);
    }

    /// <summary>On the rare tick: the lead comes out if its vehicle has gone or moved off, and gives a start when that vehicle needs one.</summary>
    void KeepLead()
    {
        if (leadTo == null) return;
        if (leadTo.Destroyed || !leadTo.Spawned || leadTo.Map != Vehicle.Map || LeadReach <= 0 || !InReach(leadTo))
        {
            TakeInLead("PhobosVehicleSystems_LeadOutOfReach".Translate(leadTo.LabelShort.Named("VEHICLE")).Resolve());
            return;
        }
        if (!LeadLive) return;
        CompPhobosVehicleSystems? systems = leadTo.GetComp<CompPhobosVehicleSystems>();
        if (systems?.JumpStartNeed() != null) systems.ReceiveLeadStart(Vehicle);
    }

    public override void PostDrawExtraSelectionOverlays()
    {
        base.PostDrawExtraSelectionOverlays();
        if (leadTo is { Spawned: true } target && Vehicle.Spawned) GenDraw.DrawLineBetween(((Thing)Vehicle).DrawPos, ((Thing)target).DrawPos, SimpleColor.Orange);
    }

    public override string? CompInspectStringExtra()
    {
        if (!deployed) return null;
        var lines = new List<string>();
        if (fault != null) lines.Add(fault);
        else lines.Add("PhobosVehicleSystems_GenOutputLine".Translate(outputWatts.ToString("N0").Named("WATTS"), load.ToStringPercent().Named("LOAD")).Resolve());
        if (heat == HeatState.Warming) lines.Add("PhobosVehicleSystems_GenHeatWarming".Translate().Resolve());
        else if (heat == HeatState.WarmEnough) lines.Add("PhobosVehicleSystems_GenHeatWarm".Translate().Resolve());
        else if (heat == HeatState.Outdoors) lines.Add("PhobosVehicleSystems_GenHeatOutdoors".Translate().Resolve());
        if (leadTo != null)
        {
            lines.Add(LeadLive
                ? "PhobosVehicleSystems_LeadLine".Translate(leadTo.LabelShort.Named("VEHICLE"), leadWatts.ToString("N0").Named("WATTS")).Resolve()
                : "PhobosVehicleSystems_LeadLineIdle".Translate(leadTo.LabelShort.Named("VEHICLE")).Resolve());
        }
        if (BufferCapacity > 0f) lines.Add("PhobosVehicleSystems_GenBufferLine".Translate(buffer.ToString("N0").Named("CHARGE"), BufferCapacity.ToString("N0").Named("CAPACITY")).Resolve());
        return string.Join("\n", lines);
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (Vehicle.Faction != Faction.OfPlayer) yield break;
        yield return GeneratorGizmos.SetUpButton(this);
        if (LeadReach > 0 || leadTo != null) yield return GeneratorGizmos.LeadButton(this);
    }
}
