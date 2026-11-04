using Phobos.BurnsAndSmoke.Defs;
using Phobos.BurnsAndSmoke.Services;
using Phobos.Framework;
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
                    return "PhobosBurns_TipBurnLoad".Translate((severity * 100f).ToString("0"));
                case PhobosBurnTooltipKind.BurnShock:
                    if (mode == DisplayMode.Plain) return "PhobosBurns_TipBurnShockPlain".Translate();
                    BurnShockReadout readout = BurnLoadService.Readout(Pawn);
                    string line = "PhobosBurns_TipBurnShock".Translate(readout.Baux.ToString("0"), (readout.Target * 100f).ToString("0"));
                    if (mode == DisplayMode.Engineer)
                    {
                        line += "\n" + "PhobosBurns_TipBurnShockEngineer".Translate(readout.AgeYears.ToString("0"), (readout.Tbsa * 100f).ToString("0"),
                            readout.InhalationInjury ? PhobosBurnRulesDef.Current.inhalationBauxPoints.ToString("0") : "0",
                            (readout.TendedFraction * 100f).ToString("0"), readout.InBed ? "PhobosBurns_Yes".Translate().ToString() : "PhobosBurns_No".Translate().ToString(),
                            (readout.FluidsQuality * 100f).ToString("0"));
                    }
                    return line;
                case PhobosBurnTooltipKind.CarbonMonoxide:
                    if (mode == DisplayMode.Plain) return "PhobosBurns_TipCarbonMonoxidePlain".Translate();
                    return "PhobosBurns_TipCarbonMonoxide".Translate((severity * 100f).ToString("0"));
                case PhobosBurnTooltipKind.LungInjury:
                    if (mode == DisplayMode.Plain) return "PhobosBurns_TipLungInjuryPlain".Translate();
                    return "PhobosBurns_TipLungInjury".Translate((severity * 100f).ToString("0"));
                case PhobosBurnTooltipKind.SmokeAftermath:
                    return SmokeAftermath();
                default:
                    return "";
            }
        }
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
