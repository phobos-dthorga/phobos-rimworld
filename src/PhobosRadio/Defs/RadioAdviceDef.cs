using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Phobos.Radio;

/// <summary>
/// One kind of help a skilled colonist can give over the radio (Radio 0.23.0; docs/development/radio-advice-and-relays.md, the owner's cards
/// of 26 Nov 2026): tending, surgery, a vehicle repair, trade talks. Each is a row, so another is data, not code. What the call lifts is D'thorga
/// Framework's skill lift for <see cref="act"/> (Phobos.Framework.Skills.SkillActs, or a name of the Def's own).
/// </summary>
public class RadioAdviceDef : Def
{
    /// <summary>The skill talked through.</summary>
    public SkillDef skill = null!;
    /// <summary>The act lifted: D'thorga Framework's skill act name (Tend, Surgery, Trade, PhobosVehicleRepair).</summary>
    public string act = "";
    /// <summary>The work an adviser must have switched on in the Work tab; with none, the adviser must only be capable of social work.</summary>
    public WorkTypeDef? workType;
    /// <summary>Stats whose factors for <see cref="skill"/> count at the lifted level while a call is live, or at the act on the road.</summary>
    public List<StatDef> liftedStats = new();
    /// <summary>Work speeds slowed while a call is live: each step is spoken.</summary>
    public List<StatDef> slowedStats = new();
    /// <summary>A colonist on a map doing one of these jobs opens a call, when an adviser is free and in touch.</summary>
    public List<JobDef> jobs = new();
    /// <summary>Only bills on a person (an operation), for a job of bills.</summary>
    public bool medicalBillsOnly;
    /// <summary>The trade negotiator, while trade talks are open: the game stops while you trade, so only an adviser on a hand-held already
    /// in touch can take the call, and their time is booked when the talks close.</summary>
    public bool trade;
    /// <summary>The lift is offered before the job starts, to a skill gate (a repair's minimum skill), whenever an adviser could take the call.</summary>
    public bool gate;
    /// <summary>How long an adviser stays on the radio after helping with an act that has no job (a tend or repair on the road, the trade
    /// talks), in ticks.</summary>
    public int bookedTicks = 1250;
    /// <summary>Something whose presence near the carer adds <see cref="aidShare"/> of the gap (the numbered medical chest).</summary>
    public ThingDef? aid;
    public float aidShare;
    /// <summary>A riskier act has its own switch in the mod settings, on until the player switches it off (designs.md).</summary>
    public bool ownSetting;
    [MustTranslate] public string? settingLabel;
    [MustTranslate] public string? settingDescription;
    /// <summary>What the act is called in a sentence: "talking Mako through {0}".</summary>
    [MustTranslate] public string actLabel = "";

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (skill == null) yield return "RadioAdviceDef needs a skill";
        if (act.NullOrEmpty()) yield return "RadioAdviceDef needs an act";
        if (actLabel.NullOrEmpty()) yield return "RadioAdviceDef needs an actLabel";
        if (ownSetting && settingLabel.NullOrEmpty()) yield return "RadioAdviceDef with its own setting needs a settingLabel";
        if (aid != null && aidShare <= 0f) yield return "RadioAdviceDef names an aid with no aidShare";
    }
}

[DefOf]
public static class RadioAdviceDefOf
{
    public static JobDef PhobosRadio_AdviseByRadio = null!;
    public static JobDef PhobosRadio_AdviseByRadioBooked = null!;
    public static ThoughtDef PhobosRadio_TalkedThrough = null!;

    static RadioAdviceDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(RadioAdviceDefOf));
}
