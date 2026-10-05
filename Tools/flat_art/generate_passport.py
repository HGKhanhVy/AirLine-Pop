"""Generates the passport: one map page per country, plus the small pieces laid over it.

Each page is the country drawn flat on a sea panel, projected from real longitude and
latitude so the cities sit where they really are. Cities that crowd each other are pushed
apart for their markers, with a thin leader back to the true spot. The page positions go
to passport.json for the Unity installer; the markers themselves (landmark pins, visa
stamps, route dots, the plane) are drawn by the game over this picture.

Run generate_postcards.py first: this reads its routes.json and draws the pins from the
same landmark scenes.

    python Tools/flat_art/generate_passport.py
"""
import json
import math
import os

from PIL import Image, ImageDraw

from generate_home import BROWN, CORAL, CREAM, SS, WHITE, Canvas, font

ROOT = os.path.join(os.path.dirname(__file__), "..", "..")
OUT = os.path.join(ROOT, "Assets", "_Game", "Art", "Passport")
ROUTES = os.path.join(ROOT, "Assets", "_Game", "Art", "Postcards", "routes.json")
UIKIT = os.path.join(ROOT, "Assets", "_Game", "Art", "UIKit")

PAGE_W, PAGE_H = 900, 1200
MARGIN = 90
MARKER_GAP = 138  # how close two city markers may sit, in page pixels

SEA = (206, 232, 244, 255)
SEA_LINE = (178, 214, 232, 255)
LAND = (196, 228, 172, 255)
LAND_DARK = (172, 212, 150, 255)
NILE = (120, 184, 222, 255)
GOLD = (246, 196, 72, 255)

# Outlines as (longitude, latitude), traced coarsely: the look is a hand-drawn map, not
# an atlas. A shape may carry an inset shift, used to bring Hawaii next to the mainland.
COUNTRIES = {
    "vietnam": {
        "shapes": [
            [(102.1, 22.4), (103.3, 22.8), (104.0, 22.8), (104.8, 23.2), (105.5, 23.3), (106.7, 22.8), (106.6, 22.3),
             (107.4, 21.9), (108.0, 21.5), (107.0, 20.9), (106.6, 20.3), (105.9, 19.7), (105.7, 19.0), (106.5, 18.0),
             (107.1, 17.1), (108.0, 16.3), (108.8, 15.4), (109.2, 13.8), (109.3, 12.4), (109.1, 11.6), (108.6, 11.1),
             (107.6, 10.5), (106.8, 10.3), (106.4, 9.6), (105.2, 8.6), (104.8, 8.6), (104.8, 9.6), (104.5, 10.4),
             (105.1, 10.9), (106.2, 11.0), (106.4, 11.9), (107.5, 12.3), (107.6, 13.5), (107.5, 14.6), (107.0, 15.3),
             (107.4, 16.0), (106.6, 16.6), (105.6, 17.8), (105.1, 18.6), (104.0, 19.3), (104.2, 19.8), (104.9, 20.2),
             (104.4, 20.8), (103.2, 20.8), (102.7, 21.6)],
            [(103.85, 10.45), (104.08, 10.38), (104.05, 10.0), (103.95, 10.05)],
        ],
        "cities": {
            "hanoi": (105.85, 21.03), "ha_long": (107.08, 20.95), "ninh_binh": (105.97, 20.25), "sa_pa": (103.84, 22.34),
            "phong_nha": (106.28, 17.59), "hue": (107.59, 16.46), "da_nang": (108.2, 16.05), "hoi_an": (108.33, 15.88),
            "quy_nhon": (109.22, 13.78), "nha_trang": (109.19, 12.24), "da_lat": (108.44, 11.94), "mui_ne": (108.29, 10.93),
            "vung_tau": (107.08, 10.35), "saigon": (106.7, 10.78), "phu_quoc": (103.96, 10.23), "can_tho": (105.78, 10.03),
        },
    },
    "japan": {
        "shapes": [
            [(140.0, 41.4), (141.2, 41.8), (141.7, 42.6), (143.3, 42.0), (145.5, 43.3), (145.3, 44.3), (142.0, 45.5),
             (141.6, 45.2), (141.4, 43.4), (140.4, 43.3), (139.9, 42.6)],
            [(141.4, 41.4), (141.9, 40.0), (141.6, 38.3), (140.9, 38.1), (141.0, 37.0), (140.6, 36.1), (140.8, 35.7),
             (139.8, 35.0), (139.2, 35.2), (138.8, 34.6), (137.3, 34.6), (136.8, 34.3), (136.0, 33.5), (135.1, 33.8),
             (135.3, 34.6), (134.0, 34.6), (132.4, 34.2), (131.0, 34.0), (131.0, 34.4), (132.6, 35.4), (133.5, 35.5),
             (135.6, 35.6), (136.0, 36.2), (136.8, 37.3), (137.4, 37.4), (137.0, 36.8), (138.6, 37.6), (139.5, 38.4),
             (140.0, 39.5), (140.0, 40.7), (140.4, 41.2)],
            [(132.6, 33.0), (132.9, 34.0), (134.2, 34.3), (134.7, 33.8), (134.2, 33.3), (133.0, 32.7)],
            [(130.9, 34.0), (131.9, 33.3), (131.4, 31.4), (130.7, 31.0), (130.2, 31.4), (129.7, 32.6), (129.6, 33.3),
             (130.3, 33.6)],
            [(127.6, 26.1), (127.9, 26.5), (128.3, 26.85), (128.0, 26.6), (127.7, 26.2)],
        ],
        "cities": {
            "tokyo": (139.69, 35.68), "kyoto": (135.77, 35.01), "osaka": (135.5, 34.69), "sapporo": (141.35, 43.06),
            "okinawa": (127.8, 26.33),
        },
    },
    "korea": {
        "shapes": [
            [(126.1, 37.75), (126.7, 37.95), (127.2, 38.3), (128.1, 38.3), (128.4, 38.6), (129.4, 37.1), (129.5, 36.0),
             (129.3, 35.3), (128.6, 35.0), (127.6, 34.7), (126.6, 34.3), (126.3, 34.6), (126.5, 35.1), (126.4, 36.0),
             (126.6, 36.9)],
            [(126.15, 33.4), (126.4, 33.55), (126.8, 33.55), (126.95, 33.4), (126.7, 33.22), (126.3, 33.22)],
        ],
        "cities": {
            "seoul": (126.98, 37.57), "busan": (129.08, 35.18), "jeju": (126.53, 33.4), "gyeongju": (129.22, 35.86),
            "incheon": (126.44, 37.46),
        },
    },
    "france": {
        "shapes": [
            [(2.5, 51.1), (4.2, 49.95), (5.8, 49.5), (8.2, 49.0), (7.6, 47.6), (7.0, 47.4), (6.1, 46.2), (7.0, 45.9),
             (6.6, 45.1), (7.0, 44.2), (7.5, 43.8), (6.6, 43.1), (4.6, 43.4), (3.1, 43.1), (3.1, 42.4), (1.7, 42.5),
             (-1.8, 43.4), (-1.2, 44.6), (-1.2, 46.2), (-2.2, 47.1), (-4.7, 48.0), (-4.6, 48.6), (-1.9, 48.7),
             (-1.4, 48.6), (-1.6, 49.7), (0.2, 49.5), (1.6, 50.2)],
            [(9.4, 43.0), (9.55, 42.2), (9.2, 41.4), (8.6, 41.9), (8.7, 42.6)],
        ],
        "cities": {
            "paris": (2.35, 48.86), "nice": (7.27, 43.7), "lyon": (4.84, 45.76), "provence": (5.98, 43.84),
            "bordeaux": (-0.58, 44.84),
        },
    },
    "egypt": {
        "shapes": [
            [(25.0, 31.6), (27.3, 31.4), (29.1, 30.8), (30.6, 31.5), (32.2, 31.3), (34.2, 31.3), (34.9, 29.5),
             (34.3, 28.0), (33.6, 27.9), (32.6, 29.9), (32.4, 29.6), (33.6, 27.6), (35.6, 23.9), (35.6, 23.1),
             (36.9, 22.0), (25.0, 22.0)],
        ],
        "river": [(31.0, 31.4), (31.2, 30.0), (31.0, 28.5), (30.8, 27.5), (31.6, 26.5), (32.7, 25.7), (32.9, 24.1), (32.4, 22.1)],
        "cities": {
            "cairo": (31.24, 30.04), "giza": (31.13, 29.98), "luxor": (32.64, 25.69), "aswan": (32.9, 24.09),
            "alexandria": (29.92, 31.2),
        },
    },
    "usa": {
        "shapes": [
            [(-124.7, 48.4), (-95.2, 49.0), (-89.5, 48.0), (-82.5, 45.3), (-82.4, 42.0), (-79.0, 43.3), (-75.0, 45.0),
             (-71.5, 45.0), (-69.2, 47.4), (-67.0, 44.9), (-70.0, 43.6), (-70.6, 41.7), (-74.0, 40.6), (-75.5, 39.0),
             (-76.0, 37.0), (-75.5, 35.3), (-78.0, 33.8), (-81.0, 31.5), (-80.0, 26.8), (-80.6, 25.2), (-81.8, 26.0),
             (-82.8, 28.0), (-84.0, 30.0), (-86.5, 30.4), (-89.5, 30.2), (-89.2, 29.0), (-91.0, 29.2), (-94.0, 29.6),
             (-97.2, 27.6), (-97.4, 25.9), (-99.2, 26.5), (-101.4, 29.8), (-103.3, 29.0), (-104.7, 29.9),
             (-106.5, 31.8), (-108.2, 31.3), (-111.1, 31.3), (-114.8, 32.5), (-117.1, 32.5), (-118.5, 34.0),
             (-120.6, 34.6), (-122.5, 37.5), (-124.2, 40.4), (-124.6, 42.8), (-124.0, 46.3)],
            ([(-159.8, 22.2), (-159.3, 22.2), (-159.3, 21.9), (-159.8, 21.9)], "hawaii"),
            ([(-158.3, 21.7), (-157.6, 21.7), (-157.6, 21.25), (-158.3, 21.25)], "hawaii"),
            ([(-157.0, 21.2), (-156.0, 21.0), (-155.9, 20.6), (-156.5, 20.6)], "hawaii"),
            ([(-155.9, 20.2), (-155.0, 19.9), (-154.8, 19.5), (-155.6, 18.9), (-156.0, 19.7)], "hawaii"),
        ],
        "insets": {"hawaii": (40.0, 6.0)},
        "cities": {
            "new_york": (-74.0, 40.71), "san_francisco": (-122.42, 37.77), "hawaii": ((-157.0, 20.6), "hawaii"),
            "los_angeles": (-118.24, 34.05), "chicago": (-87.63, 41.88),
        },
    },
}


def unpack(item):
    """A shape or city is either bare coordinates or (coordinates, inset name)."""
    if isinstance(item, tuple) and len(item) == 2 and isinstance(item[1], str):
        return item[0], item[1]
    return item, None


def shifted(points, inset, insets):
    dx, dy = insets.get(inset, (0.0, 0.0)) if inset else (0.0, 0.0)
    return [(lon + dx, lat + dy) for lon, lat in points]


class Projection:
    """Equirectangular around the country's middle latitude, fitted into the page."""

    def __init__(self, points):
        lat0 = sum(p[1] for p in points) / len(points)
        self.k = math.cos(math.radians(lat0))
        xs = [p[0] * self.k for p in points]
        ys = [-p[1] for p in points]
        x0, x1, y0, y1 = min(xs), max(xs), min(ys), max(ys)
        self.scale = min((PAGE_W - 2 * MARGIN) / (x1 - x0), (PAGE_H - 2 * MARGIN) / (y1 - y0))
        self.ox = (PAGE_W - (x1 - x0) * self.scale) / 2 - x0 * self.scale
        self.oy = (PAGE_H - (y1 - y0) * self.scale) / 2 - y0 * self.scale

    def __call__(self, lon, lat):
        return lon * self.k * self.scale + self.ox, -lat * self.scale + self.oy


def spread(points, gap, iterations=400):
    """Pushes markers apart until none sit closer than the gap, each pulled gently home."""
    home = list(points)
    pos = [list(p) for p in points]
    for _ in range(iterations):
        moved = False
        for i in range(len(pos)):
            for j in range(i + 1, len(pos)):
                dx, dy = pos[j][0] - pos[i][0], pos[j][1] - pos[i][1]
                d = math.hypot(dx, dy)
                if d < gap:
                    if d < 1e-3:
                        dx, dy, d = 1.0, 0.0, 1.0
                    push = (gap - d) / 2 * 0.5
                    pos[i][0] -= dx / d * push
                    pos[i][1] -= dy / d * push
                    pos[j][0] += dx / d * push
                    pos[j][1] += dy / d * push
                    moved = True
        for p, h in zip(pos, home):
            p[0] += (h[0] - p[0]) * 0.01
            p[1] += (h[1] - p[1]) * 0.01
            p[0] = min(max(p[0], 70), PAGE_W - 70)
            p[1] = min(max(p[1], 70), PAGE_H - 90)
        if not moved:
            break
    return [tuple(p) for p in pos]


def sea_panel(c):
    c.rect((14, 14, PAGE_W - 14, PAGE_H - 14), 36, SEA, line=5, puffy=False)
    for i in range(14):
        y = 80 + i * 80
        x = 60 + (i * 137) % 600
        c.d.arc(c._box((x, y, x + 60, y + 24)), 200, 340, fill=SEA_LINE, width=4 * SS)
        c.d.arc(c._box((x + 40, y, x + 100, y + 24)), 200, 340, fill=SEA_LINE, width=4 * SS)


def compass(c, x, y):
    for ang, length, col in ((-90, 46, CORAL), (90, 46, CREAM), (0, 30, CREAM), (180, 30, CREAM)):
        r = math.radians(ang)
        tip = (x + length * math.cos(r), y + length * math.sin(r))
        side = math.radians(ang + 90)
        a = (x + 9 * math.cos(side), y + 9 * math.sin(side))
        b = (x - 9 * math.cos(side), y - 9 * math.sin(side))
        c.poly([tip, a, b], col, line=3, puffy=False)
    c.oval((x - 7, y - 7, x + 7, y + 7), GOLD, line=3, puffy=False)


def page(region, data, order):
    insets = data.get("insets", {})
    raw = [unpack(s) for s in data["shapes"]]
    shapes = [shifted(points, inset, insets) for points, inset in raw]
    cities = {}
    for cid, item in data["cities"].items():
        coords, inset = unpack(item)
        cities[cid] = shifted([coords], inset, insets)[0]
    proj = Projection([p for s in shapes for p in s] + list(cities.values()))

    c = Canvas(PAGE_W, PAGE_H)
    sea_panel(c)
    # An inset (Hawaii) gets a frame, so it does not read as part of the mainland.
    for inset in insets:
        box = [proj(lon, lat) for (_, name), s in zip(raw, shapes) if name == inset for lon, lat in s]
        xs, ys = [p[0] for p in box], [p[1] for p in box]
        frame = (min(xs) - 30, min(ys) - 30, max(xs) + 30, max(ys) + 30)
        c.d.rounded_rectangle(c._box(frame), radius=16 * SS, outline=BROWN, width=3 * SS)
    for s in shapes:
        c.poly([proj(lon, lat) for lon, lat in s], LAND, line=5, puffy=False)
    if "river" in data:
        c.line([proj(lon, lat) for lon, lat in data["river"]], NILE, 8)
    compass(c, PAGE_W - 92, PAGE_H - 110)

    true_spots = [proj(*cities[cid]) for cid in order]
    markers = spread(true_spots, MARKER_GAP)
    for (tx, ty), (mx, my) in zip(true_spots, markers):
        if math.hypot(mx - tx, my - ty) > 24:
            c.d.line(c._box((tx, ty, mx, my)), fill=(100, 56, 53, 140), width=3 * SS)
        c.oval((tx - 6, ty - 6, tx + 6, ty + 6), CORAL, line=3, puffy=False)

    first = markers[0]
    toward = (MARGIN - first[0], MARGIN - first[1])
    d = math.hypot(*toward) or 1.0
    reach = min(170.0, d)
    entry = (first[0] + toward[0] / d * reach, first[1] + toward[1] / d * reach)

    name = "passport_" + region
    c.img.resize((PAGE_W, PAGE_H), Image.LANCZOS).save(os.path.join(OUT, name + ".png"))
    return {
        "region": region,
        "map": name,
        "entry": [entry[0] / PAGE_W, entry[1] / PAGE_H],
        "cities": [{"id": cid, "uv": [m[0] / PAGE_W, m[1] / PAGE_H]} for cid, m in zip(order, markers)],
    }


def save(c, name, folder=OUT, scale=1):
    c.img.resize((c.w * scale, c.h * scale), Image.LANCZOS).save(os.path.join(folder, name + ".png"))


def route_dot():
    c = Canvas(32, 32)
    c.d.ellipse(c._box((2, 2, 30, 30)), fill=WHITE)
    save(c, "route_dot")


def plane():
    """The airline's plane seen from above, nose to the right: the game turns it along the route."""
    s = 96
    c = Canvas(s, s)
    body = [(88, 48), (78, 41), (56, 40), (40, 14), (30, 14), (38, 40), (18, 40), (10, 30), (4, 30), (8, 48),
            (4, 66), (10, 66), (18, 56), (38, 56), (30, 82), (40, 82), (56, 56), (78, 55)]
    c.poly(body, CORAL, line=4)
    c.oval((66, 43, 76, 53), CREAM, line=2, puffy=False)
    save(c, "passport_plane")


def visa_stamp():
    """A round visa stamp in white, so the game can ink it in each country's colour."""
    s = 160
    c = Canvas(s, s)
    c.d.ellipse(c._box((8, 8, s - 8, s - 8)), outline=WHITE, width=8 * SS)
    c.d.ellipse(c._box((22, 22, s - 22, s - 22)), outline=WHITE, width=4 * SS)
    c.d.text((s / 2 * SS, 70 * SS), "VISA", font=font(34), fill=WHITE, anchor="mm")
    c.d.line(c._box((40, 92, s - 40, 92)), fill=WHITE, width=4 * SS)
    pl = [(80, 100), (84, 110), (98, 114), (84, 118), (83, 128), (88, 132), (72, 132), (77, 128), (76, 118), (62, 114), (76, 110)]
    c.d.polygon([(x * SS, y * SS) for x, y in pl], fill=WHITE)
    save(c, "visa_stamp")


def passport_icon():
    """Home button icon: a passport booklet with the globe on its cover."""
    s = 128
    c = Canvas(s, s)
    c.rect((26, 12, 102, 116), 10, (64, 92, 150, 255))
    c.d.line(c._box((36, 12, 36, 116)), fill=(44, 66, 116, 255), width=4 * SS)
    c.oval((46, 36, 90, 80), GOLD, line=3, puffy=False)
    c.d.line(c._box((46, 58, 90, 58)), fill=BROWN, width=3 * SS)
    c.d.arc(c._box((56, 36, 80, 80)), 0, 360, fill=BROWN, width=3 * SS)
    c.d.line(c._box((50, 96, 86, 96)), fill=GOLD, width=5 * SS)
    save(c, "icon_passport", folder=UIKIT, scale=2)


def main():
    os.makedirs(OUT, exist_ok=True)
    with open(ROUTES, encoding="utf8") as f:
        routes = json.load(f)
    regions = []
    for route in routes:
        if not regions or regions[-1][0] != route["region"]:
            regions.append((route["region"], []))
        regions[-1][1].append(route["id"])
    pages = [page(region, COUNTRIES[region], order) for region, order in regions]
    route_dot()
    plane()
    visa_stamp()
    passport_icon()
    with open(os.path.join(OUT, "passport.json"), "w", encoding="utf8") as f:
        json.dump(pages, f, indent=1)
    print("passport pages written:", len(pages))


if __name__ == "__main__":
    main()
