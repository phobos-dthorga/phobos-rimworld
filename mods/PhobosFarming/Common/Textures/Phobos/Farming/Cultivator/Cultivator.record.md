# Cultivator textures: asset record

**AI-generated, picked by the owner.** The Tillman cultivator, standard: made on 20 Nov 2026 with OpenAI gpt-image-2 through the image API, at high
quality, attempt 1 of 2 (both attempts kept as two models, owner's picks of 20 Nov 2026; [batch record](../../../../../../../assets/phobos-vehicles/requests/farming-batch2-pilots-record.md)). Its only reference was our own art (the Thane T130's
standard body, the owner's pick). No game or other author's art was uploaded.

**Master:** `assets/phobos-vehicles/generated/farming-cultivator-body-north/farming-cultivator-body-north-1.png`, SHA-256 `764c22025b8ea5247bea51119f9bf0e6aff73a39af8d292c5ecd7597c814a74d`,
exported by `assets/phobos-vehicles/export_farming.py` (export_vehicle.py's body export: 3 cells long on a
4 by 4 picture, harvest green as the main paint and the yellow as the accent, saturation floor
0.25, painted by regions with flecks repaired by Segment Anything's parts map).

The background AI and Segment Anything looked at every master on the graphics card on 20 Nov 2026 (owner's go); every
fleck left in a paint mask was repainted with the paint that rings it (paint_check.fill_flecks), and each was checked tinted on dark
ground with no flecks left. East and south are the north turned, with paint masks (suffix m).

**Upgrade parts fitted onto it** (`Cultivator_Wings`): AI-generated parts drawn alone and picked by the owner on
20 Nov 2026 ([parts record](../../../../../../../assets/phobos-vehicles/requests/farming-batch2-parts-record.md)), fitted by
`export_farming.py` (`fit_part`) into boxes set by the agent from the pictures, painted by the same rules, and checked tinted on dark
ground. Their export records name each box.

- `Cultivator_east.png`: SHA-256 `cd7f9edc602d1732d1088b876f0ff77c933b645f85d597c1361b12695f47a327`
- `Cultivator_eastm.png`: SHA-256 `11cb45af8bdd87dd6ced997d07495aab1e856abc0cb72eb2904115c45625dc20`
- `Cultivator_icon.png`: SHA-256 `d399705e230ebb54da1129a79db9df75112186487922e25eae5a7c1d45a86695`
- `Cultivator_north.png`: SHA-256 `a1aa31f17f80f899a4d585429e34838591ecf7a6a358a25e08c83228446d3535`
- `Cultivator_northm.png`: SHA-256 `e9338985cc83434d733b57dff4de267004ce9f0066d364f9ae2d52788aaf820c`
- `Cultivator_south.png`: SHA-256 `4f46c0b7a24e0f6a6713d18cb5ef689f761acabef0e70eb006685a837d1326f6`
- `Cultivator_southm.png`: SHA-256 `40cd23bebcc7b18ec39c812f6f1d830181fe4a301bb8e64353c3cbe88d02cf61`
- `Cultivator_Wings_east.png`: SHA-256 `0b641770c8912bd2407af2279448257b1043387b3d6fe6314869fe8c51c169a3`
- `Cultivator_Wings_eastm.png`: SHA-256 `b31b9f0a475c98203f0f60279d55b0609d235f21c47b4fbecad4878b2cad3718`
- `Cultivator_Wings_north.png`: SHA-256 `d9833ae9326e40d5a6af1a9e3847bf94b6e969213b6a438f99e0e19342a9e9c4`
- `Cultivator_Wings_northm.png`: SHA-256 `f7f72e4dc34cfdce695307e434b0ab2272ff8e8293bf676db6441e349dec099f`
- `Cultivator_Wings_south.png`: SHA-256 `4cf80b538c82a0be932c69fb72c9313fd9d7d02362528c4edf36145dec3cd517`
- `Cultivator_Wings_southm.png`: SHA-256 `a9c5685bf2b48e185cf400b97c081738efd34a7f5a2f9296f6323f80ee5f6184`

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
| Cultivator_north | `b347d1ce1d42885c598a04263c35dfbb152d57106f15626a16eacf9cc0cc6efe` |
| Cultivator_east | `da261806b35f7c7e3caf74a311d306da0800a555f9c55d26344be43f1890c9e0` |
| Cultivator_south | `69d7ef4a06108179bba0c300fbcb29b5192c24ef526be7923aa52975ee02ed3f` |
| Cultivator_Wings_north | `5c8fdbdf4b9f96cc2bed7d8f70bc324589b2f6e69e56c4501dc3437355b833b6` |
| Cultivator_Wings_east | `0833bb05ad99933db0fb392da44e37945d5ccdbd38a8fae9c8ff5359a7553229` |
| Cultivator_Wings_south | `aad7afb47eba84a0a3c3afbd7c9238e1d2b0935d465c1b355e54799efa812ddf` |
