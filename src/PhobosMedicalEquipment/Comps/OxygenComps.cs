using System.Collections.Generic;
using System.Linq;
using Phobos.Framework;
using Phobos.Framework.Breathing;
using Phobos.Framework.Defs;
using Phobos.Medical.Defs;
using Phobos.Medical.Rules;
using Phobos.Medical.Services;
using RimWorld;
using Verse;

namespace Phobos.Medical.Comps;

/// <summary>A building that gives oxygen to patients in the beds it links to. Other mods can add machines with this in XML.</summary>
public class CompProperties_PhobosOxygenSupply : CompProperties
{
    /// <summary>How many patients it serves at once.</summary>
    public int maxPatients = 2;
    /// <summary>Fuel per patient per in-game hour, if the building has a fuel comp. Without one it needs only what its other comps ask for.</summary>
    public float fuelPerPatientHour = 1f;

    public CompProperties_PhobosOxygenSupply() => compClass = typeof(CompPhobosOxygenSupply);
}

public class CompPhobosOxygenSupply : ThingComp
{
    const int RareTickInterval = 250;

    /// <summary>Who it served on its last pass, for the inspect text. Derived, so never saved.</summary>
    List<Pawn> patients = new();

    public CompProperties_PhobosOxygenSupply Props => (CompProperties_PhobosOxygenSupply)props;
    public CompFacility? Facility => parent.TryGetComp<CompFacility>();
    public CompRefuelable? Fuel => parent.TryGetComp<CompRefuelable>();

    public override void CompTickRare()
    {
        base.CompTickRare();
        patients = OxygenService.Supply(this, RareTickInterval);
    }

    public override string CompInspectStringExtra() => OxygenService.StatusOf(this, patients.Count) switch
    {
        OxygenStatus.SwitchedOff => "PhobosMedical_StatusSwitchedOff".Translate(),
        OxygenStatus.NoPower => "PhobosMedical_StatusNoPower".Translate(),
        OxygenStatus.NoBed => "PhobosMedical_StatusNoBed".Translate(),
        OxygenStatus.NoOxygen => "PhobosMedical_StatusNoOxygen".Translate(),
        OxygenStatus.Treating => "PhobosMedical_StatusTreating".Translate(string.Join(", ", patients.Select(p => p.LabelShort)).Named("PATIENTS")),
        _ => "PhobosMedical_StatusWaiting".Translate(),
    };
}

/// <summary>The treatment condition's clock: it lapses unless a machine keeps refreshing it, so it cannot outlive its source.</summary>
public class HediffCompProperties_PhobosOnOxygen : HediffCompProperties
{
    public HediffCompProperties_PhobosOnOxygen() => compClass = typeof(HediffComp_PhobosOnOxygen);
}

public class HediffComp_PhobosOnOxygen : HediffComp
{
    int lastSupplyTick = -1;

    public void Refresh() => lastSupplyTick = Find.TickManager.TicksGame;

    public override bool CompShouldRemove =>
        lastSupplyTick >= 0 && Find.TickManager.TicksGame > lastSupplyTick + PhobosRulesDef.Current<PhobosMedicalRulesDef>().oxygenLingerTicks;

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref lastSupplyTick, "PhobosMedical_LastSupplyTick", -1);
    }

    public override string CompTipStringExtra
    {
        get
        {
            if (MedicalEquipmentMod.Settings.displayMode == DisplayMode.Plain) return "PhobosMedical_TipOnOxygenPlain".Translate();
            float halfLife = parent.def.GetModExtension<PhobosSuppliedOxygenExtension>()?.carbonMonoxideHalfLifeHours ?? 0f;
            return "PhobosMedical_TipOnOxygen".Translate((halfLife * 60f).ToString("0").Named("MINUTES"));
        }
    }
}
