using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace Phobos.Framework.Skills;

// Skill lifts (Framework 0.330.0; docs/development/radio-advice-and-relays.md, the owner's cards of 26 Nov 2026): a person counted as more
// skilled than they are for one act, because someone helps them from elsewhere. The Radio Framework's advice by radio is the first source;
// a source is anything that registers here. D'thorga Framework only asks: it never decides who helps or how much.

/// <summary>The acts a lift can be for, named by string so a source matches them by name. A source may name its own.</summary>
public static class SkillActs
{
    public const string Tend = "Tend";
    public const string Surgery = "Surgery";
    public const string Trade = "Trade";
    /// <summary>A repair of one of our vehicles with supplies (D'thorga Framework's vehicle systems): a minimum skill gates it.</summary>
    public const string VehicleRepair = "PhobosVehicleRepair";
}

/// <summary>
/// A skill counted higher for one act. <paramref name="Level"/> is the level it counts as; <paramref name="SpeedFactor"/> what work speed is
/// multiplied by while it lasts; <paramref name="Reason"/> one line of player text naming who helps; <paramref name="Live"/> whether the help
/// is happening now (a call in progress), not only on offer; <paramref name="Token"/> the source's own record of it.
/// </summary>
public sealed record SkillLift(int Level, float SpeedFactor, string Reason, ISkillLiftSource Source, bool Live, object? Token = null);

/// <summary>Something that can lift a person's skill for an act: the Radio Framework's advice by radio, when it is installed.</summary>
public interface ISkillLiftSource
{
    /// <summary>
    /// The lift this person has now for the act, or null. <b>Changes nothing</b>: stat reads (the info card's too) and work givers ask it,
    /// so a source keeps any search it makes and decides nothing here.
    /// </summary>
    SkillLift? Lift(Pawn pawn, SkillDef skill, string act);

    /// <summary>The act was done with the lift, outside any call the source keeps itself (a convoy's tend or repair, which has no job): the
    /// source books its helper's time and says so.</summary>
    void Used(Pawn pawn, SkillDef skill, string act, SkillLift lift);
}

/// <summary>
/// Where skill lifts are asked for. Stat reads go through <see cref="StatPart_PhobosSkillLift"/>, which a source adds to the stats it lifts
/// with <see cref="LiftStat"/>; a gate (a minimum skill) asks <see cref="Level"/>. An act with no job of its own (a convoy's tend) is wrapped
/// in <see cref="Begin"/> and <see cref="End"/>, so a lift read inside it is booked once it is done.
/// <para>Cost: with no source registered, one count check per read of a lifted stat; with one, what the source's <c>Lift</c> costs.</para>
/// </summary>
public static class SkillLifts
{
    static readonly List<ISkillLiftSource> sources = new();

    public static void Register(ISkillLiftSource source)
    {
        if (!sources.Contains(source)) sources.Add(source);
    }

    /// <summary>Whether any source is registered.</summary>
    public static bool Any => sources.Count > 0;

    /// <summary>The person's own level, 0 where the skill is missing or disabled.</summary>
    public static int OwnLevel(Pawn pawn, SkillDef skill) => pawn.skills?.GetSkill(skill) is { TotallyDisabled: false } record ? record.Level : 0;

    /// <summary>The best lift above the person's own level for the act, or null. Inside an act's wrapper it marks that lift as used.</summary>
    public static SkillLift? Best(Pawn pawn, SkillDef skill, string act)
    {
        if (sources.Count == 0 || pawn.skills == null) return null;
        int own = OwnLevel(pawn, skill);
        SkillLift? best = null;
        foreach (ISkillLiftSource source in sources)
        {
            if (source.Lift(pawn, skill, act) is { } lift && lift.Level > own && (best == null || lift.Level > best.Level)) best = lift;
        }
        if (best != null && current is { } scope && scope.Pawn == pawn && scope.Act == act)
        {
            scope.Skill = skill;
            scope.Used = best;
        }
        return best;
    }

    /// <summary>The level the person counts as for the act: their own, or the best lift above it.</summary>
    public static int Level(Pawn pawn, SkillDef skill, string act) => Best(pawn, skill, act)?.Level ?? OwnLevel(pawn, skill);

    /// <summary>Tells the lift's source the act was done with it (see <see cref="ISkillLiftSource.Used"/>).</summary>
    public static void Used(Pawn pawn, SkillDef skill, string act, SkillLift lift) => lift.Source.Used(pawn, skill, act, lift);

    // ---- An act with no job of its own ----

    sealed class Scope
    {
        public Pawn Pawn = null!;
        public string Act = "";
        public SkillDef? Skill;
        public SkillLift? Used;
    }

    [ThreadStatic] static Scope? current;

    /// <summary>The person and act being done now inside a wrapper, or null.</summary>
    public static (Pawn pawn, string act)? Current => current == null ? null : (current.Pawn, current.Act);

    /// <summary>Starts an act with no job of its own. Always paired with <see cref="End"/>, in a finally or a Harmony finalizer.</summary>
    public static void Begin(Pawn pawn, string act) => current = new Scope { Pawn = pawn, Act = act };

    /// <summary>Ends the act; a lift read inside it is booked with its source.</summary>
    public static void End()
    {
        Scope? scope = current;
        current = null;
        if (scope?.Used is not { } lift || scope.Skill == null) return;
        try
        {
            lift.Source.Used(scope.Pawn, scope.Skill, scope.Act, lift);
        }
        catch (Exception error)
        {
            Log.ErrorOnce("[D'thorga Framework] A skill lift's source failed to book its help: " + error, 0x5C1F7A);
        }
    }

    // ---- Stats ----

    /// <summary>
    /// Adds the lift to a stat once, at start-up: <paramref name="slows"/> false lifts the stat's factors for <paramref name="skill"/> to the
    /// lifted level; true multiplies it by the lift's speed factor while the help is live (a call takes time to talk through).
    /// </summary>
    public static void LiftStat(StatDef stat, SkillDef skill, string act, bool slows)
    {
        stat.parts ??= new List<StatPart>();
        foreach (StatPart part in stat.parts)
        {
            if (part is StatPart_PhobosSkillLift held && held.skill == skill && held.act == act && held.slows == slows) return;
        }
        stat.parts.Add(new StatPart_PhobosSkillLift { parentStat = stat, skill = skill, act = act, slows = slows });
    }

    // ---- A stat's skill factor at a level ----

    static readonly FieldInfo? baseValueField = typeof(SkillNeed_BaseBonus).GetField("baseValue", BindingFlags.Instance | BindingFlags.NonPublic);
    static readonly FieldInfo? bonusField = typeof(SkillNeed_BaseBonus).GetField("bonusPerLevel", BindingFlags.Instance | BindingFlags.NonPublic);

    /// <summary>
    /// What a stat's factors for one skill are multiplied by when the skill counts as <paramref name="lifted"/> instead of <paramref name="own"/>.
    /// The game's two kinds of factor are read (a base and a bonus a level; a value for each level); another mod's kind counts as unchanged.
    /// </summary>
    public static float FactorRatio(StatDef stat, SkillDef skill, int own, int lifted)
    {
        if (stat.skillNeedFactors == null || own == lifted) return 1f;
        float ratio = 1f;
        foreach (SkillNeed need in stat.skillNeedFactors)
        {
            if (need.skill != skill) continue;
            float? at = ValueAt(need, own), to = ValueAt(need, lifted);
            if (at is > 0f && to is { } value) ratio *= value / at.Value;
        }
        return ratio;
    }

    static float? ValueAt(SkillNeed need, int level)
    {
        switch (need)
        {
            case SkillNeed_Direct direct:
                if (direct.valuesPerLevel.Count == 0) return null;
                return direct.valuesPerLevel[Math.Min(level, direct.valuesPerLevel.Count - 1)];
            case SkillNeed_BaseBonus bonus when baseValueField != null && bonusField != null:
                return baseValueField.GetValue(bonus) is float baseValue && bonusField.GetValue(bonus) is float perLevel ? baseValue + perLevel * level : null;
            default:
                return null;
        }
    }
}

/// <summary>
/// A skill lift in a stat (Framework 0.330.0): added in code by <see cref="SkillLifts.LiftStat"/>, never in XML. A lift either raises the
/// stat's factors for its skill to the lifted level, or, for a work speed, slows it while help is talked through.
/// </summary>
public class StatPart_PhobosSkillLift : StatPart
{
    public SkillDef skill = null!;
    public string act = "";
    public bool slows;

    SkillLift? LiftFor(StatRequest req)
    {
        if (!SkillLifts.Any || req.Thing is not Pawn pawn || pawn.skills == null || pawn.Faction == null || !pawn.Faction.IsPlayer) return null;
        SkillLift? lift = SkillLifts.Best(pawn, skill, act);
        return lift == null || slows && !lift.Live ? null : lift;
    }

    public override void TransformValue(StatRequest req, ref float val)
    {
        if (LiftFor(req) is not { } lift) return;
        val *= slows ? lift.SpeedFactor : SkillLifts.FactorRatio(parentStat, skill, SkillLifts.OwnLevel((Pawn)req.Thing, skill), lift.Level);
    }

    public override string? ExplanationPart(StatRequest req)
    {
        if (LiftFor(req) is not { } lift) return null;
        return slows
            ? "PhobosSkillLift_ExplainSlow".Translate(lift.Reason.Named("REASON"), lift.SpeedFactor.ToStringPercent().Named("FACTOR")).Resolve()
            : "PhobosSkillLift_ExplainLift".Translate(lift.Reason.Named("REASON"), skill.LabelCap.Named("SKILL"), lift.Level.Named("LEVEL"),
                SkillLifts.OwnLevel((Pawn)req.Thing, skill).Named("OWN")).Resolve();
    }
}
