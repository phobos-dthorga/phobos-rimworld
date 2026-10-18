"""Draw the placeholder sprites for the D'thorga Radio Framework in pure Python (zlib only), so every
image is reproducible from this file. Simple flat shapes in RimWorld's muted palette; replaced before
the public beta.

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


def handheld():
    c = Canvas(64)
    c.rect(22, 14, 42, 56, DARK)          # body
    c.rect(24, 16, 40, 54, BODY)
    c.rect(26, 22, 38, 32, SCREEN)        # display
    for row in range(3):                  # keypad
        for col in range(3):
            c.rect(27 + col * 4, 36 + row * 5, 29 + col * 4, 38 + row * 5, LIGHT)
    c.rect(26, 6, 29, 16, DARK)           # antenna
    c.rect(27, 4, 28, 16, LIGHT)
    c.save('Things/Handheld.png')


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


def signal_icon():
    c = Canvas(64)
    c.rect(8, 44, 16, 56, LIGHT)          # four bars, rising
    c.rect(20, 36, 28, 56, LIGHT)
    c.rect(32, 26, 40, 56, LIGHT)
    c.rect(44, 14, 52, 56, LIGHT)
    c.save('UI/Signal.png')


def whip():
    c = Canvas(64)                        # drawn at 1 x 1.5 cells: pole and whip
    c.rect(28, 50, 36, 60, DARK)          # base plate
    c.rect(31, 20, 33, 50, LIGHT)         # pole
    c.rect(31, 4, 33, 20, ACCENT)         # the whip itself
    c.save('Things/Whip.png')


def mast():
    c = Canvas(64)                        # drawn at 1 x 2.5 cells: lattice mast and collinear
    c.rect(24, 56, 40, 62, DARK)          # base
    for y in range(8, 56, 6):             # lattice rungs
        c.rect(27, y, 37, y + 1, LIGHT)
    c.rect(27, 8, 29, 58, LIGHT)          # legs
    c.rect(35, 8, 37, 58, LIGHT)
    c.rect(31, 0, 33, 10, ACCENT)         # collinear on top
    c.save('Things/Mast.png')


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


def coax_icon():
    c = Canvas(64)
    c.rect(0, 28, 64, 36, DARK)
    c.rect(0, 30, 64, 34, ACCENT)
    c.rect(26, 22, 38, 42, DARK)
    c.save('UI/CoaxIcon.png')


if __name__ == '__main__':
    handheld()
    base_station()
    signal_icon()
    whip()
    mast()
    coax_atlas('Coax_Atlas', DARK, ACCENT)
    coax_atlas('Coax_Blueprint_Atlas', (0x63, 0xA4, 0xE0, 180), (0x8F, 0xBD, 0xE6, 220))
    coax_icon()
