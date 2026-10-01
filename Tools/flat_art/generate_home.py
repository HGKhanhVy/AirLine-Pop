"""Generates the flat 2D Home art for AirLine Pop: the cat room and the little airport.

Same language as the board and the cats: flat fills, warm brown outline, soft pastel
palette, neutral light (no yellow cast). Flat pieces (floor, rug, grass, runway) lie on
the ground; upright pieces (furniture, buildings, trees) are drawn front-on and stand
facing the camera in Unity.

Outlines here are drawn as a larger brown shape under the fill rather than grown from
a mask, which keeps the big pieces fast to generate.

    python Tools/flat_art/generate_home.py
"""
import math
import os
import random

from PIL import Image, ImageChops, ImageDraw, ImageFilter

from puff import puff

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Art", "FlatHome")
SS = 3
PPU = 128
LINE = 6  # outline width in final pixels

BROWN = (100, 56, 53, 255)  # the passengers' outline
WHITE = (255, 255, 255, 255)
CREAM = (255, 250, 241, 255)
CREAM_DARK = (238, 228, 214, 255)
WOOD = (222, 186, 154, 255)
WOOD_ALT = (212, 174, 142, 255)
WOOD_DARK = (176, 132, 100, 255)
GREEN = (126, 196, 132, 255)
GREEN_DARK = (92, 164, 104, 255)
GREEN_LIGHT = (176, 222, 170, 255)
MINT = (196, 230, 214, 255)
SAGE = (192, 216, 200, 255)
CORAL = (240, 124, 108, 255)
CORAL_DARK = (206, 92, 82, 255)
PINK = (246, 184, 188, 255)
SKY = (178, 222, 244, 255)
SKY_DARK = (140, 196, 228, 255)
TERRACOTTA = (218, 132, 100, 255)
ASPHALT = (108, 118, 140, 255)
STONE = (190, 196, 206, 255)


class Canvas:
    """A supersampled drawing surface measured in final pixels."""

    def __init__(self, w, h):
        self.w, self.h = w, h
        self.img = Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.img)

    def _box(self, box):
        return [v * SS for v in box]

    def _puff(self, draw_mask, puffy):
        """Shades the shape just filled, when it is a foreground piece (it has an outline)."""
        if not puffy:
            return
        m = Image.new("L", self.img.size, 0)
        draw_mask(ImageDraw.Draw(m))
        puff(self.img, m, SS)

    def rect(self, box, r, fill, line=LINE, puffy=True):
        if line:
            x0, y0, x1, y1 = box
            self.d.rounded_rectangle(self._box((x0 - line, y0 - line, x1 + line, y1 + line)),
                                     radius=(r + line) * SS, fill=BROWN)
        self.d.rounded_rectangle(self._box(box), radius=max(0, r) * SS, fill=fill)
        self._puff(lambda d: d.rounded_rectangle(self._box(box), radius=max(0, r) * SS, fill=255), puffy and line)

    def oval(self, box, fill, line=LINE, puffy=True):
        if line:
            x0, y0, x1, y1 = box
            self.d.ellipse(self._box((x0 - line, y0 - line, x1 + line, y1 + line)), fill=BROWN)
        self.d.ellipse(self._box(box), fill=fill)
        self._puff(lambda d: d.ellipse(self._box(box), fill=255), puffy and line)

    def poly(self, points, fill, line=LINE, puffy=True):
        pts = [(x * SS, y * SS) for x, y in points]
        if line:
            self.d.line(pts + pts[:1], fill=BROWN, width=line * 2 * SS, joint="curve")
            for x, y in pts:
                r = line * SS
                self.d.ellipse([x - r, y - r, x + r, y + r], fill=BROWN)
        self.d.polygon(pts, fill=fill)
        self._puff(lambda d: d.polygon(pts, fill=255), puffy and line)

    def line(self, points, fill, width):
        pts = [(x * SS, y * SS) for x, y in points]
        self.d.line(pts, fill=fill, width=int(width * SS), joint="curve")
        r = width * SS / 2
        for x, y in (pts[0], pts[-1]):
            self.d.ellipse([x - r, y - r, x + r, y + r], fill=fill)

    def save(self, name):
        os.makedirs(OUT, exist_ok=True)
        self.img.resize((self.w, self.h), Image.LANCZOS).save(os.path.join(OUT, name + ".png"))


def units(u):
    return int(round(u * PPU))


# ------------------------------------------------------------------ room

def room_floor():
    """Planks running away from the camera, 1 unit wide, with staggered ends."""
    w, h = units(10), units(12)
    c = Canvas(w, h)
    rng = random.Random(7)
    plank = PPU
    for i in range(w // plank):
        x0 = i * plank
        y = -rng.randint(0, 3 * PPU)
        colour = WOOD if i % 2 == 0 else WOOD_ALT
        while y < h:
            length = rng.randint(3 * PPU, 5 * PPU)
            c.d.rectangle(c._box((x0, y, x0 + plank, y + length)), fill=colour)
            c.d.line(c._box((x0, y, x0 + plank, y)), fill=WOOD_DARK, width=3 * SS)
            # A few grain streaks.
            for _ in range(3):
                gx = x0 + rng.randint(18, plank - 18)
                gy = y + rng.randint(20, max(21, length - 60))
                c.d.line(c._box((gx, gy, gx, gy + rng.randint(24, 60))), fill=(200, 160, 128, 160), width=2 * SS)
            y += length
        c.d.line(c._box((x0, 0, x0, h)), fill=WOOD_DARK, width=3 * SS)
    c.save("room_floor")


def room_wall():
    """Back wall: cream wallpaper with a mint dot pattern over a sage wainscot."""
    w, h = units(10), units(4)
    c = Canvas(w, h)
    c.d.rectangle(c._box((0, 0, w, h)), fill=CREAM)

    # Wallpaper: offset rows of tiny paw-like dots.
    for row, y in enumerate(range(22, h - units(1.2), 48)):
        for x in range(24 + (row % 2) * 32, w, 64):
            c.d.ellipse(c._box((x - 5, y - 5, x + 5, y + 5)), fill=(206, 234, 222, 255))

    # Wainscot with board seams, a cap rail and a skirting board.
    wains_top = h - units(1.15)
    c.d.rectangle(c._box((0, wains_top, w, h)), fill=SAGE)
    for x in range(0, w, 64):
        c.d.line(c._box((x, wains_top, x, h)), fill=(168, 196, 180, 255), width=3 * SS)
    c.d.rectangle(c._box((0, wains_top - 10, w, wains_top + 4)), fill=WOOD)
    c.d.line(c._box((0, wains_top + 4, w, wains_top + 4)), fill=BROWN, width=3 * SS)
    c.d.line(c._box((0, wains_top - 10, w, wains_top - 10)), fill=BROWN, width=3 * SS)
    c.d.rectangle(c._box((0, h - 22, w, h)), fill=WOOD_DARK)
    c.d.line(c._box((0, h - 22, w, h - 22)), fill=BROWN, width=3 * SS)

    # Window left of centre: room x -1.6 on a wall spanning -5..5.
    wx = units(5 - 1.6)
    wy0, wy1 = h - units(2.95), h - units(1.45)
    ww = units(0.95)
    c.rect((wx - ww, wy0, wx + ww, wy1), 10, WOOD)
    c.rect((wx - ww + 14, wy0 + 14, wx + ww - 14, wy1 - 14), 4, SKY, line=3)
    # A cloud and a far-off plane outside.
    for cx, cy, r in ((wx - 40, wy0 + 110, 26), (wx - 10, wy0 + 96, 34), (wx + 26, wy0 + 110, 24)):
        c.d.ellipse(c._box((cx - r, cy - r, cx + r, cy + r)), fill=WHITE)
    c.d.rectangle(c._box((wx - 64, wy0 + 110, wx + 50, wy0 + 136)), fill=WHITE)
    c.line([(wx + 50, wy0 + 48), (wx + 90, wy0 + 40)], WHITE, 6)
    c.d.line(c._box((wx, wy0 + 14, wx, wy1 - 14)), fill=WOOD, width=10 * SS)
    c.d.line(c._box((wx - ww + 14, (wy0 + wy1) / 2, wx + ww - 14, (wy0 + wy1) / 2)), fill=WOOD, width=10 * SS)
    # Curtains either side, and a sill.
    for side in (-1, 1):
        x_out = wx + side * (ww + 30)
        x_in = wx + side * (ww - 26)
        pts = [(x_out, wy0 - 26), (x_in, wy0 - 26), (x_in + side * 16, wy1 - 50), (x_out + side * 4, wy1 + 20)]
        c.poly(pts, PINK, line=4)
    c.d.rounded_rectangle(c._box((wx - ww - 40, wy0 - 40, wx + ww + 40, wy0 - 22)), radius=9 * SS, fill=WOOD_DARK)
    c.rect((wx - ww - 20, wy1 - 4, wx + ww + 20, wy1 + 12), 5, WOOD, line=4)

    # A framed picture of a cat-shaped plane, right of centre.
    px, py = units(5 + 2.3), h - units(2.6)
    c.rect((px - 70, py - 56, px + 70, py + 56), 8, CORAL)
    c.rect((px - 54, py - 40, px + 54, py + 40), 4, SKY, line=3)
    c.oval((px - 26, py - 16, px + 26, py + 22), CREAM, line=3)
    c.poly([(px - 24, py - 6), (px - 20, py - 34), (px - 4, py - 16)], CREAM, line=3)
    c.poly([(px + 24, py - 6), (px + 20, py - 34), (px + 4, py - 16)], CREAM, line=3)
    c.d.ellipse(c._box((px - 12, py - 2, px - 4, py + 6)), fill=BROWN)
    c.d.ellipse(c._box((px + 4, py - 2, px + 12, py + 6)), fill=BROWN)

    # A little shelf with a mug and books, far right.
    sx, sy = units(5 + 3.9), h - units(2.1)
    c.rect((sx - 70, sy, sx + 70, sy + 14), 4, WOOD, line=4)
    c.rect((sx - 56, sy - 44, sx - 40, sy - 4), 3, CORAL, line=3)
    c.rect((sx - 36, sy - 50, sx - 20, sy - 4), 3, MINT, line=3)
    c.rect((sx - 16, sy - 40, sx, sy - 4), 3, SKY_DARK, line=3)
    c.rect((sx + 16, sy - 36, sx + 50, sy - 4), 8, CREAM, line=3)
    c.save("room_wall")


def rug():
    w, h = units(3.8), units(2.8)
    c = Canvas(w, h)
    c.oval((8, 8, w - 8, h - 8), MINT, puffy=False)
    c.d.ellipse(c._box((34, 34, w - 34, h - 34)), outline=CREAM, width=12 * SS)
    c.d.ellipse(c._box((70, 70, w - 70, h - 70)), outline=(170, 214, 196, 255), width=6 * SS)
    c.save("rug")


def sofa():
    w, h = units(3.1), units(1.7)
    c = Canvas(w, h)
    m = 10
    base = h - 26
    # Legs.
    for x in (46, w - 60):
        c.rect((x, base - 10, x + 16, base + 16), 4, WOOD_DARK, line=4)
    # Back, seat, arms.
    c.rect((m + 34, m + 20, w - m - 34, base - 70), 34, GREEN)
    c.rect((m + 20, base - 100, w - m - 20, base - 4), 26, GREEN_DARK)
    c.rect((m + 36, base - 116, w // 2 - 4, base - 60), 22, GREEN_LIGHT, line=4)
    c.rect((w // 2 + 4, base - 116, w - m - 36, base - 60), 22, GREEN_LIGHT, line=4)
    c.rect((m, base - 150, m + 58, base - 4), 26, GREEN)
    c.rect((w - m - 58, base - 150, w - m, base - 4), 26, GREEN)
    # Pillows.
    c.rect((m + 70, m + 56, m + 150, m + 126), 20, CORAL, line=4)
    c.rect((w - m - 160, m + 60, w - m - 80, m + 128), 20, CREAM, line=4)
    c.d.ellipse(c._box((w - m - 130, m + 84, w - m - 110, m + 104)), fill=PINK)
    c.save("sofa")


def cat_bed():
    w, h = units(1.5), units(0.85)
    c = Canvas(w, h)
    c.oval((8, 20, w - 8, h - 8), CORAL)
    c.oval((34, 20, w - 34, h - 40), CREAM, line=4)
    c.d.ellipse(c._box((40, 26, w - 40, h - 60)), fill=(244, 236, 226, 255))
    c.d.arc(c._box((14, 34, w - 14, h - 10)), 20, 160, fill=CORAL_DARK, width=5 * SS)
    c.save("cat_bed")


def bowl():
    w, h = units(0.8), units(0.5)
    c = Canvas(w, h)
    # Kibble heaped above the rim.
    for cx, cy in ((26, 26), (40, 20), (54, 26), (68, 22), (80, 28), (46, 30), (62, 30)):
        c.oval((cx - 8, cy - 7, cx + 8, cy + 7), WOOD_DARK, line=2)
    c.poly([(10, 30), (w - 10, 30), (w - 24, h - 8), (24, h - 8)], CORAL)
    c.d.rectangle(c._box((14, 30, w - 14, 38)), fill=CORAL_DARK)
    # A fish mark.
    fx, fy = w / 2, 46
    c.d.ellipse(c._box((fx - 12, fy - 6, fx + 8, fy + 6)), fill=CREAM)
    c.d.polygon([((fx + 6) * SS, fy * SS), ((fx + 16) * SS, (fy - 7) * SS), ((fx + 16) * SS, (fy + 7) * SS)], fill=CREAM)
    c.save("bowl")


def yarn():
    s = units(0.55)
    c = Canvas(s + 30, s)
    r = s / 2 - 8
    cx, cy = s / 2, s / 2
    c.line([(cx + r * 0.6, cy + r * 0.7), (cx + r + 10, cy + r - 2), (s + 22, cy + r - 12)], PINK, 5)
    c.oval((cx - r, cy - r, cx + r, cy + r), PINK)
    for a in (-40, 0, 40):
        rad = math.radians(a)
        c.d.arc(c._box((cx - r + 8, cy - r * 0.9, cx + r - 8, cy + r * 0.9)), 200 + a, 340 + a, fill=(222, 140, 150, 255), width=4 * SS)
    c.d.arc(c._box((cx - r * 0.7, cy - r * 0.7, cx + r * 0.7, cy + r * 0.7)), 100, 250, fill=(222, 140, 150, 255), width=4 * SS)
    c.save("yarn")


def scratch_post():
    w, h = units(1.1), units(2.3)
    c = Canvas(w, h)
    cx = w / 2
    base = h - 12
    c.rect((10, base - 34, w - 10, base), 12, CREAM)
    c.rect((cx - 22, 80, cx + 22, base - 30), 6, (222, 204, 176, 255))
    for y in range(92, int(base - 36), 14):
        c.d.line(c._box((cx - 20, y, cx + 20, y + 6)), fill=(190, 166, 132, 255), width=3 * SS)
    c.rect((6, 52, w - 6, 88), 14, CORAL)
    c.rect((20, 30, w - 20, 60), 14, PINK, line=4)
    # A toy mouse on a string.
    c.line([(w - 22, 84), (w - 22, 170)], BROWN, 3)
    c.oval((w - 42, 168, w - 6, 196), STONE, line=3)
    c.d.ellipse(c._box((w - 20, 168, w - 10, 178)), fill=PINK)
    c.save("scratch_post")


def plant():
    w, h = units(1.0), units(1.8)
    c = Canvas(w, h)
    cx = w / 2
    leaves = [(-40, 40, -70), (40, 40, 70), (-26, 10, -30), (26, 10, 30), (0, 0, 0)]
    for dx, lift, angle in leaves:
        lx, ly = cx + dx, 60 + lift
        pts = []
        for k in range(24):
            t = k / 23 * math.pi * 2
            x = math.cos(t) * 22
            y = math.sin(t) * 46
            a = math.radians(angle)
            pts.append((lx + x * math.cos(a) - y * math.sin(a), ly + x * math.sin(a) + y * math.cos(a)))
        c.poly(pts, GREEN if dx else GREEN_DARK, line=4)
        c.line([(cx, 150), (lx, ly + 20)], GREEN_DARK, 5)
    c.poly([(cx - 46, 140), (cx + 46, 140), (cx + 36, h - 10), (cx - 36, h - 10)], TERRACOTTA)
    c.rect((cx - 52, 132, cx + 52, 156), 6, (230, 150, 116, 255), line=4)
    c.save("plant")


def prop_shadow():
    w, h = 256, 128
    img = Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))
    # Flat and hard-edged in the outline brown, like the shadow under each cat.
    ImageDraw.Draw(img).ellipse([24 * SS, 28 * SS, (w - 24) * SS, (h - 28) * SS], fill=BROWN[:3] + (72,))
    img = img.filter(ImageFilter.GaussianBlur(SS))
    img.resize((w, h), Image.LANCZOS).save(os.path.join(OUT, "prop_shadow.png"))


# ------------------------------------------------------------------ airport

def grass():
    """A seamless grass tile with soft flecks and the odd flower."""
    s = 256
    c = Canvas(s, s)
    c.d.rectangle(c._box((0, 0, s, s)), fill=(160, 212, 128, 255))
    rng = random.Random(3)
    for _ in range(70):
        x, y = rng.randint(0, s), rng.randint(0, s)
        colour = (144, 198, 114, 255) if rng.random() < 0.7 else (178, 224, 146, 255)
        for ox in (-s, 0, s):
            for oy in (-s, 0, s):
                c.d.line(c._box((x + ox, y + oy, x + ox + 3, y + oy - 9)), fill=colour, width=3 * SS)
    for _ in range(4):
        x, y = rng.randint(10, s - 10), rng.randint(10, s - 10)
        c.d.ellipse(c._box((x - 4, y - 4, x + 4, y + 4)), fill=WHITE)
        c.d.ellipse(c._box((x - 1.5, y - 1.5, x + 1.5, y + 1.5)), fill=(255, 214, 120, 255))
    c.save("grass")


def runway_home():
    w, h = units(2.5), units(11)
    c = Canvas(w, h)
    c.rect((8, 8, w - 8, h - 8), 22, ASPHALT, puffy=False)
    for x in (22, w - 30):
        c.d.rectangle(c._box((x, 30, x + 8, h - 30)), fill=CREAM)
    for y in range(160, h - 200, 150):
        c.d.rounded_rectangle(c._box((w / 2 - 7, y, w / 2 + 7, y + 80)), radius=6 * SS, fill=CREAM)
    for i in range(-3, 4):
        x = w / 2 + i * 34
        c.d.rounded_rectangle(c._box((x - 10, h - 150, x + 10, h - 50)), radius=5 * SS, fill=CREAM)
    for y in range(80, h - 40, 150):
        for x in (4, w - 4):
            c.d.ellipse(c._box((x - 8, y - 8, x + 8, y + 8)), fill=(255, 226, 150, 255), outline=BROWN, width=3 * SS)
    c.save("runway_home")


def tower():
    w, h = units(1.5), units(4.3)
    c = Canvas(w, h)
    cx = w / 2
    base = h - 10
    c.rect((cx - 40, 150, cx + 40, base), 10, CREAM)
    for y in (230, 330, 430):
        c.d.rounded_rectangle(c._box((cx - 12, y, cx + 12, y + 30)), radius=6 * SS, fill=SKY)
    c.rect((cx - 70, 130, cx + 70, 160), 10, CREAM_DARK)
    c.rect((cx - 62, 64, cx + 62, 134), 12, SKY)
    for x in (cx - 30, cx, cx + 30):
        c.d.line(c._box((x, 68, x, 130)), fill=WHITE, width=4 * SS)
    c.poly([(cx - 80, 70), (cx + 80, 70), (cx + 50, 34), (cx - 50, 34)], CORAL)
    c.line([(cx, 34), (cx, 8)], BROWN, 4)
    c.oval((cx - 9, 0, cx + 9, 16), CORAL, line=3)
    c.save("tower")


def tree_round():
    w, h = units(1.4), units(1.9)
    c = Canvas(w, h)
    cx = w / 2
    c.rect((cx - 12, 150, cx + 12, h - 8), 6, WOOD_DARK, line=5)
    for x, y, r in ((cx - 36, 110, 44), (cx + 36, 110, 44), (cx, 72, 58)):
        c.oval((x - r, y - r, x + r, y + r), GREEN)
    c.save("tree_round")


def tree_pine():
    w, h = units(1.2), units(2.2)
    c = Canvas(w, h)
    cx = w / 2
    c.rect((cx - 10, 230, cx + 10, h - 8), 5, WOOD_DARK, line=5)
    for top, bottom, half in ((20, 120, 40), (70, 180, 54), (130, 244, 66)):
        c.poly([(cx, top), (cx + half, bottom), (cx - half, bottom)], GREEN_DARK if top != 20 else GREEN)
    c.save("tree_pine")


def hills():
    """Far backdrop: two layers of soft hills with a few round trees, no outline (distance)."""
    w, h = units(30), units(5)
    c = Canvas(w, h)
    far = (184, 222, 176, 255)
    near = (164, 210, 150, 255)
    tree = (140, 196, 132, 255)
    pts = [(0, h)]
    for x in range(0, w + 1, 16):
        y = h * 0.42 + math.sin(x / w * math.pi * 5.0 + 0.6) * 60 + math.sin(x / w * math.pi * 11) * 18
        pts.append((x, y))
    pts.append((w, h))
    c.d.polygon([(x * SS, y * SS) for x, y in pts], fill=far)
    rng = random.Random(11)
    for _ in range(26):
        x = rng.randint(0, w)
        y = h * 0.62 + math.sin(x / w * math.pi * 3.0) * 40
        r = rng.randint(22, 38)
        c.d.ellipse(c._box((x - r, y - r * 1.2, x + r, y + r * 0.6)), fill=tree)
    pts = [(0, h)]
    for x in range(0, w + 1, 16):
        y = h * 0.66 + math.sin(x / w * math.pi * 3.0 + 2.0) * 50
        pts.append((x, y))
    pts.append((w, h))
    c.d.polygon([(x * SS, y * SS) for x, y in pts], fill=near)
    c.save("hills")


def skyline():
    """Far backdrop: a pale blue city along the horizon, two layers, no outline (distance).
    It keeps the airport in the lounge's palette of sky blues and creams instead of hills."""
    w, h = units(30), units(4)
    c = Canvas(w, h)
    far = (196, 222, 240, 255)
    near = (172, 206, 232, 255)
    window = (214, 234, 248, 255)
    rng = random.Random(5)
    for colour, base, lo, hi in ((far, h, 120, 300), (near, h, 70, 210)):
        x = -20
        while x < w:
            bw = rng.randint(50, 120)
            bh = rng.randint(lo, hi)
            c.d.rounded_rectangle(c._box((x, base - bh, x + bw, base + 10)), radius=10 * SS, fill=colour)
            if colour == near:
                for wy in range(int(base - bh + 22), int(base - 20), 30):
                    for wx in range(int(x + 14), int(x + bw - 14), 24):
                        c.d.rounded_rectangle(c._box((wx, wy, wx + 10, wy + 14)), radius=3 * SS, fill=window)
            x += bw + rng.randint(-10, 30)
    c.save("skyline")

    # The sky behind it: the same gradient as behind the gameplay board and through the
    # lounge window, stretched over the whole backdrop in Unity.
    sky = Image.new("RGBA", (4, 256))
    for y in range(256):
        t = y / 255
        t = t * t * (3 - 2 * t)
        sky.paste(tuple(round(SKY_TOP[i] + (SKY_BOTTOM[i] - SKY_TOP[i]) * t) for i in range(3)) + (255,), (0, y, 4, y + 1))
    sky.save(os.path.join(OUT, "airport_sky.png"))


def bush():
    w, h = units(1.4), units(0.9)
    c = Canvas(w, h)
    puffs = ((46, 70, 40), (w - 46, 70, 40), (w / 2, 52, 48))
    base = (8, 70, w - 8, h - 8)
    # Outline pass for every puff first, so the fill below reads as one silhouette.
    for x, y, r in puffs:
        c.d.ellipse(c._box((x - r - LINE, y - r - LINE, x + r + LINE, y + r + LINE)), fill=BROWN)
    c.d.rounded_rectangle(c._box((base[0] - LINE, base[1], base[2] + LINE, base[3] + LINE)), radius=10 * SS, fill=BROWN)
    for x, y, r in puffs:
        c.d.ellipse(c._box((x - r, y - r, x + r, y + r)), fill=GREEN)
    c.d.rounded_rectangle(c._box(base), radius=8 * SS, fill=GREEN)
    silhouette = Image.new("L", c.img.size, 0)
    sd = ImageDraw.Draw(silhouette)
    for x, y, r in puffs:
        sd.ellipse(c._box((x - r, y - r, x + r, y + r)), fill=255)
    sd.rounded_rectangle(c._box(base), radius=8 * SS, fill=255)
    puff(c.img, silhouette, SS)
    for x, y in ((50, 60), (w / 2 + 10, 40), (w - 60, 70)):
        c.d.ellipse(c._box((x - 7, y - 7, x + 7, y + 7)), fill=(246, 184, 188, 255), outline=BROWN, width=2 * SS)
    c.save("bush")


def flowers():
    """A flat patch of little flowers lying in the grass."""
    w, h = units(1.6), units(1.0)
    c = Canvas(w, h)
    rng = random.Random(5)
    colours = [(255, 255, 255, 255), (246, 184, 188, 255), (255, 214, 150, 255), (196, 214, 246, 255)]
    for _ in range(14):
        x, y = rng.randint(16, w - 16), rng.randint(16, h - 16)
        col = rng.choice(colours)
        for a in range(5):
            ang = a / 5 * math.pi * 2
            px, py = x + math.cos(ang) * 7, y + math.sin(ang) * 7
            c.d.ellipse(c._box((px - 5, py - 5, px + 5, py + 5)), fill=col)
        c.d.ellipse(c._box((x - 3.5, y - 3.5, x + 3.5, y + 3.5)), fill=(240, 170, 90, 255))
    c.save("flowers")


def windsock():
    w, h = units(1.0), units(1.6)
    c = Canvas(w, h)
    c.line([(24, 20), (24, h - 8)], BROWN, 8)
    c.line([(24, 22), (24, h - 10)], (220, 224, 232, 255), 4)
    stripes = [CORAL, CREAM, CORAL, CREAM]
    x0, top, bottom = 30, 22, 62
    seg = (w - 40 - x0) / len(stripes)
    for i, col in enumerate(stripes):
        a = x0 + i * seg
        b = a + seg
        shrink = i * 4
        c.poly([(a, top + shrink), (b, top + shrink + 4), (b, bottom - shrink - 4), (a, bottom - shrink)], col, line=3)
    c.save("windsock")


# ------------------------------------------------------------------ departure lounge
#
# The lounge borrows the gameplay board's look on purpose: cream tiles underfoot like the
# board's squares, and the same sky and clouds through the window as behind the board.

FONT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Art", "Fonts", "Baloo2-ExtraBold.ttf")
GROUT = (214, 224, 236, 255)
TILE_CREAM = (255, 250, 241, 255)
TILE_ALT = (250, 244, 234, 255)
NAVY = (52, 64, 96, 255)
METAL = (176, 186, 204, 255)
SKY_TOP = (132, 198, 236)
SKY_BOTTOM = (208, 236, 250)


def font(size):
    from PIL import ImageFont
    return ImageFont.truetype(FONT, int(size * SS))


# ------------------------------------------------------------------ apron (airport tab)

APRON = (232, 230, 224, 255)
APRON_SEAM = (212, 208, 200, 255)
TAXI_YELLOW = (250, 204, 72, 255)
SAFETY_ORANGE = (246, 136, 56, 255)
GLASS = (150, 206, 236, 255)
GLASS_DARK = (118, 178, 220, 255)


def apron():
    """A seamless tile of the parking apron: big concrete slabs, pale like the board's squares."""
    s = 256
    c = Canvas(s, s)
    c.d.rectangle(c._box((0, 0, s, s)), fill=APRON)
    rng = random.Random(7)
    for _ in range(40):
        x, y = rng.randint(0, s), rng.randint(0, s)
        r = rng.choice((2, 3))
        for ox in (-s, 0, s):
            for oy in (-s, 0, s):
                c.d.ellipse(c._box((x + ox - r, y + oy - r, x + ox + r, y + oy + r)), fill=(222, 220, 213, 255))
    # Slab joints on the tile edges and through the middle, so the tiling reads as slabs.
    for v in (0, s / 2, s):
        c.d.line(c._box((v, 0, v, s)), fill=APRON_SEAM, width=4 * SS)
        c.d.line(c._box((0, v, s, v)), fill=APRON_SEAM, width=4 * SS)
    c.save("apron")


def apron_marks():
    """Paint on the apron (flat): the yellow lead-in line the plane rolled in along, the
    stop bar under its nose wheel and the stand number, inside a white safety box."""
    w, h = units(7), units(2.4)
    c = Canvas(w, h)
    cy = h / 2
    stop = units(1.4)
    c.line([(w + 20, cy), (stop, cy)], TAXI_YELLOW, 14)
    c.rect((stop - 8, cy - 60, stop + 8, cy + 60), 6, TAXI_YELLOW, line=0)
    c.d.rounded_rectangle(c._box((20, 16, w - 60, h - 16)), radius=18 * SS, outline=(255, 255, 255, 230), width=8 * SS)
    c.d.text((48 * SS, (h - 34) * SS), "A1", font=font(90), fill=TAXI_YELLOW, anchor="ls")
    c.save("apron_marks")


def airstairs():
    """The boarding stairs truck: a flight of steps rising to a little platform at the left,
    where it meets the plane's door."""
    w, h = units(1.8), units(1.7)
    c = Canvas(w, h)
    base = h - 40
    wheel = (92, 78, 88, 255)
    c.rect((20, base - 36, w - 14, base), 12, CORAL)
    for x in (52, w - 50):
        c.oval((x - 18, base - 14, x + 18, base + 22), wheel, line=4)
    # Side panel of the stairs, a sloping band, with the step treads notched on top.
    lo = (w - 40, base - 36)
    hi = (58, 52)
    steps = 6
    tread = []
    for i in range(steps + 1):
        t = i / steps
        x = lo[0] + (hi[0] - lo[0]) * t
        y = lo[1] + (hi[1] - lo[1]) * t
        tread.append((x, y))
        if i < steps:
            nx = lo[0] + (hi[0] - lo[0]) * (i + 1) / steps
            tread.append((nx + 4, y))
    panel = tread + [(hi[0], hi[1] + 40), (lo[0] - 40, lo[1])]
    c.poly(panel, CREAM)
    # Platform at the top and its rail.
    c.rect((20, hi[1] - 6, hi[0] + 30, hi[1] + 12), 5, CREAM, line=4)
    rail = [(26, hi[1] - 40), (hi[0] + 24, hi[1] - 40), (lo[0] - 6, lo[1] - 44)]
    c.line(rail, BROWN, 10)
    c.line(rail, METAL, 5)
    for x, y in ((26, hi[1] - 40), (hi[0] + 24, hi[1] - 40)):
        c.line([(x, y), (x, hi[1] - 4)], BROWN, 8)
        c.line([(x, y), (x, hi[1] - 4)], METAL, 4)
    c.save("airstairs")


def baggage_train():
    """A little tug pulling two carts of suitcases."""
    w, h = units(3.4), units(1.3)
    c = Canvas(w, h)
    base = h - 30
    wheel = (92, 78, 88, 255)
    # Carts first (they sit behind the tug, to the right).
    for i, x0 in enumerate((w - 300, w - 150)):
        c.rect((x0, base - 36, x0 + 136, base - 14), 6, METAL)
        for wx in (x0 + 26, x0 + 110):
            c.oval((wx - 13, base - 24, wx + 13, base + 2), wheel, line=4)
        bags = ((CORAL, 0, 54, 46), (MINT, 58, 44, 60), (TAXI_YELLOW, 20, 40, 32)) if i == 0 else \
            ((GLASS_DARK, 0, 60, 52), (PINK, 64, 40, 40), (CREAM, 30, 50, 30))
        for colour, dx, bw, bh in bags:
            top = base - 40 - bh - (36 if bh < 35 else 0)
            c.rect((x0 + 8 + dx, top, x0 + 8 + dx + bw, base - 40), 8, colour, line=4)
            c.rect((x0 + 8 + dx + bw / 2 - 8, top - 10, x0 + 8 + dx + bw / 2 + 8, top), 3, BROWN, line=0)
    for x in (w - 312, w - 162):
        c.line([(x, base - 26), (x + 16, base - 26)], BROWN, 6)
    # The tug.
    tx = 20
    c.rect((tx, base - 70, tx + 150, base - 12), 14, TAXI_YELLOW)
    c.rect((tx + 70, base - 118, tx + 140, base - 66), 10, GLASS, line=5)
    c.d.line(c._box((tx + 70, base - 92, tx + 140, base - 92)), fill=BROWN, width=4 * SS)
    for wx in (tx + 32, tx + 118):
        c.oval((wx - 20, base - 34, wx + 20, base + 6), wheel, line=4)
    c.oval((tx + 4, base - 56, tx + 22, base - 38), CREAM, line=3)
    c.save("baggage_train")


def cone():
    w, h = units(0.45), units(0.6)
    c = Canvas(w, h)
    base = h - 12
    c.rect((6, base - 14, w - 6, base), 4, SAFETY_ORANGE, line=4)
    c.poly([(w / 2 - 6, 8), (w / 2 + 6, 8), (w - 16, base - 14), (16, base - 14)], SAFETY_ORANGE, line=4)
    c.d.polygon([(x * SS, y * SS) for x, y in ((w / 2 - 12, 30), (w / 2 + 12, 30), (w / 2 + 16, 44), (w / 2 - 16, 44))],
                fill=WHITE)
    c.save("cone")


def queue_rope():
    """Queue posts with rope swags, the line for boarding."""
    w, h = units(2.6), units(0.95)
    c = Canvas(w, h)
    base = h - 10
    posts = (24, w / 2, w - 24)
    for a, b in zip(posts, posts[1:]):
        pts = [(a + (b - a) * t / 20, 40 + 26 * math.sin(math.pi * t / 20)) for t in range(21)]
        c.line(pts, BROWN, 12)
        c.line(pts, CORAL, 6)
    for x in posts:
        c.rect((x - 7, 32, x + 7, base - 10), 4, METAL, line=4)
        c.oval((x - 24, base - 16, x + 24, base), METAL, line=4)
        c.oval((x - 11, 22, x + 11, 44), METAL, line=4)
    c.save("queue_rope")


def gate_podium(suffix=""):
    """The boarding gate desk: a podium with the gate sign and a little screen."""
    w, h = units(1.3), units(1.9)
    c = Canvas(w, h)
    base = h - 10
    c.line([(w / 2, 70), (w / 2, 120)], BROWN, 12)
    c.line([(w / 2, 70), (w / 2, 120)], METAL, 6)
    c.rect((14, 8, w - 14, 76), 14, CORAL)
    c.d.text(((w / 2) * SS, 42 * SS), "A1", font=font(46), fill=CREAM, anchor="mm")
    c.rect((20, 120, w - 20, base), 14, CREAM)
    c.rect((34, 136, w - 34, 186), 8, NAVY, line=4)
    c.d.text(((w / 2) * SS, 161 * SS), SIGNS[suffix]["boarding"], font=font(17 if suffix == "" else 15), fill=TAXI_YELLOW, anchor="mm")
    c.rect((20, base - 40, w - 20, base), 10, CORAL, line=4)
    c.save("gate_podium" + suffix)


def hangar():
    """An arched hangar with its doors open and a plane's tail fin just inside."""
    w, h = units(3.4), units(2.3)
    c = Canvas(w, h)
    base = h - 10
    arch = (6, 20, w - 6, 2 * base - 20)
    c.d.pieslice(c._box((arch[0] - LINE, arch[1] - LINE, arch[2] + LINE, arch[3] + LINE)), 180, 360, fill=BROWN)
    roof = Image.new("L", c.img.size, 0)
    ImageDraw.Draw(roof).pieslice(c._box(arch), 180, 360, fill=255)
    c.d.pieslice(c._box(arch), 180, 360, fill=SAGE)
    ribs = Image.new("L", c.img.size, 0)
    rd = ImageDraw.Draw(ribs)
    for x in range(60, w - 40, 56):
        rd.line(c._box((x, 0, x, base)), fill=255, width=4 * SS)
    layer = Image.new("RGBA", c.img.size, (170, 198, 180, 0))
    layer.putalpha(ImageChops.multiply(ribs, roof))
    c.img.alpha_composite(layer)
    puff(c.img, roof, SS)
    door = (w / 2 - 110, 110, w / 2 + 110, base)
    c.rect(door, 20, (84, 90, 112, 255), line=5, puffy=False)
    fin = [(w / 2 - 30, base - 4), (w / 2 + 10, 150), (w / 2 + 52, 150), (w / 2 + 40, base - 4)]
    c.poly(fin, CORAL, line=4)
    c.d.line(c._box((w / 2 + 2, 176, w / 2 + 48, 176)), fill=CREAM, width=10 * SS)
    c.d.rectangle(c._box((0, base - 4, w, base)), fill=BROWN)
    c.save("hangar")


def terminal():
    """The airline's terminal: a low cream building with a wall of glass and the name on top."""
    w, h = units(7.2), units(3.1)
    c = Canvas(w, h)
    base = h - 12
    c.rect((20, 110, w - 20, base), 18, CREAM)
    # Glass curtain wall with mullions; the right end steps forward as the gate pier.
    c.rect((60, 150, w - 300, base - 26), 10, GLASS, line=5)
    for x in range(60 + 70, w - 300, 70):
        c.d.line(c._box((x, 152, x, base - 28)), fill=WHITE, width=5 * SS)
    c.d.line(c._box((62, 205, w - 302, 205)), fill=WHITE, width=5 * SS)
    c.rect((w - 270, 160, w - 60, base - 26), 12, GLASS_DARK, line=5)
    c.rect((w - 200, 200, w - 130, base - 26), 8, CREAM_DARK, line=4)
    # Roof: a soft coral wave over the hall.
    c.rect((6, 88, w - 6, 126), 18, CORAL)
    c.rect((40, 70, w - 40, 96), 12, CORAL_DARK, line=4)
    # The name on the roof.
    sx = w / 2 - 60
    c.rect((sx - 230, 6, sx + 230, 66), 16, NAVY)
    c.d.text((sx * SS, 37 * SS), "AIRLINE POP", font=font(44), fill=CREAM, anchor="mm")
    for x in (sx - 160, sx + 160):
        c.d.line(c._box((x, 66, x, 74)), fill=BROWN, width=6 * SS)
    c.save("terminal")


MINT_LIVERY = (104, 196, 178, 255)
MINT_LIVERY_DARK = (74, 160, 146, 255)


def plane_parked(name, livery, livery_dark, captain=True):
    """The plane parked at a stand, seen from the side with the nose to the left, standing
    on its wheels like the passengers stand on their feet. Chibi proportions to match
    them: a short, fat body, a big tail, stubby wings and a big cockpit window with the
    captain looking out."""
    w, h = units(4.4), units(2.9)
    c = Canvas(w, h)
    top, bottom = 104, 306
    base = h - 8
    wheel = (92, 78, 88, 255)

    # Behind the body: the tail fin with the airline's roundel, and the far tailplane.
    c.poly([(w - 176, top + 40), (w - 112, 10), (w - 50, 10), (w - 26, top + 70)], livery)
    c.d.ellipse(c._box((w - 118, 34, w - 70, 82)), fill=CREAM)
    c.d.ellipse(c._box((w - 106, 46, w - 82, 70)), fill=livery_dark)
    c.poly([(w - 140, top + 74), (w - 50, top + 54), (w - 30, top + 84), (w - 130, top + 96)], livery_dark)

    # Landing gear under the nose and the wing.
    for x in (112, 318, 352):
        c.line([(x, bottom - 20), (x, base - 22)], BROWN, 10)
        c.line([(x, bottom - 20), (x, base - 22)], METAL, 5)
        c.oval((x - 20, base - 40, x + 20, base), wheel, line=4)

    # The body: a fat capsule.
    c.rect((24, top, w - 24, bottom), (bottom - top) / 2, CREAM)
    c.d.rounded_rectangle(c._box((64, bottom - 64, w - 64, bottom - 46)), radius=9 * SS, fill=livery)

    for x in range(240, w - 150, 44):
        c.d.ellipse(c._box((x - 13, top + 52, x + 13, top + 80)), fill=GLASS_DARK, outline=BROWN, width=3 * SS)

    # Front door, open, where the stairs meet the plane.
    c.rect((170, top + 34, 216, bottom - 22), 12, (70, 74, 100, 255), line=5, puffy=False)

    # A big cockpit window with the captain looking out.
    win = (46, top + 24, 146, top + 100)
    c.rect(win, 26, GLASS, line=5, puffy=False)
    if captain:
        path = os.path.join(OUT, "..", "FlatCats", "passenger_bo_sit.png")
        if os.path.exists(path):
            cat = Image.open(path).convert("RGBA")
            l, t, r, b = cat.getbbox()
            bust = cat.crop((l, t, r, t + int((b - t) * 0.62)))
            bw = int(104 * SS)
            bust = bust.resize((bw, int(bust.height * bw / bust.width)), Image.LANCZOS)
            layer = Image.new("RGBA", c.img.size, (0, 0, 0, 0))
            layer.alpha_composite(bust, (int((win[0] - 2) * SS), int((win[1] - 4) * SS)))
            mask = Image.new("L", c.img.size, 0)
            ImageDraw.Draw(mask).rounded_rectangle(c._box(win), radius=26 * SS, fill=255)
            layer.putalpha(ImageChops.multiply(layer.getchannel("A"), mask))
            c.img.alpha_composite(layer)
    c.d.line(c._box((win[0] + 16, win[1] + 12, win[0] + 36, win[1] + 12)), fill=(255, 255, 255, 200), width=5 * SS)

    # The near wing, swept back and down towards us, with its engine.
    c.poly([(262, bottom - 54), (380, bottom - 54), (470, bottom + 26), (420, bottom + 30)], livery_dark)
    c.oval((300, bottom - 8, 390, bottom + 40), CREAM)
    c.oval((302, bottom, 330, bottom + 32), livery, line=4)
    c.save(name)


def light_pole():
    w, h = units(0.8), units(3.4)
    c = Canvas(w, h)
    cx = w / 2
    base = h - 10
    c.rect((cx - 7, 40, cx + 7, base - 8), 4, METAL, line=4)
    c.rect((cx - 24, base - 16, cx + 24, base), 6, METAL, line=4)
    c.rect((cx - 40, 10, cx + 40, 46), 10, CREAM)
    for x in (cx - 24, cx, cx + 24):
        c.d.ellipse(c._box((x - 8, 30, x + 8, 44)), fill=TAXI_YELLOW)
    c.save("light_pole")


def taxi_sign():
    """A taxiway sign: black board, yellow letters, on two little legs."""
    w, h = units(1.5), units(0.8)
    c = Canvas(w, h)
    base = h - 8
    for x in (30, w - 30):
        c.line([(x, 60), (x, base)], BROWN, 10)
        c.line([(x, base - 2), (x, 60)], METAL, 5)
    c.rect((8, 8, w - 8, 68), 10, (48, 46, 58, 255))
    c.rect((16, 16, w / 2 - 4, 60), 6, TAXI_YELLOW, line=0)
    c.d.text(((w / 4 + 6) * SS, 38 * SS), "A1", font=font(36), fill=(48, 46, 58, 255), anchor="mm")
    c.d.text(((w * 0.75 - 22) * SS, 38 * SS), "B", font=font(32), fill=TAXI_YELLOW, anchor="mm")
    ax = w * 0.75 + 4
    c.d.polygon([(ax * SS, 28 * SS), ((ax + 18) * SS, 38 * SS), (ax * SS, 48 * SS)], fill=TAXI_YELLOW)
    c.save("taxi_sign")


def planes():
    plane_parked("plane_parked", CORAL, CORAL_DARK)
    # Another airline at the next stand, and the same shape climbing away in the sky.
    plane_parked("plane_mint", MINT_LIVERY, MINT_LIVERY_DARK, captain=False)
    light_pole()
    taxi_sign()


def airport():
    apron()
    apron_marks()
    airstairs()
    baggage_train()
    cone()
    queue_rope()
    gate_podium()
    gate_podium("_vi")
    hangar()
    terminal()
    planes()


def lounge_floor():
    """Rounded cream tiles with pale blue grout, the board's squares laid as a floor."""
    w, h = units(10), units(12)
    c = Canvas(w, h)
    c.d.rectangle(c._box((0, 0, w, h)), fill=GROUT)
    step = PPU
    gap = 6
    for row in range(h // step + 1):
        for col in range(w // step + 1):
            x0, y0 = col * step + gap / 2, row * step + gap / 2
            colour = TILE_CREAM if (row + col) % 2 == 0 else TILE_ALT
            c.d.rounded_rectangle(c._box((x0, y0, x0 + step - gap, y0 + step - gap)), radius=16 * SS, fill=colour)
            # A faint lip along the bottom of each tile, like the board's squares.
            c.d.rounded_rectangle(c._box((x0 + 4, y0 + step - gap - 12, x0 + step - gap - 4, y0 + step - gap - 3)),
                                  radius=5 * SS, fill=(236, 228, 216, 255))
    # A soft mint runner down the middle, from the window towards the camera. Blended
    # over the tiles (drawing with alpha straight onto the canvas would replace them).
    runner = Image.new("RGBA", c.img.size, (0, 0, 0, 0))
    ImageDraw.Draw(runner).rounded_rectangle(c._box((w / 2 - step * 1.4, 0, w / 2 + step * 1.4, h)),
                                             radius=20 * SS, fill=(170, 220, 200, 70))
    c.img.alpha_composite(runner)
    c.save("lounge_floor")


# Words painted on the art, per language; each such piece is drawn once per language.
SIGNS = {
    "": {"departures": "DEPARTURES", "gate": "GATE A1", "boarding": "BOARDING"},
    "_vi": {"departures": "KHỞI HÀNH", "gate": "CỔNG A1", "boarding": "LÊN MÁY BAY"},
}


def lounge_wall(suffix=""):
    """A floor-to-ceiling window onto the sky, with the departures board hung in front."""
    w, h = units(10), units(4)
    c = Canvas(w, h)
    base = h - units(0.55)

    # Sky through the glass: the same gradient as behind the gameplay board.
    for y in range(0, int(base)):
        t = y / base
        t = t * t * (3 - 2 * t)
        col = tuple(round(SKY_TOP[i] + (SKY_BOTTOM[i] - SKY_TOP[i]) * t) for i in range(3)) + (255,)
        c.d.line(c._box((0, y, w, y)), fill=col, width=SS)

    for cx, cy, r in ((150, 150, 34), (196, 132, 46), (248, 150, 34), (880, 96, 30), (924, 80, 40), (972, 96, 30)):
        c.d.ellipse(c._box((cx - r, cy - r, cx + r, cy + r)), fill=WHITE)
    c.d.rectangle(c._box((116, 150, 282, 184)), fill=WHITE)
    c.d.rectangle(c._box((850, 96, 1002, 124)), fill=WHITE)

    # Outside: grass, the runway with its centre dashes, and the tower in the distance.
    ground = base - 70
    c.d.rectangle(c._box((0, ground, w, base)), fill=(168, 214, 142, 255))
    c.d.rectangle(c._box((0, ground + 22, w, ground + 50)), fill=ASPHALT)
    for x in range(20, w, 90):
        c.d.rectangle(c._box((x, ground + 34, x + 44, ground + 38)), fill=CREAM)
    tower = Image.open(os.path.join(OUT, "tower.png")).convert("RGBA")
    tower = tower.resize((tower.width * 2 // 5, tower.height * 2 // 5), Image.LANCZOS)
    tower = tower.resize((tower.width * SS, tower.height * SS))
    c.img.alpha_composite(tower, (int(90 * SS), int((ground + 8) * SS - tower.height)))

    # Our plane, parked at the gate outside and waiting for these passengers.
    side_plane(c, 150, ground + 42, 470)

    # Window frame: mullions every two units, a sill and a wall band below.
    frame = (250, 247, 242, 255)
    for x in range(0, w + 1, units(2)):
        c.rect((x - 10, 0, x + 10, base), 4, frame, line=4, puffy=False)
    c.rect((-10, 0, w + 10, 16), 0, frame, line=4, puffy=False)
    c.d.rectangle(c._box((0, base, w, h)), fill=CREAM)
    c.d.rectangle(c._box((0, base + 14, w, base + 26)), fill=(206, 234, 222, 255))
    c.d.line(c._box((0, base, w, base)), fill=BROWN, width=4 * SS)

    # Departures board, hung right of centre.
    bx0, by0, bx1, by1 = units(5.0), 36, units(8.6), 36 + units(1.3)
    c.rect((bx0, by0, bx1, by1), 14, NAVY, puffy=False)
    c.d.rounded_rectangle(c._box((bx0, by0, bx1, by0 + 40)), radius=14 * SS, fill=CORAL)
    c.d.rectangle(c._box((bx0, by0 + 20, bx1, by0 + 40)), fill=CORAL)
    c.d.text(((bx0 + 20) * SS, (by0 + 2) * SS), SIGNS[suffix]["departures"], font=font(30), fill=CREAM)
    # The flight rows are live text laid over this panel in Unity (DeparturesBoardView);
    # faint row stripes here keep the panel reading as a board behind them.
    for i in range(3):
        y = by0 + 52 + i * 34
        c.d.rounded_rectangle(c._box((bx0 + 12, y, bx1 - 12, y + 28)), radius=6 * SS, fill=(62, 76, 112, 255))
    for x in (bx0 + 60, bx1 - 60):
        c.d.line(c._box((x, 0, x, by0)), fill=METAL, width=5 * SS)

    # Gate sign on the left mullion.
    gx, gy = units(2.4), 70
    c.rect((gx - 70, gy, gx + 70, gy + 64), 14, CORAL)
    c.d.text(((gx - 54) * SS, (gy + 4) * SS), SIGNS[suffix]["gate"], font=font(28), fill=CREAM)
    c.save("lounge_wall" + suffix)


def side_plane(c, x0, bottom, length):
    """The airline's plane seen from the side, nose left, standing on its wheels at bottom."""
    body_h = length * 0.24
    top = bottom - body_h - 26
    x1 = x0 + length
    c.poly([(x1 - 120, top + 10), (x1 - 70, top - 70), (x1 - 30, top - 70), (x1 - 10, top + 20)], CORAL)
    c.d.line(c._box((x1 - 76, top - 40, x1 - 30, top - 40)), fill=CREAM, width=10 * SS)
    for wx in (x0 + 90, x1 - 150):
        c.line([(wx, top + body_h - 4), (wx, bottom - 12)], BROWN, 8)
        c.oval((wx - 14, bottom - 26, wx + 14, bottom), (92, 78, 88, 255), line=4)
    c.rect((x0, top, x1, top + body_h), body_h / 2, CREAM)
    c.d.rounded_rectangle(c._box((x0 + 30, top + body_h * 0.62, x1 - 20, top + body_h * 0.62 + 12)),
                          radius=6 * SS, fill=CORAL)
    for i in range(7):
        wx = x0 + 120 + i * 38
        c.d.ellipse(c._box((wx - 9, top + 22, wx + 9, top + 40)), fill=GLASS_DARK, outline=BROWN, width=3 * SS)
    c.rect((x0 + 64, top + 16, x0 + 92, top + body_h - 10), 8, CREAM_DARK, line=4)
    c.poly([(x0 + 22, top + 22), (x0 + 58, top + 14), (x0 + 58, top + 40), (x0 + 16, top + 40)], GLASS, line=4)
    # The near wing, swept back and down from the belly.
    c.poly([(x0 + 160, top + body_h * 0.62), (x0 + 250, top + body_h * 0.62), (x0 + 330, top + body_h + 22),
            (x0 + 290, top + body_h + 22)], CORAL_DARK)


def lounge_seats():
    """A row of three joined airport seats, front-on."""
    w, h = units(3.3), units(1.5)
    c = Canvas(w, h)
    beam_y = h - 58
    for x in (40, w - 56):
        c.rect((x, beam_y, x + 16, h - 10), 5, METAL, line=4)
    c.rect((14, beam_y - 10, w - 14, beam_y + 12), 8, METAL, line=4)
    seat_w = (w - 60) / 3
    for i in range(3):
        x0 = 30 + i * seat_w
        c.rect((x0 + 8, 20, x0 + seat_w - 8, beam_y - 46), 26, CORAL)
        c.rect((x0 + 4, beam_y - 58, x0 + seat_w - 4, beam_y - 14), 18, (246, 150, 136, 255))
    for i in range(4):
        x = 22 + i * seat_w
        c.rect((x - 6, beam_y - 86, x + 14, beam_y - 16), 8, CREAM)
    c.save("lounge_seats")


def carrier():
    """A cat travel carrier, the lounge's napping spot."""
    w, h = units(1.4), units(1.05)
    c = Canvas(w, h)
    c.line([(52, 44), (70, 12), (w - 70, 12), (w - 52, 44)], BROWN, 12)
    c.line([(52, 44), (70, 12), (w - 70, 12), (w - 52, 44)], (206, 234, 222, 255), 6)
    c.rect((10, 40, w - 10, h - 12), 30, CREAM)
    c.rect((10, 40, w - 10, 70), 14, (206, 234, 222, 255), line=4)
    door = (w / 2 - 50, 84, w / 2 + 50, h - 28)
    c.rect(door, 18, (92, 78, 88, 255), line=5, puffy=False)
    for x in range(int(door[0]) + 18, int(door[2]), 18):
        c.d.line(c._box((x, door[1] + 6, x, door[3] - 6)), fill=METAL, width=4 * SS)
    c.oval((w - 44, 90, w - 24, 110), CORAL, line=3)
    c.save("carrier")


def trolley():
    """A luggage cart with two stickered suitcases."""
    w, h = units(1.3), units(1.8)
    c = Canvas(w, h)
    c.line([(24, 20), (24, h - 46), (w - 30, h - 46)], BROWN, 12)
    c.line([(24, 20), (24, h - 46), (w - 30, h - 46)], METAL, 6)
    for x in (44, w - 44):
        c.oval((x - 14, h - 40, x + 14, h - 12), (92, 78, 88, 255), line=4)
    c.rect((40, 100, w - 22, h - 56), 16, (196, 230, 214, 255))
    c.rect((56, 36, w - 40, 104), 14, CORAL)
    c.rect((w / 2 - 14, 20, w / 2 + 14, 40), 6, NAVY, line=3)
    for (x, y, col) in ((74, 140, CORAL), (w - 70, 180, CREAM), (90, 60, CREAM)):
        c.oval((x - 12, y - 12, x + 12, y + 12), col, line=3)
    c.d.line(c._box((40, 150, w - 22, 150)), fill=(160, 204, 186, 255), width=6 * SS)
    c.save("trolley")


def lounge_rug():
    """A round mint rug with the airline's little plane on it (flat)."""
    w, h = units(3.4), units(2.6)
    c = Canvas(w, h)
    c.oval((8, 8, w - 8, h - 8), MINT, puffy=False)
    c.d.ellipse(c._box((34, 34, w - 34, h - 34)), outline=CREAM, width=12 * SS)
    cx, cy = w / 2, h / 2
    plane = [(cx, cy - 70), (cx + 14, cy - 40), (cx + 14, cy - 12), (cx + 90, cy + 8), (cx + 90, cy + 24), (cx + 14, cy + 12),
             (cx + 12, cy + 44), (cx + 34, cy + 58), (cx + 34, cy + 70), (cx, cy + 62), (cx - 34, cy + 70), (cx - 34, cy + 58),
             (cx - 12, cy + 44), (cx - 14, cy + 12), (cx - 90, cy + 24), (cx - 90, cy + 8), (cx - 14, cy - 12), (cx - 14, cy - 40)]
    c.d.polygon([(x * SS, y * SS) for x, y in plane], fill=CREAM)
    c.save("lounge_rug")


def lounge():
    lounge_floor()
    lounge_wall()
    lounge_wall("_vi")
    lounge_seats()
    carrier()
    trolley()
    lounge_rug()


def main():
    hills()
    skyline()
    bush()
    flowers()
    windsock()
    room_floor()
    room_wall()
    rug()
    sofa()
    cat_bed()
    bowl()
    yarn()
    scratch_post()
    plant()
    prop_shadow()
    grass()
    runway_home()
    tower()
    airport()
    tree_round()
    tree_pine()
    lounge()
    print("home art written to", os.path.abspath(OUT))


if __name__ == "__main__":
    main()
