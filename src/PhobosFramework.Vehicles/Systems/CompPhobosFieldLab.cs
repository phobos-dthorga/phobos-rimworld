using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

[DefOf]
public static class FieldLabDefOf
{
    public static VehicleStatDef Phobos_FieldLabFactor = null!;
    public static VehicleStatDef Phobos_FieldLabGrade = null!;
    public static ThingDef HiTechResearchBench = null!;

    static FieldLabDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(FieldLabDefOf));
}

/// <summary>A laboratory a vehicle carries. Every figure is authored for play (docs/development/vehicle-seabed-crawler.md, "The research vessel").</summary>
public class CompProperties_PhobosFieldLab : VehicleCompProperties
{
    /// <summary>The role whose seated people do the research. The upgrade that fits the laboratory adds it, so the seats exist only once it is fitted.</summary>
    public string roleKey = "scientist";
    /// <summary>The Intellectual skill a person needs to contribute: 7 is where the game calls someone a weak professional (owner, 10 Nov 2026).</summary>
    public int minimumIntellectual = 7;
    /// <summary>The share of the research rate kept while the vehicle moves, because the crew has more urgent work. Learning keeps its full rate.</summary>
    public float underWayFactor = 0.5f;

    public CompProperties_PhobosFieldLab() => compClass = typeof(CompPhobosFieldLab);
}

/// <summary>
/// Research done aboard: each person in a scientist's seat who is able and on shift adds to the colony's current project, through the same call a research bench
/// makes, and learns as at a bench. Nothing is saved: the summary for the inspect line is rebuilt each time it runs, and the clock starts again after a load.
/// </summary>
public class CompPhobosFieldLab : VehicleComp
{
    const int NeverRun = -1;

    int lastTick = NeverRun;
    string? summary;

    public CompProperties_PhobosFieldLab Props => (CompProperties_PhobosFieldLab)props;

    VehicleRoleHandler? Seats => Vehicle.GetHandler(Props.roleKey);

    /// <summary>Brings the research up to date. Safe to call from more than one place: it counts only the time since it last ran.</summary>
    public void Advance()
    {
        int now = Find.TickManager.TicksGame;
        int elapsed = lastTick == NeverRun ? 0 : now - lastTick;
        lastTick = now;
        if (Vehicle.Faction != Faction.OfPlayer || Seats is not { } seats)
        {
            summary = null;
            return;
        }
        summary = Work(seats, elapsed);
    }

    /// <summary>One stretch of research by everyone seated, and the line that says how it went.</summary>
    string Work(VehicleRoleHandler seats, int ticks)
    {
        List<Pawn> seated = seats.thingOwner.InnerListForReading.ToList();
        if (seated.Count == 0) return "PhobosFieldLab_NobodySeated".Translate().Resolve();
        ResearchProjectDef? project = Find.ResearchManager.GetProject();
        if (project == null) return "PhobosFieldLab_NoProject".Translate().Resolve();
        if (Refusal(project) is { } refused) return refused;

        float labFactor = Vehicle.GetStatValue(FieldLabDefOf.Phobos_FieldLabFactor);
        bool underWay = VehicleMotion.Moving(Vehicle);
        int working = 0;
        string? firstReason = null;
        foreach (Pawn pawn in seated)
        {
            FieldResearcherState state = StateOf(pawn);
            if (state != FieldResearcherState.Working)
            {
                firstReason ??= Reason(pawn, state);
                continue;
            }
            working++;
            if (ticks <= 0) continue;
            // A finished project leaves none current until the player picks the next; the game logs an error for research done with none.
            if (Find.ResearchManager.GetProject() != project) break;
            float rate = FieldResearchRule.Rate(pawn.GetStatValue(StatDefOf.ResearchSpeed), labFactor, underWay, Props.underWayFactor);
            Find.ResearchManager.ResearchPerformed(rate * ticks, pawn);
            pawn.skills.Learn(SkillDefOf.Intellectual, FieldResearchRule.Experience(ticks));
        }
        if (working == 0) return firstReason ?? "PhobosFieldLab_NobodySeated".Translate().Resolve();
        return underWay
            ? "PhobosFieldLab_WorkingUnderWay".Translate(working.Named("COUNT"), project.LabelCap.Named("PROJECT"), Props.underWayFactor.ToStringPercent().Named("SHARE")).Resolve()
            : "PhobosFieldLab_Working".Translate(working.Named("COUNT"), project.LabelCap.Named("PROJECT")).Resolve();
    }

    /// <summary>Why this laboratory cannot take on the project, or null.</summary>
    string? Refusal(ResearchProjectDef project)
    {
        ThingDef? bench = project.requiredResearchBuilding;
        FieldLabVerdict verdict = FieldResearchRule.Lab((int)Vehicle.GetStatValue(FieldLabDefOf.Phobos_FieldLabGrade), bench != null, bench != null && bench == FieldLabDefOf.HiTechResearchBench, !project.requiredResearchFacilities.NullOrEmpty());
        return verdict switch
        {
            FieldLabVerdict.Allowed => null,
            FieldLabVerdict.NoLab => "PhobosFieldLab_NotFitted".Translate().Resolve(),
            FieldLabVerdict.NeedsBetterLab => "PhobosFieldLab_NeedsBetterLab".Translate(project.LabelCap.Named("PROJECT")).Resolve(),
            _ => "PhobosFieldLab_NeedsOtherBench".Translate(project.LabelCap.Named("PROJECT"), (bench?.label ?? "").Named("BENCH")).Resolve(),
        };
    }

    FieldResearcherState StateOf(Pawn pawn)
    {
        SkillRecord? intellectual = pawn.skills?.GetSkill(SkillDefOf.Intellectual);
        bool capable = intellectual != null && !intellectual.TotallyDisabled && !pawn.WorkTypeIsDisabled(WorkTypeDefOf.Research);
        bool fit = !pawn.Dead && !pawn.Downed && pawn.Awake() && !pawn.InMentalState;
        TimeAssignmentDef? shift = pawn.timetable?.CurrentAssignment;
        bool workTime = shift == null || shift == TimeAssignmentDefOf.Work || shift == TimeAssignmentDefOf.Anything;
        return FieldResearchRule.Researcher(capable, intellectual?.Level ?? 0, Props.minimumIntellectual, fit, workTime);
    }

    string Reason(Pawn pawn, FieldResearcherState state) => state switch
    {
        FieldResearcherState.Incapable => "PhobosFieldLab_Incapable".Translate(pawn.LabelShort.Named("PAWN")).Resolve(),
        FieldResearcherState.Unskilled => "PhobosFieldLab_Unskilled".Translate(pawn.LabelShort.Named("PAWN"), Props.minimumIntellectual.Named("SKILL")).Resolve(),
        FieldResearcherState.Unfit => "PhobosFieldLab_Unfit".Translate(pawn.LabelShort.Named("PAWN")).Resolve(),
        _ => "PhobosFieldLab_OffShift".Translate(pawn.LabelShort.Named("PAWN")).Resolve(),
    };

    /// <summary>The laboratory's state, as it was when it last ran; null when it has no seats.</summary>
    public string? StateLine() => summary;

    public override void CompTickRare()
    {
        base.CompTickRare();
        Advance();
    }

    public override string? CompInspectStringExtra() => summary;
}

/// <summary>On every vehicle caravan: the research done aboard any vehicles of it with a laboratory (added by an XML patch). It does nothing for a caravan without one.</summary>
public class WorldObjectComp_PhobosFieldLabCaravan : WorldObjectComp
{
    const int AdvanceInterval = GenTicks.TickRareInterval;

    VehicleCaravan? Caravan => parent as VehicleCaravan;

    static CompPhobosFieldLab? CompOf(VehiclePawn vehicle) => vehicle.GetCachedComp<CompPhobosFieldLab>();

    public override void CompTickInterval(int delta)
    {
        base.CompTickInterval(delta);
        if (Caravan is not { } caravan || !parent.IsHashIntervalTick(AdvanceInterval, delta)) return;
        foreach (VehiclePawn vehicle in caravan.VehiclesListForReading) CompOf(vehicle)?.Advance();
    }

    public override string? CompInspectStringExtra()
    {
        if (Caravan is not { } caravan) return null;
        var lines = caravan.VehiclesListForReading.Select(v => (vehicle: v, line: CompOf(v)?.StateLine())).Where(p => p.line != null)
            .Select(p => "PhobosFieldLab_CaravanLine".Translate(p.vehicle.LabelShort.Named("VEHICLE"), p.line!.Named("STATE")).Resolve()).ToList();
        return lines.Count == 0 ? null : string.Join("\n", lines);
    }
}
