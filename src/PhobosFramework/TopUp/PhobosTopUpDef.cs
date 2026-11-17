using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Phobos.Framework.TopUp;

/// <summary>
/// Names a kind of machine that holds a material and wants more of it, so haulers keep it topped up by themselves (and, since 0.135.0,
/// a mechanitor the colony's mechanoids that carry one) (Framework 0.134.0;
/// docs/development/top-up-design.md; owner, 18 Nov 2026: colonists never refilled the drone bays' steel, "genericize this where
/// possible"). In XML only: the comp that holds the store, by its class's full name, and where on that comp to read how much more it
/// wants and of what. Every building whose Def carries that comp is then topped up; nothing is patched and no other mod's assembly is
/// referenced. A Def for another mod's comp belongs in that mod's IfModActive folder, so the names are read only while the mod is there.
///
/// The member paths are read by name at start-up and checked then: a wrong name is reported once, with the Def's name, and that Def
/// stands down. They are names in another author's code, so an update of theirs can break them; the report says so when it does.
/// </summary>
public class PhobosTopUpDef : Def
{
    /// <summary>The comp's class, by its full name ("Fortified.CompMechPlatform"); a subclass counts.</summary>
    public string compClass = "";
    /// <summary>On the comp: how much more it wants now, a whole number (a property or field; dots walk into members, "Props.count").</summary>
    public string wantedMember = "";
    /// <summary>On the comp: the material it wants, a ThingDef ("Props.fixedIngredient").</summary>
    public string ingredientMember = "";
    /// <summary>The least it must want before a hauler walks for it (authored per machine). An order by hand ignores it.</summary>
    public int leastLoad = 1;
    /// <summary>On the comp: the least load, read from the machine itself where it has a natural one (a drone's cost); wins over
    /// <see cref="leastLoad"/> when given.</summary>
    public string? leastLoadMember;
    /// <summary>On the comp: how much it can hold, a whole number ("Props.maxIngredientCount"). With <see cref="heldMember"/>, lets the
    /// player's fill level ask for more than the machine's own (Framework 0.136.0); without them the machine's own level is used.</summary>
    public string? capacityMember;
    /// <summary>On the comp: how much it holds now, a whole number ("IngredientCount").</summary>
    public string? heldMember;
    /// <summary>Buildings with the comp are topped up by haulers.</summary>
    public bool buildings = true;
    /// <summary>Mechanoids with the comp are topped up by a mechanitor, as the game's mech repair is (Framework 0.135.0).</summary>
    public bool mechanoids = true;

    [Unsaved] Type? type;
    [Unsaved] MemberPath? wanted;
    [Unsaved] MemberPath? ingredient;
    [Unsaved] MemberPath? least;
    [Unsaved] MemberPath? capacity;
    [Unsaved] MemberPath? held;
    [Unsaved] bool resolved;

    /// <summary>Why this Def stands down, in words, or null while it works.</summary>
    [Unsaved] public string? problem;

    /// <summary>The comp's type once found, or null.</summary>
    public Type? CompType
    {
        get
        {
            Resolve();
            return problem == null ? type : null;
        }
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (compClass.NullOrEmpty()) yield return "compClass is empty";
        if (wantedMember.NullOrEmpty()) yield return "wantedMember is empty";
        if (ingredientMember.NullOrEmpty()) yield return "ingredientMember is empty";
        if (leastLoad < 1) yield return "leastLoad must be 1 or more";
        if (capacityMember.NullOrEmpty() != heldMember.NullOrEmpty()) yield return "give capacityMember and heldMember together, or neither";
    }

    void Resolve()
    {
        if (resolved) return;
        resolved = true;
        type = OptionalMods.TypeNamed(compClass);
        if (type == null) { problem = "no class named " + compClass; return; }
        if (!typeof(ThingComp).IsAssignableFrom(type)) { problem = compClass + " is not a comp"; return; }
        // The job puts the material straight into the comp's own store (Framework 0.135.0), so the comp must keep one.
        if (!typeof(IThingHolder).IsAssignableFrom(type)) { problem = compClass + " keeps no store of its own (it is not an IThingHolder)"; return; }
        wanted = MemberPath.On(type, wantedMember, out problem);
        if (wanted == null) return;
        if (wanted.ValueType != typeof(int)) { problem = wantedMember + " is not a whole number"; return; }
        ingredient = MemberPath.On(type, ingredientMember, out problem);
        if (ingredient == null) return;
        if (!typeof(ThingDef).IsAssignableFrom(ingredient.ValueType)) { problem = ingredientMember + " is not a ThingDef"; return; }
        if (!leastLoadMember.NullOrEmpty())
        {
            least = WholeNumber(leastLoadMember!);
            if (problem != null) return;
        }
        if (capacityMember.NullOrEmpty() || heldMember.NullOrEmpty()) return;
        capacity = WholeNumber(capacityMember!);
        if (problem != null) return;
        held = WholeNumber(heldMember!);
    }

    MemberPath? WholeNumber(string path)
    {
        MemberPath? found = MemberPath.On(type!, path, out problem);
        if (found != null && found.ValueType != typeof(int)) problem = path + " is not a whole number";
        return problem == null ? found : null;
    }

    /// <summary>What a comp of this kind wants now; null when it wants nothing, names no material, or could not be read.</summary>
    public TopUpWant? Read(ThingComp comp)
    {
        Resolve();
        if (problem != null) return null;
        try
        {
            int ownWanted = wanted!.Read(comp) is int own ? own : 0;
            int cap = capacity?.Read(comp) is int c ? c : 0;
            int holds = held?.Read(comp) is int h ? h : 0;
            int amount = Rules.TopUpRule.Wanted(ownWanted, holds, cap, FrameworkMod.Settings.topUpLevel);
            if (amount <= 0) return null;
            if (ingredient!.Read(comp) is not ThingDef material) return null;
            int floor = least?.Read(comp) is int perLoad && perLoad > 0 ? perLoad : leastLoad;
            return new TopUpWant(material, amount, floor);
        }
        catch (Exception error)
        {
            // Another author's code threw while being read: stand down for the session instead of asking again at every look.
            problem = "reading " + compClass + " failed: " + error.GetBaseException().Message;
            TopUps.ReportProblem(this);
            return null;
        }
    }
}

/// <summary>What one machine wants: the material, how much more, and the least worth a walk.</summary>
public readonly struct TopUpWant
{
    public readonly ThingDef Material;
    public readonly int Amount;
    public readonly int LeastLoad;

    public TopUpWant(ThingDef material, int amount, int leastLoad)
    {
        Material = material;
        Amount = amount;
        LeastLoad = leastLoad;
    }
}

/// <summary>A dotted path of properties and fields on a type, found once and read many times.</summary>
sealed class MemberPath
{
    const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

    readonly List<MemberInfo> steps = new();

    public Type ValueType { get; private set; } = typeof(object);

    /// <summary>The path on <paramref name="owner"/>, or null with <paramref name="problem"/> saying which name was not found.</summary>
    public static MemberPath? On(Type owner, string path, out string? problem)
    {
        problem = null;
        var found = new MemberPath();
        Type at = owner;
        foreach (string name in path.Split('.'))
        {
            MemberInfo? member = Find(at, name);
            if (member == null)
            {
                problem = at.FullName + " has no property or field named " + name;
                return null;
            }
            found.steps.Add(member);
            at = member is PropertyInfo property ? property.PropertyType : ((FieldInfo)member).FieldType;
        }
        found.ValueType = at;
        return found;
    }

    static MemberInfo? Find(Type type, string name)
    {
        // Private members are not inherited through reflection: walk up the base types.
        for (Type? at = type; at != null; at = at.BaseType)
        {
            PropertyInfo? property = at.GetProperty(name, Members);
            if (property != null && property.GetIndexParameters().Length == 0 && property.CanRead) return property;
            FieldInfo? field = at.GetField(name, Members);
            if (field != null) return field;
        }
        return null;
    }

    public object? Read(object from)
    {
        object? value = from;
        foreach (MemberInfo step in steps)
        {
            if (value == null) return null;
            value = step is PropertyInfo property ? property.GetValue(value) : ((FieldInfo)step).GetValue(value);
        }
        return value;
    }
}
