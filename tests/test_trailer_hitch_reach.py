"""A drawbar trailer's tow lengths must put its picture's drawbar eye on the hitch.

The hitch puts a trailer's middle ``hitchToAxle - axleToCentre`` cells behind the hitch point (``HitchRule.AxleFor`` then
``HitchRule.CentreOf``). Every trailer of ours had authored lengths that put its middle 0.6 to 0.8 cells further back than its
picture's drawbar reaches, so a hitched trailer stood about a tile off the vehicle towing it, the fault the owner saw on
23 Nov 2026 with the L-ATV and the Pintle bowser (Vehicles 0.113.3, Farming 0.7.9).

This test reads each trailer Def of ours, measures how far the north picture reaches ahead of its middle, and refuses lengths that
put the eye more than a tenth of a cell from the hitch. It skips implements held rigid on a linkage (a jack-knife of nought: there
the length is read as a gap) and an articulated vehicle's driven rear half (measured at its cut, fault-read D5 of 21 Nov 2026).
"""
import tempfile
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
TRAILER = "Phobos.VehicleSystems.CompProperties_PhobosTrailer"
# Every drawbar of ours ends in the same drawn eye, about 0.18 cells across: the hitch pin goes through its middle.
EYE_RADIUS = 0.09
TOLERANCE = 0.1
OPAQUE = 20


def picture_reach(png, drawn_length):
    """How far the north picture reaches ahead of its middle, in cells."""
    with Image.open(png) as image:
        alpha = image.convert("RGBA").getchannel("A")
        width, height = alpha.size
        box = alpha.point(lambda a: 255 if a > OPAQUE else 0).getbbox()
    if box is None:
        raise ValueError(f"{png} has nothing drawn in it")
    return (height / 2 - box[1]) / (height / drawn_length)


def reach_error(png, drawn_length, hitch_to_axle, axle_to_centre):
    """Cells by which the tow lengths put the trailer's middle further back than its drawn eye (negative: closer)."""
    return (hitch_to_axle - axle_to_centre) - (picture_reach(png, drawn_length) - EYE_RADIUS)


def texture_roots():
    return [path for path in (ROOT / "mods").glob("*/**/Textures") if path.is_dir()]


def find_north(tex_path, roots):
    for root in roots:
        candidate = root / (tex_path + "_north.png")
        if candidate.is_file():
            return candidate
    return None


def drawbar_trailers():
    """(defName, file, texPath, drawn length, hitchToAxle, axleToCentre) for each drawbar trailer Def of ours."""
    for path in sorted((ROOT / "mods").rglob("*.xml")):
        if "Defs" not in path.parts:
            continue
        try:
            tree = ET.parse(path)
        except ET.ParseError:
            continue
        for definition in tree.getroot():
            props = definition.find(f"comps/li[@Class='{TRAILER}']")
            name = definition.findtext("defName")
            if props is None or name is None or definition.get("Abstract") == "True":
                continue
            if (props.findtext("drivenAxle") or "").strip().lower() == "true":
                continue
            if float(props.findtext("jackKnifeDegrees") or 70) == 0:
                continue
            draw = definition.findtext("graphicData/drawSize")
            yield (name, path.relative_to(ROOT).as_posix(), definition.findtext("graphicData/texPath"),
                   float(draw.strip("() ").split(",")[1]) if draw else None,
                   float(props.findtext("hitchToAxle") or 3.2), float(props.findtext("axleToCentre") or 0.2))


class TrailerHitchReach(unittest.TestCase):
    def test_the_check_sees_the_fault_it_was_written_for(self):
        # A 9-cell picture at 10 pixels a cell, drawn from 1.9 cells above its middle: the eye 1.81 cells ahead.
        with tempfile.TemporaryDirectory() as folder:
            png = Path(folder) / "Trailer_north.png"
            image = Image.new("RGBA", (50, 90), (0, 0, 0, 0))
            for y in range(26, 80):
                for x in range(20, 30):
                    image.putpixel((x, y), (90, 90, 90, 255))
            image.save(png)
            self.assertAlmostEqual(reach_error(png, 9, 2.81 + 0.4, 0.4), 1.0, places=2)
            self.assertAlmostEqual(reach_error(png, 9, 1.81 + 0.4, 0.4), 0.0, places=2)

    def test_every_drawbar_trailer_reaches_its_hitch(self):
        roots = texture_roots()
        found, problems = 0, []
        for name, file, tex_path, drawn, hitch_to_axle, axle_to_centre in drawbar_trailers():
            found += 1
            png = find_north(tex_path, roots) if tex_path else None
            if png is None or drawn is None:
                problems.append(f"{name} ({file}): no north picture or drawSize to measure")
                continue
            error = reach_error(png, drawn, hitch_to_axle, axle_to_centre)
            if abs(error) > TOLERANCE:
                eye = picture_reach(png, drawn) - EYE_RADIUS
                problems.append(f"{name} ({file}): hitchToAxle - axleToCentre is {hitch_to_axle - axle_to_centre:.2f} cells, the drawn eye "
                                f"{eye:.2f} ahead of the middle ({error:+.2f}); set hitchToAxle to {eye + axle_to_centre:.2f}")
        self.assertGreater(found, 0, "no trailer Defs found: has CompProperties_PhobosTrailer moved?")
        self.assertEqual(problems, [], "\n".join(problems))


if __name__ == "__main__":
    unittest.main()
