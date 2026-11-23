# The modders' reference site (23 Nov 2026)

**Status (24 Nov 2026):** built, and **published at [phobos-dthorga.github.io/phobos-rimworld](https://phobos-dthorga.github.io/phobos-rimworld/)** since 23 Nov 2026 (the
owner switched GitHub Pages on); the workflow rebuilds and republishes it on every push that touches its files. Since 24 Nov 2026
the gate builds it too.

The owner's choice D3 in the docs rewrite (23 Nov 2026): the Framework's modder reference is generated from the code with
**DocFX** (Microsoft's documentation generator, MIT licence), published on GitHub Pages, instead of written by hand. The audit had
found 232 public modder-facing types in the Framework and its vehicle systems, about half of them named in no guide.

## Read this first (gotchas)

- **Build it with `python scripts/reference-site.py build`** (about 45 s). The site goes to the kit's cache
  (`rimworld-reference-site/site`, on K: on the owner's PC); `--site` and `--work` choose other folders. Nothing is written into
  the repository.
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

## How it is built (agent choices, owner to revise)

| Part | How |
| --- | --- |
| Projects | Framework, its vehicle systems, Medical, Factory and Farming (`config/reference-site.json`), with the two modding guides as articles |
| Reading the C# | DocFX's source-file mode: it reads our `.cs` files with the game's reference assemblies and Harmony from the NuGet cache, and carries on past the other mods' types (`allowCompilationErrors`). So GitHub's runners build the same site with no Workshop files and no secrets; other mods' types are named but not linked |
| XML pages | `scripts/reference-site.py pages` writes an XML example and a defaults table for each of the 281 XML-facing types (nine kinds: comps, vehicle comps, extensions, Defs, rules Defs, vehicle system inputs, conditions and effects, hediff comps) and an "XML types" catalogue |
| Workflow | `.github/workflows/reference-site.yml`, its actions pinned to commits; deploy only when `PAGES_ENABLED` is `true` |
| Clean-up | The build folders are listed in `config/cleanup.json` ("Reference site builds") |

## What a next game would need

The script is RimWorld's: its kinds of XML-facing type are RimWorld's (comps, Defs, extensions). A next game in C# would need its
own kinds listed as data and its reference assemblies named; the DocFX project and the `check` would carry over. Moving the
generic part (DocFX config, the summary check) into phobos-modkit is the follow-up once a second game wants a reference site.
