using System;

namespace Phobos.Framework.Rules;

/// <summary>One rare tick of a generator feeding a power net.</summary>
public readonly record struct GeneratorStep(float OutputWatts, float Load, float FuelUsed, float Buffer);

/// <summary>
/// What a generator run by a governor does (docs/development/vehicle-generator-set.md). The output is what it can offer; fuel follows the load, from idle to full,
/// which is kinder than the game's own generators, whose burn is flat (a labelled simplification). A battery buffer takes the surplus and carries the net when the engine cannot.
/// </summary>
public static class GeneratorRule
{
    /// <param name="offeredWatts">What the generator can give now; 0 when it cannot run.</param>
    /// <param name="needWatts">What the net wants from it, after any other generator on the net.</param>
    /// <param name="batteriesHaveRoom">Batteries on the net would take all it can make, so it runs flat out.</param>
    public static GeneratorStep Step(float offeredWatts, float needWatts, bool batteriesHaveRoom, float buffer, float bufferCapacity, float days, float idleFuelPerDay, float fullFuelPerDay)
    {
        float need = Math.Max(0f, needWatts);
        float held = Math.Clamp(buffer, 0f, Math.Max(0f, bufferCapacity));
        if (offeredWatts <= 0f)
        {
            float carried = days > 0f ? Math.Min(need, held / days) : 0f;
            return new GeneratorStep(carried, 0f, 0f, held - carried * days);
        }
        float load = batteriesHaveRoom ? 1f : Math.Min(1f, need / offeredWatts);
        float surplus = Math.Max(0f, offeredWatts - need);
        float fuel = (idleFuelPerDay + (fullFuelPerDay - idleFuelPerDay) * load) * days;
        return new GeneratorStep(offeredWatts, load, fuel, Math.Min(bufferCapacity, held + surplus * days));
    }

    /// <summary>
    /// A rare tick with a service lead plugged in (Framework 0.101.0; docs/development/vehicle-upgrades-round-two.md): the lead takes its watts
    /// off the top of what the generator can give, never more than that; the camp's net gets the rest through <see cref="Step"/>; and the
    /// engine's load and fuel follow the lead's draw plus what the camp used. With no engine running the lead gets nothing (the buffer is the
    /// camp's).
    /// </summary>
    public static LeadStep StepWithLead(float offeredWatts, float leadWatts, float needWatts, bool batteriesHaveRoom, float buffer, float bufferCapacity, float days, float idleFuelPerDay, float fullFuelPerDay)
    {
        float lead = offeredWatts > 0f ? Math.Clamp(leadWatts, 0f, offeredWatts) : 0f;
        if (lead <= 0f) return new LeadStep(Step(offeredWatts, needWatts, batteriesHaveRoom, buffer, bufferCapacity, days, idleFuelPerDay, fullFuelPerDay), 0f);
        float campOffered = offeredWatts - lead;
        GeneratorStep camp = Step(campOffered, needWatts, batteriesHaveRoom, buffer, bufferCapacity, days, idleFuelPerDay, fullFuelPerDay);
        float campUsed = campOffered > 0f ? camp.Load * campOffered : 0f;
        float load = Math.Min(1f, (lead + campUsed) / offeredWatts);
        float fuel = (idleFuelPerDay + (fullFuelPerDay - idleFuelPerDay) * load) * days;
        return new LeadStep(camp with { Load = load, FuelUsed = fuel }, lead);
    }
}

/// <summary>A rare tick with a service lead: the camp's side (output to its net, the whole engine's load and fuel, the buffer) and the watts the lead got.</summary>
public readonly record struct LeadStep(GeneratorStep Camp, float LeadWatts);
