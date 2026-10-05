"""Generates the pieces of the flight route map: the sky, the gate signs that stand for
levels, the airport card each city lands on, the arrival stamp, clouds, and little cloud
islands carrying airport kit (control tower, baggage train, hangar, terminal) built from
the Home airport's own props, so the map speaks the same airport language as the rest of
the game.

There is no land anywhere on the map: the airline flies above the clouds, so nothing on it
can be read as a border or a territory.

    python Tools/flat_art/generate_route_map.py
"""
import math
import os

from PIL import Image

from generate_home import BROWN, CORAL, CREAM, SS, WHITE, Canvas, font

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Art", "RouteMap")
HOME_ART = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Art", "FlatHome")

SKY_TOP = (128, 196, 238)
SKY_BOTTOM = (214, 240, 252)
CLOUD_SHADE = (222, 236, 248, 255)
GREY = (204, 208, 218, 255)
GOLD = (246, 196, 72, 255)
SKY_BLUE = (112, 186, 230, 255)
MINT = (140, 210, 176, 255)
PINK = (246, 168, 186, 255)
LAVENDER = (176, 150, 222, 255)
RED = (220, 70, 70, 255)
NAVY = (60, 90, 170, 255)
NAVY_SIGN = (52, 64, 96, 255)  # the airport's sign boards, as on the Home terminal


def save(c, name):
    c.img.resize((c.w, c.h), Image.LANCZOS).save(os.path.join(OUT, name + ".png"))


def sky():
    """A tall gradient strip, stretched behind the whole map."""
    w, h = 8, 512
    img = Image.new("RGBA", (w, h))
    for y in range(h):
        t = y / (h - 1)
        col = tuple(round(SKY_TOP[i] + (SKY_BOTTOM[i] - SKY_TOP[i]) * t) for i in range(3)) + (255,)
        for x in range(w):
            img.putpixel((x, y), col)
    img.save(os.path.join(OUT, "map_sky.png"))


def gate_node(name, board, chip, label, flown=False):
    """
    A level: an airport gate sign, the word GATE over a chip the game writes the number on.
    Flown gates carry a little paw check. Drawn at twice its size on screen.
    """
    k = 2
    w, h = 128 * k, 84 * k
    c = Canvas(w, h)
    c.rect((5 * k, 5 * k, w - 5 * k, h - 5 * k), 12 * k, board, line=4 * k)
    c.d.text((w / 2 * SS, 19 * k * SS), "GATE", font=font(15 * k), fill=label, anchor="mm")
    c.rect((14 * k, 31 * k, w - 14 * k, h - 13 * k), 7 * k, chip, line=0, puffy=False)
    if flown:
        x, y, r = w - 20 * k, 20 * k, 12 * k
        c.oval((x - r, y - r, x + r, y + r), CORAL, line=3 * k, puffy=False)
        c.d.line(c._box((x - 6 * k, y, x - 1 * k, y + 5 * k)), fill=WHITE, width=3 * k * SS)
        c.d.line(c._box((x - 1 * k, y + 5 * k, x + 7 * k, y - 5 * k)), fill=WHITE, width=3 * k * SS)
    save(c, name)


def hub_card():
    """The card a city lands on: a cream body under a navy header band; the game writes on it."""
    w, h = 460, 236
    c = Canvas(w, h)
    c.rect((6, 6, w - 6, h - 6), 26, CREAM, line=6)
    c.d.rounded_rectangle(c._box((6, 6, w - 6, 62)), radius=26 * SS, fill=NAVY_SIGN)
    c.d.rectangle(c._box((6, 40, w - 6, 62)), fill=NAVY_SIGN)
    c.d.line(c._box((6, 62, w - 6, 62)), fill=BROWN, width=4 * SS)
    c.d.rounded_rectangle(c._box((2, 2, w - 2, h - 2)), radius=30 * SS, outline=BROWN, width=6 * SS)
    save(c, "hub_card")


def arrival_stamp():
    """Inked on a city's card once the plane has landed there: a ring, a plane, a tick."""
    s = 150
    c = Canvas(s, s)
    ink = (214, 84, 76, 255)
    c.d.ellipse(c._box((8, 8, s - 8, s - 8)), outline=ink, width=7 * SS)
    c.d.ellipse(c._box((22, 22, s - 22, s - 22)), outline=ink, width=3 * SS)
    px, py = s / 2, s / 2 - 10
    plane = [(px + 30, py), (px + 20, py - 5), (px + 4, py - 5), (px - 12, py - 28), (px - 22, py - 28), (px - 12, py - 5),
             (px - 24, py - 5), (px - 32, py - 15), (px - 37, py - 15), (px - 34, py), (px - 37, py + 15), (px - 32, py + 15),
             (px - 24, py + 5), (px - 12, py + 5), (px - 22, py + 28), (px - 12, py + 28), (px + 4, py + 5), (px + 20, py + 5)]
    c.d.polygon([(x * SS, y * SS) for x, y in plane], fill=ink)
    c.d.line(c._box((s / 2 - 22, s / 2 + 30, s / 2 - 6, s / 2 + 44)), fill=ink, width=7 * SS)
    c.d.line(c._box((s / 2 - 6, s / 2 + 44, s / 2 + 24, s / 2 + 18)), fill=ink, width=7 * SS)
    save(c, "arrival_stamp")


def cloud_base(c, cx, base, width):
    """A flat-bottomed cloud for an island to stand on."""
    puffs = [(cx - width * 0.38, base - 20, 22), (cx - width * 0.18, base - 26, 30), (cx + width * 0.04, base - 28, 32),
             (cx + width * 0.24, base - 24, 28), (cx + width * 0.4, base - 18, 20), (cx, base - 16, 26)]
    body = (cx - width / 2, base - 30, cx + width / 2, base)
    c.d.ellipse(c._box((body[0], body[1] + 6, body[2], body[3] + 6)), fill=CLOUD_SHADE)
    for dx, dy, r in puffs:
        c.d.ellipse(c._box((dx - r, dy - r + 8, dx + r, dy + r + 8)), fill=CLOUD_SHADE)
    c.d.ellipse(c._box(body), fill=WHITE)
    for dx, dy, r in puffs:
        c.d.ellipse(c._box((dx - r, dy - r, dx + r, dy + r)), fill=WHITE)


def paste_prop(c, name, x, bottom, scale):
    """Stands one of the Home airport's props on the island, its feet at the given line."""
    prop = Image.open(os.path.join(HOME_ART, name + ".png")).convert("RGBA")
    size = (round(prop.width * scale * SS), round(prop.height * scale * SS))
    prop = prop.resize(size, Image.LANCZOS)
    c.img.alpha_composite(prop, (round(x * SS - size[0] / 2), round(bottom * SS - size[1])))


def island(name, w, h, props):
    c = Canvas(w, h)
    for prop, x, scale in props:
        # The cloud only wraps the props' feet: they stand on it, not in it.
        paste_prop(c, prop, x, h - 26, scale)
    cloud_base(c, w / 2, h - 6, w * 0.92)
    save(c, name)


def cloud(name, w, h, puffs):
    # Room above the tallest puff, so no cloud is cut flat along the top of its picture.
    pad = 16
    c = Canvas(w, h + pad)
    puffs = [(dx, dy + pad, r) for dx, dy, r in puffs]
    for dx, dy, r in puffs:
        c.d.ellipse(c._box((dx - r, dy - r + 6, dx + r, dy + r + 6)), fill=CLOUD_SHADE)
    for dx, dy, r in puffs:
        c.d.ellipse(c._box((dx - r, dy - r, dx + r, dy + r)), fill=WHITE)
    save(c, name)


def boarding_pass():
    """The chapter title's card: a cream boarding pass with a sky-blue stub and a little plane."""
    w, h = 500, 170
    c = Canvas(w, h)
    stub = 360
    c.rect((8, 8, w - 8, h - 8), 22, CREAM, line=6)
    c.d.rounded_rectangle(c._box((stub, 8, w - 8, h - 8)), radius=22 * SS, fill=SKY_BLUE)
    c.d.rectangle(c._box((stub, 8, stub + 24, h - 8)), fill=SKY_BLUE)
    c.d.rounded_rectangle(c._box((2, 2, w - 2, h - 2)), radius=26 * SS, outline=BROWN, width=6 * SS)
    for y in range(26, h - 20, 18):
        c.d.line(c._box((stub, y, stub, y + 9)), fill=BROWN, width=3 * SS)
    for y in (2, h - 2):
        c.d.ellipse(c._box((stub - 16, y - 16, stub + 16, y + 16)), fill=(0, 0, 0, 0))
        c.d.arc(c._box((stub - 16, y - 16, stub + 16, y + 16)), 0, 360, fill=BROWN, width=6 * SS)
    px, py = (stub + w - 8) / 2, h / 2
    plane = [(px + 34, py), (px + 22, py - 6), (px + 4, py - 6), (px - 14, py - 34), (px - 24, py - 34), (px - 12, py - 6),
             (px - 28, py - 6), (px - 36, py - 18), (px - 42, py - 18), (px - 38, py), (px - 42, py + 18), (px - 36, py + 18),
             (px - 28, py + 6), (px - 12, py + 6), (px - 24, py + 34), (px - 14, py + 34), (px + 4, py + 6), (px + 22, py + 6)]
    c.poly(plane, WHITE, line=4, puffy=False)
    save(c, "boarding_pass")


def main():
    os.makedirs(OUT, exist_ok=True)
    sky()
    gate_node("gate_flown", NAVY_SIGN, GOLD, GOLD, flown=True)
    gate_node("gate_current", CORAL, WHITE, CREAM)
    gate_node("gate_locked", (168, 174, 188, 255), (214, 218, 228, 255), (236, 238, 244, 255))
    hub_card()
    arrival_stamp()
    cloud("cloud_a", 260, 130, ((60, 80, 40), (110, 62, 52), (168, 58, 58), (214, 82, 40), (130, 92, 44)))
    cloud("cloud_b", 200, 110, ((50, 68, 34), (98, 50, 44), (146, 66, 38), (100, 80, 34)))
    cloud("cloud_c", 320, 140, ((56, 90, 40), (112, 66, 54), (178, 56, 62), (240, 74, 50), (280, 96, 34), (160, 100, 44)))
    island("island_tower", 300, 310, (("tower", 120, 0.42), ("windsock", 210, 0.62)))
    island("island_cargo", 380, 190, (("baggage_train", 190, 0.7),))
    island("island_hangar", 360, 250, (("hangar", 170, 0.62), ("cone", 300, 0.7)))
    island("island_terminal", 440, 230, (("terminal", 220, 0.44),))
    boarding_pass()
    print("route map art written to", os.path.abspath(OUT))


if __name__ == "__main__":
    main()
