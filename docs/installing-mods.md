# Installing the mods

**Nothing is on the Steam Workshop yet.** Every mod installs from source today: build it with the .NET SDK, then copy it into
the game with `scripts/deploy.py`, **with RimWorld closed**. **Code → Download ZIP** gives you source, not a built mod, because
the assemblies are not committed.

## Read this first (gotchas)

- **Back up your save first.** Saves are in `%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Saves`.
  Your AppData folder is not covered by most cloud sync tools.
- **Close RimWorld before deploying.** The script refuses while the game runs, so a half-copied mod never loads.
- **Load order matters.** D'thorga Framework goes above every mod that needs it; Harmony, Core and the DLCs go above everything.
  Each About file asks for the right order, so RimSort and the game's own sorter get it right.
- **Switch each mod on yourself.** The script copies mods into the game's `Mods` folder; it never edits your mod list.
- **RimWorld 1.7 leads.** Each mod with code carries a 1.7 and a 1.6 assembly; the game loads the right one. The radio framework
  still declares 1.6 only.

## From source (today)

You need Git, Python 3 and a .NET SDK that builds `net10.0`.

```text
git clone https://github.com/phobos-dthorga/phobos-rimworld.git
cd phobos-rimworld
python scripts/deploy.py --build --apply --all          build every project, then copy every mod
python scripts/deploy.py --build --apply PhobosTriage   build, then copy one mod (name the folder under mods/)
python scripts/deploy.py --all                          only list what would be copied
```

The script finds the game through `--game`, the `RIMWORLD_PATH` environment variable, `.local/install-settings.json`
(`"rimworldPath"`), or the default Steam folder, in that order. It backs up whatever it replaces to `.local/deploy-backups/`.
[Building](development/building.md) has the details.

Then start RimWorld, open **Mods**, switch on the mods you want with their requirements, and let the sorter order them. Each
mod's README lists what it needs; the [project README](../README.md#the-mods) has a diagram of all of them.

## From the Steam Workshop (later)

When a mod is published:

1. **Subscribe on the Steam Workshop.** Steam downloads it, and RimWorld lists it in **Mods**.
2. **Subscribe to what it needs**: Harmony, D'thorga Framework, Vanilla Expanded Framework, and so on. Its README says which.
3. **Turn it on in the Mods menu** (or in RimSort), below what it needs.
4. **Read its "Read this first" section** before loading a save: some settings are best chosen before you start a colony, and
   the README says whether the mod is safe to add to a running save.

## Before you add or remove any mod

- **Every D'thorga mod is safe to add to a running colony.**
- **Removing one may need a step first**: removing conditions it added, deconstructing its buildings, or deleting bills that name
  its recipes. Each README says what, under "Read this first".

Problems? See [help and troubleshooting](../SUPPORT.md).
