# BottleTap

**AI-generated, then picked by the owner on 7 November 2026.** Made on 2026-11-06 with ComfyUI 0.41.0, local, FLUX.2 Klein 4B base fp8 (Black Forest Labs, Apache 2.0): 50 steps, guidance 4, seed 12.

**Made with a LoRA trained on game art.** The run used a private LoRA that was trained on 60 upscaled vanilla RimWorld sprites. The generator was given the game's vitals monitor sprite and our own generated image BottleTap-large.png as references. The LoRA and its dataset are not shared and not in this repository. No game art is in this file. Ludeon's EULA allows its resources "as a basis or reference for a Mod" and does not mention AI training, so whether that covers this is unsettled. The owner chose to ship it: see the [design record](../../../../../../docs/development/art-automation-and-medical-equipment.md).

**Near-copy check:** passed. The closest was FleshmassPieceC.png (structure 0.465, hash distance 31). Flagged at a hash distance of 12 or less, or structure of 0.6 or more. A slightly tilted copy can still pass.

**Prompt:** phobosvanilla, The fixture in the first reference image, a water bottle tap seen from above: a square steel basin with rounded corners and a little clear water in its base, a brass feed pipe and spout at the top edge, and a red hand wheel on the pipe. The middle of the basin stays open, so bottles stood in it never hide the spout. Keep its shape, layout and colours. Draw it in exactly the art style of the second reference image: a game sprite seen from above at a slight angle, with a thick near-black outline around every shape, soft flat cel shading, a muted palette, rounded panel corners and a few small clean details. One object, centred, on a plain pure white background. No floor, no cast shadow, no text, no labels, no people.

- Model SHA-256: `44bab3a86fe98b85d21dd2a4729ebdc3ae51fb8a39f76e457e18c724219e6840`
- LoRA SHA-256: `392b164f1bbe52b0cfd96c629e31e3214bd2571c0de1ab1ab3e8f8d366a88ef6` (the first private LoRA, phobos-private-vanilla)
- Master SHA-256: `4af49e969a274963991b62e91a223cc8b8f272f04edeede4e2b3ec2a6a0ae69e`
- This file: 128 x 128 px, from `scripts/art/export_sprite.py`. SHA-256 `0acad8c50b4c8e95ee525c88c44b7761e56817c164a0f4719feed79b4be2b570`.
