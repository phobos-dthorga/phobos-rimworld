using System.Linq;
using Phobos.Framework;
using Phobos.Framework.Persistence;
using Phobos.Framework.Rules;

namespace Phobos.AircraftRearmPatches;

/// <summary>
/// Each jet loads as its own mod's jet again (Patches 0.45.2; the owner's report of 26 Nov 2026: the A-10 had no Sortie button and hovered
/// when told to take off). The jets' update of 26 Nov 2026 gave the Harrier, Close Air Support and the Prowler each a converter for saves made
/// before it (OldSaves, in each of AV8BHarrier.dll, A10Warthog.dll and EA6BProwler.dll, read the same day). Each moves a class of the old
/// shared FixedWingAircraftFramework namespace to its own copy whenever the old class no longer exists, whichever jet it is inside, and each
/// puts itself first in the game's list of converters; so the last to start, the Prowler's in the owner's game, took every jet. The
/// Harriers, A-10 and Su-25 then ran the Prowler's code, which finds none of its own fixed-wing data on them and flies them as a VTOL: no
/// Sortie, a hover instead of a runway take-off. Every save made since held them so, out of reach of any converter, since the game skips
/// converters when a save's mod list matches the running game's.
///
/// D'thorga Framework's class families put that right on load: a class from the old namespace or any copy is made as the copy its jet's Def
/// names. It touches nothing the jets' own converters already get right. Temporary in part: once the author's converters ask which jet a class
/// is in, a save made before their update loads right without it; a save written since still needs it. docs/development/temporary-patches.md
/// has its row.
/// </summary>
internal static class JetClasses
{
    /// <summary>The namespace the three mods shared until 26 Nov 2026.</summary>
    internal const string SharedNamespace = "FixedWingAircraftFramework";

    internal static void Register(LoadReport report)
    {
        ClassFamilies.Register(new ClassFamily(FixedWingCopies.All.Select(copy => copy.Namespace).Prepend(SharedNamespace)));
        report.On("each jet in a save loads as its own mod's jet, not as another jet mod's copy");
    }
}
