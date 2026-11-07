using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Framework.Health;

public class CompProperties_PhobosPatient : CompProperties
{
    public CompProperties_PhobosPatient() => compClass = typeof(CompPhobosPatient);
}

/// <summary>
/// The one comp every humanlike pawn carries for the Phobos mods' per-pawn care choices (<see cref="PatientPreferences"/>). It
/// saves only what the player set, as plain fields on the pawn, so a save loads without the framework and without a line.
/// It draws a gizmo per registered preference for the pawns the doctors look after, and the registered inspect lines. No tick.
/// </summary>
public class CompPhobosPatient : ThingComp
{

    List<string> keys = new();
    List<int> values = new();

    /// <summary>The pawn, or null when the comp sits on something else: some mods give corpses the comp too (an owner's save, 7 Nov
    /// 2026, showed it on a dead colonist's corpse), and a dead person has no care choices to show.</summary>
    Pawn? Pawn => parent as Pawn;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Collections.Look(ref keys, "PhobosPatient_ChoiceKeys", LookMode.Value);
        Scribe_Collections.Look(ref values, "PhobosPatient_ChoiceValues", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            keys ??= new List<string>();
            values ??= new List<int>();
            if (keys.Count != values.Count)
            {
                keys.Clear();
                values.Clear();
            }
        }
    }

    /// <summary>The player's saved choice for a key, or null for none (the mod's default applies).</summary>
    public int? SavedChoice(string key)
    {
        int index = keys.IndexOf(key);
        return index < 0 ? null : values[index];
    }

    /// <summary>Saves a choice, or clears it with null so the mod's default applies again.</summary>
    public void SetChoice(string key, int? choice)
    {
        int index = keys.IndexOf(key);
        if (choice == null)
        {
            if (index >= 0)
            {
                keys.RemoveAt(index);
                values.RemoveAt(index);
            }
            return;
        }
        if (index < 0)
        {
            keys.Add(key);
            values.Add(choice.Value);
        }
        else
        {
            values[index] = choice.Value;
        }
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (Pawn is not Pawn pawn || Patients.GroupOf(pawn) == null) yield break;
        IReadOnlyList<PatientPreference> preferences = PatientPreferences.All;
        for (int i = 0; i < preferences.Count; i++)
        {
            PatientPreference preference = preferences[i];
            if (preference.Applies != null && !preference.Applies(pawn)) continue;
            int choice = PatientPreferences.Choice(pawn, preference);
            yield return new Command_Action
            {
                defaultLabel = preference.LabelKey.Translate(preference.OptionKeys[choice].Translate().Named("CHOICE")),
                defaultDesc = preference.DescKey.Translate(),
                icon = PatientPreferences.IconFor(preference),
                action = () => Find.WindowStack.Add(new FloatMenu(PatientPreferences.MenuFor(pawn, preference))),
            };
        }
    }

    public override string? CompInspectStringExtra()
    {
        IReadOnlyList<System.Func<Pawn, string?>> providers = PatientPreferences.InspectLines;
        if (providers.Count == 0 || Pawn is not Pawn pawn || pawn.Dead) return null;
        StringBuilder? text = null;
        for (int i = 0; i < providers.Count; i++)
        {
            string? line = providers[i](pawn);
            if (string.IsNullOrEmpty(line)) continue;
            text ??= new StringBuilder();
            if (text.Length > 0) text.Append('\n');
            text.Append(line);
        }
        return text?.ToString();
    }
}

/// <summary>
/// Puts the comp on every humanlike race once the Defs are loaded. Code rather than XML because an XML patch reaches only the
/// Defs that spell out their intelligence; races from Humanoid Alien Races and races inheriting it would be missed.
/// </summary>
[StaticConstructorOnStartup]
public static class PatientCompInjector
{
    static PatientCompInjector()
    {
        int added = 0;
        List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading;
        for (int i = 0; i < defs.Count; i++)
        {
            ThingDef def = defs[i];
            // Only things that really are pawns: a corpse or other thing that carries a race must not get it.
            if (def.race == null || !def.race.Humanlike || def.thingClass == null || !typeof(Pawn).IsAssignableFrom(def.thingClass)
                || def.HasComp(typeof(CompPhobosPatient))) continue;
            def.comps ??= new List<CompProperties>();
            var props = new CompProperties_PhobosPatient();
            def.comps.Add(props);
            props.ResolveReferences(def);
            added++;
        }
        Added = added;
    }

    /// <summary>How many races got the comp, for the load line.</summary>
    public static int Added { get; private set; }
}
