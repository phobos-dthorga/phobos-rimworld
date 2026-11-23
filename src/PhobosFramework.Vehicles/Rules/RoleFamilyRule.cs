using System.Collections.Generic;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Numbered roles roll up into their base role (Framework 0.256.0; the owner's report of 23 Nov 2026 that the Systems and Health tabs
/// disagreed on many aircraft, and the owner's ask: "genericize how these talk with each other, so any future updates/additions can just be
/// picked up"). A vehicle that binds Engine1, Engine2 and Engine3 has three engines, so a system that names "Engine" covers all three, rather
/// than whatever single part a generic finder took for "the engine" (sometimes a cover). Any role ending in digits is a member of the role
/// its digits are taken from, so a new numbered role needs nothing more. Game-free.
/// </summary>
public static class RoleFamilyRule
{
    /// <summary>The base role of a numbered role ("Engine12" gives "Engine"), or null for a role with no number or nothing before it.</summary>
    public static string? BaseOf(string role)
    {
        int end = role.Length;
        while (end > 0 && char.IsDigit(role[end - 1])) end--;
        return end == role.Length || end == 0 ? null : role.Substring(0, end);
    }

    /// <summary>The number of a numbered role ("Engine12" gives 12), or 0 without one.</summary>
    public static int NumberOf(string role)
    {
        string? baseRole = BaseOf(role);
        return baseRole != null && int.TryParse(role.Substring(baseRole.Length), out int number) ? number : 0;
    }

    /// <summary>Each base role with members among <paramref name="roles"/>, the members in number order. A base role with a single member
    /// is a family too: one engine bound as Engine1 is still the engine.</summary>
    public static Dictionary<string, List<string>> Families(IEnumerable<string> roles)
    {
        var families = new Dictionary<string, List<string>>();
        foreach (string role in roles)
        {
            if (BaseOf(role) is not { } baseRole) continue;
            if (!families.TryGetValue(baseRole, out List<string>? members)) families[baseRole] = members = new List<string>();
            members.Add(role);
        }
        foreach (List<string> members in families.Values) members.Sort((a, b) => NumberOf(a).CompareTo(NumberOf(b)));
        return families;
    }
}
