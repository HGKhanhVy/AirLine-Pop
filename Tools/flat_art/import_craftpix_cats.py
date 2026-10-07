"""Turns the Craftpix "Cute Cats" character sprites into the game's passenger cats.

The pack (craftpix.net, free licence: fine to ship inside the game, not to resell as
assets) has fifteen chibi cats, many dressed for a trip: backpacks, caps, goggles. Each
regular gets one of them, matched by fur colour; the pack has no black or grey cat, so
Mun and Tro are recoloured from its flat palette, anti-aliased edges included.

For every regular this writes, into Assets/_Game/Art/FlatCats:
- Motion/cat_<id>_<Clip>.png: flip-book sheets (six frames a row) mirrored to face left,
  like the rest of the cat art, plus Motion/cat_motion.json with the frame size and pivot;
- passenger_<id>_sit.png / _happy.png: the cat waiting on a board square and hopping;
and the HUD's cat icon from Bơ's face.

    python Tools/flat_art/import_craftpix_cats.py [path to the unzipped pack]
    python Tools/flat_art/import_craftpix_cats.py --dress    (only re-dress the guests)
"""
import json
import os
import sys

from PIL import Image

HERE = os.path.dirname(__file__)
DEFAULT_PACK = os.path.join(os.path.expanduser("~"), "Downloads", "craftpix-net-532103-cute-cats-game-character-sprites")
CATS = os.path.join(HERE, "..", "..", "Assets", "_Game", "Art", "FlatCats")
MOTION = os.path.join(CATS, "Motion")
UI = os.path.join(HERE, "..", "..", "Assets", "_Game", "Art", "UIKit")

OUTLINE = (0x64, 0x38, 0x35)
# Frames keep the pack's own resolution so they stay crisp on dense phone screens.
SCALE = 1.0
SHEET_COLUMNS = 6
# A pack cat is about 245 px from ears to feet; this makes it about 0.63 units tall, about
# the size of the old sitting cats, so rosters keep their scales.
PIXELS_PER_UNIT = 390


def hexc(h):
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


# Fur recolours: source palette colour -> new colour. Clothes and outline stay as drawn.
# Regulars come from the pack's plain cats, with no glasses or hat of their own, so the
# accessories they unlock sit on a bare head instead of over built-in props.
MUN = {  # charcoal with a white eye patch, from the plain peach cat
    "f3cba8": "6f6a7c", "bb927b": "5b5768", "c28862": "524e5e", "a77d6b": "56525f",
    "8e5b42": "403c48", "9e6951": "47434f", "905e4a": "403c48", "ffbb56": "807b8d",
    "f4e8c2": "e9e6ee",
}
TRO = {  # ash grey with white, from the plain cream calico
    "f4e8c2": "eef0f4", "bca48b": "c3c8d3", "f9ab47": "a9b1c0", "bf7e40": "8a92a2",
    "c28862": "959dad", "9e6951": "7b8394", "905e4a": "6c7484", "8e5b42": "6c7484",
    "ffbb56": "dfe3ea",
}

REGULARS = {
    "bo": ("Character03", None),     # ginger, cap and backpack: the mascot
    "kem": ("Character07", None),    # cream calico with a blue backpack
    "muop": ("Character04", None),   # brown with a yellow backpack
    "mun": ("Character09", MUN),     # black cat with a white eye patch
    "tro": ("Character05", TRO),     # grey and white
}

# Visitors drawn the same way but never seated on the board: the VIP guest, crown and cape.
GUESTS = {
    "vip": ("Character06", None),
}

# Guests dressed up after import, by id: His Majesty wears imperial yellow.
DRESS = {
    "vip": "royal_robe",
}

# Clip -> (pack animation, take every n-th frame, seconds, loops)
CLIPS = {
    "Idle": ("Idle", 2, 1.0, True),
    "Walk": ("Walk", 2, 0.8, True),
    "Jump": ("Jump", 2, 0.8, True),
    "Roll": ("Roll", 1, 0.45, True),
    "Dizzy": ("Stuned", 2, 1.0, True),
}


# ------------------------------------------------------------------ recolour

def recolour(img, mapping):
    """Swaps palette colours, carrying the swap through edge pixels that blend a fur colour
    with the outline or with another fur colour, so no old colour fringes remain."""
    if not mapping:
        return img
    pairs = [(hexc(a), hexc(b)) for a, b in mapping.items()]
    anchors = [OUTLINE] + [a for a, _ in pairs]
    targets = {OUTLINE: OUTLINE}
    targets.update({a: b for a, b in pairs})
    cache = {}

    def swap(rgb):
        if rgb in cache:
            return cache[rgb]
        best, best_err = rgb, 9.0
        # A pixel is either a palette colour or a mix of two; find the closest such mix.
        for i, a in enumerate(anchors):
            for b in anchors[i:]:
                d = [b[k] - a[k] for k in range(3)]
                dd = sum(v * v for v in d)
                t = 0.0 if dd == 0 else max(0.0, min(1.0, sum((rgb[k] - a[k]) * d[k] for k in range(3)) / dd))
                mix = [a[k] + d[k] * t for k in range(3)]
                err = sum((rgb[k] - mix[k]) ** 2 for k in range(3)) ** 0.5
                if err < best_err and (a != OUTLINE or b != OUTLINE):
                    ta, tb = targets[a], targets[b]
                    best_err = err
                    best = tuple(int(round(ta[k] + (tb[k] - ta[k]) * t + (rgb[k] - mix[k]))) for k in range(3))
        best = tuple(max(0, min(255, v)) for v in best)
        cache[rgb] = best
        return best

    out = img.copy()
    px = out.load()
    w, h = out.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a:
                px[x, y] = swap((r, g, b)) + (a,)
    return out


def royal_robe(img):
    """Dresses the VIP guest as His Majesty: the pack's teal hoodie turns imperial yellow
    and the teal band of his crown turns red. Both are the same teal, so they are told
    apart by shape: the band is a wide flat strip, the hoodie and its sleeves are not.
    Shading is kept by carrying each pixel's brightness over. Safe to run twice."""
    import numpy as np
    from scipy import ndimage

    a = np.asarray(img.convert("RGBA")).astype(np.float32) / 255
    rgb = a[..., :3]
    mx, mn = rgb.max(-1), rgb.min(-1)
    d = mx - mn
    sat = np.where(mx > 0, d / np.maximum(mx, 1e-6), 0)
    hue = np.zeros_like(mx)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    on = d > 1e-6
    is_r = on & (mx == r)
    is_g = on & (mx == g) & ~is_r
    is_b = on & ~is_r & ~is_g
    hue[is_r] = ((g - b)[is_r] / d[is_r]) % 6
    hue[is_g] = (b - r)[is_g] / d[is_g] + 2
    hue[is_b] = (r - g)[is_b] / d[is_b] + 4
    hue *= 60
    teal = (hue > 170) & (hue < 215) & (sat > 0.25) & (mx > 0.2) & (a[..., 3] > 0.05)
    # Anti-aliased edges are a paler teal; they follow whichever part they border.
    fringe = (hue > 165) & (hue < 220) & (sat > 0.1) & (a[..., 3] > 0.02)

    labels, count = ndimage.label(teal)
    if count == 0:
        return img
    sizes = ndimage.sum(teal, labels, range(1, count + 1))
    largest = sizes.max()
    band = np.zeros_like(teal)
    robe = np.zeros_like(teal)
    for i, (box, size) in enumerate(zip(ndimage.find_objects(labels), sizes)):
        part = labels[box] == i + 1
        tall = box[0].stop - box[0].start
        wide = box[1].stop - box[1].start
        (band if size < 0.3 * largest and wide > 1.8 * tall else robe)[box] |= part
    grow = np.ones((3, 3), bool)
    band |= fringe & ndimage.binary_dilation(band, grow, 2) & ~robe
    robe |= fringe & ndimage.binary_dilation(robe, grow, 2) & ~band

    def paint(mask, hue_deg, saturation, lift):
        v = np.clip(mx[mask] * lift, 0, 1)
        c = v * saturation
        h = hue_deg / 60.0
        x = c * (1 - abs(h % 2 - 1))
        sector = int(h) % 6
        parts = [(c, x, 0), (x, c, 0), (0, c, x), (0, x, c), (x, 0, c), (c, 0, x)][sector]
        m = v - c
        for k in range(3):
            rgb[..., k][mask] = (parts[k] if not np.isscalar(parts[k]) else np.full_like(v, parts[k])) + m

    paint(robe, 44.0, 0.78, 1.45)
    paint(band, 356.0, 0.72, 1.25)
    out = np.dstack([np.clip(rgb, 0, 1), a[..., 3:]])
    return Image.fromarray((out * 255).astype(np.uint8), "RGBA")


def dress_guest_sheets():
    """Applies each guest's outfit to its imported sheets."""
    for breed, outfit in DRESS.items():
        for clip in CLIPS:
            path = os.path.join(MOTION, "cat_%s_%s.png" % (breed, clip))
            if os.path.exists(path):
                globals()[outfit](Image.open(path)).save(path)
        print("dressed", breed, "in", outfit, flush=True)


# ------------------------------------------------------------------ frames

def frame_paths(pack, character, animation):
    folder = os.path.join(pack, "Png", character, animation)
    names = [n for n in os.listdir(folder) if n.endswith(".png")]
    return [os.path.join(folder, n) for n in sorted(names, key=lambda n: int(n.rsplit("_", 1)[1][:-4]))]


def pick(paths, step):
    return paths[::step]


def union_box(boxes):
    boxes = [b for b in boxes if b]
    return (min(b[0] for b in boxes), min(b[1] for b in boxes), max(b[2] for b in boxes), max(b[3] for b in boxes))


def ground_point(img):
    """Centre of the soft floor shadow under the cat: where it stands."""
    px = img.load()
    w, h = img.size
    xs, ys = [], []
    for y in range(h // 2, h):
        for x in range(w):
            r, g, b, a = px[x, y]
            # The pack's shadow is the outline brown at about a quarter opacity.
            if 40 < a < 110 and abs(r - OUTLINE[0]) + abs(g - OUTLINE[1]) + abs(b - OUTLINE[2]) < 24:
                xs.append(x)
                ys.append(y)
    return (sum(xs) / len(xs), sum(ys) / len(ys))


def main():
    pack = sys.argv[1] if len(sys.argv) > 1 else DEFAULT_PACK
    os.makedirs(MOTION, exist_ok=True)

    # One crop for every cat and clip, so all frames share a size and a standing point.
    boxes = []
    for character, _ in list(REGULARS.values()) + list(GUESTS.values()):
        for animation, step, _, _ in CLIPS.values():
            for path in pick(frame_paths(pack, character, animation), step):
                boxes.append(Image.open(path).getbbox())
    box = union_box(boxes)
    pad = 6
    box = (box[0] - pad, box[1] - pad, box[2] + pad, box[3] + pad)
    width, height = box[2] - box[0], box[3] - box[1]
    out_w, out_h = int(width * SCALE), int(height * SCALE)

    first = Image.open(frame_paths(pack, REGULARS["bo"][0], "Idle")[0]).convert("RGBA")
    gx, gy = ground_point(first)
    # Mirrored: the pack's cats face right, the game's face left.
    pivot_x = (box[2] - gx) * SCALE
    pivot_y = (box[3] - gy) * SCALE

    def prepare(path, mapping):
        img = recolour(Image.open(path).convert("RGBA").crop(box), mapping)
        img = img.transpose(Image.FLIP_LEFT_RIGHT)
        return img.resize((out_w, out_h), Image.LANCZOS)

    clips_meta = []
    for breed, (character, mapping) in list(REGULARS.items()) + list(GUESTS.items()):
        for clip, (animation, step, seconds, loop) in CLIPS.items():
            frames = [prepare(p, mapping) for p in pick(frame_paths(pack, character, animation), step)]
            cols = min(SHEET_COLUMNS, len(frames))
            rows = (len(frames) + cols - 1) // cols
            sheet = Image.new("RGBA", (cols * out_w, rows * out_h), (0, 0, 0, 0))
            for i, f in enumerate(frames):
                sheet.alpha_composite(f, ((i % cols) * out_w, (i // cols) * out_h))
            sheet.save(os.path.join(MOTION, "cat_%s_%s.png" % (breed, clip)))
            if breed == "bo":
                clips_meta.append({"name": clip, "count": len(frames), "columns": cols, "seconds": seconds, "loop": loop})
        if breed in REGULARS:
            passengers(pack, breed, character, mapping, box, (gx, gy))
        print("imported", breed, "from", character, flush=True)

    with open(os.path.join(MOTION, "cat_motion.json"), "w") as fh:
        json.dump({"frameWidth": out_w, "frameHeight": out_h, "pivotX": pivot_x, "pivotY": pivot_y,
                   "pixelsPerUnit": PIXELS_PER_UNIT, "clips": clips_meta}, fh, indent=1)

    dress_guest_sheets()
    icon(pack)


# ------------------------------------------------------------------ board passengers and icon

PASSENGER = 256
# The shadow's centre in the 256 px sprite: just above the board pivot line (492 / 512 =
# 246 px), so the whole shadow fits in the canvas.
PASSENGER_FEET = 238
PASSENGER_HEIGHT = 196    # cat height in the sprite, ears to feet; leaves room for the tail and shadow
PASSENGER_MARGIN = 4      # clear pixels kept above the ears


def passengers(pack, breed, character, mapping, box, ground):
    sit = Image.open(frame_paths(pack, character, "Idle")[0]).convert("RGBA")
    # Just off the ground rather than the top of the jump: the peak lifts the cat right
    # out of the sprite's canvas.
    jumps = frame_paths(pack, character, "Jump")
    happy = Image.open(jumps[len(jumps) // 5]).convert("RGBA")
    # One scale for both, from the standing cat, so a hop lifts the cat rather than shrinking it.
    scale = PASSENGER_HEIGHT / max(1, ground[1] - sit.getbbox()[1])
    for name, img in (("sit", sit), ("happy", happy)):
        img = recolour(img, mapping).transpose(Image.FLIP_LEFT_RIGHT)
        gx = img.width - ground[0]
        img = img.resize((int(img.width * scale), int(img.height * scale)), Image.LANCZOS)
        x = int(PASSENGER / 2 - gx * scale)
        y = int(PASSENGER_FEET - ground[1] * scale)
        # Keep the whole cat on the canvas: the shadow is lifted until it fits at the
        # bottom, and a hop is lowered until the ears fit at the top.
        box = img.getbbox()
        y = min(y, PASSENGER - PASSENGER_MARGIN - box[3])
        y = max(y, PASSENGER_MARGIN - box[1])
        out = Image.new("RGBA", (PASSENGER, PASSENGER), (0, 0, 0, 0))
        out.alpha_composite(img, (x, y))
        out.save(os.path.join(CATS, "passenger_%s_%s.png" % (breed, name)))


def icon(pack):
    """The HUD's passenger icon: Bơ's face, cropped square."""
    img = Image.open(frame_paths(pack, REGULARS["bo"][0], "Idle")[0]).convert("RGBA")
    img = img.transpose(Image.FLIP_LEFT_RIGHT)
    left, top, right, bottom = img.getbbox()
    side = int((right - left) * 0.78)
    cx = (left + right) // 2
    face = img.crop((cx - side // 2, top, cx + side // 2, top + side))
    face.resize((256, 256), Image.LANCZOS).save(os.path.join(UI, "icon_cat.png"))


if __name__ == "__main__":
    if "--dress" in sys.argv:
        dress_guest_sheets()
    else:
        main()
