using System.Reflection;
using Verse;

namespace Phobos.Framework.Work;

/// <summary>
/// Holds a map's room and region rebuild while a slice destroys many things, so the game rebuilds once at the end of the slice rather
/// than whenever something asks for a room (placing leavings does, for each destroyed building). It sets RegionAndRoomUpdater's
/// private "working" flag: while it is set, TryRebuildDirtyRegionsAndRooms returns at once and a room lookup in a dirtied cell answers
/// null, which the game's callers already handle. Setting Enabled to false instead would log a warning on every lookup. Read from the
/// 1.6 and 1.7 assemblies (7 Nov 2026); by reflection, because the framework has no Harmony.
/// </summary>
public static class RoomRebuildHold
{
    static readonly FieldInfo? working = typeof(RegionAndRoomUpdater).GetField("working", BindingFlags.Instance | BindingFlags.NonPublic);

    public static bool Available => working != null && working.FieldType == typeof(bool);

    /// <summary>Holds the map's rebuild. False when it can't, or someone else already holds it: then don't release it.</summary>
    public static bool TryHold(Map map)
    {
        if (!Available) return false;
        RegionAndRoomUpdater updater = map.regionAndRoomUpdater;
        if (working!.GetValue(updater) is true) return false;
        working.SetValue(updater, true);
        return true;
    }

    /// <summary>Lets the rebuild go and runs it once, now.</summary>
    public static void Release(Map map)
    {
        RegionAndRoomUpdater updater = map.regionAndRoomUpdater;
        working!.SetValue(updater, false);
        updater.TryRebuildDirtyRegionsAndRooms();
    }
}
