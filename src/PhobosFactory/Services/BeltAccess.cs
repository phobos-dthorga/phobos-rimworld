using System;
using HarmonyLib;
using Phobos.Factory.Rules;
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
/// storage's cells, which is how vanilla shelves and Adaptive Storage hold them. Hoppers carry the same switch,
/// starting on (0.11.0): off, belts leave them alone, while belts can still deliver into them and machines and
/// colonists can still take from them (<see cref="BeltRule"/>).
/// </summary>
public static class BeltAccess
{
    /// <summary>
    /// How often a belt looks for a stack in switched-on storage, in ticks: VFE Factory's own rhythm for
    /// items dropped on a belt. A belt fed from storage is therefore a little slower than one fed from a
    /// factory hopper, which it checks every tick; the saving matters with large storage beside idle belts.
    /// </summary>
    public const int PullScanIntervalTicks = 10;

    /// <summary>
    /// VFE Factory's own lookup of what stands beside a belt, the very building its pull is about to read. It
    /// matters because a belt beside a hopper asks up to three times a tick while it has room.
    /// </summary>
    private static readonly Func<Building_Conveyor, Rot4, Building>? CachedNeighbour = MakeCachedNeighbour();

    /// <summary>Whether VFE Factory feeds belts from this Def by itself: its hopper class, or the Def flag.</summary>
    public static bool IsHopperDef(ThingDef def) =>
        typeof(Building_FactoryHopper).IsAssignableFrom(def.thingClass) ||
        def.GetModExtension<FactoryHopperExtension>()?.isfactoryHopper == true;

    /// <summary>Whether this is ordinary storage the player has switched on.</summary>
    public static bool IsSwitchedOn(Building_Storage storage)
    {
        var access = storage.GetComp<CompBeltAccess>();
        return access != null && BeltRule.IsSwitchedOn(access.BeltsMayTake, access.Props.startsOn);
    }

    /// <summary>Whether this is a hopper the player has told belts to leave alone.</summary>
    public static bool RefusesBelts(Building_Storage storage)
    {
        var access = storage.GetComp<CompBeltAccess>();
        return access != null && BeltRule.RefusesBelts(access.BeltsMayTake, access.Props.startsOn);
    }

    public static void SetBeltsMayTake(CompBeltAccess access, bool value)
    {
        access.BeltsMayTake = value;
        string key = access.Props.startsOn
            ? (value ? "PhobosFactory_BeltsMayTakeHopperOn" : "PhobosFactory_BeltsMayTakeHopperOff")
            : (value ? "PhobosFactory_BeltsMayTakeOn" : "PhobosFactory_BeltsMayTakeOff");
        // No look target, so toggling many shelves at once shows one message.
        Messages.Message(key.Translate(), MessageTypeDefOf.SilentInput, historical: false);
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

    /// <summary>The storage at <paramref name="side"/> of the belt, as VFE Factory's own pull sees it.</summary>
    public static Building_Storage? StorageBeside(Building_Conveyor belt, Rot4 side) =>
        CachedNeighbour != null ? CachedNeighbour(belt, side) as Building_Storage : StorageAt(belt, side);

    /// <summary>
    /// Whether VFE Factory would take from this storage as a hopper but its Def asks belts to obey its filter
    /// (<see cref="BeltFilterExtension"/>).
    /// </summary>
    public static bool ObeysFilter(Building_Storage storage) =>
        storage.def.HasModExtension<BeltFilterExtension>() && IsHopperDef(storage.def);

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
            if (thing is Building_Storage storage && IsSwitchedOn(storage)) return true;
        }
        return false;
    }

    /// <summary>The side directly behind a belt: opposite the way it points.</summary>
    private static Rot4 Behind(Building_Conveyor belt) => belt.Rotation.Opposite;

    private static Building_Storage? SwitchedOnStorage(Building_Conveyor belt, Rot4 side)
    {
        var storage = StorageAt(belt, side);
        return storage != null && IsSwitchedOn(storage) ? storage : null;
    }

    private static Func<Building_Conveyor, Rot4, Building>? MakeCachedNeighbour()
    {
        var method = AccessTools.Method(typeof(Building_Conveyor), "GetCachedNeighborBuilding", new[] { typeof(Rot4) });
        return method != null ? AccessTools.MethodDelegate<Func<Building_Conveyor, Rot4, Building>>(method) : null;
    }

    private static Building_Storage? StorageAt(Building_Conveyor belt, Rot4 side)
    {
        var map = belt.Map;
        var cell = belt.Position + side.FacingCell;
        return map != null && cell.InBounds(map) ? cell.GetFirstThing<Building_Storage>(map) : null;
    }
}
