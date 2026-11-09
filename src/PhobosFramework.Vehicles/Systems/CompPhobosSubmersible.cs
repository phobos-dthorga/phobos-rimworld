using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

[DefOf]
public static class SubmersibleDefOf
{
    public static VehicleStatDef Phobos_DiveEndurance = null!;
    public static VehicleStatDef Phobos_DiveControl = null!;

    static SubmersibleDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(SubmersibleDefOf));
}

/// <summary>Makes a Sea vehicle a submersible. Every figure is authored for play; the design record says why each is what it is (docs/development/submarine-framework.md).</summary>
public class CompProperties_PhobosSubmersible : VehicleCompProperties
{
    /// <summary>How long the tanks take to flood.</summary>
    public float diveSeconds = 15f;
    /// <summary>How long the tanks take to blow.</summary>
    public float surfaceSeconds = 10f;
    /// <summary>Below this share of Phobos_DiveControl it will not dive, and a boat already down comes up.</summary>
    public float controlFloor = 0.5f;
    /// <summary>The share of a full reserve it must hold before it will dive.</summary>
    public float minimumReserveShare = 0.1f;
    /// <summary>How many times faster the reserve goes while it is moving.</summary>
    public float moveBurnFactor = 3f;
    /// <summary>Hours of reserve won back per hour on the surface.</summary>
    public float rechargeFactor = 4f;
    /// <summary>The kinds of damage that carry through water to a boat that is under. Empty or missing means explosions.</summary>
    public List<DamageDef>? damageThatReaches;
    /// <summary>The share of such a hit that gets through.</summary>
    public float damageFactor = 0.5f;
    /// <summary>Water deep enough to dive in, besides the water nobody can wade.</summary>
    public List<TerrainDef>? alsoDeepEnough;
    /// <summary>Turrets, by key, that may fire from below, such as torpedo tubes. Every other turret is held while under.</summary>
    public List<string>? turretsThatFireBelow;

    public CompProperties_PhobosSubmersible() => compClass = typeof(CompPhobosSubmersible);
}

/// <summary>
/// A boat that can go under. Vehicle Framework has no depth, so a dive is a state of ours (docs/development/submarine-framework.md): the hull stays where it is, is drawn as a shadow, cannot be
/// seen or shot at, and cannot fire or land anyone. It runs on a reserve of air and charge that the surface refills. The state lives on the vehicle, so it travels in a caravan.
/// </summary>
public class CompPhobosSubmersible : VehicleComp
{
    const float TicksPerSecond = 60f;
    const float Unset = -1f;
    const int NeverRun = -1;

    DivePhase phase = DivePhase.Surfaced;
    int phaseStartTick;
    float reserve = Unset;
    int lastAdvanceTick = NeverRun;

    public CompProperties_PhobosSubmersible Props => (CompProperties_PhobosSubmersible)props;
    public DivePhase Phase => phase;
    public bool Hidden => DiveRule.Hidden(phase);
    public float Capacity => Vehicle.GetStatValue(SubmersibleDefOf.Phobos_DiveEndurance);
    public float Reserve => reserve < 0f ? Capacity : reserve;
    float Control => Vehicle.GetStatValue(SubmersibleDefOf.Phobos_DiveControl);
    VehicleCaravan? Caravan => Vehicle.GetVehicleCaravan();
    bool Moving => Caravan is { } caravan ? caravan.vehiclePather is { MovingNow: true } : Vehicle.vehiclePather is { Moving: true };
    int TicksToSettle => (int)((phase == DivePhase.Diving ? Props.diveSeconds : Props.surfaceSeconds) * TicksPerSecond);

    /// <summary>Whether damage of this kind carries through water to the hull.</summary>
    bool Reaches(DamageDef def) => Props.damageThatReaches.NullOrEmpty() ? def == DamageDefOf.Bomb : Props.damageThatReaches!.Contains(def);

    /// <summary>Water deep enough to hide a hull: what nobody can wade, or what the design adds.</summary>
    bool DeepEnough(TerrainDef terrain) => (terrain.IsWater && terrain.passability == Traversability.Impassable) || (Props.alsoDeepEnough?.Contains(terrain) ?? false);

    bool DeepEnoughHere
    {
        get
        {
            Map map = Vehicle.Map;
            return Vehicle.OccupiedRect().Cells.All(cell => cell.InBounds(map) && DeepEnough(cell.GetTerrain(map)));
        }
    }

    static bool TileDeepEnough(VehicleCaravan caravan) => Find.WorldGrid[caravan.Tile].WaterCovered;

    /// <summary>The worst of the parts that keep it trimmed, for telling the player which one to mend.</summary>
    string WeakestPartLabel => Vehicle.statHandler.components
        .Where(c => c.props.categories?.Contains(SubmersibleDefOf.Phobos_DiveControl) ?? false)
        .OrderBy(c => c.HealthPercent).FirstOrDefault()?.props.label ?? Vehicle.LabelShort;

    string Shallow => "PhobosSubmersible_RefuseShallow".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();

    /// <summary>Why it will not dive now, or null.</summary>
    public string? DiveRefusal()
    {
        if (phase != DivePhase.Surfaced) return "PhobosSubmersible_RefuseNotSurfaced".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        if (!Vehicle.HasEnoughOperators) return "PhobosSubmersible_RefuseCrew".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        if (Control < Props.controlFloor) return "PhobosSubmersible_RefusePart".Translate(WeakestPartLabel.Named("PART")).Resolve();
        if (!DiveRule.EnoughToDive(Reserve, Capacity, Props.minimumReserveShare)) return "PhobosSubmersible_RefuseReserve".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        if (Vehicle.Spawned) return DeepEnoughHere ? null : Shallow;
        return Caravan is { } caravan && TileDeepEnough(caravan) ? null : Shallow;
    }

    /// <summary>Why a boat that is going or gone must come up now, or null.</summary>
    string? MustComeUp()
    {
        if (Reserve <= 0f) return "PhobosSubmersible_UpEmpty".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        if (Control < Props.controlFloor) return "PhobosSubmersible_UpPart".Translate(Vehicle.LabelShort.Named("VEHICLE"), WeakestPartLabel.Named("PART")).Resolve();
        bool deep = Vehicle.Spawned ? DeepEnoughHere : Caravan is not { } caravan || TileDeepEnough(caravan);
        return deep ? null : "PhobosSubmersible_UpShallow".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
    }

    void SetPhase(DivePhase next)
    {
        phase = next;
        phaseStartTick = Find.TickManager.TicksGame;
        SyncHidden();
    }

    void SyncHidden() => Submersibles.SetHidden(Vehicle, Vehicle.Spawned && Hidden);

    void Tell(string text, MessageTypeDef type)
    {
        if (Vehicle.Faction != Faction.OfPlayer) return;
        if (Caravan is { } caravan) Messages.Message(text, caravan, type, historical: false);
        else if (Vehicle.Spawned) Messages.Message(text, Vehicle, type, historical: false);
    }

    /// <summary>Starts it down, or says why not. The one way any order to dive reaches the vehicle.</summary>
    public bool TryDive()
    {
        string? refused = DiveRefusal();
        if (refused != null)
        {
            Tell(refused, MessageTypeDefOf.RejectInput);
            return false;
        }
        SetPhase(DivePhase.Diving);
        Tell("PhobosSubmersible_ToldDiving".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), MessageTypeDefOf.SilentInput);
        return true;
    }

    /// <summary>Brings it up. It is in sight from the moment the tanks start to blow.</summary>
    public void Surface(string? forcedBecause = null)
    {
        if (!DiveRule.GoingOrGone(phase)) return;
        SetPhase(DivePhase.Surfacing);
        if (forcedBecause != null) Tell(forcedBecause, MessageTypeDefOf.NegativeEvent);
        else Tell("PhobosSubmersible_ToldSurfacing".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), MessageTypeDefOf.SilentInput);
    }

    /// <summary>Brings the reserve and the phase up to date. Safe to call from more than one place: it charges only for the time since it last ran.</summary>
    public void Advance()
    {
        int now = Find.TickManager.TicksGame;
        if (lastAdvanceTick == NeverRun)
        {
            lastAdvanceTick = now;
            if (reserve < 0f) reserve = Capacity;
            return;
        }
        int elapsed = now - lastAdvanceTick;
        if (elapsed <= 0) return;
        lastAdvanceTick = now;
        reserve = DiveRule.ReserveAfter(Reserve, Capacity, elapsed / (float)GenDate.TicksPerHour, phase, Moving, Props.moveBurnFactor, Props.rechargeFactor);
        DivePhase settled = DiveRule.Settled(phase, now - phaseStartTick, TicksToSettle);
        if (settled != phase)
        {
            SetPhase(settled);
            if (settled == DivePhase.Submerged) Tell("PhobosSubmersible_ToldUnder".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), MessageTypeDefOf.SilentInput);
        }
        if (DiveRule.GoingOrGone(phase) && MustComeUp() is { } because) Surface(because);
    }

    /// <summary>A dev tool's way to leave only this many hours in reserve.</summary>
    public void SetReserveForTesting(float hours) => reserve = hours;

    /// <summary>What is left of a hit on a hull that is under: true if nothing is.</summary>
    public bool AbsorbBelow(ref DamageInfo dinfo)
    {
        float left = DiveRule.DamageBelow(dinfo.Amount, Reaches(dinfo.Def), Props.damageFactor);
        if (left <= 0f) return true;
        dinfo.SetAmount(left);
        return false;
    }

    /// <summary>Whether this turret must hold fire while the boat is under.</summary>
    public bool HoldsBelow(VehicleTurret turret) => !(Props.turretsThatFireBelow?.Contains(turret.key) ?? false);

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        // A boat arriving from the world map may come out over shallows. It was never seen going down, so it simply arrives on the surface.
        if (!respawningAfterLoad && DiveRule.GoingOrGone(phase) && !DeepEnoughHere) phase = DivePhase.Surfaced;
        SyncHidden();
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        Submersibles.SetHidden(Vehicle, false);
        base.PostDeSpawn(map, mode);
    }

    public override void CompTickRare()
    {
        base.CompTickRare();
        Advance();
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref phase, "PhobosDivePhase", DivePhase.Surfaced);
        Scribe_Values.Look(ref phaseStartTick, "PhobosDivePhaseStart");
        Scribe_Values.Look(ref reserve, "PhobosDiveReserve", Unset);
        Scribe_Values.Look(ref lastAdvanceTick, "PhobosDiveLastAdvance", NeverRun);
    }

    string PhaseLabel => phase switch
    {
        DivePhase.Diving => "PhobosSubmersible_PhaseDiving".Translate().Resolve(),
        DivePhase.Submerged => "PhobosSubmersible_PhaseUnder".Translate().Resolve(),
        DivePhase.Surfacing => "PhobosSubmersible_PhaseSurfacing".Translate().Resolve(),
        _ => "PhobosSubmersible_PhaseSurfaced".Translate().Resolve(),
    };

    /// <summary>One line on the boat's state, for the map and for its caravan.</summary>
    public string StateLine() => "PhobosSubmersible_Line".Translate(PhaseLabel.Named("STATE"), Reserve.ToString("0.0").Named("LEFT"), Capacity.ToString("0.0").Named("FULL")).Resolve();

    public override string CompInspectStringExtra() => StateLine();

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (Vehicle.Faction != Faction.OfPlayer) yield break;
        yield return Submersibles.ToggleGizmo(DiveRule.GoingOrGone(phase), DiveRefusal(), Props, () =>
        {
            if (DiveRule.GoingOrGone(phase)) Surface();
            else TryDive();
        });
    }
}
