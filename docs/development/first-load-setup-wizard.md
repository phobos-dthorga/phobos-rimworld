# A setup wizard on first load (9 Nov 2026, designed 28 Nov 2026)

**Status (28 Nov 2026):** a design record; nothing is built yet. The owner recorded the idea on 9 Nov 2026 and asked on 28 Nov 2026 for a
generic wizard that introduces new players to our mods and helps them set up, chose by card to write this design first, then answered
its six questions ([answers](#the-owners-answers-28-nov-2026)): on the main menu, in the game, presets first, a card per mod, **built
now**, and other mods named.

How Framework services works now: [systems/framework-services.md](systems/framework-services.md); this record keeps the wizard's history.

## Read this first

- **Two jobs, one window**: **introduce** our mods to a new player, and **set up** the choices that must be made before a colony
  (the owner, 28 Nov), above all how our mods change other authors' mods (the owner, 9 Nov).
- **The hardest constraint**: some choices need a restart (D'thorga Vehicle Systems' **Which vehicles** and **Parts for building**), and
  the Radio's **Metres per map cell** should be set before a colony. So the wizard's natural moment is **the main menu, before any
  colony is loaded** [A].
- **It only drives settings that already exist.** UI presents, services decide: every choice in the wizard writes the same setting its
  mod's settings page writes, and the settings page stays the master. Skipping the wizard leaves today's defaults.
- **Generic from the start** (the shared rule, "build for every game from the start"): the pages are **data**, each mod adding its own,
  so a new setting is a row, not a rewrite. This also answers the 9 Nov worry that a wizard written early is rewritten with every option.
- Marks: **[O]** the owner's words or choice, **[A]** the agent's proposal, for the owner to revise.

## The owner's words

- **9 Nov 2026** [O]: when the time is right, offer the player a wizard the first time they load a game with our mods, to customise how
  they wish to play. "Above all, it covers how our mods affect third-party mods."
- **19 Nov 2026** [O], for D'thorga Farming (SL8): "We'll also have a wizard at first-run that helps get the player started": the
  preset (**Field care** the default), where the rules apply (**paddocks by default**, every growing zone a separate option, off by
  default) and the switches ([farming soil](farming-soil.md)).
- **28 Nov 2026** [O]: "we also need to create a genericized wizard screen which will help new players become introduced to our mods
  and help them get setup." Then, by card: "Design record next".

## What it would hold [A]

The settings each mod's README already asks a player to decide before a colony (its "Read this first"), with the player guide's
[settings to decide before a colony](../player-guide/getting-started.md#settings-to-decide-before-a-colony). This is the list on
28 Nov 2026, not a final inventory: each mod's pages are its own data, kept with its settings.

| Mod | Introduce | Set up | Touches other authors' mods | Restart |
| --- | --- | --- | --- | --- |
| **D'thorga Framework** (Vehicle Systems) | What vehicle systems add; standby power; the D'thorga Architect tab | **Which vehicles**, **Parts for building**, **How engines and drivetrains work**, aircraft too heavy to lift off, jump-starts, **Destroyed parts need replacing**, boilers, **How much to show**, the gas effects | Yes: parts on every Vehicle Framework vehicle, repairs, drivetrain, fuel, hover | Two of them |
| **D'thorga Radio Framework** | Stations, hand-helds, coverage, calling factions | **Metres per map cell**, the display mode (Plain, Operator, Engineer), radio terminals' noise, allies' raid warnings | No | No; but before a colony |
| **D'thorga Medical** | Care needs, the gases, the equipment | **Tox gas acts fast** (off: slow-acting), **People in pain fetch their own painkillers**, fires heating the air in the open, how far doctors may go without orders; which pieces are on without their optional mods | Through gases | No |
| **D'thorga Farming** | Paddocks, soil, machines | The preset (Field care), where the rules apply, **Hands may help on new paddocks**, a driver failing a harvest | Vehicle Framework machines | No |
| **D'thorga Patches** | What it fixes, by mod present | InterRim Ballistic Missile's blast settings; the Harrier's runway | Yes: by design | No |
| **D'thorga Vehicles**, Factory Additions, Water Tap | One card each | Little or nothing | Their hosts | No |

The Framework's **Keep the game's log going after its message limit** (on; it changes the game's limit for every mod) belongs on the
Framework's page too, as the 9 Nov table had it. The player guide's table is the wizard's first draft: each of its rows becomes a choice.

## How it would work [A]

### Pages as data

- **One wizard, hosted by D'thorga Framework**, which every one of our mods already depends on. Each mod adds pages; the wizard shows the
  pages of the mods that are active, in load order, after a welcome page.
- **A page is a Def in the mod's XML** (`PhobosSetupPageDef`, name for the owner to revise): its title, a short introduction, the mod's
  own preview picture, and its choices. **A choice names a setting** by a stable id that the mod registers once in C# (how to read it,
  how to write it, whether it needs a restart). The wizard never knows a mod's settings class.
- **Presets first, details on request** (the owner's standing preference for buttons and round values over sliders): a page offers two
  or three named choices ("as the game plays", the recommended one, a harsher one), with "show me each setting" opening the rows.
- **Pages that touch other authors' mods say so**, naming those mods, as the settings window already does: a window of settings, not
  text met in play, so the "in-world text only" rule does not apply to it [A, for the owner to confirm].
- **Restart choices are gathered at the end**: "These take effect after a restart", with the game's own restart prompt.

### When it opens

- **On the main menu**, the first time the game starts with one of our mods active, once per install. It opens again by itself
  **only for what is new**: a mod of ours newly active, or a page whose choices changed since it was last seen (a version on the page).
  A **"Setup and introduction" button** in the Framework's settings opens it at any time.
- **Loading a colony made before the wizard existed** changes nothing: the colony keeps its settings, and the wizard opens as above.

### The introduction, in play [A]

The game has its own teacher: the **Learning Helper** (its `ConceptDef` lessons, shown when the player first meets a thing). Our
mods could add lessons of their own, so the introduction continues in play where the wizard's cards stop, instead of a longer wizard.
An option, below.

## The four questions

- **What it lets the player do**: make the choices that matter before a colony, in one place, in plain words; meet each mod once.
- **What it stops**: starting a colony with a restart-only setting at a default the player would not have chosen; reading six READMEs
  to find which choices change other authors' mods.
- **How it changes play**: nothing in play itself; the first start is longer by a minute, and can be skipped.
- **What outside notices**: nothing in the game [A, stated as a choice]: it writes settings only. The Learning Helper option would add
  lessons to the game's own Learning Helper.

**Risk**: none to the player's things; it writes only settings they choose, and skipping keeps the defaults.

## Cost [A]

Nothing at all in play: the window exists only while open on the main menu. Reading the pages' Defs costs what any Def costs at start-up.

## What a next game would need

The page format (a page: title, introduction, picture, choices naming settings by id, presets, restart flag) is game-free data. A next
game needs: its own window (a Project Zomboid panel in Lua, an Ostranauts screen), its own way to read and write a setting by id,
and its own "first start" moment. The pages' text and presets carry over in form. **D'thorga Companion could show the same pages out of
game** (it already has Settings → Setup for its own setup), writing a game's settings file while the game is closed, which removes the
restart problem entirely; that is an option below, not a lean, because a player from the Workshop may not have the Companion.

## Questions for the owner

To be put as cards; each with the agent's lean.

1. **When it opens**: on the main menu, once per install, and again only for what is new (the agent's lean); at the first load of each
   colony; or both.
2. **Where it lives**: in the game, hosted by the Framework (the agent's lean); in D'thorga Companion, out of game; or both, from the
   same pages.
3. **How a page asks**: presets first, each setting on request (the agent's lean); every setting asked in turn; presets only.
4. **The introduction**: a short card per mod with its picture and a link to the player guide (the agent's lean); add our own Learning
   Helper lessons in play as well; or the wizard setting up only, no introduction.
5. **When to build**: the frame and the pages now, since pages as data keep up with new settings (the agent's lean); at the first
   public beta gate, as the 9 Nov view had it; or the frame now with Farming's page alone (SL8) first.
6. **Naming other mods**: allowed in the wizard as in the settings window (the agent's lean), or only "other vehicle mods" in
   general.

## The owner's answers (28 Nov 2026)

Each card as put, the chosen option first and marked; every choice was the agent's lean.

| # | Card | Chosen | Not chosen |
| --- | --- | --- | --- |
| 1 | When it opens | **The main menu, once per install**, and again only for what is new; a button in the Framework's settings reopens it | At each colony's first load. Both |
| 2 | Where it lives | **In the game**, hosted by D'thorga Framework | In D'thorga Companion, out of game. Both, from the same pages |
| 3 | How a page asks | **Presets first, each setting on request** | Every setting in turn. Presets only |
| 4 | The introduction | **A card per mod**: its preview picture, a few lines, a link to its player guide chapter | Cards plus our own Learning Helper lessons in play. No introduction |
| 5 | When to build | **The frame and the pages now** | The frame with Farming's page alone first. At the public beta gate |
| 6 | Naming other mods | **Yes, as the settings window does** | General words only ("other vehicle mods") |

## History

- **9 Nov 2026**: recorded for later ("Status: recorded for later"), with an example table of choices that change other authors'
  content and the agent's view that it should wait until the settings had settled, at the public beta gate at the latest. Its open
  points (per install or per colony, one wizard or one per mod, skipping, restart choices, the four questions, player text) are
  answered above as proposals.
- **19 Nov 2026**: Farming's SL8 made it the wizard's first use.
- **28 Nov 2026**: the owner asked for it as a generic wizard and for this design.
