using Phobos.Framework;
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
    public virtual float StatFactor(VehicleStatDef stat) => 1f;
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

    public override void OnEnter(SystemContext context)
    {
        if (amount > 0f) context.Comp.WearRole(role, amount);
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
    public override string? DraftRefusal(SystemContext context) => reason.Translate(context.Vehicle.LabelShort.Named("VEHICLE")).Resolve();
}

/// <summary>The vehicle can be started but not driven; the reason is a translation key with {VEHICLE}.</summary>
public class Effect_RefuseMove : VehicleSystemEffect
{
    public string reason = "";
    public override string? MoveRefusal(SystemContext context) => reason.Translate(context.Vehicle.LabelShort.Named("VEHICLE")).Resolve();
}

/// <summary>Multiplies one of Vehicle Framework's stats (move speed, cargo) while the stage lasts.</summary>
public class Effect_StatFactor : VehicleSystemEffect
{
    public VehicleStatDef? stat;
    public float factor = 1f;
    public override float StatFactor(VehicleStatDef asked) => asked == stat ? factor : 1f;
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

/// <summary>Throws flecks from the vehicle (steam, smoke), so its state shows on the map.</summary>
public class Effect_Fleck : VehicleSystemEffect
{
    public FleckDef? fleck;
    public float perHour = 20f;
    public float scale = 1f;
    public override bool Consequential => false;

    public override void Apply(SystemContext context, float hours)
    {
        VehiclePawn vehicle = context.Vehicle;
        if (fleck == null || !vehicle.Spawned || vehicle.Map != Find.CurrentMap) return;
        float expected = perHour * hours;
        int count = Mathf.FloorToInt(expected) + (Rand.Chance(expected - Mathf.Floor(expected)) ? 1 : 0);
        for (int i = 0; i < count; i++)
        {
            // The cell's centre, not the vehicle's DrawPos: Vehicle Framework's 1.6 build names Unity's types from another version.
            Vector3 at = vehicle.Position.ToVector3Shifted() + new Vector3(Rand.Range(-0.4f, 0.4f), 0f, Rand.Range(-0.4f, 0.4f));
            FleckMaker.Static(at, vehicle.Map, fleck, scale);
        }
    }
}

/// <summary>A message when the stage starts, at most once in the rules Def's cooldown. Key arguments: {VEHICLE}.</summary>
public class Effect_Message : VehicleSystemEffect
{
    public string message = "";
    public MessageTypeDef? type;
    public override bool Consequential => false;

    public override void OnEnter(SystemContext context)
    {
        if (context.Vehicle.Faction != Faction.OfPlayer) return;
        float cooldown = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>().messageCooldownHours;
        if (!context.Comp.MayNotify(context.StageKey, cooldown)) return;
        Messages.Message(message.Translate(context.Vehicle.LabelShort.Named("VEHICLE")), context.Vehicle, type ?? MessageTypeDefOf.NegativeEvent);
    }
}

/// <summary>A letter when the stage starts, for the rare and costly (a seized engine). Key arguments: {VEHICLE}.</summary>
public class Effect_Letter : VehicleSystemEffect
{
    public string label = "";
    public string text = "";
    public LetterDef? letter;

    public override void OnEnter(SystemContext context)
    {
        if (context.Vehicle.Faction != Faction.OfPlayer) return;
        float cooldown = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>().letterCooldownHours;
        if (!context.Comp.MayNotify(context.StageKey + ":letter", cooldown)) return;
        NamedArgument vehicle = context.Vehicle.LabelShort.Named("VEHICLE");
        Find.LetterStack.ReceiveLetter(label.Translate(vehicle), text.Translate(vehicle), letter ?? LetterDefOf.NegativeEvent, context.Vehicle);
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

    public override void Apply(SystemContext context, float hours)
    {
        CompFueledTravel? fuel = context.Vehicle.CompFueledTravel;
        if (fuel == null || fuel.EmptyTank) return;
        float rate = perHourParameter != null ? context.Parameter(perHourParameter) : perHour;
        if (ofCapacity) rate *= fuel.FuelCapacity;
        fuel.ConsumeFuel(Mathf.Min(rate * hours, fuel.Fuel));
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
        if (fuel.Fuel > cap + 0.01f) fuel.ConsumeFuel(fuel.Fuel - cap);
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
        if (condition != null && severity > 0f) context.Comp.AddCondition(role, condition, severity, context.State.Def.defName);
    }

    public override void Apply(SystemContext context, float hours)
    {
        if (condition != null && perHour > 0f) context.Comp.AddCondition(role, condition, perHour * hours, context.State.Def.defName);
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

    public override void Apply(SystemContext context, float hours)
    {
        if (!Rand.Chance((float)FailureRule.ChanceIn(hours, mtbHours))) return;
        VehicleComponent? part = context.Comp.BreakRole(role, healthFraction);
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

