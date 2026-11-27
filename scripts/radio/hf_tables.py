"""HF propagation tables from VOACAP (docs/development/radio-hf.md; the owner's cards of 28 Nov 2026: VOACAP, offline, shipped as tables).

Game-free and standard library only, so the same module runs on Windows (the command, packing) and inside WSL (the worker that drives
voacapl). VOACAP is never shipped: it runs only on the machine that makes the tables, and what ships is our packed summary of its
predictions.

The grid: month, sunspot number, the path's midpoint latitude, its length and the midpoint's local hour, for each band. VOACAP's
ionosphere is Earth's, tied to Earth's magnetic field, which a RimWorld planet does not have, so each cell is averaged over paths at
several longitudes and in two directions (north-south and east-west): a planet gets Earth's typical ionosphere rather than one
meridian's. An approximation, labelled as such in the record. Isotropic antennas at both ends and 1 kW, so a cell's loss is the path's
own and the game's link budget adds each antenna's gain, as it does for every other band.
"""
import json
import math
import os
import statistics
import struct

MONTHS = list(range(1, 13))
# A quiet sun to a busy one (smoothed sunspot number); the game looks between them. Agent's choice, 28 Nov 2026.
SUNSPOTS = [0, 50, 100, 150, 200]
LATITUDES = list(range(-80, 81, 10))
# Path lengths in km, to about half way round (a RimWorld planet at HF's 30 km a tile is about 40,000 km round).
DISTANCES = [50, 100, 200, 300, 500, 750, 1000, 1500, 2000, 2500, 3000, 4000, 5000, 6000, 8000, 10000, 12500, 15000, 19000]
# Amateur bands VOACAP covers (2 to 30 MHz), at a frequency in each; 160 m lies below VOACAP's range.
BANDS = {"80m": 3.60, "40m": 7.10, "30m": 10.12, "20m": 14.20, "17m": 18.10, "15m": 21.20, "12m": 24.94, "10m": 28.50}
LONGITUDES = [-90, 0, 90, 180]
BEARINGS = [0, 90]
# Man-made noise at 3 MHz in VOACAP's scale (-dBW/Hz): 164 is "remote", leaving mostly natural noise; a colony's own sources are the
# game's to add (agent's choice).
MANMADE_NOISE = 164
EARTH_KM = 6371.0


def endpoints(lat, lon, bearing, km):
    """The two ends of a great-circle path of this length whose midpoint is (lat, lon), running along this bearing."""
    phi, lam, half = math.radians(lat), math.radians(lon), km / 2 / EARTH_KM
    ends = []
    for b in (bearing, bearing + 180):
        theta = math.radians(b)
        phi2 = math.asin(math.sin(phi) * math.cos(half) + math.cos(phi) * math.sin(half) * math.cos(theta))
        lam2 = lam + math.atan2(math.sin(theta) * math.sin(half) * math.cos(phi), math.cos(half) - math.sin(phi) * math.sin(phi2))
        ends.append((math.degrees(phi2), (math.degrees(lam2) + 540) % 360 - 180))
    return ends


def _hemi(value, positive, negative):
    return abs(value), positive if value >= 0 else negative


def deck(month, ssn, lat, lon, bearing, km, bands=BANDS):
    """VOACAP's input deck for one path, every hour, every band, in its fixed columns."""
    (lat1, lon1), (lat2, lon2) = endpoints(lat, lon, bearing, km)
    a1, n1 = _hemi(lat1, "N", "S")
    o1, e1 = _hemi(lon1, "E", "W")
    a2, n2 = _hemi(lat2, "N", "S")
    o2, e2 = _hemi(lon2, "E", "W")
    freqs = "".join(f"{f:5.2f}" for f in bands.values()) + " 0.00" * (11 - len(bands))
    iso = "default/isotrope".ljust(21)
    return "\n".join([
        "LINEMAX     999       number of lines-per-page",
        "COEFFS    CCIR",
        "TIME          1   24    1    1",
        f"MONTH      2000{month:5.2f}",
        f"SUNSPOT{ssn:7.0f}.",
        "LABEL     A                   B",
        f"CIRCUIT{a1:8.2f}{n1}{o1:9.2f}{e1}{a2:9.2f}{n2}{o2:9.2f}{e2}  S     0",
        f"SYSTEM       1. {MANMADE_NOISE:3d}. 0.10  90. 73.0 3.00 0.10",
        "FPROB      1.00 1.00 1.00 0.00",
        f"ANTENNA       1    1    2   30     0.000[{iso}]  0.0    1.0000",
        f"ANTENNA       2    2    2   30     0.000[{iso}]  0.0    0.0000",
        # Fixed columns: the first 5-wide field starts at column 11, so the card's name takes ten (one space, not two); a field out
        # by one reads "214.2" for 14.20 after a five-figure frequency (found 28 Nov 2026).
        f"FREQUENCY {freqs}",
        "METHOD       30    0",
        "EXECUTE",
        "QUIT",
        ""])


_ROWS = {"MUFday": "mufday", "LOSS": "loss", "N DBW": "noise", "SIG LW": "sigLow", "SIG UP": "sigHigh"}


def parse(text, bands=BANDS):
    """
    VOACAP's method 30 output: for each universal hour, the MUF and, for each band, the share of days it is open (MUFday), the path
    loss, the noise at the receiver and the signal's spread. Each row's first column is the MUF's own, so the bands follow it. A dash
    (no figure) is left out.
    """
    names = list(bands)
    hours, current = {}, None
    for line in text.splitlines():
        tokens = line.split()
        if not tokens:
            continue
        if tokens[-1] == "FREQ" and len(tokens) >= 3:
            current = {"muf": float(tokens[1]), "bands": {}}
            hours[int(round(float(tokens[0]))) % 24] = current
            continue
        if current is None:
            continue
        two = len(tokens) >= 3 and tokens[-1] in ("DBW", "LW", "UP")
        label = " ".join(tokens[-2:]) if two else tokens[-1]
        key = _ROWS.get(label)
        if not key:
            continue
        values = tokens[:-2] if two else tokens[:-1]
        for name, value in zip(names, values[1:]):
            if value != "-":
                current["bands"].setdefault(name, {})[key] = float(value)
    return hours


def frequencies_read(text, bands=BANDS):
    """Whether VOACAP read the frequencies asked for: its first hour's FREQ row, to a tenth of a MHz (a card out by a column reads
    another frequency silently)."""
    for line in text.splitlines():
        tokens = line.split()
        if tokens and tokens[-1] == "FREQ":
            try:
                read = [float(t) for t in tokens[2:2 + len(bands)]]
            except ValueError:
                return False
            return len(read) == len(bands) and all(abs(r - f) < 0.06 for r, f in zip(read, bands.values()))
    return False


def to_local(hours, lon):
    """Universal hours to the path midpoint's local hours (longitude / 15, whole hours on this grid)."""
    shift = round(lon / 15)
    return {(h + shift) % 24: v for h, v in hours.items()}


def combine(samples, bands=BANDS):
    """
    One cell from its paths (each already in local hours): the mean MUF and share of days open, the median loss and spread (a closed
    path's huge loss would drag a mean), the mean noise. Agent's choices, labelled in the record.
    """
    out = {}
    for hour in range(24):
        present = [s[hour] for s in samples if hour in s]
        if not present:
            continue
        cell = {"muf": round(statistics.fmean(p["muf"] for p in present), 2), "bands": {}}
        for name in bands:
            rows = [p["bands"].get(name, {}) for p in present]
            def pick(key, how):
                values = [r[key] for r in rows if key in r]
                return round(how(values), 2) if values else None
            cell["bands"][name] = {"mufday": pick("mufday", statistics.fmean), "loss": pick("loss", statistics.median),
                                   "noise": pick("noise", statistics.fmean), "sigLow": pick("sigLow", statistics.median),
                                   "sigHigh": pick("sigHigh", statistics.median)}
        out[hour] = cell
    return out


def jobs():
    """Every cell of the grid, as (month, sunspots, latitude, km)."""
    return [(m, s, lat, km) for m in MONTHS for s in SUNSPOTS for lat in LATITUDES for km in DISTANCES]


# ---- Packing: the binary the game reads (format 1) ----
MAGIC = b"PHFT"
FORMAT = 1
MUF_STEP = 0.25        # MHz a step
LOSS_FLOOR = 80        # dB at byte 0; byte 255 means no usable path (a loss of 335 dB or more, or none given)
SPREAD_STEP = 0.1      # dB a step
NOISE_TOP = 100        # noise byte b means -(NOISE_TOP + b) dBW/Hz


def _byte(value, lo=0, hi=255):
    return max(lo, min(hi, int(round(value))))


def pack(cells, bands):
    """
    The tables as bytes, little-endian: a header naming every axis, then for each month, sunspot level, latitude, distance and local
    hour the MUF and, for each band, its share of days open, its loss and its lower-decile spread; then the noise at the receiver for
    each month, sunspot level, latitude, hour and band, read from the shortest paths. cells maps (month, ssn, lat, km) to combine()'s
    output.
    """
    out = bytearray(MAGIC)
    out += struct.pack("<H", FORMAT)
    out += struct.pack("<B", len(MONTHS))
    out += struct.pack("<B", len(SUNSPOTS)) + struct.pack(f"<{len(SUNSPOTS)}H", *SUNSPOTS)
    out += struct.pack("<B", len(LATITUDES)) + struct.pack(f"<{len(LATITUDES)}b", *LATITUDES)
    out += struct.pack("<B", len(DISTANCES)) + struct.pack(f"<{len(DISTANCES)}H", *DISTANCES)
    out += struct.pack("<B", 24)
    out += struct.pack("<B", len(bands)) + struct.pack(f"<{len(bands)}H", *(round(BANDS[b] * 1000) for b in bands))
    for m in MONTHS:
        for s in SUNSPOTS:
            for lat in LATITUDES:
                for km in DISTANCES:
                    hours = cells.get((m, s, lat, km), {})
                    for h in range(24):
                        cell = hours.get(h)
                        out.append(_byte(cell["muf"] / MUF_STEP) if cell else 0)
                        for b in bands:
                            band = (cell or {}).get("bands", {}).get(b, {})
                            opened, loss, spread = band.get("mufday"), band.get("loss"), band.get("sigLow")
                            out.append(_byte(opened * 250) if opened is not None else 0)
                            out.append(_byte(loss - LOSS_FLOOR, 0, 254) if loss is not None and loss < LOSS_FLOOR + 255 else 255)
                            out.append(_byte(spread / SPREAD_STEP) if spread is not None else 0)
    shortest = DISTANCES[0]
    for m in MONTHS:
        for s in SUNSPOTS:
            for lat in LATITUDES:
                hours = cells.get((m, s, lat, shortest), {})
                for h in range(24):
                    for b in bands:
                        noise = hours.get(h, {}).get("bands", {}).get(b, {}).get("noise")
                        out.append(_byte(-noise - NOISE_TOP) if noise is not None else 255)
    return bytes(out)


def unpack_header(data):
    """The axes a packed file holds, and where its two blocks start: for checks and tests."""
    if data[:4] != MAGIC:
        raise ValueError("not an HF table file")
    at = 4
    (fmt,) = struct.unpack_from("<H", data, at); at += 2
    (months,) = struct.unpack_from("<B", data, at); at += 1
    def axis(code, size):
        nonlocal at
        (n,) = struct.unpack_from("<B", data, at); at += 1
        values = list(struct.unpack_from(f"<{n}{code}", data, at)); at += n * size
        return values
    ssn = axis("H", 2)
    lats = axis("b", 1)
    dists = axis("H", 2)
    (hours,) = struct.unpack_from("<B", data, at); at += 1
    khz = axis("H", 2)
    paths = at
    noise = paths + months * len(ssn) * len(lats) * len(dists) * hours * (1 + 3 * len(khz))
    return {"format": fmt, "months": months, "sunspots": ssn, "latitudes": lats, "distances": dists, "hours": hours, "kHz": khz,
            "paths": paths, "noise": noise, "size": noise + months * len(ssn) * len(lats) * hours * len(khz)}


# ---- The worker, run inside WSL ----
def worker(out_folder, itshfbc, parallel, months=None):
    """Runs every path of the grid through voacapl at low priority and writes one JSON file of combined cells per month."""
    import subprocess
    import tempfile
    from concurrent.futures import ThreadPoolExecutor

    def one(args):
        month, ssn, lat, km, lon, bearing = args
        with tempfile.TemporaryDirectory(dir="/tmp") as folder:
            with open(os.path.join(folder, "in.dat"), "w") as f:
                f.write(deck(month, ssn, lat, lon, bearing, km))
            done = subprocess.run(["nice", "-n", "19", "voacapl", "-s", f"--run-dir={folder}", itshfbc, "in.dat", "out.out"],
                                  capture_output=True, text=True)
            path = os.path.join(folder, "out.out")
            if done.returncode != 0 or not os.path.exists(path):
                return args, None
            with open(path) as f:
                text = f.read()
            return args, to_local(parse(text), lon) if frequencies_read(text) else None

    os.makedirs(out_folder, exist_ok=True)
    failures = []
    for month in months or MONTHS:
        work = [(month, s, lat, km, lon, b) for s in SUNSPOTS for lat in LATITUDES for km in DISTANCES for lon in LONGITUDES
                for b in BEARINGS]
        samples = {}
        with ThreadPoolExecutor(parallel) as pool:
            for (m, s, lat, km, lon, b), hours in pool.map(one, work):
                if hours is None or len(hours) != 24:
                    failures.append([m, s, lat, km, lon, b])
                    continue
                samples.setdefault((s, lat, km), []).append(hours)
        cells = {f"{s}|{lat}|{km}": combine(v) for (s, lat, km), v in samples.items()}
        with open(os.path.join(out_folder, f"month-{month:02d}.json"), "w") as f:
            json.dump({"month": month, "cells": cells}, f, separators=(",", ":"))
        print(json.dumps({"month": month, "runs": len(work), "failures": len(failures)}), flush=True)
    return failures


def load_cells(folder):
    """The worker's month files, as pack() reads them."""
    cells = {}
    for month in MONTHS:
        path = os.path.join(folder, f"month-{month:02d}.json")
        if not os.path.exists(path):
            continue
        with open(path) as f:
            data = json.load(f)
        for key, hours in data["cells"].items():
            s, lat, km = (int(x) for x in key.split("|"))
            cells[(month, s, lat, km)] = {int(h): v for h, v in hours.items()}
    return cells


if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser(description="The worker, inside WSL: run the grid through voacapl.")
    parser.add_argument("out")
    parser.add_argument("--itshfbc", default="/root/itshfbc")
    parser.add_argument("--parallel", type=int, default=8)
    parser.add_argument("--months", type=int, nargs="*")
    args = parser.parse_args()
    failed = worker(args.out, args.itshfbc, args.parallel, args.months)
    print(json.dumps({"failures": len(failed), "first": failed[:5]}))
