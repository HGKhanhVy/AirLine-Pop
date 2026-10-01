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
    for x in (cx - w * 0.36, cx + w * 0.36):
        R(c, (x - 7, base - h, x + 7, base), TORII, r=3, line=3)
    R(c, (cx - w * 0.44, base - h * 0.78, cx + w * 0.44, base - h * 0.7), TORII, r=3, line=3)
    P(c, [(cx - w / 2 - 6, base - h - 4), (cx + w / 2 + 6, base - h - 4), (cx + w / 2 - 2, base - h + 10), (cx - w / 2 + 2, base - h + 10)], TORII, line=3)
    R(c, (cx - w / 2 - 2, base - h - 14, cx + w / 2 + 2, base - h - 4), (60, 60, 80, 255), r=3, line=3)


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
    L(c, [(20, 50), (240, 74), (460, 48)], BROWN, 3)
    for i, x in enumerate((60, 120, 180, 300, 360, 420)):
        y = 60 + 14 * math.sin(i)
        col = (CORAL, GOLD, (226, 110, 160, 255))[i % 3]
        O(c, (x - 12, y, x + 12, y + 28), col, line=3)


def ha_long(c, mood):
    sea(c, mood, 220)
    for x, h, w in ((80, 130, 70), (190, 170, 80), (330, 140, 74), (430, 110, 60)):
        P(c, [(x - w / 2, 222), (x - w / 2 + 8, 222 - h * 0.7), (x - 10, 222 - h), (x + 14, 222 - h + 6),
              (x + w / 2 - 6, 222 - h * 0.6), (x + w / 2, 222)], (104, 150, 130, 255))
        c.d.ellipse(c._box((x - 12, 222 - h + 4, x + 14, 222 - h + 24)), fill=GREEN)
    # A junk boat with its orange batten sails.
    bx = 260
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
    bx = 380
    P(c, [(bx - 24, 246), (bx - 14, 60), (bx + 14, 60), (bx + 24, 246)], (150, 190, 220, 255))
    windows(c, (bx - 12, 76, bx + 12, 240), lit(mood), step=(10, 16), size=(4, 8))
    P(c, [(bx + 6, 120), (bx + 46, 112), (bx + 46, 124), (bx + 8, 128)], METAL, line=3)
    # Bến Thành market and its clock tower.
    cx = 230
    R(c, (cx - 120, 196, cx + 120, 246), (240, 220, 180, 255))
    for x in range(int(cx - 104), int(cx + 104), 34):
        R(c, (x, 206, x + 18, 236), (200, 120, 90, 255), r=8, line=3)
    R(c, (cx - 26, 130, cx + 26, 246), (240, 220, 180, 255))
    P(c, [(cx - 32, 132), (cx + 32, 132), (cx, 104)], RED)
    O(c, (cx - 16, 144, cx + 16, 176), CREAM, line=3)
    L(c, [(cx, 160), (cx, 150)], BROWN, 3)
    L(c, [(cx, 160), (cx + 8, 162)], BROWN, 3)


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
        c.d.line(c._box((cx - (248 - y) * 0.32, y, cx + (248 - y) * 0.32, y)), fill=WHITE, width=3 * c_ss(c))
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
    for k, x in enumerate((210, 186, 162)):
        torii(c, x, GROUND_Y + 6 - k * 6, 84 - k * 10, 98 - k * 10)
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
    # A hanok village: white walls under dark curving tiled roofs, by the sea.
    c.d.polygon([(xy[0] * c_ss(c), xy[1] * c_ss(c)) for xy in ((140, 250), (240, 150), (480, 130), (480, 250))], fill=GREEN_DARK)
    for x, y, w in ((210, 248, 110), (330, 236, 100), (400, 200, 90), (270, 196, 80)):
        R(c, (x - w / 2 + 10, y - 40, x + w / 2 - 10, y), WHITE, r=2, line=3)
        for k in range(3):
            c.d.rectangle(c._box((x - w / 2 + 20 + k * (w - 40) / 3, y - 32, x - w / 2 + 34 + k * (w - 40) / 3, y - 12)), fill=(196, 150, 110, 255))
        roof(c, x, y - 40, w, 22, (70, 80, 96, 255), curl=12)
    flag_pole(c, 120, 250, 110, korea_flag)


def _jeju_scene(c, mood):
    sea(c, mood, 220)
    # Seongsan Ilchulbong: the green crater rising out of the sea.
    P(c, [(150, 222), (200, 110), (330, 100), (400, 222)], GREEN_DARK)
    c.d.ellipse(c._box((206, 102, 324, 126)), fill=GREEN)
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
    roof(c, cx, 132, 270, 40, (70, 80, 96, 255), curl=20)
    c.d.rectangle(c._box((cx - 110, 220, cx + 110, 250)), fill=(200, 110, 100, 90))
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
    P(c, [(px - 50, py + 14), (px + 40, py - 10), (px + 54, py - 8), (px + 44, py + 4), (px - 46, py + 26)], WHITE)
    P(c, [(px - 4, py + 6), (px + 20, py + 34), (px + 32, py + 32), (px + 20, py + 2)], (150, 190, 220, 255), line=3)
    flag_pole(c, 90, 246, 130, korea_flag)


# ================================================================== Pháp

def _paris_scene(c, mood):
    ground(c, mood)
    cx, base = 280, GROUND_Y
    tower = (150, 120, 104, 255)
    P(c, [(cx - 64, base), (cx - 16, 108), (cx + 16, 108), (cx + 64, base), (cx + 38, base), (cx, 172), (cx - 38, base)], tower)
    R(c, (cx - 38, 168, cx + 38, 178), tower, r=3)
    P(c, [(cx - 16, 110), (cx - 6, 40), (cx + 6, 40), (cx + 16, 110)], tower)
    R(c, (cx - 18, 104, cx + 18, 112), tower, r=3)
    L(c, [(cx, 40), (cx, 24)], BROWN, 3)
    for x in (380, 420):
        O(c, (x - 20, 196, x + 20, 236), GREEN_DARK)


def _nice_scene(c, mood):
    sea(c, mood, 210)
    c.d.rectangle(c._box((0, 246, W, H)), fill=(220, 214, 204, 255))
    # The Promenade des Anglais: blue chairs and palms by the Baie des Anges.
    for x in (330, 420):
        palm(c, x, 246, 100, 8)
    for x in (150, 210, 270):
        R(c, (x, 226, x + 34, 238), (90, 150, 220, 255), r=4, line=3)
        R(c, (x + 24, 206, x + 34, 238), (90, 150, 220, 255), r=4, line=3)
    for x, y in ((120, 214), (200, 220)):
        c.d.line(c._box((x, y, x + 40, y)), fill=WHITE, width=3 * c_ss(c))


def lyon(c, mood):
    ground(c, mood, 250, colour=(214, 204, 192, 255))
    # A French café: the striped awning, a table outside, a croissant and a baguette.
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
    R(c, (cx + 82, 214, cx + 126, 222), (90, 70, 60, 255), r=3, line=3)
    L(c, [(cx + 104, 222), (cx + 104, 250)], BROWN, 4)
    P(c, [(cx + 86, 214), (cx + 98, 196), (cx + 112, 196), (cx + 124, 214)], GOLD, line=3)
    L(c, [(cx - 150, 250), (cx - 110, 170)], BROWN, 18)
    L(c, [(cx - 150, 250), (cx - 110, 170)], (226, 172, 104, 255), 12)
    tricolour_bunting(c)


def _provence_scene(c, mood):
    ground(c, mood, 200, colour=LAVENDER)
    # Rows of lavender running to the horizon, a stone farmhouse at the end.
    for i in range(7):
        x = -60 + i * 90
        c.d.line(c._box((x, H, 240 + (x - 240) * 0.18, 200)), fill=(130, 100, 180, 255), width=10 * c_ss(c))
    R(c, (320, 150, 420, 202), STONE)
    P(c, [(312, 152), (428, 152), (400, 128), (340, 128)], (210, 130, 100, 255))
    R(c, (340, 166, 356, 186), (150, 190, 220, 255), r=3, line=3)
    R(c, (376, 168, 396, 202), (150, 110, 90, 255), r=3, line=3)
    O(c, (110, 50, 160, 100), GOLD, line=3)


def _bordeaux_scene(c, mood):
    ground(c, mood, 210)
    # Vineyard rows in front of a château with pointed turrets.
    for y in range(222, H, 18):
        c.d.line(c._box((0, y, W, y)), fill=GREEN_DARK, width=8 * c_ss(c))
        for x in range(10, W, 26):
            c.d.ellipse(c._box((x - 4, y - 8, x + 4, y)), fill=(120, 70, 120, 255))
    cx = 290
    R(c, (cx - 70, 130, cx + 70, 210), CREAM)
    for x in (cx - 82, cx + 58):
        R(c, (x, 110, x + 24, 210), CREAM)
        P(c, [(x - 6, 112), (x + 30, 112), (x + 12, 74)], SLATE)
    P(c, [(cx - 76, 132), (cx + 76, 132), (cx + 50, 108), (cx - 50, 108)], SLATE)
    for x in (cx - 46, cx - 12, cx + 22):
        R(c, (x, 150, x + 18, 180), (150, 190, 220, 255), r=3, line=3)


# ================================================================== Ai Cập

def cairo(c, mood):
    ground(c, mood, 244, colour=SAND)
    far_pyramids(c, 222)
    # A camel caravan crossing the desert below the pyramids.
    for x, s in ((210, 1.0), (320, 0.85), (410, 0.7)):
        camel(c, x, 244 - (1 - s) * 10, s)
    palm(c, 70, 244, 100, 8)


def giza(c, mood):
    ground(c, mood, GROUND_Y, colour=SAND)
    for x, s in ((300, 1.0), (410, 0.72), (190, 0.56)):
        w, h = 180 * s, 140 * s
        P(c, [(x - w / 2, GROUND_Y), (x, GROUND_Y - h), (x + w / 2, GROUND_Y)], SAND)
        c.d.polygon([(xy[0] * c_ss(c), xy[1] * c_ss(c)) for xy in ((x, GROUND_Y - h), (x + w / 2, GROUND_Y), (x + w / 6, GROUND_Y))], fill=SAND_DARK)
    # The Sphinx lying in front.
    sx = 120
    R(c, (sx, 214, sx + 90, 240), SAND_DARK, r=8)
    P(c, [(sx + 60, 214), (sx + 64, 176), (sx + 92, 176), (sx + 96, 214)], SAND_DARK)
    R(c, (sx + 68, 180, sx + 88, 204), SAND, r=6, line=3)


def luxor(c, mood):
    ground(c, mood, 248, colour=SAND)
    far_pyramids(c, 226)
    # The golden mask of a pharaoh: striped headdress, a calm face, the plaited beard.
    cx = 300
    blue = (60, 90, 170, 255)
    P(c, [(cx - 90, 230), (cx - 70, 80), (cx, 56), (cx + 70, 80), (cx + 90, 230)], GOLD)
    for k in range(1, 7):
        y = 80 + k * 22
        c.d.line(c._box((cx - 74 - k * 2, y, cx - 44, y)), fill=blue, width=7 * c_ss(c))
        c.d.line(c._box((cx + 44, y, cx + 74 + k * 2, y)), fill=blue, width=7 * c_ss(c))
    O(c, (cx - 44, 86, cx + 44, 196), GOLD)
    for ex in (cx - 18, cx + 18):
        P(c, [(ex - 14, 128), (ex + 14, 128), (ex + 6, 136), (ex - 10, 136)], WHITE, line=3)
        c.d.ellipse(c._box((ex - 4, 128, ex + 4, 136)), fill=BROWN)
    R(c, (cx - 10, 196, cx + 10, 240), blue, r=4)
    R(c, (cx - 50, 200, cx + 50, 214), blue, r=4)


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
    c.d.rectangle(c._box((180, 228, 420, 250)), fill=STONE_DARK)
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
    P(c, [(240, 244), (252, 160), (264, 160), (276, 244)], (240, 140, 100, 255))
    c.d.line(c._box((258, 166, 258, 240)), fill=WHITE, width=3 * c_ss(c))


def _los_angeles_scene(c, mood):
    # The Hollywood sign on the hills, palms along the boulevard.
    c.d.polygon([(xy[0] * c_ss(c), xy[1] * c_ss(c)) for xy in ((0, 250), (60, 170), (200, 120), (340, 140), (480, 110), (480, 250))], fill=(170, 150, 110, 255))
    ground(c, mood, 250, colour=(196, 188, 176, 255))
    for i, ch in enumerate("HOLLYWOOD"):
        x = 140 + i * 30
        y = 142 + 6 * math.sin(i * 0.8)
        R(c, (x, y, x + 24, y + 34), WHITE, r=2, line=2)
        c.d.text(((x + 12) * c_ss(c), (y + 18) * c_ss(c)), ch, fill=BROWN, anchor="mm", font=font_small(c))
    for x in (60, 410, 450):
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
    "da_lat": da_lat, "hoi_an": hoi_an, "ha_long": ha_long, "hue": hue, "saigon": sai_gon,
    "tokyo": tokyo, "kyoto": kyoto, "osaka": osaka, "sapporo": sapporo, "okinawa": okinawa,
    "seoul": seoul, "busan": busan, "jeju": jeju, "gyeongju": gyeongju, "incheon": incheon,
    "paris": paris, "nice": nice, "lyon": lyon, "provence": provence, "bordeaux": bordeaux,
    "cairo": cairo, "giza": giza, "luxor": luxor, "aswan": aswan, "alexandria": alexandria,
    "new_york": new_york, "san_francisco": san_francisco, "hawaii": hawaii, "los_angeles": los_angeles,
    "chicago": chicago,
}
