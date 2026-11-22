# TractorT400B textures: asset record

**AI-generated, picked by the owner.** The Thane T400 B articulated tractor, ballasted: front and rear halves: made on 20 Nov 2026 with OpenAI gpt-image-2 through the image API, at high
quality, attempt 1 of 2 (both attempts kept as two models, owner's picks of 20 Nov 2026; [batch record](../../../../../../../assets/phobos-vehicles/requests/farming-batch2-pilots-record.md)). Its only reference was our own art (the Thane T130's
standard body, the owner's pick). No game or other author's art was uploaded.

**Master:** `assets/phobos-vehicles/generated/farming-tractor-t400-body-north/farming-tractor-t400-body-north-1.png`, SHA-256 `d6bbb63c41355f59bc4dc8dae049a4a62400eaa980a6164d50cf078ad3ab60af`,
exported by `assets/phobos-vehicles/export_farming.py` whole (TractorT400B, 8 cells long on a 5 by 10 picture), then cut above its hinge into a front and a
rear half, each centred on a 5 by 5 picture, the whole hinge with the rear half; the uncut picture is not shipped.

The background AI and Segment Anything looked at every master on the graphics card on 20 Nov 2026 (owner's go); every
fleck left in a paint mask was repainted with the paint that rings it (paint_check.fill_flecks), and each was checked tinted on dark
ground with no flecks left. East and south are the north turned, with paint masks (suffix m).

**Upgrade parts fitted onto it** (`TractorT400BFront_Tank`): AI-generated parts drawn alone and picked by the owner on
20 Nov 2026 ([parts record](../../../../../../../assets/phobos-vehicles/requests/farming-batch2-parts-record.md)), fitted by
`export_farming.py` (`fit_part`) into boxes set by the agent from the pictures, painted by the same rules, and checked tinted on dark
ground. Their export records name each box.

- `TractorT400BFront_east.png`: SHA-256 `d287b1f502c80a640de835c4d9e5f70bfaa7d53d3c46c12074a2a9fdd9c6d099`
- `TractorT400BFront_eastm.png`: SHA-256 `f243c23407753ff10a65cd0060c1a2b1841350bae2c9cfa8ceabc1e7d6e154f0`
- `TractorT400BFront_north.png`: SHA-256 `400c47e098ad747baa8641e962c1b419899aa8a92e350a432ad5d4f9334f96c0`
- `TractorT400BFront_northm.png`: SHA-256 `0d83a1a54fc87ee1bb88cd1501684da6fea1d9a6dc88729690333a075d51e230`
- `TractorT400BFront_south.png`: SHA-256 `612331b48acc8fb97dcb0e8799bd26c5683a93ca9e7988b0afa1d7e68c18dbef`
- `TractorT400BFront_southm.png`: SHA-256 `0a1ba359a939b57a138faeda3a37945a080918675d3d07407182f693e5e8b300`
- `TractorT400BFront_Tank_east.png`: SHA-256 `e6f2c8d99ecbc52e91f65dae10a366fb8ada47ef05a1a77c4c44649fc1d1cc2c`
- `TractorT400BFront_Tank_eastm.png`: SHA-256 `0faa65dc55d6bf5a10a3215e4dc249bcac4aed6f887dc1e388c8699a123ec421`
- `TractorT400BFront_Tank_north.png`: SHA-256 `d6bd15e499da8b327dc080d7ccf99bee2af7fa8796c2839ae90a8094ca6b575b`
- `TractorT400BFront_Tank_northm.png`: SHA-256 `a3de12da92ff306ded644b238ac9e9c6631f69c120b673097aa9d801c88972f7`
- `TractorT400BFront_Tank_south.png`: SHA-256 `a011922c9d15344401028d893f7e27e9abab9b0454b1571f0d1db4733628f663`
- `TractorT400BFront_Tank_southm.png`: SHA-256 `5ccdc87059f0c6d194334587428dea386d12baea78fcbe5ec0667d9962a544d5`
- `TractorT400BRear_east.png`: SHA-256 `9adac6514583b2fc484cd715245c7260d32a9c50b568edff1f29df14abe3dbd9`
- `TractorT400BRear_eastm.png`: SHA-256 `d1b9f6049ecf5d5cf49146c903ad2e9bd957dd402bf331a92945dfcd736534eb`
- `TractorT400BRear_north.png`: SHA-256 `c1a0f26ab473c588be6b3a9f25a283bb6d8a2ff7e0c556d59cc0f101e4d4a060`
- `TractorT400BRear_northm.png`: SHA-256 `8fa08a179938fc696c3cf6c1570e095378cdf5790924463cc47c42f67acc3b9a`
- `TractorT400BRear_south.png`: SHA-256 `8bce5b7767a9382a7b52f369438b3581c640615a0e448f75455a8303d4f0f32b`
- `TractorT400BRear_southm.png`: SHA-256 `c488858fb214b734710e643ded0236d16d95f9ac0e1b4cca6e71367bb026ae11`

## Own colours, no paint, clean rims (23 Nov 2026)

**Made from the shipped pictures, not from the master**, D'thorga Farming 0.7.7 (the owner's choices of 23 Nov 2026: no paint for
Farming's machines, and the white dithering at the borders fixed). `python scripts/art/bake_paint.py assets/phobos-vehicles/farming-unpainted.json
--apply`: each painted picture became what the game drew in its own colours (the grey texture times this vehicle's Def colours under the
mask, overlays and halves in their vehicle's colours), its mask was removed, and every partly transparent pixel on its outer edge paler
than the pixel inside it took that pixel's colour, keeping its transparency (the pale rim left by exports before the soft-rim fix,
5c6d04e8). Nothing moved: outlines, positions and overlays' fits are as before. East and south are the north turned. No game or other
author's art was used.

| Texture | SHA-256 |
| --- | --- |
| TractorT400BFront_north | `9f3635f80f9747b97632c80fd57b314498975f9e5ebe2e470b4efdba45045056` |
| TractorT400BFront_east | `2b8128ff02fd576055e8890d23f95a7406180060c017bfb72fff690339ea23f5` |
| TractorT400BFront_south | `c4bbbfaf0536eb6e6ac2feb81a9ef03d400cd96a25df7922aa9191d3944880ec` |
| TractorT400BFront_Tank_north | `e5064b86305145acdfe950fb3e5f10e2790763437611492cb62c01a1c8899fd0` |
| TractorT400BFront_Tank_east | `89530e6dd9a7c567a168ba6d6b4f23ba4b8ad320a011b17b8d4c2367bc03b67b` |
| TractorT400BFront_Tank_south | `1c62cf86c71ca0528727a945cede241d83551ce17433e2b751009905ab2563d2` |
| TractorT400BRear_north | `064fbf8f6e95d78d694a01cbddaf34664169495b038ff62d6d60817f494e697e` |
| TractorT400BRear_east | `389042d17d83fade51ef3e25790e1564f4504619defb213650e910ff59fa2d60` |
| TractorT400BRear_south | `fa8958aeac94d65fba1f3984830b809d9d708b8d66a2c10cd5202e4a0b14a7ef` |
