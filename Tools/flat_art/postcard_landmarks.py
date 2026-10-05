"""The landmark on each city's postcard: what a traveller would send home from there.

One function per city, drawn on a 480 x 320 card in the flat sticker language: brown
outline, flat fills, one hard shade. The picture area is inset 16 px; the city name sits
bottom left and the stamp top right, so each scene keeps its subject in the middle and
right and leaves the lower left calm. Foreign cards also carry what their country is
known for (see "national symbols"), so they read even to a player who has never heard
of the city.
"""
import math

from generate_home import BROWN, CORAL, CREAM, WHITE

W, H = 480, 320
GROUND_Y = 236
LINE = 4

STONE = (214, 200, 178, 255)
STONE_DARK = (180, 164, 140, 255)
SAND = (236, 204, 156, 255)
SAND_DARK = (214, 172, 128, 255)
RED = (226, 92, 76, 255)
GOLD = (246, 196, 72, 255)
GREEN = (120, 186, 120, 255)
GREEN_DARK = (84, 150, 96, 255)
PINE = (76, 136, 104, 255)
SLATE = (96, 108, 140, 255)
NAVY = (60, 70, 104, 255)
SEA = (104, 176, 214, 255)
SEA_NIGHT = (58, 84, 130, 255)
TEAL = (88, 168, 156, 255)
LAVENDER = (168, 140, 214, 255)
PINK = (240, 160, 172, 255)
METAL = (176, 186, 204, 255)
STEEL = (150, 156, 170, 255)


def R(c, box, fill, r=4, line=LINE):
    c.rect(box, r, fill, line=line)


def P(c, pts, fill, line=LINE):
    c.poly(pts, fill, line=line)


def O(c, box, fill, line=LINE):
    c.oval(box, fill, line=line)


def L(c, pts, fill, width):
    c.line(pts, fill, width)


def on_polyline(pts, x):
    """Height of a polyline at x, for hanging things on a string."""
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        if x0 <= x <= x1:
            return y0 + (y1 - y0) * (x - x0) / (x1 - x0)
    return pts[-1][1]


def is_night(mood):
    return mood[4]


def ground(c, mood, y=GROUND_Y, colour=None):
    far = mood[3] + (255,)
    pts = [(0, H)] + [(x, y - 26 + 12 * math.sin(x / 60.0)) for x in range(0, W + 1, 8)] + [(W, H)]
    c.d.polygon([(px * c_ss(c), py * c_ss(c)) for px, py in pts], fill=far)
    c.d.rectangle(c._box((0, y, W, H)), fill=colour or (mood[2] + (255,)))


def sea(c, mood, y=GROUND_Y):
    col = SEA_NIGHT if is_night(mood) else SEA
    c.d.rectangle(c._box((0, y, W, H)), fill=col)
    for i in range(6):
        x = 40 + i * 74
        yy = y + 18 + (i % 3) * 16
        c.d.line(c._box((x, yy, x + 28, yy)), fill=(255, 255, 255, 150), width=3 * c_ss(c))


def c_ss(c):
    return c.img.width // c.w


def windows(c, box, colour, step=(14, 18), size=(6, 8)):
    x0, y0, x1, y1 = box
    for wy in range(int(y0), int(y1 - size[1]), step[1]):
        for wx in range(int(x0), int(x1 - size[0]), step[0]):
            c.d.rectangle(c._box((wx, wy, wx + size[0], wy + size[1])), fill=colour)


def lit(mood):
    return (255, 236, 180, 255) if is_night(mood) else (210, 230, 246, 255)


def palm(c, x, base, h=90, lean=10):
    top = (x + lean, base - h)
    L(c, [(x, base), (x + lean * 0.4, base - h * 0.5), top], BROWN, 14)
    L(c, [(x, base), (x + lean * 0.4, base - h * 0.5), top], (186, 140, 100, 255), 8)
    for ang in (-160, -120, -60, -20, 20):
        rad = math.radians(ang)
        end = (top[0] + 46 * math.cos(rad), top[1] + 30 * math.sin(rad) + 14)
        mid = (top[0] + 26 * math.cos(rad), top[1] + 18 * math.sin(rad) - 4)
        L(c, [top, mid, end], BROWN, 14)
        L(c, [top, mid, end], GREEN, 8)


def pine(c, x, base, h=70):
    R(c, (x - 4, base - 16, x + 4, base), (150, 110, 80, 255), r=2, line=3)
    P(c, [(x, base - h), (x + h * 0.32, base - 12), (x - h * 0.32, base - 12)], PINE, line=3)


def roof(c, cx, y, w, h, colour, curl=10):
    """An East Asian roof: wide eaves that turn up at the tips."""
    P(c, [(cx - w / 2 - curl, y - curl * 0.6), (cx - w / 2 + 8, y), (cx + w / 2 - 8, y), (cx + w / 2 + curl, y - curl * 0.6),
          (cx + w / 2 - 14, y - h), (cx - w / 2 + 14, y - h)], colour)


# ================================================================== national symbols
# A foreign card shows what the whole country is known for, so a player who has never
# heard of the city still knows where the plane flew: cherry blossom for Japan, the
# taegeuk flag for Korea, the tricolour for France, pyramids for Egypt, the stars and
# stripes for the USA.

SAKURA = (250, 196, 210, 255)
SAKURA_DARK = (236, 150, 176, 255)
TORII = (226, 80, 64, 255)


def sakura_branch(c, flip=False):
    """A blossoming branch reaching into the top of the picture."""
    def X(x):
        return W - x if flip else x
    L(c, [(X(16), 108), (X(70), 86), (X(130), 70), (X(178), 56)], BROWN, 9)
    L(c, [(X(16), 108), (X(70), 86), (X(130), 70), (X(178), 56)], (150, 104, 84, 255), 5)
    for x, y in ((40, 92), (70, 80), (96, 90), (118, 62), (146, 72), (170, 48), (60, 104), (132, 52)):
        O(c, (X(x) - 11, y - 11, X(x) + 11, y + 11), SAKURA, line=3)
        c.d.ellipse(c._box((X(x) - 3, y - 3, X(x) + 3, y + 3)), fill=SAKURA_DARK)


def sakura_tree(c, x, base, size=1.0):
    R(c, (x - 6 * size, base - 50 * size, x + 6 * size, base), (150, 104, 84, 255), r=3, line=3)
    for dx, dy, r in ((-26, -64, 26), (24, -66, 26), (0, -86, 30)):
        O(c, (x + (dx - r) * size, base + (dy - r) * size, x + (dx + r) * size, base + (dy + r) * size), SAKURA, line=3)


def petals(c, count=10):
    for i in range(count):
        x = (i * 89) % 420 + 40
        y = (i * 47) % 150 + 60
        c.d.ellipse(c._box((x - 4, y - 3, x + 4, y + 3)), fill=SAKURA)


def fuji(c, cx, base, half, h, snow=WHITE):
    P(c, [(cx - half, base), (cx - 18, base - h), (cx + 18, base - h), (cx + half, base)], (120, 150, 196, 255))
    P(c, [(cx - 44, base - h + 46), (cx - 18, base - h), (cx + 18, base - h), (cx + 44, base - h + 46),
          (cx + 26, base - h + 38), (cx + 10, base - h + 50), (cx - 8, base - h + 38), (cx - 26, base - h + 50)], snow, line=3)


def torii(c, cx, base, w, h):
    """Posts, the tie beam (nuki), a centre strut, then the red and the wider dark top beam."""
    for x in (cx - w * 0.36, cx + w * 0.36):
        R(c, (x - 7, base - h + 6, x + 7, base), TORII, r=3, line=3)
    R(c, (cx - w * 0.44, base - h * 0.74, cx + w * 0.44, base - h * 0.66), TORII, r=3, line=3)
    R(c, (cx - 5, base - h + 6, cx + 5, base - h * 0.74), TORII, r=2, line=3)
    P(c, [(cx - w / 2, base - h - 2), (cx + w / 2, base - h - 2), (cx + w / 2 - 4, base - h + 8), (cx - w / 2 + 4, base - h + 8)], TORII, line=3)
    P(c, [(cx - w / 2 - 12, base - h - 16), (cx + w / 2 + 12, base - h - 16), (cx + w / 2 + 4, base - h - 4), (cx - w / 2 - 4, base - h - 4)], (60, 60, 80, 255), line=3)


def flag_pole(c, x, base, h, draw_flag):
    L(c, [(x, base), (x, base - h)], BROWN, 6)
    L(c, [(x, base), (x, base - h)], METAL, 3)
    draw_flag(c, x + 2, base - h)


def korea_flag(c, x, y):
    R(c, (x, y, x + 60, y + 40), WHITE, r=2, line=3)
    c.d.pieslice(c._box((x + 20, y + 10, x + 40, y + 30)), 180, 360, fill=(220, 70, 70, 255))
    c.d.pieslice(c._box((x + 20, y + 10, x + 40, y + 30)), 0, 180, fill=(60, 90, 170, 255))
    for bx, by in ((x + 8, y + 6), (x + 46, y + 6), (x + 8, y + 26), (x + 46, y + 26)):
        for k in range(3):
            c.d.line(c._box((bx, by + k * 3, bx + 7, by + k * 3)), fill=BROWN, width=2 * c_ss(c))


def france_flag(c, x, y):
    for i, col in enumerate(((60, 90, 170, 255), WHITE, (220, 70, 70, 255))):
        c.d.rectangle(c._box((x + i * 20, y, x + i * 20 + 20, y + 40)), fill=col)
    c.d.rectangle(c._box((x, y, x + 60, y + 40)), outline=BROWN, width=3 * c_ss(c))


def usa_flag(c, x, y):
    for k in range(7):
        col = (220, 70, 70, 255) if k % 2 == 0 else WHITE
        c.d.rectangle(c._box((x, y + k * 40 / 7, x + 64, y + (k + 1) * 40 / 7)), fill=col)
    c.d.rectangle(c._box((x, y, x + 28, y + 22)), fill=(60, 80, 150, 255))
    for i in range(3):
        for j in range(2):
            c.d.ellipse(c._box((x + 5 + i * 8, y + 5 + j * 9, x + 9 + i * 8, y + 9 + j * 9)), fill=WHITE)
    c.d.rectangle(c._box((x, y, x + 64, y + 40)), outline=BROWN, width=3 * c_ss(c))


def tricolour_bunting(c):
    """A string of blue, white and red pennants across the top of the picture."""
    L(c, [(16, 40), (200, 64), (380, 44)], BROWN, 3)
    cols = ((60, 90, 170, 255), WHITE, (220, 70, 70, 255))
    for i in range(9):
        x = 30 + i * 38
        y = 42 + 22 * math.sin(i / 8 * math.pi) - (i / 8) * 4
        P(c, [(x - 12, y), (x + 12, y + 2), (x, y + 24)], cols[i % 3], line=3)


def far_pyramids(c, y=GROUND_Y - 22):
    for x, w, h in ((120, 120, 70), (196, 90, 52), (60, 70, 40)):
        c.d.polygon([(px * c_ss(c), py * c_ss(c)) for px, py in ((x - w / 2, y), (x, y - h), (x + w / 2, y))], fill=(226, 198, 160, 255))


def camel(c, x, base, s=1.0):
    body = (196, 150, 104, 255)
    for lx in (x - 22, x - 10, x + 10, x + 22):
        R(c, (lx - 3 * s, base - 40 * s, lx + 3 * s, base), body, r=2, line=2)
    O(c, (x - 34 * s, base - 70 * s, x + 34 * s, base - 34 * s), body, line=3)
    O(c, (x - 16 * s, base - 88 * s, x + 12 * s, base - 60 * s), body, line=3)
    L(c, [(x + 30 * s, base - 52 * s), (x + 44 * s, base - 76 * s)], BROWN, 12 * s)
    L(c, [(x + 30 * s, base - 52 * s), (x + 44 * s, base - 76 * s)], body, 7 * s)
    O(c, (x + 36 * s, base - 90 * s, x + 60 * s, base - 72 * s), body, line=3)


# ================================================================== Việt Nam

def da_lat(c, mood):
    ground(c, mood)
    for x, h in ((60, 70), (96, 90), (420, 80), (452, 64)):
        pine(c, x, GROUND_Y, h)
    # Đà Lạt cathedral: pink walls, a tall spire with the rooster on top.
    cx = 270
    R(c, (cx - 70, 170, cx + 70, GROUND_Y), PINK)
    P(c, [(cx - 78, 172), (cx + 78, 172), (cx, 136)], CORAL)
    R(c, (cx - 18, 110, cx + 18, GROUND_Y), PINK)
    P(c, [(cx - 22, 112), (cx + 22, 112), (cx, 40)], CORAL)
    L(c, [(cx, 40), (cx, 26)], BROWN, 3)
    P(c, [(cx - 2, 26), (cx + 10, 18), (cx + 8, 28), (cx + 14, 30), (cx, 32)], GOLD, line=2)
    O(c, (cx - 9, 130, cx + 9, 148), CREAM, line=3)
    R(c, (cx - 10, 200, cx + 10, GROUND_Y), (150, 110, 90, 255), r=8)
    for x in (cx - 50, cx + 34):
        R(c, (x, 188, x + 16, 214), (190, 220, 240, 255), r=8, line=3)
    # Flower beds.
    for i, col in enumerate((CORAL, GOLD, PINK, LAVENDER)):
        c.d.ellipse(c._box((340 + i * 30, 244, 362 + i * 30, 258)), fill=col)


def hoi_an(c, mood):
    sea(c, mood, 230)
    # The Japanese covered bridge: an arched deck under a curved tiled roof.
    cx = 290
    P(c, [(cx - 110, 230), (cx - 80, 196), (cx + 80, 196), (cx + 110, 230), (cx + 84, 230), (cx + 60, 212), (cx - 60, 212), (cx - 84, 230)], STONE)
    R(c, (cx - 80, 160, cx + 80, 200), (196, 140, 96, 255), r=4)
    for x in range(int(cx - 70), int(cx + 70), 28):
        R(c, (x, 168, x + 14, 196), (120, 80, 60, 255), r=2, line=2)
    roof(c, cx, 160, 190, 34, (150, 96, 80, 255), curl=14)
    # Lanterns along a string.
    string = [(20, 50), (240, 74), (460, 48)]
    L(c, string, BROWN, 3)
    for i, x in enumerate((60, 120, 180, 300, 360, 420)):
        y = on_polyline(string, x) + 4 + (i % 2) * 6
        L(c, [(x, y - 6), (x, y + 2)], BROWN, 2)
        col = (CORAL, GOLD, (226, 110, 160, 255))[i % 3]
        O(c, (x - 12, y, x + 12, y + 28), col, line=3)


def ha_long(c, mood):
    sea(c, mood, 220)
    for x, h, w in ((80, 130, 70), (190, 170, 80), (330, 140, 74), (430, 110, 60)):
        P(c, [(x - w / 2, 222), (x - w / 2 + 8, 222 - h * 0.7), (x - 10, 222 - h), (x + 14, 222 - h + 6),
              (x + w / 2 - 6, 222 - h * 0.6), (x + w / 2, 222)], (104, 150, 130, 255))
        c.d.ellipse(c._box((x - 12, 222 - h + 4, x + 14, 222 - h + 24)), fill=GREEN)
    # A junk boat with its orange batten sails.
    bx = 310
    P(c, [(bx - 70, 248), (bx + 70, 248), (bx + 54, 270), (bx - 54, 270)], (150, 100, 70, 255))
    for x, h in ((bx - 30, 80), (bx + 20, 96)):
        L(c, [(x, 248), (x, 248 - h)], BROWN, 4)
        P(c, [(x - 26, 248 - h + 6), (x + 22, 248 - h), (x + 26, 244), (x - 24, 244)], (240, 140, 70, 255), line=3)
        for k in range(1, 4):
            y = 248 - h + k * h / 4
            c.d.line(c._box((x - 24, y, x + 24, y)), fill=BROWN, width=2 * c_ss(c))


def hue(c, mood):
    ground(c, mood, 250)
    sea(c, mood, 262)
    # Thiên Mụ pagoda: seven octagonal storeys tapering up.
    cx = 300
    y = 250
    for i in range(7):
        w = 74 - i * 7
        h = 22 - i
        R(c, (cx - w / 2, y - h, cx + w / 2, y), STONE, r=3, line=3)
        R(c, (cx - 5, y - h + 5, cx + 5, y - 3), (120, 80, 60, 255), r=4, line=2)
        P(c, [(cx - w / 2 - 8, y - h + 2), (cx + w / 2 + 8, y - h + 2), (cx + w / 2 - 4, y - h - 6), (cx - w / 2 + 4, y - h - 6)], (180, 110, 90, 255), line=3)
        y -= h + 6
    L(c, [(cx, y + 4), (cx, y - 22)], BROWN, 4)
    O(c, (cx - 6, y - 30, cx + 6, y - 18), GOLD, line=2)
    pine(c, 400, 250, 70)
    pine(c, 430, 250, 54)


def sai_gon(c, mood):
    ground(c, mood, 246, colour=(196, 188, 176, 255))
    # Bitexco tower behind, with its helipad.
    bx = 396
    P(c, [(bx - 22, 246), (bx - 13, 112), (bx + 13, 112), (bx + 22, 246)], (150, 190, 220, 255))
    windows(c, (bx - 11, 126, bx + 11, 240), lit(mood), step=(10, 16), size=(4, 8))
    P(c, [(bx + 6, 156), (bx + 40, 150), (bx + 40, 160), (bx + 8, 164)], METAL, line=3)
    # Bến Thành market and its clock tower.
    cx = 230
    R(c, (cx - 120, 196, cx + 120, 246), (240, 220, 180, 255))
    for x in (cx - 108, cx - 78, cx - 48, cx + 30, cx + 60, cx + 90):
        R(c, (x, 206, x + 18, 236), (200, 120, 90, 255), r=8, line=3)
    R(c, (cx - 26, 130, cx + 26, 246), (240, 220, 180, 255))
    P(c, [(cx - 32, 132), (cx + 32, 132), (cx, 104)], RED)
    O(c, (cx - 16, 144, cx + 16, 176), CREAM, line=3)
    L(c, [(cx, 160), (cx, 150)], BROWN, 3)
    L(c, [(cx, 160), (cx + 8, 162)], BROWN, 3)


BRICK = (196, 104, 80, 255)
BRICK_DARK = (160, 80, 64, 255)
TILE_ROOF = (176, 96, 76, 255)
STRAW = (236, 208, 140, 255)
WOOD_BOAT = (150, 100, 70, 255)
CAVE = (46, 40, 52, 255)


def boat(c, x, y, s=1.0, hull=WOOD_BOAT):
    """A Mekong-style wooden boat with a raised bow and a painted eye."""
    P(c, [(x - 60 * s, y - 14 * s), (x + 50 * s, y - 14 * s), (x + 70 * s, y - 26 * s), (x + 58 * s, y), (x - 50 * s, y)], hull, line=3)
    O(c, (x + 44 * s, y - 16 * s, x + 54 * s, y - 8 * s), WHITE, line=2)
    c.d.ellipse(c._box((x + 47 * s, y - 14 * s, x + 51 * s, y - 10 * s)), fill=RED)


def rower(c, x, y, s=1.0):
    """A rower in a nón lá: a body, a round face and the conical leaf hat."""
    R(c, (x - 6 * s, y - 22 * s, x + 6 * s, y), (120, 150, 200, 255), r=3, line=3)
    O(c, (x - 6 * s, y - 32 * s, x + 6 * s, y - 20 * s), (250, 220, 190, 255), line=2)
    P(c, [(x - 16 * s, y - 28 * s), (x + 16 * s, y - 28 * s), (x, y - 44 * s)], STRAW, line=3)


def cham_tower(c, cx, base, s=1.0):
    """A Cham brick tower: tiers shrinking upwards, each with corner finials, a dark doorway."""
    tiers = ((34, 64), (26, 32), (18, 24), (11, 18))
    y = base
    for half, h in tiers:
        R(c, (cx - half * s, y - h * s, cx + half * s, y), BRICK, r=2, line=3)
        for fx in (cx - half * s, cx + half * s):
            P(c, [(fx - 5 * s, y - h * s), (fx + 5 * s, y - h * s), (fx, y - h * s - 12 * s)], BRICK_DARK, line=2)
        y -= h * s
    P(c, [(cx - 9 * s, y), (cx + 9 * s, y), (cx, y - 22 * s)], BRICK_DARK, line=3)
    R(c, (cx - 9 * s, base - 40 * s, cx + 9 * s, base), CAVE, r=int(8 * s), line=3)


def ha_noi(c, mood):
    ground(c, mood)
    for x in (410, 446):
        R(c, (x - 4, GROUND_Y - 30, x + 4, GROUND_Y), (150, 110, 80, 255), r=2, line=3)
        O(c, (x - 26, GROUND_Y - 80, x + 26, GROUND_Y - 26), GREEN, line=3)
    # Khuê Văn Các in the Temple of Literature: brick piers below, the pavilion with its sun windows above.
    cx = 290
    R(c, (cx - 74, 190, cx + 74, GROUND_Y), STONE)
    for x in (cx - 56, cx - 11, cx + 34):
        R(c, (x, 202, x + 22, GROUND_Y), (120, 90, 80, 255), r=10, line=3)
    roof(c, cx, 192, 170, 18, TILE_ROOF, curl=14)
    R(c, (cx - 46, 132, cx + 46, 176), (204, 92, 72, 255), r=3)
    for wx in (cx - 22, cx + 22):
        O(c, (wx - 15, 139, wx + 15, 169), CREAM, line=3)
        for k in range(8):
            a = k * math.pi / 4
            c.d.line(c._box((wx, 154, wx + 13 * math.cos(a), 154 + 13 * math.sin(a))), fill=BROWN, width=2 * c_ss(c))
    roof(c, cx, 134, 136, 28, TILE_ROOF, curl=16)
    L(c, [(cx - 44, 106), (cx + 44, 106)], BROWN, 4)
    O(c, (cx - 6, 96, cx + 6, 108), GOLD, line=2)


def sa_pa(c, mood):
    # Fansipan behind, rice terraces stepping down the valley, a stilt house on a ledge.
    P(c, [(40, 200), (170, 92), (290, 200)], (110, 150, 130, 255))
    P(c, [(200, 200), (340, 64), (480, 200)], (96, 140, 124, 255))
    P(c, [(312, 92), (340, 64), (368, 92), (352, 86), (340, 96), (326, 86)], WHITE, line=3)
    greens = ((176, 222, 140, 255), (150, 206, 120, 255), (196, 230, 150, 255), (140, 196, 112, 255))
    for k in range(6):
        top = 168 + k * 26
        pts = [(x, top + 9 * math.sin(x / 46.0 + k * 1.3)) for x in range(0, W + 1, 8)]
        c.d.polygon([(px * c_ss(c), py * c_ss(c)) for px, py in pts + [(W, H), (0, H)]], fill=greens[k % len(greens)])
        L(c, pts, (96, 150, 96, 255), 3)
    hx, hy = 390, 200
    for x in (hx - 30, hx + 22):
        L(c, [(x, hy), (x, hy + 18)], BROWN, 4)
    R(c, (hx - 34, hy - 30, hx + 30, hy), (200, 160, 120, 255), r=2, line=3)
    R(c, (hx - 8, hy - 22, hx + 6, hy - 8), (120, 90, 80, 255), r=2, line=2)
    P(c, [(hx - 44, hy - 28), (hx + 40, hy - 28), (hx, hy - 60)], STRAW, line=3)


def ninh_binh(c, mood):
    # Tràng An: round limestone karsts, a cave the boats row through, a rower in a nón lá.
    karst = (98, 150, 120, 255)
    for x, top, w in ((60, 120, 90), (420, 110, 100)):
        P(c, [(x - w / 2, 230), (x - w / 2 + 6, top + 40), (x - 14, top), (x + 16, top + 4), (x + w / 2 - 4, top + 36), (x + w / 2, 230)], karst)
        c.d.ellipse(c._box((x - 16, top + 2, x + 18, top + 26)), fill=GREEN)
    P(c, [(150, 230), (160, 120), (210, 86), (290, 80), (340, 108), (360, 230)], karst)
    c.d.ellipse(c._box((200, 84, 300, 120)), fill=GREEN)
    O(c, (196, 172, 304, 262), CAVE)
    sea(c, mood, 224)
    boat(c, 380, 262, 0.9)
    rower(c, 360, 250)
    L(c, [(378, 222), (410, 262)], BROWN, 4)


def phong_nha(c, mood):
    # A cave mouth in the limestone cliff, jungle along the top, the Son river flowing out of it.
    cliff = (160, 160, 150, 255)
    R(c, (0, 70, W, 250), cliff, r=0)
    for i, x in enumerate(range(-10, W + 40, 44)):
        r = 22 + (i * 7) % 12
        O(c, (x - r, 72 - r, x + r, 72 + r * 0.8), GREEN_DARK if i % 2 else PINE, line=3)
    O(c, (130, 110, 410, 330), CAVE)
    # A few thin stalactites of different lengths hanging from the arch.
    for x, length in ((196, 18), (238, 30), (262, 14), (300, 24), (346, 16)):
        top = 220 - 110 * math.sqrt(max(0.0, 1 - ((x - 270) / 140.0) ** 2)) + 2
        c.d.polygon([(px * c_ss(c), py * c_ss(c)) for px, py in ((x - 6, top - 4), (x + 6, top - 4), (x, top + length))], fill=cliff)
    for x, y in ((200, 196), (330, 182), (270, 210)):
        c.d.ellipse(c._box((x - 5, y - 5, x + 5, y + 5)), fill=(255, 220, 140, 255))
    sea(c, mood, 238)
    for x in (324, 344, 364):
        R(c, (x - 6, 244, x + 6, 258), (240, 140, 100, 255), r=3, line=2)
        O(c, (x - 6, 232, x + 6, 244), (250, 220, 190, 255), line=2)
    boat(c, 350, 270, 0.8, hull=(90, 140, 190, 255))


def da_nang(c, mood):
    ground(c, mood, 250, colour=(96, 156, 110, 255))
    for i, x in enumerate(range(30, W, 46)):
        r = 24 + (i * 5) % 8
        O(c, (x - r, 238 - r, x + r, 238 + r), (110, 170, 120, 255), line=3)
    # Cầu Vàng: the golden walkway lying across two giant stone palms, the fingers curling up round it.
    stone = (176, 172, 168, 255)

    def deck_y(x):
        return 150 - 24 * math.sin(math.pi * x / W)

    hands = (270, 380)
    for hx in hands:
        R(c, (hx - 22, 200, hx + 22, H), stone, r=6)
        R(c, (hx - 40, deck_y(hx) + 2, hx + 40, deck_y(hx) + 58), stone, r=26)
    deck = [(x, deck_y(x)) for x in range(0, W + 1, 12)]
    L(c, deck, BROWN, 18)
    L(c, deck, GOLD, 12)
    L(c, [(x, y - 14) for x, y in deck], BROWN, 3)
    for x, y in deck[::2]:
        c.d.line(c._box((x, y - 14, x, y - 6)), fill=BROWN, width=2 * c_ss(c))
    for hx in hands:
        for k in range(4):
            fx = hx - 27 + k * 18
            fy = deck_y(fx)
            R(c, (fx - 7, fy - 22 + abs(k - 1.5) * 4, fx + 7, fy + 24), stone, r=7, line=3)
        tx = hx + 40
        P(c, [(tx - 6, deck_y(tx) + 30), (tx + 10, deck_y(tx) + 4), (tx + 20, deck_y(tx) + 10), (tx + 6, deck_y(tx) + 40)], stone, line=3)


def nha_trang(c, mood):
    sea(c, mood, 186)
    c.d.polygon([(px * c_ss(c), py * c_ss(c)) for px, py in ((0, H), (0, 236), (160, 216), (300, 200), (480, 214), (480, H))],
                fill=mood[2] + (255,))
    P(c, [(70, 196), (130, 196), (120, 206), (80, 206)], WOOD_BOAT, line=3)
    L(c, [(100, 196), (100, 160)], BROWN, 3)
    P(c, [(102, 162), (124, 192), (102, 192)], WHITE, line=3)
    # The Po Nagar Cham towers on their hill above the bay.
    cham_tower(c, 230, 220, 0.7)
    cham_tower(c, 400, 216, 0.8)
    cham_tower(c, 310, 214, 1.15)


def quy_nhon(c, mood):
    sea(c, mood, 160)
    # Eo Gió: two grassy arms of rock closing round a cove, boulders and surf at their feet.
    rock = (200, 168, 132, 255)
    P(c, [(0, 140), (80, 146), (150, 200), (130, 246), (0, 256)], rock)
    P(c, [(480, 104), (380, 112), (318, 160), (322, 236), (380, 290), (480, 300)], rock)
    c.d.polygon([(px * c_ss(c), py * c_ss(c)) for px, py in ((480, 106), (382, 114), (350, 136), (480, 132))], fill=GREEN)
    c.d.polygon([(px * c_ss(c), py * c_ss(c)) for px, py in ((0, 142), (80, 148), (104, 166), (0, 160))], fill=GREEN)
    for x, y, r in ((300, 246, 16), (340, 270, 20), (150, 236, 14), (120, 254, 12)):
        O(c, (x - r, y - r * 0.8, x + r, y + r * 0.8), (176, 146, 116, 255), line=3)
    for x, y in ((280, 258), (320, 286), (168, 250)):
        c.d.ellipse(c._box((x - 14, y - 4, x + 14, y + 4)), fill=(255, 255, 255, 200))


def mui_ne(c, mood):
    sea(c, mood, 176)
    # Mũi Né: rippled red dunes in front, round basket boats bobbing in the bay.
    for x in (330, 380, 430):
        O(c, (x - 16, 184, x + 16, 198), (150, 104, 70, 255), line=3)
        c.d.line(c._box((x - 12, 191, x + 12, 191)), fill=BROWN, width=2 * c_ss(c))
    back = [(0, 222)] + [(x, 214 - 30 * math.sin(x / 80.0)) for x in range(0, W + 1, 8)] + [(W, 222)]
    c.d.polygon([(px * c_ss(c), py * c_ss(c)) for px, py in back + [(W, H), (0, H)]], fill=(240, 184, 124, 255))
    front = [(x, 252 - 22 * math.sin(x / 70.0 + 2)) for x in range(0, W + 1, 8)]
    c.d.polygon([(px * c_ss(c), py * c_ss(c)) for px, py in front + [(W, H), (0, H)]], fill=(226, 140, 96, 255))
    L(c, front, (190, 104, 76, 255), 3)
    for k in range(4):
        ripple = [(x, 270 + k * 12 - 22 * math.sin(x / 70.0 + 2) * 0.6) for x in range(200, W + 1, 8)]
        L(c, ripple, (204, 120, 84, 255), 2)
    palm(c, 430, 236, 100, -10)


def vung_tau(c, mood):
    sea(c, mood, 220)
    # The Christ of Vũng Tàu, arms open on top of Núi Nhỏ, a fishing boat below.
    P(c, [(120, 222), (230, 132), (330, 124), (440, 222)], GREEN_DARK)
    for x, y in ((210, 170), (250, 150), (300, 160), (350, 180), (280, 196)):
        c.d.ellipse(c._box((x - 10, y - 8, x + 10, y + 8)), fill=GREEN)
    L(c, [(330, 214), (300, 190), (320, 166), (290, 144), (282, 128)], CREAM, 3)
    cx = 282
    R(c, (cx - 12, 108, cx + 12, 128), STONE, r=2, line=3)
    P(c, [(cx - 11, 110), (cx - 6, 62), (cx + 6, 62), (cx + 11, 110)], WHITE, line=3)
    R(c, (cx - 34, 64, cx + 34, 71), WHITE, r=3, line=3)
    O(c, (cx - 7, 46, cx + 7, 60), WHITE, line=3)
    boat(c, 400, 262, 0.7, hull=(70, 120, 180, 255))


def phu_quoc(c, mood):
    sea(c, mood, 176)
    for x, w in ((280, 70), (340, 40)):
        c.d.ellipse(c._box((x - w, 164, x + w, 192)), fill=GREEN_DARK)
    # Bãi Sao: white sand, leaning palms with a hammock, the cable car out to the islands.
    L(c, [(16, 70), (320, 168)], BROWN, 2)
    gx, gy = 150, 113
    L(c, [(gx, gy), (gx, gy + 10)], BROWN, 2)
    R(c, (gx - 12, gy + 10, gx + 12, gy + 28), RED, r=4, line=3)
    R(c, (gx - 8, gy + 14, gx + 8, gy + 21), (190, 220, 240, 255), r=2, line=0)
    c.d.polygon([(px * c_ss(c), py * c_ss(c)) for px, py in ((0, H), (0, 250), (200, 234), (480, 224), (480, H))], fill=(252, 246, 230, 255))
    palm(c, 360, 262, 130, -22)
    palm(c, 450, 256, 120, 10)
    sling = [(352, 214), (380, 232), (410, 236), (446, 214)]
    L(c, sling, BROWN, 8)
    L(c, sling, CORAL, 4)
    star = [(300 + (10 if k % 2 == 0 else 4) * math.cos(math.radians(-90 + k * 36)),
             280 + (10 if k % 2 == 0 else 4) * math.sin(math.radians(-90 + k * 36))) for k in range(10)]
    P(c, star, (246, 150, 90, 255), line=2)


def can_tho(c, mood):
    ground(c, mood, 204, colour=(130, 184, 120, 255))
    for x in (60, 150, 250, 350, 440):
        palm(c, x, 214, 70, 6 if x % 100 else -6)
    sea(c, mood, 214)
    # Cái Răng floating market: boats piled with fruit, a cây bẹo pole showing what each one sells.
    for x, y, s in ((130, 232, 0.5), (400, 240, 0.7), (290, 274, 1.0)):
        boat(c, x, y, s)
        L(c, [(x - 30 * s, y - 14 * s), (x - 30 * s, y - 90 * s)], BROWN, max(2, 4 * s))
        for k, col in enumerate(((246, 150, 60, 255), GREEN, GOLD)):
            O(c, (x - 38 * s, y - (84 - k * 18) * s, x - 22 * s, y - (70 - k * 18) * s), col, line=2)
        for k, col in enumerate(((90, 160, 90, 255), (246, 150, 60, 255), GOLD, (90, 160, 90, 255))):
            fx = x - 8 * s + k * 14 * s
            O(c, (fx - 9 * s, y - 30 * s, fx + 9 * s, y - 14 * s), col, line=2)
    rower(c, 330, 262)

# ================================================================== Nhật Bản

def _tokyo_scene(c, mood):
    ground(c, mood, 248)
    for x, w, h in ((40, 50, 90), (100, 40, 120), (400, 60, 100), (440, 30, 70)):
        R(c, (x, 248 - h, x + w, 248), (160, 172, 200, 255), r=3, line=3)
        windows(c, (x + 6, 248 - h + 8, x + w - 4, 244), lit(mood), step=(12, 16), size=(5, 7))
    # Tokyo Tower: an orange and white lattice.
    cx = 270
    orange = (240, 110, 60, 255)
    P(c, [(cx - 70, 248), (cx - 12, 60), (cx + 12, 60), (cx + 70, 248), (cx + 44, 248), (cx, 160), (cx - 44, 248)], orange)
    for y, w in ((200, 92), (150, 60), (100, 34)):
        R(c, (cx - w / 2, y, cx + w / 2, y + 12), WHITE, r=3, line=3)
    for y in (126, 178, 224):
        # Keep each band inside the outline, and split it where the legs open.
        outer = 12 + (y - 60) * 58 / 188 - 4
        inner = 44 * (y - 160) / 88 + 4
        if inner > 4:
            for sgn in (-1, 1):
                c.d.line(c._box((cx + sgn * inner, y, cx + sgn * outer, y)), fill=WHITE, width=3 * c_ss(c))
        else:
            c.d.line(c._box((cx - outer, y, cx + outer, y)), fill=WHITE, width=3 * c_ss(c))
    L(c, [(cx, 60), (cx, 28)], BROWN, 4)


def kyoto(c, mood):
    ground(c, mood)
    # Rows of red torii gates climbing towards a five-storey pagoda, under cherry blossom.
    cx = 330
    y = GROUND_Y
    for i, (w, h) in enumerate(((84, 22), (74, 20), (64, 19), (54, 18), (44, 17))):
        R(c, (cx - w / 2 + 12, y - h, cx + w / 2 - 12, y), CREAM, r=2, line=3)
        P(c, [(cx - w / 2 - 6, y - h + 2), (cx + w / 2 + 6, y - h + 2), (cx + w / 2 - 6, y - h - 8), (cx - w / 2 + 6, y - h - 8)], (80, 70, 80, 255), line=3)
        y -= h + 10
    L(c, [(cx, y + 6), (cx, y - 20)], BROWN, 4)
    # Far gates first, so the near gate covers them.
    for k, x in reversed(list(enumerate((210, 182, 154)))):
        torii(c, x, GROUND_Y + 6 - k * 8, 84 - k * 12, 98 - k * 14)
    sakura_tree(c, 430, GROUND_Y, 0.9)
    sakura_branch(c)
    petals(c)


def _osaka_scene(c, mood):
    ground(c, mood, 248)
    cx = 280
    # Stone base, then white storeys with green roofs and gold trim.
    P(c, [(cx - 120, 248), (cx - 100, 196), (cx + 100, 196), (cx + 120, 248)], STONE_DARK)
    for i, (w, h) in enumerate(((150, 34), (120, 30), (92, 28), (64, 26))):
        y = 196 - i * 40
        R(c, (cx - w / 2, y - h, cx + w / 2, y), WHITE, r=3)
        roof(c, cx, y - h, w + 24, 14, (100, 168, 150, 255))
        c.d.line(c._box((cx - w / 2 - 4, y - h + 2, cx + w / 2 + 4, y - h + 2)), fill=GOLD, width=3 * c_ss(c))
    P(c, [(cx - 10, 52), (cx + 10, 52), (cx, 36)], GOLD, line=3)


def sapporo(c, mood):
    ground(c, mood, 240, colour=(244, 248, 252, 255))
    # Snow country: the snow-capped mountain and the bullet train racing past.
    fuji(c, 270, 214, 190, 150)
    c.d.rectangle(c._box((0, 214, W, 240)), fill=(244, 248, 252, 255))
    L(c, [(0, 236), (W, 236)], BROWN, 6)
    L(c, [(0, 236), (W, 236)], METAL, 3)
    P(c, [(120, 234), (120, 204), (300, 204), (360, 222), (370, 234)], WHITE)
    c.d.line(c._box((124, 222, 360, 222)), fill=(60, 110, 190, 255), width=5 * c_ss(c))
    for x in range(140, 300, 26):
        R(c, (x, 210, x + 16, 218), (150, 190, 220, 255), r=3, line=2)
    for i in range(24):
        x = (i * 83) % 440 + 20
        y = (i * 41) % 130 + 30
        c.d.ellipse(c._box((x - 3, y - 3, x + 3, y + 3)), fill=WHITE)


def okinawa(c, mood):
    sea(c, mood, 200)
    c.d.rectangle(c._box((0, 252, W, H)), fill=SAND)
    # A great red torii standing in the sea, a beach and palms.
    torii(c, 290, 238, 150, 150)
    c.d.ellipse(c._box((200, 232, 380, 246)), fill=(255, 255, 255, 140))
    palm(c, 430, 252, 110, -16)
    sakura_branch(c)


# ================================================================== Hàn Quốc

def seoul(c, mood):
    ground(c, mood, 248)
    # N Seoul Tower on its hill, and Gyeongbokgung's great gate in front.
    c.d.ellipse(c._box((20, 150, 300, 320)), fill=GREEN_DARK)
    tx = 150
    P(c, [(tx - 10, 160), (tx - 5, 90), (tx + 5, 90), (tx + 10, 160)], WHITE)
    R(c, (tx - 22, 82, tx + 22, 100), WHITE, r=8, line=3)
    L(c, [(tx, 82), (tx, 50)], BROWN, 4)
    cx = 310
    R(c, (cx - 110, 196, cx + 110, 248), STONE)
    for x in (cx - 70, cx - 18, cx + 34):
        R(c, (x, 214, x + 36, 248), (90, 70, 60, 255), r=16, line=3)
    for x in range(int(cx - 80), int(cx + 80), 32):
        R(c, (x, 166, x + 10, 196), RED, r=2, line=3)
    R(c, (cx - 92, 158, cx + 92, 170), TEAL, r=3, line=3)
    roof(c, cx, 158, 230, 30, (70, 80, 96, 255), curl=16)
    flag_pole(c, 386, 248, 70, korea_flag)


def busan(c, mood):
    sea(c, mood, 250)
    # Gamcheon Culture Village: pastel box houses stepping up the hillside, far rows first.
    c.d.polygon([(xy[0] * c_ss(c), xy[1] * c_ss(c)) for xy in ((140, 250), (240, 150), (480, 128), (480, 250))], fill=GREEN_DARK)
    pastel = ((150, 206, 214, 255), PINK, (250, 214, 120, 255), (160, 190, 236, 255), (190, 226, 176, 255), LAVENDER)
    rows = ((168, (300, 360, 420), 50), (206, (250, 318, 388, 452), 56), (246, (214, 290, 366), 64))
    k = 0
    for y, xs, w in rows:
        for x in xs:
            R(c, (x - w / 2, y - 38, x + w / 2, y), pastel[k % len(pastel)], r=2, line=3)
            R(c, (x - w / 2 - 4, y - 44, x + w / 2 + 4, y - 36), (70, 80, 96, 255), r=2, line=3)
            for wx in (x - w / 4, x + w / 4):
                c.d.rectangle(c._box((wx - 5, y - 28, wx + 5, y - 16)), fill=(90, 120, 160, 255))
            k += 1
    flag_pole(c, 120, 250, 110, korea_flag)


def _jeju_scene(c, mood):
    sea(c, mood, 220)
    # Seongsan Ilchulbong: the green crater rising out of the sea.
    P(c, [(150, 222), (206, 108), (324, 108), (400, 222)], GREEN_DARK)
    c.d.ellipse(c._box((216, 111, 314, 127)), fill=GREEN)
    # A dol hareubang, the stone grandfather, on the shore: a hat, big eyes, hands on belly.
    c.d.rectangle(c._box((0, 248, W, H)), fill=(150, 140, 136, 255))
    hx = 420
    stone = (176, 172, 176, 255)
    O(c, (hx - 30, 156, hx + 30, 250), stone)
    O(c, (hx - 30, 118, hx + 30, 172), stone)
    R(c, (hx - 32, 108, hx + 32, 124), (140, 136, 140, 255), r=8, line=3)
    for ex in (hx - 13, hx + 13):
        O(c, (ex - 8, 132, ex + 8, 148), CREAM, line=3)
        c.d.ellipse(c._box((ex - 3, 137, ex + 3, 143)), fill=BROWN)
    O(c, (hx - 7, 146, hx + 7, 162), stone, line=3)
    for y in (194, 210):
        c.d.line(c._box((hx - 20, y, hx + 20, y)), fill=BROWN, width=3 * c_ss(c))


def gyeongju(c, mood):
    sea(c, mood, 210)
    # A royal pavilion on its lotus pond: red pillars, a dark sweeping roof, its reflection.
    cx = 300
    R(c, (cx - 120, 196, cx + 120, 212), STONE)
    for x in range(int(cx - 104), int(cx + 104), 34):
        R(c, (x, 140, x + 10, 198), RED, r=2, line=3)
    R(c, (cx - 112, 132, cx + 112, 144), TEAL, r=3, line=3)
    roof(c, cx, 132, 250, 40, (70, 80, 96, 255), curl=18)
    for x in range(int(cx - 104), int(cx + 104), 34):
        c.d.rectangle(c._box((x, 218, x + 10, 248)), fill=(226, 92, 76, 90))
    for y in (228, 240):
        c.d.line(c._box((cx - 100, y, cx + 100, y)), fill=(255, 255, 255, 120), width=2 * c_ss(c))
    for x in (90, 150):
        O(c, (x - 18, 232, x + 18, 246), GREEN, line=3)
        O(c, (x - 6, 222, x + 6, 236), PINK, line=2)
    flag_pole(c, 70, 210, 110, korea_flag)


def incheon(c, mood):
    ground(c, mood, 246, colour=(200, 196, 190, 255))
    # The gateway to Korea: the airport terminal, a jet climbing out, the flag flying.
    cx = 300
    P(c, [(cx - 150, 246), (cx - 150, 196), (cx, 170), (cx + 150, 196), (cx + 150, 246)], (210, 226, 240, 255))
    for x in range(int(cx - 130), int(cx + 130), 26):
        c.d.line(c._box((x, 190, x, 244)), fill=WHITE, width=3 * c_ss(c))
    L(c, [(cx - 150, 196), (cx, 170), (cx + 150, 196)], BROWN, 5)
    px, py = 200, 110
    P(c, [(px - 44, py + 18), (px - 62, py - 12), (px - 50, py - 14), (px - 28, py + 12)], (150, 190, 220, 255), line=3)
    P(c, [(px - 50, py + 14), (px + 40, py - 10), (px + 54, py - 8), (px + 44, py + 4), (px - 46, py + 26)], WHITE)
    P(c, [(px - 4, py + 6), (px + 20, py + 34), (px + 32, py + 32), (px + 20, py + 2)], (150, 190, 220, 255), line=3)
    for k in range(6):
        wx, wy = px - 30 + k * 11, py + 12 - k * 2.6
        c.d.ellipse(c._box((wx - 2, wy - 2, wx + 2, wy + 2)), fill=(90, 120, 160, 255))
    flag_pole(c, 90, 246, 130, korea_flag)


# ================================================================== Pháp

def _paris_scene(c, mood):
    ground(c, mood)
    cx, base = 280, GROUND_Y
    tower = (150, 120, 104, 255)
    # Legs ending in a round arch, then the middle and the spire, platforms drawn over the joins.
    arch = [(cx + 40 * math.cos(a), base - 4 - 38 * math.sin(a)) for a in [i * math.pi / 8 for i in range(9)]]
    P(c, [(cx - 66, base), (cx - 26, 160), (cx + 26, 160), (cx + 66, base), (cx + 40, base)] + arch + [(cx - 40, base)], tower)
    P(c, [(cx - 26, 162), (cx - 12, 112), (cx + 12, 112), (cx + 26, 162)], tower)
    P(c, [(cx - 12, 114), (cx - 3, 70), (cx + 3, 70), (cx + 12, 114)], tower)
    R(c, (cx - 40, 156, cx + 40, 166), tower, r=3)
    R(c, (cx - 18, 108, cx + 18, 116), tower, r=3)
    L(c, [(cx, 70), (cx, 58)], BROWN, 3)
    for x in (380, 420):
        O(c, (x - 20, 196, x + 20, 236), GREEN_DARK)


def _nice_scene(c, mood):
    sea(c, mood, 196)
    c.d.rectangle(c._box((0, 232, W, H)), fill=(220, 214, 204, 255))
    L(c, [(0, 232), (W, 232)], WHITE, 4)
    # The Promenade des Anglais: blue chairs and palms on the walk above the Baie des Anges.
    for x in (340, 430):
        palm(c, x, 262, 110, 8)
    for x in (160, 220, 280):
        R(c, (x, 250, x + 34, 260), (90, 150, 220, 255), r=4, line=3)
        R(c, (x + 24, 228, x + 34, 260), (90, 150, 220, 255), r=4, line=3)


def lyon(c, mood):
    ground(c, mood, 250, colour=(214, 204, 192, 255))
    # A French café: the striped awning, a table outside with a coffee and a croissant.
    cx = 290
    R(c, (cx - 120, 120, cx + 120, 250), CREAM)
    R(c, (cx - 100, 160, cx - 10, 230), (190, 220, 240, 255), r=4, line=3)
    R(c, (cx + 16, 160, cx + 66, 250), (120, 90, 80, 255), r=4, line=3)
    for i in range(8):
        col = (60, 90, 170, 255) if i % 2 == 0 else WHITE
        x = cx - 128 + i * 32
        P(c, [(x, 126), (x + 32, 126), (x + 32, 150), (x + 16, 158), (x, 150)], col, line=3)
    R(c, (cx - 60, 90, cx + 60, 118), (60, 90, 170, 255), r=6)
    c.d.text((cx * c_ss(c), 104 * c_ss(c)), "CAFÉ", fill=CREAM, anchor="mm", font=font_small(c))
    L(c, [(cx + 104, 222), (cx + 104, 250)], BROWN, 4)
    L(c, [(cx + 92, 250), (cx + 116, 250)], BROWN, 4)
    R(c, (cx + 80, 216, cx + 128, 222), (90, 70, 60, 255), r=3, line=3)
    O(c, (cx + 86, 202, cx + 106, 214), GOLD, line=3)
    R(c, (cx + 110, 202, cx + 122, 214), WHITE, r=3, line=3)
    tricolour_bunting(c)


def _provence_scene(c, mood):
    # The sun sets behind the far hills, clear of the bunting.
    O(c, (196, 126, 244, 174), GOLD, line=3)
    ground(c, mood, 200, colour=LAVENDER)
    # Rows of lavender running to the horizon, a stone farmhouse at the end.
    for i in range(7):
        x = -60 + i * 90
        c.d.line(c._box((x, H, 240 + (x - 240) * 0.18, 200)), fill=(130, 100, 180, 255), width=10 * c_ss(c))
    R(c, (320, 150, 420, 202), STONE)
    P(c, [(312, 152), (428, 152), (400, 128), (340, 128)], (210, 130, 100, 255))
    R(c, (340, 166, 356, 186), (150, 190, 220, 255), r=3, line=3)
    R(c, (376, 168, 396, 202), (150, 110, 90, 255), r=3, line=3)


def _bordeaux_scene(c, mood):
    ground(c, mood, 210)
    # Vineyard rows in front of a château with pointed turrets.
    for y in range(222, H, 18):
        c.d.line(c._box((0, y, W, y)), fill=GREEN_DARK, width=8 * c_ss(c))
        for x in range(10, W, 26):
            c.d.ellipse(c._box((x - 4, y - 8, x + 4, y)), fill=(120, 70, 120, 255))
    # Main house and roof first, then the turrets in front of it, all below the bunting.
    cx = 260
    R(c, (cx - 70, 150, cx + 70, 210), CREAM)
    P(c, [(cx - 76, 152), (cx + 76, 152), (cx + 50, 126), (cx - 50, 126)], SLATE)
    for x in (cx - 46, cx - 12, cx + 22):
        R(c, (x, 166, x + 18, 194), (150, 190, 220, 255), r=3, line=3)
    for x in (cx - 94, cx + 70):
        R(c, (x, 134, x + 24, 210), CREAM)
        P(c, [(x - 6, 136), (x + 30, 136), (x + 12, 98)], SLATE)
        R(c, (x + 7, 158, x + 17, 176), (150, 190, 220, 255), r=5, line=3)


# ================================================================== Ai Cập

def cairo(c, mood):
    ground(c, mood, 244, colour=SAND)
    far_pyramids(c, 222)
    # The Muhammad Ali Mosque on the Citadel: a great dome on its drum, half domes, two pencil minarets.
    cx = 270
    for x in (cx - 92, cx + 92):
        R(c, (x - 6, 82, x + 6, 244), STONE, r=2, line=3)
        R(c, (x - 10, 128, x + 10, 136), STONE_DARK, r=2, line=3)
        R(c, (x - 10, 176, x + 10, 184), STONE_DARK, r=2, line=3)
        P(c, [(x - 8, 84), (x + 8, 84), (x, 54)], SLATE, line=3)
    O(c, (cx - 40, 96, cx + 40, 172), SLATE)
    L(c, [(cx, 96), (cx, 80)], BROWN, 3)
    O(c, (cx - 5, 72, cx + 5, 82), GOLD, line=2)
    R(c, (cx - 46, 140, cx + 46, 176), STONE, r=3)
    for x in (cx - 34, cx - 12, cx + 10, cx + 32):
        R(c, (x - 4, 148, x + 4, 166), (120, 100, 90, 255), r=4, line=2)
    for x in (cx - 50, cx + 50):
        O(c, (x - 28, 162, x + 28, 214), SLATE)
    R(c, (cx - 86, 190, cx + 86, 244), STONE, r=3)
    for x in (cx - 66, cx - 34, cx + 22, cx + 54):
        R(c, (x, 204, x + 12, 228), (120, 100, 90, 255), r=6, line=3)
    R(c, (cx - 12, 200, cx + 12, 244), (150, 110, 90, 255), r=10)
    camel(c, 420, 252, 0.6)


def giza(c, mood):
    ground(c, mood, GROUND_Y, colour=SAND)
    for x, s in ((300, 1.0), (410, 0.72), (190, 0.56)):
        w, h = 180 * s, 140 * s
        P(c, [(x - w / 2, GROUND_Y), (x, GROUND_Y - h), (x + w / 2, GROUND_Y)], SAND)
        c.d.polygon([(xy[0] * c_ss(c), xy[1] * c_ss(c)) for xy in ((x, GROUND_Y - h), (x + w / 2, GROUND_Y), (x + w / 6, GROUND_Y))], fill=SAND_DARK)
    # The Sphinx lying in front.
    sx = 104
    R(c, (sx, 212, sx + 96, 240), SAND_DARK, r=12)
    R(c, (sx + 80, 228, sx + 126, 240), SAND_DARK, r=6)
    P(c, [(sx + 58, 222), (sx + 62, 188), (sx + 70, 172), (sx + 94, 172), (sx + 102, 188), (sx + 106, 222)], SAND)
    for y in (196, 206, 216):
        c.d.line(c._box((sx + 62, y, sx + 70, y)), fill=SAND_DARK, width=3 * c_ss(c))
        c.d.line(c._box((sx + 94, y, sx + 102, y)), fill=SAND_DARK, width=3 * c_ss(c))
    R(c, (sx + 71, 180, sx + 93, 210), SAND_DARK, r=6, line=3)
    for ex in (sx + 77, sx + 87):
        c.d.ellipse(c._box((ex - 2, 190, ex + 2, 194)), fill=BROWN)


def luxor(c, mood):
    ground(c, mood, 248, colour=SAND)
    far_pyramids(c, 226)
    # The golden mask of a pharaoh: striped headdress, a calm face, the plaited beard.
    cx = 300
    blue = (60, 90, 170, 255)
    P(c, [(cx - 90, 230), (cx - 70, 80), (cx, 56), (cx + 70, 80), (cx + 90, 230)], GOLD)
    for k in range(1, 6):
        y = 80 + k * 22
        # Stripes stop just inside the slanted outline of the headdress.
        edge = 90 - 20 * (230 - y) / 150 - 6
        c.d.line(c._box((cx - edge, y, cx - 44, y)), fill=blue, width=7 * c_ss(c))
        c.d.line(c._box((cx + 44, y, cx + edge, y)), fill=blue, width=7 * c_ss(c))
    O(c, (cx - 44, 86, cx + 44, 196), GOLD)
    for ex in (cx - 18, cx + 18):
        P(c, [(ex - 14, 128), (ex + 14, 128), (ex + 6, 136), (ex - 10, 136)], WHITE, line=3)
        c.d.ellipse(c._box((ex - 4, 128, ex + 4, 136)), fill=BROWN)
    R(c, (cx - 60, 192, cx + 60, 212), blue, r=8)
    c.d.line(c._box((cx - 52, 202, cx + 52, 202)), fill=GOLD, width=4 * c_ss(c))
    R(c, (cx - 9, 210, cx + 9, 240), blue, r=4)
    for y in (220, 230):
        c.d.line(c._box((cx - 7, y, cx + 7, y)), fill=GOLD, width=3 * c_ss(c))


def _aswan_scene(c, mood):
    c.d.polygon([(xy[0] * c_ss(c), xy[1] * c_ss(c)) for xy in ((0, 200), (120, 170), (260, 196), (400, 160), (480, 186), (480, 230), (0, 230))], fill=SAND)
    sea(c, mood, 226)
    # Feluccas with their tall slanting sails on the Nile.
    for x, s in ((260, 1.0), (390, 0.7)):
        P(c, [(x - 50 * s, 252), (x + 50 * s, 252), (x + 36 * s, 266), (x - 36 * s, 266)], (150, 100, 70, 255))
        L(c, [(x, 252), (x, 252 - 120 * s)], BROWN, 4)
        P(c, [(x + 2, 252 - 118 * s), (x + 60 * s, 248), (x + 4, 248)], WHITE)
    palm(c, 80, 226, 90, 10)


def _alexandria_scene(c, mood):
    sea(c, mood, 228)
    # A lighthouse on the harbour wall, its lamp lit.
    R(c, (180, 226, 420, 250), STONE_DARK, r=3, line=3)
    cx = 300
    R(c, (cx - 50, 178, cx + 50, 230), STONE)
    R(c, (cx - 34, 116, cx + 34, 180), STONE)
    R(c, (cx - 22, 70, cx + 22, 118), STONE)
    O(c, (cx - 16, 44, cx + 16, 76), GOLD)
    if is_night(mood):
        for ang in (-10, 190):
            rad = math.radians(ang)
            c.d.polygon([(cx * c_ss(c), 60 * c_ss(c)),
                         ((cx + 200 * math.cos(rad)) * c_ss(c), (60 + 200 * math.sin(rad) - 20) * c_ss(c)),
                         ((cx + 200 * math.cos(rad)) * c_ss(c), (60 + 200 * math.sin(rad) + 20) * c_ss(c))],
                        fill=(255, 236, 170, 90))
    for y in (190, 136):
        R(c, (cx - 8, y, cx + 8, y + 22), (120, 90, 70, 255), r=3, line=3)


# ================================================================== Mỹ

def _new_york_scene(c, mood):
    ground(c, mood, 250)
    for x, w, h in ((20, 46, 120), (70, 36, 160), (380, 44, 140), (430, 40, 100)):
        R(c, (x, 250 - h, x + w, 250), (160, 172, 200, 255), r=3, line=3)
        windows(c, (x + 6, 250 - h + 8, x + w - 4, 246), lit(mood), step=(12, 16), size=(5, 7))
    # The Statue of Liberty in her green copper, torch raised.
    copper = (120, 190, 170, 255)
    cx = 250
    R(c, (cx - 36, 206, cx + 36, 250), STONE)
    P(c, [(cx - 24, 206), (cx - 14, 120), (cx + 14, 120), (cx + 24, 206)], copper)
    O(c, (cx - 14, 96, cx + 14, 126), copper)
    for ang in range(-150, -20, 26):
        rad = math.radians(ang)
        c.d.line(c._box((cx + 12 * math.cos(rad), 104 + 12 * math.sin(rad), cx + 24 * math.cos(rad), 104 + 24 * math.sin(rad))),
                 fill=BROWN, width=4 * c_ss(c))
    L(c, [(cx + 14, 136), (cx + 26, 96), (cx + 30, 62)], BROWN, 12)
    L(c, [(cx + 14, 136), (cx + 26, 96), (cx + 30, 62)], copper, 7)
    P(c, [(cx + 22, 64), (cx + 38, 64), (cx + 30, 40)], GOLD, line=3)


def _san_francisco_scene(c, mood):
    sea(c, mood, 230)
    c.d.polygon([(xy[0] * c_ss(c), xy[1] * c_ss(c)) for xy in ((0, 230), (0, 190), (80, 170), (140, 230))], fill=GREEN_DARK)
    # The Golden Gate: two red towers and the curving cables between.
    red = (220, 90, 60, 255)
    L(c, [(60, 214), (480, 214)], BROWN, 12)
    L(c, [(60, 214), (480, 214)], red, 7)
    for tx in (190, 380):
        R(c, (tx - 10, 96, tx + 10, 240), red, r=3)
        for y in (110, 150, 190):
            R(c, (tx - 14, y, tx + 14, y + 8), red, r=2, line=3)
    L(c, [(80, 200), (130, 160), (190, 98), (285, 188), (380, 98), (430, 160), (480, 190)], BROWN, 5)
    L(c, [(80, 200), (130, 160), (190, 98), (285, 188), (380, 98), (430, 160), (480, 190)], red, 3)


def _hawaii_scene(c, mood):
    sea(c, mood, 210)
    # Diamond Head across the bay, a beach, palms and a surfboard.
    P(c, [(190, 212), (260, 132), (340, 120), (420, 212)], (130, 160, 110, 255))
    c.d.rectangle(c._box((0, 242, W, H)), fill=SAND)
    palm(c, 420, 242, 120, -14)
    palm(c, 380, 242, 90, 6)
    O(c, (242, 150, 274, 256), (240, 140, 100, 255))
    c.d.line(c._box((258, 160, 258, 246)), fill=WHITE, width=3 * c_ss(c))
    O(c, (230, 246, 286, 262), SAND, line=3)


def _los_angeles_scene(c, mood):
    # The Hollywood sign on the hills, palms along the boulevard.
    c.d.polygon([(xy[0] * c_ss(c), xy[1] * c_ss(c)) for xy in ((0, 250), (60, 170), (200, 120), (340, 140), (480, 110), (480, 250))], fill=(170, 150, 110, 255))
    ground(c, mood, 250, colour=(196, 188, 176, 255))
    for i, ch in enumerate("HOLLYWOOD"):
        x = 118 + i * 30
        y = 146
        R(c, (x, y, x + 24, y + 34), WHITE, r=2, line=2)
        c.d.text(((x + 12) * c_ss(c), (y + 18) * c_ss(c)), ch, fill=BROWN, anchor="mm", font=font_small(c))
    for x in (60, 432, 462):
        palm(c, x, 250, 120, 6)


def chicago(c, mood):
    ground(c, mood, 250, colour=(200, 196, 190, 255))
    # A downtown skyline under a big stars and stripes.
    for x, w, h, col in ((120, 46, 130, (160, 172, 200)), (170, 40, 100, (176, 186, 210)), (300, 50, 200, (70, 74, 90)),
                         (360, 44, 150, (160, 172, 200)), (414, 50, 110, (176, 186, 210))):
        R(c, (x, 250 - h, x + w, 250), col + (255,), r=3, line=3)
        windows(c, (x + 6, 250 - h + 8, x + w - 4, 246), lit(mood), step=(12, 16), size=(5, 7))
    L(c, [(312, 50), (312, 30)], BROWN, 3)
    L(c, [(338, 50), (338, 30)], BROWN, 3)
    flag_pole(c, 230, 250, 150, usa_flag)


def font_small(c):
    from generate_home import font
    return font(20)


def tokyo(c, mood):
    fuji(c, 140, GROUND_Y - 10, 150, 130)
    _tokyo_scene(c, mood)
    sakura_branch(c)


def osaka(c, mood):
    _osaka_scene(c, mood)
    sakura_tree(c, 100, 248, 0.9); sakura_tree(c, 430, 248, 0.8); petals(c)


def jeju(c, mood):
    _jeju_scene(c, mood)
    flag_pole(c, 120, 248, 96, korea_flag)


def paris(c, mood):
    _paris_scene(c, mood)
    tricolour_bunting(c)


def nice(c, mood):
    _nice_scene(c, mood)
    tricolour_bunting(c)


def provence(c, mood):
    _provence_scene(c, mood)
    tricolour_bunting(c)


def bordeaux(c, mood):
    _bordeaux_scene(c, mood)
    tricolour_bunting(c)


def aswan(c, mood):
    far_pyramids(c, 200)
    _aswan_scene(c, mood)


def alexandria(c, mood):
    far_pyramids(c, 228)
    _alexandria_scene(c, mood)


def new_york(c, mood):
    _new_york_scene(c, mood)
    flag_pole(c, 340, 250, 120, usa_flag)


def san_francisco(c, mood):
    _san_francisco_scene(c, mood)
    flag_pole(c, 40, 228, 110, usa_flag)


def hawaii(c, mood):
    _hawaii_scene(c, mood)
    flag_pole(c, 120, 242, 100, usa_flag)


def los_angeles(c, mood):
    _los_angeles_scene(c, mood)
    flag_pole(c, 330, 250, 56, usa_flag)


LANDMARKS = {
    "hanoi": ha_noi, "ha_long": ha_long, "ninh_binh": ninh_binh, "sa_pa": sa_pa, "phong_nha": phong_nha,
    "hue": hue, "da_nang": da_nang, "hoi_an": hoi_an, "quy_nhon": quy_nhon, "nha_trang": nha_trang,
    "da_lat": da_lat, "mui_ne": mui_ne, "vung_tau": vung_tau, "saigon": sai_gon, "phu_quoc": phu_quoc,
    "can_tho": can_tho,
    "tokyo": tokyo, "kyoto": kyoto, "osaka": osaka, "sapporo": sapporo, "okinawa": okinawa,
    "seoul": seoul, "busan": busan, "jeju": jeju, "gyeongju": gyeongju, "incheon": incheon,
    "paris": paris, "nice": nice, "lyon": lyon, "provence": provence, "bordeaux": bordeaux,
    "cairo": cairo, "giza": giza, "luxor": luxor, "aswan": aswan, "alexandria": alexandria,
    "new_york": new_york, "san_francisco": san_francisco, "hawaii": hawaii, "los_angeles": los_angeles,
    "chicago": chicago,
}
