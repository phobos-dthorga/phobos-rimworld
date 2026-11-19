using System.Collections.Generic;
using System.Globalization;
using Phobos.Framework.Persistence;
using Verse;

namespace Phobos.CombatExtendedPatches.KeepGuns;

/// <summary>
/// What each colonist's own gun is (Patches 0.26.0; docs/development/soldiers-keep-their-guns.md). Three things per person: the kind of gun
/// they last held (its Def and material), the gun they let fall when they were downed (by its thing ID, while it may still lie somewhere they
/// can fetch it), and a back-off that stops them trying again for a day when the same recovery keeps undoing itself. Saved, because the game
/// forgets the dropped gun as soon as anything else is put in the person's hands, and that is the fault this part repairs.
/// Kept in D'thorga Framework's part store, so removing Combat Extended or this mod leaves no error in a save: one line of text per person,
/// "pawnId;droppedId;gunDef;stuffDef;backOffUntil;recoveries;firstRecoveryTick". Stable key and layout. Main thread only.
/// </summary>
public static class OwnGuns
{
    internal const string RecordKey = "PhobosPatches.CombatExtended.OwnGuns";
    const string LinesName = "lines";

    /// <summary>More than this many people remembered and the oldest are let go (authored: far above any colony's fighters).</summary>
    const int MaxRemembered = 400;

    static Game? loadedFor;
    static readonly Dictionary<int, OwnGun> byPawn = new();
    static readonly List<int> order = new();

    /// <summary>The record for a person, made when <paramref name="make"/> is set. Null before a game exists.</summary>
    public static OwnGun? For(Pawn pawn, bool make)
    {
        if (!EnsureLoaded()) return null;
        if (byPawn.TryGetValue(pawn.thingIDNumber, out OwnGun? gun)) return gun;
        if (!make) return null;
        gun = new OwnGun(pawn.thingIDNumber);
        byPawn[pawn.thingIDNumber] = gun;
        order.Add(pawn.thingIDNumber);
        while (order.Count > MaxRemembered)
        {
            byPawn.Remove(order[0]);
            order.RemoveAt(0);
        }
        return gun;
    }

    /// <summary>Writes every record back into the part store. Called after a change, which is rare: a new gun in hand, a drop, a recovery.</summary>
    public static void Save()
    {
        if (GameComponent_PhobosPartStore.Record(RecordKey) is not { } record) return;
        List<string> lines = record.Strings(LinesName);
        lines.Clear();
        foreach (int id in order)
        {
            if (byPawn.TryGetValue(id, out OwnGun? gun)) lines.Add(gun.ToLine());
        }
    }

    static bool EnsureLoaded()
    {
        Game? game = Current.Game;
        if (game == null) return false;
        if (ReferenceEquals(game, loadedFor)) return true;
        loadedFor = game;
        byPawn.Clear();
        order.Clear();
        if (GameComponent_PhobosPartStore.Record(RecordKey) is not { } record) return true;
        foreach (string line in record.Strings(LinesName))
        {
            if (OwnGun.FromLine(line) is not { } gun || byPawn.ContainsKey(gun.PawnId)) continue;
            byPawn[gun.PawnId] = gun;
            order.Add(gun.PawnId);
        }
        return true;
    }
}

/// <summary>One colonist's own gun, as <see cref="OwnGuns"/> keeps it.</summary>
public sealed class OwnGun
{
    /// <summary><see cref="DroppedId"/> when no gun was let fall.</summary>
    public const int None = -1;
    /// <summary><see cref="DroppedId"/> when the gun they let fall when downed is gone: destroyed, or not on the map they are on.</summary>
    public const int Lost = -2;

    public int PawnId { get; }
    /// <summary>The thing ID of the gun they let fall when downed; <see cref="None"/> or <see cref="Lost"/> otherwise.</summary>
    public int DroppedId = None;
    public string? GunDef;
    public string? StuffDef;
    /// <summary>No recovery is tried before this tick.</summary>
    public int BackOffUntil;
    /// <summary>Recoveries since <see cref="FirstRecoveryTick"/>, for the loop guard.</summary>
    public int Recoveries;
    public int FirstRecoveryTick;

    public OwnGun(int pawnId) => PawnId = pawnId;

    public ThingDef? Def => GunDef == null ? null : DefDatabase<ThingDef>.GetNamedSilentFail(GunDef);

    internal string ToLine() => string.Join(";", new[]
    {
        PawnId.ToString(CultureInfo.InvariantCulture), DroppedId.ToString(CultureInfo.InvariantCulture), GunDef ?? "", StuffDef ?? "",
        BackOffUntil.ToString(CultureInfo.InvariantCulture), Recoveries.ToString(CultureInfo.InvariantCulture),
        FirstRecoveryTick.ToString(CultureInfo.InvariantCulture),
    });

    internal static OwnGun? FromLine(string? line)
    {
        if (line.NullOrEmpty()) return null;
        string[] parts = line!.Split(';');
        if (parts.Length < 7 || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int pawnId)) return null;
        var gun = new OwnGun(pawnId);
        int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out gun.DroppedId);
        gun.GunDef = parts[2].Length == 0 ? null : parts[2];
        gun.StuffDef = parts[3].Length == 0 ? null : parts[3];
        int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out gun.BackOffUntil);
        int.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out gun.Recoveries);
        int.TryParse(parts[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out gun.FirstRecoveryTick);
        return gun;
    }
}
