"""Draws the small effect sprites the board's particles and the tutorial use: soft glows,
a dust mote, sparkle shapes, the ring sheet a cleared square pulses through, and the
paw that shows a new player where to drag.

The particle shapes are white so the particle systems can tint them. The paw follows the
passengers' style: cream fill, the shared brown outline, pink beans. Drawn 4x and scaled
down for clean edges. Rerun after a tweak; Unity keeps the GUIDs.

    python Tools/flat_art/fx_sprites.py
"""
import math
import os

from PIL import Image, ImageDraw, ImageFilter

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Art", "Fx")
SS = 4

WHITE = (255, 255, 255, 255)
BROWN = (100, 56, 53, 255)  # the passengers' outline
CREAM = (255, 246, 232, 255)
BEAN = (246, 160, 170, 255)


def canvas(w, h):
    return Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))


def save(img, name, w, h):
    os.makedirs(OUT, exist_ok=True)
    img.resize((w, h), Image.LANCZOS).save(os.path.join(OUT, name + ".png"))


def soft(img, radius):
    """Blurs only the alpha, keeping the colour white so tinting stays exact."""
    alpha = img.getchannel("A").filter(ImageFilter.GaussianBlur(radius * SS))
    out = Image.new("RGBA", img.size, WHITE)
    out.putalpha(alpha)
    return out


def soft_square():
    w = 232
    img = canvas(w, w)
    pad, r = 56, 46
    ImageDraw.Draw(img).rounded_rectangle([pad * SS, pad * SS, (w - pad) * SS, (w - pad) * SS], r * SS, fill=WHITE)
    save(soft(img, 18), "fx_soft_square", w, w)


def dust():
    w = 64
    img = canvas(w, w)
    ImageDraw.Draw(img).ellipse([22 * SS, 22 * SS, 42 * SS, 42 * SS], fill=WHITE)
    save(soft(img, 6), "fx_dust", w, w)


def plus():
    w = 128
    img = canvas(w, w)
    d = ImageDraw.Draw(img)
    arm, r = 22, 14
    d.rounded_rectangle([(64 - arm) * SS, 4 * SS, (64 + arm) * SS, 124 * SS], r * SS, fill=WHITE)
    d.rounded_rectangle([4 * SS, (64 - arm) * SS, 124 * SS, (64 + arm) * SS], r * SS, fill=WHITE)
    save(img, "fx_plus", w, w)


def disc():
    w = 128
    img = canvas(w, w)
    ImageDraw.Draw(img).ellipse([4 * SS, 4 * SS, 124 * SS, 124 * SS], fill=WHITE)
    save(img, "fx_disc", w, w)


def flash():
    """A starburst: a round core with thin rays, softened so it blooms rather than cuts."""
    w = 256
    img = canvas(w, w)
    d = ImageDraw.Draw(img)
    c = w * SS / 2
    rays = 16
    for i in range(rays):
        a = 2 * math.pi * i / rays
        length = (96 if i % 2 == 0 else 70) * SS
        half = 0.07
        pts = [(c, c),
               (c + math.cos(a - half) * length, c + math.sin(a - half) * length),
               (c + math.cos(a + half) * length, c + math.sin(a + half) * length)]
        d.polygon(pts, fill=WHITE)
    core = 44 * SS
    d.ellipse([c - core, c - core, c + core, c + core], fill=WHITE)
    save(soft(img, 9), "fx_flash", w, w)


def ring_sheet():
    """Five frames of a rounded square outline, fading and thinning, for a sheet animation."""
    frame, frames = 120, 5
    img = canvas(frame * frames, frame)
    for i in range(frames):
        layer = canvas(frame * frames, frame)
        d = ImageDraw.Draw(layer)
        x0 = i * frame
        inset = 24 - i * 2
        width = max(3, 9 - i)
        alpha = int(255 * (1 - i * 0.16))
        d.rounded_rectangle([(x0 + inset) * SS, inset * SS, (x0 + frame - inset) * SS, (frame - inset) * SS],
                            18 * SS, outline=(255, 255, 255, alpha), width=width * SS)
        img.alpha_composite(layer)
    save(img, "fx_ring_sheet", frame * frames, frame)


def tile():
    w, h = 208, 218
    img = canvas(w, h)
    ImageDraw.Draw(img).rounded_rectangle([6 * SS, 6 * SS, (w - 6) * SS, (h - 6) * SS], 40 * SS, fill=WHITE)
    save(img, "fx_tile", w, h)


def paw():
    """A cat paw pointing up-left: one solid silhouette (arm, pad, a stretched pointing
    toe and two folded toes) under one shared outline, the tip where the drag starts."""
    w, h = 200, 196
    s = SS
    mask = Image.new("L", (w * s, h * s), 0)
    d = ImageDraw.Draw(mask)

    def capsule(x0, y0, x1, y1, width):
        d.line([x0 * s, y0 * s, x1 * s, y1 * s], fill=255, width=width * s)
        for x, y in ((x0, y0), (x1, y1)):
            r = width / 2
            d.ellipse([(x - r) * s, (y - r) * s, (x + r) * s, (y + r) * s], fill=255)

    def circle(x, y, r):
        d.ellipse([(x - r) * s, (y - r) * s, (x + r) * s, (y + r) * s], fill=255)

    capsule(150, 150, 166, 166, 48)    # arm, ending in a rounded wrist inside the frame
    circle(116, 116, 44)               # pad
    capsule(112, 112, 42, 42, 36)      # pointing toe
    circle(150, 86, 18)                # folded toes along the top edge
    circle(146, 128, 17)

    outline = mask.filter(ImageFilter.MaxFilter(1 + 2 * 6 * s // 2 * 2))
    img = Image.new("RGBA", mask.size, (0, 0, 0, 0))
    img.paste(Image.new("RGBA", mask.size, BROWN), (0, 0), outline)
    img.paste(Image.new("RGBA", mask.size, CREAM), (0, 0), mask)

    beans = ImageDraw.Draw(img)
    beans.ellipse([96 * s, 102 * s, 136 * s, 136 * s], fill=BEAN)
    beans.ellipse([30 * s, 30 * s, 52 * s, 52 * s], fill=BEAN)
    beans.ellipse([143 * s, 79 * s, 157 * s, 93 * s], fill=BEAN)
    beans.ellipse([139 * s, 121 * s, 153 * s, 135 * s], fill=BEAN)

    shadow = Image.new("RGBA", img.size, (0, 0, 0, 0))
    shadow.putalpha(img.getchannel("A").point(lambda a: a * 0.25))
    shadow = shadow.filter(ImageFilter.GaussianBlur(4 * s))
    out = canvas(w, h)
    out.alpha_composite(shadow, (5 * s, 7 * s))
    out.alpha_composite(img)
    save(out, "tutorial_paw", w, h)


if __name__ == "__main__":
    soft_square()
    dust()
    plus()
    disc()
    flash()
    ring_sheet()
    tile()
    paw()
    print("FX sprites written to", os.path.normpath(OUT))
