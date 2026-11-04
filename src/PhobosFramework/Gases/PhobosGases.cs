using Phobos.Framework.Rules;
using Verse;

namespace Phobos.Framework.Gases;

/// <summary>
/// The one door for every mod that reads or adds a gas. Existing gases and effects come first (the game's gas grid, Combat
/// Extended's smoke inhalation); the framework's own room gases carry what nothing else does, or everything when the player
/// overrides. Nothing here mutates a pawn: consumers decide what breathed gas does.
/// </summary>
public static class PhobosGases
{
    /// <summary>Whether Combat Extended's smoke inhalation is the smoke signal right now.</summary>
    public static bool UsesCombatExtendedSmoke =>
        SignalRule.UseCombatExtendedSmoke(CombatExtendedSmoke.Active, CombatExtendedSmoke.InUse);

    /// <summary>Density of an existing gas at a cell, 0 to 1, from every carrier.</summary>
    public static float Density(IntVec3 cell, Map map, GasKind kind) => GasCarriers.Density(cell, map, kind);

    /// <summary>Concentration of a framework gas in the room at a cell, 0 to 1.</summary>
    public static float Concentration(IntVec3 cell, Map map, PhobosGasDef gas) =>
        MapComponent_PhobosRoomGases.For(map)?.Concentration(cell, gas) ?? 0f;

    /// <summary>
    /// What a spawned pawn breathes of an existing gas kind, 0 to 1. Smoke is Combat Extended's own inhalation severity while
    /// that runs (its masks included), otherwise the cell's density through the mask rule.
    /// </summary>
    public static float Breathed(Pawn pawn, GasKind kind)
    {
        if (!pawn.Spawned || pawn.Map == null) return 0f;
        if (kind == GasKind.Smoke)
        {
            bool useCe = UsesCombatExtendedSmoke;
            float ceSeverity = useCe ? CombatExtendedSmoke.Severity(pawn) : 0f;
            float vanilla = useCe ? 0f : Density(pawn.Position, pawn.Map, kind);
            return SignalRule.BreathedSmoke(useCe, ceSeverity, vanilla, useCe ? 1f : MaskFactor.Filtered(pawn));
        }
        return Density(pawn.Position, pawn.Map, kind) * MaskFactor.Filtered(pawn);
    }

    /// <summary>What a spawned pawn breathes of a framework gas, 0 to 1, through the gas's own mask rule.</summary>
    public static float Breathed(Pawn pawn, PhobosGasDef gas)
    {
        if (!pawn.Spawned || pawn.Map == null) return 0f;
        float concentration = Concentration(pawn.Position, pawn.Map, gas);
        return concentration <= 0f ? 0f : concentration * MaskFactor.For(pawn, gas.filteredByMask);
    }

    /// <summary>How enclosed a cell is, 0 open air to 1 a sealed roofed room.</summary>
    public static float Enclosure(IntVec3 cell, Map map)
    {
        bool roofed = cell.Roofed(map);
        if (!roofed) return 0f;
        Room? room = cell.GetRoom(map);
        bool outdoors = room == null || room.UsesOutdoorTemperature || room.PsychologicallyOutdoors;
        return EnclosureRule.Compute(true, outdoors, room?.OpenRoofCount ?? 0, room?.CellCount ?? 0);
    }

    /// <summary>Adds an existing gas kind at a cell through every carrier that takes it (the game's grid spreads it itself).</summary>
    public static void Add(IntVec3 cell, Map map, GasKind kind, float fraction) => GasCarriers.Add(cell, map, kind, fraction);

    /// <summary>Adds a framework gas to the room at a cell. <paramref name="cells"/> is in cells of gas; a 20-cell room gains cells/20.</summary>
    public static void AddOwn(IntVec3 cell, Map map, PhobosGasDef gas, float cells) =>
        MapComponent_PhobosRoomGases.For(map)?.AddGas(cell, gas, cells);

    /// <summary>Where a consumer should read this gas from, under the owner's rule and the player's override.</summary>
    public static GasSource Source(PhobosGasDef gas)
    {
        bool existing = gas.preferExisting != GasKind.None && ExistingPresent(gas.preferExisting);
        return SourceRule.Decide(FrameworkMod.Settings.gasSourceMode, existing, gas.alwaysOwn);
    }

    /// <summary>Whether an existing carrier or effect covers a gas kind in this game.</summary>
    public static bool ExistingPresent(GasKind kind)
    {
        switch (kind)
        {
            case GasKind.Smoke: return true;
            case GasKind.Toxic: return ModsConfig.BiotechActive;
            case GasKind.RotStink: return true;
            case GasKind.DeadlifeDust: return ModsConfig.AnomalyActive;
            default: return false;
        }
    }
}
