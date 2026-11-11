using System;
using System.Collections.Generic;
using System.Text;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;

namespace Phobos.Framework.Bills;

/// <summary>The player's choice of whose parts a build asks for (a setting of the framework's).</summary>
public enum BillPartsSource
{
    /// <summary>Another mod's part where that mod is installed and the part suits; ours otherwise.</summary>
    OthersWhereInstalled,
    /// <summary>Always our own parts and materials.</summary>
    AlwaysOurs,
}

/// <summary>
/// The parts bills' service (docs/development/parts-bills.md; Framework 0.95.0). At start-up it works out every
/// <see cref="PhobosBillDef"/> once and writes the result to its target's cost. Costs are Def data: nothing here is saved, and
/// removing a bill puts a thing back at the cost in its XML. A fault in one bill leaves that thing at its shell cost.
/// </summary>
[StaticConstructorOnStartup]
public static class PartsBills
{
    static readonly Phobos.Logging.PhobosLog Logger = Logging.Logs.For("D'thorga Framework", "Bills");
    static readonly List<string> NoTags = new();

    /// <summary>How many bills were applied at start-up, and how many failed.</summary>
    public static int Applied { get; private set; }
    public static int Failed { get; private set; }

    public static bool OthersAllowed => FrameworkMod.Settings.billPartsSource == BillPartsSource.OthersWhereInstalled;

    static PartsBills() => ApplyAll();

    /// <summary>Works out every bill again and writes it. Called once at start-up; a dev action may call it after a change of setting.</summary>
    public static void ApplyAll()
    {
        Applied = 0;
        Failed = 0;
        bool others = OthersAllowed;
        foreach (PhobosBillDef bill in DefDatabase<PhobosBillDef>.AllDefsListForReading)
        {
            try
            {
                bill.Apply(others);
                Applied++;
            }
            catch (Exception e)
            {
                Failed++;
                Logger.Error("The parts bill " + bill.defName + " could not be worked out. " + (bill.target?.defName ?? "Its target") + " keeps the cost in its XML.", e);
            }
        }
        // The game remembers a thing's adjusted cost the first time it is asked.
        if (Applied > 0) CostListCalculator.Reset();
    }

    /// <summary>The first role supply that fills this role, or null.</summary>
    public static PhobosRoleSupplyDef? SupplyFor(string role, IReadOnlyCollection<string>? tags = null)
    {
        tags ??= NoTags;
        foreach (PhobosRoleSupplyDef def in DefDatabase<PhobosRoleSupplyDef>.AllDefsListForReading)
        {
            if (def.Fits(role, tags)) return def;
        }
        return null;
    }

    /// <summary>
    /// A shell's cost with these parts filled in. The shell's lines keep their order. A kit of ours that covers several roles for the
    /// whole build is asked for once. <paramref name="rows"/>, when given, receives what each role came to.
    /// </summary>
    public static List<ThingDefCountClass> Resolve(IReadOnlyList<ThingDefCountClass>? shell, IEnumerable<BillPart> parts, double size,
        IReadOnlyCollection<string>? tags, bool othersAllowed, List<BillRow>? rows = null)
    {
        var shellLines = new List<BillLine>();
        if (shell != null)
        {
            foreach (ThingDefCountClass line in shell)
            {
                if (line.thingDef != null) shellLines.Add(new BillLine(line.thingDef.defName, line.count));
            }
        }
        var partLines = new List<BillLine>();
        HashSet<PhobosRoleSupplyDef>? kitsAsked = null;
        foreach (BillPart part in parts)
        {
            PhobosRoleSupplyDef? supply = SupplyFor(part.role, tags);
            if (supply == null) continue;    // a config error names it
            List<BillLine> lines = BuildBillRule.Fill(supply.Offer(), part.count, size, othersAllowed, out BillSource source);
            if (source == BillSource.Ours && supply.coversWholeBuild && supply.item != null)
            {
                kitsAsked ??= new HashSet<PhobosRoleSupplyDef>();
                if (!kitsAsked.Add(supply)) lines.Clear();
            }
            partLines.AddRange(lines);
            rows?.Add(new BillRow { Role = part.role, Count = part.count, Source = source, Supply = supply, Lines = ToCounts(lines) });
        }
        return ToCounts(BuildBillRule.Merge(shellLines, partLines));
    }

    public static List<ThingDefCountClass> Copy(List<ThingDefCountClass>? list)
    {
        var copy = new List<ThingDefCountClass>(list?.Count ?? 0);
        if (list == null) return copy;
        foreach (ThingDefCountClass line in list) copy.Add(new ThingDefCountClass(line.thingDef, line.count));
        return copy;
    }

    static List<ThingDefCountClass> ToCounts(List<BillLine> lines)
    {
        var counts = new List<ThingDefCountClass>(lines.Count);
        foreach (BillLine line in lines)
        {
            ThingDef? def = DefDatabase<ThingDef>.GetNamedSilentFail(line.Thing);
            if (def != null) counts.Add(new ThingDefCountClass(def, line.Count));
        }
        return counts;
    }

    /// <summary>Every bill as it stands: the thing, its whole cost, and each role with what filled it and whose part that is. For the dev action.</summary>
    public static string Report()
    {
        var text = new StringBuilder();
        text.Append(Applied).Append(" parts bill").Append(Applied == 1 ? "" : "s").Append(" applied");
        if (Failed > 0) text.Append(", ").Append(Failed).Append(" failed");
        text.Append("; parts for building: ").Append(OthersAllowed ? "other mods' where installed" : "always our own").Append('.');
        foreach (PhobosBillDef bill in DefDatabase<PhobosBillDef>.AllDefsListForReading)
        {
            text.Append("\n\n").Append(bill.target?.LabelCap.ToString() ?? bill.defName).Append(" (").Append(bill.defName);
            if (!double.IsNaN(bill.Size)) text.Append(", size ").Append(bill.Size.ToString("0.#"));
            text.Append("): ").Append(Describe(bill.target?.costList));
            foreach (BillRow row in bill.rows) text.Append("\n  ").Append(Describe(row));
            bill.AppendToReport(text);
        }
        return text.ToString();
    }

    public static string Describe(BillRow row)
    {
        string whose = row.Source == BillSource.Ours ? "ours" : row.Lines.Count > 0 ? row.Lines[0].thingDef.modContentPack?.Name ?? "another mod" : "another mod";
        return row.Role + " x" + row.Count + ": " + (row.Lines.Count > 0 ? Describe(row.Lines) : "already covered by a kit") + " (" + whose + ")";
    }

    public static string Describe(List<ThingDefCountClass>? cost)
    {
        if (cost == null || cost.Count == 0) return "nothing";
        var text = new StringBuilder();
        foreach (ThingDefCountClass line in cost)
        {
            if (text.Length > 0) text.Append(", ");
            text.Append(line.count).Append(' ').Append(line.thingDef.label);
        }
        return text.ToString();
    }
}
