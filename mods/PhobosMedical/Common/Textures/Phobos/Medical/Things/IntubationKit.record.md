# IntubationKit

**AI-generated, then picked by the owner on 12 November 2026** (from night 3's candidates, judged that day). Made on 2026-11-08 with ComfyUI 0.41.0, local, FLUX.2 Klein 4B base fp8 (Black Forest Labs, Apache 2.0): 50 steps, guidance 4, seed 13.

**Made with a LoRA trained on game art.** The run used a private LoRA (the v1 style model, phobos-private-vanilla) that was trained on 60 upscaled vanilla RimWorld sprites. The generator was given our own stand-in drawing `IntubationKit-large.png`, and the game's MedicineIndustrial sprite (local only, never uploaded) as references. The LoRA and its dataset are not shared and not in this repository. No game art is in this file. Ludeon's EULA allows its resources "as a basis or reference for a Mod" and does not mention AI training, so whether that covers this is unsettled. The owner chose to ship it: see the [design record](../../../../../../../docs/development/art-automation-and-medical-equipment.md).

**Near-copy check:** passed. The closest was MeditationSpot.png (structure 0.492, hash distance 26). Flagged at a hash distance of 12 or less, or structure of 0.6 or more. A slightly tilted copy can still pass.

**Prompt:** phobosvanilla, The item in the first reference image, an intubation kit lying flat: a laryngoscope with a ridged dark grey handle and a curved pale steel blade with a small lamp at its tip, beside a clear curved breathing tube with a blue cuff and a green connector. Keep its shape, layout and colours. Draw it in exactly the art style of the second reference image: a game sprite seen from directly above, south face hinted, with a thick near-black outline around every shape, soft flat cel shading, a muted palette, rounded panel corners and a few small clean details. One object, centred, on a plain pure white background. No floor, no cast shadow, no text, no labels, no people.

- Model SHA-256: `44bab3a86fe98b85d21dd2a4729ebdc3ae51fb8a39f76e457e18c724219e6840`
- LoRA SHA-256: `392b164f1bbe52b0cfd96c629e31e3214bd2571c0de1ab1ab3e8f8d366a88ef6`
- Master `intubation-kit-13.png`, SHA-256 `fb71e93bea070b6a4fd8e67cc5136d0f392f1f26709a9f337898f05345caabfc`
- `IntubationKit.png`: 128 x 128 px, from `scripts/art/export_sprite.py`. SHA-256 `d2badd515f88d74f9a424b21ec9dcb1d9a9fdcb6949902bc1e40b44ccbfa0cec`
