using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Phobos.Framework;
using Phobos.Framework.Debug;
using RimWorld.Planet;
using SmashTools.Targeting;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.AircraftRearmPatches;

/// <summary>
/// A sortie may strike any of the player's home maps, the gravship's included (Patches 0.41.0; the owner's report and question cards,
/// 25 Nov 2026: "Any home map, colonies too", "Sortie as a whole"). The fixed-wing framework the Harrier, Close Air Support's A-10 and Su-25,
/// and the Prowler share (FixedWingAircraftFramework 19o, read 25 Nov 2026) refuses a sortie onto a home map ("a sortie strikes a site, not
/// a colony": CompVehicleLauncher_CanTarget_FixedWingSortiePatch, and each mod's own Patch_CanTarget17 copy for 1.7). Since RimWorld 1.7
/// (Odyssey) every map holding the player's grav engine counts as a home (Map.IsPlayerHome, through GravshipUtility.PlayerHasGravEngine), so a
/// gravship parked at an enemy site could not be given air support at all.
///
/// What this changes, only for a fixed-wing jet the player sent with Sortie, and only for a home map:
/// - the target check: the framework's home refusal is stood down for that one check (its SortieTargeting is lowered while the check runs);
/// - the destination's label reads as a sortie, not "land at";
/// - the point to circle can be chosen on that map, as on any strike map;
/// - on arrival the jet comes in to circle that point instead of landing;
/// - while it circles a home with no beacon runway it fits, it counts as away, so out of bombs or fuel it flies back to a home with a runway
///   (the framework's own return-home choice) rather than putting down on the spot and being stranded. Over a home with a runway that fits,
///   it lands there when done, as the framework does.
/// Everything else stays the author's: Launch to World Map still lands at a home, and other factions' jets are untouched (Sortie is the
/// player's button). The framework's sortie fuel rule (CompVehicleLauncher.HasEnoughFuel) has had no target since Vehicle Framework 1.7, so
/// sorties of every kind fly without it; that is the author's and is left alone.
///
/// The record of which jet is on a sortie over which home is not saved: after a load mid-sortie the jet behaves as the framework does
/// (it lands at the home it arrives over, or on the spot when it has no runway there). Temporary: the author is active, and this stands down
/// by itself (report "off") if the framework's members it reaches by name change. docs/development/temporary-patches.md has its row.
///
/// Since 26 Nov 2026 each jet mod carries its own copy of that framework under its own namespace (FixedWingCopies), so this patches every
/// copy it finds and keeps each copy's members apart, telling a jet's copy by its class (Patches 0.45.1).
/// </summary>
public static class SortieOverHome
{
    /// <summary>One jet mod's copy of the fixed-wing framework: the members this reaches, found by name.</summary>
    sealed class Flight
    {
        public Type Pawn = null!;
        public PropertyInfo SortieTargeting = null!;
        public MethodInfo SetSortieTargeting = null!;
        public PropertyInfo FixedWingProps = null!;
        public FieldInfo RunwayLength = null!;
        public FieldInfo RunwayWidth = null!;
        public Type RunwaysComponent = null!;
        public PropertyInfo Runways = null!;
        public Type Controller = null!;
        public FieldInfo ControllerVehicle = null!;
        public MethodInfo Away = null!;
        public MethodInfo Arrive = null!;
        public MethodInfo IsLoiter = null!;

        /// <summary>The copy's members, or null when any is missing or its arrival no longer reads the home flag once.</summary>
        public static Flight? Find(FixedWingCopies.Copy copy)
        {
            Type? pawn = copy.Type("VtolVehiclePawn");
            Type? props = copy.Type("FixedWingProperties");
            Type? controller = copy.Type("VtolFlightController");
            Type? arrival = copy.Type("ArrivalAction_LandToCell_Arrived_VtolPatch");
            Type? loiter = copy.Type("LandingTargeter_GetPosState_VtolPatch");
            Type? runwaysComponent = copy.Type("MapComponent_FixedWingRunways");
            PropertyInfo? sortie = pawn == null ? null : AccessTools.Property(pawn, "SortieTargeting");
            MethodInfo? setSortie = sortie?.GetSetMethod(true);
            PropertyInfo? fixedWingProps = pawn == null ? null : AccessTools.Property(pawn, "FixedWingProps");
            FieldInfo? length = props == null ? null : AccessTools.Field(props, "runwayLength");
            FieldInfo? width = props == null ? null : AccessTools.Field(props, "runwayWidth");
            PropertyInfo? runways = runwaysComponent == null ? null : AccessTools.Property(runwaysComponent, "Runways");
            FieldInfo? vehicle = controller == null ? null : AccessTools.Field(controller, "vehicle");
            MethodInfo? away = controller == null ? null : AccessTools.PropertyGetter(controller, "IsAwayFromHome");
            MethodInfo? arrive = arrival == null ? null : AccessTools.Method(arrival, "ArriveFixedWing");
            MethodInfo? isLoiter = loiter == null ? null : AccessTools.Method(loiter, "IsFixedWingLoiterTarget");
            if (pawn == null || sortie == null || setSortie == null || fixedWingProps == null || length == null || width == null
                || runwaysComponent == null || runways == null || controller == null || vehicle == null || away == null || arrive == null
                || isLoiter == null || !ReadsHomeOnce(arrive))
                return null;
            return new Flight
            {
                Pawn = pawn, SortieTargeting = sortie, SetSortieTargeting = setSortie, FixedWingProps = fixedWingProps, RunwayLength = length,
                RunwayWidth = width, RunwaysComponent = runwaysComponent, Runways = runways, Controller = controller, ControllerVehicle = vehicle,
                Away = away, Arrive = arrive, IsLoiter = isLoiter,
            };
        }
    }

    /// <summary>The copies found, one for each active jet mod whose code still matches; at most three.</summary>
    static readonly List<Flight> flights = new();

    /// <summary>The copy a jet belongs to, by its class; null for any other vehicle.</summary>
    static Flight? For(VehiclePawn vehicle)
    {
        foreach (Flight flight in flights)
            if (flight.Pawn.IsInstanceOfType(vehicle)) return flight;
        return null;
    }

    /// <summary>The jet being checked by CanTarget with its sortie flag lowered, so the arrival-option label still reads it as a sortie.</summary>
    [ThreadStatic] static VehiclePawn? lowered;

    sealed class Over
    {
        public readonly Map Map;
        public Over(Map map) => Map = map;
    }

    /// <summary>Which home map a jet was sent to strike, from the moment its sortie is ordered until it is over another map.</summary>
    static readonly ConditionalWeakTable<VehiclePawn, Over> sorties = new();

    public static void Apply(Harmony harmony, LoadReport report)
    {
        MethodInfo canTarget = AccessTools.Method(typeof(CompVehicleLauncher), nameof(CompVehicleLauncher.CanTarget));
        MethodInfo options = AccessTools.Method(typeof(LaunchProtocol), nameof(LaunchProtocol.GetArrivalOptions), new[] { typeof(GlobalTargetInfo) });
        MethodInfo localMap = AccessTools.Method(typeof(LaunchProtocol), nameof(LaunchProtocol.StartTargetingLocalMap));
        bool vehicleFramework = canTarget != null && options != null && localMap != null;
        foreach (FixedWingCopies.Copy copy in FixedWingCopies.Active())
        {
            string feature = "a sortie of " + copy.Label + " may strike a home map, the gravship's included";
            Flight? flight = vehicleFramework ? Flight.Find(copy) : null;
            if (flight == null)
            {
                report.Off(feature, vehicleFramework ? "its code has changed" : "Vehicle Framework's code has changed");
                continue;
            }
            harmony.Patch(flight.IsLoiter, postfix: new HarmonyMethod(typeof(SortieOverHome), nameof(LoiterPostfix)));
            harmony.Patch(flight.Arrive, transpiler: new HarmonyMethod(typeof(SortieOverHome), nameof(ArriveTranspiler)));
            harmony.Patch(flight.Away, postfix: new HarmonyMethod(typeof(SortieOverHome), nameof(AwayPostfix)));
            flights.Add(flight);
            report.On(feature);
        }
        if (flights.Count == 0) return;
        // Vehicle Framework's own steps, once for every copy.
        harmony.Patch(canTarget, prefix: new HarmonyMethod(typeof(SortieOverHome), nameof(CanTargetPrefix)) { priority = Priority.First },
            finalizer: new HarmonyMethod(typeof(SortieOverHome), nameof(CanTargetFinalizer)));
        harmony.Patch(options, postfix: new HarmonyMethod(typeof(SortieOverHome), nameof(OptionsPostfix)) { priority = Priority.Last });
        harmony.Patch(localMap, prefix: new HarmonyMethod(typeof(SortieOverHome), nameof(OrderedPrefix)) { priority = Priority.First });
    }

    // ---- Who is on a sortie ----

    /// <summary>Whether the player is choosing this jet's sortie target now (its copy's SortieTargeting, read by name).</summary>
    internal static bool Targeting(VehiclePawn? vehicle) =>
        vehicle != null && (ReferenceEquals(vehicle, lowered) || For(vehicle)?.SortieTargeting.GetValue(vehicle) is true);

    /// <summary>Whether a vehicle is a jet of one of the copies found; false when its code was not found.</summary>
    internal static bool IsFixedWing(VehiclePawn vehicle) => For(vehicle) is { } flight && flight.FixedWingProps.GetValue(vehicle) != null;

    internal static Map? HomeAt(GlobalTargetInfo target) =>
        target.WorldObject is MapParent { HasMap: true } parent && parent.Map.IsPlayerHome ? parent.Map : null;

    // ---- The target check: the framework's home refusal stood down for a sortie ----

    /// <summary>Lowers the jet's sortie flag while the target check runs, so the framework's home refusal (in its prefix and each mod's 1.7
    /// copy, which read that flag) lets a home through; every other check they and Vehicle Framework make still runs. The finalizer puts the
    /// flag back whatever happens.</summary>
    public static void CanTargetPrefix(CompVehicleLauncher __instance, GlobalTargetInfo target, out bool __state)
    {
        __state = false;
        VehiclePawn vehicle = __instance.Vehicle;
        if (!target.IsValid || !Targeting(vehicle) || !IsFixedWing(vehicle) || HomeAt(target) == null) return;
        For(vehicle)!.SetSortieTargeting.Invoke(vehicle, new object[] { false });
        lowered = vehicle;
        __state = true;
    }

    public static void CanTargetFinalizer(CompVehicleLauncher __instance, bool __state)
    {
        if (!__state) return;
        lowered = null;
        For(__instance.Vehicle)?.SetSortieTargeting.Invoke(__instance.Vehicle, new object[] { true });
    }

    /// <summary>The destination reads "fly sortie to", not the framework's "land at", for a sortie to a home.</summary>
    public static void OptionsPostfix(VehiclePawn ___vehicle, GlobalTargetInfo target, ref IEnumerable<ArrivalOption> __result)
    {
        if (__result == null || !Targeting(___vehicle) || !IsFixedWing(___vehicle)) return;
        Map? home = HomeAt(target);
        if (home == null) return;
        TaggedString label = "PhobosAircraftPatches_FlySortieTo".Translate(home.Parent.LabelCap.Named("PLACE"));
        __result = __result.Where(option => option.continueWith != null)
            .Select(option => new ArrivalOption(label, option.continueWith) { AcceptanceReport = option.AcceptanceReport })
            .ToList();
    }

    /// <summary>On a home the jet is sent to strike, the point to circle is chosen as on any strike map, not a runway to land on.</summary>
    public static void LoiterPostfix(LandingTargeter targeter, ref Map map, ref bool __result)
    {
        if (__result) return;
        Map? current = Find.CurrentMap;
        if (current is not { IsPlayerHome: true }) return;
        if (Traverse.Create(targeter).Field("vehicle").GetValue() is not VehiclePawn vehicle || !Targeting(vehicle) || !IsFixedWing(vehicle)) return;
        map = current;
        __result = true;
    }

    // ---- The flight: remembered from the order to the time it is over another map ----

    /// <summary>Notes the order (a point chosen on a map, before the framework lowers the sortie flag): a sortie to a home is remembered, any
    /// other order forgets an earlier one.</summary>
    public static void OrderedPrefix(VehiclePawn vehicle, MapParent mapParent)
    {
        if (vehicle == null) return;
        sorties.Remove(vehicle);
        if (mapParent is { HasMap: true } && mapParent.Map.IsPlayerHome && Targeting(vehicle) && IsFixedWing(vehicle))
            sorties.Add(vehicle, new Over(mapParent.Map));
    }

    static bool ReadsHomeOnce(MethodInfo arrive) => PatchProcessor.GetOriginalInstructions(arrive).Any(i => i.Calls(HomeGetter));

    static readonly MethodInfo HomeGetter = AccessTools.PropertyGetter(typeof(Map), nameof(Map.IsPlayerHome));

    /// <summary>The framework's arrival asks once whether the map is a home: land if so, come in to circle the clicked point if not. For a jet
    /// on a sortie to this home, the answer is "not": the first IsPlayerHome becomes ArrivesHome(map, vehicle).</summary>
    public static IEnumerable<CodeInstruction> ArriveTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        bool done = false;
        foreach (CodeInstruction instruction in instructions)
        {
            if (!done && instruction.Calls(HomeGetter))
            {
                done = true;
                yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(instruction);
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SortieOverHome), nameof(ArrivesHome)));
                continue;
            }
            yield return instruction;
        }
    }

    public static bool ArrivesHome(Map map, VehiclePawn vehicle)
    {
        if (!sorties.TryGetValue(vehicle, out Over? over)) return map.IsPlayerHome;
        if (over.Map != map)
        {
            sorties.Remove(vehicle);
            return map.IsPlayerHome;
        }
        if (SeenInPlay.On)
        {
            string variant = HasRunwayFor(vehicle, map) ? "runway" : "no-runway";
            if (Seen.Wants("patches.sortie-over-home", vehicle, variant))
                Seen.Note("patches.sortie-over-home", vehicle, "in to circle " + map.Parent?.LabelCap + " (" + variant + ")", variant);
        }
        return false;
    }

    /// <summary>Over a home it was sent to strike that has no runway it fits, the jet counts as away, so it flies back to a home with one.</summary>
    public static void AwayPostfix(object __instance, ref bool __result)
    {
        if (__result) return;
        Flight? flight = null;
        foreach (Flight each in flights)
            if (each.Controller.IsInstanceOfType(__instance)) flight = each;
        if (flight?.ControllerVehicle.GetValue(__instance) is not VehiclePawn vehicle || !sorties.TryGetValue(vehicle, out Over? over)) return;
        if (!vehicle.Spawned || vehicle.Map != over.Map)
        {
            sorties.Remove(vehicle);
            return;
        }
        if (!HasRunwayFor(vehicle, over.Map)) __result = true;
    }

    /// <summary>The framework's own test for a home to return to: a beacon runway at least as long and as wide as the jet needs.</summary>
    static bool HasRunwayFor(VehiclePawn vehicle, Map map)
    {
        // Each copy keeps its own runways component on a map, so the jet's own copy is read.
        if (For(vehicle) is not { } flight || flight.FixedWingProps.GetValue(vehicle) is not { } props) return false;
        int length = (int)flight.RunwayLength.GetValue(props)!;
        int width = (int)flight.RunwayWidth.GetValue(props)!;
        MapComponent? component = map.components.FirstOrDefault(c => flight.RunwaysComponent.IsInstanceOfType(c));
        if (component == null || flight.Runways.GetValue(component) is not IEnumerable<CellRect> rects) return false;
        foreach (CellRect rect in rects)
            if (Math.Max(rect.Width, rect.Height) >= length && Math.Min(rect.Width, rect.Height) >= width) return true;
        return false;
    }
}
