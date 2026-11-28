using System.Collections.Generic;
using Phobos.Radio.Services;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Radio.Work;

/// <summary>
/// A child at radio school (Radio 0.31.0; docs/development/radio-friends-school-medevac.md): at the station's desk (target A) or where they
/// stand with a hand-held. It fills their learning as the game's radio talking does; with a teacher on the radio it is a lesson, the child
/// gaining one of the teacher's best skills at the game's lesson rate times the rules' radio factor, the teacher Social at the game's rate,
/// and both the game's thoughts for a lesson.
/// </summary>
public class JobDriver_PhobosRadioSchool : JobDriver, IRadioDeskCall
{
    private SkillDef? taughtSkill;
    private int sinceThoughts;
    private bool learning;
    private IntVec3 seat = IntVec3.Invalid;

    /// <summary>
    /// Radio 0.51.0: learning at a desk takes a channel there, unless the teacher sits beside them at it: a lesson in person (the owner's
    /// card of 27 Nov 2026, the terminal's second seat).
    /// </summary>
    public Thing? CallingFrom => learning && job.targetA.Thing is { } desk
        && !(RadioSchool.TeacherOf(pawn) is { } teacher && RadioSchool.Together(teacher, pawn, desk)) ? desk : null;

    public override bool TryMakePreToilReservations(bool errorOnFailed) =>
        !job.targetA.HasThing || RadioSeats.Claim(pawn, job, job.targetA.Thing, ref seat, errorOnFailed);

    public override string GetReport()
    {
        Pawn? teacher = RadioSchool.TeacherOf(pawn);
        return teacher != null && taughtSkill != null
            ? "PhobosRadioSchool_ReportLesson".Translate(teacher.LabelShort.Named("TEACHER"), taughtSkill.label.Named("SKILL")).Resolve()
            : "PhobosRadioSchool_ReportTalking".Translate().Resolve();
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnChildLearningConditions();
        this.AddFinishAction(_ =>
        {
            if (RadioSchool.TeacherOf(pawn) is { Spawned: true } teacher && teacher.CurJob is { } job && job.def == RadioSchoolDefOf.PhobosRadio_TeachByRadio
                && job.targetA.Thing == pawn)
                teacher.jobs.EndCurrentJob(JobCondition.Succeeded);
            RadioSchool.SetTeacher(pawn, null);
        });
        bool atDesk = job.targetA.HasThing;
        if (atDesk)
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => !Phobos.Radio.Services.RadioFlare.Works(job.targetA.Thing));
            yield return RadioSeats.GoTo(job.targetA.Thing, () => seat);
        }
        else this.FailOn(() => pawn.IsHashIntervalTick(250) && !RadioAdvice.InTouch(pawn));
        Toil learn = ToilMaker.MakeToil("PhobosRadio_RadioSchool");
        learn.initAction = () =>
        {
            learning = true;
            if (atDesk) RadioSeats.NoteSeated(pawn, job.targetA.Thing);
        };
        learn.AddFinishAction(() => learning = false);
        learn.tickIntervalAction = delta =>
        {
            if (atDesk && RadioSeats.HungUpBusy(this, pawn, job.targetA.Thing, delta)) return;
            if (atDesk) RadioSeats.AtDesk(pawn, job.targetA.Thing, delta);
            Lesson(delta);
            LearningUtility.LearningTickCheckEnd(pawn, delta);
        };
        if (DefDatabase<EffecterDef>.GetNamedSilentFail("Radiotalking") is { } talking) learn.WithEffect(talking, TargetIndex.A);
        learn.handlingFacing = atDesk;
        learn.defaultCompleteMode = ToilCompleteMode.Never;
        learn.socialMode = RandomSocialMode.Off;
        yield return learn;
    }

    /// <summary>A tick of the lesson, when a teacher is on the radio: the game's lesson, at the rules' share by radio, or at the in-person
    /// share when the teacher sits beside the child (Radio 0.56.0; the owner's answer of 28 Nov 2026: the full lesson rate).</summary>
    private void Lesson(int delta)
    {
        if (RadioSchool.TeacherOf(pawn) is not { } teacher) return;
        if (pawn.IsHashIntervalTick(250, delta) && !RadioSchool.StillTeaching(teacher, pawn, pawn.Map))
        {
            RadioSchool.SetTeacher(pawn, null);
            return;
        }
        if (teacher.Spawned && teacher.jobs?.curDriver is JobDriver_PhobosTeachByRadio { Talking: false }) return;
        taughtSkill ??= RadioSchool.ChooseSkill(teacher, pawn);
        if (taughtSkill == null) return;
        bool inPerson = job.targetA.Thing is { } seat && RadioSchool.Together(teacher, pawn, seat);
        RadioRulesDef rules = RadioRulesDef.Current;
        float perTick = (LearningDesireDefOf.Lessontaking?.xpPerTick ?? 0.25f) * (inPerson ? rules.schoolInPersonFactor : rules.schoolLessonFactor)
            * LearningUtility.LearningRateFactor(pawn) * HamShack.Factor(teacher);
        pawn.skills?.Learn(taughtSkill, perTick * delta);
        teacher.skills?.Learn(SkillDefOf.Social, 0.1f * delta);
        sinceThoughts += delta;
        if (sinceThoughts < 900) return;
        sinceThoughts = 0;
        teacher.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.GaveLesson, pawn);
        pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.WasTaught, teacher);
        string variant = teacher.Spawned ? "map" : "convoy";
        // Radio 0.51.0: side by side at one desk, in person; since 0.56.0 at the in-person rate, noted with it.
        if (inPerson)
        {
            variant = "in person";
            if (Seen.Wants("radio.lesson-in-person", pawn))
            {
                Seen.Note("radio.lesson-in-person", pawn, teacher.LabelShort + " at " + job.targetA.Thing.LabelShort, null,
                    new[] { new System.Collections.Generic.KeyValuePair<string, double>("rate", (double)rules.schoolInPersonFactor) });
            }
        }
        if (Seen.Wants("radio.school-lesson", pawn, variant)) Seen.Note("radio.school-lesson", pawn, teacher.LabelShort + ", " + taughtSkill.label, variant);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref taughtSkill, "PhobosRadio_TaughtSkill");
        Scribe_Values.Look(ref sinceThoughts, "PhobosRadio_SinceThoughts");
        Scribe_Values.Look(ref learning, "PhobosRadio_Learning");
        Scribe_Values.Look(ref seat, "PhobosRadio_Seat", IntVec3.Invalid);
    }
}

/// <summary>
/// A teacher giving a radio lesson on the map (Radio 0.31.0): at a station's desk (target B) or where they stand with a hand-held. Target A is
/// the child. It lasts while the child is at radio school with them as teacher.
/// </summary>
public class JobDriver_PhobosTeachByRadio : JobDriver, IRadioDeskCall
{
    private bool talking;
    private IntVec3 seat = IntVec3.Invalid;

    /// <summary>Radio 0.51.0: teaching at a desk takes a channel there, unless the child sits beside them at it, in person.</summary>
    public Thing? CallingFrom => talking && job.targetB.Thing is { } desk && !(job.targetA.Thing is Pawn child && RadioSchool.Together(pawn, child, desk)) ? desk : null;

    /// <summary>Whether they are at the radio, not still walking to the desk.</summary>
    public bool Talking => talking;

    public override bool TryMakePreToilReservations(bool errorOnFailed) =>
        !job.targetB.HasThing || RadioSeats.Claim(pawn, job, job.targetB.Thing, ref seat, errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOn(() => job.targetA.Thing is not Pawn child || child.Dead || RadioSchool.TeacherOf(child) != pawn
            || child.CurJob?.def.driverClass != typeof(JobDriver_PhobosRadioSchool));
        bool atDesk = job.targetB.HasThing;
        if (atDesk)
        {
            this.FailOnDespawnedOrNull(TargetIndex.B);
            yield return RadioSeats.GoTo(job.targetB.Thing, () => seat);
        }
        Toil teach = ToilMaker.MakeToil("PhobosRadio_TeachByRadio");
        teach.initAction = () =>
        {
            talking = true;
            if (atDesk) RadioSeats.NoteSeated(pawn, job.targetB.Thing);
        };
        if (atDesk)
        {
            teach.handlingFacing = true;
            teach.tickIntervalAction = delta =>
            {
                if (RadioSeats.HungUpBusy(this, pawn, job.targetB.Thing, delta)) return;
                RadioSeats.AtDesk(pawn, job.targetB.Thing, delta);
            };
        }
        teach.defaultCompleteMode = ToilCompleteMode.Never;
        teach.socialMode = RandomSocialMode.Off;
        teach.activeSkill = () => SkillDefOf.Social;
        teach.AddFinishAction(() => talking = false);
        yield return teach;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref talking, "PhobosRadio_Talking");
        Scribe_Values.Look(ref seat, "PhobosRadio_Seat", IntVec3.Invalid);
    }
}
