# HyperbaricChamber

**AI-generated, then picked by the owner on 12 November 2026** (from night 3's candidates, judged that day). Made on 2026-11-08 with ComfyUI 0.41.0, local, FLUX.2 Klein 4B base fp8 (Black Forest Labs, Apache 2.0): 50 steps, guidance 4, seed 11.

**Made with a LoRA trained on game art.** The run used a private LoRA (the v1 style model, phobos-private-vanilla) that was trained on 60 upscaled vanilla RimWorld sprites. The generator was given our own stand-in drawing `HyperbaricChamberNarrow-depth.png`, and our own stand-in drawing `hyperbaric-chamber-102.png` as references. The LoRA and its dataset are not shared and not in this repository. No game art is in this file. Ludeon's EULA allows its resources "as a basis or reference for a Mod" and does not mention AI training, so whether that covers this is unsettled. The owner chose to ship it: see the [design record](../../../../../../../docs/development/art-automation-and-medical-equipment.md).

**Near-copy check:** passed. The closest was DoubleSlabBed_south.png (structure 0.416, hash distance 22). Flagged at a hash distance of 12 or less, or structure of 0.6 or more. A slightly tilted copy can still pass.

**Views:** one picture from directly above serves all three facings, as the burn unit bed's does.

**Prompt:** refcontrol, a one-patient hyperbaric chamber lying north to south seen from directly above, south face hinted, in the look of the second reference image: a long white cylindrical vessel with a clear acrylic top and a round door with a brass wheel at the south end, a game sprite with a thick near-black outline and soft flat cel shading, on a plain pure white background, no text.

- Model SHA-256: `44bab3a86fe98b85d21dd2a4729ebdc3ae51fb8a39f76e457e18c724219e6840`
- LoRA SHA-256: `392b164f1bbe52b0cfd96c629e31e3214bd2571c0de1ab1ab3e8f8d366a88ef6`
- Master `hyperbaric-chamber-depth-11.png`, SHA-256 `aeb9e48625a5466c8e15324318d2931c5a7cf8bdfe538556aba69b31c63c4a6d`
- `HyperbaricChamber_south.png`: 384 x 384 px, from `scripts/art/export_sprite.py --aspect 1:3`, centred on a square canvas. SHA-256 `a18923bb85d501527a3fc0b23bdf70dd7cf2ac59f37832e744d087119ada10ad`
- `HyperbaricChamber_east.png`: 384 x 384 px, the south view turned 90 degrees anticlockwise with Pillow, since the art is from directly above. SHA-256 `ba2ec6c6256688063df0e10c3a08c8bf75492bd92b3b1dd99a060fc40c777f8a`
- `HyperbaricChamber_north.png`: 384 x 384 px, the south view turned 180 degrees. SHA-256 `31e845f157529753ad5eec68289014ddec9695b7445bc5501489944ec30c981d`
