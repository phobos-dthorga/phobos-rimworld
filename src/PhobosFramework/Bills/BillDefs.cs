using System.Collections.Generic;
using System.Xml;
using Phobos.Framework.Rules;
using Verse;

namespace Phobos.Framework.Bills;

/// <summary>
/// Another mod's part accepted for a role, only for the built things it suits (a light engine does not go in a tank). Listed with
/// MayRequire, so it is here only while its mod is active. The size is whatever the bill measures by: kilograms for a vehicle.
/// </summary>
public class RoleSubstitute
{
    public ThingDef? thing;
    /// <summary>How many of this stand for one of the role.</summary>
    public int count = 1;
    [LoadAlias("minKilograms")]
    public float minSize = float.NaN;
    [LoadAlias("maxKilograms")]
    public float maxSize = float.NaN;

    public bool Suits(double size) => thing != null && BuildBillRule.Suits(size, minSize, maxSize);
}

/// <summary>
/// What fills a role (docs/development/parts-bills.md; Framework 0.95.0). A role is a plain word a bill asks for: Engine, FuelTank,
/// Pump, Vessel. Ours always exists, as an item or as materials; another mod's parts are listed as substitutes, each only where it
/// makes sense. The first Def that fits a role wins. Any mod adds roles and substitutes in XML; no mod is named in C#.
/// </summary>
public class PhobosRoleSupplyDef : Def
{
    public List<string> roles = new();
    /// <summary>Our own part for the role.</summary>
    public ThingDef? item;
    public int itemCount = 1;
    /// <summary>Ours as materials, where there is no item of ours. For a repair, the full cost of which the player's share is charged.</summary>
    public List<ThingDefCountClass> materials = new();
    /// <summary>Our item is a kit that covers these roles for a whole build: one is asked for, however many of them the bill lists.</summary>
    public bool coversWholeBuild;
    public List<RoleSubstitute> substitutes = new();

    /// <summary>Whether this Def fills the role on a built thing with these tags. A subclass narrows it (a turbine's engine).</summary>
    public virtual bool Fits(string role, IReadOnlyCollection<string> tags) => roles.Contains(role);

    /// <summary>This Def as the game-free rule reads it.</summary>
    public BillOffer Offer()
    {
        var offer = new BillOffer { OwnItem = item?.defName, OwnItemCount = itemCount, OwnCoversWholeBuild = coversWholeBuild };
        if (materials.Count > 0)
        {
            var own = new List<BillLine>(materials.Count);
            foreach (ThingDefCountClass material in materials)
            {
                if (material.thingDef != null) own.Add(new BillLine(material.thingDef.defName, material.count));
            }
            offer.OwnMaterials = own;
        }
        if (substitutes.Count > 0)
        {
            var others = new List<BillSubstitute>(substitutes.Count);
            foreach (RoleSubstitute substitute in substitutes)
            {
                if (substitute.thing != null) others.Add(new BillSubstitute(substitute.thing.defName, substitute.count, substitute.minSize, substitute.maxSize));
            }
            offer.Substitutes = others;
        }
        return offer;
    }
}

/// <summary>One line of a bill: a role and how many. Written as the game writes a cost: &lt;Engine&gt;1&lt;/Engine&gt;.</summary>
public class BillPart
{
    public string role = "";
    public int count = 1;

    public BillPart()
    {
    }

    public BillPart(string role, int count)
    {
        this.role = role;
        this.count = count;
    }

    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        role = xmlRoot.Name;
        count = xmlRoot.FirstChild?.Value is { } text ? ParseHelper.FromString<int>(text) : 1;
    }
}

/// <summary>What one role of a bill came to, for the report.</summary>
public sealed class BillRow
{
    public string Role = "";
    public int Count;
    public BillSource Source;
    public PhobosRoleSupplyDef? Supply;
    public List<ThingDefCountClass> Lines = new();
}

/// <summary>
/// What a built thing is made of, by role (docs/development/parts-bills.md; Framework 0.95.0; owner, 11 Nov 2026). The target is
/// anything the player builds: a placed building, or a vehicle's blueprint. Its own cost list in XML is its shell (steel and the
/// like); at start-up each role here is filled from the role supplies, by another mod's part where one is installed and suits the
/// size, by ours otherwise, and the result becomes the target's cost. Nothing is saved, and a target with no bill is never touched.
/// </summary>
public class PhobosBillDef : Def
{
    public ThingDef? target;
    /// <summary>What the substitutes' size windows are read against: kilograms for a vehicle. NaN: only parts with no window suit.</summary>
    public float size = float.NaN;
    /// <summary>Words a role supply may ask for or refuse ("Turbine").</summary>
    public List<string> tags = new();
    public List<BillPart> parts = new();

    /// <summary>The target's cost as written in XML, kept so a bill can be worked out again.</summary>
    [Unsaved]
    public List<ThingDefCountClass>? shell;
    /// <summary>What each role came to when the bill was last applied.</summary>
    [Unsaved]
    public List<BillRow> rows = new();

    public virtual double Size => size;

    /// <summary>Works the bill out and writes it to the target. A subclass with more to fill (a vehicle's upgrades) adds to this.</summary>
    public virtual void Apply(bool othersAllowed)
    {
        if (target == null) return;
        shell ??= PartsBills.Copy(target.costList);
        rows.Clear();
        target.costList = PartsBills.Resolve(shell, parts, Size, tags, othersAllowed, rows);
    }

    /// <summary>More for the dev action's report than the build's roles: a subclass adds what else it filled.</summary>
    public virtual void AppendToReport(System.Text.StringBuilder text)
    {
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (target == null) yield return "names no target";
        foreach (string error in PartErrors(parts)) yield return error;
    }

    /// <summary>What is wrong with a list of parts: a role nothing supplies, or none of a role.</summary>
    protected IEnumerable<string> PartErrors(List<BillPart> list, string where = "")
    {
        foreach (BillPart part in list)
        {
            if (part.count <= 0) yield return where + "asks for " + part.count + " of the role " + part.role;
            if (PartsBills.SupplyFor(part.role, tags) == null) yield return where + "asks for the role " + part.role + ", which no PhobosRoleSupplyDef fills";
        }
    }
}
