"""Art for the night flight, formation flight and VIP flight: their rule cards (256 px, like
the runway and wind cards), the light that guides a night flight, and the VIP seal on a stamp.

    python Tools/flat_art/night_formation.py
"""
import math
import os

from PIL import Image, ImageDraw

from generate_home import BROWN, Canvas
import generate_home

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Art", "Flat")

NIGHT = (58, 70, 128, 255)
NIGHT_DEEP = (44, 52, 100, 255)
MOON = (255, 222, 120, 255)
STAR = (255, 240, 180, 255)
SKY = (168, 214, 244, 255)
PLANE = (255, 255, 255, 255)
PLANE_TRIM = (240, 124, 108, 255)
GOLD = (255, 200, 72, 255)
GOLD_DEEP = (236, 160, 52, 255)
RUBY = (240, 104, 120, 255)
WHITE = (255, 255, 255, 255)


def star(c, x, y, r, fill, line=5):
    points = []
    for i in range(10):
        a = -math.pi / 2 + i * math.pi / 5
        rr = r if i % 2 == 0 else r * 0.48
        points.append((x + rr * math.cos(a), y + rr * math.sin(a)))
    c.poly(points, fill, line=line, puffy=False)


def plane(c, x, y, s):
    """A small top-down plane, nose up, centred on (x, y), s wide: swept wings and a tail."""
    k = s / 100.0
    c.poly([(x, y - 14 * k), (x + 50 * k, y + 14 * k), (x + 50 * k, y + 26 * k), (x, y + 12 * k),
            (x - 50 * k, y + 26 * k), (x - 50 * k, y + 14 * k)], PLANE, line=5, puffy=False)
    c.poly([(x, y + 34 * k), (x + 22 * k, y + 50 * k), (x + 22 * k, y + 58 * k), (x, y + 50 * k),
            (x - 22 * k, y + 58 * k), (x - 22 * k, y + 50 * k)], PLANE, line=5, puffy=False)
    c.rect((x - 11 * k, y - 50 * k, x + 11 * k, y + 56 * k), 11 * k, PLANE, line=5, puffy=False)
    c.oval((x - 6 * k, y - 38 * k, x + 6 * k, y - 24 * k), PLANE_TRIM, line=0, puffy=False)


def rule_night():
    """A night sky card: a crescent moon, stars and a plane flying under them."""
    c = Canvas(256, 256)
    c.rect((20, 20, 236, 236), 40, NIGHT, line=7)
    c.oval((48, 44, 132, 128), MOON, line=6, puffy=False)
    c.oval((72, 34, 150, 112), NIGHT, line=0, puffy=False)
    star(c, 180, 70, 18, STAR)
    star(c, 206, 128, 11, STAR, line=4)
    star(c, 150, 120, 9, STAR, line=4)
    plane(c, 132, 168, 76)
    c.save("rule_night")


def rule_formation():
    """Two planes side by side, mirror images, on a sky card with a dashed middle line."""
    c = Canvas(256, 256)
    c.rect((20, 20, 236, 236), 40, SKY, line=7)
    for y in range(44, 220, 30):
        c.rect((124, y, 132, y + 16), 4, WHITE, line=0, puffy=False)
    plane(c, 76, 128, 74)
    plane(c, 180, 128, 74)
    c.save("rule_formation")


def crown(c, x, y, w):
    k = w / 100.0
    c.poly([(x - 50 * k, y + 30 * k), (x - 50 * k, y - 22 * k), (x - 25 * k, y + 2 * k), (x, y - 34 * k), (x + 25 * k, y + 2 * k),
            (x + 50 * k, y - 22 * k), (x + 50 * k, y + 30 * k)], GOLD, line=6, puffy=False)
    c.rect((x - 50 * k, y + 22 * k, x + 50 * k, y + 40 * k), 6 * k, GOLD_DEEP, line=6, puffy=False)
    for dx in (-26, 0, 26):
        c.oval((x + (dx - 6) * k, y + 25 * k, x + (dx + 6) * k, y + 37 * k), RUBY, line=0, puffy=False)


def rule_vip():
    """A gold star badge with a crown on it: the VIP flight."""
    c = Canvas(256, 256)
    star(c, 128, 136, 112, GOLD, line=7)
    c.oval((70, 78, 186, 194), WHITE, line=6, puffy=False)
    crown(c, 128, 136, 84)
    c.save("rule_vip")


def vip_seal():
    """The round gold seal pressed on a city's stamp once its VIP flight is flown."""
    c = Canvas(128, 128)
    c.oval((10, 10, 118, 118), GOLD, line=6)
    c.oval((26, 26, 102, 102), GOLD_DEEP, line=0, puffy=False)
    crown(c, 64, 64, 56)
    c.save("vip_seal")


def radial(name, size, colour, core, power):
    """A soft round light: opaque in its core, fading out to the edge."""
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    px = img.load()
    half = size / 2.0
    for yy in range(size):
        for xx in range(size):
            d = math.hypot(xx + 0.5 - half, yy + 0.5 - half) / half
            if d >= 1.0:
                continue
            a = 1.0 if d <= core else (1.0 - (d - core) / (1.0 - core)) ** power
            px[xx, yy] = colour[:3] + (int(255 * a),)
    img.save(os.path.join(OUT, name + ".png"))


def night_guide():
    """The light that flies the route before night falls: a bright core in a warm halo."""
    size = 128
    radial("night_guide_halo", size, (255, 226, 130), 0.0, 1.6)
    halo = Image.open(os.path.join(OUT, "night_guide_halo.png"))
    core = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    ImageDraw.Draw(core).ellipse([40, 40, 88, 88], fill=(255, 250, 225, 255))
    halo.alpha_composite(core)
    halo.save(os.path.join(OUT, "night_guide.png"))
    os.remove(os.path.join(OUT, "night_guide_halo.png"))


if __name__ == "__main__":
    generate_home.OUT = OUT
    rule_night()
    rule_formation()
    rule_vip()
    vip_seal()
    night_guide()
    print("night, formation and VIP art written")
