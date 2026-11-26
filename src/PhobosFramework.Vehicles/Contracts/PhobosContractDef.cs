using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// What a contract asks for: a fluid brought in a tank, things brought in a caravan, hours of a truck's work at the settlement, or one of
/// the two that wait at a place on the world map (Framework 0.311.0): the settlement's broken-down vehicle, towed home or mended where it
/// stands, and one of its people lying hurt, brought home alive.
/// </summary>
public enum ContractKind
{
    DeliverFluid,
    DeliverThings,
    WorkOnSite,
    TowVehicle,
    Casualty,
}

/// <summary>
/// One kind of work a settlement may ask of the player's vehicles (Framework 0.308.0; docs/development/vehicle-contracts.md; EM45 with EM9).
/// A quest (<c>PhobosVehicleContract</c>) offers it only while the player owns a vehicle that can do it; other mods add their own kinds in
/// XML. The vehicle is chosen by what it carries (a comp, or a tank that can hold the fluid), never by its name.
/// </summary>
public class PhobosContractDef : Def
{
    public ContractKind kind = ContractKind.DeliverThings;
    /// <summary>A comp the vehicle must carry (the survey truck's, the drill rig's, the tipper's, the generator set's). Null: any vehicle with
    /// our systems.</summary>
    public Type? vehicleComp;
    /// <summary>DeliverFluid: the fluid asked for, brought in any tank that can hold it.</summary>
    public PhobosTankContentsDef? fluid;
    /// <summary>DeliverThings: any thing of this category counts (stone chunks of any rock).</summary>
    public ThingCategoryDef? thingCategory;
    /// <summary>DeliverThings: one of these is asked for, chosen when the contract is offered (a named vehicle part).</summary>
    public List<ThingDef> things = new();
    /// <summary>Litres, things or hours of work asked for, before the player's largest fitting tank caps it.</summary>
    public FloatRange amountRange = new(1f, 1f);
    /// <summary>Amounts are rounded down to this, so requests read as people make them.</summary>
    public float step = 1f;
    /// <summary>Silver a unit (a litre, a thing, an hour) is worth, which the reward is valued from. Zero: the thing's own market value.</summary>
    public float valuePerUnit;
    /// <summary>WorkOnSite: the crew skill that sets the pace (none: the plain pace, as for a generator left running).</summary>
    public SkillDef? skill;
    /// <summary>WorkOnSite: fuel the working vehicle burns an hour, from its own tank.</summary>
    public float fuelPerHour;
    /// <summary>WorkOnSite: whether the work goes on while the caravan rests for the night (a generator does; a survey crew sleeps).</summary>
    public bool worksAtNight;
    /// <summary>How likely this kind is, among the kinds the player can do.</summary>
    public float selectionWeight = 1f;
    /// <summary>Whether it is offered only to a player who owns a vehicle that fits. False: to anyone (the casualty, brought home by any
    /// caravan: the owner's card of 26 Nov 2026).</summary>
    public bool needsVehicle = true;
    /// <summary>The asking faction must be at least this advanced (a settlement with no trucks has none to break down).</summary>
    public TechLevel minTechLevel = TechLevel.Undefined;

    /// <summary>Whether it waits at a place on the world map, made by its own quest, rather than at the settlement.</summary>
    public bool OnSite => kind is ContractKind.TowVehicle or ContractKind.Casualty;
    /// <summary>What is asked, for the quest's text and the settlement's line: {AMOUNT} and {THING}.</summary>
    [MustTranslate] public string request = "";
    /// <summary>How the player does it, for the quest's text.</summary>
    [MustTranslate] public string howTo = "";
    /// <summary>WorkOnSite: the work, as the settlement's line says it is going on ("surveying the ground").</summary>
    [MustTranslate] public string working = "";

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (request.NullOrEmpty()) yield return "request is empty";
        if (amountRange.min <= 0f || amountRange.max < amountRange.min) yield return "amountRange must be above zero";
        if (vehicleComp != null && !typeof(ThingComp).IsAssignableFrom(vehicleComp)) yield return "vehicleComp is not a comp: " + vehicleComp;
        switch (kind)
        {
            case ContractKind.DeliverFluid when fluid == null:
                yield return "a fluid delivery names no fluid";
                break;
            case ContractKind.DeliverThings when thingCategory == null && things.Count == 0:
                yield return "a delivery of things names neither a category nor things";
                break;
            case ContractKind.WorkOnSite when valuePerUnit <= 0f:
                yield return "work on site needs a valuePerUnit (silver an hour)";
                break;
            case ContractKind.WorkOnSite when working.NullOrEmpty():
                yield return "work on site needs its working text";
                break;
            case ContractKind.TowVehicle or ContractKind.Casualty when valuePerUnit <= 0f:
                yield return "a contract at a place on the world map needs a valuePerUnit (its worth, in silver)";
                break;
        }
    }
}
