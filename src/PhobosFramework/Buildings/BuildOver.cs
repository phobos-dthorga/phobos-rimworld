using RimWorld;
using Verse;

namespace Phobos.Framework.Buildings;

/// <summary>
/// Whether a building already on a cell stops something of ours being placed there by code (Framework 0.270.1; the owner's report of 23
/// Nov 2026: a trailer would not set up over underground pipes). The game lets a player build over what lies under the floor, a power
/// conduit or a pipe, and anything else that is no edifice; a check that refuses every building refuses those too. This asks the game's
/// own rule, with two differences, because code places at once and without a refund: an edifice always blocks (the game's rule would let
/// a wall replace a wall), and so does anything the new thing would wipe as it spawns (a power-carrying building wipes the conduit under
/// it).
/// </summary>
public static class BuildOver
{
    /// <summary>
    /// True when <paramref name="existing"/> must stop <paramref name="placing"/> going on its cell. With no Def to place (a cell that only
    /// has to be clear to walk and roof over), only an edifice blocks.
    /// </summary>
    public static bool Blocks(Thing existing, ThingDef? placing)
    {
        if (existing is not Building) return false;
        if (existing.def.IsEdifice()) return true;
        if (placing == null) return false;
        return !GenConstruct.CanPlaceBlueprintOver(placing, existing.def) || GenSpawn.SpawningWipes(placing, existing.def);
    }
}
