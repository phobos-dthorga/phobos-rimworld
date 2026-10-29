using RimWorld;
using VanillaFurnitureExpandedFactory;
using Verse;

namespace Phobos.Factory;

/// <summary>
/// What the "Belts may take from here" switch means. A switched-on storage building feeds VFE Factory's belts
/// the way a factory hopper does, with one difference: only a belt that points straight away from it (the
/// storage directly behind the belt) takes from it, one whole stack at a time, whatever the belt's filter says.
/// A hopper also feeds belts that have it at their side; storage doesn't, so belts can run past it (owner,
/// 29 Oct 2026). A factory machine reads it when it stands on an input cell. Like a factory hopper, it reads the
/// items lying on the storage's cells, which is how vanilla shelves and Adaptive Storage hold them.
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

    /// <summary>Whether switched-on storage is directly behind the belt, the one side it may take storage from.</summary>
    public static bool HasStorageToTakeFrom(Building_Conveyor belt) => SwitchedOnStorage(belt, Behind(belt)) != null;

    /// <summary>
    /// Moves the first stack found on switched-on storage onto the belt, through VFE Factory's own hand-over, when
    /// <paramref name="side"/> is behind the belt. Looks only every <see cref="PullScanIntervalTicks"/> ticks.
    /// </summary>
    public static bool TryTakeFrom(Building_Conveyor belt, Rot4 side)
    {
        if (side != Behind(belt) || !belt.IsHashIntervalTick(PullScanIntervalTicks) || !belt.HasSpace()) return false;
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

    /// <summary>The side directly behind a belt: opposite the way it points.</summary>
    private static Rot4 Behind(Building_Conveyor belt) => belt.Rotation.Opposite;

    private static Building_Storage? SwitchedOnStorage(Building_Conveyor belt, Rot4 side)
    {
        var map = belt.Map;
        var cell = belt.Position + side.FacingCell;
        if (map == null || !cell.InBounds(map)) return null;
        var storage = cell.GetFirstThing<Building_Storage>(map);
        return storage != null && BeltsMayTake(storage) ? storage : null;
    }
}
