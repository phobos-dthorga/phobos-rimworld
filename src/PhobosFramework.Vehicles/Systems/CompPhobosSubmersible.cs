using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Environment;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Phobos.VehicleSystems;

[DefOf]
public static class SubmersibleDefOf
{
    public static VehicleStatDef Phobos_DiveEndurance = null!;
    public static VehicleStatDef Phobos_DiveControl = null!;
    public static HediffDef Phobos_StaleAir = null!;
    public static VehicleStatDef Phobos_DiveBottoming = null!;
    public static VehicleStatDef Phobos_DiveStaleAirRate = null!;

    static SubmersibleDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(SubmersibleDefOf));
}

/// <summary>Makes a Sea vehicle a submersible. Every figure is authored for play; the design record says why each is what it is (docs/development/submarine-framework.md).</summary>
public class CompProperties_PhobosSubmersible : VehicleCompProperties
{
    /// <summary>Plays once at the hull when watchers spot it; null for silence.</summary>
    public SoundDef? spottedSound;
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
    /// <summary>Whether this design is small enough to hide in chest-deep water and, on the world map, in rivers. A midget boat would say yes.</summary>
    public bool divesInChestDeepWater;
    /// <summary>Turrets, by key, that may fire from below, such as torpedo tubes. Every other turret is held while under.</summary>
    public List<string>? turretsThatFireBelow;
    /// <summary>How far away, in cells, anyone can spot a hull that is under.</summary>
    public float spotRange = 25f;
    /// <summary>The chance per look, about every four seconds, that one watcher right above the hull spots it.</summary>
    public float spotChance = 0.02f;
    /// <summary>How many times easier it is to spot on the move.</summary>
    public float movingSpotFactor = 3f;
    /// <summary>What is left of a watcher's sight on the darkest night, as a share of daylight.</summary>
    public float darkSpotFloor = 0.5f;
    /// <summary>How noisy and easy to see this design is: below 1 is quieter. An upgrade or a small hull would lower it.</summary>
    public float noise = 1f;
    /// <summary>How long watchers follow a hull after last spotting it.</summary>
    public float trackedSeconds = 30f;
    /// <summary>How fast the crew's stale air builds, per hour, once they stay down with nothing left. The body clears a tenth an hour, so the default gains a fifth an hour.</summary>
    public float staleAirPerHour = 0.3f;
    /// <summary>The share of its health the bottoming gear needs before the boat can settle on it.</summary>
    public float settleFloor = 0.5f;
    /// <summary>How much of the usual reserve a boat burns while settled on the bottom.</summary>
    public float settledBurnFactor = 0.5f;
    /// <summary>How much of its usual noise a boat makes while settled on the bottom.</summary>
    public float settledNoiseFactor = 0.25f;

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
    int trackedUntilTick;
    bool spottedTold;
    bool stayDown;
    bool lostTold;
    bool settled;

    public CompProperties_PhobosSubmersible Props => (CompProperties_PhobosSubmersible)props;
    public DivePhase Phase => phase;
    public bool Hidden => DiveRule.Hidden(phase);
    /// <summary>Whether watchers know where this hull is: only then can it be seen or aimed at.</summary>
    public bool Tracked => Hidden && DiveRule.Tracked(Find.TickManager.TicksGame, trackedUntilTick);
    /// <summary>Whether the crew has been told to stay under after the air and charge run out.</summary>
    public bool StayDown => stayDown;
    /// <summary>Whether it is resting on the bottom, using less and harder to find, but unable to move until it lifts off.</summary>
    public bool Settled => settled;
    /// <summary>Whether it has gear to settle on at all. An upgrade fits it; a design may be built with it.</summary>
    public bool HasBottomingGear => Vehicle.GetStatValue(SubmersibleDefOf.Phobos_DiveBottoming) > 0f;
    /// <summary>Whether it has been ordered to stay and nobody aboard can change that. A later piece of equipment could raise it; nothing aboard can.</summary>
    public bool Lost => DiveRule.Lost(phase, Vehicle.HasEnoughOperators, stayDown);
    public float Capacity => Vehicle.GetStatValue(SubmersibleDefOf.Phobos_DiveEndurance);
    public float Reserve => reserve < 0f ? Capacity : reserve;
    float Control => Vehicle.GetStatValue(SubmersibleDefOf.Phobos_DiveControl);
    VehicleCaravan? Caravan => Vehicle.GetVehicleCaravan();
    bool Moving => VehicleMotion.Moving(Vehicle);
    int TicksToSettle => (int)((phase == DivePhase.Diving ? Props.diveSeconds : Props.surfaceSeconds) * TicksPerSecond);

    /// <summary>Whether damage of this kind carries through water to the hull.</summary>
    bool Reaches(DamageDef def) => Props.damageThatReaches.NullOrEmpty() ? def == DamageDefOf.Bomb : Props.damageThatReaches!.Contains(def);

    /// <summary>Water deep enough to hide a hull: what nobody can wade, or what the design adds.</summary>
    bool DeepEnough(TerrainDef terrain) => DiveRule.DeepEnough(terrain.IsWater, terrain.passability == Traversability.Impassable, terrain.pathCost, Props.divesInChestDeepWater) || (Props.alsoDeepEnough?.Contains(terrain) ?? false);

    bool DeepEnoughHere
    {
        get
        {
            Map map = Vehicle.Map;
            return Vehicle.OccupiedRect().Cells.All(cell => cell.InBounds(map) && DeepEnough(cell.GetTerrain(map)));
        }
    }

    /// <summary>Open water hides a caravan, and so does a river for a design small enough to use one.</summary>
    bool TileDeepEnough(VehicleCaravan caravan)
    {
        var tile = Find.WorldGrid[caravan.Tile];
        return tile.WaterCovered || (Props.divesInChestDeepWater && tile is SurfaceTile { Rivers.Count: > 0 });
    }

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

    IEnumerable<VehicleComponent> BottomingParts => Vehicle.statHandler.components.Where(c => c.props.categories?.Contains(SubmersibleDefOf.Phobos_DiveBottoming) ?? false);

    /// <summary>Why it will not settle now, or null. Part damage is read directly, so a design need not route it through the stat.</summary>
    public string? SettleRefusal()
    {
        if (phase != DivePhase.Submerged) return "PhobosSubmersible_RefuseSettleNotUnder".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        if (Moving) return "PhobosSubmersible_RefuseSettleMoving".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        VehicleComponent? worst = BottomingParts.OrderBy(c => c.HealthPercent).FirstOrDefault();
        return DiveRule.CanSettle(phase, Moving, worst?.HealthPercent ?? 1f, Props.settleFloor) ? null : "PhobosSubmersible_RefusePart".Translate((worst?.props.label ?? Vehicle.LabelShort).Named("PART")).Resolve();
    }

    /// <summary>Settles it on the bottom or lifts it off, or says why not. The one way an order to settle reaches the vehicle.</summary>
    public void ToggleSettled()
    {
        if (settled)
        {
            settled = false;
            if (HeardAboard) VehicleWorkSounds.Play(Vehicle, "LiftOff", "Settle");
            Tell("PhobosSubmersible_ToldLiftOff".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), MessageTypeDefOf.SilentInput);
            return;
        }
        string? refused = SettleRefusal();
        if (refused != null)
        {
            Tell(refused, MessageTypeDefOf.RejectInput);
            return;
        }
        settled = true;
        if (HeardAboard) VehicleWorkSounds.Play(Vehicle, "Settle");
        Tell("PhobosSubmersible_ToldSettled".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), MessageTypeDefOf.SilentInput);
    }

    /// <summary>A boat on the bottom has to lift off before it can be sent anywhere. The game's own way to refuse an order to move.</summary>
    public override AcceptanceReport CanMove(FloatMenuContext context) => SettledRefusal is { } refused ? refused : base.CanMove(context);

    /// <summary>Why a settled boat will not move, or null; also asked by MoveRefusals for orders that bypass the float menu.</summary>
    public string? SettledRefusal => settled ? "PhobosSubmersible_RefuseMoveSettled".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve() : null;

    /// <summary>Why it cannot be brought up or have its orders changed, or null.</summary>
    public string? SurfaceRefusal() => Lost ? "PhobosSubmersible_RefuseLost".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve() : null;

    /// <summary>Why a boat that is going or gone must come up now, or null.</summary>
    string? MustComeUp()
    {
        if (DiveRule.EmptyForcesUp(Reserve, stayDown)) return "PhobosSubmersible_UpEmpty".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        // Nobody left to work the boat: the tanks blow by themselves, unless its orders were to stay down. Then it is lost with all hands, which is the player's decision to make.
        if (DiveRule.CrewLossForcesUp(Vehicle.HasEnoughOperators, stayDown)) return "PhobosSubmersible_UpCrew".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        if (Control < Props.controlFloor) return "PhobosSubmersible_UpPart".Translate(Vehicle.LabelShort.Named("VEHICLE"), WeakestPartLabel.Named("PART")).Resolve();
        bool deep = Vehicle.Spawned ? DeepEnoughHere : Caravan is not { } caravan || TileDeepEnough(caravan);
        return deep ? null : "PhobosSubmersible_UpShallow".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
    }

    void SetPhase(DivePhase next)
    {
        // Diving and surfacing are seen from outside anyway; what happens under the water is heard only aboard the player's own boats.
        if (next == DivePhase.Diving && phase != DivePhase.Diving) VehicleWorkSounds.Play(Vehicle, "Dive");
        if (next == DivePhase.Surfacing && phase != DivePhase.Surfacing) VehicleWorkSounds.Play(Vehicle, "Surface");
        phase = next;
        phaseStartTick = Find.TickManager.TicksGame;
        if (next != DivePhase.Submerged)
        {
            LoseWatchers();
            settled = false;
        }
        SyncHidden();
    }

    /// <summary>Nobody is following it any more, because it is in plain sight or has left the map. Nothing to tell the player.</summary>
    void LoseWatchers()
    {
        trackedUntilTick = 0;
        spottedTold = false;
    }

    /// <summary>Watchers know where this hull is, for a while. The one way a hull becomes spotted, so sonar and the like can call it too.</summary>
    public void Spot()
    {
        if (!Hidden) return;
        trackedUntilTick = Find.TickManager.TicksGame + (int)(Props.trackedSeconds * TicksPerSecond);
        if (spottedTold) return;
        spottedTold = true;
        Tell("PhobosSubmersible_Spotted".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), MessageTypeDefOf.NegativeEvent);
        if (Vehicle.Spawned) Props.spottedSound?.PlayOneShot(new TargetInfo(Vehicle.Position, Vehicle.Map));
    }

    float WatcherVisibility
    {
        get
        {
            Map map = Vehicle.Map;
            return DiveRule.Visibility(map.weatherManager.CurWeatherAccuracyMultiplier, map.skyManager.CurSkyGlow, Props.darkSpotFloor);
        }
    }

    /// <summary>Whether this watcher, awake and with a clear line, can see the hull at all. A hull that is itself under, or anyone downed, asleep or switched off, counts as no threat and so as no watcher.</summary>
    bool CanWatch(IAttackTarget watcher) => !watcher.ThreatDisabled(null) && watcher.Thing.Position.DistanceTo(Vehicle.Position) < Props.spotRange && GenSight.LineOfSightToThing(watcher.Thing.Position, Vehicle, Vehicle.Map);

    /// <summary>A hull seen going down is followed under: the way to get away is to dive out of sight.</summary>
    void CheckSeenGoingDown()
    {
        if (Vehicle.Spawned && Vehicle.Map.attackTargetsCache.GetPotentialTargetsFor(Vehicle).Any(CanWatch)) Spot();
    }

    /// <summary>One look by everyone hostile on the map. More eyes give more chances. The line of sight, the costly part, is checked only for a watcher who would have spotted it.</summary>
    void Watch()
    {
        bool moving = Moving;
        float visibility = WatcherVisibility;
        IntVec3 at = Vehicle.Position;
        foreach (IAttackTarget watcher in Vehicle.Map.attackTargetsCache.GetPotentialTargetsFor(Vehicle))
        {
            float chance = DiveRule.SpotChance(Props.spotChance, watcher.Thing.Position.DistanceTo(at), Props.spotRange, moving, Props.movingSpotFactor, visibility, DiveRule.Noise(Props.noise, settled, Props.settledNoiseFactor));
            if (chance <= 0f || !Rand.Chance(chance) || !CanWatch(watcher)) continue;
            Spot();
            return;
        }
        if (!spottedTold || Tracked) return;
        spottedTold = false;
        Tell("PhobosSubmersible_Lost".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), MessageTypeDefOf.NeutralEvent);
    }

    /// <summary>Whether this searcher must leave the hull alone: anyone must until it is spotted, and after that anyone whose weapon cannot reach it, so a spotted boat does not soak up gunfire that could never hurt it.</summary>
    public bool OutOfReachOf(IAttackTargetSearcher? searcher)
    {
        if (!Tracked || searcher?.CurrentEffectiveVerb is not { } verb) return true;
        // Combat Extended's guns are not the game's projectile verbs, so its default shell stands in.
        ThingDef? shot = verb.GetProjectile() ?? verb.verbProps.defaultProjectile;
        return shot?.projectile == null || !Reaches(shot.projectile.damageDef);
    }

    /// <summary>Sets the standing order to stay under after the air and charge run out, and says so.</summary>
    public void SetStayDown(bool on)
    {
        if (SurfaceRefusal() is { } refused)
        {
            Tell(refused, MessageTypeDefOf.RejectInput);
            return;
        }
        stayDown = on;
        Tell((on ? "PhobosSubmersible_ToldStayDown" : "PhobosSubmersible_ToldComeUpWhenEmpty").Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), MessageTypeDefOf.SilentInput);
    }

    void BreatheStaleAir(float hours)
    {
        float amount = hours * Props.staleAirPerHour * Vehicle.GetStatValue(SubmersibleDefOf.Phobos_DiveStaleAirRate);
        foreach (Pawn pawn in Vehicle.AllPawnsAboard.ToList())
        {
            if (!pawn.Dead && pawn.RaceProps.IsFlesh) HealthUtility.AdjustSeverity(pawn, SubmersibleDefOf.Phobos_StaleAir, amount);
        }
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

    /// <summary>Starts it up on an order, or says why not. The one way an order to surface reaches the vehicle.</summary>
    public bool TrySurface()
    {
        string? refused = SurfaceRefusal();
        if (refused != null)
        {
            Tell(refused, MessageTypeDefOf.RejectInput);
            return false;
        }
        Surface();
        return true;
    }

    /// <summary>The once-only news that a boat is lost: a letter, because this is the end of a story and should not scroll away.</summary>
    void AnnounceIfLost()
    {
        if (!Lost)
        {
            lostTold = false;
            return;
        }
        if (lostTold) return;
        lostTold = true;
        if (Vehicle.Faction != Faction.OfPlayer) return;
        LookTargets where = Caravan is { } caravan ? new LookTargets(caravan) : new LookTargets(Vehicle);
        Find.LetterStack.ReceiveLetter("PhobosSubmersible_LostLabel".Translate(Vehicle.LabelShort.Named("VEHICLE")), "PhobosSubmersible_LostText".Translate(Vehicle.LabelShort.Named("VEHICLE")), LetterDefOf.NegativeEvent, where);
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
        float hours = elapsed / (float)GenDate.TicksPerHour;
        float before = Reserve;
        reserve = DiveRule.ReserveAfter(before, Capacity, hours, phase, Moving, Props.moveBurnFactor, Props.rechargeFactor, settled ? Props.settledBurnFactor : 1f);
        if (DiveRule.BreathingStaleAir(phase, before, stayDown)) BreatheStaleAir(hours);
        else if (before > 0f && DiveRule.BreathingStaleAir(phase, reserve, stayDown)) Tell("PhobosSubmersible_EmptyStayingDown".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), MessageTypeDefOf.ThreatSmall);
        DivePhase reached = DiveRule.Settled(phase, now - phaseStartTick, TicksToSettle);
        if (reached != phase)
        {
            SetPhase(reached);
            if (reached == DivePhase.Submerged)
            {
                Tell("PhobosSubmersible_ToldUnder".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), MessageTypeDefOf.SilentInput);
                // The water closes over the hull: any fire burning on it goes out (owner, 10 Nov 2026).
                if (AttachedFires.PutOut(Vehicle) > 0) Tell("PhobosSubmersible_FireOut".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), MessageTypeDefOf.PositiveEvent);
                CheckSeenGoingDown();
            }
        }
        if (DiveRule.GoingOrGone(phase) && MustComeUp() is { } because) Surface(because);
        AnnounceIfLost();
    }

    /// <summary>A dev tool's way to leave only this many hours in reserve.</summary>
    public void SetReserveForTesting(float hours) => reserve = hours;

    /// <summary>What is left of a hit on a hull that is under: true if nothing is.</summary>
    public bool AbsorbBelow(ref DamageInfo dinfo)
    {
        float left = DiveRule.DamageBelow(dinfo.Amount, Reaches(dinfo.Def), Props.damageFactor);
        if (left <= 0f) return true;
        // A blast close enough to hurt shows where the hull is: oil, air and wreckage come up.
        Spot();
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
        VehicleWorkSounds.EndAll(Vehicle);
        LoseWatchers();
        settled = false;
        base.PostDeSpawn(map, mode);
    }

    public override void CompTickRare()
    {
        base.CompTickRare();
        Advance();
        if (Hidden && Vehicle.Spawned) Watch();
        SyncSounds();
    }

    /// <summary>A hidden boat's sounds would give it away, so the sounds of being under play only for the player's own (submarine-framework.md).</summary>
    bool HeardAboard => Vehicle.Faction == Faction.OfPlayer;

    /// <summary>The hull's creaks and the air scrubber while under (the scrubber's sound names its upgrade). Presentation only.</summary>
    void SyncSounds()
    {
        bool under = HeardAboard && phase == DivePhase.Submerged;
        VehicleWorkSounds.Loop(Vehicle, "Creak", under);
        VehicleWorkSounds.Loop(Vehicle, "Scrubber", under);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref phase, "PhobosDivePhase", DivePhase.Surfaced);
        Scribe_Values.Look(ref phaseStartTick, "PhobosDivePhaseStart");
        Scribe_Values.Look(ref reserve, "PhobosDiveReserve", Unset);
        Scribe_Values.Look(ref lastAdvanceTick, "PhobosDiveLastAdvance", NeverRun);
        Scribe_Values.Look(ref trackedUntilTick, "PhobosDiveTrackedUntil");
        Scribe_Values.Look(ref spottedTold, "PhobosDiveSpottedTold");
        Scribe_Values.Look(ref stayDown, "PhobosDiveStayDown");
        Scribe_Values.Look(ref lostTold, "PhobosDiveLostTold");
        Scribe_Values.Look(ref settled, "PhobosDiveSettled");
    }

    string PhaseLabel => phase switch
    {
        DivePhase.Diving => "PhobosSubmersible_PhaseDiving".Translate().Resolve(),
        DivePhase.Submerged => (Tracked ? "PhobosSubmersible_PhaseUnderSpotted" : settled ? "PhobosSubmersible_PhaseSettled" : "PhobosSubmersible_PhaseUnder").Translate().Resolve(),
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
        bool down = DiveRule.GoingOrGone(phase);
        yield return Submersibles.ToggleGizmo(down, down ? SurfaceRefusal() : DiveRefusal(), Props, () =>
        {
            if (DiveRule.GoingOrGone(phase)) TrySurface();
            else TryDive();
        });
        yield return Submersibles.StayDownGizmo(stayDown, SurfaceRefusal(), () => SetStayDown(!stayDown));
        if (HasBottomingGear && Vehicle.Spawned) yield return Submersibles.SettleGizmo(settled, settled ? null : SettleRefusal(), ToggleSettled);
    }
}
