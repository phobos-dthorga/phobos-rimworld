# The owner's mod list

Your RimWorld mods as the game itself sees them, checked against each mod's own rules, with suggestions for
what to add next. Agents check advice and compatibility claims against this list ([AGENTS.md](../../AGENTS.md)).

**Refresh it whenever your mod list changes.** It only reads the game's files, so RimWorld can stay open:

```bash
python scripts/refresh-mod-list.py
```

Add `--online` to re-read the official Vanilla Expanded collection from Steam's public Web API (no key, no
login). Everything between a `BEGIN GENERATED` and an `END GENERATED` marker is rewritten on each run. The
suggestions are written by hand, and the refresh leaves them alone.

<!-- BEGIN GENERATED: summary -->
**As of 2026-10-28** (the date of the game's mod list): RimWorld 1.7.4990 rev1069, **300 mods switched on** (6 game and DLCs, 293 Workshop, 1 local), and **14 more subscribed but switched off**. 176 of the 294 switched-on mods already declare RimWorld 1.7.
<!-- END GENERATED: summary -->

## Read this first (gotchas)

- **This is a new-colony list.** Combat Extended is on. CE's page says it needs a new save and can't be
  removed from one.
- **Vanilla Expanded mods can be added mid-save but not safely removed.** Every VE FAQ says so. Back up the save
  before you add one.
- **Don't switch on the whole VE collection.** Its own page warns the game will "not feel like RimWorld anymore".
  Add a few, play, then add more.
- **Weapons, apparel and armour added mid-save** need your stockpile filters ticked to accept them (VE's FAQ).
- **After subscribing to anything, refresh this list.** The checks below then catch missing requirements,
  load-order breaks and mods that declare each other incompatible.

### What the mods' own rules say

<!-- BEGIN GENERATED: checks -->
- **Missing requirements:** none.
- **Switched on but not installed:** none.
- **Not marked for 1.7: 118**
  - Better Stacktraces (declares 1.6)
  - Adaptive Storage Framework (declares 1.4, 1.5, 1.6)
  - XML Extensions (declares 1.2, 1.3, 1.4, 1.5, 1.6)
  - HugsLib (declares 1.0, 1.1, 1.2, 1.3, 1.4, 1.5, 1.6)
  - Vehicle Framework (declares 1.4, 1.5, 1.6)
  - Combat Extended (declares 1.6)
  - \[MUS\]太空基地家具Space Base Furniture (declares 1.5, 1.6)
  - Adaptive Ideology Storage (declares 1.4, 1.5, 1.6)
  - Allow Tool (declares 1.0, 1.1, 1.2, 1.3, 1.4, 1.5, 1.6)
  - Anomaly Research Asteroid (declares 1.6)
  - Anthrosonae (declares 1.4, 1.5, 1.6)
  - Auto-Cut Blight - 1.6 (declares 1.0, 1.1, 1.2, 1.3, 1.4, 1.5, 1.6)
  - Bean's Turret Pack (declares 1.5, 1.6)
  - Better Gene Processor UI (declares 1.4, 1.5, 1.6)
  - Better Hot Springs (declares 1.6)
  - Bioform Matrix (declares 1.6)
  - Biosculpter Detox (declares 1.6)
  - Bladelink (declares 1.6)
  - Blueprints Forked - 1.6 (declares 1.4, 1.5, 1.6)
  - Cargo Holds (declares 1.6)
  - Cargo waste container (declares 1.4, 1.5, 1.6)
  - Choose Where To Land (declares 1.6)
  - Colony Hospital / 殖民地医院 (declares 1.6)
  - Combat Extended Armors (declares 1.3, 1.4, 1.5, 1.6)
  - Combat Extended Armory (declares 1.4, 1.5, 1.6)
  - Combat Extended Guns (declares 1.3, 1.4, 1.5, 1.6)
  - Complementary Odyssey (declares 1.6)
  - Death Rattle Continued \[1.2+\] (declares 1.3, 1.4, 1.5, 1.6)
  - Deep And Deeper (declares 1.5, 1.6)
  - Defensive Positions - Forked (declares 1.6)
  - EdB Prepare Carefully (declares 1.1, 1.2, 1.3, 1.4, 1.5, 1.6)
  - Electrofishing (declares 1.6)
  - Farmable Neutroamine (declares 1.0, 1.1, 1.2, 1.3, 1.4, 1.5, 1.6)
  - Fishing Is Fun (declares 1.6)
  - Float Sub-Menus (declares 1.6, 1.5, 1.4, 1.3)
  - Gene Ripper (declares 1.4, 1.5, 1.6)
  - Glass+Lights (declares 1.0, 1.1, 1.2, 1.3, 1.4, 1.5, 1.6)
  - Grav Reactor (declares 1.6)
  - Growth Moments Make Sense Now (declares 1.6)
  - Health Display (declares 1.5, 1.6)
  - Hemofungus (declares 1.5, 1.6)
  - Info Card Plus (declares 1.6)
  - Interaction Bubbles (declares 1.3, 1.4, 1.5, 1.6)
  - InterRim Ballistic Missile (declares 1.6)
  - Ion Weaponry (Continued) (declares 1.6)
  - Launch Sites Expire (declares 1.6)
  - Medical System Expansion 2 (declares 1.2, 1.3, 1.4, 1.5, 1.6)
  - Medicines+ (declares 1.6, 1.5, 1.4)
  - Medieval Signs (declares 1.5, 1.6)
  - Mid-saver Saver (declares 1.4, 1.5, 1.6)
  - More Quests in Space (declares 1.6)
  - MutatorWorldIcons (declares 1.6)
  - Nice-o-sphere (declares 1.6)
  - Not My Fault (declares 1.3, 1.4, 1.5, 1.6)
  - Orca Shuttle (declares 1.6)
  - Pandora:Frontier ReMake (declares 1.5, 1.6)
  - Pawn Suppression Implants (declares 1.3, 1.4, 1.5, 1.6)
  - Permits: Re-Examined (declares 1.6)
  - Pick Up And Haul (declares 1.0, 1.1, 1.2, 1.3, 1.4, 1.5, 1.6)
  - Please use the pews ! (Ritual Seats Range Fix) (declares 1.6)
  - Power Utility Tab (declares 1.6)
  - Rails and Roads of the Rim (Continued) (declares 1.3, 1.4, 1.5, 1.6)
  - Real Ruins (declares 1.0, 1.1, 1.2, 1.3, 1.4, 1.5, 1.6)
  - RimHUD (declares 1.1, 1.2, 1.3, 1.4, 1.5, 1.6)
  - ScrubDaddy's Hairstyles (declares 1.6)
  - Simple sidearms (declares 1.0, 1.1, 1.2, 1.3, 1.4, 1.5, 1.6)
  - Smart Farming (declares 1.3, 1.4, 1.5, 1.6)
  - Spaceship Solar Hull (declares 1.6)
  - Spaceship Turret Hull (declares 1.6)
  - Tameable Entities (declares 1.6)
  - Tavern (declares 1.6)
  - Tech Level Progression (declares 1.3, 1.4, 1.5, 1.6)
  - Techwear (declares 1.6)
  - True Non-Senescence (declares 1.6)
  - Tunnelers ARE deep drills (declares 1.6)
  - Turn It On and Off - RePowered (declares 1.1, 1.2, 1.3, 1.4, 1.5, 1.6)
  - Ultratec Passenger Shuttle (declares 1.6)
  - Ushankas Glittertech Expansion (declares 1.5, 1.6)
  - Ushankas Luciferium Expansion (declares 1.3, 1.4, 1.5, 1.6)
  - Ushankas Necroa Archovirus (declares 1.6)
  - Vanilla Genes Rebalanced (declares 1.4, 1.5, 1.6)
  - Vanilla Hair Retextured (declares 1.3, 1.4, 1.5, 1.6)
  - Vanilla Vehicles Expanded (declares 1.4, 1.5, 1.6)
  - Visible Wealth (declares 1.5, 1.6)
  - VTOL Vehicle (declares 1.6)
  - Warehouse Storage (declares 1.4, 1.5, 1.6)
  - Who shot my leg off? (declares 1.5, 1.6)
  - \[CF\] Anomaly Upscaled (declares 1.5, 1.6)
  - \[kyzy\] Bio Ranch (declares 1.6)
  - Alpha Genes Integrated (declares 1.6)
  - Deep Pills (declares 1.6)
  - Frontline Workshop (declares 1.6)
  - Improved Map Search (declares 1.6)
  - IRBM Vanilla Retexture (declares 1.6)
  - Munitions Industries for VFE - Factory (declares 1.6)
  - Pick Up And Haul - Shared Storage Access (declares 1.6)
  - Research Papers Tech Levels (Retexture) (declares 1.6)
  - Scrubdaddy's Bodies (declares 1.6)
  - Simple Cape and Hood Retexture (declares 1.6)
  - Taste of Vanilla - Bean Turrets (declares 1.5, 1.6)
  - Tilled Soil (declares 1.0, 1.1, 1.2, 1.3, 1.4, 1.5, 1.6)
  - Trains of the Rim (declares 1.6)
  - Ushankas Biological Warfare (declares 1.4, 1.5, 1.6)
  - Vanilla Nutrient Paste Expanded: Reimagined Progression (declares 1.5, 1.6)
  - Vanilla Outposts Expanded: Additional Outposts (declares 1.3, 1.4, 1.5, 1.6)
  - Vanilla Outposts Expanded: Prisoner Patch (declares 1.3, 1.4, 1.5, 1.6)
  - Vanilla Vehicles Expanded - Tier 3 (declares 1.4, 1.5, 1.6)
  - VFE Factories: Buffs and tweaks (declares 1.6)
  - \[kyzy\] Ship Wall Enhanced (declares 1.6)
  - Automatic Hydroponics (declares 1.6)
  - Campfire Stews (declares 1.5, 1.6)
  - Hospitality + Ideology Patch (declares 1.6)
  - Prison Labor (declares 1.0, 1.1, 1.2, 1.3, 1.4, 1.5, 1.6)
  - Vanilla Outposts Expanded: Power Grid (declares 1.3, 1.4, 1.5, 1.6)
  - VNPE RP Retexture (declares 1.5, 1.6)
  - VVE - Drop Pod Orbital Upgrade (declares 1.6)
  - Prisoner Realism (declares 1.6)
  - RimMarket — Hospitality Compatibility Patch (declares 1.6)
- **Against a mod's own load-order rule:** none.
- **Marked incompatible with each other:** none.
- **Suggested mods you already have (on, or subscribed but off): 7**
  - [Vanilla Weapons Expanded - Coilguns](https://steamcommunity.com/sharedfiles/filedetails/?id=2375692535): **on**
  - [Vanilla Trading Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2785616901): **subscribed, off**
  - [Vanilla Plants Expanded - More Plants](https://steamcommunity.com/sharedfiles/filedetails/?id=2748889667): **on**
  - [Vanilla Furniture Expanded - Props and Decor](https://steamcommunity.com/sharedfiles/filedetails/?id=2102143149): **on**
  - [Quiet Wonders](https://steamcommunity.com/sharedfiles/filedetails/?id=3822357701): **on**
  - [Smooth Terrain](https://steamcommunity.com/sharedfiles/filedetails/?id=3502765685): **on**
  - [RimShips](https://steamcommunity.com/sharedfiles/filedetails/?id=3689132119): **subscribed, off**
<!-- END GENERATED: checks -->

## Suggestions

<!-- BEGIN SUGGESTIONS -->
Agent picks from 22 Oct 2026, made for this list: a new Combat Extended colony with all five DLCs, 52 Vanilla
Expanded mods already on, gravships, vehicles and exploration. Facts come from each mod's Workshop page and,
since the owner spotted bug reports there, from **the latest 50 player comments on each page** (read
22 Oct 2026). Where a note is my own reasoning it says so. **None of these is tested in your game.** "CE patch:
yes" means Combat Extended ships its own patch for the mod (read from your CE install's `ModPatches` folder);
"n/a" means the mod has no combat content.

**My honest advice: add five to ten of these, not fifty.** Vanilla Expanded is the best-kept-up series on the
Workshop (113 of the 116 collection mods were updated between 14 and 21 Oct 2026). But an update isn't a fix:
several popular VE mods carry bug reports that are months old. You already run about 290 mods, and the VE team
itself says not to enable everything. **Read a mod's newest comments before you subscribe.**

**Every suggestion below is comment-checked** (the latest 50 comments on each page, 22 Oct 2026; the shoreline
picks on 25 Oct 2026). Where a row says nothing about comments, they were quiet.

### Start here

The picks whose comments were quiet, or whose author fixed the reported problems. **Owner's choice
(22 Oct 2026): none for now.** Vanilla Events Expanded was added, then removed the same day as too likely to
cause trouble (the psychic bloom reports below); no save was made with it on.

| Mod | What it adds | CE patch | Notes |
| --- | --- | --- | --- |
| [Vanilla Events Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=1938420742) | More, and more varied, events in the vanilla style | yes | **Switch off psychic bloom in its settings** (Options, Mod settings, Vanilla Events Expanded). Players report psychic lotuses that can't be cut or removed (Sep and Oct 2026), and one says removing the mod afterwards broke the save. Its own files allow the bloom to become a permanent world condition. If one starts anyway, the same settings page has a "Stop world condition..." button (a game must be loaded); whether that also clears lotuses already grown, its files don't say. The author says every event can be switched off, and answers in the comments (18 Oct 2026). Needs Harmony and VE Framework, which you have. |
| [Vanilla Apparel Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=1814987817) | 40 new pieces of everyday clothing | yes | You have its Accessories module but not the main mod. Its comments are quiet: two passing complaints in 50, from Sep 2025 to Oct 2026. |
| [Vanilla Aspirations Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3268401022) | Sims-style aspirations; finishing them all triggers a growth moment | n/a | Takes the place of Social Interactions (below) for deeper colonists. A March 2026 update threw errors on old saves; the author shipped a fix within a day, and the comments to Sep 2026 report nothing since. One player says it runs flawlessly on new worlds. |

### Wait: players report problems the author hasn't fixed

The first five were my first-draft "Start here" picks; their comments changed my mind. The rest turned up in
the full comment check.

- **[Vanilla Social Interactions Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2439736083): wait.**
  Its last real fix was on 3 Oct 2025 ("Fixed numerous bugs and improved performance"); the 14 Oct 2026 update
  is an automatic one. Since then players report a crash when it gives a pawn a trait (Aug 2026), pawns stuck in
  endless venting or confiding (Jul 2026) or "conversing with group" for hours (May 2026), alert spam with no
  setting to stop it (Sep 2026), and one save that broke when the mod was removed (Dec 2025). One player reports
  "almost no issue" (Feb 2026), so it isn't broken for everyone.
- **[Vanilla Traits Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2296404655): wait.**
  Wanderlust throws a load error (Jan 2026), ignores gravships (Jun 2026) and gives its penalty the moment a
  caravan returns (Oct 2026). Anxious pawns' breakdowns never end (Jan and Oct 2026). Prodigy raises the
  breakdown threshold instead of lowering it (Jun and Jul 2026). Two players traced raids that error and leave the
  map to it (Sep 2025, Sep 2026). Single traits can be switched off; the raid reports aren't tied to one trait.
- **[Vanilla Furniture Expanded - Security](https://steamcommunity.com/sharedfiles/filedetails/?id=1845154007):
  wait, with your setup.** Under CE its Tesla turret won't fire at mechanoids (21 Oct 2026), its anti-air turret
  doesn't stop CE mortar shells (Aug 2026), and its repulsor shield broke after CE's 15 Oct update. A minified
  bear trap carried aboard throws errors and stops a gravship landing (Sep 2026). Its trenches log errors
  (Sep 2026).
- **[Vanilla Landmarks Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3656316229): wait.**
  One player lost two late-game runs to corrupted terrain generation (19 Oct 2026); others see solid snow in place
  of rock (Jul 2026) and clashes with other world-generation mods. My view: you already run Planetsmith,
  Worldbuilder and Smart Odyssey, so this would be a fourth mod shaping the same world.
- **[Vanilla Achievements Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2288125657): taste
  only, and never with Cryptoforge.** Not in the VE collection, but by the VE team. Together with the
  Cryptoforge quest mod, both hook experience gain and throw an error every time a pawn learns (Jul 2026). Others report
  log spam (May 2026) and slow colonies after year five (Jun 2026). If you take it, **load it as high as
  possible**.
- **[Vanilla Ideology Expanded - Splits and Schisms](https://steamcommunity.com/sharedfiles/filedetails/?id=2573138902): wait.** "Still 100% broken", says one
  player (Dec 2025). After a schism, the old ideoligion's rituals vanish or demand the new one's altars (Jun and
  Dec 2025), and things on the map switch to the new ideoligion (Jun 2026). It is also untested with your
  Enhanced Ideology.
- **[Vanilla Storytellers Expanded - Winston Waves](https://steamcommunity.com/sharedfiles/filedetails/?id=3215569151): wait, with CE.** A Sep 2026 update made it
  reference the toxbomb launcher, which CE removes, so it errors at start-up; players say it no longer works with
  CE. Its growing waves can also cause lag (Oct 2026).
- **[Common Sense](https://steamcommunity.com/sharedfiles/filedetails/?id=1561769193): wait.** "Way too many issues and conflicts since 1.6" (Jul 2026); another
  player saw pawns doing baffling things or getting stuck (Aug 2026).
- **[Better Pawn Control](https://steamcommunity.com/sharedfiles/filedetails/?id=1541460369): wait, with gravships.** Travelling by gravship resets its policies and
  schedules and breaks the emergency button (Jun to Oct 2026), and banishing pawns fails with it on (Jun and
  Sep 2026). My first draft said its 1.6 update handled gravship moves; the comments say it doesn't yet.

### More content, by taste

**Factions and quest chains.** All have a CE patch, and all can be added mid-save. Comment notes are from
22 Oct 2026.

| Mod | What it adds | Notes |
| --- | --- | --- |
| [Vanilla Factions Expanded - Insectoids 2](https://steamcommunity.com/sharedfiles/filedetails/?id=3309003431) | Insectoid bosses summoned with thumpers; your own hive to run, breed and harvest | Can't replace Insectoids 1 in a save that had it. Most recent complaints are about RimWorld 1.7, not 1.6. |
| [Vanilla Factions Expanded - Settlers](https://steamcommunity.com/sharedfiles/filedetails/?id=2052918119) | A Western-film frontier faction | Mid-save, VE Framework's faction discovery spawns their settlements. Comments: "gatling gun is broken" (Sep 2026). |
| [Vanilla Quests Expanded - Ancients](https://steamcommunity.com/sharedfiles/filedetails/?id=3618306875) | Sealed supersoldier labs and archite gene machinery; a start scenario in an underground lab | First quest 90 to 120 days in. Comments: the underground vault sometimes fails to generate (Aug and Sep 2026). |
| [Vanilla Quests Expanded - Cryptoforge](https://steamcommunity.com/sharedfiles/filedetails/?id=3461526070) | A northern trek to a crashed ship of crypto-tech "space vikings" and its forge | One-time crafting chance at the end: bring your best crafters. **Cryptofreeze**, its spreading ice, is the top complaint: it spreads without limit and can't be put out on a creature (Sep and Oct 2026). **Never pair it with Vanilla Achievements Expanded** (errors on every experience gain, Jul 2026). |
| [Vanilla Quests Expanded - Deadlife](https://steamcommunity.com/sharedfiles/filedetails/?id=3497226454) | Ancient supply silos guarded by shambler hordes | Needs stealthy caravans. Anomaly flavour. Praised as the one that "made RimWorld scary"; one player's silo quests wouldn't complete (Apr 2026). |
| [Vanilla Quests Expanded - Drone Factory](https://steamcommunity.com/sharedfiles/filedetails/?id=3733951755) | Rogue war drones, scrapyards, and a transmitter to silence | Colony drones refund their full cost when taken apart. Comments: drones can't open doors (Aug 2026). |
| [Vanilla Quests Expanded - The Generator](https://steamcommunity.com/sharedfiles/filedetails/?id=3411401573) | ARC, an upgradable reactor that can power a whole colony | My view: you already have Rimatomics, Rimefeller and VFE Power, so take it for the quests, not the power. Comments: a quest site can spawn across water, out of reach (Oct 2026). |

**Xenotypes (Vanilla Races Expanded).** All need Biotech. All can be added mid-save, but their pages warn that
xenotype mods can't be removed safely.

| Mod | What it adds | CE patch | Notes |
| --- | --- | --- | --- |
| [Android](https://steamcommunity.com/sharedfiles/filedetails/?id=2975771801) | Buildable androids with fixed hardware genes and programmable subroutines | yes | **Androids resist vacuum**, which suits your gravships. Comments are quiet: only small things, like a creation station that can't be rotated (Sep 2026). |
| [Starjack](https://steamcommunity.com/sharedfiles/filedetails/?id=3531912428) | Odyssey's Starjacks as a space species, with astrogenes that only work in space | — | Needs Odyssey. Made for gravship play. Comments: red errors that repeat when a pawn with oxygen sensitivity wears gear above 80% vacuum resistance (Apr and Jun 2026). |
| [Insector](https://steamcommunity.com/sharedfiles/filedetails/?id=3260509684) | An insect-linked xenotype with geneline evolution | yes | Same theme as Insectoids 2. One report of a crash when starting a colony (Aug 2026). |
| [Phytokin](https://steamcommunity.com/sharedfiles/filedetails/?id=2927323805) | Plant people, with a quest to rescue one | yes | |
| [Fungoid](https://steamcommunity.com/sharedfiles/filedetails/?id=3042690053) | Fungus people | yes | |
| [Lycanthrope](https://steamcommunity.com/sharedfiles/filedetails/?id=3114453100) | Werewolves | yes | Comments: its crashed-shuttle event can leave the quest unresolved (Oct 2026). |
| [Archon](https://steamcommunity.com/sharedfiles/filedetails/?id=3067715093) | A Githyanki-inspired xenotype; transcendent Archons can come back after death | yes | Also needs Royalty. Comments are quiet; one player met an Archon raid on day 17. |

**Colonists' inner lives.** No combat content. Vanilla Aspirations Expanded is in Start here; Social Interactions
and Traits are under Wait.

| Mod | What it adds | Notes |
| --- | --- | --- |
| [Vanilla Backstories Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2861806869) | Many more backstories | Not the same as Vanilla Backgrounds Expanded, which you have. Comments quiet since early 2025. |
| [Vanilla Books Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2193152410) | Writing benches: colonists write their own books | Vanilla books can't be written by players; this adds that. Comments: errors when children read newspapers (Aug 2026), and one player stuck on the scenario screen (Sep 2026). |
| [Vanilla Psycasts Expanded - Puppeteer](https://steamcommunity.com/sharedfiles/filedetails/?id=3033779606) | A rare psycast path: brain leeches, puppets, and moving your mind into a puppet | Imperial backstories only, or via a neurotrainer. **Don't fly a gravship with pawns under subjugation** (Aug 2025 report). Making a child into a puppet throws errors (Feb 2026). |
| [Vanilla Anomaly Expanded - Insanity](https://steamcommunity.com/sharedfiles/filedetails/?id=3342412435) | A sanity meter; low sanity leads to inhumanisation | Needs Anomaly. One player's verdict: it adds a sanity system, not new anomalies (Jul 2026). |

**Food.** You now have Vanilla Cooking Expanded and Vanilla Fishing Expanded switched on.

| Mod | What it adds | Notes |
| --- | --- | --- |
| [Vanilla Cooking Expanded - Sushi](https://steamcommunity.com/sharedfiles/filedetails/?id=2158539170) | Sushi from rice and Vanilla Fishing Expanded's fish | Pairs with the two you already have. Comments quiet. |
| [Vanilla Fishing Expanded - Fishing Treasures AddOn](https://steamcommunity.com/sharedfiles/filedetails/?id=2468543398) | Base-game and modded items in the fishing treasure pool | Small add-on. With Vanilla Weapons Expanded, its patch points at removed flintlocks and logs a start-up error (Feb 2026). |
| [Vanilla Cooking Expanded - Bakery](https://steamcommunity.com/sharedfiles/filedetails/?id=3590516685) | Cookies, cupcakes and doughnuts that help colonists let go of their worst memories | Not in the VE collection, but by the VE team. Configurable. One report of pawns getting stuck after a Luxury Cake bill was added (Nov 2025). |
| [Vanilla Cooking Expanded - Haute](https://steamcommunity.com/sharedfiles/filedetails/?id=3455694362) | Meals above gourmet, from an electric haute section | Needs two different vegetables and two different condiments per meal. **One player's gravship couldn't land with Haute meals stored aboard** (Feb 2026; the author suspects a mod conflict). |
| [Vanilla Food Variety Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3334272487) | A mood bonus for varied meals | Lightweight; how much variety counts is a setting. Comments: favourite foods lean heavily towards eggs (Jul 2026). |
| [Vanilla Cooking Expanded - Stews](https://steamcommunity.com/sharedfiles/filedetails/?id=2134312965) | Slow-cooked medieval stews | My view: skip it while you keep Campfire Stews, which adds stews too. Comments: a known, low-priority sprite bug (2026). |

**World, base and industry.**

| Mod | What it adds | CE patch | Notes |
| --- | --- | --- | --- |
| [Vanilla Helixien Gas Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2877699803) | Drill for gas, pipe it, and run stoves, smithies, lamps and heaters that ignore solar flares | n/a | The chemist's pick. It runs on VE Framework's pipe system, the same one our radio framework's feedline uses. Comments: a start-up red error (Nov 2025), and gas buildings missing from one Odyssey game (Aug 2026). |
| [Vanilla Furniture Expanded - Spacer Module](https://steamcommunity.com/sharedfiles/filedetails/?id=2028381079) | A 2025 rebuild of late-game furniture: sleek beds, wall TVs, air purifiers | — | Extra pieces with Ideology. Its page says it is save-compatible. Comments: wall-mounted TVs face the wrong way or don't work (Sep and Oct 2026). |
| [Vanilla Genetics Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2801160906) | Animal hybrids from a genetics lab | yes | Mid-save, accept the hostile hybrids faction when VE Framework asks. Comments: an electrowomb bug still unfixed (Sep 2026). Add-on: [More Lab Stuff](https://steamcommunity.com/sharedfiles/filedetails/?id=2810324444), whose comments are mostly old genome-trainer errors (2024). |
| [Vanilla Ideology Expanded - Icons and Symbols](https://steamcommunity.com/sharedfiles/filedetails/?id=2552609458) | Many more ideoligion icons | n/a | More choice for a House D'thorga banner. Comments quiet. |
| [Vanilla Ideology Expanded - Hats and Rags](https://steamcommunity.com/sharedfiles/filedetails/?id=2567387768) | Ideology apparel | yes | Comments quiet. |
| [Vanilla Ideology Expanded - Relics and Artifacts](https://steamcommunity.com/sharedfiles/filedetails/?id=2564895018) | More relics | n/a | Comments: nameless books that appear and vanish (Aug 2025). |

**Animals.** All four have a CE patch.

| Mod | What it adds | Notes |
| --- | --- | --- |
| [Vanilla Animals Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2871933948) | Nearly every earlier VE animal module in one mod | Switch off any animal in its settings. With Odyssey, its duplicates of Odyssey animals are hidden. Comments: an error when a crossbred animal's father is gone (Jul 2026); otherwise quiet. |
| [Vanilla Animals Expanded - Endangered](https://steamcommunity.com/sharedfiles/filedetails/?id=2366589898) | Endangered real-world species | Comments: "release to repopulate" couldn't be ordered on 1.6 (Jul 2025). |
| [Vanilla Animals Expanded - Royal Animals](https://steamcommunity.com/sharedfiles/filedetails/?id=2858079457) | Animals tied to Royalty's mechanics | Needs Royalty. Comments: royal pheasants that won't leave the map and loop errors (Sep and Oct 2025). |
| [Vanilla Animals Expanded - Waste Animals](https://steamcommunity.com/sharedfiles/filedetails/?id=2962126499) | Creatures of polluted biomes | Needs Biotech. Comments quiet. |

**Equipment.** CE patches all of these. My view: with CE Guns, CE Armory, Dead Man's Switch, Modular Weapons 2,
Ion Weaponry and four VE faction mods, you are not short of guns. Add these only for the vanilla look.

| Mod | What it adds | Notes |
| --- | --- | --- |
| [Vanilla Weapons Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=1814383360) | The core set of vanilla-style guns, with Odyssey unique-weapon traits | Comments: unique weapons showing doubled traits (Oct 2026); the author says cryptofreeze ammo is being redesigned (21 Oct 2026). |
| [Vanilla Armour Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=1814988282) | More armour on the road from flak to power armour, plus riot gear and a riot shield | **The Siegebreaker helmet has no vacuum resistance** though its armour does (Sep 2025 to Apr 2026), awkward with VGE's vacuum checkpoint. |
| [Vanilla Weapons Expanded - Grenades](https://steamcommunity.com/sharedfiles/filedetails/?id=2194472657) | Grenade belts worn as utility items, refilled after use | **In Jul 2025 players reported it crashing the game with CE's 1.6 build.** CE now ships a patch for it and nobody has reported the crash since, but read its newest comments first. |
| [Vanilla Weapons Expanded - Frontier](https://steamcommunity.com/sharedfiles/filedetails/?id=2454918139) | Western-style guns, made so VFE Settlers' raiders aren't all revolvers | Comments quiet since 2024. |
| Also: [Heavy Weapons](https://steamcommunity.com/sharedfiles/filedetails/?id=2329126791), [Laser](https://steamcommunity.com/sharedfiles/filedetails/?id=1989352844), [Coilguns](https://steamcommunity.com/sharedfiles/filedetails/?id=2375692535), [Quickdraw](https://steamcommunity.com/sharedfiles/filedetails/?id=1906780517), [Non-Lethal](https://steamcommunity.com/sharedfiles/filedetails/?id=2454918354), [Tribal](https://steamcommunity.com/sharedfiles/filedetails/?id=2454918552), [Makeshift](https://steamcommunity.com/sharedfiles/filedetails/?id=2419690698) | Smaller themed sets | Not comment-checked one by one. |

**Storyteller.** Winston Waves is under Wait. Perry Persistent has a CE patch, and you can switch storyteller
mid-save.

| Mod | What it adds | Notes |
| --- | --- | --- |
| [Perry Persistent](https://steamcommunity.com/sharedfiles/filedetails/?id=2149702069) | A storyteller running an investigation that closes in on you | Comments quiet; one player found few quests (Jun 2025). |

### Skip, or wait

- **[Vanilla Base Generation Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3209927822): skip.**
  My view: you run Custom Base Framework, which already rebuilds faction settlements, so the two would compete
  for the same job.
- **[Vanilla Factions Expanded - Mechanoids](https://steamcommunity.com/sharedfiles/filedetails/?id=2329011599):
  wait.** Its page says it won't be updated for 1.6 because a remake is coming.
- **[Vanilla Hair Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=1888705256): taste only.**
  You already have ScrubDaddy's hairstyles and a vanilla hair retexture.
- **[Vanilla Trading Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2785616901): already
  subscribed, but switched off.** Market prices move with supply and demand, plus contracts and a stock market.
  If you switched it off on purpose, ignore this.
- **[Dubs Mint Menus - VFE Factory Compatibility](https://steamcommunity.com/sharedfiles/filedetails/?id=3779444191):
  skip (subscribed, switched off).** You tried it before 24 Oct 2026 and it didn't work well. Your game log from
  that session shows no errors from it, so it loaded. From its own source code: it leaves the factory's
  **Add process** button exactly as it was, and adds a separate Mint panel whose entries carry no ingredients.
  It was built for an older Mint Menus (1.3.1247; you have 1.3.1695), has had one upload (Aug 2026) and two
  comments. Searchable Menus, below, covers the same need.

### Beyond Vanilla Expanded

All declare RimWorld 1.6 (checked on Steam, 22 Oct 2026). Common Sense and Better Pawn Control are under Wait.

| Mod | What it adds | Notes |
| --- | --- | --- |
| [Performance Esmolas](https://steamcommunity.com/sharedfiles/filedetails/?id=3676343434) | Small, targeted performance patches | The 18 Oct triage's pick, still not added. Same author as your Slower Pawn Tick Rate. Declares 1.7 too. **One report that it stops autosaving** (21 Oct 2026, no error shown): if you add it, check that autosaves still appear. |
| [Achtung!](https://steamcommunity.com/sharedfiles/filedetails/?id=730936602) | Force work, order several pawns at once, move drafted pawns in formations | A staple for raids and emergencies. Its only recent complaint is about 1.7 (22 Oct 2026), not your 1.6. |
| [Cleaning Priority (Continued)](https://steamcommunity.com/sharedfiles/filedetails/?id=2018316486) | Ranked cleaning areas: hospital and kitchen first, the rest after | For your slob pawns. **With Dubs Bad Hygiene, switch off "prefer indoor cleaning" in Dubs' settings**, or pawns clean outside the chosen areas (its own page). Comments: removing it can glitch the UI if you made several areas (Dec 2025); cleansweeper mechs ignore its list. Updated Jul 2026. |
| [Camera+](https://steamcommunity.com/sharedfiles/filedetails/?id=867467808) | Further zoom both ways, smoother movement, and markers for pawns when zoomed far out | Comments quiet since 2021. |
| [Numbers](https://steamcommunity.com/sharedfiles/filedetails/?id=1414302321) | One customisable overview table of stats, skills, needs and gear, for colonists, prisoners and animals | Comments: clashes with other UI mods and the wildlife tab (Oct 2026), and a big FPS drop while its tab is open (Jan 2026). |
| [Searchable Menus](https://steamcommunity.com/sharedfiles/filedetails/?id=2928608119) | A search box in every long pop-up menu (15 or more entries, adjustable), including VFE Factory's **Add process** list | Added 24 Oct 2026 for your factory menus; comments read that day. Needs Float Sub-Menus, which you have. **Keep Float Sub-Menus above Nice Bill Tab and Useful Marks** (yours already is): both carry their own copy of its code, Nice Bill Tab's copy is older, and the game uses the first copy it finds (the author and players, Jul and Oct 2025). Load it after Character Editor (one report of menus vanishing with both, Jan 2026). Keep the mouse over the menu while you type. Its page says it is safe to add or remove at any time. Last updated Jul 2025, marked for 1.6. |

### Shorelines and water plants

The owner asked on 25 Oct 2026 for mods that make shorelines look better, with lilies and the like. Agent
picks; the latest comments on each page were read that day (up to 50, fewer where a page has fewer). **None of
these is tested in your game.**

**Owner's verdict (25 Oct 2026): Quiet Wonders gives "pretty much what I want"**, and it "looks amazing". The
picks below are extras for later, not needed for the wish.

**You already have more than you might think.** From the game's own files and your installed mods:

- **Odyssey grows four wild water plants.** Reeds grow on mud, marsh and shallow fresh water, rivers included,
  in nearly every biome. Bulrush and lily pads grow only on still shallow fresh water (lakes and ponds) and
  marsh: bulrush in boreal forest, tundra, cold bog, temperate forest and swamp; lily pads in temperate forest
  and swamp, cold bog, grasslands, tropical rainforest and tropical swamp. Lotus (beauty 14) grows only in
  tropical rainforest and tropical swamp. In the biomes' plant lists, lily pads carry a fifth of the reeds'
  weight (a tenth in the tropics), so they stay sparse. **Nothing grows in sea water**, and nothing but reeds
  in rivers.
- **[Vanilla Plants Expanded - More Plants](https://steamcommunity.com/sharedfiles/filedetails/?id=2748889667)
  (on) lets you plant a pond.** Its aquatic growing zone goes on shallow water, shallow and chest-deep moving
  water and marsh, and grows lotus, taro, watercress and water chestnut.
- **[Vanilla Furniture Expanded - Props and Decor](https://steamcommunity.com/sharedfiles/filedetails/?id=2102143149)
  (on) has fake lily pads and reeds** among its props: decoration that never grows or dies, paid for in silver.
  My reading of its files: they have no water rule, so they may only go on dry ground or bridges. Not checked
  in game.
- **[Quiet Wonders](https://steamcommunity.com/sharedfiles/filedetails/?id=3822357701) (switched on
  25 Oct 2026) already does most of what a shoreline-effects mod would.** It has dawn mist over water, surf
  foam on sea shores, footprints on sand, mud and riverbanks, and dragonflies, frogs and leaping fish by fresh
  water. It also has fireflies by water, a moonlit path on open water, leaves drifting downriver and rain rings
  on water, and each effect has its own switch. **Check its cost with Dubs Performance Analyzer**, which you
  have: one player measured 0.52 to 1.70 ms on average (21 Oct 2026) and another saw a large slowdown
  (23 Oct 2026). The author measured no difference in a 374-mod save, and several players saw none. Other
  reports: the cloud shadows make the screen flash white for some players (the author fixed this for 1.7 only;
  on 1.6, **switch off "Cloud shadows"** if it happens), marsh bubbles also appear on mud, and one player saw
  water dripping indoors under a mountain roof (21 to 23 Oct 2026). It is new: the first comments are from
  21 Oct 2026, and the author has shipped several fixes since.
- **[Smooth Terrain](https://steamcommunity.com/sharedfiles/filedetails/?id=3502765685) (on)** already
  smooths the blocky edges between terrain tiles, shores included.

**Picks**, all comment-checked:

| Mod | What it adds | Notes |
| --- | --- | --- |
| [TP Sea Plants](https://steamcommunity.com/sharedfiles/filedetails/?id=3643012859) | Wild seaweed in shallow sea water (harvest for food), mangrove trees (wood) and corals that grow very slowly and yield a stony material | The gap Odyssey left: its author made it because Odyssey has no sea plants. **Needs Odyssey** (the author, Jan 2026). XML only. Comments (34 in all): one player found no plants spawning at all with Biomes! Core (Mar 2026; the author couldn't reproduce it, and you don't run Biomes! Core); corals sit on top of the water rather than under it; they can't be farmed in a zone. A player asked for a non-AI version of the art and the author invited them to draw one, so the art is probably AI-assisted. Updated Mar 2026. |
| [More Birds - Shorebirds](https://steamcommunity.com/sharedfiles/filedetails/?id=3763588872) | Eight shore and wetland birds: cormorant, egret, ibis, pelican, spoonbill, sandpiper, plover and seagull, plus feather apparel | Life on the shore rather than plants. Comments (26) are all praise, no bug reports (Jul to Sep 2026); one player cheered the Australian white ibis. **The author says it isn't CE compatible**, and your CE install has no patch for it. CE's race autopatcher "will apply a preset to unpatched races to allow basic functionality" (its settings page, Autopatcher section), so the birds should work if that setting is on, but how they fight under CE is untested. Updated Jul 2026. |
| [Choose Wild Plant Spawns](https://steamcommunity.com/sharedfiles/filedetails/?id=2564446762) | A slider per plant per biome for how often wild plants spawn, so you can have more lily pads, bulrush and lotus | Mlie's. **It works on an existing save, over time**, as new wild plants spawn (Mlie, 24 Oct 2026). Its page says it is safe to add and remove. **Change water plants in small steps**: plants tied to one terrain, such as water plants, ignore fertility and can flood the map (a player, Aug 2025); add Better Sliders for exact values (its page). A biome you don't change is left alone (Mlie, Jul 2025). Unsolved reports without logs: a start-up error (Oct 2026), and a settings page that showed two dead sliders and wouldn't close (Sep 2026). Updated Jul 2025. |
| [LiquidSFX](https://steamcommunity.com/sharedfiles/filedetails/?id=3758773902) | Sound, not graphics: ambience for rivers, lakes, marshes, coasts, hot springs and lava, which stops when water freezes | Comments (22) are clean. The author reports a very low cost and ships a Dubs Analyzer file to check it (Jul and Aug 2026). Each sound can be switched off. Quiet Wonders has its own quiet surf and marsh sounds: if they double up, switch one off. Updated Oct 2026; 1.7 coming. |
| [Eccentric Extras - Beach Stuff](https://steamcommunity.com/sharedfiles/filedetails/?id=3066838193) | Just for fun: beach chairs, paintable beach umbrellas and a beach ball | For a beach colony. Comments quiet; pawns rarely play with the beach ball, since it competes with horseshoes for the same kind of fun (the author, Apr 2026). Safe to add mid-save (the author, Sep 2026). |

**Skip, or wait** (comment-checked unless it says otherwise):

- **[[LTS]Biosphere](https://steamcommunity.com/sharedfiles/filedetails/?id=3217394931): skip.** The only
  other mod with water lilies and cattails, but last updated 15 Jun 2025, before 1.6 was out. Since then players
  report red config errors (Oct 2025), aquatic zones that pawns never sow (Jul 2025), world-generation trouble
  with large mod lists (Jul 2026) and a plant missing a reference (Aug 2026). "Are you still alive?" went
  unanswered (Feb 2026). A separate [Biosphere Patches](https://steamcommunity.com/sharedfiles/filedetails/?id=3699542111)
  fixes some of it. Odyssey's lily pads already cover the main wish.
- **[ReGrowth 2](https://steamcommunity.com/sharedfiles/filedetails/?id=2260097569): skip for this.** It
  overhauls every biome rather than adding to shores. Its author says the page doesn't describe the 1.6
  version, so what it does to shores isn't documented. **It can't be removed from a save** (its FAQ). On
  19 Oct 2026 the author deleted its Aspen module by mistake, and some players' saves broke. Quiet Wonders'
  author says the two overlap (cold breath, falling leaves).
- **[Ambient Rim](https://steamcommunity.com/sharedfiles/filedetails/?id=3158997430): skip; take LiquidSFX.**
  Seagulls and wave sounds, but since 1.6 it has made music stutter. The author fixed it in Jul 2026, yet
  reports continue. Its sound switches throw errors (Aug 2026), and some players hear no river or sea at all
  (Aug 2026).
- **[Geological Landforms](https://steamcommunity.com/sharedfiles/filedetails/?id=2773943594): skip.** It
  shapes coasts and lakes, not their look. **With your Worldbuilder**, its landforms can't be edited (Aug 2026),
  and Worldbuilder warns of a patch that interferes with it (May 2026). Players also report map-generation
  errors (Jun 2026) and a crash on loading (Oct 2026). Some of its landforms switch off with Odyssey (the
  author).
- **[Painterly Rim](https://steamcommunity.com/sharedfiles/filedetails/?id=3801937839): taste only.** It
  repaints the whole natural world in oils, lily pads, reeds and lotus included. It overlaps your Vanilla
  Textures Expanded, and the later one in the load order wins. One player got a sea of red errors adding it
  mid-save (Sep 2026); another says rivers look still, with lines where shallow meets deep (Sep 2026). Its
  page says AI was used for some textures.
- **[Remove fade with water](https://steamcommunity.com/sharedfiles/filedetails/?id=3753895045): only if it
  bothers you.** Built floors keep a crisp edge where they meet water. A player says it removes every soft floor
  edge, not just the water ones (Aug 2026). How it sits with your Smooth Terrain is unknown.
- **[Animated Marsh](https://steamcommunity.com/sharedfiles/filedetails/?id=3538223255): skip.** Marshes ripple
  like water, but zoomed far out with Camera+ the whole map shows one giant ripple (Aug 2025). Not updated
  since Jul 2025.
- **[RimAtmosphere](https://steamcommunity.com/sharedfiles/filedetails/?id=3812265330) and
  [Simple FX: Splashes Continued](https://steamcommunity.com/sharedfiles/filedetails/?id=3261109179): skip.**
  Quiet Wonders already draws rain splashes, rings on water and fireflies. Not comment-checked.
- **Biomes! Islands: gone.** It wasn't updated for 1.6 (TP Sea Plants' author, Jan 2026), and only its
  translations show up among 1.6 mods.

### Just for you

- **[Dragons Descent](https://steamcommunity.com/sharedfiles/filedetails/?id=2026992161): dragons to tame and
  ride.** They grow up at 30, become Elders at 700 and Ancients at 1,400. It needs Harmony and VE Framework, and
  suggests Giddy-Up 2. You have all three, so yes, you can ride one; the author fixed riding in May 2026.
  **Load it near the bottom.** It declares 1.6 (updated June 2026). **The catch for you: under CE, dragons
  can't fire their fireballs** (Apr 2026), even though CE ships a patch for the mod. Also from the comments: the
  "dragon thieves" start scenario corrupted one player's save (Jun 2026), and drafted dragons can drop out of
  draft (Jun 2026).
- **Boats: you already have them.** Vanilla Vehicles Expanded, which is on, includes boats and aircraft.
  [RimShips](https://steamcommunity.com/sharedfiles/filedetails/?id=3689132119) is subscribed but switched off.
- **MechWarrior feel: wait.** [Vehicle Framework Expanded - Classic Mechs](https://steamcommunity.com/sharedfiles/filedetails/?id=3028240095)
  stops at 1.5 (last updated Apr 2024). The VFE Mechanoids remake is the other one to watch.
<!-- END SUGGESTIONS -->

## Vanilla Expanded: the whole collection

<!-- BEGIN GENERATED: vanilla-expanded -->
Read from [the collection](https://steamcommunity.com/sharedfiles/filedetails/?id=1884025115) on 2026-10-22: 116 mods. You have **53 switched on** and 1 subscribed but switched off. **CE patch** says whether Combat Extended ships its own patch for that mod (read from CE's `ModPatches` folder); "—" means it doesn't, so its guns and armour rely on the mod's own CE support or on CE's autopatchers.

| Mod | You have it | CE patch | 1.7 | Updated |
| --- | --- | --- | --- | --- |
| [Vanilla Animals Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2871933948) | No | yes | yes | 2026-10-14 |
| [Vanilla Animals Expanded — Endangered](https://steamcommunity.com/sharedfiles/filedetails/?id=2366589898) | No | yes | yes | 2026-10-14 |
| [Vanilla Animals Expanded — Royal Animals](https://steamcommunity.com/sharedfiles/filedetails/?id=2858079457) | No | yes | yes | 2026-10-14 |
| [Vanilla Animals Expanded — Waste Animals](https://steamcommunity.com/sharedfiles/filedetails/?id=2962126499) | No | yes | yes | 2026-10-14 |
| [Vanilla Anomaly Expanded - Insanity](https://steamcommunity.com/sharedfiles/filedetails/?id=3342412435) | No | — | yes | 2026-10-14 |
| [Vanilla Apparel Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=1814987817) | No | yes | yes | 2026-10-14 |
| [Vanilla Apparel Expanded — Accessories](https://steamcommunity.com/sharedfiles/filedetails/?id=2521176396) | **On** | yes | yes | 2026-10-14 |
| [Vanilla Armour Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=1814988282) | No | yes | yes | 2026-10-14 |
| [Vanilla Aspirations Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3268401022) | No | — | yes | 2026-10-14 |
| [Vanilla Backgrounds Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2775017012) | **On** | — | yes | 2026-10-14 |
| [Vanilla Backstories Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2861806869) | No | — | yes | 2026-10-14 |
| [Vanilla Base Generation Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3209927822) | No | yes | yes | 2026-10-14 |
| [Vanilla Books Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2193152410) | No | — | yes | 2026-10-14 |
| [Vanilla Brewing Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2186560858) | **On** | yes | yes | 2026-10-14 |
| [Vanilla Brewing Expanded - Coffees and Teas](https://steamcommunity.com/sharedfiles/filedetails/?id=2275449762) | **On** | — | yes | 2026-10-14 |
| [Vanilla Chemfuel Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2792917473) | **On** | yes | yes | 2026-10-14 |
| [Vanilla Christmas Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3380695249) | No | — | yes | 2026-10-14 |
| [Vanilla Cooking Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2134308519) | **On** | — | yes | 2026-10-14 |
| [Vanilla Cooking Expanded - Haute](https://steamcommunity.com/sharedfiles/filedetails/?id=3455694362) | No | — | yes | 2026-10-14 |
| [Vanilla Cooking Expanded - Stews](https://steamcommunity.com/sharedfiles/filedetails/?id=2134312965) | No | — | yes | 2026-10-14 |
| [Vanilla Cooking Expanded - Sushi](https://steamcommunity.com/sharedfiles/filedetails/?id=2158539170) | No | — | yes | 2026-10-14 |
| [Vanilla Events Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=1938420742) | No | yes | yes | 2026-10-14 |
| [Vanilla Expanded Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=2023507013) | **On** | yes | yes | 2026-10-21 |
| [Vanilla Factions Expanded - Classical](https://steamcommunity.com/sharedfiles/filedetails/?id=2787850474) | **On** | yes | yes | 2026-10-14 |
| [Vanilla Factions Expanded - Deserters](https://steamcommunity.com/sharedfiles/filedetails/?id=3025493377) | **On** | yes | yes | 2026-10-14 |
| [Vanilla Factions Expanded - Empire](https://steamcommunity.com/sharedfiles/filedetails/?id=2938820380) | **On** | yes | yes | 2026-10-20 |
| [Vanilla Factions Expanded - Insectoids 2](https://steamcommunity.com/sharedfiles/filedetails/?id=3309003431) | No | yes | yes | 2026-10-17 |
| [Vanilla Factions Expanded - Mechanoids](https://steamcommunity.com/sharedfiles/filedetails/?id=2329011599) | No | yes | **no** | 2025-03-15 |
| [Vanilla Factions Expanded - Medieval 2](https://steamcommunity.com/sharedfiles/filedetails/?id=3444347874) | **On** | yes | yes | 2026-10-14 |
| [Vanilla Factions Expanded - Pirates](https://steamcommunity.com/sharedfiles/filedetails/?id=2723801948) | **On** | yes | yes | 2026-10-20 |
| [Vanilla Factions Expanded - Settlers](https://steamcommunity.com/sharedfiles/filedetails/?id=2052918119) | No | yes | yes | 2026-10-14 |
| [Vanilla Factions Expanded - Tribals](https://steamcommunity.com/sharedfiles/filedetails/?id=3079786283) | **On** | yes | yes | 2026-10-21 |
| [Vanilla Fishing Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=1914064942) | **On** | — | yes | 2026-10-14 |
| [Vanilla Fishing Expanded - Fishing Treasures AddOn](https://steamcommunity.com/sharedfiles/filedetails/?id=2468543398) | No | — | yes | 2026-10-14 |
| [Vanilla Food Variety Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3334272487) | No | — | yes | 2026-10-14 |
| [Vanilla Furniture Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=1718190143) | **On** | — | yes | 2026-10-14 |
| [Vanilla Furniture Expanded - Architect](https://steamcommunity.com/sharedfiles/filedetails/?id=2608762624) | **On** | — | yes | 2026-10-14 |
| [Vanilla Furniture Expanded - Art](https://steamcommunity.com/sharedfiles/filedetails/?id=1968134023) | **On** | — | yes | 2026-10-14 |
| [Vanilla Furniture Expanded - Factory](https://steamcommunity.com/sharedfiles/filedetails/?id=3686924415) | **On** | yes | yes | 2026-10-14 |
| [Vanilla Furniture Expanded - Farming](https://steamcommunity.com/sharedfiles/filedetails/?id=1957158779) | **On** | — | yes | 2026-10-14 |
| [Vanilla Furniture Expanded - Medical Module](https://steamcommunity.com/sharedfiles/filedetails/?id=1718191613) | **On** | — | yes | 2026-10-14 |
| [Vanilla Furniture Expanded - Power](https://steamcommunity.com/sharedfiles/filedetails/?id=2062943477) | **On** | — | yes | 2026-10-14 |
| [Vanilla Furniture Expanded - Production](https://steamcommunity.com/sharedfiles/filedetails/?id=1880253632) | **On** | — | yes | 2026-10-14 |
| [Vanilla Furniture Expanded - Props and Decor](https://steamcommunity.com/sharedfiles/filedetails/?id=2102143149) | **On** | — | yes | 2026-10-14 |
| [Vanilla Furniture Expanded - Security](https://steamcommunity.com/sharedfiles/filedetails/?id=1845154007) | No | yes | yes | 2026-10-14 |
| [Vanilla Furniture Expanded - Spacer Module](https://steamcommunity.com/sharedfiles/filedetails/?id=2028381079) | No | — | yes | 2026-10-14 |
| [Vanilla Genetics Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2801160906) | No | yes | yes | 2026-10-18 |
| [Vanilla Gravship Expanded - Chapter 1](https://steamcommunity.com/sharedfiles/filedetails/?id=3609835606) | **On** | yes | yes | 2026-10-20 |
| [Vanilla Gravship Expanded - Chapter 2](https://steamcommunity.com/sharedfiles/filedetails/?id=3799737423) | **On** | — | yes | 2026-10-20 |
| [Vanilla Hair Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=1888705256) | No | — | yes | 2026-10-14 |
| [Vanilla Helixien Gas Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2877699803) | No | — | yes | 2026-10-14 |
| [Vanilla Ideology Expanded - Anima Theme](https://steamcommunity.com/sharedfiles/filedetails/?id=2666998627) | No | — | yes | 2026-10-14 |
| [Vanilla Ideology Expanded - Dryads](https://steamcommunity.com/sharedfiles/filedetails/?id=2720631512) | **On** | yes | yes | 2026-10-14 |
| [Vanilla Ideology Expanded - Hats and Rags](https://steamcommunity.com/sharedfiles/filedetails/?id=2567387768) | No | yes | yes | 2026-10-14 |
| [Vanilla Ideology Expanded - Icons and Symbols](https://steamcommunity.com/sharedfiles/filedetails/?id=2552609458) | No | — | yes | 2026-10-14 |
| [Vanilla Ideology Expanded - Memes and Structures](https://steamcommunity.com/sharedfiles/filedetails/?id=2636329500) | **On** | yes | yes | 2026-10-14 |
| [Vanilla Ideology Expanded - Relics and Artifacts](https://steamcommunity.com/sharedfiles/filedetails/?id=2564895018) | No | — | yes | 2026-10-14 |
| [Vanilla Ideology Expanded - Sophian Style](https://steamcommunity.com/sharedfiles/filedetails/?id=3194606539) | No | — | yes | 2026-10-14 |
| [Vanilla Ideology Expanded - Splits and Schisms](https://steamcommunity.com/sharedfiles/filedetails/?id=2573138902) | No | — | yes | 2026-10-14 |
| [Vanilla Landmarks Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3656316229) | No | — | yes | 2026-10-14 |
| [Vanilla Nutrient Paste Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2920385763) | **On** | — | yes | 2026-10-14 |
| [Vanilla Outposts Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2688941031) | **On** | — | yes | 2026-10-14 |
| [Vanilla Persona Weapons Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2826922787) | **On** | yes | yes | 2026-10-14 |
| [Vanilla Plants Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2134308522) | **On** | — | yes | 2026-10-14 |
| [Vanilla Plants Expanded - Flowers](https://steamcommunity.com/sharedfiles/filedetails/?id=3748351639) | No | — | yes | 2026-10-14 |
| [Vanilla Plants Expanded - More Plants](https://steamcommunity.com/sharedfiles/filedetails/?id=2748889667) | **On** | — | yes | 2026-10-14 |
| [Vanilla Plants Expanded - Mushrooms](https://steamcommunity.com/sharedfiles/filedetails/?id=3006389281) | **On** | yes | yes | 2026-10-14 |
| [Vanilla Plants Expanded - Succulents](https://steamcommunity.com/sharedfiles/filedetails/?id=2198652536) | No | — | yes | 2026-10-14 |
| [Vanilla Psycasts Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2842502659) | **On** | yes | yes | 2026-10-15 |
| [Vanilla Psycasts Expanded - Hemosage](https://steamcommunity.com/sharedfiles/filedetails/?id=2990596478) | **On** | yes | yes | 2026-10-14 |
| [Vanilla Psycasts Expanded - Puppeteer](https://steamcommunity.com/sharedfiles/filedetails/?id=3033779606) | No | — | yes | 2026-10-14 |
| [Vanilla Quests Expanded - Ancients](https://steamcommunity.com/sharedfiles/filedetails/?id=3618306875) | No | yes | yes | 2026-10-14 |
| [Vanilla Quests Expanded - Cryptoforge](https://steamcommunity.com/sharedfiles/filedetails/?id=3461526070) | No | yes | yes | 2026-10-14 |
| [Vanilla Quests Expanded - Deadlife](https://steamcommunity.com/sharedfiles/filedetails/?id=3497226454) | No | yes | yes | 2026-10-14 |
| [Vanilla Quests Expanded - Drone Factory](https://steamcommunity.com/sharedfiles/filedetails/?id=3733951755) | No | yes | yes | 2026-10-14 |
| [Vanilla Quests Expanded - The Generator](https://steamcommunity.com/sharedfiles/filedetails/?id=3411401573) | No | yes | yes | 2026-10-14 |
| [Vanilla Races Expanded - Android](https://steamcommunity.com/sharedfiles/filedetails/?id=2975771801) | No | yes | yes | 2026-10-14 |
| [Vanilla Races Expanded - Archon](https://steamcommunity.com/sharedfiles/filedetails/?id=3067715093) | No | yes | yes | 2026-10-14 |
| [Vanilla Races Expanded - Custom Icons](https://steamcommunity.com/sharedfiles/filedetails/?id=2917311689) | No | — | yes | 2026-10-14 |
| [Vanilla Races Expanded - Fungoid](https://steamcommunity.com/sharedfiles/filedetails/?id=3042690053) | No | yes | yes | 2026-10-14 |
| [Vanilla Races Expanded - Genie](https://steamcommunity.com/sharedfiles/filedetails/?id=2901424072) | **On** | — | yes | 2026-10-14 |
| [Vanilla Races Expanded - Highmate](https://steamcommunity.com/sharedfiles/filedetails/?id=2995385834) | **On** | yes | yes | 2026-10-14 |
| [Vanilla Races Expanded - Hussar](https://steamcommunity.com/sharedfiles/filedetails/?id=2893586390) | **On** | yes | yes | 2026-10-14 |
| [Vanilla Races Expanded - Insector](https://steamcommunity.com/sharedfiles/filedetails/?id=3260509684) | No | yes | yes | 2026-10-14 |
| [Vanilla Races Expanded - Lycanthrope](https://steamcommunity.com/sharedfiles/filedetails/?id=3114453100) | No | yes | yes | 2026-10-14 |
| [Vanilla Races Expanded - Phytokin](https://steamcommunity.com/sharedfiles/filedetails/?id=2927323805) | No | yes | yes | 2026-10-14 |
| [Vanilla Races Expanded - Pigskin](https://steamcommunity.com/sharedfiles/filedetails/?id=3164798809) | **On** | yes | yes | 2026-10-14 |
| [Vanilla Races Expanded - Sanguophage](https://steamcommunity.com/sharedfiles/filedetails/?id=2963116383) | **On** | yes | yes | 2026-10-14 |
| [Vanilla Races Expanded - Saurid](https://steamcommunity.com/sharedfiles/filedetails/?id=2880990495) | **On** | yes | yes | 2026-10-14 |
| [Vanilla Races Expanded - Starjack](https://steamcommunity.com/sharedfiles/filedetails/?id=3531912428) | No | — | yes | 2026-10-14 |
| [Vanilla Races Expanded - Waster](https://steamcommunity.com/sharedfiles/filedetails/?id=2983471725) | **On** | yes | yes | 2026-10-14 |
| [Vanilla Recycling Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3155781848) | **On** | — | yes | 2026-10-14 |
| [Vanilla Skills Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3400246558) | **On** | yes | yes | 2026-10-14 |
| [Vanilla Social Interactions Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2439736083) | No | — | yes | 2026-10-14 |
| [Vanilla Storytellers Expanded - Perry Persistent](https://steamcommunity.com/sharedfiles/filedetails/?id=2149702069) | No | yes | yes | 2026-10-14 |
| [Vanilla Storytellers Expanded - Winston Waves](https://steamcommunity.com/sharedfiles/filedetails/?id=3215569151) | No | yes | yes | 2026-10-21 |
| [Vanilla Temperature Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3202046258) | **On** | — | yes | 2026-10-14 |
| [Vanilla Textures Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2016436324) | **On** | — | yes | 2026-10-14 |
| [Vanilla Textures Expanded - Variations](https://steamcommunity.com/sharedfiles/filedetails/?id=2493234474) | **On** | — | yes | 2026-10-14 |
| [Vanilla Trading Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2785616901) | Subscribed, off | — | yes | 2026-10-14 |
| [Vanilla Traits Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2296404655) | No | — | yes | 2026-10-14 |
| [Vanilla Vehicles Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3014906877) | **On** | yes | yes | 2026-10-15 |
| [Vanilla Vehicles Expanded - Tier 3](https://steamcommunity.com/sharedfiles/filedetails/?id=3047892432) | **On** | yes | yes | 2026-10-15 |
| [Vanilla Vehicles Expanded - Upgrades](https://steamcommunity.com/sharedfiles/filedetails/?id=3302208420) | **On** | yes | yes | 2026-10-15 |
| [Vanilla Weapons Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=1814383360) | No | yes | yes | 2026-10-14 |
| [Vanilla Weapons Expanded - Bioferrite](https://steamcommunity.com/sharedfiles/filedetails/?id=3287469135) | **On** | yes | yes | 2026-10-14 |
| [Vanilla Weapons Expanded - Coilguns](https://steamcommunity.com/sharedfiles/filedetails/?id=2375692535) | **On** | yes | yes | 2026-10-14 |
| [Vanilla Weapons Expanded - Frontier](https://steamcommunity.com/sharedfiles/filedetails/?id=2454918139) | No | yes | yes | 2026-10-14 |
| [Vanilla Weapons Expanded - Grenades](https://steamcommunity.com/sharedfiles/filedetails/?id=2194472657) | No | yes | yes | 2026-10-14 |
| [Vanilla Weapons Expanded - Heavy Weapons](https://steamcommunity.com/sharedfiles/filedetails/?id=2329126791) | No | yes | yes | 2026-10-14 |
| [Vanilla Weapons Expanded - Laser](https://steamcommunity.com/sharedfiles/filedetails/?id=1989352844) | No | yes | yes | 2026-10-14 |
| [Vanilla Weapons Expanded - Makeshift](https://steamcommunity.com/sharedfiles/filedetails/?id=2419690698) | No | yes | yes | 2026-10-14 |
| [Vanilla Weapons Expanded - Non-Lethal](https://steamcommunity.com/sharedfiles/filedetails/?id=2454918354) | No | yes | yes | 2026-10-14 |
| [Vanilla Weapons Expanded - Quickdraw](https://steamcommunity.com/sharedfiles/filedetails/?id=1906780517) | No | yes | **no** | 2026-02-19 |
| [Vanilla Weapons Expanded - Tribal](https://steamcommunity.com/sharedfiles/filedetails/?id=2454918552) | No | yes | yes | 2026-10-14 |
| [Vehicle Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=3014915404) | **On** | yes | **no** | 2025-10-21 |
<!-- END GENERATED: vanilla-expanded -->

## Switched on

<!-- BEGIN GENERATED: active -->
### Game and DLCs (6)

| Load # | Mod | Author | Package ID | Updated | 1.7 |
| --- | --- | --- | --- | --- | --- |
| 4 | ludeon.rimworld | Ludeon Studios | `ludeon.rimworld` | — | — |
| 5 | ludeon.rimworld.royalty | Ludeon Studios | `ludeon.rimworld.royalty` | — | yes |
| 6 | ludeon.rimworld.ideology | Ludeon Studios | `ludeon.rimworld.ideology` | — | yes |
| 7 | ludeon.rimworld.biotech | Ludeon Studios | `ludeon.rimworld.biotech` | — | yes |
| 8 | ludeon.rimworld.anomaly | Ludeon Studios | `ludeon.rimworld.anomaly` | — | yes |
| 9 | ludeon.rimworld.odyssey | Ludeon Studios | `ludeon.rimworld.odyssey` | — | yes |

### Vanilla Expanded (52)

| Load # | Mod | Author | Package ID | Updated | 1.7 |
| --- | --- | --- | --- | --- | --- |
| 174 | [Vanilla Apparel Expanded — Accessories](https://steamcommunity.com/sharedfiles/filedetails/?id=2521176396) | Oskar Potocki, xrushha and 2 more | `vanillaexpanded.vaeaccessories` | 2026-10-14 | yes |
| 10 | [Vanilla Backgrounds Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2775017012) | legodude17, Oskar Potocki | `vanillaexpanded.backgrounds` | 2026-10-14 | yes |
| 175 | [Vanilla Brewing Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2186560858) | Oskar Potocki, Sarg Bjornson and 1 more | `vanillaexpanded.vbrewe` | 2026-10-14 | yes |
| 261 | [Vanilla Brewing Expanded - Coffees and Teas](https://steamcommunity.com/sharedfiles/filedetails/?id=2275449762) | Sarg Bjornson, Chowder and 1 more | `vanillaexpanded.vbrewecandt` | 2026-10-14 | yes |
| 262 | [Vanilla Chemfuel Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2792917473) | Oskar Potocki, Kikohi | `vanillaexpanded.vchemfuele` | 2026-10-14 | yes |
| 176 | [Vanilla Cooking Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2134308519) | Oskar Potocki, Sarg Bjornson and 1 more | `vanillaexpanded.vcooke` | 2026-10-14 | yes |
| 14 | [Vanilla Expanded Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=2023507013) | Oskar Potocki, XeoNovaDan and 5 more | `oskarpotocki.vanillafactionsexpanded.core` | 2026-10-21 | yes |
| 178 | [Vanilla Factions Expanded - Classical](https://steamcommunity.com/sharedfiles/filedetails/?id=2787850474) | Oskar Potocki, ISOREX and 3 more | `oskarpotocki.vfe.classical` | 2026-10-14 | yes |
| 19 | [Vanilla Factions Expanded - Deserters](https://steamcommunity.com/sharedfiles/filedetails/?id=3025493377) | Oskar Potocki, xrushha and 1 more | `oskarpotocki.vfe.deserters` | 2026-10-14 | yes |
| 15 | [Vanilla Factions Expanded - Empire](https://steamcommunity.com/sharedfiles/filedetails/?id=2938820380) | Oskar Potocki, xrushha and 2 more | `oskarpotocki.vfe.empire` | 2026-10-20 | yes |
| 16 | [Vanilla Factions Expanded - Medieval 2](https://steamcommunity.com/sharedfiles/filedetails/?id=3444347874) | Oskar Potocki, xrushha and 3 more | `oskarpotocki.vfe.medieval2` | 2026-10-25 | yes |
| 17 | [Vanilla Factions Expanded - Pirates](https://steamcommunity.com/sharedfiles/filedetails/?id=2723801948) | Oskar Potocki, Sarg Bjornson and 7 more | `oskarpotocki.vfe.pirates` | 2026-10-20 | yes |
| 179 | [Vanilla Factions Expanded - Tribals](https://steamcommunity.com/sharedfiles/filedetails/?id=3079786283) | Oskar Potocki, xrushha and 2 more | `oskarpotocki.vfe.tribals` | 2026-10-21 | yes |
| 180 | [Vanilla Fishing Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=1914064942) | Oskar Potocki, Sarg Bjornson | `vanillaexpanded.vcef` | 2026-10-14 | yes |
| 181 | [Vanilla Furniture Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=1718190143) | OskarPotocki, Taranchuk and 2 more | `vanillaexpanded.vfecore` | 2026-10-14 | yes |
| 182 | [Vanilla Furniture Expanded - Architect](https://steamcommunity.com/sharedfiles/filedetails/?id=2608762624) | Oskar Potocki, xrushha and 2 more | `vanillaexpanded.vfearchitect` | 2026-10-14 | yes |
| 183 | [Vanilla Furniture Expanded - Art](https://steamcommunity.com/sharedfiles/filedetails/?id=1968134023) | Oskar Potocki, Sarg Bjornson | `vanillaexpanded.vfeart` | 2026-10-14 | yes |
| 184 | [Vanilla Furniture Expanded - Factory](https://steamcommunity.com/sharedfiles/filedetails/?id=3686924415) | Oskar Potocki, Sarg Bjornson and 1 more | `vanillaexpanded.vfefactory` | 2026-10-14 | yes |
| 185 | [Vanilla Furniture Expanded - Farming](https://steamcommunity.com/sharedfiles/filedetails/?id=1957158779) | Oskar Potocki, dninemfive and 2 more | `vanillaexpanded.vfefarming` | 2026-10-14 | yes |
| 186 | [Vanilla Furniture Expanded - Medical Module](https://steamcommunity.com/sharedfiles/filedetails/?id=1718191613) | OskarPotocki, Atlas and 1 more | `vanillaexpanded.vfemedical` | 2026-10-14 | yes |
| 187 | [Vanilla Furniture Expanded - Power](https://steamcommunity.com/sharedfiles/filedetails/?id=2062943477) | Oskar Potocki, Sarg Bjornson | `vanillaexpanded.vfepower` | 2026-10-14 | yes |
| 188 | [Vanilla Furniture Expanded - Production](https://steamcommunity.com/sharedfiles/filedetails/?id=1880253632) | Oskar Potocki, Trunken and 1 more | `vanillaexpanded.vfeproduction` | 2026-10-14 | yes |
| 189 | [Vanilla Furniture Expanded - Props and Decor](https://steamcommunity.com/sharedfiles/filedetails/?id=2102143149) | Oskar Potocki, Sarg Bjornson | `vanillaexpanded.vfepropsanddecor` | 2026-10-14 | yes |
| 191 | [Vanilla Gravship Expanded - Chapter 1](https://steamcommunity.com/sharedfiles/filedetails/?id=3609835606) | Oskar Potocki, Sarg Bjornson and 3 more | `vanillaexpanded.gravship` | 2026-10-20 | yes |
| 263 | [Vanilla Gravship Expanded - Chapter 2](https://steamcommunity.com/sharedfiles/filedetails/?id=3799737423) | Oskar Potocki, Sarg Bjornson and 2 more | `vanillaexpanded.gravship2` | 2026-10-20 | yes |
| 193 | [Vanilla Ideology Expanded - Dryads](https://steamcommunity.com/sharedfiles/filedetails/?id=2720631512) | Oskar Potocki, Sarg Bjornson and 2 more | `vanillaexpanded.ideo.dryads` | 2026-10-14 | yes |
| 264 | [Vanilla Ideology Expanded - Memes and Structures](https://steamcommunity.com/sharedfiles/filedetails/?id=2636329500) | Oskar Potocki, Sarg Bjornson and 1 more | `vanillaexpanded.vmemese` | 2026-10-14 | yes |
| 194 | [Vanilla Nutrient Paste Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2920385763) | Oskar Potocki, Kikohi | `vanillaexpanded.vnutriente` | 2026-10-14 | yes |
| 195 | [Vanilla Outposts Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2688941031) | legodude17, Oskar Potocki | `vanillaexpanded.outposts` | 2026-10-14 | yes |
| 196 | [Vanilla Persona Weapons Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2826922787) | Oskar Potocki, Taranchuk | `vanillaexpanded.vpersonaweaponse` | 2026-10-14 | yes |
| 197 | [Vanilla Plants Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2134308522) | Oskar Potocki, Sarg Bjornson and 1 more | `vanillaexpanded.vplantse` | 2026-10-14 | yes |
| 268 | [Vanilla Plants Expanded - More Plants](https://steamcommunity.com/sharedfiles/filedetails/?id=2748889667) | Sarg Bjornson, Oskar Potocki | `vanillaexpanded.vplantsemore` | 2026-10-14 | yes |
| 269 | [Vanilla Plants Expanded - Mushrooms](https://steamcommunity.com/sharedfiles/filedetails/?id=3006389281) | Taranchuk, Sarg Bjornson and 1 more | `vanillaexpanded.vplantsemushrooms` | 2026-10-14 | yes |
| 198 | [Vanilla Psycasts Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2842502659) | erdelf, Oskar Potocki and 6 more | `vanillaexpanded.vpsycastse` | 2026-10-15 | yes |
| 270 | [Vanilla Psycasts Expanded - Hemosage](https://steamcommunity.com/sharedfiles/filedetails/?id=2990596478) | Oskar Potocki, Taranchuk and 2 more | `vanillaexpanded.vpe.hemosage` | 2026-10-14 | yes |
| 199 | [Vanilla Races Expanded - Genie](https://steamcommunity.com/sharedfiles/filedetails/?id=2901424072) | Oskar Potocki, Sarg Bjornson | `vanillaracesexpanded.genie` | 2026-10-14 | yes |
| 200 | [Vanilla Races Expanded - Highmate](https://steamcommunity.com/sharedfiles/filedetails/?id=2995385834) | Oskar Potocki, Taranchuk and 1 more | `vanillaracesexpanded.highmate` | 2026-10-14 | yes |
| 201 | [Vanilla Races Expanded - Hussar](https://steamcommunity.com/sharedfiles/filedetails/?id=2893586390) | Oskar Potocki, xrushha and 2 more | `vanillaracesexpanded.hussar` | 2026-10-14 | yes |
| 202 | [Vanilla Races Expanded - Pigskin](https://steamcommunity.com/sharedfiles/filedetails/?id=3164798809) | Oskar Potocki, Sarg Bjornson and 1 more | `vanillaracesexpanded.pigskin` | 2026-10-14 | yes |
| 203 | [Vanilla Races Expanded - Sanguophage](https://steamcommunity.com/sharedfiles/filedetails/?id=2963116383) | Oskar Potocki, Sarg Bjornson and 1 more | `vanillaracesexpanded.sanguophage` | 2026-10-14 | yes |
| 204 | [Vanilla Races Expanded - Saurid](https://steamcommunity.com/sharedfiles/filedetails/?id=2880990495) | Oskar Potocki, Neronix17 | `vanillaracesexpanded.saurid` | 2026-10-14 | yes |
| 205 | [Vanilla Races Expanded - Waster](https://steamcommunity.com/sharedfiles/filedetails/?id=2983471725) | Oskar Potocki, Sarg Bjornson | `vanillaracesexpanded.waster` | 2026-10-14 | yes |
| 206 | [Vanilla Recycling Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3155781848) | OskarPotocki, Sarg Bjornson and 1 more | `vanillaexpanded.recycling` | 2026-10-14 | yes |
| 271 | [Vanilla Skills Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3400246558) | legodude17, Oskar Potocki and 1 more | `vanillaexpanded.skills` | 2026-10-14 | yes |
| 207 | [Vanilla Temperature Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3202046258) | Oskar Potocki, xrushha and 2 more | `vanillaexpanded.temperature` | 2026-10-14 | yes |
| 208 | [Vanilla Textures Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2016436324) | Oskar Potocki, Helixien and 5 more | `vanillaexpanded.vtexe` | 2026-10-14 | yes |
| 272 | [Vanilla Textures Expanded - Variations](https://steamcommunity.com/sharedfiles/filedetails/?id=2493234474) | Sarg Bjornson | `vanillaexpanded.vtexvariations` | 2026-10-14 | yes |
| 209 | [Vanilla Vehicles Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3014906877) | Oskar Potocki, xrushha and 4 more | `oskarpotocki.vanillavehiclesexpanded` | 2026-10-15 | — |
| 273 | [Vanilla Vehicles Expanded - Tier 3](https://steamcommunity.com/sharedfiles/filedetails/?id=3047892432) | Oskar Potocki, xrushha and 3 more | `oskarpotocki.vanillavehiclesexpandedtier3` | 2026-10-15 | — |
| 274 | [Vanilla Vehicles Expanded - Upgrades](https://steamcommunity.com/sharedfiles/filedetails/?id=3302208420) | Oskar Potocki, Smash Phil | `oskarpotocki.vanillavehiclesexpandedupgrades` | 2026-10-15 | yes |
| 210 | [Vanilla Weapons Expanded - Bioferrite](https://steamcommunity.com/sharedfiles/filedetails/?id=3287469135) | Oskar Potocki, xrushha and 2 more | `vanillaexpanded.vwebf` | 2026-10-14 | yes |
| 211 | [Vanilla Weapons Expanded - Coilguns](https://steamcommunity.com/sharedfiles/filedetails/?id=2375692535) | Oskar Potocki, AveryTheKitty and 2 more | `vanillaexpanded.vwec` | 2026-10-14 | yes |

### Everything else (242)

| Load # | Mod | Author | Package ID | Updated | 1.7 |
| --- | --- | --- | --- | --- | --- |
| 218 | [\[CF\] Anomaly Upscaled](https://steamcommunity.com/sharedfiles/filedetails/?id=3239664028) | Crimson | `cf.anomalyupscaled` | 2025-06-12 | — |
| 219 | [\[FSF\] Better Exploration Loot](https://steamcommunity.com/sharedfiles/filedetails/?id=3526957922) | FrozenSnowFox | `frozensnowfox.betterexplorationloot` | 2026-10-12 | yes |
| 220 | [\[kyzy\] Bio Ranch](https://steamcommunity.com/sharedfiles/filedetails/?id=3771252359) | kyzy\_4399 | `kyzy.bioranch` | 2026-09-21 | — |
| 278 | [\[kyzy\] Ship Wall Enhanced](https://steamcommunity.com/sharedfiles/filedetails/?id=3537799933) | Kyzy\_4399 | `kyzy.shipwallenhanced` | 2026-09-17 | — |
| 22 | [\[MUS\]太空基地家具Space Base Furniture](https://steamcommunity.com/sharedfiles/filedetails/?id=3523916780) | ELUN982697019 | `mingtuwuxiang.spacebasedecorative` | 2025-09-02 | — |
| 23 | [\[Og\] Repair Your Gear](https://steamcommunity.com/sharedfiles/filedetails/?id=3513376486) | Oganesson | `og.repair.your.gear` | 2026-10-19 | yes |
| 24 | [\[sbz\] Bookcase](https://steamcommunity.com/sharedfiles/filedetails/?id=3789304104) | seobongzu | `sbz.bookcase` | 2026-10-16 | yes |
| 25 | [\[sbz\] Fridge](https://steamcommunity.com/sharedfiles/filedetails/?id=3486264784) | seobongzu | `sbz.neatstoragefridge` | 2026-10-16 | yes |
| 26 | [\[sbz\] Gravship Storage](https://steamcommunity.com/sharedfiles/filedetails/?id=3537905298) | seobongzu | `sbz.gravshipstorage` | 2026-10-16 | yes |
| 27 | [\[sbz\] Mini Mech Booster](https://steamcommunity.com/sharedfiles/filedetails/?id=3547537902) | seobongzu | `sbz.minimechbooster` | 2026-10-16 | yes |
| 28 | [\[sbz\] Neat Storage](https://steamcommunity.com/sharedfiles/filedetails/?id=3416243474) | seobongzu | `sbz.neatstorage` | 2026-10-21 | yes |
| 29 | [\[sbz\] Workbench Shelf](https://steamcommunity.com/sharedfiles/filedetails/?id=3484671309) | seobongzu | `sbz.neatstorageworkbenchshelf` | 2026-10-16 | yes |
| 30 | [Adaptive Ideology Storage](https://steamcommunity.com/sharedfiles/filedetails/?id=3301337278) | vin | `adaptive.ideology.storage` | 2025-07-07 | — |
| 11 | [Adaptive Storage Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=3033901359) | Soul, Phaneron and 1 more | `adaptive.storage.framework` | 2025-06-14 | — |
| 31 | [All Production Gives XP](https://steamcommunity.com/sharedfiles/filedetails/?id=3539744423) | Kattalisk | `kattalisk.allproductionxp` | 2026-10-14 | yes |
| 32 | [Allies are Helpful](https://steamcommunity.com/sharedfiles/filedetails/?id=3534920369) | Nina.Goblin | `ninagoblin.alliesarehelpful` | 2026-10-15 | yes |
| 33 | [Allow Tool](https://steamcommunity.com/sharedfiles/filedetails/?id=761421485) | UnlimitedHugs | `unlimitedhugs.allowtool` | 2025-07-13 | — |
| 34 | [Alpha Genes](https://steamcommunity.com/sharedfiles/filedetails/?id=2891845502) | Allie, Erin and 3 more | `sarg.alphagenes` | 2026-10-18 | yes |
| 221 | [Alpha Genes Integrated](https://steamcommunity.com/sharedfiles/filedetails/?id=3538704272) | kupa\_ | `kupa.alphagenesintegrated` | 2025-08-06 | — |
| 35 | [Anomaly Research Asteroid](https://steamcommunity.com/sharedfiles/filedetails/?id=3527726648) | Zoarak963 | `zoarak.anomalyplat` | 2025-07-17 | — |
| 36 | [Anthrosonae](https://steamcommunity.com/sharedfiles/filedetails/?id=2902258418) | Willow, Erin and 1 more | `atk.anthrosonae` | 2025-08-11 | — |
| 37 | [Apex Mechanoids](https://steamcommunity.com/sharedfiles/filedetails/?id=3792325789) | Mortis, Aelflead Wayne and 8 more | `mortis.apexmechanoids` | 2026-10-21 | yes |
| 279 | [Aquariums!](https://steamcommunity.com/sharedfiles/filedetails/?id=3478462020) | NightmareCorporation | `nightmare.aquariums` | 2026-10-19 | yes |
| 38 | [Auto-Cut Blight - 1.6](https://steamcommunity.com/sharedfiles/filedetails/?id=3520167264) | Defi | `defi.autocutblight` | 2025-07-08 | — |
| 280 | [Automatic Hydroponics](https://steamcommunity.com/sharedfiles/filedetails/?id=3528490718) | Poncho | `poncho.automatichydroponics` | 2026-07-20 | — |
| 297 | [Automatic Hydroponics Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3536424131) | Jijiang | `automatichydroponics.expanded` | 2026-10-16 | yes |
| 222 | [BattIePatch: Enhanced Bioregeneration](https://steamcommunity.com/sharedfiles/filedetails/?id=3241232294) | BattIeBear | `battiebear.battiepatch.enhancedbioregeneration` | 2026-10-25 | yes |
| 39 | [Bean's Turret Pack](https://steamcommunity.com/sharedfiles/filedetails/?id=3271187804) | Bean | `bean.turretpack` | 2025-08-10 | — |
| 40 | [Better Gene Processor UI](https://steamcommunity.com/sharedfiles/filedetails/?id=3548641009) | tomblenz | `tomblenz.bettergeneui` | 2025-11-12 | — |
| 41 | [Better Hot Springs](https://steamcommunity.com/sharedfiles/filedetails/?id=3532108439) | Elindis | `elindis.betterhotsprings` | 2025-07-24 | — |
| 3 | [Better Stacktraces](https://steamcommunity.com/sharedfiles/filedetails/?id=3541579129) | Aleksey | `aleksey.betterstacktraces` | 2026-10-23 | — |
| 42 | [Bioform Matrix](https://steamcommunity.com/sharedfiles/filedetails/?id=3535848372) | Author | `bart.bioformmatrix` | 2025-08-16 | — |
| 43 | [Biosculpter Detox](https://steamcommunity.com/sharedfiles/filedetails/?id=3540117812) | Zei33 | `zei33.biosculpterdetox` | 2025-08-22 | — |
| 223 | [Biotech for Gravship](https://steamcommunity.com/sharedfiles/filedetails/?id=3722358861) | Alsariul | `als.biotechgravship` | 2026-10-16 | yes |
| 44 | [Bladelink](https://steamcommunity.com/sharedfiles/filedetails/?id=3535749804) | Heavenly Guardian | `heavenlyguardian.bladelink` | 2025-10-06 | — |
| 45 | [Blueprints Forked - 1.6](https://steamcommunity.com/sharedfiles/filedetails/?id=3525001145) | Defi | `defi.blueprints.fork` | 2025-07-14 | — |
| 46 | [Build From Inventory - Continued](https://steamcommunity.com/sharedfiles/filedetails/?id=3534185281) | Uuugggg | `memegoddess.buildfrominventory` | 2026-10-22 | yes |
| 281 | [Bullet Sounds](https://steamcommunity.com/sharedfiles/filedetails/?id=3806470380) | Senilia | `senilia.bulletsounds` | 2026-10-27 | yes |
| 282 | [Campfire Stews](https://steamcommunity.com/sharedfiles/filedetails/?id=3467551920) | BadOaks | `badoaks.campfirestews` | 2025-07-23 | — |
| 47 | [Cargo Holds](https://steamcommunity.com/sharedfiles/filedetails/?id=3536708812) | joolsdgg | `jdgg.adaptive.cargoholds` | 2025-12-12 | — |
| 48 | [Cargo waste container](https://steamcommunity.com/sharedfiles/filedetails/?id=2888532939) | AobaKuma | `aoba.cargocontainer.waste` | 2025-12-03 | — |
| 49 | [Character Editor](https://steamcommunity.com/sharedfiles/filedetails/?id=1874644848) | void | `void.charactereditor` | 2026-10-20 | yes |
| 50 | [Choose Where To Land](https://steamcommunity.com/sharedfiles/filedetails/?id=3537970831) | 极光薄荷糖 | `kearril.choosewheretoland` | 2026-07-27 | — |
| 51 | [Colony Hospital / 殖民地医院](https://steamcommunity.com/sharedfiles/filedetails/?id=3774795066) | Jianyuan | `jianyuan.colonyhospital` | 2026-10-15 | — |
| 20 | [Combat Extended](https://steamcommunity.com/sharedfiles/filedetails/?id=2890901044) | CE Team | `ceteam.combatextended` | 2026-10-15 | — |
| 52 | [Combat Extended - Charged Ammo](https://steamcommunity.com/sharedfiles/filedetails/?id=3496978569) | Asiimoves | `asiimoves.chargedammo` | 2026-10-15 | yes |
| 53 | [Combat Extended Armors](https://steamcommunity.com/sharedfiles/filedetails/?id=3565026039) | CE Team | `ceteam.combatextendedarmors` | 2026-10-15 | — |
| 54 | [Combat Extended Armory](https://steamcommunity.com/sharedfiles/filedetails/?id=2911110083) | CE Team | `ceteam.combatextendedarmory` | 2026-10-15 | — |
| 55 | [Combat Extended Guns](https://steamcommunity.com/sharedfiles/filedetails/?id=3565025938) | CE Team | `ceteam.combatextendedguns` | 2026-10-15 | — |
| 56 | [Complementary Odyssey](https://steamcommunity.com/sharedfiles/filedetails/?id=3546612303) | MrHydralisk | `mrhydralisk.complementaryodyssey` | 2026-08-01 | — |
| 283 | [Ctrl F - Continued](https://steamcommunity.com/sharedfiles/filedetails/?id=3529520961) | Uuugggg | `memegoddess.ctrlf` | 2026-10-16 | yes |
| 224 | [Custom Base Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=3813689040) | Evil Eye Guy | `evileyeguy.custombaseframework` | 2026-10-25 | yes |
| 57 | [Customize Weapon](https://steamcommunity.com/sharedfiles/filedetails/?id=3550585103) | Vortex | `vortex.customizeweapon` | 2026-10-23 | yes |
| 284 | D'thorga Factory Additions | Phobos A. D'thorga | `phobosdthorga.factoryadditions` | — | yes |
| 58 | [Dawn of a New Day](https://steamcommunity.com/sharedfiles/filedetails/?id=3771580900) | AlenDio | `alendio.dawnofanewday` | 2026-10-17 | yes |
| 59 | [Death Rattle Continued \[1.2+\]](https://steamcommunity.com/sharedfiles/filedetails/?id=2896207870) | cat2002 - Troopersmith1 - Linq- deathrat | `troopersmith1.deathrattle` | 2025-07-20 | — |
| 60 | [Deep And Deeper](https://steamcommunity.com/sharedfiles/filedetails/?id=3509420021) | Shashlichnik, p (aka Шашлычник and 1 more | `shashlichnik.deepanddeeper` | 2025-09-13 | — |
| 225 | [Deep Pills](https://steamcommunity.com/sharedfiles/filedetails/?id=3758899296) | ODs | `ods.deeppills` | 2026-07-31 | — |
| 61 | [Defensive Positions - Forked](https://steamcommunity.com/sharedfiles/filedetails/?id=3550360467) | GonDragon | `gondragon.defensivepositions` | 2025-10-12 | — |
| 62 | [Dignified Departure](https://steamcommunity.com/sharedfiles/filedetails/?id=3790055136) | Marukun | `mrk.dignifieddeparture` | 2026-10-13 | yes |
| 63 | [Dub's Paint Shop](https://steamcommunity.com/sharedfiles/filedetails/?id=1579516669) | Dubwise | `dubwise.dubspaintshop` | 2026-10-13 | yes |
| 64 | [Dubs Bad Hygiene](https://steamcommunity.com/sharedfiles/filedetails/?id=836308268) | Dubwise | `dubwise.dubsbadhygiene` | 2026-10-26 | yes |
| 226 | [Dubs Bad Hygiene - Spring Water Patch](https://steamcommunity.com/sharedfiles/filedetails/?id=3531641684) | azrazalea | `azrazalea.dbh.springwater.patch` | 2026-10-14 | yes |
| 65 | [Dubs Break Mod](https://steamcommunity.com/sharedfiles/filedetails/?id=1722398508) | Dubwise | `dubwise.dubsbreakmod` | 2026-10-13 | yes |
| 227 | [Dubs Central Heating](https://steamcommunity.com/sharedfiles/filedetails/?id=2619214952) | Dubwise | `dubwise.dubscentralheating` | 2026-10-16 | yes |
| 66 | [Dubs Mint Menus](https://steamcommunity.com/sharedfiles/filedetails/?id=1446523594) | Dubwise | `dubwise.dubsmintmenus` | 2026-10-19 | yes |
| 67 | [Dubs Mint Minimap](https://steamcommunity.com/sharedfiles/filedetails/?id=1662119905) | Dubwise | `dubwise.dubsmintminimap` | 2026-10-13 | yes |
| 300 | [Dubs Performance Analyzer](https://steamcommunity.com/sharedfiles/filedetails/?id=2038874626) | Dubwise | `dubwise.dubsperformanceanalyzer.steam` | 2026-10-25 | yes |
| 68 | [Dubs Rimatomics](https://steamcommunity.com/sharedfiles/filedetails/?id=1127530465) | Dubwise | `dubwise.rimatomics` | 2026-10-23 | yes |
| 69 | [Dubs Rimkit](https://steamcommunity.com/sharedfiles/filedetails/?id=832333531) | Dubwise | `dubwise.dubsrimkit` | 2026-10-13 | yes |
| 70 | [EdB Prepare Carefully](https://steamcommunity.com/sharedfiles/filedetails/?id=735106432) | EdB | `edb.preparecarefully` | 2025-08-03 | — |
| 71 | [Electrofishing](https://steamcommunity.com/sharedfiles/filedetails/?id=3542849317) | acutus | `acutus.electrofishing` | 2025-08-08 | — |
| 72 | [Enemy Mechanitors](https://steamcommunity.com/sharedfiles/filedetails/?id=3793204027) | asunib | `asunib.enemymechanitors` | 2026-10-25 | yes |
| 73 | [Enhanced Ideology](https://steamcommunity.com/sharedfiles/filedetails/?id=3793123680) | dsweber2, ilyvion and 1 more | `dsweber2.enhancedideology` | 2026-10-22 | yes |
| 74 | [Enhanced Vat Learning](https://steamcommunity.com/sharedfiles/filedetails/?id=3551263267) | Nina.Goblin | `ninagoblin.enhancedvatlearning` | 2026-10-15 | yes |
| 285 | [Everybody Gets One - Continued](https://steamcommunity.com/sharedfiles/filedetails/?id=3530806680) | Uuugggg | `memegoddess.everybodygetsone` | 2026-10-22 | yes |
| 75 | [Farmable Neutroamine](https://steamcommunity.com/sharedfiles/filedetails/?id=3222103279) | kongkim | `kongkim.farmableneutroamine` | 2026-06-22 | — |
| 76 | [Female Body Variants Continued](https://steamcommunity.com/sharedfiles/filedetails/?id=3798082132) | DanZinagri, tiagocc0 | `tiagocc0.femalebodyvariants` | 2026-10-27 | yes |
| 77 | [Field Tents](https://steamcommunity.com/sharedfiles/filedetails/?id=3823513030) | lordfelix | `lordfelix.fieldtents` | 2026-10-24 | yes |
| 78 | [Fishing Is Fun](https://steamcommunity.com/sharedfiles/filedetails/?id=3538562620) | JalapenoLabs | `jalapenolabs.rimworld.fishingisfun` | 2025-08-04 | — |
| 79 | [Float Sub-Menus](https://steamcommunity.com/sharedfiles/filedetails/?id=2864015430) | kathanon | `kathanon.floatsubmenu` | 2025-07-20 | — |
| 80 | [Floor-Mounted Aquarium](https://steamcommunity.com/sharedfiles/filedetails/?id=3538928291) | Jijiang | `floor.mounted.aquarium` | 2026-10-16 | yes |
| 21 | [Fortified Features Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=3498575851) | AOBA | `aoba.framework` | 2026-10-25 | yes |
| 228 | [Frontline Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3817025489) | Serina, Airo | `serinairo.frontlineworkshop` | 2026-10-22 | — |
| 81 | [Gene Ripper](https://steamcommunity.com/sharedfiles/filedetails/?id=3524806362) | Defi | `defi.generipper` | 2025-08-12 | — |
| 229 | [Gerrymon's Misc Props: Fences](https://steamcommunity.com/sharedfiles/filedetails/?id=3797400517) | Gerrymon | `gmmp.fences` | 2026-10-19 | yes |
| 230 | [Gerrymon's Misc Props: Graveyard](https://steamcommunity.com/sharedfiles/filedetails/?id=3780458386) | Gerrymon | `gmmp.graveyard` | 2026-10-19 | yes |
| 82 | [Giddy-Up 2 - Continued](https://steamcommunity.com/sharedfiles/filedetails/?id=3674332861) | Roolo, Owlchemist and 2 more | `memegoddess.giddyup` | 2026-10-22 | yes |
| 83 | [Glass+Lights](https://steamcommunity.com/sharedfiles/filedetails/?id=826153738) | NanoCE | `nanoce.glasslights` | 2025-07-25 | — |
| 84 | [Glitterworld Medicine Production](https://steamcommunity.com/sharedfiles/filedetails/?id=2885153505) | MrHydralisk | `mrhydralisk.glitterworldmedicineproduction` | 2026-10-21 | yes |
| 85 | [Gore Perfected](https://steamcommunity.com/sharedfiles/filedetails/?id=3814447022) | tiepi | `tiepi.goreperfected` | 2026-10-26 | yes |
| 86 | [Grav Reactor](https://steamcommunity.com/sharedfiles/filedetails/?id=3550335990) | Hope | `hope.gravreactor` | 2025-08-16 | — |
| 231 | [Gravship Captain Set](https://steamcommunity.com/sharedfiles/filedetails/?id=3528780638) | Kilo | `kilo.capvachelm` | 2026-10-15 | yes |
| 232 | [Gravship Tactical Holotable](https://steamcommunity.com/sharedfiles/filedetails/?id=3820234401) | Gravship Furniture Expansion | `gravship.tactical.holotable` | 2026-10-15 | yes |
| 233 | [Gravship Water Systems](https://steamcommunity.com/sharedfiles/filedetails/?id=3587768178) | ocarina0001, tefnut and 1 more | `tefnut.gravship.water.systems` | 2026-10-15 | yes |
| 87 | [Grazing Area](https://steamcommunity.com/sharedfiles/filedetails/?id=3769685249) | gguake | `gguake.grazingarea` | 2026-10-13 | yes |
| 88 | [Growth Moments Make Sense Now](https://steamcommunity.com/sharedfiles/filedetails/?id=3264556972) | Jdalt | `jdalt.gmmsn` | 2026-01-02 | — |
| 1 | [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077) | Andreas Pardeike | `brrainz.harmony` | 2026-10-15 | yes |
| 89 | [Health Display](https://steamcommunity.com/sharedfiles/filedetails/?id=3522897532) | Sam | `gt.sam.healthdisplay` | 2025-09-24 | — |
| 90 | [Hemofungus](https://steamcommunity.com/sharedfiles/filedetails/?id=3259446830) | NoSleep | `nosleep.hemofungus` | 2025-06-19 | — |
| 91 | [Hemogen Pack - Emergency transfusion](https://steamcommunity.com/sharedfiles/filedetails/?id=3778942862) | Aoba | `aoba.hemogendirect` | 2026-10-15 | yes |
| 234 | [Hind Helicopter Gunship](https://steamcommunity.com/sharedfiles/filedetails/?id=3822052079) | potemkinvillager | `potemkinvillager.mi24hindhelicopter` | 2026-10-24 | yes |
| 235 | [Hospitality (Continued)](https://steamcommunity.com/sharedfiles/filedetails/?id=3509486825) | Zaljerem | `orion.hospitality` | 2026-10-20 | yes |
| 286 | [Hospitality + Ideology Patch](https://steamcommunity.com/sharedfiles/filedetails/?id=3789536584) | Fumblesneeze | `fumblesneeze.guestbedgizmo` | 2026-09-06 | — |
| 287 | [Hospitality: Casino](https://steamcommunity.com/sharedfiles/filedetails/?id=2939292644) | Adamas | `adamas.hospitalitycasino` | 2026-10-14 | yes |
| 288 | [Hospitality: Spa](https://steamcommunity.com/sharedfiles/filedetails/?id=2971831654) | Adamas | `adamas.hospitalityspa` | 2026-10-16 | yes |
| 289 | [Hospitality: Vending machines](https://steamcommunity.com/sharedfiles/filedetails/?id=3014885065) | Adamas | `adamas.vendingmachines` | 2026-10-16 | yes |
| 13 | [HugsLib](https://steamcommunity.com/sharedfiles/filedetails/?id=818773962) | UnlimitedHugs | `unlimitedhugs.hugslib` | 2025-07-13 | — |
| 236 | [HUMVEE](https://steamcommunity.com/sharedfiles/filedetails/?id=3824413979) | LUNARTIKA | `hmv.lunartika` | 2026-10-24 | yes |
| 92 | [Implants Ripper](https://steamcommunity.com/sharedfiles/filedetails/?id=3801329151) | MaoSaur | `maosaur.implantsripper` | 2026-10-16 | yes |
| 237 | [Improved Map Search](https://steamcommunity.com/sharedfiles/filedetails/?id=3547866455) | kathanon | `kathanon.groupedmapsearch` | 2025-09-21 | — |
| 93 | [Info Card Plus](https://steamcommunity.com/sharedfiles/filedetails/?id=3776660810) | Kaamalauppias | `kaamalauppias.infocardplus` | 2026-08-22 | — |
| 94 | [Interaction Bubbles](https://steamcommunity.com/sharedfiles/filedetails/?id=1516158345) | Jaxe | `jaxe.bubbles` | 2025-08-25 | — |
| 95 | [InterRim Ballistic Missile](https://steamcommunity.com/sharedfiles/filedetails/?id=3682304832) | Psionic Zephyr | `kazepsi.irbm` | 2026-06-30 | — |
| 96 | [Ion Weaponry (Continued)](https://steamcommunity.com/sharedfiles/filedetails/?id=3532877485) | Zaljerem | `zal.ionweaponry` | 2025-07-24 | — |
| 238 | [IRBM Vanilla Retexture](https://steamcommunity.com/sharedfiles/filedetails/?id=3769399772) | Fort\_Maximus | `fort.irbmvanillaretex` | 2026-07-22 | — |
| 97 | [Keep Converting](https://steamcommunity.com/sharedfiles/filedetails/?id=3461478214) | Linnun | `linnun.keepconverting` | 2026-10-27 | yes |
| 98 | [Launch Sites Expire](https://steamcommunity.com/sharedfiles/filedetails/?id=3525571078) | cass | `cass.launchsitesexpire` | 2025-07-18 | — |
| 239 | [Life Support Pack](https://steamcommunity.com/sharedfiles/filedetails/?id=3530278586) | Kilo | `kilo.lifesuppack` | 2026-10-15 | yes |
| 99 | [LightsOut](https://steamcommunity.com/sharedfiles/filedetails/?id=2584269293) | juanlopez2008 | `juanlopez2008.lightsout` | 2026-10-17 | yes |
| 2 | [Loading Progress](https://steamcommunity.com/sharedfiles/filedetails/?id=3535481557) | ilyvion | `ilyvion.loadingprogress` | 2026-10-21 | yes |
| 100 | [Local Mineral Scanner](https://steamcommunity.com/sharedfiles/filedetails/?id=3798782281) | Sam Hunt | `samhunt.localmineralscanner` | 2026-10-20 | yes |
| 101 | [Log After Def Error (Continued)](https://steamcommunity.com/sharedfiles/filedetails/?id=3550944748) | ordpus | `mlie.logafterdeferror` | 2026-10-19 | yes |
| 102 | [Mechlink Reinforcement](https://steamcommunity.com/sharedfiles/filedetails/?id=3824378660) | Hyaukyuu | `hyaukyuu.mechlinkreinforcement` | 2026-10-22 | yes |
| 103 | [Mechs can brew](https://steamcommunity.com/sharedfiles/filedetails/?id=3535375303) | WarspiteEnthusiast | `wsp.brewing` | 2026-10-13 | yes |
| 104 | [Medical System Expansion 2](https://steamcommunity.com/sharedfiles/filedetails/?id=2056706586) | JonM0 | `mse2.core` | 2026-01-01 | — |
| 105 | [Medical Tab - Continued](https://steamcommunity.com/sharedfiles/filedetails/?id=3658588582) | Meme Goddess | `memegoddess.medicaltab` | 2026-10-16 | yes |
| 106 | [Medicines+](https://steamcommunity.com/sharedfiles/filedetails/?id=2878860499) | Papito | `papito.medicinesplus.at` | 2025-06-13 | — |
| 107 | [Medieval Signs](https://steamcommunity.com/sharedfiles/filedetails/?id=3488857567) | Mewn Goddess | `mewn.medievalsigns` | 2025-08-08 | — |
| 108 | [MediStation](https://steamcommunity.com/sharedfiles/filedetails/?id=3791910009) | Asiimoves | `asiimoves.medistation` | 2026-10-15 | yes |
| 109 | [Mid-saver Saver](https://steamcommunity.com/sharedfiles/filedetails/?id=3261311100) | Owlchemist | `owlchemist.midsaversaver` | 2025-06-19 | — |
| 110 | [Milky Way](https://steamcommunity.com/sharedfiles/filedetails/?id=3773448562) | Andromeda | `andromeda.milkyway` | 2026-10-22 | yes |
| 111 | [Mine Sight](https://steamcommunity.com/sharedfiles/filedetails/?id=3769804600) | rabiosus | `rabiosus.minesight` | 2026-10-16 | yes |
| 112 | [ModularWeapons 2](https://steamcommunity.com/sharedfiles/filedetails/?id=3497834944) | kaitorisenkou | `kaitorisenkou.modularweapons2` | 2026-10-14 | yes |
| 240 | [Mongoose Attack Helicopter](https://steamcommunity.com/sharedfiles/filedetails/?id=3819896964) | potemkinvillager | `potemkinvillager.mongooseattackhelicopter` | 2026-10-25 | yes |
| 113 | [More Quests in Space](https://steamcommunity.com/sharedfiles/filedetails/?id=3551388162) | Holgast | `hol.spacequests` | 2025-08-18 | — |
| 241 | [Munitions Industries for VFE - Factory](https://steamcommunity.com/sharedfiles/filedetails/?id=3695330973) | turkler | `turkler.leammomod` | 2026-05-24 | — |
| 114 | [MutatorWorldIcons](https://steamcommunity.com/sharedfiles/filedetails/?id=3551904193) | Snobi | `snobi.mutatorworldicons` | 2026-01-12 | — |
| 115 | [NANAME Floors](https://steamcommunity.com/sharedfiles/filedetails/?id=3293767181) | OELS | `oels.nanamefloors` | 2026-10-15 | yes |
| 242 | [Nice Bill Tab](https://steamcommunity.com/sharedfiles/filedetails/?id=3520130671) | Andromeda | `andromeda.nicebilltab` | 2026-10-22 | yes |
| 243 | [Nice Research Tab](https://steamcommunity.com/sharedfiles/filedetails/?id=3773496046) | Andromeda | `andromeda.niceresearchtab` | 2026-10-22 | yes |
| 116 | [Nice-o-sphere](https://steamcommunity.com/sharedfiles/filedetails/?id=3547322717) | Nepenthe | `nep.niceosphere` | 2025-08-18 | — |
| 117 | [Nightmare Core](https://steamcommunity.com/sharedfiles/filedetails/?id=3047049650) | NightmareCorporation | `nightmare.core` | 2026-10-19 | yes |
| 118 | [Not My Fault](https://steamcommunity.com/sharedfiles/filedetails/?id=2870045856) | Vesper | `vesper.notmyfault` | 2025-07-30 | — |
| 119 | [Odyssey Transport Shuttle](https://steamcommunity.com/sharedfiles/filedetails/?id=3525851843) | EvilToaster | `toshtaru.odyssey.transport.shuttle` | 2026-10-25 | yes |
| 120 | [Orca Shuttle](https://steamcommunity.com/sharedfiles/filedetails/?id=3528001139) | lioness131.smallmine | `smallmine.heavyshuttle` | 2025-08-04 | — |
| 121 | [Outfit Stands Plus](https://steamcommunity.com/sharedfiles/filedetails/?id=3545172389) | khamenman | `khamenman.outfitstandsplus` | 2026-10-20 | yes |
| 122 | [Pandora:Frontier ReMake](https://steamcommunity.com/sharedfiles/filedetails/?id=3553115301) | ZawodRoshen | `zawodroshen.pandora.frontier.remake` | 2026-08-31 | — |
| 123 | [Passive Exoskeletons](https://steamcommunity.com/sharedfiles/filedetails/?id=3807490214) | Agent\_Orange | `agentorange.passiveexoskeletons` | 2026-10-20 | yes |
| 124 | [Pawn Name Variety (Continued)](https://steamcommunity.com/sharedfiles/filedetails/?id=3548290568) | Murmur | `mlie.pawnnamevariety` | 2026-10-22 | yes |
| 125 | [Pawn Suppression Implants](https://steamcommunity.com/sharedfiles/filedetails/?id=2883152389) | MrHydralisk | `mrhydralisk.pawnsuppressionimplants` | 2025-07-18 | — |
| 126 | [Performance - Slower Pawn Tick Rate](https://steamcommunity.com/sharedfiles/filedetails/?id=3524116050) | Arkymn | `arkymn.slowerpawntickrate` | 2026-10-12 | yes |
| 127 | [Permits: Re-Examined](https://steamcommunity.com/sharedfiles/filedetails/?id=3521089734) | Smxrez | `smxrez.permitsreexamined` | 2026-02-19 | — |
| 128 | [Pick Up And Haul](https://steamcommunity.com/sharedfiles/filedetails/?id=1279012058) | Mehni | `mehni.pickupandhaul` | 2025-08-28 | — |
| 244 | [Pick Up And Haul - Shared Storage Access](https://steamcommunity.com/sharedfiles/filedetails/?id=3774922633) | 79193 | `sugarcube.puahsharedstorage` | 2026-10-11 | — |
| 129 | [Planetsmith](https://steamcommunity.com/sharedfiles/filedetails/?id=3773634615) | aspctt | `aspctt.planetsmith` | 2026-10-17 | yes |
| 130 | [Please use the pews ! (Ritual Seats Range Fix)](https://steamcommunity.com/sharedfiles/filedetails/?id=3792610727) | Reqo | `reqo.pleaseusethepews` | 2026-09-06 | — |
| 131 | [Power armor has vacsuit vacuum resistance](https://steamcommunity.com/sharedfiles/filedetails/?id=3534065822) | bazoka81 | `ucp.vacpowerarmor` | 2026-10-12 | yes |
| 132 | [Power Utility Tab](https://steamcommunity.com/sharedfiles/filedetails/?id=3544059637) | Hope | `hope.powerutil` | 2025-08-13 | — |
| 133 | [Prestige Vacsuit Set](https://steamcommunity.com/sharedfiles/filedetails/?id=3529096012) | Kilo | `kilo.prestigevacset` | 2026-10-15 | yes |
| 290 | [Prison Labor](https://steamcommunity.com/sharedfiles/filedetails/?id=1899474310) | Avius, Hazzer | `avius.prisonlabor` | 2026-02-12 | — |
| 298 | [Prisoner Realism](https://steamcommunity.com/sharedfiles/filedetails/?id=3760196312) | Legator | `legator.prisonerrealism` | 2026-09-25 | — |
| 134 | [Quiet Wonders](https://steamcommunity.com/sharedfiles/filedetails/?id=3822357701) | choco | `choco.quietwonders` | 2026-10-25 | yes |
| 135 | [Rails and Roads of the Rim (Continued)](https://steamcommunity.com/sharedfiles/filedetails/?id=3271115410) | Vampiresbane | `mlie.railsandroadsoftherim` | 2026-09-14 | — |
| 136 | [Readable Numbers](https://steamcommunity.com/sharedfiles/filedetails/?id=3762441264) | 7h0m4s | `7h0m4s.rimworldreadablenumbers` | 2026-10-26 | yes |
| 137 | [Real Ruins](https://steamcommunity.com/sharedfiles/filedetails/?id=1552146295) | mr.teterew | `woolstrand.realruins` | 2026-05-20 | — |
| 138 | [Reminiscent Style Pack](https://steamcommunity.com/sharedfiles/filedetails/?id=3818904844) | Red Hawkfield | `redhawkfield.sstyle` | 2026-10-25 | yes |
| 291 | [Replace Stuff - Continued](https://steamcommunity.com/sharedfiles/filedetails/?id=3526354009) | Uuugggg | `memegoddess.replacestuff` | 2026-10-22 | yes |
| 139 | [Research Commission](https://steamcommunity.com/sharedfiles/filedetails/?id=3822832387) | Marukun | `mrk.researchcommission` | 2026-10-27 | yes |
| 140 | [Research Papers](https://steamcommunity.com/sharedfiles/filedetails/?id=3492739424) | ChaoticEnrico | `chaoticenrico.researchpapers` | 2026-10-17 | yes |
| 245 | [Research Papers Tech Levels (Retexture)](https://steamcommunity.com/sharedfiles/filedetails/?id=3759752068) | Morton | `morton.retextureresearchpaperstechlevels` | 2026-08-12 | — |
| 246 | [Rimatomics++](https://steamcommunity.com/sharedfiles/filedetails/?id=3774907235) | NachoToast | `nachotoast.rimatomicsplusplus` | 2026-10-19 | yes |
| 141 | [Rimdex](https://steamcommunity.com/sharedfiles/filedetails/?id=3794687829) | Evil Eye Guy | `evileyeguy.rimdex` | 2026-10-26 | yes |
| 142 | [Rimefeller](https://steamcommunity.com/sharedfiles/filedetails/?id=1321849735) | Dubwise | `dubwise.rimefeller` | 2026-10-22 | yes |
| 143 | [RimExodus — Seamless World](https://steamcommunity.com/sharedfiles/filedetails/?id=3790423611) | Laurence-042 | `rimexodus.seamlessworld` | 2026-10-26 | yes |
| 144 | [RimHUD](https://steamcommunity.com/sharedfiles/filedetails/?id=1508850027) | Jaxe | `jaxe.rimhud` | 2025-08-15 | — |
| 292 | [RimMarket](https://steamcommunity.com/sharedfiles/filedetails/?id=3817820660) | BenFD | `forti.rimmarket` | 2026-10-26 | yes |
| 299 | [RimMarket — Hospitality Compatibility Patch](https://steamcommunity.com/sharedfiles/filedetails/?id=3820401180) | Inoz\_Labs | `inozlabs.rimmarket.hospitality.compatibility` | 2026-10-15 | — |
| 145 | [RimPacts - Diplomacy Overhaul](https://steamcommunity.com/sharedfiles/filedetails/?id=3762723122) | wowgag | `wowgag.rimpacts` | 2026-10-24 | yes |
| 146 | [RunAndGun - Continued](https://steamcommunity.com/sharedfiles/filedetails/?id=3562365100) | MemeGoddess | `memegoddess.runandgun` | 2026-10-22 | yes |
| 247 | [Scrubdaddy's Bodies](https://steamcommunity.com/sharedfiles/filedetails/?id=3537481455) | ScrubDaddy | `scrubdaddy.bodies` | 2025-08-05 | — |
| 147 | [ScrubDaddy's Hairstyles](https://steamcommunity.com/sharedfiles/filedetails/?id=3536769145) | ScrubDaddy | `scrubdaddy.hairstyles` | 2025-07-28 | — |
| 248 | [Search and Destroy (Continued)](https://steamcommunity.com/sharedfiles/filedetails/?id=3232242247) | MemeGoddess | `memegoddess.searchanddestroy` | 2026-10-13 | yes |
| 249 | [Search and Rescue](https://steamcommunity.com/sharedfiles/filedetails/?id=3796056278) | CH4AcKO3 | `ch4acko3.searchandrescue` | 2026-10-26 | yes |
| 250 | [Simple Cape and Hood Retexture](https://steamcommunity.com/sharedfiles/filedetails/?id=3543386604) | Stokes52 | `stokes.simplehoodcape` | 2025-08-09 | — |
| 148 | [Simple sidearms](https://steamcommunity.com/sharedfiles/filedetails/?id=927155256) | PeteTimesSix | `petetimessix.simplesidearms` | 2025-08-13 | — |
| 251 | [Simple Washing Machine](https://steamcommunity.com/sharedfiles/filedetails/?id=3820234705) | Local mod | `local.washingmachine` | 2026-10-25 | yes |
| 149 | [Smart Farming](https://steamcommunity.com/sharedfiles/filedetails/?id=3220129183) | Owlchemist | `owlchemist.smartfarming` | 2025-07-28 | — |
| 150 | [Smart Medicine - Continued](https://steamcommunity.com/sharedfiles/filedetails/?id=3526251680) | Uuugggg | `memegoddess.smartmedicine` | 2026-10-13 | yes |
| 151 | [Smart Odyssey](https://steamcommunity.com/sharedfiles/filedetails/?id=3522762411) | Sarg Bjornson | `sarg.smartodyssey` | 2026-10-14 | yes |
| 152 | [Smarter Construction](https://steamcommunity.com/sharedfiles/filedetails/?id=2202185773) | dhultgren | `dhultgren.smarterconstruction` | 2026-10-13 | yes |
| 252 | [Smooth Terrain](https://steamcommunity.com/sharedfiles/filedetails/?id=3502765685) | ChaoticEnrico | `chaoticenrico.smoothterrain` | 2026-10-17 | yes |
| 153 | [Spaceship Solar Hull](https://steamcommunity.com/sharedfiles/filedetails/?id=3536171886) | MK | `mk.spaceshipsolarhull` | 2025-07-27 | — |
| 154 | [Spaceship Turret Hull](https://steamcommunity.com/sharedfiles/filedetails/?id=3536128547) | MK | `mk.spaceshipturrethull` | 2025-07-27 | — |
| 155 | [Tameable Entities](https://steamcommunity.com/sharedfiles/filedetails/?id=3781878408) | Pyromann | `pyromann.conditionedentities` | 2026-10-22 | — |
| 253 | [Taste of Vanilla - Bean Turrets](https://steamcommunity.com/sharedfiles/filedetails/?id=3545135569) | IcyCheeseCake | `icc.tov.bt` | 2025-08-09 | — |
| 156 | [Tavern](https://steamcommunity.com/sharedfiles/filedetails/?id=3775694305) | ODs | `ods.tavern` | 2026-08-08 | — |
| 157 | [TD Enhancement Pack - Continued](https://steamcommunity.com/sharedfiles/filedetails/?id=3525414162) | Uuugggg | `memegoddess.tdpack` | 2026-10-22 | yes |
| 254 | [TD Find Lib - Continued](https://steamcommunity.com/sharedfiles/filedetails/?id=3529443295) | Uuugggg | `memegoddess.tdfindlib` | 2026-10-16 | yes |
| 158 | [TDS Bug Fixes - Continued](https://steamcommunity.com/sharedfiles/filedetails/?id=3529433984) | Uuugggg | `memegoddess.tdsbugfixes` | 2026-10-13 | yes |
| 159 | [Tech Level Progression](https://steamcommunity.com/sharedfiles/filedetails/?id=2923367852) | MrHydralisk | `mrhydralisk.techlevelprogression` | 2026-03-25 | — |
| 160 | [Techwear](https://steamcommunity.com/sharedfiles/filedetails/?id=3806038418) | Hyaukyuu | `hyaukyuu.techapparel` | 2026-10-03 | — |
| 161 | [The Cosmic Odyssey - Accessory Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3758010444) | Akarthus | `akarthus.thecosmicodyssey.accessoryexpanded` | 2026-10-17 | yes |
| 162 | [The Dead Man's Switch](https://steamcommunity.com/sharedfiles/filedetails/?id=3819799478) | AobaKuma, Bread Mo and 3 more | `aoba.deadmanswitch.core` | 2026-10-26 | yes |
| 255 | [The Dead Man's Switch - More weapon sounds](https://steamcommunity.com/sharedfiles/filedetails/?id=3600522698) | Senilia | `senilia.deadmanswitch.moreweaponsounds` | 2026-10-27 | yes |
| 256 | [Tilled Soil](https://steamcommunity.com/sharedfiles/filedetails/?id=725747149) | Sam | `gt.sam.tilledsoil` | 2025-07-19 | — |
| 257 | [Trains of the Rim](https://steamcommunity.com/sharedfiles/filedetails/?id=3530446424) | PrinceOfPluto | `princeofpluto.vehicles.trains` | 2025-08-26 | — |
| 163 | [True Non-Senescence](https://steamcommunity.com/sharedfiles/filedetails/?id=3547377353) | Lunar Dawn | `lunardawn.truenonsenescence` | 2026-09-30 | — |
| 258 | [TSIND Garage Doors QA](https://steamcommunity.com/sharedfiles/filedetails/?id=3547665095) | TSIND | `tsind.garagedoors.qa` | 2026-10-13 | yes |
| 164 | [Tucked Furniture](https://steamcommunity.com/sharedfiles/filedetails/?id=3808739466) | Keno | `keno.tuckedfurniture` | 2026-10-22 | yes |
| 165 | [Tunnelers ARE deep drills](https://steamcommunity.com/sharedfiles/filedetails/?id=3521193199) | MrKociak | `mrkociak.biotechtunnelersaredeepdrills` | 2025-09-11 | — |
| 166 | [Turn It On and Off - RePowered](https://steamcommunity.com/sharedfiles/filedetails/?id=2228219259) | Mlie | `mlie.turnonoffrepowered` | 2026-09-07 | — |
| 167 | [Ultratec Passenger Shuttle](https://steamcommunity.com/sharedfiles/filedetails/?id=3551564384) | Barbed wire | `barbedwire.ultratecpassengershuttle` | 2025-09-02 | — |
| 168 | [UniversalGeneCompiler](https://steamcommunity.com/sharedfiles/filedetails/?id=3787321674) | 松鼠 | `chezhou.universalgenecompiler` | 2026-10-26 | yes |
| 169 | [Use Bedrolls - Continued](https://steamcommunity.com/sharedfiles/filedetails/?id=3534169351) | Uuugggg | `memegoddess.usebedrolls` | 2026-10-13 | yes |
| 259 | [Useful Marks](https://steamcommunity.com/sharedfiles/filedetails/?id=3506573327) | Andromeda | `andromeda.usefulmarks` | 2026-10-22 | yes |
| 260 | [Ushankas Biological Warfare](https://steamcommunity.com/sharedfiles/filedetails/?id=3221550806) | Ushanka | `ushanka.biologicalwarfare` | 2025-12-30 | — |
| 170 | [Ushankas Glittertech Expansion](https://steamcommunity.com/sharedfiles/filedetails/?id=3522676478) | Ushanka | `ushanka.glittertechexpansion` | 2026-04-04 | — |
| 171 | [Ushankas Luciferium Expansion](https://steamcommunity.com/sharedfiles/filedetails/?id=3227008885) | Ushanka | `ushanka.luciferiumexpansion` | 2025-07-21 | — |
| 172 | [Ushankas Necroa Archovirus](https://steamcommunity.com/sharedfiles/filedetails/?id=3531035748) | Ushanka | `ushanka.necroaarchovirus` | 2025-07-21 | — |
| 173 | [VacSlipsuit](https://steamcommunity.com/sharedfiles/filedetails/?id=3536829054) | Kilo | `kilo.vacslipsuit` | 2026-10-15 | yes |
| 177 | [Vanilla Expanded: Hero Backgrounds](https://steamcommunity.com/sharedfiles/filedetails/?id=3313700572) | ferny | `ferny.vanillaexpandedherobackgrounds` | 2026-10-12 | yes |
| 190 | [Vanilla Genes Rebalanced](https://steamcommunity.com/sharedfiles/filedetails/?id=2905707100) | RedMattis | `redmattis.vanillagenesrebalanced` | 2025-06-21 | — |
| 192 | [Vanilla Hair Retextured](https://steamcommunity.com/sharedfiles/filedetails/?id=2748834409) | Neronix17 | `neronix17.retexture.vanillahair` | 2025-07-10 | — |
| 265 | [Vanilla Nutrient Paste Expanded: Reimagined Progression](https://steamcommunity.com/sharedfiles/filedetails/?id=3530651481) | MrHydralisk | `mrhydralisk.vnpereimaginedprogression` | 2026-06-28 | — |
| 266 | [Vanilla Outposts Expanded: Additional Outposts](https://steamcommunity.com/sharedfiles/filedetails/?id=2873841790) | MrHydralisk | `mrhydralisk.voeadditionaloutposts` | 2025-08-19 | — |
| 293 | [Vanilla Outposts Expanded: Power Grid](https://steamcommunity.com/sharedfiles/filedetails/?id=2915686437) | MrHydralisk | `mrhydralisk.voepowergrid` | 2025-07-18 | — |
| 267 | [Vanilla Outposts Expanded: Prisoner Patch](https://steamcommunity.com/sharedfiles/filedetails/?id=3002936071) | MrHydralisk | `mrhydralisk.voeprisonerpatch` | 2025-07-18 | — |
| 18 | [Vehicle Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=3014915404) | Smash Phil | `smashphil.vehicleframework` | 2025-10-21 | — |
| 275 | [VFE Factories: Buffs and tweaks](https://steamcommunity.com/sharedfiles/filedetails/?id=3708068232) | Kimoju | `kimo.vfef.rebalance` | 2026-06-03 | — |
| 212 | [Visible Wealth](https://steamcommunity.com/sharedfiles/filedetails/?id=3461137081) | 1trickPwnyta | `visiblewealth.1trickpwnyta` | 2025-09-06 | — |
| 213 | [Visit Settlements](https://steamcommunity.com/sharedfiles/filedetails/?id=3535955435) | Nina.Goblin | `ninagoblin.visitsettlements` | 2026-10-22 | yes |
| 294 | [VNPE RP Retexture](https://steamcommunity.com/sharedfiles/filedetails/?id=3534603208) | MO, TAWM | `vnpe.rp.retextur.mo` | 2025-07-29 | — |
| 295 | [VTOL Raids](https://steamcommunity.com/sharedfiles/filedetails/?id=3821646218) | potemkinvillager | `potemkinvillager.vtolraids` | 2026-10-18 | yes |
| 214 | [VTOL Vehicle](https://steamcommunity.com/sharedfiles/filedetails/?id=3730919989) | rabiosus | `rabiosus.vtolvehicle` | 2026-06-30 | — |
| 296 | [VVE - Drop Pod Orbital Upgrade](https://steamcommunity.com/sharedfiles/filedetails/?id=3549530925) | amadejj5000 | `amadejj5000.vvedroppodorbitalupgrade` | 2025-08-16 | — |
| 276 | [Wall-Mounted Function Furniture](https://steamcommunity.com/sharedfiles/filedetails/?id=3807382130) | Local mod | `wallmounted.functionfurniture` | 2026-10-13 | yes |
| 215 | [Warehouse Storage](https://steamcommunity.com/sharedfiles/filedetails/?id=3519963835) | vin | `vin.warehouse.storage` | 2025-07-12 | — |
| 216 | [What's Missing? - Forked](https://steamcommunity.com/sharedfiles/filedetails/?id=3781470232) | Revolus | `nachotoast.revolus.whatsmissing` | 2026-10-15 | yes |
| 217 | [Who shot my leg off?](https://steamcommunity.com/sharedfiles/filedetails/?id=3491552121) | Tixiv | `tixiv.whoshotmylegoff` | 2025-06-13 | — |
| 277 | [Worldbuilder](https://steamcommunity.com/sharedfiles/filedetails/?id=3522102833) | ferny | `ferny.worldbuilder` | 2026-10-24 | yes |
| 12 | [XML Extensions](https://steamcommunity.com/sharedfiles/filedetails/?id=2574315206) | Imranfish | `imranfish.xmlextensions` | 2026-06-18 | — |
<!-- END GENERATED: active -->

## Subscribed but switched off

<!-- BEGIN GENERATED: inactive -->
| Mod | Author | Package ID | Updated | 1.7 |
| --- | --- | --- | --- | --- |
| [\[KCB\] Mini Expeditions](https://steamcommunity.com/sharedfiles/filedetails/?id=3823866055) | Kimchibok | `kimchibok.expedition` | 2026-10-27 | yes |
| [CeleTech Arsenal - Shuttle Extension](https://steamcommunity.com/sharedfiles/filedetails/?id=3760923537) | LongRange, 灵能罐头 | `longrange.celetech.shuttleextension` | 2026-09-07 | **no** |
| [Empire Refactored](https://steamcommunity.com/sharedfiles/filedetails/?id=3701480464) | Matathias | `matathias.empire` | 2026-09-11 | **no** |
| [Graphics Settings](https://steamcommunity.com/sharedfiles/filedetails/?id=1678847247) | Telefonmast | `telefonmast.graphicssettings` | 2026-10-27 | yes |
| [Humvee (Continued)](https://steamcommunity.com/sharedfiles/filedetails/?id=3764661857) | ARCA-RUS | `mlie.humvee` | 2026-07-14 | **no** |
| [MultiFloors](https://steamcommunity.com/sharedfiles/filedetails/?id=3384660931) | telardo | `telardo.multifloors` | 2025-09-27 | **no** |
| [Prepatcher](https://steamcommunity.com/sharedfiles/filedetails/?id=2934420800) | Zetrith | `zetrith.prepatcher` | 2026-02-04 | **no** |
| [Project RimFactory - Insanity](https://steamcommunity.com/sharedfiles/filedetails/?id=2037492036) | zymex | `com.spdskatr.projectrimfactory.insanity` | 2025-07-24 | **no** |
| [Project RimFactory Revived](https://steamcommunity.com/sharedfiles/filedetails/?id=2033979700) | zymex | `spdskatr.projectrimfactory` | 2026-04-01 | **no** |
| [Project RimFactory Revived Retextured](https://steamcommunity.com/sharedfiles/filedetails/?id=2883894160) | Aranmaho | `aranmaho.prf.retexture` | 2025-07-30 | **no** |
| [RimFactory Neceros Patch (Continued)](https://steamcommunity.com/sharedfiles/filedetails/?id=2862241497) | Neceros | `mlie.rimfactorynecerospatch` | 2025-07-25 | **no** |
| [RimShips](https://steamcommunity.com/sharedfiles/filedetails/?id=3689132119) | Neural Cat | `neuralcat.rimships` | 2026-08-19 | **no** |
| [Semi Random Research:P-Fork Continued](https://steamcommunity.com/sharedfiles/filedetails/?id=3778002533) | Cruesoe, Captain Muscles (Nathan Harbour) and 1 more | `cruesoe.semirandompfork.continued` | 2026-10-26 | yes |
| [Vanilla Trading Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2785616901) | Oskar Potocki, Taranchuk | `vanillaexpanded.vanillatradingexpanded` | 2026-10-14 | yes |
<!-- END GENERATED: inactive -->
