# Third-party provenance

**The repository contains no third-party code, art, sound or data** (first recorded 18 October 2026, checked again 6 November
2026). It holds only original Phobos mods, documentation, scripts and tests. This file records what each mod takes from
elsewhere: which other mods' assemblies it compiles against, which mods' Defs it patches by name, and on what terms. Nothing
listed here is copied into this repository or a build.

## RimWorld

[RimWorld](https://rimworldgame.com/) and its expansions are made by **Ludeon Studios**. Nothing from the
game is redistributed here. Our records cite facts read from the installed game's Defs and code, such as the comms
console's power draw in the [radio reference checks](docs/development/radio-reference-checks.md); citing a
value confers no ownership of the game's files. Mods follow [Ludeon's EULA](https://rimworldgame.com/eula/):
free, clearly community-created, and not official or endorsed by Ludeon. Builds compile against the reference assemblies
`Krafs.Rimworld.Ref` publishes on NuGet, not against the game's own files.

## Harmony

Five of our assemblies patch the game or another mod through **Harmony** by **Andreas Pardeike (brrainz)**:
[GitHub](https://github.com/pardeike/Harmony) (MIT licence), Workshop item 2009463077. They are Factory Additions (and its Dubs
Bad Hygiene tie-in), Medical Save Bridge, Vehicle Patches' Harrier tie-in and Burns and Smoke. They reference it at build time
only (`Lib.Harmony` 2.4.3 for RimWorld 1.6, 3.0.0-rc.4 for 1.7, `ExcludeAssets="runtime"`), so `0Harmony.dll` never ships;
players get it from the Harmony mod, which Burns and Smoke, Factory Additions and Medical Save Bridge declare as a dependency.

## Vanilla Expanded Framework

[Vanilla Expanded Framework](https://github.com/Vanilla-Expanded/VanillaExpandedFramework) by **Oskar Potocki and the Vanilla
Expanded team** (Workshop item 2023507013, `packageId` `OskarPotocki.VanillaFactionsExpanded.Core`) is a declared dependency of
D'thorga Radio Framework, Factory Additions and Medical Equipment. The radio framework's feedline and Factory Additions' piped
water run on its **PipeSystem** module: those assemblies compile against the installed `PipeSystem.dll` without bundling it.
Medical Equipment uses VEF's refuelable pipe linking through its Defs only.

VEF's licence (its `LICENSE` file, read 19 October 2026) is **Creative Commons BY-NC-ND 4.0** with riders.
The relevant rider: "Modders that create mods that depend on this framework (through marking this mod as
a dependency) are expressly permitted to create commissioned mods with commercial advantages or monetary
compensation, as well as accept donations on their mods, provided all other terms of the Licence are met."
The NoDerivatives term means we **never copy or adapt** VEF's code, DLLs, art or XML, and never
redistribute them; we only reference the installed assembly and subclass or call its public types at
runtime. Nothing from VEF is in this repository. Our own code remains MIT.

## Vanilla Furniture Expanded - Factory

D'thorga Factory Additions (`mods/PhobosFactoryAdditions/`) exists to extend
[Vanilla Furniture Expanded - Factory](https://steamcommunity.com/sharedfiles/filedetails/?id=3686924415) by
**Oskar Potocki and the Vanilla Expanded team** (`packageId` `VanillaExpanded.VFEFactory`), a declared
dependency. Its [GitHub repository](https://github.com/Vanilla-Expanded/VanillaFurnitureExpanded-Factory) and
its Workshop folder carry **no licence** (checked 29 October 2026), so nothing from it may be copied. We patch
its Defs by name in XML, read its decompiled assembly for research (kept in the ignored `.local/`), and since
0.6.0 compile against its installed `VanillaFurnitureExpandedFactory.dll` without bundling it, to patch and call
its conveyor belt at runtime. At runtime the storage switch also shows VFE Factory's own belt icon, read from its
installed Def; the image is never copied. D'thorga Water Tap's optional tie-in (`Mods/VFEFactory`) marks its bottle tap as a
factory hopper in XML. Nothing from VFE Factory is in this repository.

## Dubs Bad Hygiene

[Dubs Bad Hygiene](https://steamcommunity.com/sharedfiles/filedetails/?id=836308268) by **Dubwise** (no licence stated in
its Workshop folder). D'thorga Factory Additions' optional tie-in assembly references the installed `BadHygiene.dll` to draw
water from its plumbing, and loads only while that mod is active. D'thorga Water Tap requires it, references the same installed
`BadHygiene.dll` to fill bottles from its plumbing, and adds a comp to its water bottle in XML. Nothing from Dubs Bad Hygiene
is in this repository.

## More Injuries (Continued) and More Injuries: Organ Failure

[More Injuries (Continued)](https://steamcommunity.com/sharedfiles/filedetails/?id=3348840185) by **Th3Fr3d** (Frederik Hoeft;
[source on GitHub](https://github.com/frederik-hoeft/rimworld-more-injuries), `packageId` `Th3Fr3d.ExtendedInjuries`) is
**GPL-3.0**. Our mods reach it by name only:

- **Medical Save Bridge** finds one of its types by name at runtime.
- **Burns and Smoke** patches its Defs in XML (adding its public secondary-condition handlers to our own conditions, and removing
  one worker from its registration list), and gates the tracheotomy behind its Emergency Medicine research.
- **Medical Equipment** marks one of its conditions as helped by oxygen, in XML.
- **Triage** finds its operations by the condition each recipe removes, as it finds any mod's, and never names it.

Its decompiled assembly and published source were read for research in the ignored `.local/` and summarised in our own words.
**Nothing from More Injuries is in this repository, and none of its code is adapted.**
[More Injuries: Organ Failure](https://steamcommunity.com/sharedfiles/filedetails/?id=3733035523) by wenders was read the same
way as the worked example of a third mod driving those handlers; nothing from it is here.

## Combat Extended

[Combat Extended](https://steamcommunity.com/sharedfiles/filedetails/?id=2890901044) by the **Combat Extended team**
(`packageId` `CETeam.CombatExtended`; its README names the Creative Commons BY-NC-SA 4.0 licence, read 4 November 2026).
No assembly of CE's is referenced, and nothing from it is copied or adapted. Its decompiled assembly was read for research in
the ignored `.local/`.

- **D'thorga Framework** reads the public setting `Controller.settings.SmokeEffects`, the condition `SmokeInhalation` and the
  stat `SmokeSensitivity` by name at runtime.
- **Burns and Smoke** patches one of its Defs (`BurnSecondary`) and adds a tooltip comp to its smoke inhalation, in XML.
- **Medical Equipment** marks its smoke inhalation as helped by oxygen and its burns as burns for the burn unit bed, in XML.
- **Factory Additions** adds a medicine granulator bill for CE's psycho in XML.
- **Vehicle Patches** sets Combat Extended values on the Harrier's bombs and on Spaceship Turret Hull's turrets, in XML.

## Vehicle mods patched by D'thorga Vehicle Patches

D'thorga Vehicle Patches (`mods/PhobosVehiclePatches/`) changes values by name in XML and copies nothing:

- [Harrier VTOL Attack Aircraft](https://steamcommunity.com/sharedfiles/filedetails/?id=3826359341) by potemkinvillager, with parts
  from [Vanilla Vehicles Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3014906877) by Oskar Potocki and the
  Vanilla Expanded team and [RimThunder - Core](https://steamcommunity.com/sharedfiles/filedetails/?id=3070495204) by the
  RimThunder team;
- [Spaceship Turret Hull](https://steamcommunity.com/sharedfiles/filedetails/?id=3536128547) by MK (`mk.spaceshipturrethull`),
  only while Combat Extended is active;
- the Dead Man's Switch ammunition fabricator and rotary forger, found by Def name when present.

Its Harrier tie-in assembly references the installed `Vehicles.dll`, `SmashTools.dll` and `UpdateLogTool.dll` of
[Vehicle Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=3014915404) by Smash Phil (MIT licence), never
copied, and finds [RimPacts - Diplomacy Overhaul](https://steamcommunity.com/sharedfiles/filedetails/?id=3762723122) by wowgag
by name at runtime. Nothing from any of them is in this repository.

## Mods patched by name in XML

Each of these is touched only by XML in an optional folder that loads while that mod is active (`LoadFolders.xml`
`IfModActive`, or `MayRequire` on one file), naming its Defs. Nothing from them is copied.

| Mod | Author | `packageId` | Our mod and folder | What we add |
| --- | --- | --- | --- | --- |
| [Vanilla Gravship Expanded - Chapter 1](https://steamcommunity.com/sharedfiles/filedetails/?id=3609835606) | Oskar Potocki, Sarg Bjornson and the Vanilla Expanded team | `vanillaexpanded.gravship` | Medical Equipment, `Mods/VanillaGravshipExpanded` | A bedside oxygen stand burning its oxygen canisters |
| [[sbz] Fridge](https://steamcommunity.com/sharedfiles/filedetails/?id=3486264784) | seobongzu | `sbz.NeatStorageFridge` | Factory Additions, `Mods/SbzFridge` | Belts take corpses from its corpse fridge |
| [Medicines+](https://steamcommunity.com/sharedfiles/filedetails/?id=2878860499) | Papito | `Papito.MedicinesPlus.AT` | Factory Additions, `Mods/MedicinesPlus` | Its drug lab recipes as factory bills |
| [Deep Pills](https://steamcommunity.com/sharedfiles/filedetails/?id=3758899296) | ODs | `ODs.DeepPills` | Factory Additions, one file with `MayRequire` | Its infusions as factory bills |
| [Ushankas Biological Warfare](https://steamcommunity.com/sharedfiles/filedetails/?id=3221550806) | Ushanka | `Ushanka.BiologicalWarfare` | Factory Additions, `Mods/BiologicalWarfare` | Vaccines, antiserums and pills as factory bills |
| [Dubs Rimatomics](https://steamcommunity.com/sharedfiles/filedetails/?id=1127530465) | Dubwise | `Dubwise.Rimatomics` | Factory Additions, `Mods/Rimatomics` | Potassium iodide and machining table items as factory bills |
| [Hemofungus](https://steamcommunity.com/sharedfiles/filedetails/?id=3259446830) | NoSleep | `NoSleep.Hemofungus` | Factory Additions, `Mods/Hemofungus` | Hemogen from hemofungus as a factory bill |
| [Complementary Odyssey](https://steamcommunity.com/sharedfiles/filedetails/?id=3546612303) | MrHydralisk | `MrHydralisk.ComplementaryOdyssey` | Factory Additions, `Mods/ComplementaryOdyssey` | Vacsop as a factory bill |
| [Glitterworld Medicine Production](https://steamcommunity.com/sharedfiles/filedetails/?id=2885153505) | MrHydralisk | `MrHydralisk.GlitterworldMedicineProduction` | Factory Additions, `Mods/GlitterworldMedicine` | Glitterworld medicine as a factory bill |
| [Farmable Neutroamine](https://steamcommunity.com/sharedfiles/filedetails/?id=3222103279) | kongkim | `kongkim.FarmableNeutroamine` | Factory Additions, `Mods/FarmableNeutroamine` | Neutroamine from its leaves as a factory bill |
| [Passive Exoskeletons](https://steamcommunity.com/sharedfiles/filedetails/?id=3807490214) | Agent_Orange | `AgentOrange.PassiveExoskeletons` | Factory Additions, `Mods/PassiveExoskeletons` | Polyamide as a factory bill |
| [[DG] Simple Washing Machine](https://steamcommunity.com/sharedfiles/filedetails/?id=3820234705) | as listed on its Workshop page | `local.washingmachine` | Factory Additions, `Mods/WashingMachine` | Detergent as a factory bill |
| [Vanilla Cooking Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2134308519) | Oskar Potocki, Sarg Bjornson and the Vanilla Expanded team | `VanillaExpanded.VCookE` | Factory Additions, `Mods/VanillaCookingExpanded` | Its soups as conveyor oven bills |
| [Vanilla Chemfuel Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2792917473) | Oskar Potocki, Kikohi | `VanillaExpanded.VChemfuelE` | Factory Additions, `Mods/VanillaChemfuelExpanded` | Piped chemfuel for factory bills |
| [Vanilla Races Expanded - Android](https://steamcommunity.com/sharedfiles/filedetails/?id=2975771801) | the Vanilla Expanded team | `vanillaracesexpanded.android` | Factory Additions, `Mods/VanillaRacesExpandedAndroid` | Piped neutroamine for factory bills |

Mods named only in a load order (`loadAfter`), with nothing patched: Search and Rescue by CH4AcKO3, Hospitality (Continued) by
Zaljerem, Medical System Expansion 2 by JonM0, and VFE Factories: Buffs and tweaks by Kimoju.

## Mods named in the designs

The radio framework design plans optional tie-ins with other authors' mods, including
[Surveillance Shift](https://github.com/fernyrepos/Surveillance-Shift) by **ferny**, Combat Extended,
Vehicle Framework and the Vanilla Expanded mods. A tie-in calls another mod's public behaviour at runtime
only when that mod is active. It never copies the mod's code, art or data. Surveillance Shift's repository
has no licence file (checked 18 October 2026), so nothing from it may be copied. Every tie-in built so far is
recorded above; each new one records here what it uses and on what terms.
