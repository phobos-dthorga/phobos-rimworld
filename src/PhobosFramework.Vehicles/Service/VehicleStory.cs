using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Persistence;
using Phobos.Framework.WorldMap;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// What a vehicle's life leaves behind for the player to read (Framework 0.181.0; docs/development/vehicle-emergent-play.md, group F):
/// a fault's cause in words (EM24), the vehicle's logbook (EM23), and colonists' records and tales (EM13). Called where the events already
/// happen; every write is a list append or a record bump, never per tick. Only the player's vehicles keep a logbook: nobody reads
/// another faction's, and a captured vehicle's begins at its capture.
/// </summary>
public static class VehicleStory
{
    /// <summary>The authored gap before a hit again counts as coming under fire, so a long fight is one entry, not one per bullet.</summary>
    public const int UnderFireGapTicks = GenDate.TicksPerDay;

    /// <summary>A cause in words, with when and where: "Cause: a bullet hit, 3 days ago, near Amber Anchorage."</summary>
    public static string CauseLine(CauseNote cause) => cause.Line(cause.kind == CauseKind.Strain ? StrainWords(cause.detail) : null);

    /// <summary>
    /// A strain's key in words: the system stage that gave the condition ("coolant temperature (boiling)"), another condition's stage
    /// ("a holed radiator (losing coolant)"), or the system an event belongs to. Null for a key no longer known (a system removed).
    /// </summary>
    public static string? StrainWords(string key)
    {
        if (key.NullOrEmpty()) return null;
        string[] parts = key.Split('/');
        if (parts.Length >= 4 && parts[0] == "condition")
        {
            PhobosVehicleConditionDef? condition = DefDatabase<PhobosVehicleConditionDef>.GetNamedSilentFail(parts[1]);
            if (condition == null) return null;
            string? stage = int.TryParse(parts[3], out int index) && index >= 0 && index < condition.stages.Count ? condition.stages[index].label : null;
            return stage.NullOrEmpty()
                ? "PhobosVehicleSystems_CauseConditionPlain".Translate(condition.label.Named("CONDITION")).Resolve()
                : "PhobosVehicleSystems_CauseCondition".Translate(condition.label.Named("CONDITION"), stage.Named("STAGE")).Resolve();
        }
        PhobosVehicleSystemDef? system = parts.Length >= 1 ? DefDatabase<PhobosVehicleSystemDef>.GetNamedSilentFail(parts[0]) : null;
        if (system == null) return null;
        if (parts.Length >= 3 && parts[1] != "event")
        {
            QuantitySpec? quantity = system.quantities.FirstOrDefault(q => q.name == parts[1]);
            if (quantity != null && int.TryParse(parts[2], out int index) && index >= 0 && index < quantity.stages.Count)
            {
                return "PhobosVehicleSystems_CauseStage".Translate(quantity.label.Named("QUANTITY"), quantity.stages[index].label.Named("STAGE")).Resolve();
            }
        }
        return "PhobosVehicleSystems_CauseSystem".Translate(system.label.Named("SYSTEM")).Resolve();
    }

    static bool Keeps(CompPhobosVehicleSystems comp) => comp.Vehicle?.Faction == Faction.OfPlayer && Current.Game != null;

    /// <summary>Writes an entry in the vehicle's logbook, if it is the player's.</summary>
    public static void Log(CompPhobosVehicleSystems comp, PhobosLogEntryDef def, string who = "", string subject = "", string detail = "", float value = 0f)
    {
        if (!Keeps(comp)) return;
        comp.Book.Add(def, WorldPlaces.TileOf(comp.Vehicle), who, subject, detail, value);
        Seen.Note("vehicles.logbook", comp.Vehicle, def.defName, def.defName);
    }

    /// <summary>A tale for art, about a colonist and a vehicle's kind.</summary>
    static void Tale(TaleDef def, Pawn pawn, VehiclePawn vehicle)
    {
        TaleRecorder.RecordTale(def, pawn, vehicle.VehicleDef);
        Seen.Note("vehicles.tale", vehicle, def.defName, def.defName);
    }

    /// <summary>A new fault on a part: logged with its cause.</summary>
    public static void Fault(CompPhobosVehicleSystems comp, VehicleCondition condition, VehicleComponent part)
    {
        if (!Keeps(comp) || condition.def == null) return;
        string what = "PhobosVehicleSystems_LogFaultWhat".Translate(condition.def.label.Named("CONDITION"), (part.props.label ?? part.props.key).Named("PART")).Resolve();
        string cause = condition.cause == null ? "" : condition.cause.kind == CauseKind.Hit ? condition.cause.HitText()
            : condition.cause.OwnWords() ?? StrainWords(condition.cause.detail) ?? "";
        Log(comp, VehicleStoryDefOf.PhobosLog_Fault, subject: what, detail: cause.NullOrEmpty() ? "PhobosCause_Unknown".Translate().Resolve() : cause);
        if (condition.cause != null) Seen.Note("vehicles.fault-cause", comp.Vehicle, condition.def.defName, condition.cause.kind == CauseKind.Hit ? "hit" : "strain");
    }

    /// <summary>A hit: the first after a quiet day is "came under fire".</summary>
    public static void Hit(CompPhobosVehicleSystems comp)
    {
        if (!Keeps(comp)) return;
        int now = Find.TickManager.TicksGame;
        if (comp.LastUnderFireTick >= 0 && now - comp.LastUnderFireTick < UnderFireGapTicks)
        {
            comp.LastUnderFireTick = now;
            return;
        }
        comp.LastUnderFireTick = now;
        Log(comp, VehicleStoryDefOf.PhobosLog_UnderFire);
    }

    /// <summary>The authored gap before spall wounding someone in the same vehicle is logged again, so a long fight is a few entries, not one
    /// per fragment.</summary>
    public const int SpallLogGapTicks = GenDate.TicksPerHour;

    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<VehiclePawn, System.Runtime.CompilerServices.StrongBox<int>> lastSpallLog = new();

    /// <summary>Spall from a hit wounded someone aboard (Framework 0.209.0): logged by name, at most once an hour per vehicle. Not saved: the gap
    /// starts again after a load.</summary>
    public static void SpallWounded(VehiclePawn vehicle, Pawn pawn)
    {
        if (vehicle == null || pawn == null || vehicle.GetComp<CompPhobosVehicleSystems>() is not { } comp || !Keeps(comp)) return;
        int now = Find.TickManager.TicksGame;
        var last = lastSpallLog.GetValue(vehicle, _ => new System.Runtime.CompilerServices.StrongBox<int>(int.MinValue / 2));
        if (now - last.Value < SpallLogGapTicks) return;
        last.Value = now;
        Log(comp, VehicleStoryDefOf.PhobosLog_SpallWounded, who: pawn.LabelShort);
    }

    /// <summary>Breaking glass cut someone aboard (Framework 0.238.0, VG5): one line a person per break.</summary>
    public static void GlassSplinters(VehiclePawn vehicle, Pawn pawn)
    {
        if (vehicle == null || pawn == null || vehicle.GetComp<CompPhobosVehicleSystems>() is not { } comp || !Keeps(comp)) return;
        Log(comp, VehicleStoryDefOf.PhobosLog_GlassSplinters, who: pawn.LabelShort);
    }

    /// <summary>A vehicle's glass cracked, or went (Framework 0.238.0, VG11).</summary>
    public static void GlassBroken(VehiclePawn vehicle, bool gone)
    {
        if (vehicle?.GetComp<CompPhobosVehicleSystems>() is not { } comp || !Keeps(comp)) return;
        Log(comp, gone ? VehicleStoryDefOf.PhobosLog_GlassGone : VehicleStoryDefOf.PhobosLog_GlassCracked);
    }

    /// <summary>The spall liner wore through and catches nothing until re-lined (Framework 0.209.0).</summary>
    /// <summary>An automatic suppression system put out the fire in its zone (Framework 0.241.0, FR10(b)).</summary>
    public static void SuppressionFired(VehiclePawn vehicle, string zone)
    {
        if (vehicle?.GetComp<CompPhobosVehicleSystems>() is not { } comp || !Keeps(comp)) return;
        Log(comp, VehicleStoryDefOf.PhobosLog_SuppressionFired, detail: zone);
    }

    public static void LinerSpent(VehiclePawn vehicle)
    {
        if (vehicle?.GetComp<CompPhobosVehicleSystems>() is not { } comp || !Keeps(comp)) return;
        Log(comp, VehicleStoryDefOf.PhobosLog_LinerSpent);
    }

    /// <summary>A repair or service done by a mechanic: logged with their name, counted in their records, and a tale when it was done far
    /// from any settlement (a convoy's halt).</summary>
    public static void Repaired(CompPhobosVehicleSystems comp, Pawn mechanic, string work, bool service)
    {
        if (!Keeps(comp) || mechanic == null) return;
        Log(comp, service ? VehicleStoryDefOf.PhobosLog_Serviced : VehicleStoryDefOf.PhobosLog_Repaired, who: mechanic.LabelShort, detail: work);
        if (mechanic.records != null) mechanic.records.Increment(VehicleStoryDefOf.PhobosRecord_VehicleRepairs);
        if (!service && !comp.Vehicle.Spawned && comp.Vehicle.GetCaravan() != null && mechanic.RaceProps.Humanlike)
        {
            Tale(VehicleStoryDefOf.PhobosTale_RoadsideRepair, mechanic, comp.Vehicle);
        }
    }

    /// <summary>A vehicle hitched behind another: both logbooks say so.</summary>
    public static void Hitched(VehiclePawn tractor, VehiclePawn towed)
    {
        if (tractor.GetComp<CompPhobosVehicleSystems>() is { Usable: true } a) Log(a, VehicleStoryDefOf.PhobosLog_Towing, subject: towed.LabelShort);
        if (towed.GetComp<CompPhobosVehicleSystems>() is { Usable: true } b) Log(b, VehicleStoryDefOf.PhobosLog_TowedBy, subject: tractor.LabelShort);
    }

    /// <summary>A stuck vehicle winched out: both logbooks, the record and a tale for whoever worked the winch.</summary>
    public static void Winched(VehiclePawn tractor, VehiclePawn stuck)
    {
        Pawn? driver = Driver(tractor) ?? tractor.AllPawnsAboard.FirstOrDefault(p => p.RaceProps.Humanlike);
        if (tractor.GetComp<CompPhobosVehicleSystems>() is { Usable: true } a) Log(a, VehicleStoryDefOf.PhobosLog_Winched, who: driver?.LabelShort ?? "", subject: stuck.LabelShort);
        if (stuck.GetComp<CompPhobosVehicleSystems>() is { Usable: true } b) Log(b, VehicleStoryDefOf.PhobosLog_WinchedOut, subject: tractor.LabelShort);
        if (driver == null || tractor.Faction != Faction.OfPlayer) return;
        driver.records?.Increment(VehicleStoryDefOf.PhobosRecord_VehiclesRecovered);
        Tale(VehicleStoryDefOf.PhobosTale_VehicleRecovered, driver, stuck);
    }

    /// <summary>A vehicle taken from another faction: its logbook begins here.</summary>
    public static void Claimed(VehiclePawn vehicle, Pawn claimer, Faction? from)
    {
        if (vehicle.GetComp<CompPhobosVehicleSystems>() is not { Usable: true } comp) return;
        Log(comp, VehicleStoryDefOf.PhobosLog_Claimed, who: claimer.LabelShort, subject: from?.Name ?? "PhobosVehicleSystems_LogNobody".Translate().Resolve());
    }

    /// <summary>An aircraft of ours came down hard and stayed whole (Framework 0.185.0): the logbook, and a tale for each person aboard who
    /// walked away from it.</summary>
    public static void HardLanding(VehiclePawn aircraft)
    {
        if (aircraft.GetComp<CompPhobosVehicleSystems>() is { Usable: true } comp) Log(comp, VehicleStoryDefOf.PhobosLog_HardLanding);
        if (aircraft.Faction != Faction.OfPlayer) return;
        foreach (Pawn pawn in aircraft.AllPawnsAboard.ToList())
        {
            if (!pawn.Dead && pawn.RaceProps.Humanlike) Tale(VehicleStoryDefOf.PhobosTale_CrashSurvived, pawn, aircraft);
        }
    }

    /// <summary>
    /// An entry about a whole convoy (Framework 0.230.0): written in the logbook of its first vehicle of the player's with a logbook, so a
    /// convoy's night drives, meals and dry tanks are read where its trips are.
    /// </summary>
    public static void Convoy(Caravan? caravan, PhobosLogEntryDef def, string who = "", string subject = "", string detail = "", float value = 0f)
    {
        if (caravan is not VehicleCaravan convoy || convoy.Faction != Faction.OfPlayer) return;
        foreach (VehiclePawn vehicle in convoy.VehiclesListForReading)
        {
            if (vehicle.GetComp<CompPhobosVehicleSystems>() is not { Usable: true } comp) continue;
            Log(comp, def, who, subject, detail, value);
            return;
        }
    }

    /// <summary>An entry in one vehicle's logbook, if it has one and is the player's (Framework 0.230.0).</summary>
    public static void Of(VehiclePawn? vehicle, PhobosLogEntryDef def, string who = "", string subject = "", string detail = "", float value = 0f)
    {
        if (vehicle?.GetComp<CompPhobosVehicleSystems>() is { Usable: true } comp) Log(comp, def, who, subject, detail, value);
    }

    /// <summary>A tale for a long journey's first driver.</summary>
    public static void LongJourney(Pawn driver, VehiclePawn vehicle) => Tale(VehicleStoryDefOf.PhobosTale_LongJourney, driver, vehicle);

    /// <summary>Whoever drives the vehicle now: the fittest in a driving seat, as the driver rule reads them; null with nobody there.</summary>
    public static Pawn? Driver(VehiclePawn vehicle)
    {
        if (!vehicle.PawnsByHandlingType.TryGetValue(HandlingType.Movement, out List<Pawn>? drivers) || drivers == null) return null;
        return drivers.FirstOrDefault(p => p != null && !p.Dead && p.RaceProps.Humanlike);
    }

    /// <summary>Everyone in a driving seat now, humanlike, for the trip's names and records.</summary>
    public static IEnumerable<Pawn> Drivers(VehiclePawn vehicle)
    {
        if (!vehicle.PawnsByHandlingType.TryGetValue(HandlingType.Movement, out List<Pawn>? drivers) || drivers == null) yield break;
        foreach (Pawn pawn in drivers)
        {
            if (pawn != null && !pawn.Dead && pawn.RaceProps.Humanlike) yield return pawn;
        }
    }
}

[DefOf]
public static class VehicleStoryDefOf
{
    public static PhobosLogEntryDef PhobosLog_Fault = null!;
    public static PhobosLogEntryDef PhobosLog_UnderFire = null!;
    public static PhobosLogEntryDef PhobosLog_Repaired = null!;
    public static PhobosLogEntryDef PhobosLog_Serviced = null!;
    public static PhobosLogEntryDef PhobosLog_Towing = null!;
    public static PhobosLogEntryDef PhobosLog_TowedBy = null!;
    public static PhobosLogEntryDef PhobosLog_Winched = null!;
    public static PhobosLogEntryDef PhobosLog_WinchedOut = null!;
    public static PhobosLogEntryDef PhobosLog_Claimed = null!;
    public static PhobosLogEntryDef PhobosLog_TripStart = null!;
    public static PhobosLogEntryDef PhobosLog_TripEnd = null!;
    public static PhobosLogEntryDef PhobosLog_Stranded = null!;
    public static PhobosLogEntryDef PhobosLog_HardLanding = null!;
    public static PhobosLogEntryDef PhobosLog_LeftBehind = null!;
    public static PhobosLogEntryDef PhobosLog_RanOn = null!;
    public static PhobosLogEntryDef PhobosLog_Struck = null!;
    public static PhobosLogEntryDef PhobosLog_SpallWounded = null!;
    public static PhobosLogEntryDef PhobosLog_LinerSpent = null!;
    public static PhobosLogEntryDef PhobosLog_SuppressionFired = null!;
    public static PhobosLogEntryDef PhobosLog_GlassCracked = null!;
    public static PhobosLogEntryDef PhobosLog_GlassGone = null!;
    public static PhobosLogEntryDef PhobosLog_GlassSplinters = null!;
    public static PhobosLogEntryDef PhobosLog_Camped = null!;
    public static PhobosLogEntryDef PhobosLog_NightDrive = null!;
    public static PhobosLogEntryDef PhobosLog_HandOver = null!;
    public static PhobosLogEntryDef PhobosLog_RoadMeals = null!;
    public static PhobosLogEntryDef PhobosLog_WaterDry = null!;
    public static PhobosLogEntryDef PhobosLog_HeaterDry = null!;
    public static PhobosLogEntryDef PhobosLog_TentWoken = null!;
    public static RecordDef PhobosRecord_KilometresDriven = null!;
    public static RecordDef PhobosRecord_VehicleRepairs = null!;
    public static RecordDef PhobosRecord_VehiclesRecovered = null!;
    public static TaleDef PhobosTale_VehicleRecovered = null!;
    public static TaleDef PhobosTale_RoadsideRepair = null!;
    public static TaleDef PhobosTale_LongJourney = null!;
    public static TaleDef PhobosTale_CrashSurvived = null!;

    static VehicleStoryDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(VehicleStoryDefOf));
}
