using System.Collections.Generic;
using Phobos.Framework;
using Phobos.Framework.Effects;
using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// What a stage (or an event) does to the vehicle. Subclass it in any mod. OnEnter and OnExit run when the stage starts and ends,
/// Apply every interval while it lasts (with the hours that passed), and the refusal and factor methods are read whenever the game
/// asks. <c>when</c> limits the effect to a condition. Effects with consequences (damage, refusals, slower vehicles) are skipped when
/// the player chooses figures only.
/// </summary>
public abstract class VehicleSystemEffect
{
    public VehicleSystemCondition? when;

    /// <summary>Whether the effect changes what the vehicle can do. Figures, messages and flecks are not consequences.</summary>
    public virtual bool Consequential => true;

    public bool Active(SystemContext context) =>
        (!Consequential || !VehicleSystemsMod.Settings.showOnly) && (when == null || when.Holds(context));

    public virtual void OnEnter(SystemContext context) { }
    public virtual void OnExit(SystemContext context) { }
    public virtual void Apply(SystemContext context, float hours) { }
    public virtual string? DraftRefusal(SystemContext context) => null;
    public virtual string? MoveRefusal(SystemContext context) => null;
    /// <summary>Why an aircraft may not take off while the stage lasts (Framework 0.106.0), or null.</summary>
    public virtual string? LaunchRefusal(SystemContext context) => null;
    /// <summary>What to tell the player as the aircraft takes off while the stage lasts, where a refusal was set to warn only (Framework 0.110.0).</summary>
    public virtual string? LaunchWarning(SystemContext context) => null;
    /// <summary>A factor on the fuel an aircraft burns on the world map while the stage lasts (Framework 0.110.0, avionics A2; 1 for no change).</summary>
    public virtual float FlightFuelFactor => 1f;
    /// <summary>A factor on the fuel the engine burns while the stage lasts, beside the fittings' and tunes' (Framework 0.123.0: a clogged air
    /// filter; 1 for no change). Read where the drivetrain model settles the burn.</summary>
    public virtual float EngineFuelFactor => 1f;
    /// <summary>The share of the engine's power still available while the stage lasts (Framework 0.130.0, step S4); 1 for no loss.</summary>
    public virtual float EnginePowerFactor => 1f;
    /// <summary>How many switched loads a hit to the wiring has shorted while the stage lasts (Framework 0.123.0, cross-links G12; 0 for none).</summary>
    public virtual int ShortedLoads => 0;
    public virtual float StatFactor(VehicleStatDef stat) => 1f;
    /// <summary>The one stat this effect changes, if any (Framework 0.130.0, step S8: the update asks it rather than every stat in turn).
    /// An effect overriding <see cref="StatFactor"/> must name its stat here.</summary>
    public virtual VehicleStatDef? FactorStat => null;
    /// <summary>A factor on how fast an electric vehicle charges from the grid while the stage lasts (1 for no change).</summary>
    public virtual float ChargeFactor => 1f;
}

/// <summary>
/// Takes health from a part bound to the role without armour or the part's own reactions: <c>perHour</c> while the stage lasts,
/// and <c>amount</c> once when it starts or the event fires (a cold start's wear).
/// </summary>
public class Effect_DamagePart : VehicleSystemEffect
{
    public string role = "";
    public float perHour;
    /// <summary>A share of the part's whole health each hour, so a small engine and a big one last the same time.</summary>
    public float sharePerHour;
    public float amount;
    /// <summary>A one-off loss of this share of the part's whole health on entry (Framework 0.106.0: a pump run dry loses 5 %).</summary>
    public float share;

    public override void OnEnter(SystemContext context)
    {
        if (amount > 0f || share > 0f) context.Comp.WearRole(role, amount, share);
    }

    public override void Apply(SystemContext context, float hours)
    {
        if (perHour > 0f || sharePerHour > 0f) context.Comp.WearRole(role, perHour * hours, sharePerHour * hours);
    }
}

/// <summary>The vehicle cannot be started; the reason is a translation key with {VEHICLE}.</summary>
public class Effect_RefuseDraft : VehicleSystemEffect
{
    public string reason = "";
    /// <summary>What turning the key sounds like while this refuses (clicks for a flat battery, a crank that dies when cold); a petrol
    /// engine plays <c>soundPetrol</c> when given. Played from the dashboard's key, the one place a start is tried.</summary>
    public SoundDef? sound;
    public SoundDef? soundPetrol;
    /// <summary>The puffs from the exhaust as the key turns the engine over without a start (Framework 0.321.0: white for gelled fuel cranked
    /// through unburnt); 0 for none (a flat battery only clicks).</summary>
    public int crankPuffs;
    public FxTint crankTint = FxTint.Steam;
    public override string? DraftRefusal(SystemContext context) => reason.Translate(context.Vehicle.LabelShort.Named("VEHICLE")).Resolve();
}

/// <summary>The vehicle can be started but not driven; the reason is a translation key with {VEHICLE}.</summary>
public class Effect_RefuseMove : VehicleSystemEffect
{
    public string reason = "";
    public override string? MoveRefusal(SystemContext context) => reason.Translate(context.Vehicle.LabelShort.Named("VEHICLE")).Resolve();
}

/// <summary>
/// The aircraft may not take off; the reason is a translation key with {VEHICLE} (Framework 0.106.0, avionics A8: flight controls too
/// damaged to fly). Read by the launch check, which every launch path and the launch button ask.
/// </summary>
public class Effect_RefuseLaunch : VehicleSystemEffect
{
    public string reason = "";
    /// <summary>A player's setting that turns the refusal into a warning at take-off (Framework 0.110.0: blindFlightWarnOnly).</summary>
    public string? warnOnlySetting;
    /// <summary>What the player is told at take-off when warned only; the reason when empty.</summary>
    public string? warning;

    bool WarnOnly => warnOnlySetting != null && VehicleSystemsMod.Settings.FlagOn(warnOnlySetting);

    public override string? LaunchRefusal(SystemContext context)
    {
        if (WarnOnly) return null;
        // Seen in play (Framework 0.301.0, row 99): a take-off refused by a stage the warn-only setting could soften (blind flight).
        if (warnOnlySetting != null) Seen.Note("aircraft.blind-launch", context.Vehicle, null, "refused");
        return Reason(context);
    }

    public override string? LaunchWarning(SystemContext context)
    {
        if (!WarnOnly) return null;
        // Seen in play (Framework 0.301.0, row 99): with the warn-only setting on, the same take-off warned of instead.
        Seen.Note("aircraft.blind-launch", context.Vehicle, null, "warned");
        return warning.NullOrEmpty() ? Reason(context) : warning!.Translate(context.Vehicle.LabelShort.Named("VEHICLE")).Resolve();
    }

    string Reason(SystemContext context) => reason.Translate(context.Vehicle.LabelShort.Named("VEHICLE")).Resolve();
}

/// <summary>Multiplies the fuel an aircraft burns on the world map while the stage lasts (Framework 0.110.0, avionics A2: a wandering track).</summary>
public class Effect_FlightFuel : VehicleSystemEffect
{
    public float factor = 1f;
    public override float FlightFuelFactor => factor;
}

/// <summary>Multiplies the fuel the engine burns while the stage lasts (Framework 0.123.0, cross-links G8: a clogged air filter). It reaches the
/// burn through the drivetrain model, so with the model switched off it changes nothing.</summary>
public class Effect_EngineFuel : VehicleSystemEffect
{
    public float factor = 1f;
    public override float EngineFuelFactor => factor;
}

/// <summary>
/// Shorts <c>loads</c> of the vehicle's switched loads while the stage lasts (Framework 0.123.0, cross-links G12): a hit to the battery or the
/// alternator cuts a circuit, and what is on it stops working until the part is repaired. Which ones is worked out from the vehicle and the
/// part (WiringRule), the same at every look, so nothing about it is saved. The player is told which when it happens.
/// </summary>
public class Effect_ShortLoads : VehicleSystemEffect
{
    public int loads = 1;
    public override int ShortedLoads => loads;
}

/// <summary>
/// Takes power from the engine while the stage lasts (Framework 0.130.0; docs/development/vehicle-engine-loads.md, step S4; the owner's pick of
/// 18 Nov 2026): a choked air filter, a warped head, a starved fuel feed. In the full drivetrain model it lowers the power the engine can
/// give, so the vehicle slows only where power runs short (a climb, mud, a tow, a heavy load) and the model's figures show the loss. A
/// vehicle without that model (drivetrain off or simplified, an aircraft, a boat on the simple model) takes it as the same factor on its
/// move speed, as before.
/// </summary>
public class Effect_EnginePower : VehicleSystemEffect
{
    public float factor = 1f;
    public override float EnginePowerFactor => factor;
}

/// <summary>Multiplies one of Vehicle Framework's stats (move speed, cargo) while the stage lasts.</summary>
public class Effect_StatFactor : VehicleSystemEffect
{
    public VehicleStatDef? stat;
    public float factor = 1f;
    public override float StatFactor(VehicleStatDef asked) => asked == stat ? factor : 1f;
    public override VehicleStatDef? FactorStat => stat;
}

/// <summary>Stops the engine: the vehicle is undrafted when the stage starts.</summary>
public class Effect_ForceUndraft : VehicleSystemEffect
{
    public override void OnEnter(SystemContext context)
    {
        if (context.Vehicle.Drafted) context.Vehicle.ignition.Drafted = false;
    }
}

/// <summary>
/// Pushes heat into the room the vehicle stands in through the game's own temperature system, the way a heater does
/// (heat per second, scaled by the input).
/// </summary>
public class Effect_PushHeat : VehicleSystemEffect
{
    public float heatPerSecond;
    public VehicleSystemInput? scaleBy;
    public override bool Consequential => false;

    public override void Apply(SystemContext context, float hours)
    {
        VehiclePawn vehicle = context.Vehicle;
        if (!vehicle.Spawned) return;
        float scale = scaleBy?.Evaluate(context) ?? 1f;
        if (scale <= 0f) return;
        float seconds = hours * ElapsedRule.TicksPerHour / 60f;
        GenTemperature.PushHeat(vehicle.Position, vehicle.Map, heatPerSecond * scale * seconds);
    }
}

/// <summary>
/// Puts a gas into the room the vehicle stands in, by its real weight (Framework 0.187.0; docs/development/vehicle-emergent-play.md, EM20:
/// an engine's carbon monoxide in a closed garage). Grams an hour, scaled by the input, become the framework's room gas through the gas's
/// molar mass and what its full concentration stands for (GasMassRule). Outdoors, in a doorway or under an open roof the framework loses
/// it at once, so only a closed room fills. Behind its setting, when one is named; only where the framework carries the gas itself.
/// D'thorga Medical is what turns breathing it into harm.
/// </summary>
public class Effect_PushGas : VehicleSystemEffect
{
    public Phobos.Framework.Gases.PhobosGasDef? gas;
    public float gramsPerHour;
    public VehicleSystemInput? scaleBy;
    /// <summary>The settings flag that switches it on (VehicleSystemsSettings.FlagOn); none means always.</summary>
    public string? setting;

    public override void Apply(SystemContext context, float hours)
    {
        if (gas == null || (setting != null && !VehicleSystemsMod.Settings.FlagOn(setting))) return;
        VehiclePawn vehicle = context.Vehicle;
        if (!vehicle.Spawned || Phobos.Framework.Gases.PhobosGases.Source(gas) != Phobos.Framework.Rules.GasSource.Own) return;
        Room? room = vehicle.Position.GetRoom(vehicle.Map);
        if (room == null || room.UsesOutdoorTemperature) return;
        float scale = scaleBy?.Evaluate(context) ?? 1f;
        if (scale <= 0f) return;
        double cells = Phobos.Framework.Rules.GasMassRule.CellsFromGrams(gramsPerHour * scale * hours, gas.molarMassGramsPerMole, gas.ppmAtFullConcentration);
        if (cells <= 0) return;
        Phobos.Framework.Gases.MapComponent_PhobosRoomGases.For(vehicle.Map)?.AddGas(vehicle.Position, gas, (float)cells);
        Seen.Note("vehicles.exhaust-indoors", vehicle, gas.defName);
    }
}

/// <summary>
/// Puffs from the vehicle while the stage lasts, or once when it starts or the event fires (<c>burst</c>), so its state shows on the map
/// (Framework 0.321.0, the owner's cards of 26 Nov 2026: vehicles' visual effects). <c>tint</c> colours it by what burns (blue oil, black
/// fuel, white water); <c>at</c> places it on the vehicle; <c>drift</c> lets the map's wind carry it. The Framework core's emitter draws it
/// evenly between updates, on screen only, and the player's Vehicle effects setting can switch it off.
/// </summary>
public class Effect_Fleck : VehicleSystemEffect
{
    public FleckDef? fleck;
    public float perHour = 20f;
    public float scale = 1f;
    public FxTint tint = FxTint.Plain;
    public FxSpot at = FxSpot.Body;
    /// <summary>How many at once when the stage starts or the event fires; 0 for none.</summary>
    public int burst;
    /// <summary>Carried off by the wind (smoke, steam); false for sparks and drips, thrown from their place.</summary>
    public bool drift = true;
    public override bool Consequential => false;

    FxEmission EmissionFor(VehiclePawn vehicle, float rate) => VehicleFx.Emission(vehicle, fleck!, tint, at, rate, scale, drift);

    public override void OnEnter(SystemContext context)
    {
        if (fleck == null || burst <= 0) return;
        VehicleFx.BurstSign(context.Vehicle, EmissionFor(context.Vehicle, 0f), burst);
        Note(context.Vehicle);
    }

    public override void Apply(SystemContext context, float hours)
    {
        if (fleck == null || perHour <= 0f) return;
        VehicleFx.HoldSign(context.Vehicle, this, EmissionFor(context.Vehicle, perHour));
        Note(context.Vehicle);
    }

    void Note(VehiclePawn vehicle)
    {
        // Seen in play (Framework 0.321.0, row 578): a fault's sign, by its colour.
        if (Seen.Wants("vehicles.fx-sign", vehicle, tint.ToString())) Seen.Note("vehicles.fx-sign", vehicle, fleck?.defName + " at " + at, tint.ToString());
    }
}

/// <summary>A message when the stage starts, at most once in the rules Def's cooldown. Key arguments: {VEHICLE}.</summary>
public class Effect_Message : VehicleSystemEffect
{
    public string message = "";
    /// <summary>Framework 0.266.1: said instead when the air around the vehicle is hotter than weather can make it (a fire, a furnace;
    /// <see cref="PhobosVehicleRulesDef.hotAirCelsius"/>), for a part that heats toward the air. Its own words name the heat as the
    /// cause; the plain message would blame a descent or hard driving and give the wrong advice. Empty: the plain message.</summary>
    public string hotAirMessage = "";
    public MessageTypeDef? type;
    public override bool Consequential => false;

    public override void OnEnter(SystemContext context)
    {
        if (context.Vehicle.Faction != Faction.OfPlayer) return;
        float cooldown = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>().messageCooldownHours;
        if (!context.Comp.MayNotify(context.StageKey, cooldown)) return;
        string key = HotAir.Pick(context, message, hotAirMessage);
        Messages.Message(key.Translate(context.Vehicle.LabelShort.Named("VEHICLE")), context.Vehicle, type ?? MessageTypeDefOf.NegativeEvent);
    }
}

/// <summary>A letter when the stage starts, for the rare and costly (a seized engine). Key arguments: {VEHICLE}.</summary>
public class Effect_Letter : VehicleSystemEffect
{
    public string label = "";
    public string text = "";
    /// <summary>Framework 0.266.1: the letter's text when the air around the vehicle is fire-hot, as <see cref="Effect_Message.hotAirMessage"/>.</summary>
    public string hotAirText = "";
    public LetterDef? letter;

    public override void OnEnter(SystemContext context)
    {
        if (context.Vehicle.Faction != Faction.OfPlayer) return;
        float cooldown = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>().letterCooldownHours;
        if (!context.Comp.MayNotify(context.StageKey + ":letter", cooldown)) return;
        NamedArgument vehicle = context.Vehicle.LabelShort.Named("VEHICLE");
        string body = HotAir.Pick(context, text, hotAirText);
        Find.LetterStack.ReceiveLetter(label.Translate(vehicle), body.Translate(vehicle), letter ?? LetterDefOf.NegativeEvent, context.Vehicle);
    }
}

/// <summary>Framework 0.266.1 (the owner's fire of 23 Nov 2026: a parked tanker's brakes "faded on the descent" while it burned): which of a
/// stage's two wordings to use, the plain one or the one blaming the heat around the vehicle.</summary>
static class HotAir
{
    public static string Pick(SystemContext context, string plain, string hotAir)
    {
        if (hotAir.NullOrEmpty() || context.Comp.Ambient <= PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>().hotAirCelsius) return plain;
        if (Seen.Wants("vehicles.hot-air-message", context.Vehicle)) Seen.Note("vehicles.hot-air-message", context.Vehicle, hotAir);
        return hotAir;
    }
}

/// <summary>Burns fuel from Vehicle Framework's tank, per hour (an engine left idling by a heater, a leak).</summary>
public class Effect_ConsumeFuel : VehicleSystemEffect
{
    public float perHour;
    /// <summary>A parameter of the system holding the rate instead, so loadouts can tune it.</summary>
    public string? perHourParameter;

    /// <summary>The rate is a share of the tank or battery's capacity an hour, not an amount: for electric vehicles, whose units of
    /// charge differ from mod to mod.</summary>
    public bool ofCapacity;

    /// <summary>What the fuel is burned for, as the vehicle tells the player (Framework 0.130.0, step S2): a heater unless the XML says
    /// otherwise (a holed tank is a leak).</summary>
    public FuelUse use = FuelUse.Heater;

    public override void Apply(SystemContext context, float hours)
    {
        CompFueledTravel? fuel = context.Vehicle.CompFueledTravel;
        if (fuel == null || fuel.EmptyTank) return;
        // Framework 0.252.0: a holed side tank that has run dry behind a shut cross-feed leaks no more.
        if (use == FuelUse.Leak && context.Comp.SideTanks.LeakSideDry)
        {
            context.Comp.SideTanks.NoteLeak(0f);
            return;
        }
        float rate = perHourParameter != null ? context.Parameter(perHourParameter) : perHour;
        if (ofCapacity) rate *= fuel.FuelCapacity;
        VehicleFuel.Burn(context.Vehicle, rate * hours, use, context.Comp);
    }
}

/// <summary>Multiplies how fast an electric vehicle charges from the grid while the stage lasts (a cold or hot battery charges slowly).</summary>
public class Effect_ChargeFactor : VehicleSystemEffect
{
    public float factor = 1f;
    public override float ChargeFactor => factor;
}

/// <summary>
/// Holds the tank or battery to a share of its capacity: a worn battery holds less. The share is <c>quantity</c>'s value, or what is
/// left of it with <c>remaining</c> (1 minus a wear figure), never below <c>floor</c>. Charge above it is lost.
/// </summary>
public class Effect_LimitCharge : VehicleSystemEffect
{
    public string quantity = "";
    public bool remaining = true;
    public float floor = 0.05f;

    public override void Apply(SystemContext context, float hours)
    {
        CompFueledTravel? fuel = context.Vehicle.CompFueledTravel;
        if (fuel == null || !context.Comp.TryGetQuantity(quantity, out float value)) return;
        float share = Mathf.Clamp(remaining ? 1f - value : value, floor, 1f);
        float cap = fuel.FuelCapacity * share;
        if (fuel.Fuel > cap + 0.01f) VehicleFuel.Burn(context.Vehicle, fuel.Fuel - cap, FuelUse.Spilled, context.Comp);
    }
}

/// <summary>
/// Changes a quantity of this vehicle (in any system) once, when the stage starts or the event fires: amount, times a parameter,
/// divided by a parameter, times an input. The starter's draw on the battery is one.
/// </summary>
public class Effect_AddQuantity : VehicleSystemEffect
{
    public string quantity = "";
    public float amount;
    public string? scaleByParameter;
    public string? divideByParameter;
    public VehicleSystemInput? scaleBy;
    public override bool Consequential => false;

    public override void OnEnter(SystemContext context)
    {
        float value = amount;
        if (scaleByParameter != null) value *= context.Parameter(scaleByParameter);
        if (divideByParameter != null)
        {
            float divisor = context.Parameter(divideByParameter);
            if (divisor == 0f) return;
            value /= divisor;
        }
        if (scaleBy != null) value *= scaleBy.Evaluate(context);
        context.Comp.AddToQuantity(quantity, value);
    }
}

/// <summary>Sets a quantity of this vehicle once, when the stage starts.</summary>
public class Effect_SetQuantity : VehicleSystemEffect
{
    public string quantity = "";
    public float value;
    public override bool Consequential => false;
    public override void OnEnter(SystemContext context) => context.Comp.SetQuantity(quantity, value);
}

/// <summary>
/// Gives a part of the role a condition (or adds to the one it has): <c>severity</c> once when the stage starts or the event fires,
/// <c>perHour</c> while the stage lasts. A cause, not a hit: boiling over warps the cylinder head.
/// </summary>
public class Effect_AddCondition : VehicleSystemEffect
{
    public PhobosVehicleConditionDef? condition;
    public string role = "";
    public float severity;
    public float perHour;

    public override void OnEnter(SystemContext context)
    {
        if (condition != null && severity > 0f) context.Comp.AddCondition(role, condition, severity, context.State.Def.defName, context.StageKey);
    }

    public override void Apply(SystemContext context, float hours)
    {
        if (condition != null && perHour > 0f) context.Comp.AddCondition(role, condition, perHour * hours, context.State.Def.defName, context.StageKey);
    }
}

/// <summary>Moves a quantity of this vehicle per hour while the stage lasts: a boil-over drains the coolant.</summary>
public class Effect_RateQuantity : VehicleSystemEffect
{
    public string quantity = "";
    public float perHour;
    public override bool Consequential => false;
    public override void Apply(SystemContext context, float hours) => context.Comp.AddToQuantity(quantity, perHour * hours);
}

/// <summary>
/// A failure rolled as the game rolls breakdowns (a mean time between failures, in hours while the stage and condition hold): a
/// part of the role loses a share of its health at once, with a message naming it. Key arguments: {VEHICLE}, {PART}.
/// </summary>
public class Effect_Breakdown : VehicleSystemEffect
{
    public string role = "";
    public float mtbHours = 1000f;
    public float healthFraction = 0.3f;
    public string message = "";
    /// <summary>What the part sounds like breaking (a tyre bursting); played at the vehicle when it breaks.</summary>
    public SoundDef? sound;
    /// <summary>Where the vehicle keeps this quantity per part, break its most worn part rather than any of the role (Framework 0.247.0 for
    /// engines; generic since 0.248.0). Null: any part of the role.</summary>
    public string? mostWornOf;
    /// <summary>What happens when it breaks, run once (Framework 0.321.0: a tyre's burst of dust and rubber).</summary>
    public List<VehicleSystemEffect> onBreak = new();

    public override void Apply(SystemContext context, float hours)
    {
        if (!Rand.Chance((float)FailureRule.ChanceIn(hours, mtbHours))) return;
        VehicleComponent? part = (mostWornOf != null ? context.Comp.PartFigures.BreakMostWorn(mostWornOf, healthFraction) : null) ?? context.Comp.BreakRole(role, healthFraction);
        if (part != null) VehicleSystemSounds.PlayOnce(context.Comp, sound);
        if (part != null) foreach (VehicleSystemEffect effect in onBreak) if (effect.Active(context)) effect.OnEnter(context);
        if (VehicleSystemsMod.StagesLog.DebugEnabled)
        {
            VehicleSystemsMod.StagesLog.Debug(context.Vehicle.LabelShort + " (" + context.Vehicle.ThingID + ") breakdown rolled on role " + role + " (mean " + mtbHours
                + " h, " + healthFraction.ToStringPercent() + " of health): " + (part == null ? "no part to break" : part.props.key));
        }
        if (part == null || message.NullOrEmpty() || context.Vehicle.Faction != Faction.OfPlayer) return;
        Messages.Message(message.Translate(context.Vehicle.LabelShort.Named("VEHICLE"), (part.props.label ?? part.props.key).Named("PART")),
            context.Vehicle, MessageTypeDefOf.NegativeEvent);
    }
}

/// <summary>
/// An explosion at the vehicle, once, through the game's own explosions (a boiler's crown sheet failing), and the parts playing a
/// role broken. Only ever reached through a player's setting that allows it (boiler explosions are off by default; owner, 8 Nov 2026).
/// Away from a map (in a caravan) only the parts break.
/// </summary>
public class Effect_Explosion : VehicleSystemEffect
{
    public float radius = 5.9f;
    public int damage = 60;
    /// <summary>Armour penetration on the scale in use (the game's, or Combat Extended's MPa); NaN takes the damage's share of
    /// it, so the blast is as armour-worthy as the game's or CE's own blasts of the same damage.</summary>
    public float armorPenetration = float.NaN;
    public DamageDef? damageDef;
    public string? breakRole;

    public override void OnEnter(SystemContext context)
    {
        VehiclePawn vehicle = context.Vehicle;
        if (breakRole != null) context.Comp.BreakRole(breakRole, 1f);
        if (!vehicle.Spawned || vehicle.Map == null) return;
        float penetration = float.IsNaN(armorPenetration)
            ? CombatExtendedRules.BlastPenetrationForDamage(damage, OptionalMods.IsActive(OptionalMods.CombatExtended))
            : armorPenetration;
        GenExplosion.DoExplosion(vehicle.Position, vehicle.Map, radius, damageDef ?? DamageDefOf.Bomb, vehicle, damage, penetration);
    }
}

