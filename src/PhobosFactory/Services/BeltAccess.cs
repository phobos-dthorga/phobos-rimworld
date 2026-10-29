using RimWorld;
using VanillaFurnitureExpandedFactory;
using Verse;

namespace Phobos.Factory;

/// <summary>
/// What the "Belts may take from here" switch means. A switched-on storage building feeds VFE Factory's belts
/// the way a factory hopper does: a belt with it directly behind or at either side takes one whole stack at a
/// time, whatever the belt's filter says, and a factory machine reads it when it stands on an input cell.
/// Like a factory hopper, it reads the items lying on the storage's cells, which is how vanilla shelves and
/// Adaptive Storage hold them.
/// </summary>
public static class BeltAccess
{
    /// <summary>
    /// How often a belt looks for a stack in switched-on storage, in ticks: VFE Factory's own rhythm for
    /// items dropped on a belt. A belt fed from storage is therefore a little slower than one fed from a
    /// factory hopper, which it checks every tick; the saving matters with large storage beside idle belts.
    /// </summary>
    public const int PullScanIntervalTicks = 10;

    public static bool BeltsMayTake(Building_Storage storage) => storage.GetComp<CompBeltAccess>()?.BeltsMayTake == true;

    public static void SetBeltsMayTake(CompBeltAccess access, bool value)
    {
        access.BeltsMayTake = value;
        // No look target, so toggling many shelves at once shows one message.
        Messages.Message((value ? "PhobosFactory_BeltsMayTakeOn" : "PhobosFactory_BeltsMayTakeOff").Translate(),
            MessageTypeDefOf.SilentInput, historical: false);
    }

    /// <summary>Whether the belt has switched-on storage at a side it pulls from: behind, then either side.</summary>
    public static bool HasStorageToTakeFrom(Building_Conveyor belt)
    {
        var rotation = belt.Rotation;
        return SwitchedOnStorage(belt, rotation.Opposite) != null
            || SwitchedOnStorage(belt, rotation.Rotated(RotationDirection.Clockwise)) != null
            || SwitchedOnStorage(belt, rotation.Rotated(RotationDirection.Counterclockwise)) != null;
    }

    /// <summary>
    /// Moves the first stack found on switched-on storage at that side onto the belt, through VFE Factory's own
    /// hand-over. Looks only every <see cref="PullScanIntervalTicks"/> ticks.
    /// </summary>
    public static bool TryTakeFrom(Building_Conveyor belt, Rot4 side)
    {
        if (!belt.IsHashIntervalTick(PullScanIntervalTicks) || !belt.HasSpace()) return false;
        var storage = SwitchedOnStorage(belt, side);
        if (storage == null) return false;
        var map = belt.Map;
        foreach (var cell in storage.AllSlotCellsList())
        {
            foreach (var thing in map.thingGrid.ThingsListAtFast(cell))
            {
                if (thing.def.category != ThingCategory.Item) continue;
                belt.ReceiveBypass(thing.SplitOff(thing.stackCount));
                return true;
            }
        }
        return false;
    }

    /// <summary>Whether a factory machine may treat this input cell like a factory hopper.</summary>
    public static bool FeedsInputCell(IntVec3 cell, Map? map)
    {
        if (map == null || !cell.InBounds(map)) return false;
        foreach (var thing in map.thingGrid.ThingsListAtFast(cell))
        {
            if (thing is Building_Storage storage && BeltsMayTake(storage)) return true;
        }
        return false;
    }

    private static Building_Storage? SwitchedOnStorage(Building_Conveyor belt, Rot4 side)
    {
        var map = belt.Map;
        var cell = belt.Position + side.FacingCell;
        if (map == null || !cell.InBounds(map)) return null;
        var storage = cell.GetFirstThing<Building_Storage>(map);
        return storage != null && BeltsMayTake(storage) ? storage : null;
    }
}
