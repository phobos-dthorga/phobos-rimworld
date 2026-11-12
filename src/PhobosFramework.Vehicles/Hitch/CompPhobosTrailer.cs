using System.Collections.Generic;
using Phobos.Framework.Interface;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A trailer (Framework 0.101.0, the hitch's slice 2; docs/development/vehicle-hitch-design.md): a vehicle with no engine and no seats that a
/// vehicle with a hitch can tow. Its two lengths set how it follows: from the hitch to its axle, and from the axle to the middle of its
/// picture. Named in a trailer's own Def, so other modders can make trailers in XML.
/// </summary>
public sealed class CompProperties_PhobosTrailer : CompProperties
{
    /// <summary>From the hitch to the trailer's axle, in cells (the drawbar and the front of the deck).</summary>
    public float hitchToAxle = 3.2f;
    /// <summary>From the axle forward to the middle of the trailer's picture, in cells.</summary>
    public float axleToCentre = 0.2f;
    /// <summary>The most the trailer may stand turned from the vehicle towing it, in degrees (authored, not a force).</summary>
    public float jackKnifeDegrees = 70f;
    /// <summary>The trailer's part whose loss leaves nothing to hitch to; empty for none.</summary>
    public string drawbarPart = "Drawbar";

    public CompProperties_PhobosTrailer() => compClass = typeof(CompPhobosTrailer);

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (string error in base.ConfigErrors(parentDef)) yield return error;
        if (hitchToAxle <= 0f) yield return "hitchToAxle must be above 0";
        if (jackKnifeDegrees <= 0f || jackKnifeDegrees > 180f) yield return "jackKnifeDegrees must be between 0 and 180";
    }
}

/// <summary>The trailer's side of a hitch: what it says, and the unhitch button. The vehicle towing it holds the hitch (CompPhobosHitch).</summary>
public sealed class CompPhobosTrailer : VehicleComp
{
    public CompProperties_PhobosTrailer Props => (CompProperties_PhobosTrailer)props;

    /// <summary>The vehicle towing this trailer now, or null.</summary>
    public CompPhobosHitch? TowedBy => Hitches.Towing(Vehicle);

    /// <summary>Whether the trailer's drawbar is gone, so there is nothing to hitch to.</summary>
    public bool DrawbarBroken =>
        !Props.drawbarPart.NullOrEmpty() && Vehicle.statHandler?.GetComponent(Props.drawbarPart) is { } part && part.HealthPercent <= 0f;

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (Vehicle.Faction != Faction.OfPlayer || TowedBy is not { } hitch) yield break;
        yield return hitch.UnhitchGizmo();
    }

    public override string? CompInspectStringExtra()
    {
        if (UiGuard.Resting(GuardKey)) return UiGuard.FallbackText;
        try
        {
            if (TowedBy is { } hitch) return "PhobosHitch_InspectTowedBy".Translate(hitch.Vehicle.LabelShort.Named("TRACTOR")).Resolve();
            if (DrawbarBroken) return "PhobosHitch_InspectDrawbarBroken".Translate().Resolve();
            return null;
        }
        catch (System.Exception e) { UiGuard.Report(GuardKey, e); return null; }
    }

    const string GuardKey = "A trailer's line";
}
