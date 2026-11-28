using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Phobos.Framework.Incidents;

/// <summary>
/// One hold per raid (Framework 0.383.0; the owner's card of 28 Nov 2026, docs/development/radar-and-air-defence.md, row RD7). Three
/// mods can each hold the same raid back to give a warning: the Radio Framework's allied warning, Dubs Rimatomics' radar with its ATOM
/// upgrade, and Fortified Features Framework's radars, which take falling drop pods out of the sky for a while. The first hold counts:
/// while a raid that has already been held is being let go, none of the three holds it again.
/// <para>Why, read from their code on 28 Nov 2026: Rimatomics fires a raid it held as if the storyteller had just sent it, so the radio
/// held it again; and when Rimatomics held a raid the radio had queued, the game's queue tried it again a few game minutes later, past
/// Rimatomics, so the raid arrived then, and again when Rimatomics let its own copy go, over and over while both ran.</para>
/// <para>A raid one of our mods holds is recorded with <see cref="MarkHeld"/> (saved, so a save and load in between changes nothing);
/// Rimatomics' own releases are recognised from where they are fired. Cost: nothing until a raid fires; then one look through a list
/// of the few raids held in the last day.</para>
/// </summary>
public static class RaidHolds
{
    static int releasing;

    /// <summary>True while a raid that has already had its hold is being let go: nothing holds it again.</summary>
    public static bool Releasing => releasing > 0;

    internal static void BeginRelease() => releasing++;

    internal static void EndRelease()
    {
        if (releasing > 0) releasing--;
    }

    /// <summary>Records a raid one of our mods held and queued to fire at <paramref name="fireTick"/>, so that nothing holds it again.</summary>
    public static void MarkHeld(FiringIncident fi, int fireTick) => GameComponent_PhobosRaidHolds.Current?.Add(fi, fireTick);

    /// <summary>
    /// Called when a stand-down is seen working: another mod's hold let a raid go because it had been held already.
    /// </summary>
    internal static void NoteStoodDown(string whose)
    {
        if (Seen.Wants("framework.raid-hold-once", null, whose)) Seen.Note("framework.raid-hold-once", null, whose + " let a raid already held go", whose);
    }
}

/// <summary>One raid one of our mods held, as the game's own queue will fire it: its incident, target, faction and points, and when.</summary>
public sealed class PhobosHeldRaid : IExposable
{
    /// <summary>Raid points are saved as text and read back; two raids this close in points, of one faction on one map, are the same raid.</summary>
    const float PointsTolerance = 0.5f;

    public IncidentDef? def;
    public Map? map;
    public Faction? faction;
    public float points;
    public int fireTick;

    public PhobosHeldRaid() { }

    public PhobosHeldRaid(FiringIncident fi, int fireTick)
    {
        def = fi.def;
        map = fi.parms.target as Map;
        faction = fi.parms.faction;
        points = fi.parms.points;
        this.fireTick = fireTick;
    }

    public bool Matches(FiringIncident fi) =>
        fi.def == def && fi.parms != null && fi.parms.target == map && fi.parms.faction == faction && Math.Abs(fi.parms.points - points) <= PointsTolerance;

    /// <summary>The game's queue keeps trying a queued raid for a day after it is due (RetryDurationTicks); then it is gone.</summary>
    public bool Expired(int now) => def == null || map == null || now > fireTick + GenDate.TicksPerDay + GenDate.TicksPerHour;

    public void ExposeData()
    {
        Scribe_Defs.Look(ref def, "def");
        Scribe_References.Look(ref map, "map");
        Scribe_References.Look(ref faction, "faction");
        Scribe_Values.Look(ref points, "points");
        Scribe_Values.Look(ref fireTick, "fireTick");
    }
}

/// <summary>The raids our mods have held and not yet let go (Framework 0.383.0). Saved; a handful at most.</summary>
public sealed class GameComponent_PhobosRaidHolds : GameComponent
{
    List<PhobosHeldRaid> held = new();

    public GameComponent_PhobosRaidHolds(Game game) { }

    public static GameComponent_PhobosRaidHolds? Current => Verse.Current.Game?.GetComponent<GameComponent_PhobosRaidHolds>();

    internal void Add(FiringIncident fi, int fireTick)
    {
        Prune();
        held.Add(new PhobosHeldRaid(fi, fireTick));
    }

    /// <summary>The held raid this firing lets go, or null.</summary>
    internal PhobosHeldRaid? Find(FiringIncident fi)
    {
        if (held.Count == 0) return null;
        Prune();
        foreach (PhobosHeldRaid raid in held)
        {
            if (raid.Matches(fi)) return raid;
        }
        return null;
    }

    internal void Remove(PhobosHeldRaid raid) => held.Remove(raid);

    void Prune()
    {
        int now = Verse.Find.TickManager.TicksGame;
        held.RemoveAll(raid => raid.Expired(now));
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref held, "PhobosRaidHolds_Held", LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            held ??= new List<PhobosHeldRaid>();
            held.RemoveAll(raid => raid == null || raid.def == null || raid.map == null);
        }
    }
}

/// <summary>
/// Opens the release while the game's queue fires a raid one of our mods held (Framework 0.383.0): a prefix and finalizer on
/// <see cref="Storyteller.TryFire"/>, which only look, never skip it. When the raid comes, its record goes.
/// </summary>
[HarmonyPatch(typeof(Storyteller), nameof(Storyteller.TryFire))]
internal static class Patch_Storyteller_TryFire_RaidHolds
{
    static void Prefix(FiringIncident fi, bool queued, out PhobosHeldRaid? __state)
    {
        __state = null;
        if (!queued || fi?.def == null || GameComponent_PhobosRaidHolds.Current?.Find(fi) is not { } raid) return;
        __state = raid;
        RaidHolds.BeginRelease();
    }

    static void Finalizer(PhobosHeldRaid? __state, bool __result)
    {
        if (__state == null) return;
        RaidHolds.EndRelease();
        if (__result) GameComponent_PhobosRaidHolds.Current?.Remove(__state);
    }
}

/// <summary>
/// Dubs Rimatomics lets its held raids go from RimatomicsResearch.IncidentQueueTick, through Storyteller.TryFire as a fresh firing
/// (read 28 Nov 2026). Everything it fires there has had its hold, so the release is open for it. Only with Rimatomics active.
/// </summary>
[HarmonyPatch]
internal static class Patch_Rimatomics_IncidentQueueTick_RaidHolds
{
    const string TypeName = "Rimatomics.RimatomicsResearch";

    static bool Prepare() => OptionalMods.IsActive(OptionalMods.Rimatomics) && TargetMethod() != null;

    static MethodBase? TargetMethod() => AccessTools.TypeByName(TypeName) is { } type ? AccessTools.Method(type, "IncidentQueueTick") : null;

    static void Prefix() => RaidHolds.BeginRelease();

    static void Finalizer() => RaidHolds.EndRelease();
}

/// <summary>
/// Dubs Rimatomics' ATOM hold (RimatomicsResearch.DonkeyRubarb, read 28 Nov 2026) is skipped for a raid already held, leaving its answer
/// "let the raid come" as the caller set it. A prefix that skips the original: the reason is the one-hold rule above, and nothing else of
/// Rimatomics' changes. Only with Rimatomics active.
/// </summary>
[HarmonyPatch]
internal static class Patch_Rimatomics_DonkeyRubarb_RaidHolds
{
    const string TypeName = "Rimatomics.RimatomicsResearch";

    static bool Prepare() => OptionalMods.IsActive(OptionalMods.Rimatomics) && TargetMethod() != null;

    static MethodBase? TargetMethod() => AccessTools.TypeByName(TypeName) is { } type ? AccessTools.Method(type, "DonkeyRubarb") : null;

    static bool Prefix()
    {
        if (!RaidHolds.Releasing) return true;
        RaidHolds.NoteStoodDown("Rimatomics");
        return false;
    }
}

/// <summary>
/// Fortified Features Framework holds a falling drop pod when a radar of a faction hostile to its crew runs on the map, asking
/// MapComponent_AirDefense.ResolveDefender which faction catches it (read 28 Nov 2026). While a raid already held arrives, nobody catches
/// it. Missiles ask the same question, but never while a raid is being let go. Only with Fortified Features Framework active.
/// </summary>
[HarmonyPatch]
internal static class Patch_Fortified_ResolveDefender_RaidHolds
{
    const string TypeName = "Fortified.MapComponent_AirDefense";

    static bool Prepare() => OptionalMods.IsActive(OptionalMods.FortifiedFeatures) && TargetMethod() != null;

    static MethodBase? TargetMethod() =>
        AccessTools.TypeByName(TypeName) is { } type ? AccessTools.Method(type, "ResolveDefender", new[] { typeof(Faction) }) : null;

    static void Postfix(ref Faction? __result)
    {
        if (!RaidHolds.Releasing || __result == null) return;
        __result = null;
        RaidHolds.NoteStoodDown("Fortified");
    }
}
