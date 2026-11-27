using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace Phobos.Radio.Services;

// School of the Air (Radio 0.31.0; docs/development/radio-friends-school-medevac.md, the owner's card of 27 Nov 2026: "Our radios count, plus
// radio lessons"). A child learns by radio at our station's desk or on a hand-held in touch: radio talking with nobody to teach, a lesson
// when a teacher is in radio touch, on the map or away in a convoy whose radio reaches the child's colony. Needs Biotech, as children do.

/// <summary>
/// Radio school: who teaches whom by radio now.
/// <para>Cost: nothing each tick. The game asks the learning giver only when a child picks what to learn; a teacher is looked for once a
/// lesson, over the colony's free colonists and the convoys in touch with it. A lesson's tick checks a convoy teacher every 250 ticks.</para>
/// </summary>
public static class RadioSchool
{
    private static readonly Dictionary<Pawn, Pawn> teacherOf = new();

    public static void Clear() => teacherOf.Clear();

    /// <summary>The teacher a child has on the radio now, or null.</summary>
    public static Pawn? TeacherOf(Pawn child) => teacherOf.TryGetValue(child, out var teacher) ? teacher : null;

    public static void SetTeacher(Pawn child, Pawn? teacher)
    {
        if (teacher == null) teacherOf.Remove(child);
        else teacherOf[child] = teacher;
    }

    /// <summary>Whether this person teaches anyone by radio now.</summary>
    public static bool Teaching(Pawn teacher) => teacherOf.ContainsValue(teacher);

    /// <summary>Childcare, the game's work for teaching, when Biotech is loaded.</summary>
    private static WorkTypeDef? Childcare => DefDatabase<WorkTypeDef>.GetNamedSilentFail("Childcare");

    /// <summary>
    /// Whether an adult can teach by radio now: awake, able to talk and hear, not incapable of social work, with childcare switched on (as the
    /// game's lessons need), on no call or lesson already; on a map, free to give way to it.
    /// </summary>
    public static bool CanTeach(Pawn teacher)
    {
        if (teacher.DevelopmentalStage != DevelopmentalStage.Adult || teacher.Downed || !teacher.Awake() || teacher.InMentalState || teacher.Drafted) return false;
        if (!teacher.health.capacities.CapableOf(PawnCapacityDefOf.Talking) || !teacher.health.capacities.CapableOf(PawnCapacityDefOf.Hearing)) return false;
        if (teacher.WorkTagIsDisabled(WorkTags.Social) || teacher.skills == null) return false;
        if (Childcare is { } childcare && (teacher.WorkTypeIsDisabled(childcare) || teacher.workSettings == null || !teacher.workSettings.WorkIsActive(childcare))) return false;
        if (RadioAdvice.CallBy(teacher) != null || RadioAdvice.CallOf(teacher) != null || Teaching(teacher)) return false;
        return !teacher.Spawned || RadioAdvice.Interruptible(teacher);
    }

    /// <summary>
    /// A teacher for a child on <paramref name="map"/>, or null: a parent first, then the most skilled. On the map with a hand-held in touch
    /// or a station's desk to walk to; or in a convoy whose radio reaches this colony.
    /// </summary>
    public static Pawn? FindTeacher(Pawn child, Map map, out Thing? desk)
    {
        desk = null;
        Pawn? best = null;
        Thing? bestDesk = null;
        int bestScore = int.MinValue;
        var net = RadioNet.For(map);
        foreach (Pawn candidate in map.mapPawns.FreeColonistsSpawned)
        {
            if (candidate == child || !CanTeach(candidate)) continue;
            Thing? at = null;
            if (!RadioAdvice.InTouch(candidate))
            {
                if (candidate.InBed() || RadioAdvice.NearestDesk(candidate, net) is not { } found) continue;
                at = found;
            }
            Consider(candidate, at);
        }
        foreach (Caravan convoy in Find.WorldObjects.Caravans)
        {
            if (!convoy.IsPlayerControlled) continue;
            foreach (Pawn candidate in convoy.PawnsListForReading)
            {
                if (!candidate.IsColonist || !CanTeach(candidate)) continue;
                // Radio 0.40.0: no lessons by Morse: it carries no speech.
                if (!RemoteReach.Of(candidate).Any(home => home.Home == map && !home.Morse)) break;
                Consider(candidate, null);
            }
        }
        desk = bestDesk;
        return best;

        void Consider(Pawn candidate, Thing? at)
        {
            int score = TopSkill(candidate, child) + (IsParent(candidate, child) ? 100 : 0);
            if (score <= bestScore) return;
            best = candidate;
            bestDesk = at;
            bestScore = score;
        }
    }

    private static bool IsParent(Pawn adult, Pawn child) => child.GetMother() == adult || child.GetFather() == adult;

    private static int TopSkill(Pawn teacher, Pawn child) =>
        Teachable(teacher, child).Select(skill => teacher.skills.GetSkill(skill).Level).DefaultIfEmpty(-1).Max();

    /// <summary>The skills a teacher can teach this child, as the game's lessons choose: ones with a lesson, neither of them incapable.</summary>
    public static IEnumerable<SkillDef> Teachable(Pawn teacher, Pawn child) =>
        teacher.skills.skills.Where(s => !s.TotallyDisabled && s.def.lessonInteraction != null && child.skills?.GetSkill(s.def) is { TotallyDisabled: false })
            .Select(s => s.def);

    /// <summary>One of the teacher's four best teachable skills, as the game's lessons pick; null with none.</summary>
    public static SkillDef? ChooseSkill(Pawn teacher, Pawn child) =>
        Teachable(teacher, child).OrderByDescending(skill => teacher.skills.GetSkill(skill).Level).Take(4).ToList() is { Count: > 0 } top ? top.RandomElement() : null;

    /// <summary>Whether a teacher is still teaching this child: on the map, on our teaching job for them; in a convoy, still in touch.</summary>
    public static bool StillTeaching(Pawn teacher, Pawn child, Map map)
    {
        if (teacher.Dead || teacher.Destroyed || teacher.Downed || !teacher.Awake()) return false;
        if (teacher.Spawned) return teacher.CurJob is { } job && job.def == RadioSchoolDefOf.PhobosRadio_TeachByRadio && job.targetA.Thing == child;
        return teacher.GetCaravan() != null && RemoteReach.Of(teacher).Any(home => home.Home == map && !home.Morse);
    }
}

/// <summary>
/// Radio school, a child's learning (Radio 0.31.0): at a powered station's desk, or anywhere on a hand-held in touch with a station. After the
/// game's microelectronics research, as its radio talking needs. A teacher, when one is found, is given the call as a job (or, in a convoy,
/// needs none).
/// </summary>
public class LearningGiver_PhobosRadioSchool : LearningGiver
{
    public override bool CanGiveDesire => ResearchProjectDefOf.MicroelectronicsBasics.IsFinished;

    public override bool CanDo(Pawn pawn)
    {
        if (!base.CanDo(pawn) || !pawn.Spawned || RadioNet.For(pawn.Map).Stations.Count == 0) return false;
        return RadioAdvice.InTouch(pawn) || RadioAdvice.NearestDesk(pawn, RadioNet.For(pawn.Map)) != null;
    }

    public override Job? TryGiveJob(Pawn pawn)
    {
        bool handHeld = RadioAdvice.InTouch(pawn);
        Thing? desk = handHeld ? null : RadioAdvice.NearestDesk(pawn, RadioNet.For(pawn.Map));
        if (!handHeld && desk == null) return null;
        Pawn? teacher = RadioSchool.FindTeacher(pawn, pawn.Map, out Thing? teacherDesk);
        RadioSchool.SetTeacher(pawn, teacher);
        if (teacher != null)
            RadioLog.Add(RadioLogKind.Lesson, "PhobosRadioLog_Lesson".Translate(teacher.LabelShort.Named("TEACHER"), pawn.LabelShort.Named("CHILD")).Resolve());
        if (teacher is { Spawned: true })
        {
            Job teach = JobMaker.MakeJob(RadioSchoolDefOf.PhobosRadio_TeachByRadio, pawn, teacherDesk == null ? LocalTargetInfo.Invalid : teacherDesk);
            teacher.jobs.StartJob(teach, JobCondition.InterruptForced, null, resumeCurJobAfterwards: true);
        }
        return JobMaker.MakeJob(def.jobDef, desk == null ? LocalTargetInfo.Invalid : desk);
    }
}

[DefOf]
public static class RadioSchoolDefOf
{
    public static JobDef PhobosRadio_TeachByRadio = null!;

    static RadioSchoolDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(RadioSchoolDefOf));
}
