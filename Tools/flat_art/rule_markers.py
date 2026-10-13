"""Marks for the level rules, laid over a board square (256 px, like the tiles):
the runway a flight must land on, and the gust of wind that pushes it one way.

    python Tools/flat_art/rule_markers.py
"""
import os

from generate_home import BROWN, Canvas
import generate_home

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Art", "Flat")

TARMAC = (160, 174, 206, 255)
WHITE = (255, 255, 255, 255)
LAMP = (255, 214, 92, 255)
CORAL = (240, 124, 108, 255)
GUST = (150, 214, 246, 255)
GUST_DARK = (120, 186, 226, 255)


def runway():
    """
    The runway as a square of its own, built on the board tile's silhouette so it lines up
    exactly with its neighbours: a tarmac face with threshold bars at both ends, lamps down
    both edges and a coral touchdown target in the middle, under the tile's own lip, glint
    and outline. Nothing on it points one way, so it reads as a place to land, not a direction.
    """
    from PIL import Image, ImageDraw
    tile_dir = OUT
    face = Image.open(os.path.join(tile_dir, "tile_face.png")).convert("RGBA")
    overlay = Image.open(os.path.join(tile_dir, "tile_overlay.png")).convert("RGBA")
    k = 4
    size = face.size[0]
    art = Image.new("RGBA", (size * k, size * k), (0, 0, 0, 0))
    d = ImageDraw.Draw(art)

    def box(x0, y0, x1, y1):
        return [x0 * k, y0 * k, x1 * k, y1 * k]

    d.rectangle(box(0, 0, size, size), fill=TARMAC)
    # Threshold bars at both ends, so the square reads as a runway without pointing anywhere.
    for x in (82, 108, 148, 174):
        for y0, y1 in ((22, 52), (204, 234)):
            d.rounded_rectangle(box(x - 7, y0, x + 7, y1), radius=4 * k, fill=WHITE)
    for y in (40, 92, 144, 196):
        for x in (30, 226):
            d.ellipse(box(x - 10, y - 10, x + 10, y + 10), fill=BROWN)
            d.ellipse(box(x - 6, y - 6, x + 6, y + 6), fill=LAMP)
    # The touchdown target in the middle: where the flight has to come down.
    for r, fill in ((62, BROWN), (56, WHITE), (44, CORAL), (30, WHITE), (16, CORAL)):
        d.ellipse(box(128 - r, 128 - r, 128 + r, 128 + r), fill=fill)
    art = art.resize((size, size), Image.LANCZOS)

    tile = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    tile.paste(art, (0, 0), face.split()[3])
    tile.alpha_composite(overlay)
    tile.save(os.path.join(OUT, "rule_runway.png"))


def wind():
    """A plump arrow of wind pointing up, with two gust streaks beside it."""
    c = Canvas(256, 256)
    for x in (62, 194):
        c.line([(x, 150), (x, 206)], BROWN, 16)
        c.line([(x, 150), (x, 206)], GUST_DARK, 8)
    c.poly([(128, 38), (196, 112), (156, 112), (156, 214), (100, 214), (100, 112), (60, 112)], GUST, line=7, puffy=False)
    c.poly([(128, 70), (166, 106), (140, 106), (140, 196), (116, 196), (116, 106), (90, 106)], WHITE, line=0, puffy=False)
    c.save("rule_wind")


if __name__ == "__main__":
    generate_home.OUT = OUT
    runway()
    wind()
    print("rule markers written")
