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
public class JobDriver_PhobosRadioSchool : JobDriver
{
    private SkillDef? taughtSkill;
    private int sinceThoughts;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

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
            this.FailOn(() => job.targetA.Thing.TryGetComp<CompPowerTrader>() is { PowerOn: false });
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        }
        else this.FailOn(() => pawn.IsHashIntervalTick(250) && !RadioAdvice.InTouch(pawn));
        Toil learn = ToilMaker.MakeToil("PhobosRadio_RadioSchool");
        learn.tickIntervalAction = delta =>
        {
            if (atDesk) pawn.rotationTracker.FaceTarget(job.targetA);
            Lesson(delta);
            LearningUtility.LearningTickCheckEnd(pawn, delta);
        };
        if (DefDatabase<EffecterDef>.GetNamedSilentFail("Radiotalking") is { } talking) learn.WithEffect(talking, TargetIndex.A);
        learn.handlingFacing = atDesk;
        learn.defaultCompleteMode = ToilCompleteMode.Never;
        learn.socialMode = RandomSocialMode.Off;
        yield return learn;
    }

    /// <summary>A tick of the lesson, when a teacher is on the radio: the game's lesson, at the rules' share by radio.</summary>
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
        float perTick = (LearningDesireDefOf.Lessontaking?.xpPerTick ?? 0.25f) * RadioRulesDef.Current.schoolLessonFactor * LearningUtility.LearningRateFactor(pawn);
        pawn.skills?.Learn(taughtSkill, perTick * delta);
        teacher.skills?.Learn(SkillDefOf.Social, 0.1f * delta);
        sinceThoughts += delta;
        if (sinceThoughts < 900) return;
        sinceThoughts = 0;
        teacher.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.GaveLesson, pawn);
        pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.WasTaught, teacher);
        string variant = teacher.Spawned ? "map" : "convoy";
        if (Seen.Wants("radio.school-lesson", pawn, variant)) Seen.Note("radio.school-lesson", pawn, teacher.LabelShort + ", " + taughtSkill.label, variant);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref taughtSkill, "PhobosRadio_TaughtSkill");
        Scribe_Values.Look(ref sinceThoughts, "PhobosRadio_SinceThoughts");
    }
}

/// <summary>
/// A teacher giving a radio lesson on the map (Radio 0.31.0): at a station's desk (target B) or where they stand with a hand-held. Target A is
/// the child. It lasts while the child is at radio school with them as teacher.
/// </summary>
public class JobDriver_PhobosTeachByRadio : JobDriver
{
    private bool talking;

    /// <summary>Whether they are at the radio, not still walking to the desk.</summary>
    public bool Talking => talking;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOn(() => job.targetA.Thing is not Pawn child || child.Dead || RadioSchool.TeacherOf(child) != pawn
            || child.CurJob?.def.driverClass != typeof(JobDriver_PhobosRadioSchool));
        bool atDesk = job.targetB.HasThing;
        if (atDesk)
        {
            this.FailOnDespawnedOrNull(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
        }
        Toil teach = ToilMaker.MakeToil("PhobosRadio_TeachByRadio");
        teach.initAction = () => talking = true;
        if (atDesk)
        {
            teach.handlingFacing = true;
            teach.tickIntervalAction = _ => pawn.rotationTracker.FaceTarget(job.targetB);
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
    }
}
