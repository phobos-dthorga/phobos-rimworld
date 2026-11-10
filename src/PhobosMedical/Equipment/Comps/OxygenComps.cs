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

    public CompProperties_PhobosOxygenSupply() => compClass = typeof(CompPhobosOxygenSupply);
}

public class CompPhobosOxygenSupply : ThingComp
{
    const int RareTickInterval = 250;

    /// <summary>Who it served and who it left alone on its last pass, for the inspect text. Derived, so never saved.</summary>
    TreatmentPass pass = TreatmentPass.None;
    readonly SustainedSound flowing = new();

    public CompProperties_PhobosOxygenSupply Props => (CompProperties_PhobosOxygenSupply)props;
    public CompFacility? Facility => parent.TryGetComp<CompFacility>();
    public CompRefuelable? Fuel => parent.TryGetComp<CompRefuelable>();

    public override void CompTickRare()
    {
        base.CompTickRare();
        pass = OxygenService.Supply(this, RareTickInterval);
        flowing.Set(Props.flowingSound, pass.Treated.Count > 0, parent);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        flowing.End();
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
        MeasuringWithout = true;
        try { return PawnCapacityUtility.CalculateCapacityLevel(patient.health.hediffSet, PawnCapacityDefOf.Breathing); }
        finally { MeasuringWithout = false; }
    }

    public void SetRaisesBreathing(bool value)
    {
        if (raisesBreathing == value) return;
        raisesBreathing = value;
        pawn.health.Notify_HediffChanged(this);
    }
}

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
