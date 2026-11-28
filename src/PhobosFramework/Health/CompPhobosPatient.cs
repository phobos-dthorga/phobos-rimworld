using System;
using System.Collections.Generic;
using System.Text;
using Phobos.Framework.Logging;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Framework.Health;

/// <summary>Holds a person's care choices that the D'thorga mods offer (shown as buttons on the people the doctors look after) and the
/// inspect lines they register. D'thorga Framework adds it in code at start-up to every pawn race (humanlike ones, Humanoid Alien
/// Races' included, until 0.381.1); it has no fields, and a modder never writes it in XML: register a preference with
/// <c>PatientPreferences</c> instead.</summary>
public class CompProperties_PhobosPatient : CompProperties
{
    public CompProperties_PhobosPatient() => compClass = typeof(CompPhobosPatient);
}

/// <summary>
/// The one comp every pawn carries for the Phobos mods' per-pawn care choices (<see cref="PatientPreferences"/>) and other mods'
/// buttons. It saves only what the player set, as plain fields on the pawn, so a save loads without the framework and without a
/// line, and writes nothing at all for a pawn with no choice set. It draws a gizmo per registered preference for the pawns the
/// doctors look after, other mods' buttons for any pawn (<see cref="Interface.PawnCommands"/>, Framework 0.381.0), and the
/// registered inspect lines, each registered function guarded so one fault never empties the bar or the pane (0.381.1). No tick.
/// <para>On every pawn race from 0.381.1 (humanlike only before), so a mod that makes some other kind of pawn a colonist (sapient
/// animals) still gets the buttons. Cost per pawn that has no button: the game's own empty tick, interval and draw calls on one
/// more comp, a few nanoseconds; the buttons and lines are asked for only while a pawn is selected.</para>
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
        // Nothing written for a pawn with no choice set (0.381.1, now that every animal and vehicle has the comp); a missing node
        // loads as null, put right below.
        if (Scribe.mode != LoadSaveMode.Saving || keys.Count > 0)
        {
            Scribe_Collections.Look(ref keys, "PhobosPatient_ChoiceKeys", LookMode.Value);
            Scribe_Collections.Look(ref values, "PhobosPatient_ChoiceValues", LookMode.Value);
        }
        if (Scribe.mode != LoadSaveMode.Saving)
        {
            keys ??= new List<string>();
            values ??= new List<int>();
        }
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
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
        if (Pawn is not Pawn pawn) yield break;
        // Other mods' buttons for any pawn (Framework 0.381.0), before the care choices, which only the doctors' charges get. Each
        // provider is guarded (0.381.1): one that fails is reported once and the rest still draw.
        List<Gizmo>? others = Interface.PawnCommands.GizmosFor(pawn);
        if (others != null)
        {
            for (int i = 0; i < others.Count; i++) yield return others[i];
        }
        if (Patients.GroupOf(pawn) == null) yield break;
        IReadOnlyList<PatientPreference> preferences = PatientPreferences.All;
        for (int i = 0; i < preferences.Count; i++)
        {
            if (ButtonFor(pawn, preferences[i]) is { } button) yield return button;
        }
    }

    /// <summary>A care choice's button, or null where it does not apply. The choice's own tests come from the mod that registered it,
    /// so a fault in one is reported once and that button left out, not the whole command bar (Framework 0.381.1).</summary>
    static Command? ButtonFor(Pawn pawn, PatientPreference preference)
    {
        try
        {
            if (preference.Applies != null && !preference.Applies(pawn)) return null;
            int choice = PatientPreferences.Choice(pawn, preference);
            return new Command_Action
            {
                defaultLabel = preference.LabelKey.Translate(preference.OptionKeys[choice].Translate().Named("CHOICE")),
                defaultDesc = preference.DescKey.Translate(),
                icon = PatientPreferences.IconFor(preference),
                action = () => Find.WindowStack.Add(new FloatMenu(PatientPreferences.MenuFor(pawn, preference))),
            };
        }
        catch (Exception exception)
        {
            ProviderFaults.Report(preference.Applies ?? (Delegate)preference.DefaultFor, "the care choice " + preference.Key, pawn, exception);
            return null;
        }
    }

    public override string? CompInspectStringExtra()
    {
        IReadOnlyList<Func<Pawn, string?>> providers = PatientPreferences.InspectLines;
        if (providers.Count == 0 || Pawn is not Pawn pawn || pawn.Dead) return null;
        StringBuilder? text = null;
        for (int i = 0; i < providers.Count; i++)
        {
            string? line;
            try
            {
                line = providers[i](pawn);
            }
            catch (Exception exception)
            {
                // Another mod's line (0.381.1): reported once, and the other lines and the rest of the pane still show.
                ProviderFaults.Report(providers[i], "an inspect line", pawn, exception);
                continue;
            }
            if (string.IsNullOrEmpty(line)) continue;
            text ??= new StringBuilder();
            if (text.Length > 0) text.Append('\n');
            text.Append(line);
        }
        return text?.ToString();
    }
}

/// <summary>
/// Puts the comp on every pawn race once the Defs are loaded: every humanlike race until 0.381.1, and every race since, so a mod
/// that makes an animal or a machine a colonist (sapient animals) still gets other mods' buttons. Code rather than XML because an
/// XML patch reaches only the Defs that spell out their race; races from Humanoid Alien Races and races inheriting one would be missed.
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
            if (def.race == null || def.thingClass == null || !typeof(Pawn).IsAssignableFrom(def.thingClass)
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
