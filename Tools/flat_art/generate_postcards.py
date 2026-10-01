"""Generates the destination postcards and the route list the Unity installer reads.

Six regions of five cities, one postcard each. Every city shows its own landmark, what a
traveller would send home from there (postcard_landmarks.py), at its own time of day.
Same flat sticker language as the rest of the game: brown outline, one hard shade.

    python Tools/flat_art/generate_postcards.py
"""
import json
import os

from PIL import Image, ImageDraw

from generate_home import BROWN, CORAL, CREAM, SS, WHITE, Canvas, font
from postcard_landmarks import LANDMARKS

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Art", "Postcards")
W, H = 480, 320
INSET = 16

# Region, a short region key, then five cities. Order is flying order; keep in sync with nothing:
# the installer reads routes.json written below.
# Each card is drawn twice, captioned in English and in Vietnamese; cities only change
# their name where Vietnamese writes it differently.
ROUTES = [
    (("Vietnam", "Việt Nam"), "vietnam", [("Da Lat", "Đà Lạt"), ("Hoi An", "Hội An"), ("Ha Long", "Hạ Long"),
                                          ("Hue", "Huế"), ("Saigon", "Sài Gòn")]),
    (("Japan", "Nhật Bản"), "japan", ["Tokyo", "Kyoto", "Osaka", "Sapporo", "Okinawa"]),
    (("Korea", "Hàn Quốc"), "korea", ["Seoul", "Busan", "Jeju", "Gyeongju", "Incheon"]),
    (("France", "Pháp"), "france", ["Paris", "Nice", "Lyon", "Provence", "Bordeaux"]),
    (("Egypt", "Ai Cập"), "egypt", ["Cairo", "Giza", "Luxor", "Aswan", "Alexandria"]),
    (("USA", "Mỹ"), "usa", ["New York", "San Francisco", "Hawaii", "Los Angeles", "Chicago"]),
]

# Five moods, one per city slot: sky top, sky bottom, ground, far hills, whether stars show.
MOODS = [
    ((132, 198, 236), (214, 238, 250), (150, 206, 130), (182, 224, 170), False),   # morning
    ((244, 170, 170), (252, 222, 214), (150, 196, 140), (200, 214, 180), False),   # dawn pink
    ((150, 214, 226), (224, 246, 240), (132, 200, 150), (176, 226, 196), False),   # mint noon
    ((176, 150, 214), (246, 176, 170), (120, 170, 132), (170, 170, 190), False),   # dusk lilac
    ((44, 58, 104), (96, 104, 160), (70, 104, 96), (92, 110, 140), True),          # night
]

ASCII = {"Đ": "D", "đ": "d"}


def slug(name):
    import unicodedata
    text = "".join(ASCII.get(ch, ch) for ch in name)
    text = unicodedata.normalize("NFD", text)
    text = "".join(ch for ch in text if unicodedata.category(ch) != "Mn")
    return text.lower().replace(" ", "_")


def picture_mask(c):
    m = Image.new("L", c.img.size, 0)
    ImageDraw.Draw(m).rounded_rectangle(c._box((INSET, INSET, W - INSET, H - INSET)), radius=14 * SS, fill=255)
    return m


def sky(layer, mood):
    top, bottom = mood[0], mood[1]
    d = ImageDraw.Draw(layer)
    for y in range(H):
        t = y / H
        col = tuple(round(top[i] + (bottom[i] - top[i]) * t) for i in range(3)) + (255,)
        d.line([(0, y * SS), (W * SS, y * SS)], fill=col, width=SS)
    if mood[4]:
        for i in range(40):
            x = (i * 97) % (W - 40) + 20
            y = (i * 53) % 150 + 20
            r = 1.5 + (i % 3)
            d.ellipse([(x - r) * SS, (y - r) * SS, (x + r) * SS, (y + r) * SS], fill=(255, 250, 230, 255))
        d.ellipse([(W - 120) * SS, 40 * SS, (W - 84) * SS, 76 * SS], fill=(255, 248, 226, 255))
    else:
        for cx, cy, r in ((90, 70, 20), (116, 60, 28), (146, 72, 20), (330, 50, 16), (352, 42, 22), (376, 52, 16)):
            d.ellipse([(cx - r) * SS, (cy - r) * SS, (cx + r) * SS, (cy + r) * SS], fill=(255, 255, 255, 230))


def stamp(c):
    x0, y0 = W - INSET - 78, INSET + 12
    edge = Image.new("L", c.img.size, 0)
    ed = ImageDraw.Draw(edge)
    ed.rectangle(c._box((x0, y0, x0 + 62, y0 + 72)), fill=255)
    for i in range(7):
        for (px, py) in ((x0 + 4 + i * 9, y0), (x0 + 4 + i * 9, y0 + 72)):
            ed.ellipse(c._box((px - 3, py - 3, px + 3, py + 3)), fill=0)
    for i in range(8):
        for (px, py) in ((x0, y0 + 4 + i * 9), (x0 + 62, y0 + 4 + i * 9)):
            ed.ellipse(c._box((px - 3, py - 3, px + 3, py + 3)), fill=0)
    layer = Image.new("RGBA", c.img.size, CREAM[:3] + (0,))
    layer.putalpha(edge)
    c.img.alpha_composite(layer)
    c.rect((x0 + 8, y0 + 8, x0 + 54, y0 + 64), 4, CORAL, line=2, puffy=False)
    pl = [(x0 + 31, y0 + 18), (x0 + 35, y0 + 30), (x0 + 48, y0 + 36), (x0 + 35, y0 + 40), (x0 + 34, y0 + 52),
          (x0 + 38, y0 + 56), (x0 + 24, y0 + 56), (x0 + 28, y0 + 52), (x0 + 27, y0 + 40), (x0 + 14, y0 + 36), (x0 + 27, y0 + 30)]
    c.d.polygon([(x * SS, y * SS) for x, y in pl], fill=CREAM)
    # Postmark rings over the stamp's corner.
    ImageDraw.Draw(c.img).ellipse(c._box((x0 - 34, y0 + 30, x0 + 26, y0 + 90)), outline=(100, 56, 53, 120), width=3 * SS)


def caption(c, city, region):
    x, y = INSET + 16, H - INSET - 78
    for dx in range(-3, 4):
        for dy in range(-3, 4):
            if dx * dx + dy * dy <= 9:
                c.d.text(((x + dx) * SS, (y + dy) * SS), city, font=font(40), fill=BROWN)
    c.d.text((x * SS, y * SS), city, font=font(40), fill=CREAM)
    c.d.text(((x + 2) * SS, (y + 48) * SS), region.upper(), font=font(16), fill=(255, 255, 255, 230))


def postcard(city_id, region, city, mood_index, suffix):
    mood = MOODS[mood_index]
    c = Canvas(W, H)
    c.rect((6, 6, W - 6, H - 6), 20, CREAM, puffy=False)

    art = Canvas(W, H)
    sky(art.img, mood)
    LANDMARKS[city_id](art, mood)
    mask = picture_mask(c)
    framed = Image.new("RGBA", c.img.size, (0, 0, 0, 0))
    framed.paste(art.img, (0, 0), mask)
    c.img.alpha_composite(framed)
    ImageDraw.Draw(c.img).rounded_rectangle(c._box((INSET, INSET, W - INSET, H - INSET)), radius=14 * SS,
                                            outline=BROWN, width=4 * SS)
    stamp(c)
    caption(c, city, region)
    name = "postcard_" + city_id + suffix
    c.save_to(OUT, name)
    return name


def patch_canvas():
    def save_to(self, folder, name, scale=1):
        os.makedirs(folder, exist_ok=True)
        size = (self.w * scale, self.h * scale)
        self.img.resize(size, Image.LANCZOS).save(os.path.join(folder, name + ".png"))
    Canvas.save_to = save_to


def icon():
    """HUD / Home icon: a small postcard with a stamp."""
    s = 128
    c = Canvas(s, s)
    c.rect((10, 26, s - 10, s - 26), 10, CREAM)
    c.rect((74, 36, s - 22, 70), 4, CORAL, line=3)
    for y in (78, 90):
        c.d.line(c._box((22, y, 62, y)), fill=BROWN, width=4 * SS)
    c.d.line(c._box((22, 42, 60, 42)), fill=BROWN, width=4 * SS)
    c.d.line(c._box((22, 54, 52, 54)), fill=BROWN, width=4 * SS)
    ui = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Art", "UIKit")
    c.save_to(ui, "icon_postcard", scale=2)


def main():
    patch_canvas()
    routes = []
    for (region_en, region_vi), region_key, cities in ROUTES:
        for i, city in enumerate(cities):
            city_en, city_vi = city if isinstance(city, tuple) else (city, city)
            city_id = slug(city_en)
            routes.append({
                "id": city_id, "name": city_en, "name_vi": city_vi,
                "region": region_key, "region_name": region_en, "region_name_vi": region_vi,
                "postcard": postcard(city_id, region_en, city_en, i, ""),
                "postcard_vi": postcard(city_id, region_vi, city_vi, i, "_vi"),
            })
    icon()
    with open(os.path.join(OUT, "routes.json"), "w", encoding="utf8") as f:
        json.dump(routes, f, ensure_ascii=False, indent=1)
    print("postcards written:", len(routes))


if __name__ == "__main__":
    main()
