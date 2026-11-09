using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Phobos.Triage.Defs;
using Phobos.Triage.Rules;
using Verse;

namespace Phobos.Triage.Services;

/// <summary>
/// Runs the planner on the rules Def's interval and holds its results for the work giver and the inspect line. Derived state:
/// nothing is saved; the first pass after a load rebuilds it. Maps are offset from one another so they never plan on the same tick.
/// </summary>
public sealed class MapComponent_PhobosTriage : MapComponent
{
    // Weak: loading a game never calls MapRemoved, and a plain dictionary would keep the whole of every map loaded this session.
    static readonly ConditionalWeakTable<Map, MapComponent_PhobosTriage> byMap = new();
    readonly List<TriageEntry> entries = new();
    readonly Dictionary<Pawn, TriageEntry> byPawn = new();
    readonly List<Thing> patientsWithBills = new();

    public MapComponent_PhobosTriage(Map map) : base(map)
    {
        byMap.AddOrUpdate(map, this);
    }

    public override void MapRemoved()
    {
        byMap.Remove(map);
    }

    public static MapComponent_PhobosTriage? For(Map? map) => map != null && byMap.TryGetValue(map, out MapComponent_PhobosTriage? comp) ? comp : null;

    /// <summary>Patients in a bed with a bill to do, scored by <see cref="ScoreOf"/>.</summary>
    public IReadOnlyList<Thing> PatientsWithBills => patientsWithBills;
    public IReadOnlyList<TriageEntry> Entries => entries;

    public TriageEntry? EntryOf(Pawn pawn) => byPawn.TryGetValue(pawn, out TriageEntry? entry) ? entry : null;

    /// <summary>The doctors' order of work: a patient with no tag sits at the minor band's base.</summary>
    public float ScoreOf(Pawn pawn) => EntryOf(pawn)?.Score ?? TriageRule.Score(TriageCategory.Minor, float.PositiveInfinity, 24f);

    public override void MapComponentTick()
    {
        int interval = PhobosTriageRulesDef.Current.recomputeIntervalTicks;
        if (interval <= 0 || (Find.TickManager.TicksGame + map.uniqueID * 37) % interval != 0) return;
        Run();
    }

    /// <summary>One planning pass now. Public for the dev action.</summary>
    public void Run()
    {
        TriagePlanner.Run(map, entries, patientsWithBills);
        byPawn.Clear();
        for (int i = 0; i < entries.Count; i++) byPawn[entries[i].Pawn] = entries[i];
    }
}
