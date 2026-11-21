using System.Linq;
using DubsBadHygiene;
using Phobos.Framework.Hygiene;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems.Hygiene;

/// <summary>The crew water tank's side of a vehicle (Framework 0.232.0): the hidden tower it lays beneath itself while parked.</summary>
public class CompProperties_PhobosCrewWater : CompProperties
{
    public ThingDef? towerDef;

    public CompProperties_PhobosCrewWater() => compClass = typeof(CompPhobosCrewWater);
}

/// <summary>
/// The crew water tank (Framework 0.232.0; docs/development/vehicle-accessories.md, Y2 and "Built: the crew water tank"). A fitting in the
/// vehicle's water tank bay holds water in litres, a store that runs down as it is used and is refilled by the player's people, never by
/// itself (the accessory rule). On the road it is the caravan's water (riders drink and wash from it); parked with its engine off, it stands in
/// a hidden Dubs water tower beneath the vehicle, so plumbing or a water bowser beside it fills it, and the basin and shower its camp sets out
/// draw from it through the pipes the vehicle lays beneath the camp. Driving off takes the tower's water back aboard. With Dubs Bad Hygiene
/// only: without it water has no meaning, and the comp is never added.
/// </summary>
public sealed class CompPhobosCrewWater : ThingComp, IVehicleCarriedLoad, IVehicleWaterStore
{
    /// <summary>The ability the crew water tank fitting grants.</summary>
    public const string Ability = "CrewWater";

    float litres;
    int quality;
    float atLay;
    Thing? tower;
    bool saidLow;

    public CompProperties_PhobosCrewWater Props => (CompProperties_PhobosCrewWater)props;

    VehiclePawn Vehicle => (VehiclePawn)parent;

    CompWaterStorage? Tower => tower is { Spawned: true } ? tower.TryGetComp<CompWaterStorage>() : null;

    /// <summary>Dubs' own "unlimited pumping": every tower is kept full by the player's choice, so the stand-in gives back only what it was given.</summary>
    static bool UnlimitedPumping => ModOption.WaterPumpCapacity.Val == 0f;

    /// <summary>Whether the tank is fitted now.</summary>
    public bool Fitted => parent.GetComp<CompPhobosVehicleSystems>() is { Usable: true } systems && systems.Fittings.Grants(Ability);

    public float Capacity => Fitted ? PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>().crewTankLitres : 0f;

    /// <summary>The water held now, in the hidden tower while parked or aboard otherwise.</summary>
    public float Litres => Tower is { } water ? water.WaterStorage : litres;

    /// <summary>Water weighs a kilogram a litre, carried as cargo is (the accessory rule: its weight takes cargo room only).</summary>
    public float CarriedKilograms => Litres;

    /// <summary>Takes up to this much, from the tower while parked or from aboard; second, the worst quality it came from.</summary>
    public float Take(float most, out int from)
    {
        from = quality;
        if (most <= 0f) return 0f;
        float got;
        if (Tower is { } water)
        {
            got = Mathf.Min(most, water.WaterStorage);
            water.WaterStorage -= got;
            from = (int)water.WaterQuality;
        }
        else
        {
            got = Mathf.Min(most, litres);
            litres -= got;
        }
        if (got > 0f) Changed();
        return got;
    }

    /// <summary>Whether it is to be laid as a tower beneath the vehicle now: fitted, and the vehicle parked with its pipes beneath it.</summary>
    public void Settle(bool parked)
    {
        if (PlumbingFeed.LiteMode || !Vehicle.Spawned) return;
        bool lay = parked && Fitted;
        if (lay && tower is not { Spawned: true }) Lay();
        else if (!lay && tower is { Spawned: true }) TakeUp();
    }

    void Lay()
    {
        if (Props.towerDef == null) return;
        tower = GenSpawn.Spawn(ThingMaker.MakeThing(Props.towerDef), Vehicle.Position, Vehicle.Map, WipeMode.Vanish);
        tower.SetFaction(Vehicle.Faction);
        if (Tower is not { } water) return;
        water.WaterStorage = Mathf.Min(litres, Mathf.Min(water.Props.WaterStorageCap, Capacity));
        water.WaterQuality = (ContaminationLevel)quality;
        litres -= water.WaterStorage;
        atLay = water.WaterStorage;
        if (Seen.Wants("vehicles.crew-water-laid", Vehicle)) Seen.Note("vehicles.crew-water-laid", Vehicle, water.WaterStorage.ToString("0") + " L");
    }

    void TakeUp()
    {
        if (Tower is { } water)
        {
            // Under Dubs' unlimited pumping only what it held when laid comes back; otherwise all it holds, up to the tank's size.
            float back = UnlimitedPumping ? atLay : water.WaterStorage;
            litres = Mathf.Min(Capacity, litres + back);
            if ((int)water.WaterQuality > quality || back > 0f) quality = (int)water.WaterQuality;
            water.WaterStorage = 0f;
        }
        tower?.Destroy(DestroyMode.Vanish);
        tower = null;
        Changed();
    }

    void Changed()
    {
        CarriedLoads.Changed(Vehicle);
        if (Capacity <= 0f) return;
        bool low = Litres < Capacity * PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>().crewTankLowShare;
        if (!low)
        {
            saidLow = false;
            return;
        }
        if (saidLow || Vehicle.Faction != Faction.OfPlayer) return;
        saidLow = true;
        // Pointed at the vehicle on a map, or at its caravan on the road.
        Thing self = parent;
        LookTargets at = self.Spawned ? new LookTargets(self) : ((Pawn)self).GetCaravan() is { } caravan ? new LookTargets(caravan) : LookTargets.Invalid;
        Messages.Message("PhobosCrewWater_Low".Translate(parent.LabelShort.Named("VEHICLE")).Resolve(), at, MessageTypeDefOf.CautionInput, historical: false);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        if (tower is { Spawned: true })
        {
            if (mode == DestroyMode.KillFinalize)
            {
                tower.Destroy(DestroyMode.Vanish);
                tower = null;
            }
            else
            {
                TakeUp();
            }
        }
        base.PostDeSpawn(map, mode);
    }

    public override string? CompInspectStringExtra()
    {
        if (PlumbingFeed.LiteMode || Capacity <= 0f) return null;
        return "PhobosCrewWater_Inspect".Translate(Litres.ToString("0").Named("LITRES"), Capacity.ToString("0").Named("CAPACITY")).Resolve();
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref litres, "PhobosCrewWater_Litres");
        Scribe_Values.Look(ref quality, "PhobosCrewWater_Quality");
        Scribe_Values.Look(ref atLay, "PhobosCrewWater_AtLay");
        Scribe_References.Look(ref tower, "PhobosCrewWater_Tower");
    }
}

/// <summary>Every land vehicle that can camp, trailers apart, gets the crew water tank's comp, with Dubs Bad Hygiene only (Framework 0.232.0).</summary>
[StaticConstructorOnStartup]
public static class CrewWaterSetup
{
    static CrewWaterSetup()
    {
        if (PlumbingFeed.LiteMode) return;
        ThingDef? towerDef = DefDatabase<ThingDef>.GetNamedSilentFail("Phobos_CrewWaterStandIn");
        if (towerDef == null) return;
        foreach (VehicleDef def in DefDatabase<VehicleDef>.AllDefsListForReading)
        {
            if (def.comps == null || !def.comps.Any(c => c is CompProperties_PhobosCamp) || def.comps.Any(c => c is CompProperties_PhobosTrailer)) continue;
            if (def.comps.Any(c => c is CompProperties_PhobosCrewWater)) continue;
            def.comps.Add(new CompProperties_PhobosCrewWater { towerDef = towerDef });
        }
    }
}
