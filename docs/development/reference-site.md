# The modders' reference site (23 Nov 2026)

**Status (28 Nov 2026):** built, and **published at [phobos-dthorga.github.io/phobos-rimworld](https://phobos-dthorga.github.io/phobos-rimworld/)** since 23 Nov 2026 (the
owner switched GitHub Pages on); the workflow rebuilds and republishes it on every push that touches its files. Since 24 Nov 2026
the gate builds it too; since 28 Nov 2026 every page's footer says when it was built and from which Framework version, and it
covers every mod (the Radio Framework, Water Tap and the per-mod pages of `docs/modding/` were missing until then).

The owner's choice D3 in the docs rewrite (23 Nov 2026): the Framework's modder reference is generated from the code with
**DocFX** (Microsoft's documentation generator, MIT licence), published on GitHub Pages, instead of written by hand. The audit had
found 232 public modder-facing types in the Framework and its vehicle systems, about half of them named in no guide.

## Read this first (gotchas)

- **Build it with `python scripts/reference-site.py build`** (about 45 s). The site goes to the kit's cache
  (`rimworld-reference-site/site`, on K: on the owner's PC); `--site` and `--work` choose other folders. Nothing is written into
  the repository.
- **Every page's footer says when the site was built** (owner, 28 Nov 2026, after the site looked days old when it was not): the
  time in UTC, the Framework's version (`stamp` in `config/reference-site.json`) and the commit, with "with uncommitted changes"
  on a build from a working tree that has them. It goes into the run's copy of `docfx.json`; the committed one never changes. A
  date written by hand inside a guide is that guide's own status line, not the site's.
- **A comp the mods add in code says so on its page** (the owner's card of 28 Nov 2026,
  [what writing the guides found](guide-audit-decisions.md)): `addedInCode` in `config/reference-site.json` lists each with who gets
  it, and its page shows "Added for you" in place of an example to copy (`never: false` keeps the example, for a comp written by hand
  in the cases its note names). The catalogue marks them, and a test fails if a listed type stops being XML-facing.
- **The gate builds the whole site too** (`verify`, in its own lane beside the C# builds, about a minute; the owner's card of
  24 Nov 2026): into a throwaway folder per run, failing on any DocFX warning or error, so a broken link or cross-reference is
  caught before a commit rather than on GitHub. A machine without DocFX skips the step and says so.
- **`python scripts/reference-site.py check` is a gate step**: it fails when a modder-facing type has no `<summary>`. The 33
  types without one on 23 Nov 2026 are listed in `config/reference-site.json` (`knownGaps`), so only a new gap fails; give one a
  summary and take it off the list.
- **Modders write XML, so every XML-facing type's page shows it as XML**: a Def or `<li Class="...">` example with its fields and
  their defaults, read from the C# text by the kit's declaration scanner (nothing is compiled or loaded). A default worked out in
  code says "set in code".
- **Publishing is switched on by two settings** (the owner's, 23 Nov 2026): Settings, Pages, Source: **GitHub Actions**; and the
  Actions variable **`PAGES_ENABLED`** = `true`. Delete the variable to stop publishing; the build still runs and uploads.

## The pages for modders (28 Nov 2026)

Four pages generated on every build from the code and the changelogs, so none can fall behind; each is in the site's menu and
linked from its front page. Agent choices, owner to revise: what counts as each page's entry, below.

- **Extension points** (68 on 28 Nov 2026): every public interface and public abstract class in the site's projects (config
  `projects`), by project, interfaces before abstract classes, each linked to its API page with its summary's first sentence. Read
  with the kit's declaration scanner. Left out: anything not public all the way out, and a type whose summary says it is ours only
  ("ours only", "for our own use", "not for other mods").
- **What we patch** (260): every Harmony patch in `src/`, read by the kit's Harmony reader (the one behind doc-tables' patch list in
  `building.md`), by project: the method patched, prefix, postfix, transpiler or finalizer, our class linked to its source on
  GitHub, and its comment's first sentence (the class's `<summary>`, else its `//` lines, else the patch method's). A patch whose
  target is found by name shows what its `TargetMethod` looks up, as the kit's target scanner (check-targets' reader) reads it:
  25 of 27 on 28 Nov 2026; the other two say "by name in TargetMethod".
- **Dev actions** (91): every `[DebugAction]` in `src/`, by category (constants followed), then by its label's prefix
  ("Framework:", "Burns:"), with its summary (75 have none on 28 Nov 2026, and say the label speaks for them), where it works
  (`allowedGameStates`, the game's default a game) and how (`actionType`, the DLC it needs; "acts on what is selected" comes from
  the label, as the attribute has no such flag).
- **Changes for modders** (110): every line of `mods/*/CHANGELOG.md` marked for modders, newest first, by day, with its mod,
  version and section, linked to the line. A mark is a bullet opening "For modders", a bullet under a heading naming modders, a
  "For modders:" sentence inside a bullet (shown after the bullet's bold opening), or "(..., for modders)"; "the notes for
  modders" is not one. A merged mod's history is named as that mod, and its "unreleased at the merge" takes the next date below.

The code folders and the changelogs can be set in `config/reference-site.json` (`codeFolders`, default `["src"]`;
`changelogs`, default `mods/*/CHANGELOG.md`); neither is set today.

## An editor schema, a prototype (28 Nov 2026; not published)

`python scripts/xml-schema.py build --out FILE` writes an XML Schema (XSD 1.0) of our XML from the same scan, so VS Code (Red Hat
XML) can offer our Defs, their fields, choice values and summaries as a modder types; `validate` checks XML against it with the JDK's
Xerces (`--full` as Rider checks). On 28 Nov 2026 it gave no false errors on all 376 shipped Defs files and 152 Patches files, and
flagged wrong values. **Its limit**: XSD 1.0 cannot choose a type from `Class="..."`, so a comp's own fields appear only beside an
`xsi:type` naming the same class, and whether the game ignores that attribute is untested. Rider needs a full-checking-free setup
(its Validate action lists XSD 1.0 ambiguity reports), and Garethp's Rider plugin and the rwxml language server, which read C# and
follow `Class`, serve modders better there. **Strict validators refuse it** (found 28 Nov 2026): libxml2 as ambiguous, and
xmlschema (XSD 1.1) because each type is derived from its base by a restriction it calls illegal; Xerces without full checking, as VS
Code runs it, accepts it. It is to be made strictly valid before it is published, and the owner tries it in VS Code first (the
owner's card of 28 Nov 2026, "After I try it").

## How it is built (agent choices, owner to revise)

| Part | How |
| --- | --- |
| Projects | Every project with types a modder names in XML (`config/reference-site.json`): Framework, its vehicle systems, Medical, Factory, Farming, Radio Framework (with its vehicles part), Water Tap, and the four Dubs Bad Hygiene tie-ins. Since 28 Nov 2026 (owner: the Radio was missing) a test fails when such a project is left out. PhobosPatches is left out: it patches other authors' mods and gives nothing to build on. The guides are the three Framework guides (the core, vehicle systems and, since 28 Nov 2026, vehicle features) and every page of `docs/modding/`, with its README as the guides' front page |
| Reading the C# | DocFX's source-file mode: it reads our `.cs` files with the game's reference assemblies and Harmony from the NuGet cache, and carries on past the other mods' types (`allowCompilationErrors`). So GitHub's runners build the same site with no Workshop files and no secrets; other mods' types are named but not linked |
| XML pages | `scripts/reference-site.py pages` writes an XML example and a defaults table for each of the 281 XML-facing types (nine kinds: comps, vehicle comps, extensions, Defs, rules Defs, vehicle system inputs, conditions and effects, hediff comps) and an "XML types" catalogue |
| Workflow | `.github/workflows/reference-site.yml`, its actions pinned to commits; deploy only when `PAGES_ENABLED` is `true` |
| Clean-up | The build folders are listed in `config/cleanup.json` ("Reference site builds") |

## What a next game would need

The script is RimWorld's: its kinds of XML-facing type are RimWorld's (comps, Defs, extensions). A next game in C# would need its
own kinds listed as data and its reference assemblies named; the DocFX project and the `check` would carry over. Moving the
generic part (DocFX config, the summary check) into phobos-modkit is the follow-up once a second game wants a reference site.
