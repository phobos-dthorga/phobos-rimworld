using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Phobos.Framework;

/// <summary>
/// Weapons used up when they are fired (a grenade, a satchel charge, a one-shot rocket launcher), told apart from guns by what their verbs do,
/// never by their names (owner: match effects, not names). A weapon is single-use when any of its verbs is the game's
/// <see cref="Verb_ShootOneUse"/> or Combat Extended's one-use verb (read 19 Nov 2026 from CE's source: its grenades and satchel charges fire
/// CombatExtended.Verb_ShootCEOneUse, which destroys the weapon after its one shot). CE's verb is found by name, so this needs no CE assembly.
/// First consumer: D'thorga Patches' "soldiers keep their guns" (Patches 0.26.0, Framework 0.174.0). Cached per Def; main thread only.
/// </summary>
public static class SingleUseWeapons
{
    const string CombatExtendedOneUseVerb = "CombatExtended.Verb_ShootCEOneUse";

    static readonly Dictionary<ThingDef, bool> singleUse = new();
    static bool lookedForCe;
    static Type? ceOneUse;

    /// <summary>A ranged weapon that is used up when it fires.</summary>
    public static bool IsSingleUse(ThingDef? def)
    {
        if (def == null || !def.IsRangedWeapon) return false;
        if (singleUse.TryGetValue(def, out bool known)) return known;
        bool result = false;
        List<VerbProperties>? verbs = def.Verbs;
        if (verbs != null)
        {
            foreach (VerbProperties verb in verbs)
            {
                if (verb?.verbClass != null && IsOneUseVerb(verb.verbClass))
                {
                    result = true;
                    break;
                }
            }
        }
        singleUse[def] = result;
        return result;
    }

    /// <summary>A ranged weapon that can fire again and again: a gun, a bow, a launcher with reloads. Not a single-use one.</summary>
    public static bool IsGun(ThingDef? def) => def != null && def.IsRangedWeapon && !IsSingleUse(def);

    static bool IsOneUseVerb(Type verbClass)
    {
        if (typeof(Verb_ShootOneUse).IsAssignableFrom(verbClass)) return true;
        if (!lookedForCe)
        {
            lookedForCe = true;
            ceOneUse = OptionalMods.TypeNamed(CombatExtendedOneUseVerb);
        }
        return ceOneUse != null && ceOneUse.IsAssignableFrom(verbClass);
    }
}
