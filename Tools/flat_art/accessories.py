"""Accessories the lounge regulars unlock with loyalty, worn on their heads, and the gift box
a regular brings once a day. Each accessory is drawn with its base, the part resting on
the head, along the bottom edge: the sprite's pivot sits there.

    python Tools/flat_art/accessories.py
"""
import math
import os

import generate_home
from generate_home import BROWN, CREAM, CORAL, CORAL_DARK, Canvas

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Art", "Accessories")

PINK = (246, 150, 176, 255)
PINK_DARK = (222, 108, 142, 255)
NAVY = (60, 76, 122, 255)
NAVY_DARK = (42, 54, 92, 255)
GOLD = (250, 204, 72, 255)
GOLD_DARK = (222, 164, 44, 255)
LEAF = (126, 196, 132, 255)
LEAF_DARK = (92, 164, 104, 255)
WHITE = (255, 255, 255, 255)
SKY = (120, 196, 240, 255)
RED = (236, 96, 96, 255)


def bow():
    """A pink bow: two puffed loops and a round knot."""
    c = Canvas(160, 100)
    c.poly([(78, 52), (22, 20), (14, 70), (40, 86)], PINK)
    c.poly([(82, 52), (138, 20), (146, 70), (120, 86)], PINK)
    c.line([(30, 36), (40, 66)], PINK_DARK, 6)
    c.line([(130, 36), (120, 66)], PINK_DARK, 6)
    c.oval((62, 34, 98, 72), PINK_DARK)
    c.save("acc_bow")


def pilot_cap():
    """A navy pilot's cap with a gold wing badge and a stiff brim."""
    c = Canvas(180, 120)
    c.rect((40, 26, 140, 82), 26, NAVY)
    c.rect((24, 74, 156, 98), 12, NAVY_DARK)
    c.rect((40, 64, 140, 76), 4, WHITE, line=0, puffy=False)
    c.poly([(90, 36), (110, 46), (90, 56), (70, 46)], GOLD, line=4, puffy=False)
    c.line([(52, 46), (72, 46)], GOLD, 6)
    c.line([(108, 46), (128, 46)], GOLD, 6)
    c.save("acc_pilot_cap")


def flower_crown():
    """A green vine ring with small flowers round it, seen from the front."""
    c = Canvas(200, 90)
    c.rect((14, 54, 186, 74), 10, LEAF)
    for x, colour in ((34, PINK), (68, CREAM), (100, GOLD), (132, CREAM), (166, PINK)):
        for i in range(5):
            a = math.radians(i * 72 - 90)
            px, py = x + 11 * math.cos(a), 46 + 11 * math.sin(a)
            c.oval((px - 9, py - 9, px + 9, py + 9), colour, line=4, puffy=False)
        c.oval((x - 7, 39, x + 7, 53), GOLD_DARK if colour != GOLD else CORAL, line=0, puffy=False)
    for x in (50, 116, 150):
        c.poly([(x, 60), (x + 14, 50), (x + 24, 62), (x + 12, 70)], LEAF_DARK, line=3, puffy=False)
    c.save("acc_flower_crown")


def crown():
    """A little gold crown with three points and jewels."""
    c = Canvas(170, 120)
    c.poly([(26, 104), (26, 40), (56, 70), (85, 22), (114, 70), (144, 40), (144, 104)], GOLD)
    c.rect((26, 88, 144, 106), 6, GOLD_DARK)
    for x, colour in ((50, SKY), (85, RED), (120, SKY)):
        c.oval((x - 9, 88, x + 9, 106), colour, line=3, puffy=False)
    for x, y in ((26, 40), (85, 22), (144, 40)):
        c.oval((x - 9, y - 9, x + 9, y + 9), CREAM, line=4, puffy=False)
    c.save("acc_crown")


def gift_box():
    """The daily gift: a coral box tied with a gold ribbon and bow."""
    c = Canvas(120, 120)
    c.rect((18, 50, 102, 108), 10, CORAL)
    c.rect((12, 36, 108, 58), 8, CORAL_DARK)
    c.rect((52, 36, 68, 108), 2, GOLD, line=0, puffy=False)
    c.poly([(60, 38), (30, 16), (26, 40)], GOLD)
    c.poly([(60, 38), (90, 16), (94, 40)], GOLD)
    c.oval((50, 26, 70, 46), GOLD_DARK)
    c.save("gift_box")


def main():
    generate_home.OUT = OUT
    os.makedirs(OUT, exist_ok=True)
    bow()
    pilot_cap()
    flower_crown()
    crown()
    gift_box()
    trim()
    print("accessories written")


def trim():
    """Crops each drawing to what is drawn, so a bottom-centre pivot rests right on the head."""
    from PIL import Image
    for name in os.listdir(OUT):
        if not name.endswith(".png"):
            continue
        path = os.path.join(OUT, name)
        img = Image.open(path)
        box = img.getbbox()
        if box:
            img.crop((max(0, box[0] - 2), max(0, box[1] - 2), min(img.width, box[2] + 2), min(img.height, box[3] + 1))).save(path)


if __name__ == "__main__":
    main()
