# Our Architect tab and its sub-tabs (21 Nov 2026)

**Status (21 Nov 2026):** built in D'thorga Framework 0.205.0, with Farming 0.7.2, Medical 0.19.1 and Radio 0.1.9. Not yet tested in
game.

The owner asked whether all our buildables should sit under an Architect tab of our own, and chose by question cards on 21 Nov 2026,
after seeing Vanilla Gravship Expanded's sub-tabs in their game.

## Read this first

- **One D'thorga tab on the bar**, in Motor pool's old place beside Vehicle Framework's tab. Inside it, four sub-tabs: **Motor
  pool** (vehicles and their workshops), **Farming** (machines, field bin, silo), **Medical** and **Radio** (owner).
- **Three things stay beside another mod's buildings** (owner): the water bottle tap in Dubs Bad Hygiene's tab, the oxygen electrolyser and
  liquid oxygen store in Vanilla Gravship Expanded's vacuum tab.
- **Who lists the sub-tabs:** Vanilla Gravship Expanded while it is active (each of our sub-tabs also carries its marker), so one mod
  draws the Architect. Without it, D'thorga Framework does, with the game's own buttons (owner: "build our own"). Where another mod
  replaces the Architect (Better Architect; Dubs Mint Menus without Vanilla Gravship Expanded), each sub-tab is a tab of its own there.
- **The owner's game runs Vanilla Gravship Expanded**, so what the owner sees is its listing. Our own drawing shows only in a game
  without it.

## What the game and Vanilla Gravship Expanded do (read 21 Nov 2026)

- **The game has no sub-tabs.** Its Architect lists every `DesignationCategoryDef` on the bar (`MainTabWindow_Architect.CacheDesPanels`)
  and draws the open tab's buttons from `DesignationCategoryDef.ResolvedAllowedDesignators` (`ArchitectCategoryTab.DesignationTabOnGUI`);
  its search asks the same property of every tab on the bar.
- **Vanilla Gravship Expanded's sub-tabs** come from its `NestedCategoryExtension` (`parentCategory`, `iconTexPath`) on a tab: it takes
  such tabs off the bar and lists them down their parent's side, the parent's own buttons under "More". It stands down when Better
  Architect is active, and has its own patch for Dubs Mint Menus.
- **A list item with `MayRequire`** naming a missing mod is skipped by the game's loader, so VGE's marker on our tabs costs nothing
  without it.

## How it works

- **The tabs** are `DesignationCategoryDef`s: `PhobosDthorga` (Framework core), `PhobosVehicles_MotorPool` (Framework's Vehicle Framework
  part, defName kept), `PhobosFarming_Architect`, `PhobosMedical_Architect`, `PhobosRadio_Architect`. Each sub-tab carries
  `Phobos.Framework.Interface.PhobosNestedCategoryExtension` and VGE's `NestedCategoryExtension` with `MayRequire`. Radio does not need the
  Framework, so both its markers say `MayRequire="PhobosDthorga.Framework"` too: without the Framework it is a tab on the bar.
- **Our own listing** (`ArchitectSubTabs`, two Harmony postfixes, each with its reason in its comment): the window lists its tabs
  without the sub-tabs; a parent shows a button for each sub-tab first, and a sub-tab a button back. The buttons are the game's own
  designators, so they sit in the same grid as every build button. **While the player searches, the parent shows what its sub-tabs
  hold**, so a search still finds them. Both patches stand down unless ours is the drawer, decided once from the active mods.
- **Icons:** pictures we already ship (an engine assembly, the combine, the medical equipment category, the radio signal), and the
  game's own left arrow for the button back.
- **Nothing saved**: a tab is chosen afresh each session, as the game's own are.

## What the behaviour answers for

- **What it lets the player do:** find everything of ours in one place on the bar, sorted by what it is for, and search for it as before.
- **What it stops:** nothing. Every buildable is where its sub-tab says.
- **How it changes the way they play:** one tab to open instead of hunting through Misc among hundreds of other mods' buildings.
- **What outside it notices:** nothing, a choice: it is the Architect's layout only.
- **Risk:** none to the player's things or people, so no setting.

## Limits

- **A D'thorga tab with nothing in it** shows on the bar for a player who runs only mods of ours with no buildables (Framework and Water
  Tap alone).
- **Our own listing has not been seen by the owner**, whose game runs Vanilla Gravship Expanded: it needs a game without it.
- **Sub-tabs of a sub-tab** are not supported: a tab under a sub-tab stays on the bar.
