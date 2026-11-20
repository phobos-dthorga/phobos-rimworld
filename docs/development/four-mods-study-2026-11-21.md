# What four new mods teach us: Farm and Ranch, Moving Parts, Realistic Rain (21 Nov 2026)

**Status (21 Nov 2026):** study done. LM1 (a wider removal sweep) and LM2 (ground that stays wet after rain) are being built in
D'thorga Framework 0.204.0; the other rows are ideas only. Nothing here is tested in game.

The owner asked on 21 Nov 2026: "Is there anything we can learn from them and especially, apply to our own, Rimworld, mods? Or even
use as a soft/hard dependency?" The owner subscribed to Farm and Ranch only so it could be studied, and does not mean to play with it.

## Read this first

- **No dependency, hard or soft, on any of the four** (agent's recommendation, owner's go-ahead to build LM1 and LM2). All four were
  first published in November 2026, none carries a licence, and none has a public repository. We take ideas only, written fresh
  in our own code; no code, model, shader or XML is copied.
- **Moving Parts cannot load on RimWorld 1.7.** It is built for 1.6, and a player reported on 19 Nov 2026 that it fails at the
  main menu on `UnityEngine.FrameTiming`, which 1.7's Unity stand-in lacks. The author had not answered by 21 Nov.
- **Realistic Rain is switched on in the owner's game** (load position 158) but had not run there by 21 Nov: it arrived after the
  last launch closed. If rain or snow costs frame rate, its reflections setting is the first thing to switch off (one player
  went from 250 to 150 FPS before the author's optimisation; snow at full zoom-out still costs 50 to 70 FPS by one report).
- **A player who runs Farm and Ranch beside D'thorga Farming** should mark each field for one mod's machines, not both: its
  harvester ignores our crop classes and would cut the flour crops we exclude. This belongs in Farming's README when next touched.

## The mods

| Mod | Author | Workshop | Versions | In the owner's game | What we read |
| --- | --- | --- | --- | --- | --- |
| Farm and Ranch - Tractors, Harvesters & Smart Incubator | 澪 | [3841196254](https://steamcommunity.com/sharedfiles/filedetails/?id=3841196254) | 1.6, 1.7 | Subscribed, off (for study) | Its Defs, language files, a decompile kept in `.local`, all 8 comments |
| Moving Parts: Core | Enzo | [3837406060](https://steamcommunity.com/sharedfiles/filedetails/?id=3837406060) | 1.6 | Subscribed, off | The C# source, README and technical document it ships, both comment pages |
| Moving Parts: Warfare | Enzo | [3839986830](https://steamcommunity.com/sharedfiles/filedetails/?id=3839986830) | 1.6 | Subscribed, off | Its Defs and README |
| Realistic Rain | HoloceneHorror | [3835737267](https://steamcommunity.com/sharedfiles/filedetails/?id=3835737267) | 1.7 | **On** | Its shaders, Defs, patches, a decompile kept in `.local`, all 29 comments |

Moving Parts ships its source but no licence, so it is source to read, not to take. Its author takes the same view of another
author's unlicensed repository in their own technical document: they read it and wrote their own.

## Dependency verdicts

| Mod | Verdict | Why |
| --- | --- | --- |
| Farm and Ranch | A compatibility note only | Its machines are 3×3 buildings that never move, with a sprite slid over the field; there is nothing in it our vehicles would call |
| Moving Parts | Nothing now | 1.6 only; it hides Vehicle Framework's upgrade overlays, which our fittings and paint rely on; GPU cost never measured. Should a 1.7 build appear, a tie-in would be XML only, in an `IfModActive` folder, and would need 3D models we do not have |
| Realistic Rain | No gameplay dependency | Its author calls it purely visual, and its figures move with the player's cosmetic sliders (puddle amount, drying speed), so no gameplay should read them. Dubs Bad Hygiene stays the one soil-moisture model the owner chose |

## The rows

| Row | Idea | From | Where in ours | Status |
| --- | --- | --- | --- | --- |
| **LM1** | A removal sweep that also clears bills, filters, blueprints, frames and jobs | Farm and Ranch | Framework `Uninstall` | **Building, 0.204.0** |
| **LM2** | Ground that stays wet after the rain stops | Realistic Rain | Framework core rule and map figure; vehicle grip and landing ground | **Building, 0.204.0** |
| LM3 | Exhaust smoke that follows engine load, drawn only on screen | Moving Parts | Framework core fleck emitter, fed by the powertrain | Idea |
| LM4 | One wind direction for each map, wandering slowly (the game has wind speed but no direction) | Moving Parts | Framework core; smoke, landing dust, the sprayer's drift | Idea |
| LM5 | Tyre dust and mud thrown up by terrain | Moving Parts | Framework.Vehicles, with our own terrain figures | Idea |
| LM6 | Our first shader, from plain GLSL text (new in 1.7) | Realistic Rain | Frost on the liquid oxygen store, paddock overlays, gas | Idea |
| LM7 | A "build a machine for this paddock" button on the growing zone, locked machines naming their research | Farm and Ranch | D'thorga Farming's paddock gizmos | Idea |
| LM8 | Ready dispatch: a grower sent to the machine once a share of the paddock is ripe, with "Go now" | Farm and Ranch | Farming's field run | Idea |
| LM9 | Feed grades that give an animal a one-day condition (more produce for more hunger) | Farm and Ranch | A feed mill in the farming side-industry | Idea |
| LM10 | Fading wheel tracks behind a field run | Farm and Ranch | Framework.Vehicles, visual, unsaved | Idea |
| LM11 | A file-driven test channel for a throwaway test colony, never the owner's save | Moving Parts | Framework dev part, with D'thorga Companion | Idea |

## LM1: a wider removal sweep

**What it lets the player do:** remove one of our mods from a running colony and leave nothing behind for the game to complain
about at the next load. **What it stops:** nothing the player can still use once the mod is gone. **How it changes play:** removal
becomes one button, then save, then switch the mod off. **Who notices outside it:** the colony's bills, stockpiles, policies and
colonists' work, which lose what the mod gave them; a message names how much was cleared.

Today the sweep removes health conditions only (`CleanupService.RemoveHediffsOf`). Farm and Ranch's sweep, read in its decompile,
also ends jobs, cancels blueprints and frames, takes down buildings with a refund, destroys items, deletes bills, unticks its things
in every filter and policy, and keeps its map components out of the next save.

**Built in 0.204.0 (agent's choice of slice):**

- **Bills** whose recipe the mod adds, on every work table and every pawn's operation list.
- **Filters**: the mod's things unticked in every stockpile zone, storage building, storage group, bill and apparel and food policy.
- **Blueprints and frames** of the mod's buildings cancelled, with what was delivered dropped on the spot, as the game does.
- **Jobs** that use the mod's job kinds ended.
- Health conditions, as before.

**Left for later, and why:**

- **The mod's built things and items.** Destroying them is a loss the player cannot undo, and a vehicle holds crew and cargo.
  These need their own confirmation and a vehicle-aware step (crew out, cargo dropped). Risk if built: high, so off by default.
- **Map and world components kept out of the save.** This needs a patch on how the game fills components, and our own code would
  then meet a missing component for the rest of the session. The game already skips a missing component's data at load with one
  line in the log.

**Risk grade (agent's proposal):** moderate. It clears bills and filter ticks the player may have set by hand, all at once, but only
on a button the player presses, after a confirmation that says what will go. No setting: the button is the choice.

## LM2: ground that stays wet after the rain stops

**What it lets the player do:** read the weather's after-effects: wait for the ground to dry before a heavy tow or a landing on
open ground. **What it stops:** grip coming back the moment the rain stops, and dry dust and grass fires straight after a downpour.
**How it changes play:** rain matters for hours after it ends, longer in the cold and at night, shorter in hot sun and wind.
**Who notices outside it:** vehicles' grip on open ground, an aircraft's landing (dust and grass fires), and later D'thorga Farming's
moisture where Dubs Bad Hygiene is absent.

**The model (agent's design; every figure authored, a gameplay choice):**

- **One figure for each map, 0 (dry) to 1 (soaked), for open ground.** Under a roof it reads 0. Nothing is kept for each cell:
  Realistic Rain keeps two figures for every cell and loops over the whole map every two in-game minutes, which a gameplay figure
  does not need.
- **Rain wets it** in proportion to the game's own rain rate: an hour of ordinary rain (rate 1) soaks it.
- **It dries** at a rate set by the outdoor temperature, the daylight and the wind, each a factor of 1 at the reference (20 °C,
  full daylight, the game's ordinary wind): fully wet ground dries in about 10 hours in those conditions, about a day across a night,
  and barely at all below freezing.
- **Grip reads the wetter of the rain falling now and the ground's wetness**, so it still drops the moment rain starts (the first
  minutes of rain on a road are the slickest in life too) and recovers as the ground dries.
- **Landing ground counts wet ground as wet**: above a quarter wet, no dust is thrown up and no growth catches.

The shape agrees with Realistic Rain's picture (ground dark within the first hour, dry in about a day), so what the player sees and
what our figures say do not contradict each other. Hydrologists keep a running figure of the same shape, an "antecedent
precipitation index" (today's figure is yesterday's times a decay factor, plus today's rain); that is from memory and **unverified**,
and no figure here is drawn from it.

**Risk grade (agent's proposal):** low. It slows a vehicle a little for some hours after rain, which the player can see coming. It
sits under the existing setting **Rain, snow and ice take grip**; no new setting.

## Mistakes we will not copy

- **Guessing terrain from the words in its defName** (Realistic Rain and Moving Parts both do). Against the owner's rule to match
  effects, not names.
- **Swapping a turret verb's private target for one call** (Moving Parts). The shape of our Mongoose crash of 17 Nov 2026.
- **Patching the engine's internal rendering types** (Realistic Rain's reflections): the part most likely to break on an update.
- **Letting a cosmetic slider scale a figure gameplay reads.**
- **Sowing with no seed and skipping the yield stat and the failure roll** (Farm and Ranch). Ours keeps the game's harvest honest.
- **A feed that gives 60 for the ingredients of 50** (Farm and Ranch's dense kibble): value from nothing, by our rules.

## What 1.7 newly allows: shaders from text (LM6)

Read in the game's 1.7 decompile and in Realistic Rain. A `ShaderDef` now has a `shaderProgram` (the engine's
`ShaderProgramDefinition`: vertex and fragment file names, blend, depth, cull, render queue). At start-up the game adds every
active mod's `Shaders` folder, registers each Def under its defName, and compiles the GLSL at run time. The game's own shaders are
plain GLSL in its `Data/Core/Shaders`, so worked examples exist. A `MaterialDef` then names the shader with its textures and
figures. A static effect needs no C#.

Limits found: a mod's shader file with the same name as one of the game's replaces it for every mod, so ours need prefixed names;
compile errors show only at run time; how the game's global shader figures reach a mod's shader was inferred, not found; deploy and
the gate have not been checked with a `Shaders` folder.

## What ours already does better

- **Keeping hands out of machine fields**: ours refuses a whole paddock before any cell is looked at; Farm and Ranch filters cell by
  cell for every grower, and misses the game's 40-cell gather (our fault F7, fixed in Framework 0.202.0).
- **Honest harvests**: the game's yield stat, failure roll, seed and second products. Farm and Ranch's harvester probably leaves
  Vanilla Expanded dual crops standing with a driver aboard (the driver is off the map when the second product is placed): an
  inference from its code, not seen in play.
