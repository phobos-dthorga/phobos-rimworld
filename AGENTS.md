# Phobos RimWorld contributor instructions

These are the standing rules for work in this repository: owner directions still in force, stated once
and in their current form. What each version delivered lives in the owning mod's
`mods/<Mod>/CHANGELOG.md`; designs, research and records live in `docs/development/` (indexed in
`docs/development/README.md`). The project started on 18 October 2026 from the owner's radio framework
design (`docs/development/radio-framework-design.md`); its conventions follow Phobos Ostranauts
(`phobos-dthorga/phobos-ostranauts`), adapted to RimWorld. Keep this file to rules: when a direction is
delivered, record the delivery in the changelog or design record, and add or amend only the rule here.

**This file holds what every session needs** (owner, 21 Nov 2026: "keep a slimmed-down AGENTS at the root"). The rest lives
beside its work, in the files the next section names; a session reads the one for its work before starting it.

## Rules shared by every game

**The owner's rules for every game, mod and tool repository live once, in phobos-modkit's `rules/` folder** (owner, 19 Nov 2026: "a
central point from which global rules for all mods can be read and modified"). **Read
`external/phobos-modkit/rules/every-session.md` at the start of every session**; `CLAUDE.md` imports it. (On GitHub the folder is a
pointer to the other repository: [read the rules there](https://github.com/phobos-dthorga/phobos-modkit/blob/main/rules/every-session.md).)
This file adds what is particular to RimWorld and this repository.

- **A rule that would hold in another game's repository too does not go in this file**: it goes to phobos-modkit's `rules/`, on the
  owner's word, under that repository's rules (`external/phobos-modkit/rules/README.md` says how), and the pin then moves here.
  Where this repository must differ from a shared rule, say so beside the rule here, with the owner's reason.
- **A rule here that the shared rules now state is dropped from this file** in the commit that moves the pin, so it is never
  stated twice. The twelve rules every repository repeated moved there on 23 Nov 2026 (kit 1.105.0), and this file dropped its copies.
- `python scripts/check-all.py` checks this repository still reads them (its "shared rules" step).

## Rules kept beside their work

Seven sets of rules live in the folders they govern, so a session loads them only when it works there (owner's go-ahead,
18 Nov 2026, to cut what every session reads; four more on 21 Nov 2026). **Read the file before that work, even if no file in
its folder is open yet**: running a script does not load them.

- **Tools, scripts, tests, installs and commits**: [`scripts/AGENTS.md`](scripts/AGENTS.md). Before answering a question with
  a tool, writing a script or a test, installing, or committing.
- **C# and how a mod works**: [`src/AGENTS.md`](src/AGENTS.md). Before writing C# or designing how a mod behaves.
- **A mod's shipped files**: [`mods/AGENTS.md`](mods/AGENTS.md). Before editing a mod's Defs, language files, About,
  changelog or README.
- **Guides and records**: [`docs/AGENTS.md`](docs/AGENTS.md). Before writing a guide, a design record or a research note.
- **Art and sound**: [`assets/AGENTS.md`](assets/AGENTS.md). Before generating, picking, cutting, exporting, recording or
  shipping any picture or sound, and before touching a texture or a paint mask.
- **Vehicles and submarines**: [`src/PhobosFramework.Vehicles/AGENTS.md`](src/PhobosFramework.Vehicles/AGENTS.md). Before
  designing or changing any vehicle, upgrade tree, accessory, part or submarine mechanic, in XML or C#.
- **Radio framework**: [`src/PhobosRadio/AGENTS.md`](src/PhobosRadio/AGENTS.md): its phases and its physics rules.
- **The radio framework is on the back burner** (owner, 9 Nov 2026): no new radio features or phases, and no reviews of it, until the
  owner picks it up again. The mod stays as it is, and a fix to something broken in it is still fine when the owner reports it.
- **Submarine work is on the back burner too, and out of view** (owner, 20 Nov 2026): no new submarine features or reviews
  until the owner picks it up again; nothing is removed, and a fix to something broken is still fine when the owner reports it.
  The detail is in the vehicles' rules.

## Working with the owner

- Aim for maximum creative freedom (owner, carried over from Phobos Ostranauts): risks such as a mod becoming
  unsafe to remove from a save are engineering problems to report honestly and explore, never a reason
  to drop, defer or shrink a design.
- Candidly recommend simplifying, setting aside or changing direction when gameplay value, engine limits
  or maintenance cost make further work unconvincing. Never continue merely because effort was spent.
- Distinguish observed game behaviour from proposed designs and untested assumptions, and never claim untested compatibility
  (the shared rules: nothing counts as tested until the owner says so).
- **The owner runs gameplay tests.** Read the owner's `Player.log` before assuming a reported fault is the owner's error.
- **Ordinary causes before a bug hunt** (owner, 21 Nov 2026: the HEMTT nobody loaded "proceeded ahead, it was just there being too
  much work"). When colonists or vehicles are reported not doing something, first rule out a busy colony, work priorities, allowed
  areas, and forbidden or reserved things, and ask whether it got done later, before reading code for a fault.
- **New local data goes on K:, never the repository's drive** (owner, 19 Nov 2026): caches, clones and staging take their folder
  from a key in `.local/install-settings.json`. **Never delete anything to free space without first telling the owner** what
  is short and what their choices are.
- **Ask the owner before anything attaches to their running game, and to save first**: a recording, a stack or heap snapshot,
  a memory watch, a look inside. **Look before guessing** (owner, 21 Nov 2026): when a fault is about what the running game
  holds and no save or log shows it, ask for a snapshot while it is happening, before reasoning from code alone.
- **Tell the owner whenever a setting of ours is added, removed or given a new default** (owner, 10 Nov 2026: "so I can take a look at
  them or modify them as needed"), in the reply that delivers the change: the mod, the setting's label as the settings window shows it,
  what it does, its default and where to find it. `python scripts/mod-settings.py changes --since REV` lists every such change since a
  commit; run it against the commit a piece of work started from before reporting it done.
- **Tools before LLM reading, even when the tool needs work first** (owner, 7 November 2026): when a tool we have can answer
  it, use the tool; when it could after a fix or a feature, make that change first. The table below says which tool
  answers which question; `scripts/AGENTS.md` says how to use each.
- The owner plays **RimWorld 1.7** with all five DLCs and a large mod list (about 360 mods, RimSort, Combat Extended), on a
  high-end PC: treat measured performance as a best case for players. Read a mod's latest player comments before
  suggesting it.
- The owner is a licensed amateur radio operator and an applied chemist. Radio and chemistry realism
  matter; still label every approximation, authored value and gameplay simplification as such.

### Which tool answers which question

Use the tool before reading raw files; how to use each is in [`scripts/AGENTS.md`](scripts/AGENTS.md).

| The question | The tool |
| --- | --- |
| What the test list waits for; the owner's verdicts | `python scripts/test-pass.py report`; `apply --verdict ... --apply` |
| Anything in a save | `python scripts/query-save.py` |
| A fault in the log | `python scripts/triage-log.py` |
| The game closed by itself | `python scripts/crash-dump.py` |
| Which mod defines or patches a Def, and the Def as patched | `python scripts/query-defs.py` |
| The game's or another mod's code | `python scripts/query-code.py` |
| Another author's public source | `python scripts/source-repos.py` |
| What an update of the game or a tied mod changed | `python scripts/assembly-archive.py compare` |
| Whether an update touched what we patch | `python scripts/upstream.py` |
| After an update, everything at once | `python scripts/update-check.py` |
| Which mods patch a method | `python scripts/query-patches.py` |
| What each vehicle's systems and parts are bound to, and what looks wrong | `python scripts/vehicle-bindings.py` |
| Time, a frozen game, memory, what is inside the running game | `python scripts/perf.py` (ask first) |
| How long the game takes to start | `python scripts/startup-impact.py` |
| Which mods are ready for a game version | `python scripts/version-readiness.py` |
| The settings the owner plays with | `python scripts/mod-settings.py` |
| The owner's mod list | `python scripts/refresh-mod-list.py` |
| A mod's Workshop page, comments, a collection | `python scripts/workshop-research.py` |
| A mod's version, everywhere it is written | `python scripts/release.py constants` |
| A mod's Workshop page, change notes and upload state | `python scripts/workshop.py` (`status`, `show`, `notes`) |
| What updating or going back would change | `python scripts/installs.py` |
| The code behind a changelog entry | `python scripts/changes.py` |
| What happened to our mods over time | `python scripts/history.py` |
| Whether textures need encoding | `python scripts/textures.py` |
| What is safely copied; copying trainings and models | `python scripts/safe-copies.py` |
| Harmony targets and by-name lookups | `python scripts/check-targets.py` |

## Git and publication boundaries

- **The repository is public, and its mods are for public release** on the Steam Workshop (owner,
  18 October 2026). Never change the repository's visibility.
- External contributors use forks (`CONTRIBUTING.md`). Edit in the base checkout, not `.claude/worktrees`, so the owner can
  follow the work.
- **Steam Workshop: agents never run a real upload, handle credentials, subscribe, announce or change an item's visibility.**
  The rest is in `mods/AGENTS.md`.
- Besides what the shared rules never commit, never commit extracted game textures or sounds. Resolve game paths from local settings
  (`.local/`) or arguments. Game-derived research material and the owner's handoff originals stay in the
  ignored `.local/`. Synthetic fixtures only in tests.
- Agents never execute other authors' mod assemblies or scripts outside the game. Read their XML, About
  files and published source; decompiling for research is fine, but the output stays in `.local/` and is
  summarised in our own words, never committed.

## RimWorld engineering

- **Another author's bug is theirs to fix** (owner, 8 Nov 2026). Patch an active project only when its errors are rather extreme in
  nature (a storm that kills the log, lost colony data); otherwise leave it to the author and, where possible, draft a report for the
  owner to post. **How a report is handled is a shared rule**, `external/phobos-modkit/rules/reports.md` (owner, 22 Nov 2026), read
  before drafting one. **If the project is abandoned, this reverses**: patch it. Judge that with
  `python scripts/workshop-research.py report <mod>` (update date, the uploader's last reply) and say what it showed.
- **RimWorld 1.7 only** (owner, 8 November 2026): never fix a 1.6-only failure. The detail is in `src/AGENTS.md`.

## Builds, installation and checks

- Install to the game's `Mods` folder only through `scripts/deploy.py`, with RimWorld closed:
  `python scripts/deploy.py --build --apply --all` builds every project and installs every mod (owner, 4
  November 2026); name mods instead of `--all` for one; deploy also encodes our textures to DDS with RimSort's todds, so RimSort's
  texture step is not needed for our mods. Never edit `ModsConfig.xml` or the owner's RimSort
  lists unless the owner asks, and back them up first.
- **This repository's gate is `python scripts/check-all.py`** (the shared rule: chain the commit on it, `python
  scripts/check-all.py && git commit ...`). **The commit guard enforces it**: after cloning, run `git config core.hooksPath
  .githooks`; never pass `--no-verify`. The detail is in `scripts/AGENTS.md`.
- **The shared tools come from the pinned phobos-modkit submodule** (`external/phobos-modkit`): after cloning, or in a new
  worktree, run `git submodule update --init`. A fix to a shared tool goes to phobos-modkit first; never edit the submodule
  in place.
