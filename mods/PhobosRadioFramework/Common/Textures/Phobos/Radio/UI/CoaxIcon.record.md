# CoaxIcon

**AI-generated, then picked by the owner on 7 November 2026.** Made on 2026-11-07 with ComfyUI 0.41.0, local, FLUX.2 Klein 4B base fp8 (Black Forest Labs, Apache 2.0): 50 steps, guidance 4, seed 12.

**Made with a LoRA trained on game art.** The run used a private LoRA that was trained on 60 upscaled vanilla RimWorld sprites. The generator was given the game's industrial medicine and glitterworld medicine sprites as references. The LoRA and its dataset are not shared and not in this repository. No game art is in this file. Ludeon's EULA allows its resources "as a basis or reference for a Mod" and does not mention AI training, so whether that covers this is unsettled. The owner chose to ship it: see the [design record](../../../../../../../docs/development/art-automation-and-medical-equipment.md).

**Near-copy check:** passed. The closest was Meat_Small_a.png (structure 0.336, hash distance 26). Flagged at a hash distance of 12 or less, or structure of 0.6 or more. A slightly tilted copy can still pass.

**Prompt:** phobosvanilla, A coil of black coaxial cable with a silver connector on its end. Draw it in exactly the art style of the reference images: a small game icon with a very thick near-black outline, bold simple shapes, flat cel shading and a muted palette, readable when shrunk to a thumbnail. One object, centred, on a plain pure white background. No text, no letters, no numbers, no hands.

- Model SHA-256: `44bab3a86fe98b85d21dd2a4729ebdc3ae51fb8a39f76e457e18c724219e6840`
- LoRA SHA-256: `392b164f1bbe52b0cfd96c629e31e3214bd2571c0de1ab1ab3e8f8d366a88ef6` (the first private LoRA, phobos-private-vanilla)
- Master SHA-256: `0e946ba599247a1d26bfeb5c332fdcecb53e285c17ed2d32ab96fa036bd3dfca`
- This file: 128 x 128 px, from `scripts/art/export_sprite.py --all-white --threshold 250 --soft 20`. SHA-256 `fb176342ef04af437e58c80964a73dd66631ab70c510047f616fcc1666d6169c`.
