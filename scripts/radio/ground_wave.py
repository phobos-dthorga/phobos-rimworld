"""HF's ground-wave table from NTIA's LF/MF model (docs/development/radio-hf.md, slice 1; the owner's cards of 28 Nov 2026: the
ground wave by ITU-R P.368, whose curves LFMF computes, built in WSL).

Game-free and standard library only, so the same module runs on Windows (the command, packing) and inside WSL (the worker that drives
LFMF's command-line driver). LFMF is never shipped: what ships is our packed table of its basic transmission loss.

The grid: ground, band, polarisation, the two antennas' heights and the distance. The grounds are ITU-R P.527-6's (the Attachment to
Annex 1, Figure 24, read by eye at 7 to 14 MHz, where every curve is flat).
"""
import os
import struct

# (name, relative permittivity, conductivity in S/m): ITU-R P.527-6, Figure 24, read by eye at HF.
GROUNDS = [
    ("sea", 70.0, 5.0),
    ("wet", 30.0, 0.01),
    ("fresh", 80.0, 0.003),
    ("medium", 15.0, 0.001),
    ("dry", 3.0, 0.0001),
    ("ice", 3.0, 0.00006),
]
# The same frequencies as the skywave tables (hf_tables.BANDS), so a band reads both at one frequency.
BANDS = {"80m": 3.60, "40m": 7.10, "30m": 10.12, "20m": 14.20, "17m": 18.10, "15m": 21.20, "12m": 24.94, "10m": 28.50}
POLARISATIONS = [0, 1]  # LFMF: 0 horizontal, 1 vertical
HEIGHTS = [2, 10, 30]   # metres; LFMF takes antennas under 50 m
DISTANCES = [1, 2, 3, 5, 7, 10, 15, 20, 30, 50, 70, 100, 150, 200, 300, 500]  # km
# LFMF's surface refractivity, as its own example uses (agent's choice).
N_S = 301
POWER_W = 1000


def deck(ground, mhz, pol, h_tx, h_rx, km):
    """LFMF's driver input: one key and value a line."""
    _, eps, sigma = ground
    return "\n".join([f"h_tx__meter,{h_tx}", f"h_rx__meter,{h_rx}", f"f__mhz,{mhz}", f"p_tx__watt,{POWER_W}", f"n_s,{N_S}",
                      f"d__km,{km}", f"epsilon,{eps}", f"sigma,{sigma}", f"pol,{pol}", ""])


def parse(text):
    """The basic transmission loss in dB from the driver's output, or None when it did not run cleanly."""
    loss, ok = None, False
    for line in text.splitlines():
        if line.startswith("Return Code"):
            ok = line.split()[2] == "0"
        elif line.startswith("Basic transmission loss"):
            try:
                loss = float(line.split()[3])
            except (IndexError, ValueError):
                return None
    return loss if ok else None


def jobs():
    return [(g, b, p, ht, hr, km) for g in range(len(GROUNDS)) for b in BANDS for p in POLARISATIONS for ht in HEIGHTS for hr in HEIGHTS
            for km in DISTANCES]


# ---- Packing: the binary the game reads (format 1) ----
MAGIC = b"PHGW"
FORMAT = 1


def pack(losses, bands):
    """
    Little-endian: a header naming every axis (grounds with their permittivity and conductivity, bands in kHz, polarisations, heights,
    distances), then a float32 loss in dB for each ground, band, polarisation, transmitter height, receiver height and distance, in that
    order. losses maps (ground index, band, pol, h_tx, h_rx, km) to dB; a missing run packs as NaN.
    """
    out = bytearray(MAGIC)
    out += struct.pack("<H", FORMAT)
    out += struct.pack("<B", len(GROUNDS))
    for _, eps, sigma in GROUNDS:
        out += struct.pack("<ff", eps, sigma)
    out += struct.pack("<B", len(bands)) + struct.pack(f"<{len(bands)}H", *(round(BANDS[b] * 1000) for b in bands))
    out += struct.pack("<B", len(POLARISATIONS)) + bytes(POLARISATIONS)
    out += struct.pack("<B", len(HEIGHTS)) + bytes(HEIGHTS)
    out += struct.pack("<B", len(DISTANCES)) + struct.pack(f"<{len(DISTANCES)}H", *DISTANCES)
    for g in range(len(GROUNDS)):
        for b in bands:
            for p in POLARISATIONS:
                for ht in HEIGHTS:
                    for hr in HEIGHTS:
                        for km in DISTANCES:
                            out += struct.pack("<f", losses.get((g, b, p, ht, hr, km), float("nan")))
    return bytes(out)


def unpack_header(data):
    if data[:4] != MAGIC:
        raise ValueError("not a ground-wave table")
    at = 4
    (fmt,) = struct.unpack_from("<H", data, at); at += 2
    (n,) = struct.unpack_from("<B", data, at); at += 1
    grounds = [struct.unpack_from("<ff", data, at + 8 * i) for i in range(n)]; at += 8 * n
    (nb,) = struct.unpack_from("<B", data, at); at += 1
    khz = list(struct.unpack_from(f"<{nb}H", data, at)); at += 2 * nb
    (np_,) = struct.unpack_from("<B", data, at); at += 1
    pols = list(data[at:at + np_]); at += np_
    (nh,) = struct.unpack_from("<B", data, at); at += 1
    heights = list(data[at:at + nh]); at += nh
    (nd,) = struct.unpack_from("<B", data, at); at += 1
    dists = list(struct.unpack_from(f"<{nd}H", data, at)); at += 2 * nd
    size = at + 4 * n * nb * np_ * nh * nh * nd
    return {"format": fmt, "grounds": grounds, "kHz": khz, "polarisations": pols, "heights": heights, "distances": dists, "losses": at,
            "size": size}


# ---- The worker, run inside WSL ----
def worker(out_path, driver, parallel):
    """Every point of the grid through LFMF's driver; writes one JSON file of losses."""
    import json
    import subprocess
    import tempfile
    from concurrent.futures import ThreadPoolExecutor

    def one(job):
        g, b, p, ht, hr, km = job
        with tempfile.TemporaryDirectory(dir="/tmp") as folder:
            src, dst = os.path.join(folder, "in.txt"), os.path.join(folder, "out.txt")
            with open(src, "w") as f:
                f.write(deck(GROUNDS[g], BANDS[b], p, ht, hr, km))
            subprocess.run(["nice", "-n", "19", driver, "-i", src, "-o", dst], capture_output=True)
            if not os.path.exists(dst):
                return job, None
            with open(dst) as f:
                return job, parse(f.read())

    results, failures = {}, []
    with ThreadPoolExecutor(parallel) as pool:
        for job, loss in pool.map(one, jobs()):
            if loss is None:
                failures.append(list(job))
            else:
                results["|".join(map(str, job))] = loss
    with open(out_path, "w") as f:
        json.dump({"grounds": GROUNDS, "losses": results}, f, separators=(",", ":"))
    return failures


def load(path):
    import json
    with open(path) as f:
        data = json.load(f)
    out = {}
    for key, loss in data["losses"].items():
        g, b, p, ht, hr, km = key.split("|")
        out[(int(g), b, int(p), int(ht), int(hr), int(km))] = loss
    return out


if __name__ == "__main__":
    import argparse
    import json
    parser = argparse.ArgumentParser(description="The worker, inside WSL: the ground-wave grid through LFMF's driver.")
    parser.add_argument("out")
    parser.add_argument("--driver", default="/opt/lfmf/bin/LFMFDriver-1.1.0-Linux-x86_64")
    parser.add_argument("--parallel", type=int, default=8)
    args = parser.parse_args()
    failed = worker(args.out, args.driver, args.parallel)
    print(json.dumps({"runs": len(jobs()), "failures": len(failed), "first": failed[:5]}))
