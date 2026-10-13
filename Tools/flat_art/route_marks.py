"""Art for the newer flight conditions and flights: the turbulence cloud, the beacon and the
overpass laid over a board square (256 px, like the wind arrow), their rule cards, the storm
flight's card and banner icon, and the small icon for the daily flight.

    python Tools/flat_art/route_marks.py
"""
import math
import os

from generate_home import BROWN, Canvas
import generate_home
from night_formation import plane, star

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Art", "Flat")

WHITE = (255, 255, 255, 255)
CLOUD = (214, 222, 240, 255)
CLOUD_SHADE = (176, 188, 218, 255)
STORM = (92, 104, 150, 255)
STORM_DEEP = (70, 80, 124, 255)
BOLT = (255, 214, 92, 255)
BEACON = (255, 156, 84, 255)
BEACON_GLOW = (255, 224, 150, 255)
POLE = (150, 160, 186, 255)
ARROW = (240, 124, 108, 255)
DECK = (170, 182, 210, 255)
DECK_EDGE = (130, 144, 180, 255)
ROAD = (120, 132, 162, 255)
SKY = (168, 214, 244, 255)
MINT = (150, 222, 196, 255)
CORAL = (240, 124, 108, 255)
CALENDAR = (255, 250, 240, 255)
STORM_CLOUD = (196, 204, 226, 255)
CHEEK = (246, 170, 170, 255)


def cloud_shape(c, cx, cy, w, fill, line=6):
    """A plump cloud of four puffs on a flat base, w wide, centred on (cx, cy)."""
    k = w / 200.0
    puffs = [(-62, 14, 46), (-20, -18, 58), (34, -10, 52), (70, 18, 40)]
    for dx, dy, r in puffs:
        c.oval((cx + (dx - r) * k, cy + (dy - r) * k, cx + (dx + r) * k, cy + (dy + r) * k), fill, line=line, puffy=False)
    c.rect((cx - 100 * k, cy + 4 * k, cx + 104 * k, cy + 56 * k), 26 * k, fill, line=line, puffy=False)
    # The puffs' outlines are drawn one by one, so the inside is filled again to hide the seams.
    for dx, dy, r in puffs:
        rr = r - 6
        c.oval((cx + (dx - rr) * k, cy + (dy - rr) * k, cx + (dx + rr) * k, cy + (dy + rr) * k), fill, line=0, puffy=False)
    c.rect((cx - 94 * k, cy + 10 * k, cx + 98 * k, cy + 50 * k), 22 * k, fill, line=0, puffy=False)


def bolt(c, x, y, scale=1.0, line=4):
    pts = [(0, 0), (18, 0), (8, 22), (20, 22), (-4, 52), (2, 30), (-10, 30)]
    c.poly([(x + px * scale, y + py * scale) for px, py in pts], BOLT, line=line, puffy=False)


def grumpy_face(c, cx, cy, k=1.0):
    """A grumpy little storm face: frowning brows, dot eyes, a wavy mouth and pink cheeks."""
    for dx in (-26, 26):
        c.oval((cx + dx * k - 6 * k, cy - 6 * k, cx + dx * k + 6 * k, cy + 6 * k), BROWN, line=0, puffy=False)
    c.line([(cx - 36 * k, cy - 18 * k), (cx - 18 * k, cy - 12 * k)], BROWN, 5 * k)
    c.line([(cx + 36 * k, cy - 18 * k), (cx + 18 * k, cy - 12 * k)], BROWN, 5 * k)
    c.line([(cx - 10 * k, cy + 16 * k), (cx - 3 * k, cy + 11 * k), (cx + 3 * k, cy + 16 * k), (cx + 10 * k, cy + 11 * k)],
           BROWN, 4 * k)
    for dx in (-40, 40):
        c.oval((cx + dx * k - 9 * k, cy + 6 * k, cx + dx * k + 9 * k, cy + 16 * k), CHEEK, line=0, puffy=False)


def grumpy_cloud(c, cx, cy, width):
    """The turbulence cloud: grey-blue, grumpy, two bolts under it. No arrow, so it never reads as a direction."""
    k = width / 210.0
    cloud_shape(c, cx, cy, width, STORM_CLOUD)
    grumpy_face(c, cx, cy + 22 * k, k)
    bolt(c, cx - 54 * k, cy + 66 * k, k)
    bolt(c, cx + 48 * k, cy + 68 * k, k)


def mark_cloud():
    """Turbulence on a board square."""
    c = Canvas(256, 256)
    grumpy_cloud(c, 128, 110, 210)
    c.save("mark_cloud")


def corner_arrow(c, x, y, fill, width=20, head=28, reach=(232, 236)):
    """An arrow that comes up from the bottom to (x, y) and turns right there: the turn a beacon asks for."""
    end_x, bottom = reach
    path = [(x, bottom), (x, y), (end_x - head, y)]
    c.line(path, BROWN, width + 12)
    c.line(path, fill, width)
    c.poly([(end_x + 4, y), (end_x - head, y - head), (end_x - head, y + head)], fill, line=6, puffy=False)


def beacon_light(c, x, y, r):
    """The beacon itself: a soft glow, an orange lamp and a glint."""
    c.oval((x - r * 1.7, y - r * 1.7, x + r * 1.7, y + r * 1.7), BEACON_GLOW, line=0, puffy=False)
    c.oval((x - r, y - r, x + r, y + r), BEACON, line=6, puffy=False)
    c.oval((x - r * 0.55, y - r * 0.6, x - r * 0.05, y - r * 0.1), WHITE, line=0, puffy=False)


def mark_beacon():
    """Beacon: an arrow turning a corner, with the beacon lit right where it turns."""
    c = Canvas(256, 256)
    corner_arrow(c, 84, 120, ARROW)
    beacon_light(c, 84, 120, 36)
    c.save("mark_beacon")


def mark_bridge():
    """Overpass: a road running up the square, crossed by a raised deck with railings and a shadow."""
    c = Canvas(256, 256)
    c.rect((92, 18, 164, 238), 10, ROAD, line=6, puffy=False)
    for y in (30, 78, 178, 222):
        c.rect((122, y, 134, y + 22), 5, WHITE, line=0, puffy=False)
    c.rect((20, 126, 236, 150), 8, (0, 0, 0, 60), line=0, puffy=False)
    c.rect((20, 92, 236, 140), 12, DECK, line=6, puffy=False)
    c.rect((20, 84, 236, 96), 5, DECK_EDGE, line=5, puffy=False)
    c.rect((20, 136, 236, 148), 5, DECK_EDGE, line=5, puffy=False)
    for x in range(44, 236, 48):
        c.rect((x - 4, 72, x + 4, 92), 3, DECK_EDGE, line=4, puffy=False)
    c.rect((60, 112, 196, 120), 4, WHITE, line=0, puffy=False)
    c.save("mark_bridge")


def card(c, fill):
    c.rect((20, 20, 236, 236), 40, fill, line=7)


def rule_turbulence():
    c = Canvas(256, 256)
    card(c, SKY)
    grumpy_cloud(c, 128, 104, 176)
    c.save("rule_turbulence")


def rule_beacon():
    c = Canvas(256, 256)
    card(c, MINT)
    corner_arrow(c, 88, 118, ARROW, 18, 26, (214, 214))
    beacon_light(c, 88, 118, 32)
    c.save("rule_beacon")


def rule_bridge():
    c = Canvas(256, 256)
    card(c, SKY)
    c.rect((100, 32, 156, 224), 8, ROAD, line=6, puffy=False)
    c.rect((32, 100, 224, 146), 12, DECK, line=6, puffy=False)
    c.rect((32, 92, 224, 104), 5, DECK_EDGE, line=5, puffy=False)
    c.rect((32, 142, 224, 154), 5, DECK_EDGE, line=5, puffy=False)
    plane(c, 128, 70, 50)
    c.save("rule_bridge")


def rule_storm():
    """The storm flight: a dark cloud with a lightning bolt over a small plane, on a deep sky card."""
    c = Canvas(256, 256)
    card(c, STORM)
    cloud_shape(c, 128, 92, 180, CLOUD_SHADE)
    c.poly([(126, 118), (160, 118), (140, 156), (166, 156), (112, 222), (124, 172), (100, 172)], BOLT, line=6, puffy=False)
    for x, y in ((60, 170), (196, 150), (70, 206)):
        c.line([(x, y), (x - 8, y + 22)], WHITE, 6)
    c.save("rule_storm")




def icon_daily():
    """The daily flight: a calendar page with a small plane on it."""
    c = Canvas(128, 128)
    c.rect((14, 22, 114, 116), 16, CALENDAR, line=6)
    c.rect((14, 22, 114, 48), 12, CORAL, line=6, puffy=False)
    for x in (40, 88):
        c.rect((x - 5, 10, x + 5, 34), 5, WHITE, line=5, puffy=False)
    plane(c, 64, 84, 52)
    c.save("icon_daily")


def icon_storm():
    """The storm banner's badge: the storm cloud and bolt alone, on no card."""
    c = Canvas(128, 128)
    cloud_shape(c, 64, 48, 110, CLOUD_SHADE, line=5)
    c.poly([(62, 66), (82, 66), (70, 88), (86, 88), (54, 124), (62, 96), (48, 96)], BOLT, line=5, puffy=False)
    c.save("icon_storm")


if __name__ == "__main__":
    generate_home.OUT = OUT
    mark_cloud()
    mark_beacon()
    mark_bridge()
    rule_turbulence()
    rule_beacon()
    rule_bridge()
    rule_storm()
    icon_daily()
    icon_storm()
    print("route marks written")
