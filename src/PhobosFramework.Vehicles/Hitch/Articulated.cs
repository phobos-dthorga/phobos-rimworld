using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>A vehicle built as the front half of an articulated pair: the Def of its rear half, set down and coupled behind it.</summary>
public sealed class CompProperties_PhobosArticulated : CompProperties
{
    /// <summary>The rear half's vehicle Def: a trailer of ours, towed by a permanent coupling this vehicle's hitch has.</summary>
    public VehicleDef? rearHalf;

    public CompProperties_PhobosArticulated() => compClass = typeof(CompPhobosArticulated);

    public override System.Collections.Generic.IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (string error in base.ConfigErrors(parentDef)) yield return error;
        if (rearHalf == null) yield return "an articulated vehicle names no rear half";
    }
}

/// <summary>
/// An articulated vehicle (Framework 0.195.0; docs/development/farming-batch-two.md, the Thane T400): two vehicles joined at a pivot on
/// our hitch, the front driving and the rear following within its swing limit, its weight on driven wheels (drivenAxle). Built from one
/// blueprint, the front half sets its rear half down behind it and couples it, at its next tick (never from its spawning: AGENTS.md),
/// once; the pair is then held together by a permanent coupling with no Unhitch button. If there is no room behind, it says so and tries
/// again shortly. Saved: only whether the pair has been put together, so a loaded save never makes a second rear half.
/// </summary>
public sealed class CompPhobosArticulated : VehicleComp
{
    /// <summary>How long to wait before trying again when there was no room behind.</summary>
    const int RetryTicks = 250;

    bool assembled;
    int nextTry;

    public CompProperties_PhobosArticulated Props => (CompProperties_PhobosArticulated)props;

    public override bool TickByRequest => true;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (!assembled) Vehicle.RequestTickStart(this);
    }

    public override void CompTick()
    {
        base.CompTick();
        if (assembled || !Vehicle.Spawned)
        {
            Vehicle.RequestTickStop(this);
            return;
        }
        int now = Find.TickManager.TicksGame;
        if (now < nextTry) return;
        nextTry = now + RetryTicks;
        if (Assemble()) Vehicle.RequestTickStop(this);
    }

    /// <summary>Makes the rear half, sets it down behind and couples it. False, with the player told why, when there is no room.</summary>
    bool Assemble()
    {
        if (Props.rearHalf == null || Vehicle.GetComp<CompPhobosHitch>() is not { } hitch) return assembled = true;
        if (hitch.Trailer != null)
        {
            // A save from before this comp, or a pair put together by hand: nothing to make.
            assembled = true;
            return true;
        }
        Map map = Vehicle.Map;
        IntVec3 behind = Vehicle.Position - Vehicle.Rotation.FacingCell * ((Vehicle.def.Size.z + Props.rearHalf.Size.z) / 2);
        if (!behind.InBounds(map) || !GenGrid.Standable(behind, map))
        {
            NoRoom();
            return false;
        }
        VehiclePawn rear = VehicleSpawner.GenerateVehicle(Props.rearHalf, Vehicle.Faction);
        GenSpawn.Spawn(rear, behind, map, Vehicle.Rotation);
        if (!hitch.Couple(rear))
        {
            rear.Destroy(DestroyMode.Vanish);
            NoRoom();
            return false;
        }
        assembled = true;
        VehicleSystemsMod.HitchLog.Debug(Vehicle.LabelShort + " (" + Vehicle.ThingID + ") put together with its rear half " + rear.ThingID);
        if (Seen.Wants("vehicles.articulated.assembled", Vehicle)) Seen.Note("vehicles.articulated.assembled", Vehicle, Vehicle.def.defName);
        return true;
    }

    void NoRoom()
    {
        if (Vehicle.Faction != Faction.OfPlayer) return;
        Messages.Message("PhobosArticulated_NoRoom".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), Vehicle, MessageTypeDefOf.RejectInput,
            historical: false);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref assembled, "PhobosArticulated_Assembled");
    }
}
