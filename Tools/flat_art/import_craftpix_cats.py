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
MUN = {  # charcoal, from the peach cat in shutter shades
    "f3cba8": "5a5664", "bb927b": "48444f", "c28862": "413d48", "9e6951": "35323b",
    "a77d6b": "3d3944", "8e5b42": "2f2c34", "905e4a": "2f2c34", "ffbb56": "6e6979",
}
TRO = {  # ash grey with white, from the cream cat in swimming goggles
    "f4e8c2": "eef0f4", "bca48b": "c3c8d3", "f9ab47": "a9b1c0", "bf7e40": "8a92a2",
    "c28862": "959dad", "9e6951": "7b8394", "905e4a": "6c7484", "8e5b42": "6c7484",
    "ffbb56": "dfe3ea",
}

REGULARS = {
    "bo": ("Character03", None),     # ginger, cap and backpack: the mascot
    "kem": ("Character07", None),    # cream calico with a blue backpack
    "muop": ("Character04", None),   # brown with a yellow backpack
    "mun": ("Character14", MUN),     # black cat in shades
    "tro": ("Character08", TRO),     # grey in goggles
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
    for character, _ in REGULARS.values():
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
    for breed, (character, mapping) in REGULARS.items():
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
        passengers(pack, breed, character, mapping, box, (gx, gy))
        print("imported", breed, "from", character, flush=True)

    with open(os.path.join(MOTION, "cat_motion.json"), "w") as fh:
        json.dump({"frameWidth": out_w, "frameHeight": out_h, "pivotX": pivot_x, "pivotY": pivot_y,
                   "pixelsPerUnit": PIXELS_PER_UNIT, "clips": clips_meta}, fh, indent=1)

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
    main()
