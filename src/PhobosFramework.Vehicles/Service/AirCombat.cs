using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Phobos.Framework;
using Phobos.Framework.Defs;
using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Air combat, game side (Framework 0.119.0; owner, 13 and 17 Nov 2026; docs/development/air-defence-tie-ins.md, AD2, AD3 and AD7;
/// docs/development/vehicle-avionics.md, A12, A14 and A16). The one place that decides what another mod's air defence does to our aircraft
/// and what their avionics do about it. It names no other mod's types: the tie-in that does (PhobosFramework.Vehicles.Dmse, loaded only with
/// The Dead Man's Switch - Expedition) reads and calls these. Every faction's aircraft; nothing here is saved except through the vehicle
/// itself (its parts' health and its cargo).
/// </summary>
public static class AirCombat
{
    const float WorkingFrom = 0.6f;
    public const string WarningQuantity = "warningReceiver";
    public const string CountermeasuresQuantity = "countermeasures";
    public const string OxygenQuantity = "crewOxygen";

    static PhobosAirCombatRulesDef Rules => PhobosRulesDef.Current<PhobosAirCombatRulesDef>();

    // ---- Capabilities ----

    /// <summary>The vehicle's systems, spawned or not (an aircraft in flight on the world map is not spawned).</summary>
    public static CompPhobosVehicleSystems? Systems(VehiclePawn? vehicle) =>
        vehicle == null ? null : CompPhobosVehicleSystems.Of(vehicle) ?? vehicle.GetComp<CompPhobosVehicleSystems>();

    /// <summary>Whether one of the avionics capabilities works now: carried, its part sound enough and powered.</summary>
    public static bool Works(VehiclePawn? vehicle, string quantity)
    {
        if (Systems(vehicle) is not { Usable: true } comp) return false;
        // Built first if nothing has looked yet (Framework 0.120.5, fault-read A6): another faction's systems are made on first look, so its
        // receiver read as broken, and its dodge was cut, until it was hit or the player selected it.
        _ = comp.States;
        return comp.TryGetQuantity(quantity, out float value) && value >= WorkingFrom;
    }

    /// <summary>Whether the aircraft carries the capability at all (whatever its state).</summary>
    public static bool Carries(VehiclePawn? vehicle, Func<AvionicsFit, bool> capability) =>
        Systems(vehicle)?.Props.loadout?.avionics is { } fit && capability(fit);

    /// <summary>How the aircraft looks to an air defence: its loadout's figures, or the rules' defaults.</summary>
    public static AirTrackSpec Track(VehiclePawn? vehicle) => Systems(vehicle)?.Props.loadout?.avionics?.airTrack ?? DefaultTrack;

    static readonly AirTrackSpec DefaultTrack = new();

    /// <summary>Whether an aircraft that cruises high must fly low now, its oxygen out (A14).</summary>
    public static bool FliesLow(VehiclePawn? vehicle) =>
        Carries(vehicle, fit => fit.cruisesHigh) && !Works(vehicle, OxygenQuantity);

    // ---- Countermeasures (A16) ----

    /// <summary>Countermeasure cartridges in the aircraft's cargo.</summary>
    public static int Cartridges(VehiclePawn? vehicle)
    {
        ThingDef? def = Rules.cartridgeDef;
        ThingOwner? cargo = vehicle?.inventory?.innerContainer;
        return def == null || cargo == null ? 0 : cargo.TotalStackCountOfDef(def);
    }

    /// <summary>Whether the dispensers would fire a salvo now: they work and a salvo's cartridges are aboard.</summary>
    public static bool CanDispense(VehiclePawn? vehicle) =>
        Works(vehicle, CountermeasuresQuantity) && Cartridges(vehicle) >= Math.Max(1, Rules.cartridgesPerSalvo);

    /// <summary>Fires one salvo: takes its cartridges from the cargo. False, and nothing taken, when it cannot.</summary>
    public static bool SpendSalvo(VehiclePawn vehicle)
    {
        if (!CanDispense(vehicle)) return false;
        ThingDef def = Rules.cartridgeDef!;
        int left = Math.Max(1, Rules.cartridgesPerSalvo);
        ThingOwner cargo = vehicle.inventory.innerContainer;
        for (int i = cargo.Count - 1; i >= 0 && left > 0; i--)
        {
            Thing thing = cargo[i];
            if (thing.def != def) continue;
            int take = Math.Min(left, thing.stackCount);
            thing.SplitOff(take).Destroy();
            left -= take;
        }
        Seen.Note("aircraft.countermeasures-salvo", vehicle);
        return true;
    }

    /// <summary>The factor countermeasures put on an air defence's chance to hit this aircraft on the world map.</summary>
    public static float HitChanceFactor(VehiclePawn vehicle)
    {
        PhobosAirCombatRulesDef rules = Rules;
        return (float)AirCombatRule.HitChanceFactor(Works(vehicle, CountermeasuresQuantity), Cartridges(vehicle) >= Math.Max(1, rules.cartridgesPerSalvo),
            Works(vehicle, WarningQuantity), rules.hitChanceFactorCued, rules.hitChanceFactorLate);
    }

    /// <summary>The factor chaff puts on the time a fire-control radar needs to lock on to this aircraft.</summary>
    public static float LockTimeFactor(VehiclePawn vehicle)
    {
        PhobosAirCombatRulesDef rules = Rules;
        return (float)AirCombatRule.LockTimeFactor(Works(vehicle, CountermeasuresQuantity), Cartridges(vehicle) >= Math.Max(1, rules.cartridgesPerSalvo),
            Works(vehicle, WarningQuantity), rules.lockTimeFactorCued);
    }

    // ---- The warning receiver (AD2) ----

    public enum Warning
    {
        Tracked,
        LockedOn,
        Launched,
    }

    static readonly ConditionalWeakTable<VehiclePawn, Dictionary<Warning, int>> lastWarned = new();

    /// <summary>
    /// Tells the player what the warning receiver heard, if the aircraft is theirs and the receiver works; a warning of one kind is not
    /// repeated within the rules' hours. On a launch the crew fires chaff and flares (spent here, once per shot), and the message says so.
    /// </summary>
    public static void Heard(AerialVehicleInFlight flight, Warning warning, string site)
    {
        VehiclePawn? vehicle = flight?.Vehicle;
        if (vehicle == null) return;
        bool receiver = Works(vehicle, WarningQuantity);
        bool launched = warning == Warning.Launched;
        // Fired on the receiver's cue, or late, by eye, without it: either way a shot is answered while cartridges last.
        bool fired = launched && SpendSalvo(vehicle);
        // Spent without a receiver, the player still hears of it (Framework 0.120.5, fault-read A13): the cartridges went silently.
        if (fired && !receiver && vehicle.Faction == Faction.OfPlayer)
        {
            Messages.Message("PhobosAirCombat_FiredByEye".Translate(vehicle.LabelShort.Named("VEHICLE")), new GlobalTargetInfo(flight), MessageTypeDefOf.NegativeEvent);
        }
        if (vehicle.Faction != Faction.OfPlayer || !receiver) return;
        Seen.Note("aircraft.warning-receiver", vehicle, warning.ToString());
        Dictionary<Warning, int> last = lastWarned.GetOrCreateValue(vehicle);
        int now = Find.TickManager.TicksGame;
        int cooldown = (int)(Rules.warningCooldownHours * GenDate.TicksPerHour);
        if (!launched && last.TryGetValue(warning, out int tick) && now - tick < cooldown) return;
        last[warning] = now;
        NamedArgument who = vehicle.LabelShort.Named("VEHICLE");
        NamedArgument where = site.Named("SITE");
        string text = warning switch
        {
            Warning.Tracked => "PhobosAirCombat_WarnTracked".Translate(who, where),
            Warning.LockedOn => "PhobosAirCombat_WarnLockedOn".Translate(who, where),
            _ when fired => "PhobosAirCombat_WarnLaunchedFired".Translate(who, where),
            _ when Carries(vehicle, fit => fit.countermeasures) => "PhobosAirCombat_WarnLaunchedNoCartridges".Translate(who, where),
            _ => "PhobosAirCombat_WarnLaunched".Translate(who, where),
        };
        MessageTypeDef type = warning == Warning.Tracked ? MessageTypeDefOf.CautionInput : MessageTypeDefOf.ThreatBig;
        Messages.Message(text, new GlobalTargetInfo(flight), type, historical: false);
    }

    // ---- A hit in flight (AD3) ----

    /// <summary>
    /// An interceptor hit an aircraft in flight on the world map: the damage is shared out over its hit areas, as any hit is (people in
    /// seats there can be hurt too). The aircraft flies on, damaged, unless it can no longer fly: its controls below the launch floor, every
    /// engine out or its airframe failed. Then it comes down at a crash site, as one out of fuel does (owner, 17 Nov 2026: "down only if
    /// unflyable"). Its crew may be short of breath if its oxygen failed at height (the player's setting).
    /// </summary>
    public static void Intercepted(AerialVehicleInFlight flight, Faction? by)
    {
        VehiclePawn? vehicle = flight?.Vehicle;
        if (vehicle == null || flight!.Destroyed) return;
        PhobosAirCombatRulesDef rules = Rules;
        CompPhobosVehicleSystems? comp = Systems(vehicle);
        bool oxygenBefore = Works(vehicle, OxygenQuantity);
        bool high = Carries(vehicle, fit => fit.cruisesHigh) && oxygenBefore;

        (int hits, double each) = AirCombatRule.SplitDamage(rules.interceptDamage, rules.interceptHits);
        DamageDef damage = rules.interceptDamageDef ?? DamageDefOf.Bomb;
        // Framework 0.120.5 (fault-read A2): under Combat Extended the penetration is on its scale (MPa for a blast), as armour-worthy as its
        // own blasts of the same damage (CombatExtendedRules, as cook-off does). The Def's figure is on the game's scale, and against CE's
        // armour every hit was deflected: the Harrier could never be brought down there.
        float penetration = OptionalMods.IsActive(OptionalMods.CombatExtended)
            ? CombatExtendedRules.BlastPenetrationForDamage(UnityEngine.Mathf.RoundToInt((float)each), true)
            : rules.interceptArmourPenetration;
        IntVec2 size = vehicle.VehicleDef.Size;
        for (int i = 0; i < hits; i++)
        {
            var cell = new IntVec2(Rand.RangeInclusive(-(size.x - 1) / 2, size.x / 2), Rand.RangeInclusive(-(size.z - 1) / 2, size.z / 2));
            vehicle.statHandler.TakeDamage(new DamageInfo(damage, (float)each, penetration), cell);
        }
        comp?.UpdateNow(evenIfNoTimePassed: true);
        Seen.Note("aircraft.hit-in-flight", vehicle, by?.Name);

        if (high && !Works(vehicle, OxygenQuantity)) Hypoxia(vehicle);

        float control = vehicle.GetStatValue(VehicleStatDefOf.FlightControl);
        (int working, int engines) = comp?.Powertrain?.Engines() ?? (1, 1);
        bool airframeFailed = vehicle.GetStatValue(VehicleStatDefOf.BodyIntegrity) < rules.airframeFailsBelow;
        bool staysUp = AirCombatRule.StaysUp(control, working, engines, airframeFailed);
        NamedArgument who = vehicle.LabelShort.Named("VEHICLE");
        NamedArgument whom = (by?.Name ?? "PhobosAirCombat_Unknown".Translate().Resolve()).Named("FACTION");
        if (staysUp)
        {
            if (vehicle.Faction == Faction.OfPlayer)
            {
                Messages.Message("PhobosAirCombat_HitFliesOn".Translate(who, whom), new GlobalTargetInfo(flight), MessageTypeDefOf.NegativeEvent);
            }
            return;
        }
        string why = airframeFailed ? "PhobosAirCombat_DownAirframe".Translate(whom).Resolve()
            : working <= 0 && engines > 0 ? "PhobosAirCombat_DownEngines".Translate(whom).Resolve()
            : "PhobosAirCombat_DownControls".Translate(whom).Resolve();
        Seen.Note("aircraft.shot-down", vehicle, why);
        // Vehicle Framework's own crash: a site with the wreck and its crew, as when an aircraft runs out of fuel.
        flight.InitiateCrashEvent(null, why);
    }

    /// <summary>The crew's oxygen failed at height (A14): each person aboard is short of breath for a while, if the player chose it.</summary>
    static void Hypoxia(VehiclePawn vehicle)
    {
        PhobosAirCombatRulesDef rules = Rules;
        if (!VehicleSystemsMod.Settings.altitudeHypoxia || rules.hypoxiaHediff == null) return;
        int count = 0;
        foreach (Pawn pawn in vehicle.AllPawnsAboard)
        {
            if (pawn.Dead || pawn.health == null || !pawn.RaceProps.IsFlesh) continue;
            Hediff hediff = pawn.health.GetOrAddHediff(rules.hypoxiaHediff);
            hediff.Severity = UnityEngine.Mathf.Max(hediff.Severity, rules.hypoxiaSeverity);
            count++;
        }
        if (count == 0) return;
        Seen.Note("aircraft.altitude-hypoxia", vehicle, count.ToString());
        if (vehicle.Faction == Faction.OfPlayer)
        {
            Messages.Message("PhobosAirCombat_Hypoxia".Translate(vehicle.LabelShort.Named("VEHICLE")), MessageTypeDefOf.NegativeHealthEvent);
        }
    }

    // ---- Evasion on a map (A12 and the Harrier's salvo) ----

    static readonly ConditionalWeakTable<VehiclePawn, StrongBox<int>> salvoUntil = new();

    static readonly List<Func<VehiclePawn, bool>> mapSalvoVehicles = new();

    /// <summary>A tie-in names the vehicles whose dodge chance a salvo on a map adds to (the Harrier's, through D'thorga Patches): only they get
    /// the button, since a salvo does nothing where no dodge chance exists.</summary>
    public static void RegisterMapSalvo(Func<VehiclePawn, bool> vehicles)
    {
        if (vehicles != null && !mapSalvoVehicles.Contains(vehicles)) mapSalvoVehicles.Add(vehicles);
    }

    /// <summary>Whether this vehicle gets the salvo button: it carries dispensers and a tie-in scales its dodge chance.</summary>
    public static bool HasMapSalvo(VehiclePawn vehicle)
    {
        if (!Carries(vehicle, fit => fit.countermeasures)) return false;
        foreach (Func<VehiclePawn, bool> check in mapSalvoVehicles)
        {
            if (check(vehicle)) return true;
        }
        return false;
    }

    /// <summary>Whether a salvo fired on a map still covers the aircraft.</summary>
    public static bool SalvoActive(VehiclePawn vehicle) =>
        salvoUntil.TryGetValue(vehicle, out StrongBox<int>? until) && Find.TickManager.TicksGame < until.Value;

    /// <summary>The cartridges one salvo uses.</summary>
    public static int CartridgesPerSalvo => Math.Max(1, Rules.cartridgesPerSalvo);

    /// <summary>How long a salvo fired from the map lasts, in seconds at normal speed, for its button's text (from the rules' ticks).</summary>
    public static int SalvoSeconds => Math.Max(1, (int)Math.Round(Rules.mapSalvoTicks / (double)GenTicks.TicksPerRealSecond));

    /// <summary>The cartridges' own picture, for the salvo button; if it cannot be found, our switch icon, never the game's red cross.</summary>
    public static UnityEngine.Texture2D CartridgeIcon =>
        Rules.cartridgeDef?.uiIcon is { } icon && icon != BaseContent.BadTex ? icon : SystemIcons.Switch;

    /// <summary>The salvo button's action: fires, and says what it did or why it could not.</summary>
    public static void MapSalvoNow(VehiclePawn vehicle, bool airborne)
    {
        if (MapSalvo(vehicle, airborne) is { } refusal)
        {
            Messages.Message(refusal, vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        Messages.Message("PhobosAirCombat_SalvoFired".Translate(vehicle.LabelShort.Named("VEHICLE"), Cartridges(vehicle).Named("LEFT")), vehicle,
            MessageTypeDefOf.SilentInput, historical: false);
    }

    /// <summary>Fires a salvo on a map: a few seconds' extra dodge. Why it cannot, as a translated reason, or null when it fired.</summary>
    public static string? MapSalvo(VehiclePawn vehicle, bool airborne)
    {
        if (MapSalvoRefusal(vehicle, airborne) is { } refusal) return refusal;
        SpendSalvo(vehicle);
        salvoUntil.GetOrCreateValue(vehicle).Value = Find.TickManager.TicksGame + Rules.mapSalvoTicks;
        Seen.Note("aircraft.map-salvo", vehicle);
        return null;
    }

    /// <summary>Why a salvo cannot be fired on a map now, or null.</summary>
    public static string? MapSalvoRefusal(VehiclePawn vehicle, bool airborne)
    {
        if (!airborne) return "PhobosAirCombat_SalvoNotAirborne".Translate(vehicle.LabelShort.Named("VEHICLE"));
        if (!Works(vehicle, CountermeasuresQuantity)) return "PhobosAirCombat_SalvoDispensersOut".Translate(vehicle.LabelShort.Named("VEHICLE"));
        if (Cartridges(vehicle) < Math.Max(1, Rules.cartridgesPerSalvo)) return "PhobosAirCombat_SalvoNoCartridges".Translate(vehicle.LabelShort.Named("VEHICLE"));
        if (SalvoActive(vehicle)) return "PhobosAirCombat_SalvoAlready".Translate(vehicle.LabelShort.Named("VEHICLE"));
        return null;
    }

    /// <summary>
    /// An aircraft's chance to dodge a shot on a map (A12 with the salvo): its author's figure, scaled by how well its controls answer and by
    /// its warning receiver, plus a salvo's bonus while it lasts. A vehicle without our systems keeps its author's figure.
    /// </summary>
    public static float Evasion(VehiclePawn vehicle, float authorsChance)
    {
        if (authorsChance <= 0f || Systems(vehicle) is not { Usable: true }) return authorsChance;
        PhobosAirCombatRulesDef rules = Rules;
        bool carriesWarning = Carries(vehicle, fit => fit.warning);
        bool warning = !carriesWarning || Works(vehicle, WarningQuantity);
        float control = vehicle.GetStatValue(VehicleStatDefOf.FlightControl);
        float bonus = SalvoActive(vehicle) ? rules.mapSalvoEvasionBonus : 0f;
        float chance = (float)AirCombatRule.Evasion(authorsChance, control, warning, rules.noWarningEvasionFactor, bonus, Math.Max(rules.evasionCap, authorsChance));
        if (chance < authorsChance) Seen.Note("aircraft.evasion-reduced", vehicle, chance.ToString("0.00"));
        return chance;
    }
}

/// <summary>
/// Reasons another mod's state may refuse an aircraft's take-off (Framework 0.119.0; AD7: an air defence on alert closes its airspace). A tie-in
/// registers a check; the launch check every launch path and the launch button ask reads them after our own systems'. A check that throws is
/// dropped for the session, with one error.
/// </summary>
public static class LaunchChecks
{
    static readonly List<Func<VehiclePawn, string?>> checks = new();

    public static void Register(Func<VehiclePawn, string?> check)
    {
        if (check != null && !checks.Contains(check)) checks.Add(check);
    }

    /// <summary>The first registered refusal for this aircraft, or null.</summary>
    public static string? Refusal(VehiclePawn vehicle)
    {
        for (int i = 0; i < checks.Count; i++)
        {
            try
            {
                if (checks[i](vehicle) is { } refusal) return refusal;
            }
            catch (Exception error)
            {
                VehicleSystemsMod.Logger.Error("a launch check failed and is switched off until the game restarts: " + error);
                checks.RemoveAt(i--);
            }
        }
        return null;
    }
}

/// <summary>The Dead Man's Switch - Expedition, whose air defence our tie-in assembly joins (Framework 0.119.0).</summary>
public static class DmseTieIn
{
    public const string PackageId = "Aoba.DeadManSwitch.Expedition";
}
