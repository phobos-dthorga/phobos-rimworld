# What writing the modding guides found, and the owner's choices (28 Nov 2026)

**Status (28 Nov 2026):** decided and built (Medical 0.38.0, Framework 0.371.0, Radio Framework 0.61.0, and the reference site);
not yet tested in game (rows 753 to 758). Each card's options are recorded below as they were put, the owner's choice first and
marked, so a later change of mind starts from the same list (owner, 26 Nov 2026: "Document the other possibilities before they are
lost").

On 28 Nov 2026 every XML-facing type of the D'thorga mods got a section in a modding guide (the guides' changelog lines and
[the reference site's record](reference-site.md)). The agents that drafted them from the code found behaviour that disagreed with its
own summary, fields nothing read, and mistakes that load without an error. The owner asked for all of it to be dealt with ("possible
issues or not"), and answered sixteen question cards. Two faults found on the way were fixed before the cards: the fuel heater and
the vehicle solar panel were never offered in the fittings window (Framework 0.368.1), and the anaesthesia machine's bellows never
sounded (Medical 0.37.1).

## Behaviour in play

| Card | Found | Options (the owner's choice first) |
| --- | --- | --- |
| **Sound loop** | A looping vehicle sound (Effect_SoundLoop) given a `when` may never stop once `when` turns false mid-stage: the loop is stopped only when the stage ends, a step skipped while `when` is false. No shipped system does this | **Chosen: fix it to stop**: the loop ends as soon as `when` turns false and starts again when it turns true. Not chosen: a guide warning only (use `whileEngineRunning`) |
| **Heater draw** | The Dubs Bad Hygiene water heater on a set-out trailer adds its 300 W the whole time it is set out, even switched off or not heating | **Chosen: only while heating**, as a thermostat heater would. Not chosen: while switched on (a simple standby model); keep as now |
| **Shed comforts** | The cab stereo and reading light have shed rank 0 ("essential, never shed"), so they keep draining a failing battery while the heater is cut | **Chosen: shed them first** (the highest rank: a real low-voltage cut-off drops entertainment first). Not chosen: keep them never shed |
| **Garage CO** | The "figures only" setting also stops an engine filling a closed garage with carbon monoxide (Effect_PushGas counts as a consequence); nothing said it was deliberate | **Chosen: keep stopping it**, now recorded as deliberate: poisoned colonists are a consequence. Not chosen: gas still flows under "figures only", as part of the gas layer |

## Code tidiness and silent mistakes

| Card | Found | Options (the owner's choice first) |
| --- | --- | --- |
| **Unread feed** | Only the rescue hoist, recovery winch and cabin oxygen read a fitting's `feed`. Six fittings carry one nothing reads (the route kit, the uncrewed kit, the mech link; the Radio's aviation, marine and dual-band radios); their code checks the load by name | **Chosen: make them read `feed`**, so one place says what each fitting needs and the window and refusals show it alike. Not chosen: drop `feed` from those six; leave as is |
| **Duplicate condition** | When_Granted and When_FittingGrants are the same test; When_Granted carries a stray summary belonging to another condition | **Chosen: keep both, name one**: fix the summary, and the guide recommends When_FittingGrants and keeps When_Granted so nobody's XML breaks. Not chosen: remove When_Granted |
| **Load checks** (several could be picked) | XML mistakes that load with no error | **Chosen, all four**: a misspelt setting name in When_Setting or Input_Setting (reads "off"); an unknown Input_EngineFeed `reading` (reads oil level); a delivery contract by thing category with no `valuePerUnit` (pays 0); a second default part grade, or a second default tune on one axis (the first loaded wins). Each becomes a config error naming the Def |
| **Radiation fields** | PhobosRadiationRulesDef's `courseEndsBelow` (0.2) and `smallestShown` (0.05) are read by no code | **Chosen: wire them in**: radiation sickness ends once it falls below `courseEndsBelow`, and doses below `smallestShown` are not shown (illness ends a little sooner). Not chosen: remove them |

## Medicine and the reference site

| Card | Found | Options (the owner's choice first) |
| --- | --- | --- |
| **Caesium** | The radiation XML pairs a 110-day biological half-life (credited to the Radiogardase label) with a 0.33 speed-up taken from that label's Goiânia figures (about 80 days to 26): about 36 days, mixing two baselines | **Chosen: one source for both**: read the label's own text first, then take the half-life with and without Prussian blue from the same figures, labelled with the source; any unverified figure stays marked unverified. Not chosen: keep as authored, labelled as an approximation |
| **Deepening** | HediffCompProperties_PhobosChemicalDeepening on any other condition deepens every marked acid burn on the person, not just its own | **Chosen: in code, only its own burn**. Not chosen: the guide tells modders not to use it |
| **Oxygen speed** | Supplied oxygen speeds the clearing of any condition carrying HediffCompProperties_PhobosHalfLife, not only carbon monoxide | **Chosen: a per-condition switch**, on for carbon monoxide, off by default for anything else. Not chosen: any condition, as now |
| **Site hint** | The reference site tells modders to put the standby and patient comps "in a ThingDef's comps", but the Framework adds both in code | **Chosen: teach the site**: a list in its config of comps added in code, whose pages say "added for you, never write it" (all the auto-added comps, not only these two). Not chosen: leave it |

## Vehicles' quieter corners

| Card | Found | Options (the owner's choice first) |
| --- | --- | --- |
| **Rear half** | An articulated vehicle's front half with no hitch (switched off in the settings, or `noHitch`) never makes its rear half, silently | **Chosen: warn in the log**, naming the vehicle and why. Not chosen: a guide note only |
| **Reservoirs** | CompProperties_PhobosReservoir's summary promises any building can have one, but pipe filling works only on set-out pieces, only the paste tap draws from one, and PhobosNodeGraphicExtension is read only by the paste tap | **Chosen: in code, any building**: filling, drawing and the picture work on any building with the comp. Not chosen: narrow the summary and guide |
| **Medical faults** | Read from code, unconfirmed: the Dubs water feed draws water whenever its building is on and powered, even idle; the rise-then-fall comp on another mod's condition might set severity to 0 at its first check | **Chosen: check both, fix if real**, with a changelog entry, or report if not. Not chosen: leave them in this record only |
| **Rare fields** | The vehicle systems guide names seven loadout fields with no rows (`cruise`, `engineOutYaw`, `engineArms`, `sideTanks`, `heldMassComps`, `cellsOfRemap`, `fittedOnOlderSaves`) and thin rows for `empHardening` and `intakeDebris` | **Chosen: write them now**. Not chosen: later |

## The second round: what building the first round decided (28 Nov 2026)

Choices the agents made while building the cards above, put to the owner once it was pushed.

| Card | Found | Options (the owner's choice first) |
| --- | --- | --- |
| **Small doses** | With `courseEndsBelow` wired in, an illness whose height never rises above it (roughly under 0.7 to 1 Gy) rose to its height and vanished at once, instead of easing off over about four days | **Chosen: ease off, then end**: the line ends only an illness that was once worse than it; a small one eases off as before. Not chosen: vanish at its height (as first built); lower the line (say 0.1) |
| **Reservoir draw** | "Drawing from it on any building" was built as topping up a building's own CompRefuelable that burns the reservoir's item; eating still needed the paste tap's class or C# | **Chosen: also let people eat from it**: any reservoir holding a meal-like item serves meals to colonists, on any building. Not chosen: keep as built (refuelling only) |
| **Older saves** | `fittedOnOlderSaves` settles each vehicle once by one saved mark, which every vehicle saved since Framework 0.208.0 has, so an entry added now reaches almost none | **Chosen: a per-fitting record**, as `factoryFittings` keeps, so a new entry reaches every existing vehicle once. Not chosen: document it only; retire the field (kept readable) |
| **engineArms** | A list whose length does not match the bound engine roles is ignored without a word | **Chosen: a config error naming the Def**, shipped loadouts checked first. Not chosen: leave it to the guide's warning |
| **Own deepening** | Building "only its own burn", the agent chose: the burn's own severity rises (a fresh wound would carry the effect and double every hour), the body part is never taken (it stops at the last hit point), and it stops when tended or scarred | **Chosen: let it take the part**: a burn left untreated long enough may destroy the body part, as a real untreated chemical burn can; the rest as built. Not chosen: keep as built (stop at the last hit point) |

Building "also let people eat from it" found that the game's food search counts a building as a food source only when its class is
exactly the game's nutrient paste dispenser, so (read from code, not seen in play) colonists probably never ate from the Billy's
paste tap, a subclass (test row 453 watches for it). The owner answered a further card:

| Card | Found | Options (the owner's choice first) |
| --- | --- | --- |
| **Paste taps** | Eating from a reservoir needs a Harmony patch either way, and the shipped paste tap needs one too | **Chosen: one small patch**: a postfix on the game's food-source group test also counts subclasses of the dispenser, so the Billy's tap and any modder's building of our paste-tap class serve meals, in XML alone; it runs when a thing is registered, not on every food search. Not chosen: a meal set out on the doorstep with no patch (haulers carry it off, it rots, animals eat it); a full set of about eight patches so any class serves (fragile, one in a hot path); check in play first and build nothing yet |

## The modders' documentation round (28 Nov 2026)

The owner asked for every improvement offered to the docs for other programmers ("Are there any other ways we can enhance the modding
documentation"), all eight chosen: an example mod, a first-hour walkthrough, an extension points index, a stability page, the 33 missing
summaries, the patches and dev actions pages, the XML checker for modders and an editor schema prototype. Building them raised these
cards:

| Card | Found | Options (the owner's choice first) |
| --- | --- | --- |
| **Setting names** | Other modders' XML names our settings (When_Setting) | **Chosen: stable, like saved keys**: never renamed; a retired one keeps answering "off" rather than a config error. Not chosen: stable only from release; no promise |
| **Pre-release renames** | Nothing settled whether names may change before release | **Chosen: yes, with a save conversion** and a "For modders" changelog line, frozen from release. Not chosen: frozen now |
| **Obsolete** | How long an obsolete member stays | **Chosen: until the next version raising the first number**, which removes them all at once with a list. Not chosen: forever; six months |
| **Checker home** | The checker's generic parts (a .NET assembly reader, RimWorld's XML readers) sat inside the one script | **Chosen: move them to phobos-modkit, with a step writing one file** for modders. Not chosen: keep them in the script |
| **Publish the schema** | The editor schema has no false errors on our files but cannot follow `Class` | **Chosen: after the owner tries it** in VS Code. Not chosen: publish now, labelled untested; shelve it |
| **Editor tools** | Garethp's Rider plugin (MIT) and the RWXML language server (Apache-2.0) follow `Class` | **Chosen: mention both** in "Your first hour", with no endorsement implied. Not chosen: the Rider plugin only; neither |
| **Dev summaries** | The dev actions page showed only a summary's first sentence | **Chosen: show whole summaries**. Not chosen: first sentence only |
| **Housekeeping** (several could be picked) | An empty `K:\tmp-xsd-research` made by mistake; the schema's XSD 1.1 test skipped without the `xmlschema` package | **Chosen, both**: the folder to be deleted (the safety check refused an agent's removal, so it is the owner's), and the owner installed `xmlschema`, whose test then showed strict validators refuse the schema |

## Smaller findings dealt with without a card (agent's choices, owner to revise)

- The Radio's Architect Def comment said the Radio "does not need D'thorga Framework"; it has declared it since Radio 0.4.0. The comment
  is corrected.
- PhobosEnforceUpgradeResearch has done nothing since Framework 0.137.2. It stays, so XML naming it still loads; the guide calls it
  retired.
