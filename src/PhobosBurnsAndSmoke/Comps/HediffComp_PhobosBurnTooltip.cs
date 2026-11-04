using Phobos.BurnsAndSmoke.Defs;
using Phobos.BurnsAndSmoke.Services;
using Phobos.Framework;
using RimWorld;
using Verse;

namespace Phobos.BurnsAndSmoke.Comps;

public enum PhobosBurnTooltipKind
{
    BurnLoad,
    BurnShock,
    CarbonMonoxide,
    LungInjury,
    /// <summary>Added to another mod's smoke condition: what follows from breathing smoke, in the same words in every display mode.</summary>
    SmokeAftermath,
}

/// <summary>The health-tab tooltip by display mode. Presentation only: it reads what the services wrote.</summary>
public class HediffCompProperties_PhobosBurnTooltip : HediffCompProperties
{
    public PhobosBurnTooltipKind kind;

    public HediffCompProperties_PhobosBurnTooltip() => compClass = typeof(HediffComp_PhobosBurnTooltip);
}

public class HediffComp_PhobosBurnTooltip : HediffComp
{
    HediffCompProperties_PhobosBurnTooltip Props => (HediffCompProperties_PhobosBurnTooltip)props;

    public override string CompTipStringExtra
    {
        get
        {
            DisplayMode mode = BurnsAndSmokeMod.Settings.displayMode;
            float severity = parent.Severity;
            switch (Props.kind)
            {
                case PhobosBurnTooltipKind.BurnLoad:
                    if (mode == DisplayMode.Plain) return "PhobosBurns_TipBurnLoadPlain".Translate();
                    return "PhobosBurns_TipBurnLoad".Translate((severity * 100f).ToString("0").Named("BURNED"), Percent(PhobosBurnRulesDef.Current.shockStartTbsa).Named("SHOCKSTART"));
                case PhobosBurnTooltipKind.BurnShock:
                    if (mode == DisplayMode.Plain) return "PhobosBurns_TipBurnShockPlain".Translate();
                    BurnShockReadout readout = BurnLoadService.Readout(Pawn);
                    string line = "PhobosBurns_TipBurnShock".Translate(readout.Baux.ToString("0").Named("SCORE"), (readout.Target * 100f).ToString("0").Named("TARGET"));
                    if (mode == DisplayMode.Engineer)
                    {
                        PhobosBurnRulesDef rules = PhobosBurnRulesDef.Current;
                        line += "\n" + "PhobosBurns_TipBurnShockEngineer".Translate(readout.AgeYears.ToString("0").Named("AGE"), (readout.Tbsa * 100f).ToString("0").Named("BURNED"),
                            (readout.InhalationInjury ? rules.inhalationBauxPoints.ToString("0") : "0").Named("INHALATION"),
                            (readout.TendedFraction * 100f).ToString("0").Named("TENDED"),
                            (readout.InBed ? "PhobosBurns_Yes".Translate().ToString() : "PhobosBurns_No".Translate().ToString()).Named("INBED"),
                            (readout.FluidsQuality * 100f).ToString("0").Named("FLUIDS"),
                            rules.bauxNoDeathScore.ToString("0").Named("NODEATH"), (rules.bauxNoSurvivorScore - rules.bauxNoDeathScore).ToString("0").Named("RANGE"),
                            rules.tendedShockRelief.ToString("0.##").Named("TENDEDRELIEF"), rules.bedRestShockRelief.ToString("0.##").Named("BEDRELIEF"),
                            rules.fluidsShockReliefPerQuality.ToString("0.##").Named("FLUIDSRELIEF"), rules.shockTreatmentFloor.ToString("0.##").Named("FLOOR"),
                            rules.shockThresholdFloor.ToString("0.##").Named("STARTFLOOR"), Percent(rules.shockStartTbsa).Named("SHOCKSTART"));
                    }
                    return line;
                case PhobosBurnTooltipKind.CarbonMonoxide:
                    if (mode == DisplayMode.Plain) return "PhobosBurns_TipCarbonMonoxidePlain".Translate();
                    return CarbonMonoxideTip(severity);
                case PhobosBurnTooltipKind.LungInjury:
                    if (mode == DisplayMode.Plain) return "PhobosBurns_TipLungInjuryPlain".Translate();
                    return LungInjuryTip(severity);
                case PhobosBurnTooltipKind.SmokeAftermath:
                    return SmokeAftermath();
                default:
                    return "";
            }
        }
    }

    static string Percent(float fraction) => (fraction * 100f).ToString("0.#");

    /// <summary>The half-lives this condition actually uses, read from its own comp, so tuning the Def changes the text.</summary>
    string CarbonMonoxideTip(float severity)
    {
        HediffCompProperties_PhobosHalfLife? clearance = parent.def.CompProps<HediffCompProperties_PhobosHalfLife>();
        return "PhobosBurns_TipCarbonMonoxide".Translate((severity * 100f).ToString("0").Named("LEVEL"),
            (clearance?.halfLifeHoursRoofed ?? 0f).ToString("0.#").Named("ROOFED"), (clearance?.halfLifeHoursOutdoors ?? 0f).ToString("0.#").Named("OUTDOORS"));
    }

    /// <summary>The recovery rates this condition actually uses, read from its own comps. Tended well is tend quality 1.</summary>
    string LungInjuryTip(float severity)
    {
        float untended = -(parent.def.CompProps<HediffCompProperties_SeverityPerDay>()?.severityPerDay ?? 0f);
        float tended = untended - (parent.def.CompProps<HediffCompProperties_TendDuration>()?.severityPerDayTended ?? 0f);
        return "PhobosBurns_TipLungInjury".Translate((severity * 100f).ToString("0").Named("LEVEL"), Percent(untended).Named("UNTENDED"), Percent(tended).Named("TENDED"));
    }

    /// <summary>Promises only what the player's toggles leave switched on.</summary>
    static string SmokeAftermath()
    {
        BurnsAndSmokeSettings settings = BurnsAndSmokeMod.Settings;
        string lungs = settings.smoke ? "PhobosBurns_TipSmokeAftermathLungs".Translate().ToString() : "";
        string carbonMonoxide = settings.carbonMonoxide ? "PhobosBurns_TipSmokeAftermathCarbonMonoxide".Translate().ToString() : "";
        return lungs.Length > 0 && carbonMonoxide.Length > 0 ? lungs + " " + carbonMonoxide : lungs + carbonMonoxide;
    }
}
