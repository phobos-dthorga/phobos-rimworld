using System;
using System.Collections.Generic;
using IRBM;
using Phobos.Framework;
using Phobos.Framework.Work;
using Phobos.MissilePatches.Rules;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.MissilePatches.Strike;

/// <summary>
/// D'thorga's strike on one map (D'thorga Patches 0.9.0): zones from ground zero (the map's centre; the missile mod has no impact cell)
/// instead of the missile mod's "everything on the map". First every pawn, building, item and plant, nearest first; then every cell
/// (craters, ash, pollution, fires). A neighbouring map within the warhead's reach gets only the thermal effects, at a fixed distance,
/// unless the player chose the full strike everywhere. The target map holds the white screen until its fireball and blast zones are
/// done, then fades it.
/// </summary>
public sealed class StrikeWork : FrameWork
{
    readonly WorldObjectCompProperties_StrategicPayload props;
    readonly PhobosStrikeExtension zones;
    readonly PhobosStrikeRulesDef rules;
    readonly BlastSlice slice = new();

    List<Thing>? things;
    float[] thingDistances = Array.Empty<float>();
    int nextThing;
    IntVec3[] cells = Array.Empty<IntVec3>();
    float[] cellDistances = Array.Empty<float>();
    int nextCell;

    ShelterMode shelter;
    bool rubble;
    CorpseMode corpses;
    bool fragmentsLive;
    float fireBase;
    bool rimatomics;
    bool radiation;
    bool emp;
    bool pollution;
    ThingDef? ash;
    HediffDef? flashBlindness;

    bool fadeStarted;
    float fadeStartedAt = -1f;
    bool longMessageShown;

    public StrikeWork(Map map, WorldObjectCompProperties_StrategicPayload props, PhobosStrikeExtension zones, bool full, bool isTarget,
        Thing? launcher, string? sourceLabel)
    {
        Map = map;
        PlaceLabel = map.Parent?.Label ?? "an unnamed map";
        this.props = props;
        this.zones = zones;
        rules = PhobosStrikeRulesDef.Rules;
        Full = full;
        IsTarget = isTarget;
        Launcher = launcher;
        SourceLabel = sourceLabel;
    }

    public Map Map { get; }
    /// <summary>Fireball and blast zones on this map; otherwise only the thermal effects, at the neighbour distance.</summary>
    public bool Full { get; }
    public bool IsTarget { get; }
    public Thing? Launcher { get; }
    public string? SourceLabel { get; }

    public int Total => things?.Count ?? 0;
    public int Vaporised { get; private set; }
    public int Wrecked { get; private set; }
    public int Damaged { get; private set; }
    public int Killed { get; private set; }
    public int Burned { get; private set; }
    public int Blinded { get; private set; }
    public int Irradiated { get; private set; }
    public int Stunned { get; private set; }
    public int Wilted { get; private set; }
    public int Sheltered { get; private set; }
    public int Gone { get; private set; }
    public int Fires { get; private set; }
    public int Scoured { get; private set; }
    public int Polluted { get; private set; }
    public int RoofsStripped { get; private set; }
    public int Errors { get; private set; }
    public string? FirstError { get; private set; }
    public int Next => nextThing + nextCell;
    public int Count => Total + cells.Length;

    /// <summary>Read once, while the map is alive: a map that has been closed throws on its parent.</summary>
    public string PlaceLabel { get; }
    public override string Label => (Full ? "Strike on " : "Heat and fallout over ") + PlaceLabel;
    public override bool StillValid => Current.Game != null && !Map.Disposed && Map.Index >= 0 && Find.Maps.Contains(Map);
    public override int MinimumStepsPerSlice => IrbmBudgetRule.MinimumThingsPerSlice;
    public override bool Finished => things != null && nextThing >= things.Count && nextCell >= cells.Length;

    /// <summary>While the target map's screen is white, or fading back, the strike may take more of each frame.</summary>
    public override float BudgetMs
    {
        get
        {
            float normal = MissilePatchesMod.BudgetMs(float.MaxValue);
            bool white = IsTarget && (!fadeStarted || Time.realtimeSinceStartup - fadeStartedAt < IrbmBudgetRule.WhiteScreenSeconds);
            return white && MissilePatchesMod.Settings.whiteScreenBudget && normal < rules.whiteScreenBudgetMs ? rules.whiteScreenBudgetMs : normal;
        }
    }

    protected override void Start()
    {
        MissilePatchesSettings settings = MissilePatchesMod.Settings;
        shelter = ShelterRule.Effective(settings.shelter, props.penetrateThickRoof);
        rubble = settings.rubble;
        corpses = settings.corpses;
        fragmentsLive = CombatExtendedFragments.Live(settings.fragments);
        fireBase = rules.FireChance(settings.fires);
        rimatomics = settings.fallout == FalloutMode.Automatic && zones.fallout && RimatomicsTieIn.Active;
        radiation = rimatomics && settings.radiation && RimatomicsTieIn.CanIrradiate;
        flashBlindness = rimatomics ? RimatomicsTieIn.FlashBlindness : null;
        emp = settings.emp && zones.emp;
        pollution = settings.pollution && zones.pollution && ModsConfig.BiotechActive;
        ash = OptionalMods.ThingDefNamed("Filth_Ash");
        // Before anything is harmed: the town's garrison, for an ally's assault that follows (RimPacts sizes it to who is standing).
        if (Full) WorldComponent_PhobosStrikeMemory.RecordBeforeStrike(Map);
        if (Full) StripRoofs();
        SnapshotThings();
        SnapshotCells();
    }

    float DistanceTo(IntVec3 cell) => Full ? (cell - Map.Center).LengthHorizontal : rules.neighbourDistance;

    StrikeZone ZoneAt(float distance) => Full ? StrikeRule.Zone(distance, zones.fireballRadius, zones.blastRadius) : StrikeZone.Thermal;

    bool IsSheltered(IntVec3 cell, StrikeZone zone)
    {
        if (zone == StrikeZone.Fireball) return false;
        RoofDef? roof = Map.roofGrid.RoofAt(cell);
        return ShelterRule.Sheltered(shelter, roof != null, roof?.isNatural ?? false, roof?.isThickRoof ?? false);
    }

    /// <summary>Built roofs everywhere, and every roof within the fireball, mountain or not: one pass before anything falls.</summary>
    void StripRoofs()
    {
        RoofGrid roofs = Map.roofGrid;
        foreach (IntVec3 cell in Map.AllCells)
        {
            RoofDef? roof = roofs.RoofAt(cell);
            if (roof == null) continue;
            if (roof.isNatural && DistanceTo(cell) > zones.fireballRadius) continue;
            roofs.SetRoof(cell, null);
            RoofsStripped++;
        }
    }

    void SnapshotThings()
    {
        List<Thing> all = Map.listerThings.AllThings;
        var chosen = new List<Thing>(all.Count);
        var distances = new List<float>(all.Count);
        for (int i = 0; i < all.Count; i++)
        {
            Thing thing = all[i];
            ThingCategory category = thing.def.category;
            if (!thing.Spawned || (category != ThingCategory.Pawn && category != ThingCategory.Building && category != ThingCategory.Item
                && category != ThingCategory.Plant)) continue;
            float distance = DistanceTo(thing.Position);
            if (IsSheltered(thing.Position, ZoneAt(distance)))
            {
                Sheltered++;
                continue;
            }
            chosen.Add(thing);
            distances.Add(distance);
        }
        Thing[] ordered = chosen.ToArray();
        float[] keys = distances.ToArray();
        Array.Sort(keys, ordered);
        things = new List<Thing>(ordered);
        thingDistances = keys;
    }

    void SnapshotCells()
    {
        bool anyCellWork = fireBase > 0f || (Full && (ash != null || pollution));
        if (!anyCellWork) return;
        var list = new List<IntVec3>(Map.cellIndices.NumGridCells);
        foreach (IntVec3 cell in Map.AllCells) list.Add(cell);
        IntVec3[] ordered = list.ToArray();
        var keys = new float[ordered.Length];
        for (int i = 0; i < ordered.Length; i++) keys[i] = DistanceTo(ordered[i]);
        if (Full) Array.Sort(keys, ordered);
        cells = ordered;
        cellDistances = keys;
    }

    protected override void BeginSlice() => slice.Begin(Map, SourceLabel);

    protected override void EndSlice()
    {
        slice.End(Map);
        if (IsTarget && !fadeStarted && SecondsSinceStart >= rules.holdWhiteMaxSeconds) StartFade();
        if (!longMessageShown && !Finished && SecondsSinceStart >= IrbmBudgetRule.LongBlastSeconds && MissilePatchesMod.Settings.messages)
        {
            longMessageShown = true;
            Messages.Message("PhobosMissilePatches_BlastStillRunning".Translate(PlaceLabel.Named("PLACE")), MessageTypeDefOf.NeutralEvent, historical: false);
        }
    }

    protected override void Step()
    {
        try
        {
            if (things != null && nextThing < things.Count)
            {
                int index = nextThing++;
                float distance = thingDistances[index];
                if (IsTarget && !fadeStarted && distance > zones.blastRadius) StartFade();
                HitThing(things[index], distance);
                return;
            }
            if (IsTarget && !fadeStarted) StartFade();
            if (nextCell < cells.Length)
            {
                int index = nextCell++;
                HitCell(cells[index], cellDistances[index]);
            }
        }
        catch (Exception error)
        {
            Errors++;
            FirstError ??= error.ToString();
        }
    }

    void StartFade()
    {
        fadeStarted = true;
        fadeStartedAt = Time.realtimeSinceStartup;
        ScreenFader.StartFade(Color.clear, IrbmBudgetRule.WhiteScreenSeconds);
    }

    void HitThing(Thing thing, float distance)
    {
        if (thing.Destroyed || !thing.Spawned || thing.Map != Map)
        {
            Gone++;
            return;
        }
        StrikeZone zone = ZoneAt(distance);
        if (thing is Pawn pawn) HitPawn(pawn, distance, zone);
        else if (zone == StrikeZone.Thermal) HeatThing(thing, distance);
        else Vaporise(thing, zone);
    }

    void Vaporise(Thing thing, StrikeZone zone)
    {
        if (!thing.def.destroyable) return;
        ThingHit hit = zone == StrikeZone.Fireball
            ? ThingHit.Vanish
            : HitRule.Choose(false, thing.def.category == ThingCategory.Building, true, thing.def.useHitPoints, true, thing.HitPoints,
                float.MaxValue, rubble, fragmentsLive);
        Apply(thing, hit, new DamageInfo(DamageDefOf.Bomb, props.damageAmount, props.armorPenetration, -1f, Launcher, null, IRBM_DefOf.IRBM_MissileWeapon));
    }

    /// <summary>Beyond the blast: plants wilt, buildings and items take what the blast still carries, powered things stop.</summary>
    void HeatThing(Thing thing, float distance)
    {
        if (thing is Plant plant)
        {
            if (!plant.LeaflessNow)
            {
                plant.MakeLeafless(Plant.LeaflessCause.Poison, sendMessage: false);
                Wilted++;
            }
            return;
        }
        if (Full)
        {
            float amount = StrikeRule.Falloff(distance, zones.reach) * rules.buildingThermalFactor;
            if (amount > 0f)
            {
                ThingHit hit = HitRule.Choose(false, thing.def.category == ThingCategory.Building, true, thing.def.useHitPoints,
                    thing.def.destroyable, thing.HitPoints, amount, rubble, fragmentsLive);
                Apply(thing, hit, new DamageInfo(DamageDefOf.Bomb, amount, 0f, -1f, Launcher, null, IRBM_DefOf.IRBM_MissileWeapon));
            }
        }
        Pulse(thing);
    }

    void Apply(Thing thing, ThingHit hit, DamageInfo damage)
    {
        switch (hit)
        {
            case ThingHit.Vanish:
                IrbmSweep.EmptyHeldThings(thing);
                thing.Destroy(DestroyMode.Vanish);
                Vaporised++;
                break;
            case ThingHit.KillFinalize:
                thing.Destroy(DestroyMode.KillFinalize);
                Wrecked++;
                break;
            default:
                thing.TakeDamage(damage);
                Damaged++;
                break;
        }
    }

    void HitPawn(Pawn pawn, float distance, StrikeZone zone)
    {
        if (zone == StrikeZone.Fireball)
        {
            var core = new DamageInfo(DamageDefOf.Bomb, props.damageAmount, props.armorPenetration, -1f, Launcher, null, IRBM_DefOf.IRBM_MissileWeapon);
            if (pawn.RaceProps.body != null) core.SetHitPart(pawn.RaceProps.body.corePart);
            pawn.TakeDamage(core);
            AfterDeath(pawn, vaporise: corpses == CorpseMode.Vaporised);
            return;
        }
        float falloff = StrikeRule.Falloff(distance, zones.reach);
        float blast = falloff * (zone == StrikeZone.Blast ? rules.pawnBlastFactor : rules.pawnThermalBlastFactor);
        if (blast > 0f) pawn.TakeDamage(new DamageInfo(DamageDefOf.Bomb, blast, 0f, -1f, Launcher, null, IRBM_DefOf.IRBM_MissileWeapon));
        if (!pawn.Dead) Burn(pawn, falloff * rules.burnFactor);
        if (pawn.Dead)
        {
            AfterDeath(pawn, vaporise: zone == StrikeZone.Blast && corpses == CorpseMode.Vaporised);
            return;
        }
        float strength = StrikeRule.Strength(distance, zones.fireballRadius, zones.reach);
        Blind(pawn, strength);
        if (radiation)
        {
            RimatomicsTieIn.Irradiate(pawn, rules.radiationStrength * strength);
            Irradiated++;
        }
        Pulse(pawn);
    }

    void AfterDeath(Pawn pawn, bool vaporise)
    {
        if (!pawn.Dead) return;
        Killed++;
        if (vaporise && pawn.Corpse is { Spawned: true } corpse) corpse.Destroy();
    }

    /// <summary>Burns spread over the skin in several hits, so D'thorga Medical's burn load and shock see a burned body, not one part.</summary>
    void Burn(Pawn pawn, float total)
    {
        int hits = StrikeRule.BurnHits(total, rules.maxBurnPerHit, rules.maxBurnHits);
        if (hits == 0) return;
        float each = total / hits;
        for (int i = 0; i < hits && !pawn.Dead; i++)
        {
            var burn = new DamageInfo(DamageDefOf.Burn, each, 0f, -1f, Launcher, null, IRBM_DefOf.IRBM_MissileWeapon);
            burn.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
            pawn.TakeDamage(burn);
        }
        Burned++;
    }

    /// <summary>Rimatomics' flash blindness on each eye, more likely nearer the fireball.</summary>
    void Blind(Pawn pawn, float strength)
    {
        if (flashBlindness == null || pawn.health?.hediffSet == null || pawn.health.hediffSet.HasHediff(flashBlindness)) return;
        if (!Rand.Chance(strength * rules.flashBlindChance)) return;
        bool any = false;
        foreach (BodyPartRecord part in pawn.health.hediffSet.GetNotMissingParts())
        {
            if (!part.def.tags.Contains(BodyPartTagDefOf.SightSource)) continue;
            pawn.health.AddHediff(flashBlindness, part);
            any = true;
        }
        if (any) Blinded++;
    }

    /// <summary>The pulse stops powered buildings and stuns mechanoids, through the game's own EMP damage.</summary>
    void Pulse(Thing thing)
    {
        if (!emp || thing.Destroyed || !thing.Spawned) return;
        bool stunnable = thing is Pawn pawn ? pawn.RaceProps.IsMechanoid : thing.TryGetComp<CompStunnable>() != null;
        if (!stunnable) return;
        thing.TakeDamage(new DamageInfo(DamageDefOf.EMP, rules.empAmount, 0f, -1f, Launcher));
        Stunned++;
    }

    void HitCell(IntVec3 cell, float distance)
    {
        StrikeZone zone = ZoneAt(distance);
        if (IsSheltered(cell, zone)) return;
        if (zone == StrikeZone.Fireball)
        {
            TerrainGrid terrain = Map.terrainGrid;
            if (terrain.CanRemoveTopLayerAt(cell))
            {
                terrain.RemoveTopLayer(cell, doLeavings: false);
                Scoured++;
            }
            if (ash != null) FilthMaker.TryMakeFilth(cell, Map, ash);
            Pollute(cell);
            return;
        }
        if (zone == StrikeZone.Blast)
        {
            if (ash != null && Rand.Chance(rules.blastAshChance)) FilthMaker.TryMakeFilth(cell, Map, ash);
            if (Rand.Chance(rules.blastPollutionChance)) Pollute(cell);
        }
        float chance = StrikeRule.FireChance(fireBase, distance, Full ? zones.blastRadius : 0f, zones.reach, rules.fireEdgeFactor);
        if (chance > 0f && Rand.Chance(chance) && FireUtility.TryStartFireIn(cell, Map, rules.fireSize, Launcher)) Fires++;
    }

    void Pollute(IntVec3 cell)
    {
        if (!pollution || Map.pollutionGrid.IsPolluted(cell)) return;
        Map.pollutionGrid.SetPolluted(cell, true, silent: true);
        Polluted++;
    }

    protected override void Finish()
    {
        if (IsTarget && !fadeStarted) StartFade();
        string line = MissilePatchesMod.LogPrefix + " " + Label + ": " + Total + " things and " + cells.Length + " cells in "
            + SecondsSinceStart.ToString("0.0") + " s over " + Frames + " frame" + (Frames == 1 ? "" : "s") + " (" + WorkMs.ToString("0") + " ms of work)"
            + ": vaporised " + Vaporised + ", wrecked " + Wrecked + ", damaged " + Damaged + ", killed " + Killed + ", burned " + Burned
            + ", blinded " + Blinded + ", irradiated " + Irradiated + ", stunned " + Stunned + ", plants wilted " + Wilted
            + ", sheltered " + Sheltered + " (" + shelter + "), already gone " + Gone + ", roofs stripped " + RoofsStripped
            + ", ground scoured " + Scoured + ", cells polluted " + Polluted + ", fires " + Fires
            + (Errors > 0 ? ", " + Errors + " error" + (Errors == 1 ? "" : "s") + " swallowed; the first: " + FirstError : "") + ".";
        if (Errors > 0) Log.Warning(line); else Log.Message(line);
        if (longMessageShown)
        {
            Messages.Message("PhobosMissilePatches_BlastSpent".Translate(PlaceLabel.Named("PLACE")), MessageTypeDefOf.NeutralEvent, historical: false);
        }
    }

    protected override void Dropped()
    {
        if (IsTarget && !fadeStarted) StartFade();
        Log.Message(MissilePatchesMod.LogPrefix + " " + Label + " was dropped at " + Next + " of " + Count + ".");
    }
}
