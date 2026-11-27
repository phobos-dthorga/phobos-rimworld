using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Environment;
using Phobos.Framework.Rules;
using Phobos.Framework.Work;
using RimWorld;
using Verse;

namespace Phobos.Framework.Health;

/// <summary>
/// A prompt radiation dose from a blast that carries a <see cref="PhobosRadiationBurst"/> (Framework 0.358.0; docs/development/radiation-bursts.md;
/// owner, 27 Nov 2026: our radiation for IRBM's tactical warheads and the other nuclear and antimatter weapons on the owner's list). Every
/// flesh person on the map within the burst's reach takes the dose by distance (<see cref="RadiationBurstRule"/>), cut by their cover
/// (<see cref="RadiationShelterRule"/>) or, aboard a vehicle, by its hull's share; given in grays through <see cref="RadiationDoses"/>, so only
/// while D'thorga Medical's model is on. Call it before the blast it belongs to: the dose arrives with the flash, and roofs are judged before
/// they fall. The player's people dosed are told in one letter per frame, however many bursts made it (a cluster of warheads lands at once).
/// </summary>
public static class RadiationBursts
{
    const string RimatomicsId = "Dubwise.Rimatomics";
    const string RimatomicsFallout = "NuclearFallout";

    static readonly List<(Pawn pawn, float grays)> toTell = new();
    static bool tellingQueued;

    /// <summary>Whether a burst of this kind gives a dose now: its setting on and a radiation model taking doses.</summary>
    public static bool On(RadiationBurstKind kind)
    {
        FrameworkSettings s = FrameworkMod.Settings;
        bool setting = kind == RadiationBurstKind.Antimatter ? s.radiationFromAntimatter : s.radiationFromNuclear;
        return setting && RadiationDoses.Active;
    }

    /// <summary>The burst a def carries, if any (a ThingDef, a warhead, a support type).</summary>
    public static PhobosRadiationBurst? Of(Def? def) => def?.modExtensions == null ? null : def.GetModExtension<PhobosRadiationBurst>();

    /// <summary>Every Def of any kind that carries a burst, for the dev action: ThingDefs, IRBM's warheads, Legion's support types.</summary>
    public static IEnumerable<Def> Carriers()
    {
        foreach (Type type in GenDefDatabase.AllDefTypesWithDatabases())
        {
            foreach (Def def in GenDefDatabase.GetAllDefsInDatabaseForDef(type))
            {
                if (Of(def) != null) yield return def;
            }
        }
    }

    /// <summary>The explosion radius a ThingDef gives itself, as a projectile or through the game's explosive comp; 0 for anything else.</summary>
    public static float ExplosionRadius(Def def) => def is ThingDef thing
        ? thing.projectile?.explosionRadius ?? thing.GetCompProperties<CompProperties_Explosive>()?.explosiveRadius ?? 0f
        : 0f;

    /// <summary>
    /// Gives the burst's dose on <paramref name="map"/>, centred on <paramref name="centre"/> (ignored for a whole-map burst), with
    /// <paramref name="explosionRadius"/> as its edge where the burst names none. Returns how many people took a dose. Never throws: a fault
    /// is logged, and the blast it belongs to carries on.
    /// </summary>
    public static int Burst(Map? map, IntVec3 centre, PhobosRadiationBurst? burst, float explosionRadius, string source)
    {
        if (map == null || burst == null || !On(burst.kind)) return 0;
        try
        {
            return Give(map, centre, burst, explosionRadius, source);
        }
        catch (Exception error)
        {
            FrameworkMod.Logger.Error("A radiation burst (" + source + ") failed; its blast carries on. " + error);
            return 0;
        }
    }

    static int Give(Map map, IntVec3 centre, PhobosRadiationBurst burst, float explosionRadius, string source)
    {
        float edge = burst.edgeRadius > 0f ? burst.edgeRadius : Math.Max(1f, explosionRadius);
        float reach = burst.wholeMap ? float.MaxValue : RadiationBurstRule.Reach(edge, burst.graysAtEdge, burst.airLength);
        // A burst reaching across half the map or more brings out people a mod keeps off it first (Framework 0.364.0; a smaller one does
        // not, as bringing a base's reserve out for a shell would change the base for little).
        if (burst.wholeMap || reach * 2f >= Math.Min(map.Size.x, map.Size.z)) HeldPawns.BringOut(map, "a radiation burst (" + source + ")");
        int dosed = 0, sheltered = 0, aboard = 0;
        float most = 0f;
        foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned.ToList())
        {
            if (pawn.Dead) continue;
            float distance = burst.wholeMap ? 0f : (pawn.Position - centre).LengthHorizontal;
            if (distance > reach) continue;
            float outside = burst.wholeMap ? burst.graysAtEdge : RadiationBurstRule.Grays(distance, edge, burst.graysAtEdge, burst.airLength);
            float pass = RadiationShelterRule.DosePass(ShelterCovers.Of(pawn), burst.buildingsShield);
            if (pass < 1f) sheltered++;
            if (LargeBlasts.TakerFor(pawn) is { } taker)
            {
                // A vehicle takes no dose; the people inside take what its hull lets through, and what its own cover does.
                foreach ((Pawn inside, float share) in taker.Aboard(pawn))
                {
                    if (Dose(inside, outside * pass * share, source, ref most)) { dosed++; aboard++; }
                }
                continue;
            }
            if (Dose(pawn, outside * pass, source, ref most)) dosed++;
        }
        int falloutDays = Fallout(map, burst, source);
        FrameworkMod.Logger.Info("Radiation burst (" + source + ", " + burst.kind + ") on " + (map.Parent?.Label ?? "a map")
            + (burst.wholeMap ? ", the whole map" : " at " + centre + ", edge " + edge.ToString("0.#") + " cells, reach " + reach.ToString("0"))
            + ": " + dosed + " dosed (" + aboard + " aboard vehicles, " + sheltered + " under cover), the most " + most.ToString("0.00") + " Gy"
            + (falloutDays > 0 ? ", fallout for " + falloutDays + " days" : "") + ".");
        string variant = burst.kind.ToString();
        if (dosed > 0 && Seen.Wants("health.radiation-burst", null, variant))
        {
            Seen.Note("health.radiation-burst", null, source + ": " + dosed + " dosed, the most " + most.ToString("0.00") + " Gy", variant);
        }
        return dosed;
    }

    static bool Dose(Pawn pawn, float grays, string source, ref float most)
    {
        if (grays < RadiationBurstRule.LeastGrays || pawn.Dead || pawn.RaceProps is not { IsFlesh: true }) return false;
        if (!RadiationDoses.Give(pawn, grays, source)) return false;
        if (grays > most) most = grays;
        if (RadiationDoseLetter.Tells(pawn)) Tell(pawn, grays);
        return true;
    }

    /// <summary>Dubs Rimatomics' fallout, by name, unless the map has it already: the days registered, or 0.</summary>
    /// <summary>The burst's fallout: ours, dosing by age and cover, at its rate for its days (Framework 0.364.0); and Dubs Rimatomics' own
    /// condition, by name, for its sky and plants, unless the map has it already (its dose stands down where ours lies). The days laid, or 0.</summary>
    static int Fallout(Map map, PhobosRadiationBurst burst, string source)
    {
        if (burst.falloutDays.max <= 0f) return 0;
        float days = burst.falloutDays.RandomInRange;
        if (days <= 0f) return 0;
        FalloutFields.Lay(map, burst.falloutGraysPerHour, days, source);
        if (OptionalMods.IsActive(RimatomicsId) && DefDatabase<GameConditionDef>.GetNamedSilentFail(RimatomicsFallout) is { } def
            && !map.GameConditionManager.ConditionIsActive(def))
        {
            map.GameConditionManager.RegisterCondition(GameConditionMaker.MakeCondition(def, (int)(days * GenDate.TicksPerDay)));
        }
        return (int)Math.Ceiling(days);
    }

    static void Tell(Pawn pawn, float grays)
    {
        toTell.Add((pawn, grays));
        if (tellingQueued) return;
        tellingQueued = true;
        FrameWorkQueue.Enqueue(new Telling());
    }

    /// <summary>Sends the letter at the next frame's work, so every burst of one moment shares it.</summary>
    sealed class Telling : FrameWork
    {
        bool done;

        public override string Label => "Telling radiation doses";
        public override bool Finished => done;

        protected override void Start() { }

        protected internal override void Step() => done = true;

        protected internal override void Finish() => SendNow();

        protected internal override void Dropped() => SendNow();
    }

    static void SendNow()
    {
        tellingQueued = false;
        if (toTell.Count == 0) return;
        var doses = toTell.ToList();
        toTell.Clear();
        RadiationDoseLetter.Send(doses, "PhobosFramework_BurstDoseLetterLabel".Translate());
    }
}
