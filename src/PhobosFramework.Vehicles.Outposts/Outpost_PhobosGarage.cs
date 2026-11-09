using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Outposts;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems.Garage;

/// <summary>The garage's figures, from its Def. All of them are authored balance.</summary>
public class PhobosGarageExtension : OutpostExtension
{
    public int baysPerPerson = 3;
    public int maxBays = 12;
    public float repairPointsPerSkillLevel = 40f;
    public float repairPointsPerSteel = 10f;
    public float repairPointsPerComponent = 400f;

    /// <summary>How long vehicles left behind by a garage that has closed stay where they are.</summary>
    public int releasedVehicleDays = 30;
}

/// <summary>
/// A staffed outpost that holds vehicles with no time limit and slowly mends them (docs/development/vehicle-garage-design.md). The outposts
/// code refuses vehicles, so the garage keeps its own saved list and never offers them to AddPawn. Its vehicles are not ticked, so
/// nothing about them changes while parked and they cost nothing per tick. All the rules are <see cref="GarageService"/>'s.
/// </summary>
public class Outpost_PhobosGarage : Outpost, IPhobosVehicleHolder
{
    static readonly PhobosGarageExtension Defaults = new();

    ThingOwner<Thing> parked;

    public Outpost_PhobosGarage() => parked = new ThingOwner<Thing>(this);

    public PhobosGarageExtension Figures => Ext as PhobosGarageExtension ?? Defaults;
    public ThingOwner Parked => parked;
    public IEnumerable<VehiclePawn> HeldVehicles => parked.InnerListForReading.OfType<VehiclePawn>();
    public int ParkedCount => HeldVehicles.Count();

    public override void Produce() => GarageService.RepairCycle(this);

    public override string ProductionString() => GarageService.StatusLine(this);

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref parked, "PhobosGarage_Parked", this);
        parked ??= new ThingOwner<Thing>(this);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) MarkFoundingPaid();
    }

    /// <summary>
    /// A garage is paid for once, by <see cref="GarageService.Found"/>. The outposts code keeps its own private flag and, while that is
    /// unset, takes the founding cost out of the outpost's stores when a lone caravan joins: here, the repair stock. Read from Vanilla
    /// Expanded Outposts (Outpost.AddPawn), 9 Nov 2026; the field has no other way in, so a reflected write. Also set on load, for
    /// garages founded before this.
    /// </summary>
    internal void MarkFoundingPaid() => typeof(Outpost).GetField("costPaid", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(this, true);

    public override void PostRemove()
    {
        base.PostRemove();
        GarageService.ReleaseAll(this);
    }

    public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan)
    {
        // The outposts code's "add a person" lists a convoy's vehicles too, and fails on them: a convoy parks here instead, and its crew
        // can join once they are on foot.
        bool hasVehicles = caravan.PawnsListForReading.Any(p => p is VehiclePawn);
        foreach (Gizmo gizmo in base.GetCaravanGizmos(caravan))
        {
            if (hasVehicles && gizmo is Command command && command.icon == GarageIcons.Add) continue;
            yield return gizmo;
        }
        if (caravan is VehicleCaravan convoy) yield return GarageCommands.Park(this, convoy);
        if (parked.Count > 0) yield return GarageCommands.TakeOut(this, caravan);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
        {
            if (gizmo is Command command && command.icon == GarageIcons.Pack && parked.Count > 0)
            {
                command.Disable("PhobosGarage_PackRefused".Translate());
            }
            yield return gizmo;
        }
    }
}
