"""The looks sold in the shop's Themes shelf: each repaints the sky behind the board and
the route map, the gate signs on the map, and tints the board's tiles.

The ids are the board skins' ids, saved with the player's profile; keep the tile tint and
start pad hue in sync with ThemeInstaller.

    python Tools/flat_art/themes.py
"""
import os

from PIL import Image, ImageDraw

from generate_home import BROWN, SS, Canvas, font
from generate_route_map import gate_node

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Art", "Themes")

GREY_BOARD = (168, 174, 188, 255)
GREY_CHIP = (214, 218, 228, 255)
GREY_LABEL = (236, 238, 244, 255)
GOLD = (246, 196, 72, 255)
WHITE = (255, 255, 255, 255)
CREAM = (255, 250, 241, 255)

# id: sky top, sky bottom, tile tint, flown gate board, flown chip, current gate board.
THEMES = {
    "default": ((132, 198, 236), (208, 236, 250), (255, 250, 241), (52, 64, 96, 255), GOLD, (240, 124, 108, 255)),
    "ocean": ((128, 210, 192), (222, 246, 236), (238, 255, 248), (44, 112, 104, 255), (250, 214, 120, 255), (64, 170, 150, 255)),
    "sunset": ((246, 156, 136), (255, 222, 180), (255, 242, 228), (122, 66, 92, 255), GOLD, (246, 150, 64, 255)),
    "starter": ((244, 182, 206), (255, 236, 242), (255, 240, 245), (150, 80, 120, 255), (255, 236, 160, 255), (236, 110, 150, 255)),
    "neon": ((34, 46, 96), (92, 94, 158), (222, 230, 255), (28, 34, 68, 255), GOLD, (246, 176, 64, 255)),
}


def gradient(name, top, bottom, w, h, stars=False):
    img = Image.new("RGBA", (w, h))
    for y in range(h):
        t = y / (h - 1)
        t = t * t * (3 - 2 * t)
        col = tuple(round(top[i] + (bottom[i] - top[i]) * t) for i in range(3)) + (255,)
        for x in range(w):
            img.putpixel((x, y), col)
    img.save(os.path.join(OUT, name + ".png"))


# id: runway tarmac, centre line, edge lamp, the kind of centre mark, cloud tint.
RUNWAYS = {
    "default": ((150, 160, 184, 255), (250, 204, 72, 255), (255, 238, 186, 255), "dash", (255, 255, 255, 255)),
    "ocean": ((92, 168, 158, 255), (255, 255, 255, 255), (220, 255, 240, 255), "dash", (240, 255, 250, 255)),
    "sunset": ((150, 104, 126, 255), (255, 176, 84, 255), (255, 214, 170, 255), "dash", (255, 228, 214, 255)),
    "starter": ((212, 142, 172, 255), (255, 255, 255, 255), (255, 214, 230, 255), "flower", (255, 238, 245, 255)),
    "neon": ((52, 60, 108, 255), (255, 214, 96, 255), (120, 230, 255, 255), "star", (150, 150, 204, 255)),
}


def runway_art(theme_id):
    """The theme's runway: its tarmac with edge lamps, and its centre marks (dashes,
    sakura flowers or little stars), tiled along the path like the default ones."""
    tarmac, mark, lamp, kind, _ = RUNWAYS[theme_id]
    k = 4
    w, h = 64, 64
    img = Image.new("RGBA", (w * k, h * k), tarmac)
    d = ImageDraw.Draw(img)
    for cy in (11, h - 11):
        if theme_id == "neon":
            d.ellipse([(w / 2 - 11) * k, (cy - 11) * k, (w / 2 + 11) * k, (cy + 11) * k], fill=lamp[:3] + (90,))
        d.ellipse([(w / 2 - 7) * k, (cy - 7) * k, (w / 2 + 7) * k, (cy + 7) * k], fill=BROWN)
        d.ellipse([(w / 2 - 4.5) * k, (cy - 4.5) * k, (w / 2 + 4.5) * k, (cy + 4.5) * k], fill=lamp)
    img.resize((w, h), Image.LANCZOS).save(os.path.join(OUT, "runway_lights_" + theme_id + ".png"))

    import math
    w, h = 128, 32
    img = Image.new("RGBA", (w * k, h * k), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    if kind == "dash":
        d.rounded_rectangle([8 * k, 6 * k, 72 * k, (h - 6) * k], radius=10 * k, fill=mark)
    elif kind == "flower":
        cx, cy = 40, h / 2
        for i in range(5):
            a = math.radians(i * 72 - 90)
            px, py = cx + 7 * math.cos(a), cy + 7 * math.sin(a)
            d.ellipse([(px - 6) * k, (py - 6) * k, (px + 6) * k, (py + 6) * k], fill=mark)
        d.ellipse([(cx - 4) * k, (cy - 4) * k, (cx + 4) * k, (cy + 4) * k], fill=(250, 204, 72, 255))
    else:
        cx, cy = 40, h / 2
        pts = []
        for i in range(10):
            r = 12 if i % 2 == 0 else 5
            a = math.radians(i * 36 - 90)
            pts.append(((cx + r * math.cos(a)) * k, (cy + r * math.sin(a)) * k))
        d.polygon(pts, fill=mark)
    img.resize((w, h), Image.LANCZOS).save(os.path.join(OUT, "runway_dash_" + theme_id + ".png"))


START_HUE = {"default": 140, "ocean": 165, "sunset": 20, "starter": 335, "neon": 220}
PLANE = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Art", "Flat", "airplane_full.png")


def hue_colour(hue, sat, val):
    import colorsys
    r, g, b = colorsys.hsv_to_rgb(hue / 360.0, sat, val)
    return (round(r * 255), round(g * 255), round(b * 255), 255)


def preview(theme_id, sky_top, sky_bottom, tile, flown_board, flown_chip, current_board):
    """
    The shop card's picture: a little board as it looks in this theme. Its sky with a cloud
    (stars at night), a row of tiles the runway runs across, the start pad in the theme's
    hue, the plane on the runway, and the map's gate signs beside it.
    """
    w, h = 480, 250
    c = Canvas(w, h)
    sky = Image.new("RGBA", c.img.size)
    sd = ImageDraw.Draw(sky)
    for y in range(h * SS):
        t = y / (h * SS - 1)
        sd.line([(0, y), (w * SS, y)], fill=tuple(round(sky_top[i] + (sky_bottom[i] - sky_top[i]) * t) for i in range(3)) + (255,))
    mask = Image.new("L", c.img.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle(c._box((0, 0, w, h)), radius=24 * SS, fill=255)
    c.img.paste(sky, (0, 0), mask)

    if theme_id == "neon":
        for i in range(22):
            x, y = 14 + (i * 83) % 452, 10 + (i * 47) % 60
            r = 1.5 + (i % 3) * 0.8
            c.d.ellipse(c._box((x - r, y - r, x + r, y + r)), fill=(255, 248, 220, 255))
        c.d.ellipse(c._box((430, 14, 458, 42)), fill=(255, 246, 220, 255))
    else:
        for cx, cy, sc in ((70, 30, 0.8), (420, 26, 0.7)):
            for dx, dy, r in ((-26, 6, 18), (0, -6, 24), (26, 6, 18)):
                c.d.ellipse(c._box((cx + (dx - r) * sc, cy + (dy - r) * sc, cx + (dx + r) * sc, cy + (dy + r) * sc)), fill=RUNWAYS[theme_id][4][:3] + (235,))

    # The board: two rows of four tiles, the runway in from the start pad and round the corner.
    size, gap, x0, y0 = 70, 10, 18, 62
    hue = START_HUE[theme_id]
    flown = hue_colour(hue, 0.05, 0.98)
    for row in range(2):
        for col in range(4):
            x, y = x0 + col * (size + gap), y0 + row * (size + gap)
            on_route = (row == 0) or (col == 3)
            c.rect((x, y, x + size, y + size), 9, (flown if on_route else tile + (255,)), line=4)
    pad = hue_colour(hue, 0.48, 0.8)
    c.oval((x0 + 8, y0 + 8, x0 + size - 8, y0 + size - 8), pad, line=4)
    lane, mark, lamp, kind, cloud = RUNWAYS[theme_id]
    cy = y0 + size / 2
    cx = x0 + 3 * (size + gap) + size / 2
    for box in ((x0 + size / 2 - 15, cy - 15, cx + 15, cy + 15), (cx - 15, cy - 15, cx + 15, y0 + size + gap + size / 2)):
        c.d.rounded_rectangle(c._box(box), radius=15 * SS, fill=BROWN)
        c.d.rounded_rectangle(c._box((box[0] + 4, box[1] + 4, box[2] - 4, box[3] - 4)), radius=11 * SS, fill=lane)
    for x in range(int(x0 + size), int(cx - 14), 26):
        if kind == "dash":
            c.d.rounded_rectangle(c._box((x, cy - 3.5, x + 14, cy + 3.5)), radius=3 * SS, fill=mark)
        else:
            c.d.ellipse(c._box((x + 1, cy - 6, x + 13, cy + 6)), fill=mark)
    if os.path.exists(PLANE):
        plane = Image.open(PLANE).convert("RGBA").resize((96 * SS, 96 * SS), Image.LANCZOS).rotate(-90, expand=True)
        c.img.alpha_composite(plane, (int((x0 + 2 * (size + gap) - 14) * SS), int((cy - 48) * SS)))

    # The map's gate signs in this theme.
    for i, (board, chip, label) in enumerate(((current_board, WHITE, CREAM), (flown_board, flown_chip, flown_chip))):
        gx, gy = 352, 58 + i * 88
        c.rect((gx, gy, gx + 110, gy + 72), 12, board, line=4)
        c.d.text(((gx + 55) * SS, (gy + 18) * SS), "GATE", font=font(16), fill=label, anchor="mm")
        c.rect((gx + 12, gy + 34, gx + 98, gy + 62), 6, chip, line=0, puffy=False)
        c.d.text(((gx + 55) * SS, (gy + 48) * SS), str(12 - i), font=font(20), fill=board, anchor="mm")
    c.img.resize((w, h), Image.LANCZOS).save(os.path.join(OUT, "theme_" + theme_id + ".png"))


def main():
    os.makedirs(OUT, exist_ok=True)
    import generate_route_map
    generate_route_map.OUT = OUT
    for theme_id, (top, bottom, tile, flown_board, flown_chip, current_board) in THEMES.items():
        gradient("sky_" + theme_id, top, bottom, 4, 256)
        gradient("map_sky_" + theme_id, top, bottom, 8, 512)
        gate_node("gate_flown_" + theme_id, flown_board, flown_chip, flown_chip, flown=True)
        gate_node("gate_current_" + theme_id, current_board, WHITE, CREAM)
        gate_node("gate_locked_" + theme_id, GREY_BOARD, GREY_CHIP, GREY_LABEL)
        runway_art(theme_id)
        preview(theme_id, top, bottom, tile, flown_board, flown_chip, current_board)
    print("themes written:", len(THEMES))


if __name__ == "__main__":
    main()
