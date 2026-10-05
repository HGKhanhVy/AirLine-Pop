"""The airline's planes sold in the shop: each its own shape, not only its own paint.

Every model is drawn twice: from above for the board (nose up, the cockpit bubble at the
same spot on every model, because the game lays Captain Bơ over it as a separate sprite),
and side-on with the nose to the left for the stand on Home, the captain looking out.

    python Tools/flat_art/plane_models.py
"""
import math
import os

from PIL import Image, ImageChops, ImageDraw

from generate_home import BROWN, CREAM, SS, WHITE, Canvas, units

ROOT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Art")
FLAT = os.path.join(ROOT, "Flat")
HOME = os.path.join(ROOT, "FlatHome")
CATS = os.path.join(ROOT, "FlatCats")

GLASS = (170, 222, 245, 255)
GLASS_DARK = (118, 178, 220, 255)
WHEEL = (92, 78, 88, 255)
METAL = (176, 186, 204, 255)
LINE = 6

# id, file suffix, main colour, dark colour. The ids are saved with the player's profile
# and once named paint jobs, so they stay: each now stands for a model of its own.
MODELS = [
    ("coral", "", (240, 124, 108, 255), (206, 92, 82, 255)),
    ("sunny", "_sunny", (246, 192, 72, 255), (214, 150, 40, 255)),
    ("sky", "_sky", (104, 170, 226, 255), (70, 132, 196, 255)),
    ("mint", "_mint", (104, 196, 178, 255), (74, 160, 146, 255)),
    ("lavender", "_lavender", (168, 140, 214, 255), (132, 104, 182, 255)),
    ("whale", "_whale", (120, 168, 220, 255), (84, 128, 186, 255)),
]

TOP = 512
C = TOP / 2
COCKPIT = (C - 74, 100, C + 74, 250)


def save(c, folder, name, size):
    os.makedirs(folder, exist_ok=True)
    c.img.resize(size, Image.LANCZOS).save(os.path.join(folder, name + ".png"))


def mirror(points):
    return [(2 * C - x, y) for x, y in points]


def cockpit(c):
    c.oval(COCKPIT, GLASS, line=LINE + 2, puffy=False)


def windows_top(c, ys, dx=54):
    for y in ys:
        for x in (C - dx, C + dx):
            c.oval((x - 8, y - 8, x + 8, y + 8), GLASS, line=3, puffy=False)


def paw(c, cx, cy, colour, s=1.0):
    c.d.ellipse(c._box((cx - 11 * s, cy - 2 * s, cx + 11 * s, cy + 16 * s)), fill=colour)
    for dx, dy in ((-15, -6), (-6, -15), (6, -15), (15, -6)):
        c.d.ellipse(c._box((cx + (dx - 5) * s, cy + (dy - 6) * s, cx + (dx + 5) * s, cy + (dy + 6) * s)), fill=colour)


# ------------------------------------------------------------------ from above

def top_classic(c, main, dark):
    for pts in ([(C - 60, 200), (C - 236, 270), (C - 236, 306), (C - 60, 292)],):
        c.poly(pts, dark, line=LINE)
        c.poly(mirror(pts), dark, line=LINE)
    for x in (C - 148, C + 148):
        c.oval((x - 28, 214, x + 28, 296), CREAM, line=LINE)
    tail = [(C - 36, 376), (C - 132, 420), (C - 132, 448), (C - 36, 438)]
    c.poly(tail, main, line=LINE)
    c.poly(mirror(tail), main, line=LINE)
    c.oval((C - 96, 40, C + 96, 432), CREAM, line=LINE + 2)
    c.rect((C - 14, 352, C + 14, 462), 14, main, line=LINE)
    windows_top(c, (262, 296, 330))
    paw(c, C, 300, main)
    cockpit(c)


def top_prop(c, main, dark):
    """A vintage propeller plane: long straight wings, a round nose and a spinning prop."""
    c.rect((C - 240, 196, C + 240, 260), 26, main, line=LINE)
    c.rect((C - 240, 214, C + 240, 230), 8, dark, line=0, puffy=False)
    c.rect((C - 110, 400, C + 110, 440), 18, main, line=LINE)
    c.oval((C - 84, 60, C + 84, 450), CREAM, line=LINE + 2)
    c.rect((C - 12, 380, C + 12, 470), 12, dark, line=LINE)
    c.rect((C - 120, 34, C + 120, 54), 10, METAL, line=4)
    c.oval((C - 22, 26, C + 22, 66), main, line=4)
    windows_top(c, (290, 330), dx=46)
    paw(c, C, 330, main, 0.9)
    cockpit(c)


def top_jumbo(c, main, dark):
    """A jumbo jet: a wide body with its hump, swept wings and four engines."""
    wing = [(C - 70, 190), (C - 246, 300), (C - 246, 330), (C - 70, 300)]
    c.poly(wing, dark, line=LINE)
    c.poly(mirror(wing), dark, line=LINE)
    for x, y in ((C - 128, 232), (C - 196, 272), (C + 128, 232), (C + 196, 272)):
        c.oval((x - 22, y - 30, x + 22, y + 30), CREAM, line=LINE)
    tail = [(C - 40, 382), (C - 150, 430), (C - 150, 456), (C - 40, 446)]
    c.poly(tail, main, line=LINE)
    c.poly(mirror(tail), main, line=LINE)
    c.oval((C - 110, 34, C + 110, 450), CREAM, line=LINE + 2)
    c.oval((C - 70, 70, C + 70, 300), main, line=LINE)
    c.rect((C - 16, 356, C + 16, 470), 16, main, line=LINE)
    windows_top(c, (330, 364, 398), dx=70)
    cockpit(c)


def top_seaplane(c, main, dark):
    """A seaplane: a high straight wing and two floats running under it."""
    for x in (C - 130, C + 130):
        c.rect((x - 28, 130, x + 28, 420), 28, main, line=LINE)
        c.rect((x - 14, 150, x + 14, 400), 14, dark, line=0, puffy=False)
    c.oval((C - 82, 60, C + 82, 440), CREAM, line=LINE + 2)
    c.rect((C - 250, 220, C + 250, 280), 22, main, line=LINE)
    c.rect((C - 100, 396, C + 100, 432), 16, dark, line=LINE)
    c.rect((C - 12, 372, C + 12, 466), 12, main, line=LINE)
    for x in (C - 190, C + 190):
        c.oval((x - 22, 196, x + 22, 252), CREAM, line=LINE)
    cockpit(c)


def top_supersonic(c, main, dark):
    """A supersonic jet: a long needle nose and one big delta wing."""
    delta = [(C, 150), (C + 210, 420), (C + 60, 430), (C, 400), (C - 60, 430), (C - 210, 420)]
    c.poly(delta, dark, line=LINE)
    c.poly([(C, 210), (C + 150, 400), (C, 390), (C - 150, 400)], main, line=0, puffy=False)
    c.oval((C - 70, 20, C + 70, 470), CREAM, line=LINE + 2)
    c.poly([(C - 10, 330), (C + 10, 330), (C + 14, 474), (C - 14, 474)], main, line=LINE)
    for x in (C - 40, C + 40):
        c.rect((x - 14, 420, x + 14, 470), 6, METAL, line=4)
    windows_top(c, (300, 336), dx=40)
    cockpit(c)


def top_whale(c, main, dark):
    """A flying whale: a round blue body, flippers for wings and flukes for a tail."""
    flipper = [(C - 80, 230), (C - 230, 300), (C - 210, 336), (C - 76, 300)]
    c.poly(flipper, dark, line=LINE)
    c.poly(mirror(flipper), dark, line=LINE)
    fluke = [(C, 420), (C - 120, 440), (C - 140, 486), (C - 40, 470), (C, 450)]
    c.poly(fluke, dark, line=LINE)
    c.poly(mirror(fluke), dark, line=LINE)
    c.oval((C - 116, 50, C + 116, 450), main, line=LINE + 2)
    c.oval((C - 74, 260, C + 74, 430), (226, 240, 250, 255), line=0, puffy=False)
    for x in (C - 60, C + 60):
        c.oval((x - 10, 300, x + 10, 320), WHITE, line=3, puffy=False)
    c.oval((C - 10, 330, C + 10, 350), (90, 160, 220, 255), line=3, puffy=False)
    cockpit(c)


TOP_DRAW = {"coral": top_classic, "sunny": top_prop, "sky": top_jumbo, "mint": top_seaplane,
            "lavender": top_supersonic, "whale": top_whale}


# ------------------------------------------------------------------ side-on

def captain_window(c, win, radius=26):
    c.rect(win, radius, GLASS, line=5, puffy=False)
    path = os.path.join(CATS, "passenger_bo_sit.png")
    if os.path.exists(path):
        cat = Image.open(path).convert("RGBA")
        l, t, r, b = cat.getbbox()
        bust = cat.crop((l, t, r, t + int((b - t) * 0.62)))
        bw = int((win[2] - win[0] + 4) * SS)
        bust = bust.resize((bw, int(bust.height * bw / bust.width)), Image.LANCZOS)
        layer = Image.new("RGBA", c.img.size, (0, 0, 0, 0))
        layer.alpha_composite(bust, (int((win[0] - 2) * SS), int((win[1] - 4) * SS)))
        mask = Image.new("L", c.img.size, 0)
        ImageDraw.Draw(mask).rounded_rectangle(c._box(win), radius=radius * SS, fill=255)
        layer.putalpha(ImageChops.multiply(layer.getchannel("A"), mask))
        c.img.alpha_composite(layer)
    c.d.line(c._box((win[0] + 14, win[1] + 12, win[0] + 32, win[1] + 12)), fill=(255, 255, 255, 200), width=5 * SS)


def gear(c, xs, top_y, base):
    for x in xs:
        c.line([(x, top_y), (x, base - 22)], BROWN, 10)
        c.line([(x, top_y), (x, base - 22)], METAL, 5)
        c.oval((x - 20, base - 40, x + 20, base), WHEEL, line=4)


def portholes(c, xs, y):
    for x in xs:
        c.d.ellipse(c._box((x - 13, y - 14, x + 13, y + 14)), fill=GLASS_DARK, outline=BROWN, width=3 * SS)


def side_classic(c, main, dark, w, h):
    top, bottom, base = 104, 306, h - 8
    c.poly([(w - 176, top + 40), (w - 112, 10), (w - 50, 10), (w - 26, top + 70)], main)
    c.d.ellipse(c._box((w - 122, 28, w - 66, 84)), fill=CREAM)
    paw(c, w - 94, 58, dark)
    c.poly([(w - 140, top + 74), (w - 50, top + 54), (w - 30, top + 84), (w - 130, top + 96)], dark)
    gear(c, (112, 318, 352), bottom - 20, base)
    c.rect((24, top, w - 24, bottom), (bottom - top) / 2, CREAM)
    c.d.rounded_rectangle(c._box((64, bottom - 64, w - 64, bottom - 46)), radius=9 * SS, fill=main)
    portholes(c, range(240, w - 150, 44), top + 66)
    c.rect((170, top + 34, 216, bottom - 22), 12, (70, 74, 100, 255), line=5, puffy=False)
    captain_window(c, (46, top + 24, 146, top + 100))
    c.poly([(262, bottom - 54), (380, bottom - 54), (470, bottom + 26), (420, bottom + 30)], dark)
    c.oval((300, bottom - 8, 390, bottom + 40), CREAM)
    c.oval((302, bottom, 330, bottom + 32), main, line=4)


def side_prop(c, main, dark, w, h):
    """The vintage prop plane: round nose with a propeller, a tail wheel, a tall fin."""
    top, bottom, base = 130, 290, h - 8
    c.poly([(w - 150, top + 30), (w - 90, 24), (w - 46, 24), (w - 40, top + 60)], dark)
    paw(c, w - 76, 70, CREAM, 0.9)
    gear(c, (150,), bottom - 10, base)
    c.line([(w - 80, bottom - 20), (w - 70, base - 14)], BROWN, 8)
    c.oval((w - 86, base - 26, w - 58, base), WHEEL, line=4)
    c.rect((60, top, w - 40, bottom), (bottom - top) / 2, main)
    c.rect((60, top + 70, w - 60, top + 86), 8, CREAM, line=0, puffy=False)
    c.oval((30, top + 20, 110, bottom - 20), dark)
    c.rect((10, top - 40, 34, bottom + 40), 12, METAL, line=4)
    c.oval((14, top + 50, 46, bottom - 50), (90, 92, 110, 255), line=4)
    captain_window(c, (150, top - 30, 250, top + 52))
    c.rect((150, bottom - 60, 400, bottom - 30), 14, dark)
    portholes(c, (300, 344), top + 50)


def side_jumbo(c, main, dark, w, h):
    """The jumbo: a big body with the upper-deck hump, four engines under the wing."""
    top, bottom, base = 110, 312, h - 6
    c.poly([(w - 170, top + 30), (w - 106, 4), (w - 46, 4), (w - 22, top + 60)], main)
    c.d.ellipse(c._box((w - 118, 22, w - 66, 74)), fill=CREAM)
    paw(c, w - 92, 50, dark)
    gear(c, (110, 300, 340, 380), bottom - 20, base)
    c.rect((20, top, w - 20, bottom), (bottom - top) / 2, CREAM)
    c.rect((40, top - 50, 290, top + 60), 50, CREAM)
    c.d.rounded_rectangle(c._box((60, bottom - 66, w - 60, bottom - 48)), radius=9 * SS, fill=main)
    portholes(c, range(200, w - 140, 40), top + 70)
    portholes(c, (170, 210, 250), top - 6)
    captain_window(c, (44, top - 30, 140, top + 40))
    c.poly([(250, bottom - 56), (400, bottom - 56), (500, bottom + 26), (446, bottom + 30)], dark)
    for x in (300, 400):
        c.oval((x - 40, bottom - 4, x + 40, bottom + 38), CREAM)
        c.oval((x - 38, bottom + 4, x - 12, bottom + 30), main, line=4)


def side_seaplane(c, main, dark, w, h):
    """The seaplane: a high wing on struts and two floats instead of wheels."""
    top, bottom = 120, 270
    floats_y = 330
    c.poly([(w - 150, top + 30), (w - 96, 20), (w - 50, 20), (w - 34, top + 60)], dark)
    paw(c, w - 80, 66, CREAM, 0.9)
    for x in (150, 330):
        c.line([(x, bottom - 10), (x - 10, floats_y - 16)], BROWN, 9)
        c.line([(x, bottom - 10), (x - 10, floats_y - 16)], METAL, 4)
    c.rect((60, floats_y - 26, w - 120, floats_y + 18), 22, main)
    c.rect((60, floats_y - 4, w - 120, floats_y + 18), 12, dark, line=0, puffy=False)
    c.rect((40, top, w - 40, bottom), (bottom - top) / 2, CREAM)
    c.d.rounded_rectangle(c._box((70, bottom - 54, w - 70, bottom - 40)), radius=7 * SS, fill=main)
    c.rect((120, top - 40, 420, top - 14), 12, main)
    for x in (170, 370):
        c.line([(x, top - 14), (x + 20, top + 20)], BROWN, 8)
    portholes(c, range(250, w - 150, 44), top + 60)
    captain_window(c, (70, top + 14, 166, top + 88))


def side_supersonic(c, main, dark, w, h):
    """The supersonic jet: a needle nose drooping forward, a slim body, a tall swept fin."""
    top, bottom, base = 150, 260, h - 8
    c.poly([(w - 150, top + 10), (w - 70, 10), (w - 30, 10), (w - 30, top + 40)], main)
    paw(c, w - 60, 66, CREAM, 0.8)
    gear(c, (150, 320, 350), bottom - 10, base)
    c.poly([(8, top + 70), (110, top), (w - 20, top), (w - 20, bottom), (110, bottom)], CREAM)
    c.d.line(c._box((110, bottom - 30, w - 40, bottom - 30)), fill=main, width=12 * SS)
    c.poly([(200, bottom - 20), (440, bottom - 20), (480, bottom + 18), (240, bottom + 18)], dark)
    portholes(c, range(250, w - 120, 36), top + 40)
    captain_window(c, (120, top + 6, 200, top + 62), radius=20)


def side_whale(c, main, dark, w, h):
    """The flying whale: a round body, a big smile, a flipper and the tail flukes."""
    top, bottom, base = 90, 316, h - 8
    c.poly([(w - 120, 190), (w - 30, 110), (w - 10, 150), (w - 60, 210), (w - 10, 270), (w - 30, 300)], dark)
    gear(c, (150, 330), bottom - 30, base)
    c.oval((20, top, w - 80, bottom), main)
    c.d.chord(c._box((20, top, w - 80, bottom)), 28, 152, fill=(226, 240, 250, 255))
    c.d.arc(c._box((40, top + 100, 180, top + 170)), 20, 160, fill=BROWN, width=5 * SS)
    c.oval((60, top + 70, 80, top + 92), BROWN, line=0, puffy=False)
    c.line([(200, top + 6), (190, top - 40)], (150, 206, 236, 255), 10)
    c.line([(200, top + 6), (230, top - 36)], (150, 206, 236, 255), 10)
    c.poly([(260, bottom - 70), (380, bottom - 60), (360, bottom + 10), (290, bottom - 10)], dark)
    portholes(c, (280, 320, 360), top + 80)
    captain_window(c, (110, top + 20, 200, top + 90))


SIDE_DRAW = {"coral": side_classic, "sunny": side_prop, "sky": side_jumbo, "mint": side_seaplane,
             "lavender": side_supersonic, "whale": side_whale}


def main():
    for model_id, suffix, main_colour, dark in MODELS:
        # The airline's own coral plane keeps the art generate_flat_art.py and generate_home.py draw.
        if model_id == "coral":
            continue

        top = Canvas(TOP, TOP)
        TOP_DRAW[model_id](top, main_colour, dark)
        save(top, FLAT, "airplane" + suffix, (TOP, TOP))

        w, h = units(4.4), units(2.9)
        side = Canvas(w, h)
        SIDE_DRAW[model_id](side, main_colour, dark, w, h)
        save(side, HOME, "plane_parked" + suffix, (w, h))
    print("plane models written:", len(MODELS))


if __name__ == "__main__":
    main()
