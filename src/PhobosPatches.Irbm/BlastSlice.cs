using IRBM;
using Phobos.Framework.Work;
using Verse;

namespace Phobos.MissilePatches;

/// <summary>
/// What every slice of a strategic blast sets up and puts back, shared by the missile mod's sweep (<see cref="IrbmSweep"/>) and
/// D'thorga's strike (Strike/StrikeWork): the missile mod's damage context (no damage multipliers, the missile's name in the logs),
/// the map's held room rebuild, and Combat Extended's wall fragments as the player chose.
/// </summary>
internal sealed class BlastSlice
{
    bool heldRooms;
    bool previousIgnore;
    string? previousLabel;
    CombatExtendedFragments.Override fragmentChange;

    public void Begin(Map map, string? sourceLabel)
    {
        previousIgnore = IRBM_DamageContext.ignoreDamageMultipliers;
        previousLabel = IRBM_DamageContext.currentDamageSourceLabel;
        IRBM_DamageContext.ignoreDamageMultipliers = true;
        IRBM_DamageContext.currentDamageSourceLabel = sourceLabel;
        if (IRBM_DefOf.IRBM_MissileWeapon != null && !sourceLabel.NullOrEmpty()) IRBM_DefOf.IRBM_MissileWeapon.label = sourceLabel;
        heldRooms = MissilePatchesMod.Settings.holdRoomRebuilds && RoomRebuildHold.TryHold(map);
        fragmentChange = CombatExtendedFragments.Apply(MissilePatchesMod.Settings.fragments);
    }

    public void End(Map map)
    {
        CombatExtendedFragments.Restore(fragmentChange);
        fragmentChange = default;
        if (heldRooms) RoomRebuildHold.Release(map);
        heldRooms = false;
        IRBM_DamageContext.ignoreDamageMultipliers = previousIgnore;
        IRBM_DamageContext.currentDamageSourceLabel = previousLabel;
    }
}
