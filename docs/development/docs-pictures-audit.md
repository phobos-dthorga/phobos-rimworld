# Docs and pictures audit (28 Nov 2026)

**Status (28 Nov 2026):** audited; night 18's pictures queued, not yet run. Nothing else built: findings D1 to D5 and G2 to G4
wait for the owner's word. Agent's audit, on the owner's request of 28 Nov 2026: "queue graphics that would be beneficial to
enhancing our documentation ... do an audit now and see where the docs can benefit in more ways than one from just graphics, and
maybe the game itself too".

## Read this first

- **Night 17's three icons never ran** (the dual-band radio fitting, Raise the mast, radio coverage): no picture of them exists. They
  are first in night 18's queue now, so they are not lost again.
- **Nothing here ships until the owner picks.** Scenes are judged on a contact sheet, icons on review cards (`review_cards.py
  sprites`), both offered on D'thorga Companion's Picks page.
- **Scenes are docs art, so they are made locally** (assets/AGENTS.md: only what appears in the game world goes to the paid route).
  The previews' recipe is kept (Flux.1-schnell seeds 1 to 4, Z-Image two seeds, 1280 x 720, lower third dark for a title), but the
  words "RimWorld mod preview" are dropped: on night 4 they made schnell paint a fake game logo.

## Queued for tonight (`.local/art/night-18.json`)

Estimated from earlier runs: about 2.5 min an icon, 7 s a schnell scene, 20 s a Z-Image scene; about 1.5 hours in all.

| Step | Request | Pictures | What for |
| --- | --- | --- | --- |
| 1 to 3 | `night-17-dual-band-icon.json`, `night-17-road-icons.json`, `night-17-cover-icon.json` | 6 | Night 17's three icons, carried over |
| 4 | `night-18-standard-batch4-icons.json` | 6 | Landing lights, searchlight, galley (another session's) |
| 5 | `assets/phobos-previews/requests/night-18-docs.json` | 72 | Ten guide scenes, D'thorga Farming's missing preview, a house crest |
| 6 | `assets/vehicle-systems/requests/night-18-borrowed-icons.json` | 12 | Six icons the game borrows today (G1) |

### The scenes (step 5)

Six tries each (four schnell, two Z-Image). Each request entry's `art_plan.for` names the page it is for.

| Scene | For | Why |
| --- | --- | --- |
| guide-front-door | The player guide's front door | A colony with a truck, mast, tent, tractor and helicopter: every mod at a glance |
| guide-getting-started | Getting started | No picture today |
| guide-aircraft | Aircraft and sling loads | No picture in 479 lines |
| guide-camping | Camping and life on the road | No picture today |
| guide-supply-network | Setting up a supply network | No picture today |
| guide-damage-repairs | Vehicle damage and repairs | No picture today |
| guide-vehicle-workshop | The vehicle workshop | Only icon figures today |
| guide-on-the-road | Vehicles on the road | No picture in 640 lines |
| guide-submarines | Submarines | Only the refits figure today |
| guide-modding | `docs/modding/README.md` and the reference site's front page | The modders' pages have no picture |
| hero-farming | D'thorga Farming's `About/Preview.png`, its guide chapter and README | **The only mod with no Workshop preview** |
| house-crest | The guide's front door, README tops, the reference site's logo and favicon | **Agent's idea, the owner's to take or leave**: the brand has no mark of its own. A red dragon with golden-orange wings, cream horns and amber eyes on a dark shield. Any motto is added by script, never generated |

Five chapters already open with their mod's preview (Factory, Medical, Patches, Radio, Water Tap). **Working vehicles and Vehicle
systems need no new art** (D1).

### The icons (step 6)

Agent's choice of which borrowings to redraw: those where the borrowed picture shows something else, so a player is misled.

| Icon | Ships as | Borrows today | Uses covered |
| --- | --- | --- | --- |
| Engine fire suppression | `Fittings/SuppressionEngine.png` | The suppressant bottle (an item) | 6 |
| Crew fire suppression | `Fittings/SuppressionCrew.png` | The same bottle, so the two fittings look alike | 6 |
| Electric pump | `Fittings/ElectricPump.png` | The deep-cycle battery | 5 trees |
| Route kit | `Fittings/RouteKit.png` | The tow hitch | 2 |
| Dining annex | `Vehicles/FieldKitchen/BillyDiningAnnex.png` | The game's stone wall menu icon | 1 |
| Radio school | `Radio/UI/RadioSchool.png` | The game's Radio talking lesson icon | 1 |

## After the run

1. Background masks (`python scripts/art/clean_backgrounds.py masks --cpu`), then icon cards: `python scripts/art/review_cards.py
   sprites REQUEST --size 128` for each icon request (offered on the Picks page).
2. Scenes: a contact sheet per scene (`scripts/art/contact_sheet.py`), offered on the Picks page.
3. On picks: icons ship with `scripts/art/ship_picks.py REQUEST NAME=TRY` and their Defs are pointed at them (a fix version for
   Framework, Vehicles and Radio); scenes are titled with `scripts/art/make_preview.py` and put at the top of their chapter, each with
   a record. Archive every try to the art branches (`scripts/art/archive_branch.py`).

## Findings beyond pictures (no graphics card)

Ranked by gain for the work, agent's judgement.

| # | Finding | Proposal | Who |
| --- | --- | --- | --- |
| D1 | Working vehicles and Vehicle systems have no opening picture, yet the Vehicles preview (trucks on a ridge) and the Framework preview (a mechanic under a bonnet) fit them | Put each at the top of its chapter | Agent, on the owner's word |
| D2 | Four chapters have no figure at all: aircraft, camping, vehicles on the road, supply network | New figures from shipped textures with `scripts/art/guide_figures.py`: aircraft fittings (cargo hook, rescue hoist, fast rope, ferry tank, ECM pod), camp gear (deck tent, cab bunk, Billy kitchen), road-work and supply-run buttons, the route kit and depot | Agent, on the owner's word |
| D3 | Chapters that describe a chain of events have no diagram | Mermaid diagrams: hit to knocked out to wreck to repair (damage and repairs); the supply network's worked example; the aircraft's checks before take-off; the radio's link from set to coax to antenna to path to receiver. For modders, one map of how a family, loadout, fitting, upgrade node and system refer to each other, at the top of `vehicle-features-modding.md` (2,183 lines, no diagram) and `vehicle-systems-modding.md` | Agent, on the owner's word |
| D4 | No mod README opens with a picture, so each GitHub mod folder is text only | Put each mod's `About/Preview.png` under its title (Farming after its pick) | Agent, on the owner's word |
| D5 | No Workshop page shows a picture beyond Steam's own preview | Steam's `[img]` can show guide figures from the public repository; check how it looks on the Publish page's preview first. Uploading stays the owner's | Owner's choice |
| D6 | Long chapters have no contents list | **Not needed**: GitHub and the reference site both draw one for every page. Agent's judgement | None |

## Findings in the game

| # | Finding | Proposal | Who |
| --- | --- | --- | --- |
| G1 | 73 borrowed pictures are open in `python scripts/art-audit.py` | Six redrawn tonight (above), covering 21 of them | Queued |
| G2 | The depot's world map icon is the fuel lamp of the dashboard | A white shape on transparency, like the other world map icons, drawn by script: no graphics card | Agent, on the owner's word |
| G3 | Things seen in the world still borrow: the canvas wall (the game's wall atlas), the analyser bench (the Crawler's laboratory icon), the cabin beds (the swag) and the dual-band radio set as an item (the repeater kit) | Paid route, two tries each, one card each (assets/AGENTS.md); the canvas wall's atlas by script, the owner's choice of 17 Nov 2026 | Owner's go and spending limit |
| G4 | Many borrowings show the same thing, or rightly another mod's | Accept them in `config/art-audit.json` with their reasons, which empties the open list: the steam engine's firebox warmth (the cab heater lamp: same job), bowser lagging (the tanker's insulated tank), the C-27J's stretchers (the litter kit), the long-range family (the auxiliary tank), trailer water treatment (the reverse-osmosis kit), Frontline Workshop's and the MaxxPro's gun mounts (their authors' own gun pictures), and the game's own pictures used for the same act (deconstruct to take down a relay mast, the trade-request icon for contracts, the growing zone for paddocks, motes, glow and the skyfaller shadow) | Owner's word |
| G5 | The crew water tank and the trailer water feeds borrow the power point's picture | Check whether they are ever drawn (their names say stand-in) before choosing a route | Agent |

**What a next game would need** (the shared rule on building for every game): the scene recipe is data (a request file and a title
script), so another game needs only its own prompts and a preview size; the art audit's borrowed list comes from phobos-modkit.
