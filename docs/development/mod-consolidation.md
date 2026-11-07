# Mod consolidation: seven mods into two, eleven into six (7 Nov 2026)

The record of why and how eleven D'thorga mods became six on 7 November 2026 (seven of them merged into two), who decided what, and what came of it. **Not yet
tested in game** as merged mods. The player guides are [D'thorga Medical](../../mods/PhobosMedical/README.md) and
[D'thorga Patches](../../mods/PhobosPatches/README.md); the standing rule it produced is "Fewer, broader mods" in `AGENTS.md`.

## The decision

| | Before (11 mods) | After (6 mods) |
| --- | --- | --- |
| Medical | Burns and Smoke 0.5.0, Emergency Dosing 0.3.1, Triage 0.2.0, Medical Equipment 0.5.0, Medical Save Bridge 0.1.2 | **D'thorga Medical 0.6.0** |
| Other authors' mods | Vehicle Patches 0.6.0, Missile Patches 0.1.0 | **D'thorga Patches 0.7.0** |
| Unchanged | D'thorga Framework, Factory Additions, Water Tap, Radio Framework | the same four (Framework 0.5.0 to 0.6.0) |

**Why now.** The owner, 7 November 2026: "we have an absolute metric tonne of disparate mods and it's getting rather silly at this
point, and even quite hard to maintain." The agent's survey that day found the cost was mostly ceremony repeated per mod (a Mod
class, a settings window, a rules Def, a load line, a README with gotchas, a changelog, a version kept in three places) and that
**nothing was on the Workshop yet** (no mod had a `PublishedFileId.txt`), so a merge cost no item IDs, subscribers or public saves.
After the first upload every merge would be far more expensive.

**Who chose what.**

- The merges, the order (Patches first, then Medical) and keeping Factory Additions, Water Tap, Radio Framework and the Framework
  separate: **the agent's recommendation, agreed by the owner** ("I agree with everything else").
- **The save bridge joins D'thorga Medical, behind a question on load** (convert now, not now, or never): **the owner's call**. The
  agent had recommended keeping it a separate mod, because it is a one-time conversion most players never need and the one medical
  mod whose main path was tested in game. The owner's design answers both: it stays inert unless a save needs it, and it no longer
  acts without asking.
- Versions, identities and every detail of how: agent defaults, below.

**What was argued against, and why** (the agent's reasoning, accepted with the plan):

- **Water Tap into Factory Additions: no.** Factory Additions hard-requires Vanilla Furniture Expanded - Factory and Vanilla Expanded
  Framework; a Water Tap player needs neither.
- **Anything into the Radio Framework or D'thorga Framework: no.** The radio framework is meant to have a public identity other
  authors depend on, and the Framework must stay free of content so others can use it without our content mods.
- **Medical into the Framework: no.** The Framework answers questions (who is a patient, how dangerous a condition is, who can
  operate); the content mod decides what to do about them.
- **What merging does not fix:** most remaining upkeep is the number of other authors' mods we tie into (Factory Additions alone has
  fifteen optional folders), which a merge leaves as it is.

## How it was done

**Identities kept.** RimWorld saves name classes by namespace and type name (not by assembly), Defs by defName, and settings by key.
So every namespace (`Phobos.BurnsAndSmoke`, `Phobos.Dosing`, `Phobos.Triage`, `Phobos.Medical`, `Phobos.MedicalSaveBridge`,
`Phobos.VehiclePatches`, `Phobos.MissilePatches`), class name, defName, language key and saved key was kept; only the packaging
changed. A save made with the old mods should load with the new ones after the game's usual "mod list changed" warning. **Not yet
tested in game.**

**Layout.** One project, `src/PhobosMedical`, with a folder per former mod; one test project, `tests/PhobosMedical.Rules.Tests`,
likewise. The four other medical mods' XML, textures and language files moved into `mods/PhobosMedical` (Medical Equipment's folder,
renamed); no two files shared a path. D'thorga Patches keeps one assembly per patched mod, each in that mod's `IfModActive` folder
(`PhobosPatches.Harrier`, `PhobosPatches.Irbm`), as the optional tie-in rule requires. Files were moved with `git mv`, so their
history follows them.

**One Mod class per merged mod** for D'thorga Medical: the five former Mod classes became static parts that report a load line each,
draw a section of one scrolling settings window, and read their settings from one file. One display mode for every part (the owner
had Operator in all four). One button removes every condition the mod adds. One Harmony ID, `PhobosDthorga.Medical`, and one pass
applying the patch classes. D'thorga Patches keeps a Mod class per assembly, because each loads only with its own patched mod.

**Settings carried over once.** The game names a settings file after the mod's folder and Mod class, so every merged mod would have
started from defaults. The Framework's new `SettingsImport` reads each old file once, on the first start, through the game's own
reader. The owner's files held real choices (Triage's authority levels and death-chance limit, two Dosing choices, Operator display
mode, a 12.4 ms missile budget), so this matters.

**Guards against running old and new together.** Both merged mods list the replaced package IDs under `incompatibleWith`, so RimSort
warns, and D'thorga Medical logs an error naming any replaced mod still active.

**The save bridge's question.** The game reads a save's header twice: once in the load menu (`GameDataSaveLoader.CheckVersionAndLoadGame`
through `PreLoadUtility.CheckVersionAndLoad`, which shows the game's own version and mod-mismatch dialogs) and once in the real load
(`SavedGameLoaderNow.LoadGameFromSaveFileNow`). A prefix on the first adds the question after the game's dialogs and before the load;
a prefix on the second tells the header gate which file is loading; the gate takes the player's choice for that one load. It asks only
when converting would change something (new body parts, or More Injuries new to the save; agent choice, since asking about every new
mod would ask on almost every load). "Never" is kept per save file name in the settings, with a button to forget. A load another mod
starts without the menu converts, as the bridge always did. **Switching to D'thorga Medical does not ask**: it adds no body parts.
Read from the 1.6 and 1.7 assemblies; **not yet tested in game.**

**Framework 0.6.0 grew where both merged mods needed it**: `SettingsImport`, `SettingsUi.Section` and `SettingsScroll`, and a part
name on `LoadReport` ([framework record](phobos-framework.md)). Two candidates the agent had proposed were **not** built: a
frame-budgeted work queue from the missile fix, because the owner has proposals for that mod still to come, and a base class for
interval map components, because it would change the timing code of parts the owner has tested in game.

## What came of it

- **Builds:** D'thorga Medical, its Dubs Bad Hygiene tie-in, both D'thorga Patches assemblies and the Framework build for RimWorld 1.7
  and 1.6 with no warnings. The game-free rule tests, the repository's Python tests, the layout and link checks pass (see the commit).
- **A fault found and fixed on the way:** Medical Equipment's Dubs Bad Hygiene tie-in had been shipping a second copy of
  `PhobosFramework.dll` since Framework 0.5.0, through a transitive project reference (the owner's installed copy has it). The shared
  build rules now make every project reference `Private=false` by default, so no mod's folder carries another mod's assembly.
- **A fault found and fixed on the way:** the one-time Vanilla Gravship Expanded letter saved its "shown" flag through its own settings
  object, which has no owning Mod after the merge and would have thrown; it now saves through D'thorga Medical.
- **The missile fix's first test came in during the merge** (the owner, 13:18): the game no longer froze, but the blast was still
  running 14 minutes later. It moved into D'thorga Patches unchanged, at the owner's request, pending their proposals; the
  [missile record](missile-patches.md) has the result and a correction to its first diagnosis.
- **Left behind:** nothing in the repository; the old folders' ignored build outputs were deleted. The owner's game `Mods` folder
  still holds the seven old mods until they are switched off and the new two deployed (the game was running, so nothing was
  deployed in this change). Deleting the old installed folders is the owner's call, after the new mods load a colony cleanly.
- **Not yet tested in game:** everything about the merged mods.

## Test protocol (owner runs it)

1. With RimWorld closed: `python scripts/deploy.py --build --apply PhobosFramework PhobosMedical PhobosPatches`.
2. In RimSort: **switch off** Burns and Smoke, Emergency Dosing, Triage, Medical Equipment, Medical Save Bridge, Vehicle Patches and
   Missile Patches; **switch on** D'thorga Medical and D'thorga Patches. Sort. RimSort should show no incompatibility.
3. Start the game. Read the load lines: one per part, each "[D'thorga Medical] 0.6.0 loaded (part)", and "Settings carried over from"
   naming four mods. No error naming a replaced mod.
4. Mod settings, D'thorga Medical: one window, sections, scrolls; Triage's authorities and death-chance limit as you had them; display
   mode Operator.
5. Load a copy of the current colony. The game warns that the mod list changed; load anyway. **No save-bridge question should
   appear.** Check a burned or treated colonist, a bed with an oxygen device, a patient with a triage tag. Report any error naming
   `Phobos.`.
6. Optional, the question itself: on a throwaway save made without More Injuries, switch More Injuries on and load: the question
   appears. Try "Not now" (the log says HELD), then load again and "Convert now" (the log says ACTIVE). Don't save the "Not now" load.
