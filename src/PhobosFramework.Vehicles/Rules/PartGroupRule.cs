using System;
using System.Collections.Generic;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Which of a vehicle's parts fold into one row on the Health tab (Framework 0.114.0; owner, 16 Nov 2026: the list looked busy, above all
/// on a helicopter with thirty parts), game-free. Parts are alike when their names differ only by where they sit: "front left landing
/// wheel" and "rear right landing wheel" are both a landing wheel, "engine 1" and "engine 2" both an engine. The position words come
/// from the language file, so a translation supplies its own; a name made only of position words stands alone. A group is named by the
/// words every member shares, in the first member's order, so "left rear panel" and "right rear panel" make "rear panel". Order follows
/// first appearance. Presentation only: nothing in play reads a group.
/// </summary>
public static class PartGroupRule
{
    public readonly struct Group
    {
        public readonly string Label;
        public readonly IReadOnlyList<int> Members;

        public Group(string label, IReadOnlyList<int> members)
        {
            Label = label;
            Members = members;
        }
    }

    static readonly char[] Spaces = { ' ', '\t' };

    public static List<Group> Groups(IReadOnlyList<string> labels, ICollection<string> positionWords)
    {
        var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string word in positionWords)
        {
            if (!string.IsNullOrWhiteSpace(word)) words.Add(word.Trim());
        }
        var order = new List<string>();
        var members = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < labels.Count; i++)
        {
            string key = Key(labels[i], words);
            // A name with nothing left once the position words go (or no name at all) stands alone.
            if (key.Length == 0) key = "\u0001" + i;
            if (!members.TryGetValue(key, out List<int>? list))
            {
                members[key] = list = new List<int>();
                order.Add(key);
            }
            list.Add(i);
        }
        var groups = new List<Group>(order.Count);
        foreach (string key in order)
        {
            List<int> list = members[key];
            groups.Add(new Group(list.Count == 1 ? labels[list[0]] ?? "" : SharedWords(labels, list), list));
        }
        return groups;
    }

    static bool IsNumber(string token)
    {
        foreach (char c in token)
        {
            if (!char.IsDigit(c) && c != '#') return false;
        }
        return token.Length > 0;
    }

    static string Key(string? label, HashSet<string> words)
    {
        if (string.IsNullOrWhiteSpace(label)) return "";
        var kept = new List<string>();
        foreach (string token in label!.Split(Spaces, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!words.Contains(token) && !IsNumber(token)) kept.Add(token.ToLowerInvariant());
        }
        return string.Join(" ", kept);
    }

    /// <summary>The words of the first member that every member has, in the first member's order, numbers left out.</summary>
    static string SharedWords(IReadOnlyList<string> labels, List<int> members)
    {
        var shared = new List<string>();
        foreach (string token in labels[members[0]].Split(Spaces, StringSplitOptions.RemoveEmptyEntries))
        {
            if (IsNumber(token)) continue;
            bool everywhere = true;
            for (int m = 1; m < members.Count && everywhere; m++)
            {
                everywhere = false;
                foreach (string other in labels[members[m]].Split(Spaces, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (string.Equals(other, token, StringComparison.OrdinalIgnoreCase))
                    {
                        everywhere = true;
                        break;
                    }
                }
            }
            if (everywhere) shared.Add(token);
        }
        return shared.Count == 0 ? labels[members[0]] : string.Join(" ", shared);
    }
}
