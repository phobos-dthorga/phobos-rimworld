using System.Linq;
using PipeSystem;
using RimWorld;
using VanillaFurnitureExpandedFactory;
using Verse;

namespace Phobos.Factory;

/// <summary>
/// What belts may take from storage that isn't a factory hopper. A building switched on with "Belts may take
/// from here" feeds VFE Factory's belts the way a factory hopper does, with two differences: only a belt that
/// points straight away from it (the storage directly behind the belt) takes from it, and only stacks the
/// storage's own filter allows. A hopper also feeds belts that have it at their side; storage doesn't, so belts
/// can run past it (owner, 29 Oct 2026). A factory machine reads it when it stands on an input cell. Storage whose
/// Def VFE Factory treats as a hopper and that carries <see cref="BeltFilterExtension"/> (the corpse fridge) keeps
/// the hopper's sides but obeys its filter too (0.7.0). Like a factory hopper, this reads the items lying on the
/// storage's cells, which is how vanilla shelves and Adaptive Storage hold them.
/// </summary>
public static class BeltAccess
{
    /// <summary>
    /// How often a belt looks for a stack in switched-on storage, in ticks: VFE Factory's own rhythm for
    /// items dropped on a belt. A belt fed from storage is therefore a little slower than one fed from a
    /// factory hopper, which it checks every tick; the saving matters with large storage beside idle belts.
    /// </summary>
    public const int PullScanIntervalTicks = 10;

    /// <summary>Whether any loaded storage Def carries <see cref="BeltFilterExtension"/>; worked out on first use, once Defs are loaded.</summary>
    private static bool? anyFilteredHopperDefs;

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
    /// Moves the first stack on switched-on storage that its filter allows onto the belt, when
    /// <paramref name="side"/> is behind the belt. Looks only every <see cref="PullScanIntervalTicks"/> ticks.
    /// </summary>
    public static bool TryTakeFrom(Building_Conveyor belt, Rot4 side)
    {
        if (side != Behind(belt) || !belt.IsHashIntervalTick(PullScanIntervalTicks) || !belt.HasSpace()) return false;
        var storage = SwitchedOnStorage(belt, side);
        return storage != null && TakeFirstAllowed(belt, storage);
    }

    /// <summary>
    /// Storage at <paramref name="side"/> of the belt that VFE Factory would take from as a hopper but whose Def
    /// asks belts to obey its filter (<see cref="BeltFilterExtension"/>); null for anything else, which VFE Factory
    /// handles as before.
    /// </summary>
    public static Building_Storage? FilteredHopperAt(Building_Conveyor belt, Rot4 side)
    {
        // VFE Factory asks up to three times a tick per empty belt beside a hopper; without any such Def loaded
        // (no [sbz] Fridge) this is the whole cost.
        anyFilteredHopperDefs ??= DefDatabase<ThingDef>.AllDefsListForReading.Any(def => def.HasModExtension<BeltFilterExtension>());
        if (anyFilteredHopperDefs == false) return null;
        var storage = StorageAt(belt, side);
        if (storage == null || !storage.def.HasModExtension<BeltFilterExtension>()) return null;
        return storage is Building_FactoryHopper || storage.def.GetModExtension<FactoryHopperExtension>()?.isfactoryHopper == true
            ? storage
            : null;
    }

    /// <summary>
    /// Moves the first stack on the storage that its filter allows onto the belt, whole, as VFE Factory takes from
    /// a hopper: Thing.SplitOff of a whole stack despawns it, and ReceiveBypass starts it moving. Stacks the filter
    /// rejects stay where they are; haulers move them on, because the storage doesn't accept them.
    /// </summary>
    public static bool TakeFirstAllowed(Building_Conveyor belt, Building_Storage storage)
    {
        var map = belt.Map;
        var settings = storage.GetStoreSettings();
        foreach (var cell in storage.AllSlotCellsList())
        {
            foreach (var thing in map.thingGrid.ThingsListAtFast(cell))
            {
                if (thing.def.category != ThingCategory.Item || !settings.AllowedToAccept(thing)) continue;
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
        var storage = StorageAt(belt, side);
        return storage != null && BeltsMayTake(storage) ? storage : null;
    }

    private static Building_Storage? StorageAt(Building_Conveyor belt, Rot4 side)
    {
        var map = belt.Map;
        var cell = belt.Position + side.FacingCell;
        return map != null && cell.InBounds(map) ? cell.GetFirstThing<Building_Storage>(map) : null;
    }
}
