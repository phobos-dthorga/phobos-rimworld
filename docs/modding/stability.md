# Stability: what you can rely on

Which names in D'thorga Framework (also spelt dthorga) and the other D'thorga mods your mod can build on without fear of them
changing, which may still change before the first public release, and how you are told when something does. This page states only
what the project's rules and code show, and the owner's answers of 28 Nov 2026 on setting names, renames before release and how
long obsolete names stay.

## Read this first (gotchas)

- **Nothing is released yet.** Every guide says the schema or names **may still change before the first public release**, and that
  the guide will say so when they do. Until then, read the changelogs' **"For modders"** lines before you update.
- **Identities are stable; words are not.** A `defName`, a saved key, a class or namespace name and a `packageId` are kept once saves
  can hold them. Labels, descriptions and every other word a player reads may change at any time. **Never match on a label or on
  translated text.**
- **Old XML keeps loading.** When we replace a field, a condition or a class, the old one is kept (often doing the same as the new),
  so your files still load; the guides mark it and say what to use instead.
- **Obsolete C# members are kept, not removed.** They are marked `[Obsolete]` with the member to use instead, so your build warns
  you rather than breaks.
- **Nothing here is tested in game** by another author's mod. The guides and the example are not yet tested in game.

## Names that are stable, and why

**Saves hold them.** A save written today names Defs, classes and keys, and it must load after an update. So the project's rules
keep each of these once a save can hold it. **Renaming is presentation only: labels change, identities don't** (`src/AGENTS.md`,
"Identifiers and branding").

| Name | Why it stays | The rule |
| --- | --- | --- |
| **`packageId`** (`PhobosDthorga.Framework` and the others) | Load orders, your `modDependencies` and Harmony hold it; the Harmony ID is the `packageId` | "Never change either once a mod is published" (`mods/AGENTS.md`, "Identifiers and branding") |
| **`defName`s** | Saves and your XML name them | Prefixed `Phobos` and kept stable once saves can hold them (`src/AGENTS.md`) |
| **Saved (Scribe) keys** | They are how a save is read back | Stable, with older saves loaded through conversions that are safe to run twice (`src/AGENTS.md`, "Saves") |
| **C# namespaces and class names** | Saves name a saved object's class; your XML names comps and Defs by full class name (`Class="Phobos.VehicleSystems.Effect_Message"`) | Prefixed and kept stable (`src/AGENTS.md`). When mods are merged, "a merge keeps every namespace, class name, defName, language key and saved key" |
| **Translation keys** | Your translations and our language files name them | Prefixed `Phobos` and kept stable (`src/AGENTS.md`); "never translate defNames, saved keys or settings keys" |
| **Display names** | Not stable | When an older Phobos-named mod takes its D'thorga name, "package IDs, internal identifiers, repository names and anything a save can hold stay as they are" (`external/phobos-modkit/rules/brand.md`) |

**A class can even move without breaking saves.** Since Framework 0.309.0 a mod can register namespaces as one class family, so a
saved object loads as the copy of its class its own mod holds, even in a save written with the wrong one (the Framework's changelog,
0.309.0).

**Settings keys.** The rules say settings keys are never translated, and when mods merge, the player's settings are carried over once
(`SettingsImport`). XML that names a setting, such as `When_Setting`'s `setting`, is checked at load: an unknown name is a config
error naming your Def ([the list of names](../vehicle-systems-modding.md#conditions)). The owner is told whenever a setting is added,
removed or given a new default (root `AGENTS.md`). **A setting's name, as XML reads it, is stable like a saved key** (the owner,
28 Nov 2026): never renamed, and a setting we retire keeps answering "off" rather than becoming a config error in your mod.

## What may still change before the first public release

Each guide says so in its own words:

| Guide | Its warning |
| --- | --- |
| [D'thorga Framework for modders](../framework-modding.md) | "Names may still change before the first public release; this page will say so when they do." |
| [Vehicle systems for modders](../vehicle-systems-modding.md) | "**Not yet tested in game**: the schema may change before the first public release, and this page will say so when it does." |
| [Vehicle features for modders](../vehicle-features-modding.md) | "**Not yet tested in game** as another modder's vehicle would use it, and names may still change before the first public release." |

In practice that means:

- **A field's default or meaning may change.** Changes of meaning are written in the changelog; give a field explicitly when your mod
  depends on its value.
- **New checks may refuse what loaded before.** Framework 0.371.0, for one, turned several silent XML mistakes into config errors that
  name the Def. Your XML was already wrong; now you are told.
- **Appended vehicle parts may come and go between versions.** The vehicle systems drop saved parts no loadout appends any more, so
  "a design may lose or gain appended parts between versions" ([Read this first](../vehicle-systems-modding.md#read-this-first-gotchas)).
  Your own parts stay safe if you append them last and never reorder them.
- **Before the first public release, a `defName`, class or saved key may still be renamed, but only with a conversion that keeps
  older saves loading and a "For modders" changelog line** (the owner, 28 Nov 2026). From release on, they are frozen.

## How you are told

- **The changelogs' "For modders" lines.** Each mod's `CHANGELOG.md` (for example
  [D'thorga Framework's](../../mods/PhobosFramework/CHANGELOG.md)) marks a change that matters to other mods' authors with "For
  modders". An entry that affects saves says so under **Save compatibility**.
- **The reference site's Changes for modders page**, a list of those lines gathered from every changelog, on
  <https://phobos-dthorga.github.io/phobos-rimworld/>.
- **The guides themselves.** Each says when a name changes, and marks what is kept only for older XML.
- **Config errors at load.** A field or class we no longer accept, or a value out of range, is named in the log with your Def.

## The compatibility promises the code keeps

Each is a promise the code and its changelog entry show, not a general policy:

| What | Kept how | Since |
| --- | --- | --- |
| **The kits' load-name constants** (the route kit, the uncrewed-flight kit, the mech link, and the Radio Framework's aviation, marine, dual-band, remote control and repeater radios) | Kept "so other authors' C# still compiles" (the code's own comment), marked `[Obsolete]`, each naming its replacement: `Feeds.Works(vehicle, "<ability>")` | Framework 0.371.0 ("the kits' load-name constants are kept but marked obsolete") |
| **`When_Granted`** | Kept, the same test as `When_FittingGrants`; use `When_FittingGrants` in new XML | Framework 0.371.0 ("kept so older XML loads") |
| **`late` on an appended part** | Still read; superseded by `last` for anything new | Framework 0.366.0 |
| **`PhobosEnforceUpgradeResearch`** | Does nothing since Framework 0.137.2, as Vehicle Framework enforces upgrade research itself; kept so older trees that name it still load | Framework 0.137.2 |
| **The tipper's `engineDemand`** | No longer read; kept so old patches load | [Tipper](../vehicle-features-modding.md#tipper) |
| **Saved objects of a moved class** | Load as their own mod's copy once a class family is registered | Framework 0.309.0 |

**How long an obsolete name stays** (the owner, 28 Nov 2026): **until the next version that raises the first number** (an
incompatible release). That version removes them all at once, and its changelog lists every one, so you can update in one go.

## Where next

- [Your first hour](first-hour.md): the example mod, step by step.
- The guides: [D'thorga Framework](../framework-modding.md), [vehicle systems](../vehicle-systems-modding.md) and [vehicle
  features](../vehicle-features-modding.md); and [each other mod's page](README.md).
- The reference site, with every field of every class: <https://phobos-dthorga.github.io/phobos-rimworld/>.
