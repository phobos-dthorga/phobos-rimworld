using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>
/// S3, the comms console needs reach (Radio 0.64.0; docs/development/radio-comms-console.md, the owner's choice of 28 Nov 2026: with HF, a
/// setting, off by default, as RT6 graded it a high risk): while the setting is on, the game's own comms console lists only the factions
/// the colony's radio reaches (<see cref="RadioDiplomacy.InReach"/>, kept an hour), with HF most of the planet. Passing ships are left as
/// they are. Off, the console is untouched.
/// <para>Cost: one look in the hour's reach list when the console's menu is opened.</para>
/// </summary>
[HarmonyPatch(typeof(Building_CommsConsole), nameof(Building_CommsConsole.GetCommTargets))]
internal static class Patch_CommsConsole_GetCommTargets_NeedsReach
{
    // A postfix: the game builds its list, and this only leaves out factions out of reach.
    static void Postfix(Pawn myPawn, ref IEnumerable<ICommunicable> __result)
    {
        if (!RadioMod.Settings.consoleNeedsReach || myPawn?.Map is not { } map) return;
        var reached = new HashSet<Faction>(RadioDiplomacy.InReach(map).Select(link => link.Faction));
        List<ICommunicable> kept = __result.Where(target => target is not Faction faction || reached.Contains(faction)).ToList();
        if (Seen.Wants("radio.console-needs-reach", myPawn))
            Seen.Note("radio.console-needs-reach", myPawn, reached.Count + " factions in reach");
        __result = kept;
    }
}
