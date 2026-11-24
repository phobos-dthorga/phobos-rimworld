using System.Collections.Generic;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>What a radio built into a pawn that wears nothing (a vehicle, a mechanoid) does on the net (Radio 0.10.0).</summary>
public enum CarriedRadioUse
{
    /// <summary>Not fitted, or switched off: the net leaves it out.</summary>
    Off,
    /// <summary>Its links are worked out and shown, but it asks no channel: a vehicle's radio waiting to be driven from a station.</summary>
    StandingBy,
    /// <summary>It asks a channel of its kind, as a worn hand-held does.</summary>
    InUse,
    /// <summary>
    /// In use, and asking a picture as well (Radio 0.15.0; the autonomous vehicles record, step 6): the vehicle picture kind's channels at
    /// the same station, where the picture's margin on the same path holds (Physics.OtherMode).
    /// </summary>
    InUseWithPicture,
}

/// <summary>
/// A tie-in's say on the radios built into the pawns it knows: whether a vehicle's upgrade puts the radio in place, whether it is
/// under control. The net asks; it never names a vehicle itself (the autonomous vehicles record, step 2).
/// </summary>
public interface ICarriedRadioRule
{
    /// <summary>Its answer for this pawn's radio, or null when it has nothing to say about the pawn.</summary>
    CarriedRadioUse? UseOf(Pawn carrier, CompRadio radio);
}

/// <summary>
/// The seam for radios that are not worn (Radio 0.10.0; the autonomous vehicles record, step 2). A radio comp on a pawn's own def,
/// or put there by a tie-in, counts on the net when that pawn is spawned and of the colony and is not a person (a person's radios are
/// worn). The rules registered here decide what it does; with no rule answering, it is in use, as a worn radio is.
/// <para>Cost: the net asks once per recompute (about once a second) for each such pawn; a pawn with no radio costs one comp lookup.</para>
/// </summary>
public static class CarriedRadios
{
    private static readonly List<ICarriedRadioRule> rules = new();

    /// <summary>Adds a tie-in's rule, once; called from its start-up.</summary>
    public static void Register(ICarriedRadioRule rule)
    {
        if (!rules.Contains(rule)) rules.Add(rule);
    }

    /// <summary>The first rule's answer that has one; with none, in use.</summary>
    public static CarriedRadioUse UseOf(Pawn carrier, CompRadio radio)
    {
        foreach (var rule in rules)
        {
            if (rule.UseOf(carrier, radio) is { } use) return use;
        }
        return CarriedRadioUse.InUse;
    }

    /// <summary>
    /// Fits a radio to a thing for this session only, as a tie-in's test does: the comp is not in the thing's def, so it is gone when
    /// the game is loaded again. Refuses a thing with no comps of its own, or one that has a radio already.
    /// </summary>
    public static CompRadio? FitForSession(ThingWithComps thing, CompProperties_Radio props)
    {
        if (thing.GetComp<CompRadio>() != null) return null;
        if (Phobos.Framework.SessionComps.Add(thing, props) is not CompRadio comp) return null;
        if (thing.Spawned) RadioNet.For(thing.Map)?.MarkDirty();
        return comp;
    }
}
