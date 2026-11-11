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

/// <summary>One division of a tank, with its own fluid. A tank without compartment upgrades has exactly one.</summary>
public class TankCompartment : IExposable
{
    const float EmptyBelow = 0.5f;

    public PhobosTankContentsDef? contents;
    /// <summary>What this empty compartment last held, until it is purged.</summary>
    public PhobosTankContentsDef? residue;
    public float amount;
    public float spoilHours;
    /// <summary>A number a port may keep about the load's quality (Dubs Bad Hygiene's contamination level).</summary>
    public int quality;

    public bool HasLoad => amount >= EmptyBelow;
    public float Kilograms => contents == null ? 0f : amount * contents.kilogramsPerUnit;

    /// <summary>Empties it, leaving what it held as residue to be purged.</summary>
    public void Clear()
    {
        if (contents != null) residue = contents;
        amount = 0f;
        spoilHours = 0f;
        quality = 0;
    }

    public void ExposeData()
    {
        Scribe_Defs.Look(ref contents, "PhobosTankContents");
        Scribe_Defs.Look(ref residue, "PhobosTankResidue");
        Scribe_Values.Look(ref amount, "PhobosTankAmount");
        Scribe_Values.Look(ref spoilHours, "PhobosTankSpoilHours");
        Scribe_Values.Look(ref quality, "PhobosTankQuality");
        // A fluid whose mod has gone: a load of nothing would break the tank's menus.
        if (Scribe.mode == LoadSaveMode.PostLoadInit && contents == null) amount = 0f;
    }
}

/// <summary>
/// A vehicle's bulk tank (docs/development/utility-vehicles-design.md, Bulk tanker). Which tank is fitted, and how many compartments it has,
/// is read from the upgrade tree; each compartment's fluid and the tank's temperature and set-up state are saved here. One tank, one
/// temperature; the pump serves one compartment at a time, the selected one. Everything runs on the rare tick. Gizmos in TankGizmos read
/// this and call its service methods; nothing else changes it.
/// </summary>
public class CompPhobosTank : CompPhobosSetUp
{
    const string DamageOverlayKey = "PhobosTankDamage";
    const float HoursPerRareTick = GenTicks.TickRareInterval / (float)GenDate.TicksPerHour;
    /// <summary>Degrees above its freezing point that a fresh load arrives at (authored).</summary>
    const float ArrivalMargin = 5f;
    /// <summary>How big a fire a self-igniting spill starts (authored).</summary>
    const float SpillFireSize = 0.6f;

    List<TankCompartment> compartments = new();
    int selected;
    float temperature = 21f;
    TankPumpMode mode = TankPumpMode.Hold;
    bool ventWarned;
    /// <summary>Whether a compartment leaked at the last look. Derived, never saved.</summary>
    bool leaking;
    bool registered;
    /// <summary>Which damage overlays are drawn now. Derived from part health, never saved.</summary>
    bool[]? damageShown;

    /// <summary>Why the pump last did nothing, for the inspect line. Derived, never saved.</summary>
    string? pumpStatus;

    public CompProperties_PhobosTank Props => (CompProperties_PhobosTank)props;
    public float Temperature => temperature;
    public TankPumpMode Mode => mode;
    public float Capacity => Vehicle.GetStatValue(TankDefOf.Phobos_TankCapacity);

    public IReadOnlyList<TankCompartment> Compartments
    {
        get
        {
            EnsureOne();
            return compartments;
        }
    }

    /// <summary>The compartment the pump and the contents button work on.</summary>
    TankCompartment Selected
    {
        get
        {
            EnsureOne();
            return compartments[Mathf.Clamp(selected, 0, compartments.Count - 1)];
        }
    }

    public int SelectedIndex => Mathf.Clamp(selected, 0, Mathf.Max(0, compartments.Count - 1));
    public PhobosTankContentsDef? Contents => Selected.contents;
    public float Amount => Selected.amount;
    public float CompartmentCapacity => TankRule.CompartmentCapacity(Capacity, Compartments.Count);
    public float Space => Mathf.Max(0f, CompartmentCapacity - Selected.amount);
    public bool HasLoad => Compartments.Any(c => c.HasLoad);
    public float LoadKilograms => Compartments.Sum(c => c.Kilograms);
    public bool NeedsPurge => Compartments.Any(NeedsPurging);
    public string ResidueLabels => Compartments.Where(NeedsPurging).Select(c => c.residue!.label).Distinct().ToCommaList();

    static bool NeedsPurging(TankCompartment compartment) => compartment.residue != null && !compartment.HasLoad;

    public int Quality
    {
        get => Selected.quality;
        set => Selected.quality = value;
    }

    public float WeightedLoad
    {
        get
        {
            TankFitting? fitting = Fitted;
            float capacity = fitting == null ? 0f : Capacity;
            return capacity <= 0f ? 0f : Compartments.Sum(c => c.amount) / capacity * fitting!.loadWeight;
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
                if (Unlocked(tree, fitting.node)) return fitting;
            }
            return null;
        }
    }

    static bool Unlocked(CompUpgradeTree tree, string key) => VehicleUpgrades.Unlocked(tree, key);

    /// <summary>How many compartments the fitted tank has: by its upgrades, or its most if the player's setting makes them free.</summary>
    int WantedCompartments
    {
        get
        {
            TankFitting? fitting = Fitted;
            CompUpgradeTree? tree = Vehicle.CompUpgradeTree;
            if (fitting == null || tree == null) return 1;
            if (!VehicleSystemsMod.Settings.FlagOn("tankCompartmentsNeedUpgrades")) return Mathf.Max(1, fitting.maxCompartments);
            int count = 1;
            foreach (TankCompartmentNode entry in Props.compartmentNodes)
            {
                if (entry.compartments > count && Unlocked(tree, entry.node)) count = entry.compartments;
            }
            return Mathf.Clamp(count, 1, Mathf.Max(1, fitting.maxCompartments));
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

    bool SelectedFrozen => Selected.HasLoad && Selected.contents != null && TankRule.Frozen(temperature, Selected.contents.freezesBelow);

    /// <summary>A fresh load arrives at the outside temperature, but what can freeze arrives liquid and cools in the tank.</summary>
    float ArrivalTemperature(PhobosTankContentsDef def, float outside) =>
        float.IsNaN(def.freezesBelow) ? outside : Mathf.Max(outside, def.freezesBelow + ArrivalMargin);

    /// <summary>The cells a hose or a coupling reaches: under the vehicle and all round it.</summary>
    public IEnumerable<IntVec3> ContactCells()
    {
        foreach (IntVec3 cell in Vehicle.OccupiedRect()) yield return cell;
        foreach (IntVec3 cell in Vehicle.SurroundingCells) yield return cell;
    }

    void EnsureOne()
    {
        if (compartments.Count == 0) compartments.Add(new TankCompartment());
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Collections.Look(ref compartments, "PhobosTankCompartments", LookMode.Deep);
        Scribe_Values.Look(ref selected, "PhobosTankSelected");
        Scribe_Values.Look(ref temperature, "PhobosTankTemperature", 21f);
        Scribe_Values.Look(ref mode, "PhobosTankMode", TankPumpMode.Hold);
        compartments ??= new List<TankCompartment>();
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            // A tanker left set up with no tank by an earlier version: there is nothing to pack up, and no button to do it with.
            if (deployed && Fitted == null)
            {
                deployed = false;
                mode = TankPumpMode.Hold;
            }
            if (deployed) ShowHoses(true);
            RedrawDamage();
        });
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        // Leaving the map, for a caravan or a launch, takes the hoses in.
        ShowHoses(false);
        deployed = false;
        this.mode = TankPumpMode.Hold;
        base.PostDeSpawn(map, mode);
    }

    /// <summary>The game's work bar while the pump runs (owner, 12 Nov 2026): the pumped compartment's fill, rising as it fills and
    /// falling as it empties. Not shown while the pump holds or waits on something; the inspect pane says what.</summary>
    public override void PostDraw()
    {
        base.PostDraw();
        if (!deployed || mode == TankPumpMode.Hold || pumpStatus != null || Fitted == null) return;
        float capacity = CompartmentCapacity;
        if (capacity > 0f) Phobos.Framework.Interface.MapProgressBar.DrawOver(Vehicle, Amount / capacity);
    }

    public override void EventRegistration()
    {
        base.EventRegistration();
        if (registered) return;
        registered = true;
        Vehicle.AddEvent(VehicleEventDefOf.UpgradeRefundCompleted, OnTankRemoved);
        Vehicle.AddEvent(VehicleEventDefOf.UpgradeRefundCompleted, OnTreeChanged);
        Vehicle.AddEvent(VehicleEventDefOf.UpgradeCompleted, OnTreeChanged);
    }

    void OnTreeChanged()
    {
        SyncCompartments();
        SyncDamage();
    }

    /// <summary>Taking a tank off with a load in it spills the load: Vehicle Framework has no way to refuse the job (agent's choice, 8 Nov
    /// 2026, instead of a Harmony patch). A new tank starts clean.</summary>
    void OnTankRemoved()
    {
        if (Fitted != null) return;
        if (HasLoad)
        {
            foreach (TankCompartment compartment in Compartments) SpillOnGround(compartment.contents, compartment.amount);
            Tell("PhobosVehicleSystems_TankRemovedSpilled".Translate(Vehicle.LabelShort.Named("VEHICLE")), MessageTypeDefOf.NegativeEvent);
        }
        compartments.Clear();
        selected = 0;
        // With no tank the gizmos go, Pack up among them: a tanker left set up could never move again.
        deployed = false;
        mode = TankPumpMode.Hold;
        ShowHoses(false);
        AfterLoadChange();
    }

    /// <summary>Makes the compartments match the tree and the player's setting. What no longer fits is spilled, and the player is told, as when
    /// a tank is taken off with a load in it.</summary>
    void SyncCompartments()
    {
        EnsureOne();
        int wanted = WantedCompartments;
        while (compartments.Count < wanted) compartments.Add(new TankCompartment());
        bool spilled = false;
        for (int i = compartments.Count - 1; i >= wanted; i--)
        {
            TankCompartment extra = compartments[i];
            if (extra.HasLoad)
            {
                SpillOnGround(extra.contents, extra.amount);
                spilled = true;
            }
            compartments.RemoveAt(i);
        }
        selected = Mathf.Clamp(selected, 0, compartments.Count - 1);
        float room = CompartmentCapacity;
        foreach (TankCompartment compartment in compartments)
        {
            if (room <= 0f || compartment.amount <= room + 0.5f) continue;
            SpillOnGround(compartment.contents, compartment.amount - room);
            compartment.amount = room;
            spilled = true;
        }
        if (!spilled) return;
        Tell("PhobosVehicleSystems_TankCompartmentsSpilled".Translate(Vehicle.LabelShort.Named("VEHICLE")), MessageTypeDefOf.NegativeEvent);
        AfterLoadChange();
    }

    protected override string DeployedSaveKey => "PhobosTankDeployed";

    protected override GraphicDataOverlay? SetUpLook => Props.hosesOut;

    protected override string SetUpRefusal => "PhobosVehicleSystems_TankSetUpRefusal".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();

    protected override bool HeldBySetUp => TankRule.HoldsVehicle(deployed, Fitted != null);

    /// <summary>Whether the shell is sound enough to be filled, by the pump or by a bulk order.</summary>
    public bool ShellTakesLoad => TankRule.TakesLoad(Vehicle.GetStatValue(TankDefOf.Phobos_TankIntegrity), Props.integrityFloor);

    public override void CompTickRare()
    {
        base.CompTickRare();
        leaking = false;
        RareTank();
        SyncSounds();
    }

    /// <summary>The pump while it moves a load, and a leak while one runs (docs/development/sound-catalogue.md). Presentation only.</summary>
    void SyncSounds()
    {
        VehicleWorkSounds.Loop(Vehicle, "Pump", deployed && mode != TankPumpMode.Hold && pumpStatus == null);
        VehicleWorkSounds.Loop(Vehicle, "Leak", leaking);
    }

    void RareTank()
    {
        SyncDamage();
        SyncCompartments();
        TankFitting? fitting = Fitted;
        float outside = Outside;
        if (fitting == null || !HasLoad)
        {
            temperature = outside;
            ventWarned = false;
            if (fitting != null && deployed) Pump();
            return;
        }
        temperature = TankRule.NextTemperature(temperature, outside, HoursPerRareTick, fitting.hoursToOutside);
        if (Burst()) return;
        Leak();
        Vent(outside);
        Spoil(fitting);
        if (deployed) Pump();
    }

    bool Burst()
    {
        VehicleComponent? shell = Vehicle.statHandler?.GetComponent(Props.shellPart);
        if (shell == null || shell.Health > 0f) return false;
        Rupture();
        return true;
    }

    /// <summary>The whole tank goes at once, whatever its compartments hold. Only what burns feeds the blast.</summary>
    void Rupture()
    {
        List<TankCompartment> loaded = Compartments.Where(c => c.HasLoad && c.contents != null).ToList();
        if (loaded.Count == 0) return;
        float burning = 0f;
        float factor = 1f;
        DamageDef? damage = null;
        foreach (TankCompartment compartment in loaded)
        {
            PhobosTankContentsDef fluid = compartment.contents!;
            SpillOnGround(fluid, compartment.amount);
            if (fluid.flammable)
            {
                burning += compartment.amount;
                factor = Mathf.Max(factor, fluid.explosionRadiusFactor);
                damage ??= fluid.explosionDamage;
            }
        }
        string labels = loaded.Select(c => c.contents!.label).Distinct().ToCommaList();
        foreach (TankCompartment compartment in loaded) compartment.Clear();
        AfterLoadChange();
        if (Vehicle.Spawned && burning > 0f && VehicleSystemsMod.Settings.FlagOn("tankExplosions"))
        {
            float radius = TankRule.ExplosionRadius(burning, Props.explosionReferenceUnits, Props.explosionReferenceRadius, factor, Props.explosionMaxRadius);
            if (radius > 0.5f) GenExplosion.DoExplosion(Vehicle.Position, Vehicle.Map, radius, damage ?? DamageDefOf.Flame, Vehicle, Props.explosionDamage);
        }
        Tell("PhobosVehicleSystems_TankBurst".Translate(Vehicle.LabelShort.Named("VEHICLE"), labels.Named("CONTENTS")), MessageTypeDefOf.NegativeEvent);
    }

    void Leak()
    {
        float integrity = Vehicle.GetStatValue(TankDefOf.Phobos_TankIntegrity);
        float room = CompartmentCapacity;
        foreach (TankCompartment compartment in Compartments)
        {
            if (!compartment.HasLoad) continue;
            float lost = TankRule.Leaked(room, integrity, Props.leakFractionPerHour, HoursPerRareTick, compartment.amount);
            if (lost <= 0f) continue;
            leaking = true;
            compartment.amount -= lost;
            SpillOnGround(compartment.contents, lost);
            if (!compartment.HasLoad) compartment.Clear();
            AfterLoadChange();
        }
    }

    /// <summary>A relief valve opens in a fire or extreme heat and vents the load; one too damaged to open lets the vessel burst.</summary>
    void Vent(float outside)
    {
        foreach (TankCompartment compartment in Compartments)
        {
            PhobosTankContentsDef? fluid = compartment.contents;
            if (!compartment.HasLoad || fluid is not { Vents: true }) continue;
            if (outside < fluid.ventsAbove && !Vehicle.IsBurning())
            {
                ventWarned = false;
                continue;
            }
            VehicleComponent? valve = Vehicle.statHandler?.GetComponent(Props.valvePart);
            if (valve != null && valve.Efficiency < Props.controlFloor)
            {
                Rupture();
                return;
            }
            compartment.amount = Mathf.Max(0f, compartment.amount - CompartmentCapacity * Props.ventFractionPerHour * HoursPerRareTick);
            if (!compartment.HasLoad) compartment.Clear();
            AfterLoadChange();
            if (!ventWarned)
            {
                ventWarned = true;
                VehicleWorkSounds.Play(Vehicle, "Relief");
                Tell("PhobosVehicleSystems_TankVenting".Translate(Vehicle.LabelShort.Named("VEHICLE"), fluid.label.Named("CONTENTS")), MessageTypeDefOf.NegativeEvent);
            }
        }
    }

    void Spoil(TankFitting fitting)
    {
        foreach (TankCompartment compartment in Compartments)
        {
            PhobosTankContentsDef? fluid = compartment.contents;
            if (!compartment.HasLoad || fluid == null || fluid.spoilsAfterDays <= 0f) continue;
            compartment.spoilHours = TankRule.NextSpoilHours(compartment.spoilHours, temperature, HoursPerRareTick, fitting.spoilRate);
            if (!TankRule.Spoiled(compartment.spoilHours, fluid.spoilsAfterDays)) continue;
            Tell("PhobosVehicleSystems_TankSpoiled".Translate(Vehicle.LabelShort.Named("VEHICLE"), fluid.label.Named("CONTENTS"), fluid.spoilWord.Named("SPOILED")), MessageTypeDefOf.NegativeEvent);
            Pollute(fluid, compartment.amount);
            compartment.Clear();
            AfterLoadChange();
        }
    }

    /// <summary>Why the pump cannot run now, in-world, or null. The gizmos and the inspect line show it.</summary>
    public string? PumpBlocked()
    {
        if (SelectedFrozen) return "PhobosVehicleSystems_TankFrozen".Translate(Selected.contents!.label.Named("CONTENTS")).Resolve();
        if (Vehicle.GetStatValue(TankDefOf.Phobos_RigControl) < Props.controlFloor) return "PhobosVehicleSystems_TankControlsDamaged".Translate().Resolve();
        if (PumpShare() < Props.pumpFloor) return "PhobosVehicleSystems_TankPumpDamaged".Translate().Resolve();
        return null;
    }

    float PumpShare()
    {
        float design = Vehicle.VehicleDef.GetStatValueAbstract(TankDefOf.Phobos_PumpRate);
        return design <= 0f ? 0f : Vehicle.GetStatValue(TankDefOf.Phobos_PumpRate) / design;
    }

    float ThickFactor(PhobosTankContentsDef fluid)
    {
        if (fluid.thickensOnlyWithSetting != null && !VehicleSystemsMod.Settings.FlagOn(fluid.thickensOnlyWithSetting)) return 1f;
        return TankRule.PumpFactor(temperature, fluid.thickensBelow, fluid.thickestAt, fluid.thickFloor);
    }

    void Pump()
    {
        pumpStatus = null;
        TankCompartment compartment = Selected;
        PhobosTankContentsDef? fluid = compartment.contents;
        if (mode == TankPumpMode.Hold) return;
        if (fluid == null)
        {
            pumpStatus = "PhobosVehicleSystems_TankNoContents".Translate().Resolve();
            return;
        }
        pumpStatus = PumpBlocked();
        if (pumpStatus != null) return;
        float rate = Vehicle.GetStatValue(TankDefOf.Phobos_PumpRate);
        if (mode == TankPumpMode.Load)
        {
            if (!ShellTakesLoad)
            {
                pumpStatus = "PhobosVehicleSystems_TankHoled".Translate().Resolve();
                return;
            }
            float most = TankRule.Moved(rate, HoursPerRareTick, ThickFactor(fluid), float.MaxValue, Space);
            if (most <= 0f)
            {
                pumpStatus = "PhobosVehicleSystems_TankFull".Translate().Resolve();
                return;
            }
            bool tankWasEmpty = !HasLoad;
            float moved = RunPorts(fluid, most, load: true);
            if (moved <= 0f) return;
            if (tankWasEmpty) temperature = ArrivalTemperature(fluid, Outside);
            compartment.residue = null;
            compartment.amount += moved;
            AfterLoadChange();
            return;
        }
        if (!compartment.HasLoad)
        {
            pumpStatus = "PhobosVehicleSystems_TankEmpty".Translate().Resolve();
            return;
        }
        float given = RunPorts(fluid, TankRule.Moved(rate, HoursPerRareTick, ThickFactor(fluid), compartment.amount, float.MaxValue), load: false);
        if (given <= 0f) return;
        compartment.amount = Mathf.Max(0f, compartment.amount - given);
        if (!compartment.HasLoad) compartment.Clear();
        AfterLoadChange();
    }

    float RunPorts(PhobosTankContentsDef fluid, float most, bool load)
    {
        string? reason = null;
        foreach (TankPort port in fluid.ports)
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

    void AfterLoadChange() => Vehicle.GetComp<CompPhobosVehicleSystems>()?.Powertrain?.MarkMassDirty();

    void SpillOnGround(PhobosTankContentsDef? fluid, float units)
    {
        if (fluid == null || !Vehicle.Spawned || units <= 0f) return;
        if (fluid.spillFilth != null)
        {
            int count = Mathf.Clamp(Mathf.CeilToInt(units / 100f), 1, 6);
            FilthMaker.TryMakeFilth(Vehicle.Position, Vehicle.Map, fluid.spillFilth, count);
        }
        if (fluid.ignitesWhenSpilled) FireUtility.TryStartFireIn(Vehicle.Position, Vehicle.Map, SpillFireSize, Vehicle);
        Pollute(fluid, units);
    }

    void Pollute(PhobosTankContentsDef fluid, float units)
    {
        if (fluid.pollutionCellsPerUnit <= 0f || !Vehicle.Spawned || !ModsConfig.BiotechActive) return;
        int cells = GenMath.RoundRandom(units * fluid.pollutionCellsPerUnit);
        if (cells > 0) PollutionUtility.GrowPollutionAt(Vehicle.Position, Vehicle.Map, cells);
    }

    void Tell(string text, MessageTypeDef type) => Messages.Message(text, Vehicle, type, historical: false);

    // ---- Services the gizmos and the purge job call. ----

    /// <summary>A hit strikes one compartment that holds something. If that load can detonate, the whole tank may go.</summary>
    public void ShellHit(float units)
    {
        if (units <= 0f) return;
        if (!Compartments.Where(c => c.HasLoad).TryRandomElement(out TankCompartment struck)) return;
        if (struck.contents is { detonatesOnHit: > 0f } && VehicleSystemsMod.Settings.FlagOn("tankExplosions") && Rand.Chance(struck.contents.detonatesOnHit))
        {
            Rupture();
            return;
        }
        float lost = Mathf.Min(units, struck.amount);
        struck.amount -= lost;
        SpillOnGround(struck.contents, lost);
        if (!struck.HasLoad) struck.Clear();
        AfterLoadChange();
    }

    /// <summary>The fluids this tank could be set to hold; CannotHold explains why not, when not.</summary>
    public IEnumerable<PhobosTankContentsDef> ContentsChoices()
    {
        TankFitting? fitting = Fitted;
        if (fitting == null) return Enumerable.Empty<PhobosTankContentsDef>();
        return DefDatabase<PhobosTankContentsDef>.AllDefsListForReading.Where(def => def.phase == fitting.phase);
    }

    /// <summary>Why the selected compartment cannot be set to this, or null.</summary>
    public string? CannotHold(PhobosTankContentsDef def)
    {
        TankCompartment compartment = Selected;
        if (compartment.HasLoad) return "PhobosVehicleSystems_TankDrainFirst".Translate(compartment.contents!.label.Named("CONTENTS")).Resolve();
        if (compartment.residue != null && compartment.residue != def) return "PhobosVehicleSystems_TankPurgeFirst".Translate(compartment.residue.label.Named("CONTENTS")).Resolve();
        foreach (TankCompartment other in Compartments)
        {
            if (other == compartment) continue;
            PhobosTankContentsDef? neighbour = other.contents ?? other.residue;
            if (neighbour != null && neighbour.IsFood != def.IsFood)
            {
                return "PhobosVehicleSystems_TankFoodApart".Translate(def.label.Named("CONTENTS"), neighbour.label.Named("OTHER")).Resolve();
            }
        }
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
        Selected.contents = def;
        Tell("PhobosVehicleSystems_TankContentsSet".Translate(Vehicle.LabelShort.Named("VEHICLE"), def.label.Named("CONTENTS")), MessageTypeDefOf.SilentInput);
    }

    /// <summary>Moves the pump and the contents button on to the next compartment.</summary>
    public void SelectNextCompartment()
    {
        selected = (SelectedIndex + 1) % Compartments.Count;
        pumpStatus = null;
        Tell("PhobosVehicleSystems_TankCompartmentSelected".Translate(Vehicle.LabelShort.Named("VEHICLE"), (selected + 1).Named("INDEX")), MessageTypeDefOf.SilentInput);
    }

    /// <summary>Whether the selected compartment already holds fluid that more of the same can join.</summary>
    public bool CanTopUp => Selected.HasLoad;

    /// <summary>Fills the selected compartment with a purchase. BulkOrders has already checked and taken payment.</summary>
    public void ReceiveBulk(PhobosTankContentsDef fluid, float units)
    {
        TankCompartment compartment = Selected;
        if (!HasLoad) temperature = ArrivalTemperature(fluid, Outside);
        compartment.contents = fluid;
        compartment.residue = null;
        compartment.amount += units;
        AfterLoadChange();
    }

    /// <summary>Why the tanker cannot set up now, or null.</summary>
    public string? CannotDeploy()
    {
        if (Fitted == null) return "PhobosVehicleSystems_TankNoneFitted".Translate().Resolve();
        if (Selected.contents == null && !Compartments.Any(c => c.contents != null)) return "PhobosVehicleSystems_TankNoContents".Translate().Resolve();
        if (!Vehicle.Spawned) return "PhobosVehicleSystems_TankNotOnMap".Translate().Resolve();
        if (StillMoving) return "PhobosVehicleSystems_TankStopFirst".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
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
        if (deployed != on) VehicleWorkSounds.Play(Vehicle, "Hoses");
        deployed = on;
        if (!on) mode = TankPumpMode.Hold;
        pumpStatus = null;
        ShowHoses(on);
        SyncSounds();
        Tell(on ? "PhobosVehicleSystems_TankSetUp".Translate(Vehicle.LabelShort.Named("VEHICLE")) : "PhobosVehicleSystems_TankPackedUp".Translate(Vehicle.LabelShort.Named("VEHICLE")), MessageTypeDefOf.SilentInput);
    }

    public void SetMode(TankPumpMode wanted)
    {
        mode = wanted;
        pumpStatus = null;
        SyncSounds();
        NamedArgument name = Vehicle.LabelShort.Named("VEHICLE");
        Tell(wanted switch
        {
            TankPumpMode.Load => "PhobosVehicleSystems_TankPumpLoading".Translate(name),
            TankPumpMode.Unload => "PhobosVehicleSystems_TankPumpUnloading".Translate(name),
            _ => "PhobosVehicleSystems_TankPumpHolding".Translate(name),
        }, MessageTypeDefOf.SilentInput);
    }

    /// <summary>Finishes a purge of every empty compartment that needs one, drawing its water; false (with the message said) if the water is not there.</summary>
    public bool FinishPurge()
    {
        if (!NeedsPurge) return false;
        if (!VehicleWater.TryDraw(Vehicle, Props.purgeLitres))
        {
            Tell("PhobosVehicleSystems_TankPurgeNoWater".Translate(Vehicle.LabelShort.Named("VEHICLE")), MessageTypeDefOf.RejectInput);
            return false;
        }
        foreach (TankCompartment compartment in Compartments.Where(NeedsPurging))
        {
            compartment.residue = null;
            compartment.contents = null;
        }
        Tell("PhobosVehicleSystems_TankPurged".Translate(Vehicle.LabelShort.Named("VEHICLE")), MessageTypeDefOf.TaskCompletion);
        return true;
    }

    /// <summary>Starts the damage overlays again from nothing, after spawning.</summary>
    void RedrawDamage()
    {
        GraphicOverlayRenderer? renderer = Vehicle.DrawTracker?.overlayRenderer;
        if (renderer == null) return;
        for (int i = 0; i < Props.damageOverlays.Count; i++) renderer.RemoveOverlays(DamageOverlayKey + i);
        damageShown = null;
        SyncDamage();
    }

    /// <summary>Shows each damage overlay while its part is damaged. Runs on the rare tick because Combat Extended writes part health
    /// without an event, and after a tank is fitted or removed.</summary>
    void SyncDamage()
    {
        GraphicOverlayRenderer? renderer = Vehicle.DrawTracker?.overlayRenderer;
        if (renderer == null || !Vehicle.Spawned || Props.damageOverlays.Count == 0) return;
        damageShown ??= new bool[Props.damageOverlays.Count];
        TankFitting? fitting = Fitted;
        for (int i = 0; i < Props.damageOverlays.Count; i++)
        {
            TankDamageOverlay entry = Props.damageOverlays[i];
            VehicleComponent? part = Vehicle.statHandler?.GetComponent(entry.part);
            bool applies = entry.scope == TankDamageScope.Vehicle
                || (fitting != null && (entry.scope == TankDamageScope.Liquid ? fitting.phase == TankPhase.Liquid : fitting.phase == TankPhase.Gas));
            bool show = applies && part != null && part.HealthPercent < entry.belowHealth && entry.overlay != null;
            if (show == damageShown[i]) continue;
            damageShown[i] = show;
            if (show) renderer.AddOverlay(DamageOverlayKey + i, GraphicOverlay.Create(entry.overlay!, Vehicle));
            else renderer.RemoveOverlays(DamageOverlayKey + i);
        }
    }

    void ShowHoses(bool on) => ShowSetUp(on);

    string InspectLine(TankCompartment compartment, int index, float room)
    {
        NamedArgument capacity = room.ToString("N0").Named("CAPACITY");
        if (Compartments.Count == 1)
        {
            return compartment.contents == null
                ? "PhobosVehicleSystems_TankInspectUnset".Translate(capacity).Resolve()
                : "PhobosVehicleSystems_TankInspect".Translate(compartment.amount.ToString("N0").Named("AMOUNT"), capacity, compartment.contents.unitLabel.Named("UNIT"), compartment.contents.label.Named("CONTENTS")).Resolve();
        }
        NamedArgument number = (index + 1).Named("INDEX");
        return compartment.contents == null
            ? "PhobosVehicleSystems_TankInspectCompartmentUnset".Translate(number, capacity).Resolve()
            : "PhobosVehicleSystems_TankInspectCompartment".Translate(number, compartment.amount.ToString("N0").Named("AMOUNT"), capacity, compartment.contents.unitLabel.Named("UNIT"), compartment.contents.label.Named("CONTENTS")).Resolve();
    }

    public override string? CompInspectStringExtra()
    {
        if (Fitted == null) return "PhobosVehicleSystems_TankNoneFitted".Translate().Resolve();
        float room = CompartmentCapacity;
        var lines = new List<string>();
        for (int i = 0; i < Compartments.Count; i++) lines.Add(InspectLine(Compartments[i], i, room));
        if (HasLoad) lines.Add("PhobosVehicleSystems_TankInspectTemperature".Translate(temperature.ToStringTemperature("F0").Named("TEMPERATURE")).Resolve());
        if (NeedsPurge) lines.Add("PhobosVehicleSystems_TankInspectNeedsPurge".Translate(ResidueLabels.Named("CONTENTS")).Resolve());
        if (deployed && pumpStatus != null) lines.Add(pumpStatus);
        return string.Join("\n", lines);
    }

    public override IEnumerable<Gizmo> CompCaravanGizmos()
    {
        foreach (Gizmo gizmo in base.CompCaravanGizmos()) yield return gizmo;
        if (Vehicle.Faction != Faction.OfPlayer || Fitted == null) yield break;
        foreach (Gizmo gizmo in TankGizmos.ForCaravan(this)) yield return gizmo;
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (Vehicle.Faction != Faction.OfPlayer || Fitted == null) yield break;
        foreach (Gizmo gizmo in TankGizmos.For(this)) yield return gizmo;
    }
}
