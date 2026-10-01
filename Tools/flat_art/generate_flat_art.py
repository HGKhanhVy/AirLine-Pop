"""Generates the flat 2D board art for AirLine Pop: tiles, runway, airplane, sky and clouds.

Style: matched to the Craftpix cats the passengers use: flat colour, the same dark
red-brown outline, one hard-edged shade (puff.py) and flat shadows, no gloss. Foreground pieces (tiles, airplane) carry an outline; background pieces
(sky, clouds) do not, so the board always reads in front. Everything is drawn 4x and
scaled down for clean edges. Rerun after tweaking a colour; Unity keeps the GUIDs.

    python Tools/flat_art/generate_flat_art.py
"""
import math
import os

from PIL import Image, ImageChops, ImageDraw, ImageFilter

from puff import puff

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Art", "Flat")
CATS = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Art", "FlatCats")
SS = 4

BROWN = (100, 56, 53, 255)  # the passengers' outline
WHITE = (255, 255, 255, 255)
CREAM = (255, 252, 246, 255)
# The airline's livery, shared with the plane at the stand on Home.
CORAL = (240, 124, 108, 255)
CORAL_DARK = (206, 92, 82, 255)
SKY_BLUE = (150, 206, 236, 255)
GLASS = (170, 222, 245, 255)
GREY = (120, 128, 146, 255)


def canvas(w, h):
    return Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))


def save(img, name, w, h):
    os.makedirs(OUT, exist_ok=True)
    img.resize((w, h), Image.LANCZOS).save(os.path.join(OUT, name + ".png"))


def scaled(box):
    return [v * SS for v in box]


def mask_rrect(w, h, box, r):
    m = Image.new("L", (w * SS, h * SS), 0)
    ImageDraw.Draw(m).rounded_rectangle(scaled(box), radius=r * SS, fill=255)
    return m


def fill_mask(img, mask, colour):
    # The mask (scaled by the colour's own alpha) becomes the layer's alpha. Compositing
    # against transparent black instead would grey out every soft edge.
    alpha = colour[3] if len(colour) > 3 else 255
    layer = Image.new("RGBA", img.size, colour[:3] + (0,))
    layer.putalpha(mask if alpha == 255 else mask.point(lambda v: v * alpha // 255))
    img.alpha_composite(layer)


def grow(mask, px):
    """Dilates a mask by px final pixels, which is how every outline here is made."""
    size = px * SS * 2 + 1
    return mask.filter(ImageFilter.MaxFilter(size if size % 2 else size + 1))


def outline_under(img, mask, px, colour=BROWN):
    """Returns img with a uniform outline of px pixels drawn behind the shape in mask."""
    out = Image.new("RGBA", img.size, (0, 0, 0, 0))
    fill_mask(out, grow(mask, px), colour)
    out.alpha_composite(img)
    return out


# ---------------------------------------------------------------- tiles

TILE = 256
TILE_R = 58
LIP = 22
TILE_OUTLINE = 7


def tile_face():
    """White tile silhouette, tinted at runtime by the board palette."""
    img = canvas(TILE, TILE)
    fill_mask(img, mask_rrect(TILE, TILE, (0, 0, TILE, TILE), TILE_R), WHITE)
    save(img, "tile_face", TILE, TILE)


def tile_overlay():
    """Everything on a tile that keeps its colour: lip shade, glint and outline."""
    img = canvas(TILE, TILE)
    full = mask_rrect(TILE, TILE, (0, 0, TILE, TILE), TILE_R)
    top = mask_rrect(TILE, TILE, (0, 0, TILE, TILE - LIP), TILE_R)

    # The lip: the bottom band of the tile, darkened so any tint reads as a thick block.
    lip = ImageChops.subtract(full, top)
    fill_mask(img, lip, (60, 40, 30, 60))

    # A soft seam between face and lip.
    seam = ImageChops.subtract(grow(top, 2), top)
    seam = ImageChops.multiply(seam, full)
    fill_mask(img, seam, BROWN[:3] + (70,))

    # The same puffy shading every other piece gets, painted into the overlay so it
    # works over whatever colour the face is tinted.
    puff(img, top, SS)

    # Outline: the silhouette minus an inset copy. Drawn from shapes rather than eroded,
    # because the tile touches the canvas edge and an erosion does not bite there.
    o = TILE_OUTLINE
    inner = mask_rrect(TILE, TILE, (o, o, TILE - o, TILE - o), TILE_R - o)
    ring = ImageChops.subtract(full, inner)
    fill_mask(img, ring, BROWN)
    save(img, "tile_overlay", TILE, TILE)


def tile_shadow():
    size = TILE + 64
    img = canvas(size, size)
    m = mask_rrect(size, size, (32, 44, 32 + TILE, 44 + TILE), TILE_R)
    # A flat, hard-edged shadow in the outline brown, like the one under each cat.
    fill_mask(img, m, BROWN[:3] + (60,))
    img = img.filter(ImageFilter.GaussianBlur(SS))
    save(img, "tile_shadow", size, size)


def tile_flash():
    """A white tile-shaped sheet for the connect flash."""
    img = canvas(TILE, TILE)
    fill_mask(img, mask_rrect(TILE, TILE, (0, 0, TILE, TILE - LIP), TILE_R), WHITE)
    save(img, "tile_flash", TILE, TILE)


TAXI_YELLOW = (250, 204, 72, 255)
RUNWAY = (150, 160, 184, 255)


def start_pad():
    """The start square is stand A1, as on the apron at Home: a yellow pad with the stand's
    name, and a yellow ring that pulses round it until the first move."""
    s = 256
    img = canvas(s, s)
    d = ImageDraw.Draw(img)
    d.ellipse(scaled((30, 30, s - 30, s - 30)), outline=BROWN, width=22 * SS)
    d.ellipse(scaled((36, 36, s - 36, s - 36)), outline=TAXI_YELLOW, width=10 * SS)
    save(img, "start_ring", s, s)

    img = canvas(s, s)
    d = ImageDraw.Draw(img)
    d.ellipse(scaled((62, 62, s - 62, s - 62)), fill=BROWN)
    d.ellipse(scaled((70, 70, s - 70, s - 70)), fill=TAXI_YELLOW)
    from PIL import ImageFont
    font = ImageFont.truetype(os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Art", "Fonts",
                                           "Baloo2-ExtraBold.ttf"), 64 * SS)
    d.text((s / 2 * SS, s / 2 * SS + 4 * SS), "A1", font=font, fill=BROWN, anchor="mm")
    save(img, "start_dot", s, s)


# ---------------------------------------------------------------- runway

def runway_dash():
    """One tile of the runway centre line: a yellow dash, then a gap, like the taxi lines
    on the apron at Home. Tiled along the path."""
    w, h = 128, 32
    img = canvas(w, h)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle(scaled((8, 6, 72, h - 6)), radius=10 * SS, fill=TAXI_YELLOW)
    save(img, "runway_dash", w, h)


def runway_lights():
    """One tile of the runway's surface, repeated along it: soft blue-grey tarmac with a
    lamp on each edge. Across the line is the texture's height, along it its width."""
    w, h = 64, 64
    img = canvas(w, h)
    d = ImageDraw.Draw(img)
    d.rectangle(scaled((0, 0, w, h)), fill=RUNWAY)
    for cy in (11, h - 11):
        d.ellipse(scaled((w / 2 - 7, cy - 7, w / 2 + 7, cy + 7)), fill=BROWN)
        d.ellipse(scaled((w / 2 - 4.5, cy - 4.5, w / 2 + 4.5, cy + 4.5)), fill=(255, 238, 186, 255))
    save(img, "runway_lights", w, h)


def solid_white():
    img = Image.new("RGBA", (8, 8), WHITE)
    os.makedirs(OUT, exist_ok=True)
    img.save(os.path.join(OUT, "solid_white.png"))


def glow():
    s = 128
    img = canvas(s, s)
    d = ImageDraw.Draw(img)
    d.ellipse(scaled((24, 24, s - 24, s - 24)), fill=WHITE)
    img = img.filter(ImageFilter.GaussianBlur(12 * SS))
    d = ImageDraw.Draw(img)
    d.ellipse(scaled((44, 44, s - 44, s - 44)), fill=WHITE)
    save(img, "spark_glow", s, s)


# ---------------------------------------------------------------- airplane

PLANE = 512


def plane_shapes():
    """Returns (silhouette mask, list of (mask, colour) layers) for the top-down plane."""
    s = PLANE
    c = s / 2

    def poly_mask(points):
        m = Image.new("L", (s * SS, s * SS), 0)
        ImageDraw.Draw(m).polygon([(x * SS, y * SS) for x, y in points], fill=255)
        return m

    def rr(box, r):
        return mask_rrect(s, s, box, r)

    def ell(box):
        m = Image.new("L", (s * SS, s * SS), 0)
        ImageDraw.Draw(m).ellipse(scaled(box), fill=255)
        return m

    def ring(mask, px):
        """A brown line hugging the outside of a part, so parts read as separate pieces."""
        return ImageChops.subtract(grow(mask, px), mask)

    def blank():
        return Image.new("L", (s * SS, s * SS), 0)

    def smooth(mask, px=6):
        return mask.filter(ImageFilter.GaussianBlur(px * SS)).point(lambda v: 255 if v > 127 else 0)

    def mirror(points):
        return [(2 * c - x, y) for x, y in points]

    # The airline's plane seen from above: the same chibi jet that waits at the stand on
    # Home, in the same livery. A fat cream body, short swept coral wings with an engine
    # each, a coral tail with the airline's roundel and a big cockpit window up front.
    body = ell((c - 96, 40, c + 96, 432))
    wing_pts = [(c - 60, 200), (c - 236, 270), (c - 236, 306), (c - 60, 292)]
    wings = ImageChops.lighter(smooth(poly_mask(wing_pts)), smooth(poly_mask(mirror(wing_pts))))
    engines = ImageChops.lighter(ell((c - 176, 214, c - 120, 296)), ell((c + 120, 214, c + 176, 296)))
    intakes = ImageChops.lighter(ell((c - 168, 216, c - 128, 236)), ell((c + 128, 216, c + 168, 236)))
    tail_pts = [(c - 36, 376), (c - 132, 420), (c - 132, 448), (c - 36, 438)]
    tail_plane = ImageChops.lighter(smooth(poly_mask(tail_pts), 4), smooth(poly_mask(mirror(tail_pts)), 4))
    fin = rr((c - 14, 352, c + 14, 462), 14)

    silhouette = body
    for part in (wings, engines, tail_plane, fin):
        silhouette = ImageChops.lighter(silhouette, part)

    layers = []
    layers.append((tail_plane, CORAL, True))
    layers.append((wings, CORAL_DARK, True))
    layers.append((ring(engines, 5), BROWN))
    layers.append((engines, CREAM, True))
    layers.append((intakes, CORAL))

    layers.append((ring(body, 6), BROWN))
    layers.append((body, CREAM, True))
    # The livery's cheatline, seen from above as a stripe down each side.
    stripes = ImageChops.lighter(rr((c - 84, 220, c - 70, 400), 7), rr((c + 70, 220, c + 84, 400), 7))
    layers.append((ImageChops.multiply(stripes, body), CORAL))
    windows = blank()
    wd = ImageDraw.Draw(windows)
    for y in (262, 296, 330):
        for x in (c - 54, c + 54):
            wd.ellipse(scaled((x - 7, y - 7, x + 7, y + 7)), fill=255)
    layers.append((ring(windows, 3), BROWN))
    layers.append((windows, GLASS))

    # The airline's roundel on the roof, and the fin's top edge running back to the tail.
    roundel = ell((c - 24, 270, c + 24, 318))
    layers.append((ring(roundel, 4), BROWN))
    layers.append((roundel, CREAM))
    layers.append((ell((c - 12, 282, c + 12, 306)), CORAL))
    layers.append((ring(fin, 5), BROWN))
    layers.append((fin, CORAL, True))

    # Cockpit bubble for the cat captain. The captain is Bơ from the passenger art,
    # pasted in by captain() into its own sprite so it can stay upright as the plane turns.
    cockpit = ell((c - 74, 100, c + 74, 250))
    layers.append((ring(cockpit, 6), BROWN))
    layers.append((cockpit, GLASS))
    return silhouette, layers, cockpit


def captain(cockpit):
    """Bơ, cap and all, peeking out of the cockpit bubble: the head and shoulders of the
    passenger sprite, clipped to the glass."""
    img = Image.new("RGBA", cockpit.size, (0, 0, 0, 0))
    path = os.path.join(CATS, "passenger_bo_sit.png")
    if not os.path.exists(path):
        return img
    cat = Image.open(path).convert("RGBA")
    left, top, right, bottom = cat.getbbox()
    bust = cat.crop((left, top, right, top + int((bottom - top) * 0.68)))
    width = 158 * SS
    bust = bust.resize((width, int(bust.height * width / bust.width)), Image.LANCZOS)
    c = PLANE / 2
    img.alpha_composite(bust, (int(c * SS - width / 2), int(104 * SS)))
    img.putalpha(ImageChops.multiply(img.getchannel("A"), cockpit))
    return img


def paint_layers(img, layers):
    for layer in layers:
        mask, colour = layer[0], layer[1]
        fill_mask(img, mask, colour)
        if len(layer) > 2 and layer[2]:
            puff(img, mask, SS)


# The pilot sprite is cut square around the cockpit's centre, so it can be turned about
# its own middle. Keep in sync with SpriteAirplaneRig's pilot offset.
COCKPIT_CENTRE = (256, 175)
PILOT_SIZE = 176


def airplane():
    s = PLANE
    silhouette, body_layers, cockpit = plane_shapes()
    img = Image.new("RGBA", (s * SS, s * SS), (0, 0, 0, 0))

    paint_layers(img, body_layers)
    body = outline_under(img, silhouette, 12)
    save(body, "airplane", s, s)

    pilot = Image.new("RGBA", body.size, (0, 0, 0, 0))
    pilot.alpha_composite(captain(cockpit))
    x, y = COCKPIT_CENTRE
    half = PILOT_SIZE // 2
    cut = pilot.crop(((x - half) * SS, (y - half) * SS, (x + half) * SS, (y + half) * SS))
    save(cut, "airplane_pilot", PILOT_SIZE, PILOT_SIZE)

    # The whole plane in one image, for places where it never turns (the Home runway).
    full = body.copy()
    full.alpha_composite(pilot)
    save(full, "airplane_full", s, s)

    shadow = Image.new("RGBA", (s * SS, s * SS), (0, 0, 0, 0))
    fill_mask(shadow, grow(silhouette, 12), BROWN)
    shadow = shadow.filter(ImageFilter.GaussianBlur(SS))
    save(shadow, "airplane_shadow", s, s)


# ---------------------------------------------------------------- hint

def paper_plane():
    """The hint's guide: a paper plane seen from above, nose up, gliding the next squares
    ahead of ours. A plane, so it belongs in an airline game; paper, so it is never taken
    for the player's own."""
    s = 256
    c = s / 2
    img = canvas(s, s)

    def poly(points):
        m = Image.new("L", (s * SS, s * SS), 0)
        ImageDraw.Draw(m).polygon([(x * SS, y * SS) for x, y in points], fill=255)
        return m

    nose, tail = 30, 214
    wing_l = poly([(c, nose), (c - 92, tail), (c - 8, 186)])
    wing_r = poly([(c, nose), (c + 92, tail), (c + 8, 186)])
    keel = poly([(c, nose + 20), (c - 12, 200), (c, 222), (c + 12, 200)])
    silhouette = ImageChops.lighter(ImageChops.lighter(wing_l, wing_r), keel)

    fill_mask(img, wing_l, WHITE)
    fill_mask(img, wing_r, (226, 240, 250, 255))
    fill_mask(img, keel, (196, 222, 242, 255))
    # The centre fold and a crease on each wing, drawn as fine brown lines.
    lines = Image.new("L", (s * SS, s * SS), 0)
    ld = ImageDraw.Draw(lines)
    ld.line([(c * SS, nose * SS), (c * SS, 214 * SS)], fill=255, width=4 * SS)
    ld.line([(c * SS, (nose + 30) * SS), ((c - 52) * SS, 200 * SS)], fill=150, width=3 * SS)
    ld.line([(c * SS, (nose + 30) * SS), ((c + 52) * SS, 200 * SS)], fill=150, width=3 * SS)
    fill_mask(img, ImageChops.multiply(lines, silhouette), BROWN)
    out = outline_under(img, silhouette, 9)
    save(out, "paper_plane", s, s)


def hint_puff():
    """One puff of the paper plane's trail: a little white cloud dot with a brown rim, like
    a vapour trail drawn by a child. Laid one by one behind the plane."""
    s = 64
    img = canvas(s, s)
    d = ImageDraw.Draw(img)
    d.ellipse(scaled((8, 8, s - 8, s - 8)), fill=BROWN)
    d.ellipse(scaled((14, 14, s - 14, s - 14)), fill=WHITE)
    save(img, "hint_puff", s, s)


# ---------------------------------------------------------------- sky

def sky():
    """A vertical gradient, stretched over the whole view at runtime."""
    top = (132, 198, 236)
    bottom = (208, 236, 250)
    h = 256
    img = Image.new("RGBA", (4, h))
    for y in range(h):
        t = y / (h - 1)
        t = t * t * (3 - 2 * t)
        colour = tuple(round(top[i] + (bottom[i] - top[i]) * t) for i in range(3)) + (255,)
        for x in range(4):
            img.putpixel((x, y), colour)
    os.makedirs(OUT, exist_ok=True)
    img.save(os.path.join(OUT, "sky_gradient.png"))


def cloud(name, puffs, w, h):
    """A flat cloud: white puffs over a light blue underside, no outline (background)."""
    img = canvas(w, h)
    body = Image.new("L", img.size, 0)
    d = ImageDraw.Draw(body)
    for (x, y, r) in puffs:
        d.ellipse(scaled((x - r, y - r, x + r, y + r)), fill=255)
    base_y = max(y + r * 0.35 for (x, y, r) in puffs)
    d.rounded_rectangle(scaled((min(x - r * 0.6 for x, y, r in puffs), base_y - 40,
                                max(x + r * 0.6 for x, y, r in puffs), base_y + 6)), radius=24 * SS, fill=255)
    fill_mask(img, body, (214, 234, 248, 255))
    top = body.transform(body.size, Image.AFFINE, (1, 0, 0, 0, 1, 12 * SS))
    top = ImageChops.multiply(top, body)
    fill_mask(img, top, (255, 255, 255, 255))
    save(img, name, w, h)


def clouds():
    cloud("cloud_0", [(90, 110, 58), (160, 86, 72), (236, 112, 56), (290, 130, 38)], 340, 180)
    cloud("cloud_1", [(70, 96, 44), (128, 76, 58), (186, 100, 44)], 240, 150)
    cloud("cloud_2", [(80, 118, 50), (150, 88, 66), (222, 96, 60), (292, 116, 48), (346, 132, 30)], 390, 180)


def main():
    tile_face()
    tile_overlay()
    tile_shadow()
    tile_flash()
    start_pad()
    runway_dash()
    runway_lights()
    solid_white()
    glow()
    airplane()
    paper_plane()
    hint_puff()
    sky()
    clouds()
    print("flat art written to", os.path.abspath(OUT))


if __name__ == "__main__":
    main()
