using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A condition one part of a vehicle can have, as a body part has an injury: a severity from 0 to 1 with stages, each stage with the
/// same effects a system's stage has (a leak drains a quantity, a fault slows the vehicle or refuses a start). Hits give it (a part-hit
/// rule with a condition), causes give it (Effect_AddCondition in a system's stage: boiling over warps the head), and a repair, fading
/// or a service clears it. It belongs to Vehicle Framework's part, whose health stays the one health figure; the condition only adds
/// what the health cannot say (docs/development/vehicle-health-design.md, slice 2).
/// </summary>
public class PhobosVehicleConditionDef : Def
{
    /// <summary>The stages over the severity, from 0 to 1, in order; the same stage type as a system's quantities.</summary>
    public List<StageSpec> stages = new();
    /// <summary>Severity lost per hour by itself (0: it never fades).</summary>
    public float fadePerHour;
    /// <summary>A repair that brings the part to full health clears it (Vehicle Framework's free repair). Off for a condition only a
    /// service with supplies should clear.</summary>
    public bool clearsOnRepair = true;
    /// <summary>From this severity up, a repair no longer clears it: it needs <see cref="service"/>. Not set: every severity is alike.</summary>
    public float needsServiceFrom = float.NaN;
    /// <summary>The job that puts it right at or above <see cref="needsServiceFrom"/>: its label, supplies and work.</summary>
    public ServiceSpec? service;
    /// <summary>A colonist can patch it in the field: it halves, once, and only a proper repair or service ends it.</summary>
    public bool fieldFixable;

    public bool NeedsService(float severity) => !float.IsNaN(needsServiceFrom) && severity >= needsServiceFrom;

    public bool RepairClears(float severity) => clearsOnRepair && !NeedsService(severity);

    [Unsaved] QuantitySpec? severity;

    /// <summary>The severity as a quantity, so the stage rules, the tabs' formatting and the stage colours work on it unchanged.</summary>
    public QuantitySpec Severity => severity ??= new QuantitySpec
    {
        name = "severity",
        label = label,
        unit = QuantityUnit.Fraction,
        min = 0f,
        max = 1f,
        startsAt = QuantityStart.Empty,
        stages = stages,
    };

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (stages.Count == 0) yield return "a condition needs at least one stage";
        var from = new List<double>();
        var to = new List<double>();
        foreach (StageSpec stage in stages)
        {
            from.Add(stage.from);
            to.Add(stage.to);
            if ((!float.IsNaN(stage.from) && (stage.from < 0f || stage.from > 1f)) || (!float.IsNaN(stage.to) && (stage.to < 0f || stage.to > 1f)))
            {
                yield return "the stage " + stage.label + " lies outside a severity of 0 to 1";
            }
        }
        if (!StageRule.Ordered(from, to)) yield return "stages overlap or are out of order";
        if (fadePerHour < 0f) yield return "fadePerHour is below 0";
        if (!float.IsNaN(needsServiceFrom) && service == null) yield return "needsServiceFrom is set, with no service to put it right";
    }
}
