"""How candidates are ordered for review and how copies are spotted. Game-free and Pillow-free, so it can be tested. The
weights and thresholds are authored starting values. They only choose what the owner sees first: nothing is ever deleted.
"""
SHARPNESS_FULL_MARKS = 400.0  # Edge variance at which an image scores full marks for sharpness (authored).
CONTENT_MIN, CONTENT_MAX = 0.15, 0.85  # A subject filling this share of the frame is framed sensibly (authored).
DUPLICATE_DISTANCE = 6  # Hash bits of 64 within which two candidates count as the same picture (authored).
# Set from one test on 5 Nov 2026: copies, blurs and recolours of vanilla sprites against unrelated art (which reached 0.43 and
# 17 bits at closest). A copy tilted a few degrees can still pass, so the owner's eye stays the last check.
NEAR_COPY_DISTANCE = 12  # Within this many bits of a source image, an output is flagged (authored).
NEAR_COPY_SSIM = 0.60  # At or above this structural similarity to a source image, an output is flagged (authored).
# A copy keeps its subject's proportions: measured 12 Nov 2026, pastes, blurs, recolours and 4-degree tilts of 12 game sprites
# stayed within 1.13 times of their source's width to height, while every hash-only flag that turned out false was 1.96 times
# or more apart (the owner's fuel tank against the game's wide 1 by 3 altar: 13 to 16 times). Authored, with room above 1.13.
SAME_PROPORTIONS = 1.35
BLANK_LEVEL = 235  # A window whose darkest pixel is this pale or paler has nothing drawn in it (export_sprite.py's background level).


def score(measure):
    """0 to 1. Half for a clean plain background (exporting needs one), the rest for sharpness and sensible framing."""
    background = max(0.0, min(1.0, measure['border_white']))
    sharpness = max(0.0, min(1.0, measure['sharpness'] / SHARPNESS_FULL_MARKS))
    framed = 1.0 if CONTENT_MIN <= measure['content'] <= CONTENT_MAX else 0.0
    return round(0.5 * background + 0.3 * sharpness + 0.2 * framed, 3)


def hamming(a, b):
    return bin(a ^ b).count('1')


def rank(measures, max_distance=DUPLICATE_DISTANCE):
    """Best first, with each repeat marked by the better image it repeats. Input and output are lists of dicts."""
    ordered = sorted(({**m, 'score': score(m)} for m in measures), key=lambda m: (-m['score'], m['file']))
    kept = []
    for measure in ordered:
        twin = next((k for k in kept if hamming(k['hash'], measure['hash']) <= max_distance), None)
        if twin:
            measure['duplicate_of'] = twin['file']
        else:
            kept.append(measure)
    return ordered


def compares_structure(darkest_first, darkest_second, blank_level=BLANK_LEVEL):
    """Whether a window of the structural comparison counts: only where at least one picture has something drawn. Two blank
    windows are equal by construction, so counting them made any thin subject on white look like any sprite with empty corners
    (12 Nov 2026: a power cable scored 0.63 against the game's double slab bed, whose outline is nothing like it)."""
    return min(darkest_first, darkest_second) < blank_level


def mean_or_zero(values):
    """The mean of the windows that counted; nothing drawn in either picture shares no structure."""
    return sum(values) / len(values) if values else 0.0


def same_proportions(first_ratio, second_ratio, max_factor=SAME_PROPORTIONS):
    """Whether two subjects, cropped, have about the same width to height: a copy does, whatever was done to it."""
    if first_ratio <= 0 or second_ratio <= 0:
        return False
    return max(first_ratio, second_ratio) / min(first_ratio, second_ratio) <= max_factor


def is_near_copy(distance, ssim, max_distance=NEAR_COPY_DISTANCE, min_ssim=NEAR_COPY_SSIM, proportions_agree=True):
    """Either signal is enough: the check exists so nothing ships as a copy, so it errs towards flagging. The hash counts only
    between subjects of about the same proportions: each is stretched to a square before it is hashed, so a long thin subject
    hashes as rows of the same shading, and matched any sprite that stretches the same way, upright or wide (owner, 12 Nov
    2026: "there is no tank like it in the original game"). The structural score is unaffected."""
    return (distance <= max_distance and proportions_agree) or ssim >= min_ssim
