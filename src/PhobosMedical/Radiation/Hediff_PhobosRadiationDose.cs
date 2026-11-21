using System.Collections.Generic;
using Phobos.Framework;
using Phobos.Medical;
using Phobos.Radiation.Rules;
using Phobos.Radiation.Services;
using Verse;

namespace Phobos.Radiation;

/// <summary>
/// A person's radiation dose (D'thorga Medical 0.21.0; docs/development/radiation-model.md, RM2 to RM4, RM7): every dose by the game day
/// it was taken, so repair can be worked out; what is already beyond repair; the lifetime dose; the highest effective dose reached, which
/// the course follows; when the course began and when they first vomited. It holds state only; <see cref="RadiationService"/> decides.
/// Saved under stable keys: the delayed illness is the point. Its severity is the effective dose in grays, for its stages.
/// </summary>
public class Hediff_PhobosRadiationDose : HediffWithComps
{
    /// <summary>Doses by game day, newest last.</summary>
    public List<int> doseDays = new();
    public List<float> doseGrays = new();
    /// <summary>The never-repaired tenth of doses too old to keep one by one.</summary>
    public float beyondRepair;
    /// <summary>Every gray ever taken, never repaired: for late effects and the exposure category.</summary>
    public float lifetime;
    /// <summary>The highest effective dose reached since the course began.</summary>
    public float peak;
    /// <summary>The tick the course began (the dose crossed the threshold), or -1.</summary>
    public int courseStart = -1;
    /// <summary>The tick of the first vomiting in this course, or -1.</summary>
    public int firstVomit = -1;
    /// <summary>Whether this person will vomit in this course: fixed when it begins, from who they are, never rolled each time.</summary>
    public bool willVomit;
    /// <summary>The tick a blood count was taken in a hospital bed, two days or more after the course began, or -1.</summary>
    public int bloodCount = -1;
    /// <summary>The illness's highest severity in this course, so it never rises again while recovering.</summary>
    public float illnessHeight;
    /// <summary>Where the last dose came from, told in the tooltip.</summary>
    public string? lastSource;

    // ---- Late effects (Medical 0.23.0, RM8) ----

    /// <summary>The highest effective dose ever reached, after repair: what sterility follows.</summary>
    public float highest;
    /// <summary>The game day a late effect is due, <see cref="LateNone"/> while none is coming, <see cref="LateGiven"/> once given.</summary>
    public int leukaemiaDay = LateNone;
    public int cancerDay = LateNone;
    public int cataractDay = LateNone;
    /// <summary>The highest dose a temporary sterility was given for, so a larger dose gives a longer one.</summary>
    public float sterileTemporaryFor;
    public bool sterilePermanent;
    /// <summary>The pregnancy the prenatal doses belong to (its load ID), and the dose taken in each <see cref="Rules.PrenatalWindow"/>.</summary>
    public int pregnancyId = -1;
    public List<float> prenatal = new();

    // ---- Contamination (Medical 0.24.0, RM6) ----

    /// <summary>Fallout dust on the skin, as last set, and the tick it was set: it fades by the 7:10 rule.</summary>
    public float skinDust;
    public int skinDustTick;
    /// <summary>The dose still to come from inside, in grays: delivered over weeks as the body clears it.</summary>
    public float inside;
    /// <summary>The thyroid's dose from fallout's iodine, in grays, and when a thyroid cancer is due.</summary>
    public float thyroid;
    public int thyroidCancerDay = LateNone;

    public const int LateNone = -1;
    public const int LateGiven = -2;

    public float Effective => Severity;

    public bool InCourse => courseStart >= 0;

    public void AddDose(int day, float grays)
    {
        int last = doseDays.Count - 1;
        if (last >= 0 && doseDays[last] == day) doseGrays[last] += grays;
        else
        {
            doseDays.Add(day);
            doseGrays.Add(grays);
        }
        lifetime += grays;
    }

    public void StartCourse(int tick, bool vomits)
    {
        courseStart = tick;
        firstVomit = -1;
        willVomit = vomits;
        bloodCount = -1;
        illnessHeight = 0f;
        peak = Effective;
    }

    public void EndCourse()
    {
        courseStart = -1;
        firstVomit = -1;
        bloodCount = -1;
        illnessHeight = 0f;
        peak = 0f;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref doseDays, "PhobosRadiation_DoseDays", LookMode.Value);
        Scribe_Collections.Look(ref doseGrays, "PhobosRadiation_DoseGrays", LookMode.Value);
        Scribe_Values.Look(ref beyondRepair, "PhobosRadiation_BeyondRepair");
        Scribe_Values.Look(ref lifetime, "PhobosRadiation_Lifetime");
        Scribe_Values.Look(ref peak, "PhobosRadiation_Peak");
        Scribe_Values.Look(ref courseStart, "PhobosRadiation_CourseStart", -1);
        Scribe_Values.Look(ref firstVomit, "PhobosRadiation_FirstVomit", -1);
        Scribe_Values.Look(ref willVomit, "PhobosRadiation_WillVomit");
        Scribe_Values.Look(ref bloodCount, "PhobosRadiation_BloodCount", -1);
        Scribe_Values.Look(ref illnessHeight, "PhobosRadiation_IllnessHeight");
        Scribe_Values.Look(ref lastSource, "PhobosRadiation_LastSource");
        Scribe_Values.Look(ref highest, "PhobosRadiation_Highest");
        Scribe_Values.Look(ref leukaemiaDay, "PhobosRadiation_LeukaemiaDay", LateNone);
        Scribe_Values.Look(ref cancerDay, "PhobosRadiation_CancerDay", LateNone);
        Scribe_Values.Look(ref cataractDay, "PhobosRadiation_CataractDay", LateNone);
        Scribe_Values.Look(ref sterileTemporaryFor, "PhobosRadiation_SterileTemporaryFor");
        Scribe_Values.Look(ref sterilePermanent, "PhobosRadiation_SterilePermanent");
        Scribe_Values.Look(ref pregnancyId, "PhobosRadiation_PregnancyId", -1);
        Scribe_Collections.Look(ref prenatal, "PhobosRadiation_Prenatal", LookMode.Value);
        Scribe_Values.Look(ref skinDust, "PhobosRadiation_SkinDust");
        Scribe_Values.Look(ref skinDustTick, "PhobosRadiation_SkinDustTick");
        Scribe_Values.Look(ref inside, "PhobosRadiation_Inside");
        Scribe_Values.Look(ref thyroid, "PhobosRadiation_Thyroid");
        Scribe_Values.Look(ref thyroidCancerDay, "PhobosRadiation_ThyroidCancerDay", LateNone);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            prenatal ??= new List<float>();
            if (highest < Severity) highest = Severity;
            doseDays ??= new List<int>();
            doseGrays ??= new List<float>();
            if (doseDays.Count != doseGrays.Count)
            {
                doseDays.Clear();
                doseGrays.Clear();
            }
        }
    }

    public override string TipStringExtra
    {
        get
        {
            string text = base.TipStringExtra;
            string line = RadiationService.Describe(this) + Contamination.Tip(pawn, this);
            return string.IsNullOrEmpty(text) ? line : text + "\n" + line;
        }
    }
}
