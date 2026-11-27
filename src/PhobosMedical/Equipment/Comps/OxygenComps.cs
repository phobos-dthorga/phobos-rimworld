using System.Collections.Generic;
using System.Linq;
using Phobos.Framework;
using Phobos.Framework.Breathing;
using Phobos.Framework.Defs;
using Phobos.Framework.Health;
using Phobos.Medical.Defs;
using Phobos.Medical.Rules;
using Phobos.Medical.Services;
using Phobos.Framework.Sounds;
using RimWorld;
using Verse;
using Verse.Sound;

namespace Phobos.Medical.Comps;

/// <summary>A building that gives oxygen to patients in the beds it links to, or, if it is a bed itself, to whoever lies in it. Other
/// mods can add machines with this in XML.</summary>
public class CompProperties_PhobosOxygenSupply : CompProperties
{
    /// <summary>How many patients it serves at once.</summary>
    public int maxPatients = 2;
    /// <summary>Fuel per patient per in-game hour, if the building has a fuel comp. Without one it needs only what its other comps ask for.</summary>
    public float fuelPerPatientHour = 1f;
    /// <summary>The condition it gives; empty for the ordinary oxygen condition. A stronger treatment (a pressure chamber) names its own.</summary>
    public HediffDef? treatment;
    /// <summary>Serves anyone lying in the eight cells around it, in a bed or on the floor, with no bed link: a trolley wheeled to a casualty.</summary>
    public bool servesAdjacent;
    /// <summary>Loops while it gives oxygen to anyone (a regulator's hiss); null for silence, as the concentrator's own hum is its power sound.</summary>
    public SoundDef? flowingSound;
    /// <summary>Plays once when it starts treating someone after treating nobody (a pressure chamber's door sealing); null for none.</summary>
    public SoundDef? startSound;

    public CompProperties_PhobosOxygenSupply() => compClass = typeof(CompPhobosOxygenSupply);
}

public class CompPhobosOxygenSupply : ThingComp
{
    const int RareTickInterval = 250;

    /// <summary>Who it served and who it left alone on its last pass, for the inspect text. Derived, so never saved.</summary>
    TreatmentPass pass = TreatmentPass.None;
    readonly SustainedSound flowing = new();
    /// <summary>Whether it treated anyone at its last pass; null until its first pass after spawning, so a load replays no start sound.</summary>
    bool? treatingBefore;

    public CompProperties_PhobosOxygenSupply Props => (CompProperties_PhobosOxygenSupply)props;
    public CompFacility? Facility => parent.TryGetComp<CompFacility>();
    public CompRefuelable? Fuel => parent.TryGetComp<CompRefuelable>();

    public override void CompTickRare()
    {
        base.CompTickRare();
        pass = OxygenService.Supply(this, RareTickInterval);
        bool treating = pass.Treated.Count > 0;
        if (treating && treatingBefore == false) Props.startSound?.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
        treatingBefore = treating;
        flowing.Set(Props.flowingSound, treating, parent);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        flowing.End();
        treatingBefore = null;
        base.PostDeSpawn(map, mode);
    }

    public override string CompInspectStringExtra()
    {
        string status = (OxygenService.StatusOf(this, pass) switch
        {
            OxygenStatus.SwitchedOff => "PhobosMedical_StatusSwitchedOff".Translate(),
            OxygenStatus.NoPower => "PhobosMedical_StatusNoPower".Translate(),
            OxygenStatus.NoBed => "PhobosMedical_StatusNoBed".Translate(),
            OxygenStatus.NoOxygen => "PhobosMedical_StatusNoOxygen".Translate(),
            OxygenStatus.Treating => "PhobosMedical_StatusTreating".Translate(TreatmentPass.Names(pass.Treated).Named("PATIENTS")),
            OxygenStatus.NotNeeded => "PhobosMedical_StatusNotNeeded".Translate(TreatmentPass.Names(pass.NotNeeding).Named("PATIENTS")),
            _ => "PhobosMedical_StatusWaiting".Translate(),
        }).Resolve();
        // A chamber also says why an operation its patient waits for isn't being done.
        if (parent is Building_Bed bed && ChamberService.IsChamber(bed) && ChamberService.OccupantWaitingForSurgery(bed) is Pawn waiting)
            status += "\n" + "PhobosMedical_ChamberNoSurgery".Translate(waiting.LabelShort.Named("PATIENT")).Resolve();
        return status;
    }
}

/// <summary>
/// A breathing treatment: oxygen from a machine or a kit, or air pushed in by hand. Its Def has two stages: one that raises breathing
/// (with a ceiling at normal) and one that leaves it alone. A fixed ceiling would also cut a pawn whose lungs are better than normal, so
/// the stage is chosen from the patient's breathing measured without any of these treatments, when it starts and every few seconds
/// after. The choice is derived, so never saved.
/// </summary>
public class Hediff_PhobosOnOxygen : HediffWithComps
{
    const int LeavesBreathingStage = 0;
    const int RaisesBreathingStage = 1;
    const int CheckIntervalTicks = 250;

    /// <summary>True while measuring a patient's breathing without any breathing treatment (this, hand ventilation, a ventilator). One
    /// flag serves every instance: the game calculates on one thread.</summary>
    internal static bool MeasuringWithout { get; private set; }

    /// <summary>Starts true so a struggling patient keeps their help between loading a save and the first check.</summary>
    bool raisesBreathing = true;
    int ticksToCheck;

    public override void PostAdd(DamageInfo? dinfo)
    {
        base.PostAdd(dinfo);
        // Only the field: the game is still adding this condition and refreshes the pawn's capacities once it has.
        raisesBreathing = OxygenRule.RaisesBreathing(BreathingWithoutOxygen());
    }

    public override void PostTickInterval(int delta)
    {
        base.PostTickInterval(delta);
        ticksToCheck -= delta;
        if (ticksToCheck > 0) return;
        ticksToCheck = CheckIntervalTicks;
        CheckBreathing();
        PhobosBreathing.HealOnSuppliedOxygen(pawn, this, CheckIntervalTicks);
    }

    void CheckBreathing() => SetRaisesBreathing(OxygenRule.RaisesBreathing(BreathingWithoutOxygen()));

    public override int CurStageIndex => MeasuringWithout || !raisesBreathing ? LeavesBreathingStage : RaisesBreathingStage;

    public float BreathingWithoutOxygen() => BreathingWithoutTreatments(pawn);

    /// <summary>A patient's breathing with every breathing treatment left out: what their own body manages.</summary>
    public static float BreathingWithoutTreatments(Pawn patient)
    {
        // With no breathing treatment on the patient there is nothing to leave out, and the game's own remembered figure is the same
        // one. The full calculation below walks the whole body, and doctors ask this for every patient and every device they consider.
        if (!HasBreathingTreatment(patient)) return patient.health.capacities.GetLevel(PawnCapacityDefOf.Breathing);
        MeasuringWithout = true;
        try { return PawnCapacityUtility.CalculateCapacityLevel(patient.health.hediffSet, PawnCapacityDefOf.Breathing); }
        finally { MeasuringWithout = false; }
    }

    /// <summary>Whether the patient carries a condition whose stage answers to <see cref="MeasuringWithout"/>: oxygen, hand ventilation
    /// or a ventilator. A dead pawn's remembered figure is zero, so it takes the full calculation, as before.</summary>
    static bool HasBreathingTreatment(Pawn patient)
    {
        if (patient.Dead) return true;
        List<Hediff> hediffs = patient.health.hediffSet.hediffs;
        for (int i = 0; i < hediffs.Count; i++)
        {
            if (hediffs[i] is Hediff_PhobosOnOxygen || hediffs[i] is Hediff_PhobosVentilated) return true;
        }
        return false;
    }

    public void SetRaisesBreathing(bool value)
    {
        if (raisesBreathing == value) return;
        raisesBreathing = value;
        pawn.health.Notify_HediffChanged(this);
    }
}

/// <summary>The clock and tooltip of a breathing treatment (oxygen from a machine or kit, air pushed in by hand): the condition lapses
/// <c>lingerTicks</c> after its source last refreshed it, and its tooltip says how fast it clears carbon monoxide against room air. Goes in
/// the <c>comps</c> of a HediffDef whose <c>hediffClass</c> is <c>Phobos.Medical.Comps.Hediff_PhobosOnOxygen</c>, with that class's two
/// stages; the clearance comes from the Def's <c>PhobosSuppliedOxygenExtension</c>, and without one it only helps breathing.</summary>
public class HediffCompProperties_PhobosOnOxygen : HediffCompProperties_PhobosTreatment
{
    /// <summary>Carbon monoxide's half-life on room air, for comparison in the tooltip only: about 320 minutes (Weaver and colleagues,
    /// 2000, as cited in emergency medicine references; see docs/development/burns-and-smoke.md). The clearance itself is the poison's.</summary>
    public float roomAirHalfLifeHours = 5.33f;

    public HediffCompProperties_PhobosOnOxygen() => compClass = typeof(HediffComp_PhobosOnOxygen);
}

public class HediffComp_PhobosOnOxygen : HediffComp_PhobosTreatment
{
    public override string CompTipStringExtra
    {
        get
        {
            float halfLife = parent.def.GetModExtension<PhobosSuppliedOxygenExtension>()?.carbonMonoxideHalfLifeHours ?? 0f;
            float roomAir = ((HediffCompProperties_PhobosOnOxygen)props).roomAirHalfLifeHours;
            if (halfLife <= 0f) return "PhobosMedical_TipBreathingOnly".Translate();
            if (MedicalMod.DisplayMode == DisplayMode.Plain)
                return "PhobosMedical_TipOnOxygenPlain".Translate(OxygenRule.TimesFaster(roomAir, halfLife).ToString("0").Named("TIMES"));
            return "PhobosMedical_TipOnOxygen".Translate((halfLife * 60f).ToString("0").Named("MINUTES"), (roomAir * 60f).ToString("0").Named("ROOMAIR"));
        }
    }
}
