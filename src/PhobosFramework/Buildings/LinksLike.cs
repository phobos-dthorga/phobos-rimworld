using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Phobos.Framework.Buildings;

/// <summary>
/// Marks a building that should take every facility, and every stat it lacks, that another building has once all mods are loaded
/// (Framework 0.257.0). Other mods add their bedside facilities (an IV drip, a vitals centre, surgical tools) to the game's hospital
/// bed by its name, so a bed of ours built on it would miss them all; with this extension it gets whatever the named bed has,
/// including from mods added later, without our naming any of them.
/// <code>
/// &lt;li Class="Phobos.Framework.Buildings.PhobosLinksLikeExtension"&gt;&lt;like&gt;HospitalBed&lt;/like&gt;&lt;/li&gt;
/// </code>
/// </summary>
public class PhobosLinksLikeExtension : DefModExtension
{
    /// <summary>The building whose facilities and stats this one takes.</summary>
    public ThingDef? like;

    /// <summary>Whether to take the stats the named building has and this one does not (its own figures always stand).</summary>
    public bool missingStats = true;

    /// <summary>
    /// When given, only facilities that change at least one of these stats are taken (Framework 0.259.0): the hyperbaric chamber takes a
    /// hospital bed's facilities that better tending or immunity, and leaves its comfort furniture. A facility is chosen by what it
    /// does, never by its name (the owner's rule: match effects, not names), and a facility taken brings all its offsets.
    /// </summary>
    public List<StatDef>? onlyStats;
}

/// <summary>
/// On a building that took facilities from another: notes the first of them seen linked, when the building is selected (test rows 428
/// and 432). Added in code by <see cref="LinksLike"/>, never written in XML. It never ticks and saves nothing.
/// </summary>
public class CompPhobosLinksLike : ThingComp
{
    public override string? CompInspectStringExtra()
    {
        LinksLike.NoteLinked(parent);
        return null;
    }
}

/// <summary>
/// Applies every <see cref="PhobosLinksLikeExtension"/> once at start-up. The game fills each facility's list of buildings it can link
/// to while Defs load, from the buildings that name it, so a facility added here is also added to that facility's own list; without
/// both, the link line would show but the facility would never link.
/// </summary>
[StaticConstructorOnStartup]
public static class LinksLike
{
    /// <summary>The facilities each building took from another, for the seen-in-play note. Derived from Defs, never saved.</summary>
    static readonly Dictionary<ThingDef, HashSet<ThingDef>> taken = new();

    static LinksLike()
    {
        List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading;
        for (int i = 0; i < defs.Count; i++)
        {
            ThingDef def = defs[i];
            PhobosLinksLikeExtension? ext = def.GetModExtension<PhobosLinksLikeExtension>();
            if (ext?.like == null || ext.like == def) continue;
            Buildings++;
            int facilities = TakeFacilities(def, ext.like, ext.onlyStats);
            Facilities += facilities;
            if (ext.missingStats) Stats += TakeMissingStats(def, ext.like);
            if (facilities == 0) continue;
            var note = new CompProperties { compClass = typeof(CompPhobosLinksLike) };
            def.comps.Add(note);
            note.ResolveReferences(def);
        }
    }

    /// <summary>How many buildings took another's links, and how many facilities and stats they took in all, for the load line.</summary>
    public static int Buildings { get; private set; }
    public static int Facilities { get; private set; }
    public static int Stats { get; private set; }

    /// <summary>
    /// Notes, once a session per building, a facility it took from another being linked to it in play, with the building's kind as the
    /// case. With the seen-in-play setting off it reads one setting and returns.
    /// </summary>
    public static void NoteLinked(ThingWithComps building)
    {
        if (!Seen.Wants("framework.links-like.linked", building, building.def.defName)) return;
        if (!taken.TryGetValue(building.def, out HashSet<ThingDef>? took)) return;
        List<Thing>? linked = building.GetComp<CompAffectedByFacilities>()?.LinkedFacilitiesListForReading;
        if (linked == null) return;
        foreach (Thing facility in linked)
        {
            if (!took.Contains(facility.def)) continue;
            Seen.Note("framework.links-like.linked", building, facility.def.defName, building.def.defName);
            return;
        }
    }

    /// <summary>The load line's words for what was taken.</summary>
    public static string Describe() =>
        Buildings + " building(s) linking like another: " + Facilities + " facilit" + (Facilities == 1 ? "y" : "ies") + " and "
        + Stats + " stat" + (Stats == 1 ? "" : "s") + " taken";

    static int TakeFacilities(ThingDef def, ThingDef like, List<StatDef>? onlyStats)
    {
        List<ThingDef>? theirs = like.GetCompProperties<CompProperties_AffectedByFacilities>()?.linkableFacilities;
        if (theirs == null || theirs.Count == 0) return 0;
        CompProperties_AffectedByFacilities? mine = def.GetCompProperties<CompProperties_AffectedByFacilities>();
        if (mine == null)
        {
            mine = new CompProperties_AffectedByFacilities();
            def.comps ??= new List<CompProperties>();
            def.comps.Add(mine);
        }
        mine.linkableFacilities ??= new List<ThingDef>();
        int added = 0;
        foreach (ThingDef facility in theirs)
        {
            if (facility == null || mine.linkableFacilities.Contains(facility)) continue;
            CompProperties_Facility? props = facility.GetCompProperties<CompProperties_Facility>();
            if (onlyStats != null && !ChangesAny(props, onlyStats)) continue;
            mine.linkableFacilities.Add(facility);
            if (!taken.TryGetValue(def, out HashSet<ThingDef>? took)) taken[def] = took = new HashSet<ThingDef>();
            took.Add(facility);
            if (props != null)
            {
                props.linkableBuildings ??= new List<ThingDef>();
                if (!props.linkableBuildings.Contains(def)) props.linkableBuildings.Add(def);
            }
            added++;
        }
        return added;
    }

    /// <summary>Whether a facility changes at least one of the stats, by the offsets on its Def.</summary>
    static bool ChangesAny(CompProperties_Facility? props, List<StatDef> stats)
    {
        if (props?.statOffsets == null) return false;
        foreach (StatModifier offset in props.statOffsets)
        {
            if (offset?.stat != null && stats.Contains(offset.stat)) return true;
        }
        return false;
    }

    static int TakeMissingStats(ThingDef def, ThingDef like)
    {
        if (like.statBases == null) return 0;
        def.statBases ??= new List<StatModifier>();
        int added = 0;
        foreach (StatModifier theirs in like.statBases)
        {
            if (theirs?.stat == null || def.statBases.StatListContains(theirs.stat)) continue;
            def.statBases.Add(new StatModifier { stat = theirs.stat, value = theirs.value });
            added++;
        }
        return added;
    }
}
