"""Draw the placeholder sprites for the D'thorga Radio Framework that are still placeholders, in pure Python (zlib only), so
every image is reproducible from this file: the base station and the feedline atlases. Simple flat shapes in RimWorld's muted
palette; replaced before the public beta. The hand-held radio, mast, whip and both icons are generated art the owner picked (7 Nov
2026), so this script no longer draws them and can't overwrite them.

    python assets/phobos-radio/make_textures.py
"""
from pathlib import Path
import struct
import zlib

ROOT = Path(__file__).resolve().parents[2]
TEXTURES = ROOT / 'mods' / 'PhobosRadioFramework' / 'Common' / 'Textures' / 'Phobos' / 'Radio'
CLEAR = (0, 0, 0, 0)
DARK = (0x2A, 0x2B, 0x27, 255)
BODY = (0x4A, 0x4C, 0x45, 255)
LIGHT = (0x8F, 0x91, 0x88, 255)
ACCENT = (0xD6, 0xB2, 0x5E, 255)
SCREEN = (0x63, 0xA4, 0xE0, 255)


def png(width, height, pixels):
    raw = b''.join(b'\0' + bytes(channel for pixel in row for channel in pixel) for row in pixels)

    def chunk(kind, data):
        return struct.pack('>I', len(data)) + kind + data + struct.pack('>I', zlib.crc32(kind + data) & 0xFFFFFFFF)

    return (b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', width, height, 8, 6, 0, 0, 0))
            + chunk(b'IDAT', zlib.compress(raw, 9)) + chunk(b'IEND', b''))


class Canvas:
    def __init__(self, size):
        self.size = size
        self.pixels = [[CLEAR] * size for _ in range(size)]

    def rect(self, x0, y0, x1, y1, colour):
        for y in range(max(y0, 0), min(y1, self.size)):
            for x in range(max(x0, 0), min(x1, self.size)):
                self.pixels[y][x] = colour

    def save(self, relative):
        path = TEXTURES / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(png(self.size, self.size, self.pixels))
        print(f'wrote {path}')


def base_station():
    c = Canvas(128)
    c.rect(10, 40, 118, 118, DARK)        # desk
    c.rect(14, 44, 114, 114, BODY)
    c.rect(22, 54, 70, 84, DARK)          # transceiver
    c.rect(26, 58, 50, 72, SCREEN)        # frequency display
    for i in range(4):                    # knobs
        c.rect(54 + i * 4, 60, 56 + i * 4, 62, LIGHT)
    c.rect(26, 76, 66, 80, LIGHT)         # speaker grille
    c.rect(78, 54, 106, 84, DARK)         # power supply
    c.rect(82, 60, 102, 64, ACCENT)
    c.rect(98, 14, 102, 54, DARK)         # whip
    c.rect(99, 10, 101, 54, LIGHT)
    c.save('Things/BaseStation.png')


COAX_UP, COAX_RIGHT, COAX_DOWN, COAX_LEFT = 1, 2, 4, 8


def coax_atlas(name, colour, centre):
    """A 4 x 4 linked atlas: tile i has arms for the bits Up=1, Right=2, Down=4, Left=8; column i % 4,
    row i // 4 counted from the bottom (Unity's texture offset origin), so PNG row = 3 - i // 4."""
    c = Canvas(256)
    tile = 64
    for i in range(16):
        ox = (i % 4) * tile
        oy = (3 - i // 4) * tile
        cx, cy = ox + 32, oy + 32
        c.rect(cx - 5, cy - 5, cx + 5, cy + 5, centre)
        if i & COAX_UP: c.rect(cx - 3, oy, cx + 3, cy, colour)
        if i & COAX_DOWN: c.rect(cx - 3, cy, cx + 3, oy + tile, colour)
        if i & COAX_LEFT: c.rect(ox, cy - 3, cx, cy + 3, colour)
        if i & COAX_RIGHT: c.rect(cx, cy - 3, ox + tile, cy + 3, colour)
        if i == 0: c.rect(cx - 7, cy - 7, cx + 7, cy + 7, colour)
    c.save(f'Linked/{name}.png')


if __name__ == '__main__':
    base_station()
    coax_atlas('Coax_Atlas', DARK, ACCENT)
    coax_atlas('Coax_Blueprint_Atlas', (0x63, 0xA4, 0xE0, 180), (0x8F, 0xBD, 0xE6, 220))
