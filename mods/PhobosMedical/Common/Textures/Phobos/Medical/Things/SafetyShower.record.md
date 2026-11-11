# SafetyShower

**AI-generated, then picked by the owner on 12 November 2026** (from night 3's candidates, judged that day). Made on 2026-11-08 with ComfyUI 0.41.0, local, FLUX.2 Klein 4B base fp8 (Black Forest Labs, Apache 2.0): 50 steps, guidance 4, seed 14.

**Made with a LoRA trained on game art.** The run used a private LoRA (the v1 style model, phobos-private-vanilla) that was trained on 60 upscaled vanilla RimWorld sprites. The generator was given our own stand-in drawing `SafetyShower-large.png`, and the game's VitalsMonitor sprite (local only, never uploaded) as references. The LoRA and its dataset are not shared and not in this repository. No game art is in this file. Ludeon's EULA allows its resources "as a basis or reference for a Mod" and does not mention AI training, so whether that covers this is unsettled. The owner chose to ship it: see the [design record](../../../../../../../docs/development/art-automation-and-medical-equipment.md).

**Near-copy check:** passed. The closest was FleshmassPieceC.png (structure 0.293, hash distance 33). Flagged at a hash distance of 12 or less, or structure of 0.6 or more. A slightly tilted copy can still pass.

**Prompt:** phobosvanilla, The machine in the first reference image, a safety shower and eyewash: a round green drench head over a dark grated floor tray, a yellow pull rod ending in a yellow handle, and a white eyewash bowl with two green nozzles at one corner. Keep its shape, layout and colours. Draw it in exactly the art style of the second reference image: a game sprite seen from directly above, south face hinted, with a thick near-black outline around every shape, soft flat cel shading, a muted palette, rounded panel corners and a few small clean details. One object, centred, on a plain pure white background. No floor, no cast shadow, no text, no labels, no people.

- Model SHA-256: `44bab3a86fe98b85d21dd2a4729ebdc3ae51fb8a39f76e457e18c724219e6840`
- LoRA SHA-256: `392b164f1bbe52b0cfd96c629e31e3214bd2571c0de1ab1ab3e8f8d366a88ef6`
- Master `safety-shower-14.png`, SHA-256 `a529ff9eb94396340d5a74c78561cd0ca774de9c672d2b50cc074eff009900b4`
- `SafetyShower.png`: 256 x 256 px, from `scripts/art/export_sprite.py`. SHA-256 `087db145ebef7fae5b129183f94876b85980e1c163a4e1bbb222e15648a898c0`
