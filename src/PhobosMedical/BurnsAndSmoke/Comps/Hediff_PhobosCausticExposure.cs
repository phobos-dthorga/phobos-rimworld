using Phobos.BurnsAndSmoke.Defs;
using Phobos.BurnsAndSmoke.Services;
using Phobos.Framework;
using Phobos.Medical;
using RimWorld;
using Verse;

namespace Phobos.BurnsAndSmoke.Comps;

/// <summary>
/// The caustic gas dose: three exposures (eyes, lungs and throat, skin) and the highest the eyes and lungs reached, how long since the
/// first, and which one-off tracks have fired. The delayed eye and lung harm follows the highest dose, since the current one fades.
/// Its severity is the largest exposure, for the stages. It holds state only; <see cref="CausticService"/> decides and does everything on
/// the burns pass. Saved, with stable keys, because the delayed harm is the point.
/// </summary>
public class Hediff_PhobosCausticExposure : HediffWithComps
{
    public float eyeDose;
    public float lungDose;
    public float skinDose;
    public float eyePeak;
    public float lungPeak;
    public int ticksSinceFirst;
    public int ticksSinceExposed;
    public float lungGiven;
    public float skinPending;
    public bool throatRolled;
    public bool immunityRolled;
    public bool aftermathRolled;

    public float MaxDose => eyeDose > lungDose ? (eyeDose > skinDose ? eyeDose : skinDose) : (lungDose > skinDose ? lungDose : skinDose);

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref eyeDose, "PhobosBurns_EyeDose");
        Scribe_Values.Look(ref lungDose, "PhobosBurns_LungDose");
        Scribe_Values.Look(ref skinDose, "PhobosBurns_SkinDose");
        Scribe_Values.Look(ref eyePeak, "PhobosBurns_EyePeak");
        Scribe_Values.Look(ref lungPeak, "PhobosBurns_LungPeak");
        Scribe_Values.Look(ref ticksSinceFirst, "PhobosBurns_TicksSinceFirst");
        Scribe_Values.Look(ref ticksSinceExposed, "PhobosBurns_TicksSinceExposed");
        Scribe_Values.Look(ref lungGiven, "PhobosBurns_LungGiven");
        Scribe_Values.Look(ref skinPending, "PhobosBurns_SkinPending");
        Scribe_Values.Look(ref throatRolled, "PhobosBurns_ThroatRolled");
        Scribe_Values.Look(ref immunityRolled, "PhobosBurns_ImmunityRolled");
        Scribe_Values.Look(ref aftermathRolled, "PhobosBurns_AftermathRolled");
    }

    public override string TipStringExtra
    {
        get
        {
            string text = base.TipStringExtra;
            float hoursToOnset = CausticService.HoursToOnset(this);
            string line;
            if (MedicalMod.DisplayMode == DisplayMode.Plain)
                line = (hoursToOnset > 0f ? "PhobosBurns_CausticTipLaterPlain" : "PhobosBurns_CausticTipNowPlain").Translate().Resolve();
            else
                line = "PhobosBurns_CausticTip".Translate(eyeDose.ToStringPercent().Named("EYES"), lungDose.ToStringPercent().Named("LUNGS"),
                    skinDose.ToStringPercent().Named("SKIN"), hoursToOnset.ToString("0").Named("HOURS")).Resolve();
            return string.IsNullOrEmpty(text) ? line : text + "\n" + line;
        }
    }
}
