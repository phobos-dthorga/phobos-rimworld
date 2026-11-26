using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A trailer towed on wrecked running gear (Framework 0.322.0, EM42; docs/development/vehicle-road-work.md; the owner's card of 26 Nov
/// 2026: "warn, then allow", as the unwise orders do, EM29). Coupling one is said plainly first and goes ahead. Once one is behind a
/// vehicle, on a map or on the road, the driver is told once, and with its setting on (agent default: on) the vehicle crawls at
/// <see cref="TrailerFaultRule.CrawlShare"/> of its speed and the dragged gear wears further while it moves. The way out is the player's:
/// repair it, carry on slowly, or leave the trailer at a depot. Read on the front vehicle's rare tick; the speed cap reads what it found.
/// </summary>
public static class TrailerFaults
{
    /// <summary>The trailer's first destroyed running part (a part that feeds its speed), or null.</summary>
    public static VehicleComponent? Wrecked(VehiclePawn trailer)
    {
        if (trailer.statHandler?.components is not { } parts) return null;
        foreach (VehicleComponent part in parts)
        {
            if (part.Health <= 0f && part.props.categories?.Contains(VehicleStatDefOf.MoveSpeed) == true) return part;
        }
        return null;
    }

    /// <summary>The first vehicle behind this one, anywhere in its line, on wrecked running gear; or null.</summary>
    public static VehiclePawn? WreckBehind(VehiclePawn front) => CompPhobosHitch.Behind(front).FirstOrDefault(v => Wrecked(v) != null);

    /// <summary>"The trailer is on a wrecked axle: it will be dragged at a crawl." Said before a coupling goes ahead; null for sound gear.</summary>
    public static string? CouplingWarning(VehiclePawn trailer) => Wrecked(trailer) is { } part
        ? "PhobosTrailerFault_CouplingWarning".Translate(trailer.LabelShort.Named("TRAILER"), PartLabel(part).Named("PART"),
            TrailerFaultRule.CrawlShare.ToStringPercent().Named("SHARE")).Resolve()
        : null;

    static string PartLabel(VehicleComponent part) => part.props.label ?? part.props.key;

    /// <summary>
    /// The front vehicle's rare look: finds a wreck behind it, says so once when it first appears, and wears the dragged gear while the
    /// line moves. Only the front of a line looks, so a wreck is counted once.
    /// </summary>
    public static void Look(CompPhobosVehicleSystems front, float hours)
    {
        CompPhobosHitch? hitch = front.Hitch;
        if (hitch == null || Hitches.Towing(front.Vehicle) != null) return;
        VehiclePawn? wreck = hitch.Trailer == null ? null : WreckBehind(front.Vehicle);
        VehiclePawn? before = hitch.DraggingWreck;
        hitch.DraggingWreck = wreck;
        if (wreck == null) return;
        if (wreck != before && hitch.LookedForWreck)
        {
            string text = "PhobosTrailerFault_Wrecked".Translate(front.Vehicle.LabelShort.Named("VEHICLE"), wreck.LabelShort.Named("TRAILER"),
                PartLabel(Wrecked(wreck)!).Named("PART")).Resolve();
            if (front.Vehicle.Faction == Faction.OfPlayer)
            {
                Messages.Message(text, front.Vehicle.GetVehicleCaravan() is { } convoy ? convoy : front.Vehicle, MessageTypeDefOf.NegativeEvent, historical: true);
                VehicleStory.Of(wreck, RoadWorkDefOf.PhobosLog_TrailerWrecked, detail: PartLabel(Wrecked(wreck)!));
            }
            if (Seen.Wants("vehicles.trailer-fault", wreck, "noticed")) Seen.Note("vehicles.trailer-fault", wreck, front.Vehicle.Spawned ? "on a map" : "on the road", "noticed");
        }
        hitch.LookedForWreck = true;
        if (!VehicleSystemsMod.Settings.trailerFaults || !front.Moving || wreck.statHandler?.components is not { } parts) return;
        foreach (VehicleComponent part in parts)
        {
            if (part.props.categories?.Contains(VehicleStatDefOf.MoveSpeed) != true || part.Health <= 0f || part.Health >= part.MaxHealth) continue;
            part.SetHealth(TrailerFaultRule.AfterDrag(part.Health, part.MaxHealth, hours));
        }
        if (Seen.Wants("vehicles.trailer-fault", wreck, "dragged")) Seen.Note("vehicles.trailer-fault", wreck, front.Vehicle.Spawned ? "on a map" : "on the road", "dragged");
    }
}
