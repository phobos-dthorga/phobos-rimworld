using System;
using System.Collections.Generic;
using Phobos.Dosing.Needs;
using Phobos.Dosing.Rules;
using Verse;

namespace Phobos.Dosing.Defs;

/// <summary>
/// One reason a person is given, or takes, something from the colony's stock (Medical 0.28.0; docs/development/medical-care-needs.md):
/// a row of the care needs table. The Def says how serious the need is, in what order doctors look at it, whether it keeps a reserve
/// and whether an unmet one is shown; its worker reads the patient and matches the items. Another mod adds a need with a Def and a
/// worker class of its own.
/// </summary>
public class PhobosCareNeedDef : Def
{
    /// <summary>The class that reads who has this need and what answers it.</summary>
    public Type? workerClass;
    /// <summary>Doctors look at needs in this order, lowest first; the first with something to give decides the dose.</summary>
    public float order;
    /// <summary>What is at stake: an addictive item needs serious or worse, a rare item life-threatening. A worker may raise it for one
    /// patient (a cure is life-threatening when the condition can kill).</summary>
    public CareSeriousness seriousness = CareSeriousness.Serious;
    /// <summary>Who acts on it where the player has no setting for it (a need another mod adds).</summary>
    public CareMode defaultMode = CareMode.DoctorsOnly;
    /// <summary>The need never takes an item below the number the player keeps back.</summary>
    public bool usesReserve;
    /// <summary>An alert names a person with this need while nothing in stock answers it.</summary>
    public bool alertWhenUnmet = true;
    /// <summary>Whether an animal the doctors look after can have this need (Medical 0.34.0); people only when false.</summary>
    public bool forAnimals;

    [Unsaved] CareNeedWorker? worker;

    public CareNeedWorker Worker
    {
        get
        {
            if (worker == null)
            {
                // A Def with no worker class, or one that cannot be made, gets the base worker: a need nobody has (ConfigErrors says why).
                worker = Activator.CreateInstance(workerClass ?? typeof(CareNeedWorker)) as CareNeedWorker ?? new CareNeedWorker();
                worker.def = this;
            }
            return worker;
        }
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (workerClass == null) yield return "has no workerClass";
        else if (!typeof(CareNeedWorker).IsAssignableFrom(workerClass)) yield return "workerClass " + workerClass + " is not a CareNeedWorker";
    }
}

/// <summary>The care needs in the order doctors look at them. Built on first use, after the Defs load.</summary>
public static class CareNeeds
{
    static List<PhobosCareNeedDef>? ordered;

    public static IReadOnlyList<PhobosCareNeedDef> Ordered
    {
        get
        {
            if (ordered == null)
            {
                ordered = new List<PhobosCareNeedDef>(DefDatabase<PhobosCareNeedDef>.AllDefsListForReading);
                ordered.Sort((a, b) => a.order != b.order ? a.order.CompareTo(b.order) : string.CompareOrdinal(a.defName, b.defName));
            }
            return ordered;
        }
    }
}
