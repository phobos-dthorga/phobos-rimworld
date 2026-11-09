# Animating our vehicles: research (9 Nov 2026)

**A research record. Nothing is built; nothing here is tested in game.** The owner asked (9 Nov 2026) whether our vehicles can make use of
animations, how they would work, whether they need several versions of a similar sprite, and whether shaders could do the job instead to
lessen the cost of generated images; then, before any more art is made, where motion would benefit each of our designs.

Facts marked **from the code** were read on 9 Nov 2026 from our decompiles and summarised in our own words, never copied: **VF** is Vehicle
Framework's 1.6 build (`.local/decompiled/VehicleFramework-1.6/Vehicles.decompiled.cs`; the installed item has no 1.7 folder, so the 1.7 game
loads this build), **GAME** is RimWorld 1.7 (`.local/decompiled/full/all.cs`), **FWAF** and **HAR** are the Harrier's two assemblies
(`.local/decompiled/FixedWingAircraftFramework/`, `.local/decompiled/AV8BHarrier-dll/`). Workshop XML was read from the installed items. Two
things are **inferred, not read**: SmashTools (where VF's animator lives) and Vanilla Chemfuel Expanded's DLL are not decompiled.

## Correction, 9 Nov 2026: what a rotation can do from straight above

The first version of this record said a boom or mast could "raise on a hinge" from one picture. **It cannot.** The game is seen from straight above, and an overlay's `Transform.rotation` turns it flat
across the ground, like a clock hand: it can swing a part out to the side, but it can never lift it towards the camera. Sliding works the same way, flat. **A true raise would need drawing the part shorter as it rises, which an overlay's rotation
and position cannot do**: it means drawing the part ourselves in `PostDraw` with a scaled matrix, as the Harrier draws its body (FWAF:13796-13809). Whether SmashTools' `Transform` has a scale of its own is unread. **The owner's choice for the survey truck's
boom (9 Nov 2026): it swings out to the side.** Built in Framework 0.51.0: `SetUpMovingPart`, a hinge about a pivot measured off the art, with `HingeRule` and its tests. The rig's mast and the tipper's body in the table below still need this choice.

**Not clipping (owner, 9 Nov 2026: "try and ensure that the extendable bits don't clip with other textures").** Before setting up, the sweep of each part is checked on both sides against walls, rock, buildings and other vehicles, and the clearer side is used; with neither clear it
refuses and says so. Trees count only if both sides have them, because refusing in a forest would be worse than a boom over a tree. The power point keeps off the swept cells. **What it cannot stop:** something built or driven under the boom after it is out, and the game has no depth
sorting for a vehicle's overlays, so a part always draws at the truck's height.

## Read this first

1. **Vehicle Framework animates nothing on a ground vehicle by itself.** Everything that moves in it is tied to an aircraft taking off or landing
   (spinning `Graphic_Propeller` and `Graphic_Rotator` overlays, driven only by `PropellerTakeoff`, VF:27061-27109), a turret firing (recoil and
   muzzle motes), or fuel burning (`motesGenerated` exhaust, VF:19617-19630). There is no wheel or track code at all. RimThunder and Trains of the
   Rim ship static sprites. VF's keyframe animator is dormant: a vehicle's `animator` is only ever filled by a save load (VF:14410-14414), and its
   hook is compiled out (VF:14194).
2. **Several versions of a sprite are needed only for frame-by-frame motion, and that route is closed to us anyway:** VF's frame player
   (`Graphic_Animate`, VF:37970-38104) plays only in launch protocols and muzzle motes; as a body overlay it shows its first frame. **Procedural
   motion needs one picture per moving part:** an overlay's `Transform.rotation` and `.position` are public and re-read every frame (VF:6270,
   6413-6416, 75150-75165), so a comp of ours can raise a mast or spin a head with one still image and almost no cost. The VCHE pumpjack works this
   way (three textures: base, bottom and pump; its comp moves the pump and draws it with its own matrix, inferred from its texture names and member
   names), and so does the Harrier (rotors cross-fade a stopped and a spinning texture, FWAF:14062-14151; the nozzle flame is one `MoteGlow` texture
   jittered by noise, HAR:2529-2557). **Two-state looks already work:** `CompPhobosSetUp.ShowSetUp` adds and removes an overlay today.
3. **Shaders instead of frames: possible, and the dearest tool.** The game loads a mod's own shaders from an AssetBundle when a `ShaderTypeDef`'s
   path matches no built-in shader (GAME:24310-24322), one bundle per platform (GAME:96464-96520); VF ships its three RGB shaders exactly so, built
   per game version with the game's Unity (VF:35847-35935). A scrolling tread or a blinking light in a shader would cost nothing to run and no image
   generation, but needs a Unity project at the game's Unity version and a rebuild per game version and platform. **The cheaper route to the same
   saving is procedural C#:** one still image per moving part, moved by a comp. **The game's own shaders need no bundle** and an overlay's
   `graphicData` names one (`MoteGlow`, `MoteGlowPulse`, `TransparentPostLight`, GAME:24124-24190): a pulsing glow is a plain sprite on `MoteGlowPulse`,
   **to be verified at build** on a `GraphicOverlay` material.

**What this means for art:** every moving part is **its own picture, drawn at rest on the body's canvas, with the body drawn without it**, and the
request names the hinge cell. No frame sheets, ever. A paid pilot of a vehicle is the bare body plus a short list of part sprites.

## What Vehicle Framework offers, mechanism by mechanism (from the code)

| Mechanism | What it does | Where | Extra sprites | Our use |
| --- | --- | --- | --- | --- |
| `drawProperties.graphicOverlays` | A layer over the body: `identifier`, `graphicData`, a fixed `rotation`, `dynamicShadows`, `component` | VF:6217-6230 | one set (north, east, south) | Every look we switch |
| The overlay's runtime transform | Each frame the draw angle is `transform.rotation + data.rotation` and `transform.position` is added; the pre-render step runs every frame, so a change shows at once. Rotation is about the overlay's own centre; the draw offset turns only with the body, so a hinge needs its position recomputed | VF:6413-6416, 75150-75165, 38727-38729 | none | **Mast, boom, dish, head, body lift** |
| `AddOverlay` and `RemoveOverlays` | Runtime add and remove; `GraphicOverlay.Create` on the main thread; removing frees the materials | VF:6634-6658, 6462 | one set per state | Two-state looks (built) |
| The `component` gate | An overlay can name a part; the check exists but `RecacheComponent` and the events are only ever wired for turrets, so **the gate is never applied and the overlay always draws** | VF:6402-6410 against 21350 and 63588 | | Why our damage looks are switched by hand on the rare tick |
| `Graphic_Rotator`, `Graphic_Propeller`, `Graphic_ReversePropeller` | Spin through `SetAcceleration`, called only by aircraft take-off and landing | VF:38105-38130, 6660-6667, 27061-27109 | one | Still on a ground vehicle unless our comp drives it; a fan or drum |
| `Graphic_Animate` | A folder of frames by index; indexed only by launch protocols' `additionalTextures` and `MoteCannonPlume`; first frame only as an overlay | VF:37970-38104, 26246-26256, 31511-31610 | VF's own: 7 to 10 | Not for us |
| Turret `recoil` and `vehicleRecoil` | A kick on a shot, per turret and per body | VF:30248-30259, 65869-65873, 75111-75118 | none | Nothing of ours fires |
| Turret `motes` | Muzzle animation properties | VF:35757-35801 | a mote sheet | Nothing of ours fires |
| `CompProperties_FueledTravel.motesGenerated` | A thrown mote every `ticksToSpawnMote` while fuel burns, with an offset and wind | VF:19941-19999, 19617-19630 | one mote texture | Exhaust while driving; no Workshop mod read uses it |
| Launch flecks and dust | Curves in launch-protocol properties | VF:26846-26880, 26955-26988 | none | Aircraft only |
| `VehicleTrack_Wake` | The only track type; boats | VF:7117-7135, 7345-7364 | none | No boat of ours |
| Shaders | `CutoutComplexRGB`, `CutoutComplexPattern`, `CutoutComplexSkin` from VF's bundles, per game version | VF:51077-51089, 35847-35935 | | The tanker's paint; `PatternDef` is a static pattern |
| The keyframe animator | Clips ship with VF (`Animations/*.rwa`); nothing creates an animator at run time; the draft hook is `[Conditional("ANIMATOR")]` | VF:13499, 14410-14414, 14194 | | Dormant: not used |
| Hooks for our C# | A vehicle comp's `PostDraw` runs every frame (`DynamicDrawPhaseAt` calls `Comps_PostDraw`); `GraphicOverlay.Transform` is public | VF:16043-16055, 6270 | | The generic motion piece below |

Searched for and absent from the 1.6 build: `rotateOnMove`, `PropertyAnimator`, `Graphic_Cannon`, `dustMotes`, `trailMotes`, any `AnimationDef`.

## How the Harrier and the VCHE pumpjack do it

- **Harrier, all procedural, no frame sheets.** The body is drawn with a scale and position matrix (FWAF:13796-13809, 17383), with jitter while flying
  (FWAF:12148-12161). Each rotor has a stopped and a spinning texture; a spin factor ramps from 0 to 1 and cross-fades them, each turned by the rotor angle
  (FWAF:898-928, 13863-13880, 14062-14151). The shadow stretches with the sun's angle (FWAF:14026-14030). The nozzle flame is one `MoteGlow` texture drawn
  as two layers whose size, angle and alpha wobble with Perlin noise (HAR:2529-2557). Jet wash is flecks chosen by terrain (HAR:2420-2462). Its sprites:
  the body variants, `EngineFlash`, `RocketFlare`, a shadow.
- **VCHE pumpjack, inferred.** `CompProperties_PumpjackAnimation` takes no XML (its Def, `DeepchemNet.xml:410`); it ships `DeepchemPumpjack`,
  `_Bottom` and `_Pump` textures; the DLL's names include `pumpPos`, `pumpPosMax`, `bottomPos`, `PostDraw`, `DrawMesh`. Most likely: slide the pump
  between two positions each tick and draw the two parts with their own matrices. Two sprites, no sheet, no shader.

## What the game itself offers (GAME)

- `Graphic_Flicker` cycles frames every 15 ticks but errors without a `thingDef`, which VF overlays do not pass (GAME:152959-152985; VF:6440): not usable
  as a VF overlay. `Graphic_Random` and `Graphic_Appearances` pick a fixed variant (GAME:154028-154060, 152558-152562): not animation.
- `AnimationDef` keyframes pawn render-tree nodes (GAME:32041-32213); vehicles skip that tree (VF:16034-16047): not applicable.
- Shaders: `ShaderTypeDefOf` (GAME:701226) and `ShaderDatabase` (GAME:24124-24190) include `MoteGlow`, `MoteGlowPulse`, `MotePulse`, `TransparentPostLight`,
  `CutoutFlying`, `GasRotating`: no bundle needed to name one.
- `CompGlower` is real light (GAME:344894-344906); whether it follows a moving vehicle is **not checked**, but it works on any building we spawn (the generator
  set's cable end, the working trucks' power point).
- Custom shaders from a mod's AssetBundle: GAME:24310-24322 and 96464-96520, as in answer 3.

## What our code does today

`CompPhobosSetUp.ShowSetUp` (`src/PhobosFramework.Vehicles/Systems/CompPhobosSetUp.cs`) removes and adds one overlay by key: a two-state switch used by the
tanker's hoses, the generator set's cable and the working trucks. The tanker switches its damage overlays by hand on the rare tick (`CompPhobosTank.cs`), because
the gate above is not wired. `Effect_Fleck` (`Schema/Effects.cs`) throws flecks by the hour, with a warning against reading `vehicle.DrawPos` under VF 1.6's
Unity type names: any `PostDraw` of ours must avoid the same. No `Graphic` subclass of ours exists; no `graphicOverlays` of ours yet move.

## Three kinds of motion, by cost

| Kind | How | Extra art | Code |
| --- | --- | --- | --- |
| **A. Two-state look** | `AddOverlay` and `RemoveOverlays`, as `ShowSetUp` does | one overlay set per state | none new |
| **B. Moving part** | one overlay whose transform a comp changes each frame; a hinge needs the pivot and position maths | **one cut-out sprite per moving part**, at rest on the body's canvas; the body drawn without it | a small generic piece: `PhobosOverlayMotion` on `CompPhobosSetUp`, declared per overlay in XML as `hinge` (pivot cell, from and to angles, seconds, while set up), `spin` (turns a second, while working or by load) or `slide` (offset, seconds); it runs only while the vehicle is set up or working |
| **C. Effects without art** | the game's flecks and motes; the game's shaders on an overlay's `graphicData` (to verify at build); `CompGlower` on the buildings we spawn | none, or one plain glow sprite | mostly XML |

## Where motion would pay off, vehicle by vehicle (agent proposals, owner to pick)

| Vehicle | Motion worth having | Kind | Art it needs | Not worth it |
| --- | --- | --- | --- | --- |
| **Bulk tanker** (art exists) | Hoses out (built); a drip or splash fleck at the hose end while pumping; damage (built) | A, C | nothing new | A sloshing level: nothing shows a tank's inside from above |
| **Generator set** | Cable out (built); **exhaust puffs from the stack scaled by load** while deployed; **a cooling fan on the housing spinning by load**; **real light at night** from the cable end (`CompGlower` on `Phobos_GeneratorStub`) | C, B, C | body bare; **one fan sprite**; the stack's cell named in the record | A sound enclosure (cut by the owner) |
| **Survey truck** | **Boom raising on a hinge over about two seconds** when set up, folding on pack-up; outriggers two-state; **the long-range dish turning slowly** while long range is on; **the powered array pulsing** (a glow sprite on `MoteGlowPulse`) while fast; a lamp glow at night; a small fleck when a find lands | B, A, B, C | body bare with the boom drawn **separately**: body, **boom sprite** (hinge at its root), **dish sprite**, array glow sprite, outrigger set | Geophone spikes and the probe: static node overlays |
| **Drill rig** | **Mast raising on a hinge** when set up; outriggers two-state; **rotary head spinning while drilling**; **dust or chip flecks at the hole** while working, dark for the deepchem head; lamps at night; the casing rack static | B, A, B, C | body bare, **mast sprite** (hinge at the rear), **rotary head sprite** (round, so a spin reads), outrigger set, bit overlays static | A separate drill-string sprite: the mast covers it |
| **Tipper** | **The body lifting**: from above, a raised body reads as the body sprite sliding rearward a little and growing (scale 1.0 to 1.1 over two seconds) with its shadow lengthening (`dynamicShadows`), then the load appearing, which is the real animation; dust flecks in the fan; the tailgate swinging (a small hinge) | B, C | body bare, **tipper body sprite** separate from the chassis, tailgate sprite | Per-item fall arcs: placement is instant and that is fine |
| **All five** | **Wheels:** nothing; from above they do not visibly turn and VF has no tread code. **Damage:** smoke flecks from an engine in its hot stage (the systems know the stage). **Headlights at night** as a glow sprite while the headlights load is on | C | one headlight glow sprite shared by the family | Shader-scrolled treads: no tracked vehicle of ours |

**Proposed order, when the owner wants it built (not in this record):** the generic motion piece and the survey truck's boom hinge first, because one truck proves
hinge, spin and pulse together; then the rig's mast and head; the generator set's fan and glow; the tipper's lift last. Each is XML on existing overlays plus the one
comp; one or two transforms a frame per set-up vehicle, inside the budget the owner measured on 9 Nov 2026.

## Argued against

- **Frame sheets:** VF's only frame player cannot be a body overlay, and a sheet costs a generated image per frame for motion a transform gives for free.
- **The SmashTools animator:** dormant in the shipped build; building on it would be building on nothing.
- **Our own shaders now:** the Unity toolchain per game version and platform is the dearest route to effects the game's own shaders and one still image give.
- **Reusing VF's spinners unchanged:** they spin only for aircraft; the comp has to drive them anyway, so a plain overlay with a transform is simpler.

## Open questions

| Question | Agent lean |
| --- | --- |
| Does a `GraphicOverlay` material honour `graphicData.shaderType` (`MoteGlowPulse`)? | Verify at the first build with one glow sprite; if not, a `PostDraw` with `MaterialPool.MatFrom` as the Harrier does |
| Does `CompGlower` follow a moving vehicle? | Not needed: our lights sit on buildings we spawn while set up |
| Does Vehicle Framework's 1.7 build change any of this? | Only the 1.6 build exists on the owner's PC; re-read when a 1.7 build ships |
| Which motions does the owner want first? | The survey truck's boom, dish and array, since it proves all three kinds |

## Who chose what

- **Owner (9 Nov 2026):** research and record only; explore where motion would benefit each design before more art; no stand-in vehicle pictures (icons only).
- **Agent:** the three kinds and their costs; every row of the vehicle table; the generic motion piece and its XML shape; the order; the leans above.

## Sources

- Decompiles, read 9 Nov 2026 and summarised, never copied: Vehicle Framework 1.6 (SmashPhil, MIT), RimWorld 1.7 (Ludeon Studios), FixedWingAircraftFramework
  and AV8BHarrier (potemkinvillager), at the lines cited.
- Workshop XML, read from the installed items: Vanilla Vehicles Expanded (3014906877: `Mosquito_VehiclePawn.xml:73-79`, `Frog_VehiclePawn.xml:502`,
  `Roadkill_MainTurret.xml:51-92`, `Dinghy_VehiclePawn.xml:69`), Vehicle Framework (3014915404: `Defs/Effects/Mote_CannonPlume.xml`, `Animations/`), Vanilla
  Chemfuel Expanded (2792917473: `DeepchemNet.xml:410` and its textures), RimThunder (3070495204, 3022258867) and Trains of the Rim (3530446424): static sprites.
- Ours: `CompPhobosSetUp.cs`, `CompPhobosTank.cs`, `Schema/Effects.cs`, [art services](vehicle-art-services.md), [the utility vehicles design](utility-vehicles-design.md).
