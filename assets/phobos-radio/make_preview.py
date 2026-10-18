"""Draw the placeholder Preview.png for the D'thorga Radio Framework: a dark panel with a mast and
three signal arcs. Pure Python (zlib only), deterministic, so the image is reproducible from this file.

    python assets/phobos-radio/make_preview.py
"""
import math
from pathlib import Path
import struct
import zlib

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'mods' / 'PhobosRadioFramework' / 'About' / 'Preview.png'
WIDTH, HEIGHT = 640, 360
BACKGROUND = (0x17, 0x18, 0x14)
PANEL = (0x20, 0x22, 0x1C)
LINE = (0x3A, 0x3D, 0x33)
MAST = (0xA8, 0xA3, 0x92)
ARC = (0xD6, 0xB2, 0x5E)


def png(width, height, rows):
    raw = b''.join(b'\0' + bytes(channel for pixel in row for channel in pixel) for row in rows)

    def chunk(kind, data):
        return struct.pack('>I', len(data)) + kind + data + struct.pack('>I', zlib.crc32(kind + data) & 0xFFFFFFFF)

    return (b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', width, height, 8, 2, 0, 0, 0))
            + chunk(b'IDAT', zlib.compress(raw, 9)) + chunk(b'IEND', b''))


def render():
    pixels = [[BACKGROUND] * WIDTH for _ in range(HEIGHT)]

    def put(x, y, colour):
        if 0 <= x < WIDTH and 0 <= y < HEIGHT:
            pixels[y][x] = colour

    for y in range(24, HEIGHT - 24):
        for x in range(24, WIDTH - 24):
            edge = y in (24, HEIGHT - 25) or x in (24, WIDTH - 25)
            put(x, y, LINE if edge else PANEL)
    base_x, top_y, bottom_y = 200, 90, 300
    for y in range(top_y, bottom_y):
        for dx in range(-2, 3):
            put(base_x + dx, y, MAST)
    for y in range(bottom_y - 4, bottom_y):
        for x in range(base_x - 40, base_x + 41):
            put(x, y, MAST)
    for tier, (y, half) in enumerate(((120, 28), (160, 40), (200, 52))):
        for x in range(base_x - half, base_x + half + 1):
            put(x, y, MAST)
            put(x, y + 1, MAST)
    for radius in (70, 120, 170):
        for step in range(0, 1800):
            angle = math.radians(-60 + step * 120 / 1800)
            x = int(round(base_x + 12 + radius * math.cos(angle)))
            y = int(round(top_y + 10 + radius * math.sin(angle)))
            for dx in range(0, 3):
                put(x + dx, y, ARC)
    return pixels


if __name__ == '__main__':
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_bytes(png(WIDTH, HEIGHT, render()))
    print(f'wrote {OUT}')
