"""Generates the destination postcards and the route list the Unity installer reads.

Six regions (Vietnam has sixteen cities, the others five), one postcard per city. Every city shows its own landmark, what a
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
PIN_OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Art", "Passport")
PIN = 128
W, H = 480, 320
INSET = 16

# Region, a short region key, then five cities. Order is flying order; keep in sync with nothing:
# the installer reads routes.json written below.
# Each card is drawn twice, captioned in English and in Vietnamese. A city is
# (name, Vietnamese name, landmark, Vietnamese landmark, airport code): the landmark is what
# the picture shows, captioned under the city together with the country. The code is the
# real IATA code of the city's airport, left empty where the city has none of its own.
ROUTES = [
    (("Vietnam", "Việt Nam"), "vietnam", [
        ("Hanoi", "Hà Nội", "Temple of Literature", "Văn Miếu", "HAN"),
        ("Ha Long", "Hạ Long", "Ha Long Bay", "Vịnh Hạ Long", "VDO"),
        ("Ninh Binh", "Ninh Bình", "Trang An", "Tràng An", ""),
        ("Sa Pa", "Sa Pa", "Rice Terraces", "Ruộng bậc thang", ""),
        ("Phong Nha", "Phong Nha", "Phong Nha Cave", "Động Phong Nha", "VDH"),
        ("Hue", "Huế", "Thien Mu Pagoda", "Chùa Thiên Mụ", "HUI"),
        ("Da Nang", "Đà Nẵng", "Golden Bridge", "Cầu Vàng", "DAD"),
        ("Hoi An", "Hội An", "Japanese Bridge", "Chùa Cầu", ""),
        ("Quy Nhon", "Quy Nhơn", "Eo Gio Cove", "Eo Gió", "UIH"),
        ("Nha Trang", "Nha Trang", "Po Nagar Towers", "Tháp Bà Ponagar", "CXR"),
        ("Da Lat", "Đà Lạt", "Da Lat Cathedral", "Nhà thờ Con Gà", "DLI"),
        ("Mui Ne", "Mũi Né", "Red Sand Dunes", "Đồi cát đỏ", ""),
        ("Vung Tau", "Vũng Tàu", "Christ of Vung Tau", "Tượng Chúa Kitô Vua", "VTG"),
        ("Saigon", "Sài Gòn", "Ben Thanh Market", "Chợ Bến Thành", "SGN"),
        ("Phu Quoc", "Phú Quốc", "Sao Beach", "Bãi Sao", "PQC"),
        ("Can Tho", "Cần Thơ", "Cai Rang Floating Market", "Chợ nổi Cái Răng", "VCA")]),
    (("Japan", "Nhật Bản"), "japan", [
        ("Tokyo", "Tokyo", "Tokyo Tower", "Tháp Tokyo", "HND"),
        ("Kyoto", "Kyoto", "Fushimi Inari Shrine", "Đền Fushimi Inari", "UKY"),
        ("Osaka", "Osaka", "Osaka Castle", "Lâu đài Osaka", "KIX"),
        ("Sapporo", "Sapporo", "Mount Yotei", "Núi Yotei", "CTS"),
        ("Okinawa", "Okinawa", "Naminoue Shrine", "Đền Naminoue", "OKA")]),
    (("Korea", "Hàn Quốc"), "korea", [
        ("Seoul", "Seoul", "Gyeongbokgung Palace", "Cung Gyeongbok", "GMP"),
        ("Busan", "Busan", "Gamcheon Village", "Làng Gamcheon", "PUS"),
        ("Jeju", "Jeju", "Seongsan Ilchulbong", "Đỉnh Seongsan Ilchulbong", "CJU"),
        ("Gyeongju", "Gyeongju", "Wolji Pond", "Hồ Wolji", ""),
        ("Incheon", "Incheon", "Incheon Airport", "Sân bay Incheon", "ICN")]),
    (("France", "Pháp"), "france", [
        ("Paris", "Paris", "Eiffel Tower", "Tháp Eiffel", "CDG"),
        ("Nice", "Nice", "Promenade des Anglais", "Đại lộ Anglais", "NCE"),
        ("Lyon", "Lyon", "Vieux Lyon", "Phố cổ Lyon", "LYS"),
        ("Provence", "Provence", "Valensole Lavender", "Đồi oải hương Valensole", "MRS"),
        ("Bordeaux", "Bordeaux", "Médoc Vineyards", "Vườn nho Médoc", "BOD")]),
    (("Egypt", "Ai Cập"), "egypt", [
        ("Cairo", "Cairo", "Muhammad Ali Mosque", "Thánh đường Muhammad Ali", "CAI"),
        ("Giza", "Giza", "Pyramids & Sphinx", "Kim tự tháp & Nhân sư", "SPX"),
        ("Luxor", "Luxor", "Valley of the Kings", "Thung lũng các Vị vua", "LXR"),
        ("Aswan", "Aswan", "Feluccas on the Nile", "Thuyền buồm sông Nile", "ASW"),
        ("Alexandria", "Alexandria", "Pharos Lighthouse", "Hải đăng Pharos", "HBE")]),
    (("USA", "Mỹ"), "usa", [
        ("New York", "New York", "Statue of Liberty", "Tượng Nữ thần Tự do", "JFK"),
        ("San Francisco", "San Francisco", "Golden Gate Bridge", "Cầu Cổng Vàng", "SFO"),
        ("Hawaii", "Hawaii", "Diamond Head", "Núi Diamond Head", "HNL"),
        ("Los Angeles", "Los Angeles", "Hollywood Sign", "Bảng chữ Hollywood", "LAX"),
        ("Chicago", "Chicago", "Willis Tower", "Tháp Willis", "ORD")]),
]

# Five moods, cycled through a region's cities: sky top, sky bottom, ground, far hills, whether stars show.
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


def caption(c, city, landmark, region):
    x, y = INSET + 16, H - INSET - 84
    for dx in range(-3, 4):
        for dy in range(-3, 4):
            if dx * dx + dy * dy <= 9:
                c.d.text(((x + dx) * SS, (y + dy) * SS), city, font=font(40), fill=BROWN)
    c.d.text((x * SS, y * SS), city, font=font(40), fill=CREAM)
    # Landmark and country on a brown pill, so they read over sea, snow or sand alike.
    label = landmark + "  ·  " + region
    small = font(15)
    left, top, right, bottom = c.d.textbbox((0, 0), label, font=small)
    pad_x, pill_y0, pill_y1 = 9, H - INSET - 32, H - INSET - 10
    pill_w = (right - left) / SS + pad_x * 2
    c.d.rounded_rectangle(c._box((x - 2, pill_y0, x - 2 + pill_w, pill_y1)), radius=11 * SS, fill=BROWN)
    c.d.text(((x - 2 + pad_x) * SS - left, ((pill_y0 + pill_y1) / 2) * SS), label, font=small, fill=CREAM, anchor="lm")


def postcard(city_id, region, city, landmark, mood_index, suffix):
    mood = MOODS[mood_index % len(MOODS)]
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
    caption(c, city, landmark, region)
    name = "postcard_" + city_id + suffix
    c.save_to(OUT, name)
    return name


def pin(city_id, mood_index):
    """The city's marker on the passport map: the middle of its postcard scene in a round frame."""
    mood = MOODS[mood_index % len(MOODS)]
    art = Canvas(W, H)
    sky(art.img, mood)
    LANDMARKS[city_id](art, mood)
    side = 250
    cx, cy = 290, 160
    crop = art.img.crop(tuple(v * SS for v in (cx - side / 2, cy - side / 2, cx + side / 2, cy + side / 2)))
    size = crop.size[0]
    ring = round(size * 0.07)
    out = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    disc = Image.new("L", (size, size), 0)
    ImageDraw.Draw(disc).ellipse((0, 0, size - 1, size - 1), fill=255)
    ImageDraw.Draw(out).ellipse((0, 0, size - 1, size - 1), fill=BROWN)
    ImageDraw.Draw(out).ellipse((ring, ring, size - 1 - ring, size - 1 - ring), fill=CREAM)
    inner = Image.new("L", (size, size), 0)
    ImageDraw.Draw(inner).ellipse((ring * 2, ring * 2, size - 1 - ring * 2, size - 1 - ring * 2), fill=255)
    out.paste(crop, (0, 0), inner)
    out.putalpha(Image.composite(disc, Image.new("L", (size, size), 0), disc))
    os.makedirs(PIN_OUT, exist_ok=True)
    name = "pin_" + city_id
    out.resize((PIN, PIN), Image.LANCZOS).save(os.path.join(PIN_OUT, name + ".png"))
    return name


STAMP_W, STAMP_H = 180, 216


def stamp_art(city_id, mood_index, code):
    """
    The city's stamp for the collection: a perforated postage stamp, the middle of its
    postcard scene in a coral frame, the airline's name and the airport code underneath.
    """
    mood = MOODS[mood_index % len(MOODS)]
    art = Canvas(W, H)
    sky(art.img, mood)
    LANDMARKS[city_id](art, mood)
    side = 230
    cx, cy = 290, 150
    crop = art.img.crop(tuple(v * SS for v in (cx - side / 2, cy - side / 2, cx + side / 2, cy + side / 2)))

    c = Canvas(STAMP_W, STAMP_H)
    edge = 10
    # Paper with its perforations punched out along every side.
    paper = Image.new("L", c.img.size, 0)
    pd = ImageDraw.Draw(paper)
    pd.rectangle(c._box((edge - 4, edge - 4, STAMP_W - edge + 4, STAMP_H - edge + 4)), fill=255)
    step, r = 15, 5
    for x in range(int(edge + 4), int(STAMP_W - edge), step):
        for y in (edge - 4, STAMP_H - edge + 4):
            pd.ellipse(c._box((x - r, y - r, x + r, y + r)), fill=0)
    for y in range(int(edge + 4), int(STAMP_H - edge), step):
        for x in (edge - 4, STAMP_W - edge + 4):
            pd.ellipse(c._box((x - r, y - r, x + r, y + r)), fill=0)
    shadow = Image.new("RGBA", c.img.size, (0, 0, 0, 0))
    shadow.paste(Image.new("RGBA", c.img.size, (100, 56, 53, 255)), (0, 0), paper)
    c.img.alpha_composite(shadow, (2 * SS, 3 * SS))
    face = Image.new("RGBA", c.img.size, (0, 0, 0, 0))
    face.paste(Image.new("RGBA", c.img.size, CREAM), (0, 0), paper)
    c.img.alpha_composite(face)

    pic = (22, 22, STAMP_W - 22, STAMP_W - 22)
    c.d.rectangle(c._box((pic[0] - 5, pic[1] - 5, pic[2] + 5, pic[3] + 5)), fill=CORAL)
    size = (int((pic[2] - pic[0]) * SS), int((pic[3] - pic[1]) * SS))
    c.img.paste(crop.resize(size, Image.LANCZOS), (int(pic[0] * SS), int(pic[1] * SS)))
    c.d.rectangle(c._box(pic), outline=BROWN, width=3 * SS)

    c.d.text((22 * SS, (STAMP_H - 26) * SS), "AIRLINE POP", font=font(13), fill=BROWN, anchor="lm")
    if code:
        c.d.text(((STAMP_W - 22) * SS, (STAMP_H - 27) * SS), code, font=font(20), fill=CORAL, anchor="rm")
    name = "stamp_" + city_id
    c.img.resize((STAMP_W, STAMP_H), Image.LANCZOS).save(os.path.join(OUT, name + ".png"))
    return name


def postmark():
    """The cancellation inked over a stamp once collected: a ring and three wavy lines."""
    w, h = 200, 150
    c = Canvas(w, h)
    ink = (100, 56, 53, 200)
    c.d.ellipse(c._box((10, 20, 120, 130)), outline=ink, width=5 * SS)
    c.d.ellipse(c._box((24, 34, 106, 116)), outline=ink, width=2 * SS)
    import math
    for k in range(3):
        y0 = 48 + k * 26
        pts = [((96 + t * 2.5) * SS, (y0 + 7 * math.sin(t / 7.0)) * SS) for t in range(0, 40)]
        c.d.line(pts, fill=ink, width=5 * SS)
    c.img.resize((w, h), Image.LANCZOS).save(os.path.join(OUT, "postmark.png"))


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
        for i, (city_en, city_vi, landmark_en, landmark_vi, code) in enumerate(cities):
            city_id = slug(city_en)
            routes.append({
                "id": city_id, "name": city_en, "name_vi": city_vi,
                "landmark": landmark_en, "landmark_vi": landmark_vi, "code": code,
                "region": region_key, "region_name": region_en, "region_name_vi": region_vi,
                "postcard": postcard(city_id, region_en, city_en, landmark_en, i, ""),
                "postcard_vi": postcard(city_id, region_vi, city_vi, landmark_vi, i, "_vi"),
                "pin": pin(city_id, i),
                "stamp": stamp_art(city_id, i, code),
            })
    icon()
    postmark()
    with open(os.path.join(OUT, "routes.json"), "w", encoding="utf8") as f:
        json.dump(routes, f, ensure_ascii=False, indent=1)
    print("postcards written:", len(routes))


if __name__ == "__main__":
    main()
