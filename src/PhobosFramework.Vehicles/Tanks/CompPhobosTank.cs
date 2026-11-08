using System.Collections.Generic;
using System.Linq;
using Phobos.Framework;
using Phobos.Framework.Rules;
using RimWorld;
using SmashTools;
using UnityEngine;
using Vehicles;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

/// <summary>What the pump is told to do while the tanker is set up.</summary>
public enum TankPumpMode
{
    Hold,
    Load,
    Unload,
}

/// <summary>
/// A vehicle's bulk tank (docs/development/utility-vehicles-design.md, Bulk tanker). Which tank is fitted is read from the upgrade tree;
/// the contents, their temperature and the set-up state are saved here. Everything runs on the rare tick. Gizmos in TankGizmos read this
/// and call its service methods; nothing else changes it.
/// </summary>
public class CompPhobosTank : VehicleComp
{
    const string HoseOverlayKey = "PhobosTankHoses";
    const float HoursPerRareTick = GenTicks.TickRareInterval / (float)GenDate.TicksPerHour;
    const float EmptyBelow = 0.5f;
    /// <summary>Degrees above its freezing point that a fresh load arrives at (authored).</summary>
    const float ArrivalMargin = 5f;
    /// <summary>How big a fire a self-igniting spill starts (authored).</summary>
    const float SpillFireSize = 0.6f;

    PhobosTankContentsDef? contents;
    /// <summary>What the empty tank last held, until it is purged.</summary>
    PhobosTankContentsDef? residue;
    float amount;
    float temperature = 21f;
    float spoilHours;
    bool deployed;
    TankPumpMode mode = TankPumpMode.Hold;
    /// <summary>A number a port may keep about the load's quality (Dubs Bad Hygiene's contamination level).</summary>
    int quality;
    bool ventWarned;
    bool registered;

    /// <summary>Why the pump last did nothing, for the inspect line. Derived, never saved.</summary>
    string? pumpStatus;

    public CompProperties_PhobosTank Props => (CompProperties_PhobosTank)props;
    public PhobosTankContentsDef? Contents => contents;
    public PhobosTankContentsDef? Residue => residue;
    public float Amount => amount;
    public float Temperature => temperature;
    public bool Deployed => deployed;
    public TankPumpMode Mode => mode;
    public float Capacity => Vehicle.GetStatValue(TankDefOf.Phobos_TankCapacity);
    public float Space => Mathf.Max(0f, Capacity - amount);
    public bool HasLoad => amount >= EmptyBelow;
    public float LoadKilograms => contents == null ? 0f : amount * contents.kilogramsPerUnit;
    public bool NeedsPurge => residue != null && !HasLoad;

    public int Quality
    {
        get => quality;
        set => quality = value;
    }

    public float WeightedLoad
    {
        get
        {
            TankFitting? fitting = Fitted;
            float capacity = fitting == null ? 0f : Capacity;
            return capacity <= 0f ? 0f : amount / capacity * fitting!.loadWeight;
        }
    }

    /// <summary>The tank the upgrade tree has fitted, or null. Read each time: a handful of keys, and the tree is the only authority.</summary>
    public TankFitting? Fitted
    {
        get
        {
            CompUpgradeTree? tree = Vehicle.CompUpgradeTree;
            if (tree == null) return null;
            foreach (TankFitting fitting in Props.fittings)
            {
                UpgradeNode? node = tree.Props.def.GetNode(fitting.node);
                if (node != null && tree.NodeUnlocked(node)) return fitting;
            }
            return null;
        }
    }

    float Outside
    {
        get
        {
            CompPhobosVehicleSystems? systems = Vehicle.GetComp<CompPhobosVehicleSystems>();
            if (systems != null) return systems.Ambient;
            return Vehicle.Spawned ? Vehicle.Position.GetTemperature(Vehicle.Map) : temperature;
        }
    }

    bool Frozen => HasLoad && contents != null && TankRule.Frozen(temperature, contents.freezesBelow);

    /// <summary>A fresh load arrives at the outside temperature, but what can freeze arrives liquid and cools in the tank.</summary>
    float ArrivalTemperature(float outside) =>
        contents == null || float.IsNaN(contents.freezesBelow) ? outside : Mathf.Max(outside, contents.freezesBelow + ArrivalMargin);

    /// <summary>The cells a hose or a coupling reaches: under the vehicle and all round it.</summary>
    public IEnumerable<IntVec3> ContactCells()
    {
        foreach (IntVec3 cell in Vehicle.OccupiedRect()) yield return cell;
        foreach (IntVec3 cell in Vehicle.SurroundingCells) yield return cell;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Defs.Look(ref contents, "PhobosTankContents");
        Scribe_Defs.Look(ref residue, "PhobosTankResidue");
        Scribe_Values.Look(ref amount, "PhobosTankAmount");
        Scribe_Values.Look(ref temperature, "PhobosTankTemperature", 21f);
        Scribe_Values.Look(ref spoilHours, "PhobosTankSpoilHours");
        Scribe_Values.Look(ref deployed, "PhobosTankDeployed");
        Scribe_Values.Look(ref mode, "PhobosTankMode", TankPumpMode.Hold);
        Scribe_Values.Look(ref quality, "PhobosTankQuality");
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (deployed) LongEventHandler.ExecuteWhenFinished(() => ShowHoses(true));
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        // Leaving the map, for a caravan or a launch, takes the hoses in.
        deployed = false;
        this.mode = TankPumpMode.Hold;
        base.PostDeSpawn(map, mode);
    }

    public override void EventRegistration()
    {
        base.EventRegistration();
        if (registered) return;
        registered = true;
        Vehicle.AddEvent(VehicleEventDefOf.UpgradeRefundCompleted, OnTankRemoved);
    }

    /// <summary>Taking a tank off with a load in it spills the load: Vehicle Framework has no way to refuse the job (agent's choice, 8 Nov
    /// 2026, instead of a Harmony patch). A new tank starts clean.</summary>
    void OnTankRemoved()
    {
        if (Fitted != null) return;
        if (HasLoad)
        {
            SpillOnGround(amount);
            Tell("PhobosVehicleSystems_TankRemovedSpilled".Translate(Vehicle.LabelShort.Named("VEHICLE")), MessageTypeDefOf.NegativeEvent);
        }
        ClearLoad();
        contents = null;
        residue = null;
    }

    public override AcceptanceReport CanMove(FloatMenuContext context) =>
        deployed ? "PhobosVehicleSystems_TankSetUpRefusal".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve() : base.CanMove(context);

    public override AcceptanceReport CanDraft() =>
        deployed ? "PhobosVehicleSystems_TankSetUpRefusal".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve() : base.CanDraft();

    public override void CompTickRare()
    {
        base.CompTickRare();
        TankFitting? fitting = Fitted;
        float outside = Outside;
        if (fitting == null || contents == null || !HasLoad)
        {
            temperature = outside;
            spoilHours = 0f;
            ventWarned = false;
            if (fitting != null && deployed) Pump(fitting);
            return;
        }
        temperature = TankRule.NextTemperature(temperature, outside, HoursPerRareTick, fitting.hoursToOutside);
        if (Burst()) return;
        Leak();
        if (!HasLoad) return;
        Vent(outside);
        if (!HasLoad) return;
        Spoil(fitting);
        if (HasLoad && deployed) Pump(fitting);
    }

    bool Burst()
    {
        VehicleComponent? shell = Vehicle.statHandler?.GetComponent(Props.shellPart);
        if (shell == null || shell.Health > 0f) return false;
        Rupture();
        return true;
    }

    void Rupture()
    {
        float lost = amount;
        PhobosTankContentsDef? was = contents;
        if (was == null) return;
        SpillOnGround(lost);
        ClearLoad();
        residue = was;
        if (Vehicle.Spawned && was.flammable && VehicleSystemsMod.Settings.FlagOn("tankExplosions"))
        {
            float radius = TankRule.ExplosionRadius(lost, Props.explosionReferenceUnits, Props.explosionReferenceRadius, was.explosionRadiusFactor, Props.explosionMaxRadius);
            if (radius > 0.5f) GenExplosion.DoExplosion(Vehicle.Position, Vehicle.Map, radius, was.explosionDamage ?? DamageDefOf.Flame, Vehicle, Props.explosionDamage);
        }
        Tell("PhobosVehicleSystems_TankBurst".Translate(Vehicle.LabelShort.Named("VEHICLE"), was.label.Named("CONTENTS")), MessageTypeDefOf.NegativeEvent);
    }

    void Leak()
    {
        float integrity = Vehicle.GetStatValue(TankDefOf.Phobos_TankIntegrity);
        float lost = TankRule.Leaked(Capacity, integrity, Props.leakFractionPerHour, HoursPerRareTick, amount);
        if (lost <= 0f) return;
        amount -= lost;
        SpillOnGround(lost);
        AfterLoadChange();
    }

    /// <summary>A relief valve opens in a fire or extreme heat and vents the load; one too damaged to open lets the vessel burst.</summary>
    void Vent(float outside)
    {
        if (contents is not { Vents: true }) return;
        if (outside < contents.ventsAbove && !Vehicle.IsBurning())
        {
            ventWarned = false;
            return;
        }
        VehicleComponent? valve = Vehicle.statHandler?.GetComponent(Props.valvePart);
        if (valve != null && valve.Efficiency < Props.controlFloor)
        {
            Rupture();
            return;
        }
        amount = Mathf.Max(0f, amount - Capacity * Props.ventFractionPerHour * HoursPerRareTick);
        AfterLoadChange();
        if (!ventWarned)
        {
            ventWarned = true;
            Tell("PhobosVehicleSystems_TankVenting".Translate(Vehicle.LabelShort.Named("VEHICLE"), contents.label.Named("CONTENTS")), MessageTypeDefOf.NegativeEvent);
        }
    }

    void Spoil(TankFitting fitting)
    {
        if (contents == null || contents.spoilsAfterDays <= 0f) return;
        spoilHours = TankRule.NextSpoilHours(spoilHours, temperature, HoursPerRareTick, fitting.spoilRate);
        if (!TankRule.Spoiled(spoilHours, contents.spoilsAfterDays)) return;
        Tell("PhobosVehicleSystems_TankSpoiled".Translate(Vehicle.LabelShort.Named("VEHICLE"), contents.label.Named("CONTENTS"), contents.spoilWord.Named("SPOILED")), MessageTypeDefOf.NegativeEvent);
        PhobosTankContentsDef was = contents;
        Pollute(amount);
        ClearLoad();
        residue = was;
    }

    /// <summary>Why the pump cannot run now, in-world, or null. The gizmos and the inspect line show it.</summary>
    public string? PumpBlocked()
    {
        if (Frozen) return "PhobosVehicleSystems_TankFrozen".Translate(contents!.label.Named("CONTENTS")).Resolve();
        if (Vehicle.GetStatValue(TankDefOf.Phobos_RigControl) < Props.controlFloor) return "PhobosVehicleSystems_TankControlsDamaged".Translate().Resolve();
        if (PumpShare() < Props.pumpFloor) return "PhobosVehicleSystems_TankPumpDamaged".Translate().Resolve();
        return null;
    }

    float PumpShare()
    {
        float design = Vehicle.VehicleDef.GetStatValueAbstract(TankDefOf.Phobos_PumpRate);
        return design <= 0f ? 0f : Vehicle.GetStatValue(TankDefOf.Phobos_PumpRate) / design;
    }

    float ThickFactor()
    {
        if (contents == null) return 1f;
        if (contents.thickensOnlyWithSetting != null && !VehicleSystemsMod.Settings.FlagOn(contents.thickensOnlyWithSetting)) return 1f;
        return TankRule.PumpFactor(temperature, contents.thickensBelow, contents.thickestAt, contents.thickFloor);
    }

    void Pump(TankFitting fitting)
    {
        pumpStatus = null;
        if (mode == TankPumpMode.Hold || contents == null) return;
        pumpStatus = PumpBlocked();
        if (pumpStatus != null) return;
        float rate = Vehicle.GetStatValue(TankDefOf.Phobos_PumpRate);
        if (mode == TankPumpMode.Load)
        {
            if (Vehicle.GetStatValue(TankDefOf.Phobos_TankIntegrity) < Props.integrityFloor)
            {
                pumpStatus = "PhobosVehicleSystems_TankHoled".Translate().Resolve();
                return;
            }
            float most = TankRule.Moved(rate, HoursPerRareTick, ThickFactor(), float.MaxValue, Space);
            if (most <= 0f)
            {
                pumpStatus = "PhobosVehicleSystems_TankFull".Translate().Resolve();
                return;
            }
            bool wasEmpty = !HasLoad;
            float moved = RunPorts(most, load: true);
            if (moved <= 0f) return;
            if (wasEmpty) temperature = ArrivalTemperature(Outside);
            amount += moved;
            AfterLoadChange();
            return;
        }
        if (!HasLoad)
        {
            pumpStatus = "PhobosVehicleSystems_TankEmpty".Translate().Resolve();
            return;
        }
        float out_ = RunPorts(TankRule.Moved(rate, HoursPerRareTick, ThickFactor(), amount, float.MaxValue), load: false);
        if (out_ <= 0f) return;
        amount = Mathf.Max(0f, amount - out_);
        if (!HasLoad) FinishEmptying();
        AfterLoadChange();
    }

    float RunPorts(float most, bool load)
    {
        string? reason = null;
        foreach (TankPort port in contents!.ports)
        {
            float moved = load ? port.Draw(this, most, out string? why) : port.Give(this, most, out why);
            if (moved > 0f)
            {
                pumpStatus = null;
                return moved;
            }
            reason ??= why;
        }
        pumpStatus = reason ?? "PhobosVehicleSystems_TankNothingToPump".Translate().Resolve();
        return 0f;
    }

    void FinishEmptying()
    {
        residue = contents;
        ClearLoad();
    }

    void ClearLoad()
    {
        amount = 0f;
        spoilHours = 0f;
        quality = 0;
        AfterLoadChange();
    }

    void AfterLoadChange() => Vehicle.GetComp<CompPhobosVehicleSystems>()?.Powertrain?.MarkMassDirty();

    void SpillOnGround(float units)
    {
        if (contents == null || !Vehicle.Spawned || units <= 0f) return;
        if (contents.spillFilth != null)
        {
            int count = Mathf.Clamp(Mathf.CeilToInt(units / 100f), 1, 6);
            FilthMaker.TryMakeFilth(Vehicle.Position, Vehicle.Map, contents.spillFilth, count);
        }
        if (contents.ignitesWhenSpilled) FireUtility.TryStartFireIn(Vehicle.Position, Vehicle.Map, SpillFireSize, Vehicle);
        Pollute(units);
    }

    void Pollute(float units)
    {
        if (contents == null || contents.pollutionCellsPerUnit <= 0f || !Vehicle.Spawned || !ModsConfig.BiotechActive) return;
        int cells = GenMath.RoundRandom(units * contents.pollutionCellsPerUnit);
        if (cells > 0) PollutionUtility.GrowPollutionAt(Vehicle.Position, Vehicle.Map, cells);
    }

    void Tell(string text, MessageTypeDef type) => Messages.Message(text, Vehicle, type, historical: false);

    // ---- Services the gizmos and the purge job call. ----

    public void ShellHit(float units)
    {
        if (!HasLoad || units <= 0f) return;
        if (contents is { detonatesOnHit: > 0f } && VehicleSystemsMod.Settings.FlagOn("tankExplosions") && Rand.Chance(contents.detonatesOnHit))
        {
            Rupture();
            return;
        }
        float lost = Mathf.Min(units, amount);
        amount -= lost;
        SpillOnGround(lost);
        if (!HasLoad) FinishEmptying();
        AfterLoadChange();
    }

    /// <summary>The fluids this tank could be set to hold; the reason explains why not, when not.</summary>
    public IEnumerable<PhobosTankContentsDef> ContentsChoices()
    {
        TankFitting? fitting = Fitted;
        if (fitting == null) return Enumerable.Empty<PhobosTankContentsDef>();
        return DefDatabase<PhobosTankContentsDef>.AllDefsListForReading.Where(def => def.phase == fitting.phase);
    }

    /// <summary>Why the contents cannot be changed to this, or null.</summary>
    public string? CannotHold(PhobosTankContentsDef def)
    {
        if (HasLoad) return "PhobosVehicleSystems_TankDrainFirst".Translate(contents!.label.Named("CONTENTS")).Resolve();
        if (residue != null && residue != def) return "PhobosVehicleSystems_TankPurgeFirst".Translate(residue.label.Named("CONTENTS")).Resolve();
        return null;
    }

    public void ChooseContents(PhobosTankContentsDef def)
    {
        string? refused = CannotHold(def);
        if (refused != null)
        {
            Tell(refused, MessageTypeDefOf.RejectInput);
            return;
        }
        contents = def;
        Tell("PhobosVehicleSystems_TankContentsSet".Translate(Vehicle.LabelShort.Named("VEHICLE"), def.label.Named("CONTENTS")), MessageTypeDefOf.SilentInput);
    }

    /// <summary>Why the tanker cannot set up now, or null.</summary>
    public string? CannotDeploy()
    {
        if (Fitted == null) return "PhobosVehicleSystems_TankNoneFitted".Translate().Resolve();
        if (contents == null) return "PhobosVehicleSystems_TankNoContents".Translate().Resolve();
        if (!Vehicle.Spawned) return "PhobosVehicleSystems_TankNotOnMap".Translate().Resolve();
        if (Vehicle.Drafted) return "PhobosVehicleSystems_TankStopFirst".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        if (PumpShare() < Props.pumpFloor) return "PhobosVehicleSystems_TankPumpDamaged".Translate().Resolve();
        return null;
    }

    public void SetDeployed(bool on)
    {
        if (on)
        {
            string? refused = CannotDeploy();
            if (refused != null)
            {
                Tell(refused, MessageTypeDefOf.RejectInput);
                return;
            }
        }
        deployed = on;
        if (!on) mode = TankPumpMode.Hold;
        pumpStatus = null;
        ShowHoses(on);
        Tell((on ? "PhobosVehicleSystems_TankSetUp" : "PhobosVehicleSystems_TankPackedUp").Translate(Vehicle.LabelShort.Named("VEHICLE")), MessageTypeDefOf.SilentInput);
    }

    public void SetMode(TankPumpMode wanted)
    {
        mode = wanted;
        pumpStatus = null;
        string key = wanted switch
        {
            TankPumpMode.Load => "PhobosVehicleSystems_TankPumpLoading",
            TankPumpMode.Unload => "PhobosVehicleSystems_TankPumpUnloading",
            _ => "PhobosVehicleSystems_TankPumpHolding",
        };
        Tell(key.Translate(Vehicle.LabelShort.Named("VEHICLE")), MessageTypeDefOf.SilentInput);
    }

    /// <summary>Finishes a purge, drawing its water; false (with the message said) if the water is not there.</summary>
    public bool FinishPurge()
    {
        if (!NeedsPurge) return false;
        if (!VehicleWater.TryDraw(Vehicle, Props.purgeLitres))
        {
            Tell("PhobosVehicleSystems_TankPurgeNoWater".Translate(Vehicle.LabelShort.Named("VEHICLE")), MessageTypeDefOf.RejectInput);
            return false;
        }
        residue = null;
        contents = null;
        Tell("PhobosVehicleSystems_TankPurged".Translate(Vehicle.LabelShort.Named("VEHICLE")), MessageTypeDefOf.TaskCompletion);
        return true;
    }

    void ShowHoses(bool on)
    {
        GraphicOverlayRenderer? renderer = Vehicle.DrawTracker?.overlayRenderer;
        if (renderer == null || Props.hosesOut == null) return;
        renderer.RemoveOverlays(HoseOverlayKey);
        if (on && Vehicle.Spawned) renderer.AddOverlay(HoseOverlayKey, GraphicOverlay.Create(Props.hosesOut, Vehicle));
    }

    public override string? CompInspectStringExtra()
    {
        if (Fitted == null) return "PhobosVehicleSystems_TankNoneFitted".Translate().Resolve();
        string line = contents == null
            ? "PhobosVehicleSystems_TankInspectUnset".Translate(Capacity.ToString("N0").Named("CAPACITY")).Resolve()
            : "PhobosVehicleSystems_TankInspect".Translate(amount.ToString("N0").Named("AMOUNT"), Capacity.ToString("N0").Named("CAPACITY"),
                contents.unitLabel.Named("UNIT"), contents.label.Named("CONTENTS"), temperature.ToStringTemperature("F0").Named("TEMPERATURE")).Resolve();
        if (NeedsPurge) line += "\n" + "PhobosVehicleSystems_TankInspectNeedsPurge".Translate(residue!.label.Named("CONTENTS")).Resolve();
        if (deployed && pumpStatus != null) line += "\n" + pumpStatus;
        return line;
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (Vehicle.Faction != Faction.OfPlayer || Fitted == null) yield break;
        foreach (Gizmo gizmo in TankGizmos.For(this)) yield return gizmo;
    }
}
