using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Marks a building as service yard equipment (Framework 0.210.0; docs/development/vehicle-service-yard.md): standing in a yard or touching
/// its edge, it adds this many cells to the yard's allowance. A yard needs at least one piece marked <see cref="required"/> (the dyno bay) to
/// be a yard at all. XML, so another mod's building can be equipment too.
/// </summary>
public class PhobosYardEquipmentExtension : DefModExtension
{
    /// <summary>The cells it adds to its yard's allowance (authored [A]: a dyno bay 80, a fuel pump 40, an inspection pit 40).</summary>
    public int cells = 40;
    /// <summary>A yard needs one of these to be a yard.</summary>
    public bool required;
}

/// <summary>
/// A service yard (Framework 0.210.0; docs/development/vehicle-service-yard.md; owner, 20 Nov 2026: "a larger zone on its own and meant for
/// servicing and parking in one", "with a specific, or at least, a restrained size"): ground the player marks beside a dyno bay where
/// vehicles are worked faster, take turns on the dyno where they stand, are fuelled by its pumps and wait for their next job. Its size is
/// held to what its equipment allows; it keeps every cell when equipment is lost, gives nothing while too big, and says so. What it knows
/// of itself is worked out by <see cref="MapComponent_PhobosServiceYards"/> and kept here for a moment; nothing of that is saved.
/// </summary>
public class Zone_PhobosServiceYard : Zone
{
    /// <summary>One colour for every yard, at the game's own zone opacity (authored: a workshop's safety yellow).</summary>
    static readonly Color YardColour = new(0.85f, 0.68f, 0.15f, 0.09f);

    static Designator_ZoneAdd_PhobosServiceYard? expand;

    /// <summary>What the yard's last look found. Derived, never saved.</summary>
    internal YardState? State;

    public Zone_PhobosServiceYard()
    {
    }

    public Zone_PhobosServiceYard(ZoneManager zoneManager)
        : base("PhobosServiceYard_Label".Translate(), zoneManager)
    {
    }

    /// <summary>The yard gives its services: its dyno bay stands beside it and it is no bigger than its equipment allows.</summary>
    public bool Works => State is { Works: true };

    public override bool IsMultiselectable => true;

    protected override Color NextZoneColor => YardColour;

    public override void AddCell(IntVec3 c)
    {
        base.AddCell(c);
        ServiceYards.Dirty(Map);
    }

    public override void RemoveCell(IntVec3 c)
    {
        base.RemoveCell(c);
        ServiceYards.Dirty(Map);
    }

    /// <summary>The same tool that marks a yard grows the one selected, within its allowance.</summary>
    public override IEnumerable<Gizmo> GetZoneAddGizmos()
    {
        yield return expand ??= new Designator_ZoneAdd_PhobosServiceYard { defaultLabel = "PhobosServiceYard_Expand".Translate() };
    }

    public override string GetInspectString()
    {
        ServiceYards.For(Map)?.EnsureFresh();
        YardState? state = State;
        if (state == null) return "";
        var text = new StringBuilder();
        text.Append("PhobosServiceYard_InspectCells".Translate(state.Cells.Named("CELLS"), state.Allowance.Named("ALLOWANCE")).Resolve());
        text.AppendLine();
        text.Append(state.Equipment.Count == 0 ? "PhobosServiceYard_InspectNoEquipment".Translate().Resolve()
            : "PhobosServiceYard_InspectEquipment".Translate(state.Equipment.Select(e => e.LabelShort).ToCommaList(useAnd: true).Named("EQUIPMENT")).Resolve());
        text.AppendLine();
        if (!state.HasDyno) text.Append("PhobosServiceYard_InspectNoDyno".Translate().Resolve());
        else if (state.Overflow > 0) text.Append("PhobosServiceYard_InspectTooBig".Translate(state.Overflow.Named("CELLS")).Resolve());
        else
        {
            text.Append("PhobosServiceYard_InspectVehicles".Translate(state.Vehicles.Count.Named("COUNT"), state.OnDyno.Named("DYNO"),
                state.Waiting.Named("WAITING")).Resolve());
        }
        return text.ToString();
    }
}

/// <summary>
/// Marks a service yard on the ground, from the motor pool tab. Cells past the yard's allowance are refused, and the player told why, with
/// the yard's cells and its allowance: it never draws past it.
/// </summary>
public class Designator_ZoneAdd_PhobosServiceYard : Designator_ZoneAdd
{
    protected override string NewZoneLabel => "PhobosServiceYard_Label".Translate();

    public Designator_ZoneAdd_PhobosServiceYard()
    {
        zoneTypeToPlace = typeof(Zone_PhobosServiceYard);
        defaultLabel = "PhobosServiceYard_Label".Translate();
        defaultDesc = "PhobosServiceYard_Desc".Translate();
        icon = SystemIcons.For("Phobos/VehicleSystems/BayTrips/ServiceYard");
    }

    protected override Zone MakeNewZone() => new Zone_PhobosServiceYard(Find.CurrentMap.zoneManager);

    public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
    {
        List<IntVec3> all = cells.ToList();
        // One click on a yard selects it, as the game's own tool does: nothing is added.
        if (all.Count == 1 && Map.zoneManager.ZoneAt(all[0]) != null)
        {
            base.DesignateMultiCell(all);
            return;
        }
        List<IntVec3> fresh = all.Where(c => Map.zoneManager.ZoneAt(c) == null).ToList();
        if (fresh.Count == 0)
        {
            base.DesignateMultiCell(all);
            return;
        }
        Zone_PhobosServiceYard? target = Find.Selector.SelectedZone as Zone_PhobosServiceYard;
        if (target == null)
        {
            // The game grows the one yard the drag touches; a drag over two, or none, makes a new one.
            List<Zone_PhobosServiceYard> touched = all.Select(c => Map.zoneManager.ZoneAt(c)).OfType<Zone_PhobosServiceYard>().Distinct().ToList();
            if (touched.Count == 1) target = touched[0];
        }
        List<IntVec3> kept = ServiceYards.Trim(Map, target, fresh, out string? refusal);
        if (refusal != null) Messages.Message(refusal, MessageTypeDefOf.RejectInput, historical: false);
        if (kept.Count == 0) return;
        // Cells already in a yard are passed on so the game still knows which yard the drag grows.
        base.DesignateMultiCell(all.Where(c => Map.zoneManager.ZoneAt(c) != null).Concat(kept));
        string marked = refusal != null ? "capped" : "within";
        if (Seen.Wants("vehicles.service-yard.marked", null, marked)) Seen.Note("vehicles.service-yard.marked", null, kept.Count + " cells", marked);
    }
}
