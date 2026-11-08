using Phobos.Framework.Rules;
using Verse;

namespace Phobos.Framework.Items;

/// <summary>
/// A thing that holds a few uses: a booster pack's starts, a kit's refills. The uses are saved with the thing; a new thing is full.
/// What spends a use, and what recharges it (a recipe, time aboard a running vehicle), is up to the mod that uses it; the arithmetic
/// is <see cref="ChargeRules"/>.
/// </summary>
public class CompProperties_PhobosCharges : CompProperties
{
    /// <summary>Uses when full.</summary>
    public int uses = 4;
    /// <summary>Hours of recharging per use, for mods that recharge over time; 0 means it never recharges over time.</summary>
    public float rechargeHoursPerUse;
    /// <summary>The Keyed line for the inspect pane, with {COUNT} and {MAX}; the Framework's own when empty.</summary>
    public string? inspectKey;

    public CompProperties_PhobosCharges() => compClass = typeof(CompPhobosCharges);
}

public class CompPhobosCharges : ThingComp
{
    float charge = -1f;

    public CompProperties_PhobosCharges Props => (CompProperties_PhobosCharges)props;

    /// <summary>The charge in uses, with part-charged uses as fractions.</summary>
    public float Charge
    {
        get
        {
            if (charge < 0) charge = Props.uses;
            return charge;
        }
    }

    public int WholeUses => ChargeRules.WholeUses(Charge);
    public bool CanUse => ChargeRules.CanUse(Charge);
    public bool Full => Charge >= Props.uses - 1e-4f;

    /// <summary>Spends one use; false (and nothing spent) when no whole use is left.</summary>
    public bool TryUse()
    {
        if (!CanUse) return false;
        charge = (float)ChargeRules.AfterUse(Charge);
        return true;
    }

    /// <summary>Recharges for some hours at the Def's hours per use.</summary>
    public void Recharge(double hours) => charge = (float)ChargeRules.Recharged(Charge, Props.uses, hours, Props.rechargeHoursPerUse);

    /// <summary>Sets the charge, for dev actions.</summary>
    public void DebugSet(float uses) => charge = System.Math.Max(0f, System.Math.Min(Props.uses, uses));

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref charge, "PhobosFramework_Charge", -1f);
    }

    public override string CompInspectStringExtra() =>
        (Props.inspectKey.NullOrEmpty() ? "PhobosFramework_ChargesLeft" : Props.inspectKey!).Translate(WholeUses.Named("COUNT"), Props.uses.Named("MAX"));
}

/// <summary>Matches things whose charges are full, so a recharging recipe can disallow them (its fixed ingredient filter).</summary>
public class SpecialThingFilterWorker_PhobosChargesFull : SpecialThingFilterWorker
{
    public override bool Matches(Thing t) => t.TryGetComp<CompPhobosCharges>() is { } charges && charges.Full;

    public override bool CanEverMatch(ThingDef def) => def.HasComp(typeof(CompPhobosCharges));
}
