"""Generates the AirLine Pop UI kit: 9-slice panels, chunky buttons and flat icons.

Style matches the Craftpix cats the passengers use (cream, olive, earthy orange, the same
dark red-brown outline): flat rounded blocks with a hard-edged lip, matte, no gloss. Everything is drawn 4x and
scaled down for clean edges. Rerun after tweaking a colour; Unity keeps the GUIDs.

    python Tools/ui_kit/generate_ui_kit.py
"""
import math
import os

from PIL import Image, ImageChops, ImageDraw, ImageFilter

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Art", "UIKit")
# Drawn at SS times the layout size and written at OUT_SCALE times it: layout numbers
# below stay in the original 128 px grid, the files come out at 256 px.
SS = 8
OUT_SCALE = 2

BROWN = (100, 56, 53, 255)  # the passengers' outline
CREAM = (255, 250, 241, 255)
CREAM_DARK = (232, 222, 204, 255)
# The secondary colour is the sky the planes fly in, as through the lounge window.
SKY = (112, 186, 230, 255)
SKY_DARK = (70, 140, 196, 255)
SKY_LIGHT = (214, 236, 250, 255)
SKY_LIGHT_DARK = (178, 214, 240, 255)
ORANGE = (246, 150, 64, 255)
ORANGE_DARK = (204, 104, 40, 255)
ORANGE_LIGHT = (255, 186, 110, 255)
PINK = (242, 160, 170, 255)
PINK_DARK = (200, 106, 120, 255)
GOLD = (255, 204, 64, 255)
GOLD_DARK = (214, 142, 26, 255)
WHITE = (255, 255, 255, 255)
SHADOW = (100, 56, 53, 70)

# Icon outline width at the final 128 px size.
OUTLINE_PX = 7


def canvas(w, h):
    return Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))


def save(img, name, w, h):
    """Writes at OUT_SCALE times the layout size, so buttons and icons stay crisp on
    high-density phones. Unity imports the kit at a matching pixels-per-unit."""
    os.makedirs(OUT, exist_ok=True)
    img.resize((w * OUT_SCALE, h * OUT_SCALE), Image.LANCZOS).save(os.path.join(OUT, name + ".png"))


def dilate_round(alpha, radius):
    """Grows a hard-edged mask by radius pixels with round corners.

    A square MaxFilter grows corners into squares, which reads as a chipped, blocky
    outline. Blurring and cutting just above zero grows it evenly in every direction:
    with sigma = radius / 2, the 2-sigma point sits radius pixels out.
    """
    hard = alpha.point(lambda v: 255 if v > 127 else 0)
    blurred = hard.filter(ImageFilter.GaussianBlur(radius / 2.0))
    return blurred.point(lambda v: 255 if v > 6 else 0)


def rrect(d, box, r, fill):
    d.rounded_rectangle([v * SS for v in box], radius=r * SS, fill=fill)


def block(name, w, h, r, face, lip, outline=4, lip_h=11):
    """A chunky block like the passengers are drawn: a dark brown outline as thick as
    theirs, a deep darker lip along the bottom, a flat face on top."""
    img = canvas(w, h)
    d = ImageDraw.Draw(img)
    rrect(d, (0, 0, w, h), r, BROWN)
    rrect(d, (outline, outline, w - outline, h - outline), r - outline, lip)
    rrect(d, (outline, outline, w - outline, h - outline - lip_h), r - outline, face)
    save(img, name, w, h)


def panel(name, w, h, r, fill, outline=4, shade=CREAM_DARK):
    img = canvas(w, h)
    d = ImageDraw.Draw(img)
    rrect(d, (0, 0, w, h), r, BROWN)
    rrect(d, (outline, outline, w - outline, h - outline), r - outline, shade)
    rrect(d, (outline, outline, w - outline, h - outline - 6), r - outline, fill)
    save(img, name, w, h)


def ticket(name, w, h):
    """The Play button as a boarding pass: the orange body, a notch either side where it
    tears, a dotted perforation, and a lighter stub on the right for the plane icon."""
    r, outline, lip_h, notch = 24, 4, 11, 13
    perf = w * 0.74

    shape = Image.new("L", (w * SS, h * SS), 0)
    sd = ImageDraw.Draw(shape)
    pad = outline
    sd.rounded_rectangle([pad * SS, pad * SS, (w - pad) * SS, (h - pad) * SS], radius=r * SS, fill=255)
    for cy in (pad, h - pad):
        sd.ellipse([(perf - notch) * SS, (cy - notch) * SS, (perf + notch) * SS, (cy + notch) * SS], fill=0)

    img = canvas(w, h)
    outline_mask = dilate_round(shape, outline * SS)
    brown = Image.new("RGBA", img.size, BROWN)
    img.paste(brown, (0, 0), outline_mask)

    def fill(colour, region):
        layer = Image.new("RGBA", img.size, colour)
        img.paste(layer, (0, 0), ImageChops.multiply(shape, region))

    everything = Image.new("L", img.size, 255)
    stub = Image.new("L", img.size, 0)
    ImageDraw.Draw(stub).rectangle([perf * SS, 0, w * SS, h * SS], fill=255)
    lip = Image.new("L", img.size, 0)
    ImageDraw.Draw(lip).rectangle([0, (h - pad - lip_h) * SS, w * SS, h * SS], fill=255)

    fill(ORANGE_DARK, everything)
    fill(ORANGE, ImageChops.subtract(everything, lip))
    fill(ORANGE_LIGHT, ImageChops.subtract(stub, lip))

    d = ImageDraw.Draw(img)
    y = pad + notch + 4
    while y < h - pad - notch - lip_h:
        d.rounded_rectangle([(perf - 2.5) * SS, y * SS, (perf + 2.5) * SS, (y + 7) * SS], radius=2 * SS, fill=CREAM)
        y += 13
    save(img, name, w, h)


def trough(name, w, h, r, fill, inset):
    """A sunken track: brown outline, a shadow along the inside top, the floor below it."""
    img = canvas(w, h)
    d = ImageDraw.Draw(img)
    o = 4
    rrect(d, (0, 0, w, h), r, BROWN)
    rrect(d, (o, o, w - o, h - o), r - o, inset)
    rrect(d, (o, o + 5, w - o, h - o), r - o, fill)
    save(img, name, w, h)


def pill(name, w, h, r, face, lip, lip_h=5):
    """A plain rounded fill with a darker underside and no outline: it sits inside a trough."""
    img = canvas(w, h)
    d = ImageDraw.Draw(img)
    rrect(d, (0, 0, w, h), r, lip)
    rrect(d, (0, 0, w, h - lip_h), r, face)
    save(img, name, w, h)


def switch():
    """The settings switch: a track drawn on and off (swapped, not tinted) and its knob."""
    block("toggle_on", 68, 36, 18, SKY, SKY_DARK, outline=4, lip_h=6)
    block("toggle_off", 68, 36, 18, (222, 212, 196, 255), (190, 178, 160, 255), outline=4, lip_h=6)
    block("toggle_knob", 30, 30, 15, CREAM, CREAM_DARK, outline=3, lip_h=5)


def soft_shadow(name, w, h):
    img = canvas(w, h)
    d = ImageDraw.Draw(img)
    d.ellipse([0, 0, w * SS, h * SS], fill=SHADOW)
    img = img.filter(ImageFilter.GaussianBlur(6 * SS))
    save(img, name, w, h)


def icon(name, draw_fn, size=128, color=WHITE):
    """Draws the icon's fill, then derives a uniform brown outline by growing its alpha."""
    s = size * SS
    fill = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    draw_fn(ImageDraw.Draw(fill), s, color)
    alpha = dilate_round(fill.split()[3], OUTLINE_PX * SS)
    img = Image.new("RGBA", (s, s), BROWN)
    img.putalpha(alpha)
    img.alpha_composite(fill)
    save(img, "icon_" + name, size, size)


def arc_pts(cx, cy, r, a0, a1, n=40):
    return [(cx + r * math.cos(math.radians(a0 + (a1 - a0) * i / n)),
             cy + r * math.sin(math.radians(a0 + (a1 - a0) * i / n))) for i in range(n + 1)]


def stroke(d, pts, c, w):
    d.line(pts, fill=c, width=int(w), joint="curve")
    for p in (pts[0], pts[-1]):
        d.ellipse([p[0] - w / 2, p[1] - w / 2, p[0] + w / 2, p[1] + w / 2], fill=c)


def draw_coin(d, s, c):
    d.ellipse([s * .12, s * .12, s * .88, s * .88], fill=GOLD_DARK)
    d.ellipse([s * .12, s * .12, s * .88, s * .82], fill=GOLD)
    d.ellipse([s * .30, s * .28, s * .70, s * .66], fill=GOLD_DARK)
    d.ellipse([s * .33, s * .29, s * .67, s * .62], fill=(255, 226, 120, 255))


def draw_home(d, s, c):
    d.polygon([(s * .5, s * .14), (s * .88, s * .48), (s * .12, s * .48)], fill=c)
    d.rounded_rectangle([s * .22, s * .40, s * .78, s * .86], radius=s * .05, fill=c)
    d.rounded_rectangle([s * .42, s * .60, s * .58, s * .86], radius=s * .04, fill=SKY_DARK)


def draw_paw(d, s, c):
    d.ellipse([s * .26, s * .46, s * .74, s * .86], fill=c)
    for bx, by in ((.12, .30), (.31, .12), (.53, .12), (.72, .30)):
        d.ellipse([s * bx, s * by, s * (bx + .17), s * (by + .22)], fill=c)


def draw_shop(d, s, c):
    stroke(d, arc_pts(s * .5, s * .38, s * .17, 180, 360), c, s * .07)
    d.rounded_rectangle([s * .16, s * .36, s * .84, s * .86], radius=s * .1, fill=c)
    d.ellipse([s * .31, s * .48, s * .39, s * .56], fill=BROWN)
    d.ellipse([s * .61, s * .48, s * .69, s * .56], fill=BROWN)


def draw_pause(d, s, c):
    d.rounded_rectangle([s * .24, s * .18, s * .42, s * .82], radius=s * .06, fill=c)
    d.rounded_rectangle([s * .58, s * .18, s * .76, s * .82], radius=s * .06, fill=c)


def draw_undo(d, s, c):
    stroke(d, arc_pts(s * .54, s * .56, s * .26, 205, 450), c, s * .12)
    d.polygon([(s * .12, s * .40), (s * .42, s * .28), (s * .34, s * .60)], fill=c)


def draw_restart(d, s, c):
    stroke(d, arc_pts(s * .5, s * .54, s * .28, -60, 230), c, s * .12)
    d.polygon([(s * .60, s * .14), (s * .86, s * .32), (s * .56, s * .42)], fill=c)


def draw_settings(d, s, c):
    """A gear: a round body with eight chunky, rounded teeth and a hole in the middle."""
    cx = cy = s * .5
    # Kept inside 0.37 of the size so the outline added around it never meets the edge.
    tooth_w, tooth_in, tooth_out = s * .14, s * .22, s * .355
    for i in range(8):
        a = math.radians(i * 45)
        ux, uy = math.cos(a), math.sin(a)
        px, py = -uy, ux
        corners = [(cx + ux * r + px * w, cy + uy * r + py * w)
                   for r, w in ((tooth_in, -tooth_w / 2), (tooth_out, -tooth_w / 2 * .8),
                                (tooth_out, tooth_w / 2 * .8), (tooth_in, tooth_w / 2))]
        d.polygon(corners, fill=c)
        # Round the tooth's tip.
        tx, ty = cx + ux * tooth_out, cy + uy * tooth_out
        rr_ = tooth_w * .4
        d.ellipse([tx - rr_, ty - rr_, tx + rr_, ty + rr_], fill=c)
    d.ellipse([cx - s * .27, cy - s * .27, cx + s * .27, cy + s * .27], fill=c)
    d.ellipse([cx - s * .11, cy - s * .11, cx + s * .11, cy + s * .11], fill=SKY_DARK)


def draw_lock(d, s, c):
    stroke(d, arc_pts(s * .5, s * .42, s * .17, 180, 360) + [(s * .67, s * .5)], c, s * .08)
    stroke(d, [(s * .33, s * .42), (s * .33, s * .5)], c, s * .08)
    d.rounded_rectangle([s * .2, s * .44, s * .8, s * .86], radius=s * .1, fill=c)
    d.ellipse([s * .44, s * .58, s * .56, s * .70], fill=BROWN)


def draw_bulb(d, s, c):
    """A light bulb: the hint."""
    d.ellipse([s * .24, s * .10, s * .76, s * .62], fill=c)
    d.rounded_rectangle([s * .36, s * .50, s * .64, s * .72], radius=s * .05, fill=c)
    d.rounded_rectangle([s * .36, s * .74, s * .64, s * .82], radius=s * .04, fill=c)
    d.rounded_rectangle([s * .41, s * .84, s * .59, s * .90], radius=s * .03, fill=c)
    for x0, x1 in ((.42, .46), (.54, .58)):
        d.rounded_rectangle([s * x0, s * .40, s * x1, s * .66], radius=s * .02, fill=BROWN)


def draw_globe(d, s, c):
    """A globe: the language setting."""
    d.ellipse([s * .12, s * .12, s * .88, s * .88], fill=c)
    w = int(s * .045)
    d.ellipse([s * .34, s * .12, s * .66, s * .88], outline=SKY_DARK, width=w)
    d.line([(s * .5, s * .14), (s * .5, s * .86)], fill=SKY_DARK, width=w)
    d.line([(s * .14, s * .5), (s * .86, s * .5)], fill=SKY_DARK, width=w)
    d.arc([s * .2, s * .1, s * .8, s * .38], 30, 150, fill=SKY_DARK, width=w)
    d.arc([s * .2, s * .62, s * .8, s * .9], 210, 330, fill=SKY_DARK, width=w)


def draw_check(d, s, c):
    stroke(d, [(s * .2, s * .52), (s * .42, s * .74), (s * .8, s * .28)], c, s * .15)


def draw_close(d, s, c):
    stroke(d, [(s * .26, s * .26), (s * .74, s * .74)], c, s * .15)
    stroke(d, [(s * .74, s * .26), (s * .26, s * .74)], c, s * .15)


def draw_heart(d, s, c):
    pts = []
    for i in range(80):
        t = math.pi * 2 * i / 80
        x = 16 * math.sin(t) ** 3
        y = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
        pts.append((s * .5 + x * s * .024, s * .47 - y * s * .024))
    d.polygon(pts, fill=c)


def draw_fish(d, s, c):
    d.ellipse([s * .12, s * .30, s * .70, s * .70], fill=c)
    d.polygon([(s * .60, s * .5), (s * .88, s * .28), (s * .88, s * .72)], fill=c)
    d.ellipse([s * .24, s * .43, s * .31, s * .50], fill=BROWN)


def draw_play(d, s, c):
    d.polygon([(s * .30, s * .18), (s * .82, s * .5), (s * .30, s * .82)], fill=c)


def draw_plane(d, s, c):
    d.rounded_rectangle([s * .43, s * .10, s * .57, s * .88], radius=s * .07, fill=c)
    d.polygon([(s * .45, s * .40), (s * .88, s * .60), (s * .88, s * .68), (s * .45, s * .58)], fill=c)
    d.polygon([(s * .55, s * .40), (s * .12, s * .60), (s * .12, s * .68), (s * .55, s * .58)], fill=c)
    d.polygon([(s * .47, s * .76), (s * .70, s * .86), (s * .70, s * .90), (s * .30, s * .90),
               (s * .30, s * .86), (s * .53, s * .76)], fill=c)


def draw_airport(d, s, c):
    d.rounded_rectangle([s * .14, s * .50, s * .86, s * .86], radius=s * .06, fill=c)
    d.rounded_rectangle([s * .42, s * .24, s * .58, s * .52], radius=s * .03, fill=c)
    d.rounded_rectangle([s * .32, s * .12, s * .68, s * .28], radius=s * .06, fill=c)
    for x in (.24, .40, .56, .72):
        d.rounded_rectangle([s * x, s * .60, s * (x + .08), s * .70], radius=s * .02, fill=SKY_DARK)


def draw_sound(d, s, c):
    d.polygon([(s * .14, s * .38), (s * .32, s * .38), (s * .54, s * .18), (s * .54, s * .82),
               (s * .32, s * .62), (s * .14, s * .62)], fill=c)
    stroke(d, arc_pts(s * .52, s * .5, s * .2, -40, 40, 20), c, s * .07)
    stroke(d, arc_pts(s * .52, s * .5, s * .32, -40, 40, 20), c, s * .07)


def draw_music(d, s, c):
    stroke(d, [(s * .38, s * .70), (s * .38, s * .20), (s * .78, s * .13), (s * .78, s * .62)], c, s * .08)
    d.ellipse([s * .16, s * .60, s * .42, s * .82], fill=c)
    d.ellipse([s * .56, s * .52, s * .82, s * .74], fill=c)


def draw_vibrate(d, s, c):
    d.rounded_rectangle([s * .34, s * .14, s * .66, s * .86], radius=s * .08, fill=c)
    stroke(d, [(s * .18, s * .38), (s * .18, s * .62)], c, s * .06)
    stroke(d, [(s * .82, s * .38), (s * .82, s * .62)], c, s * .06)


def draw_motion(d, s, c):
    d.ellipse([s * .44, s * .30, s * .82, s * .68], fill=c)
    for y, x0 in ((.36, .22), (.49, .14), (.62, .22)):
        stroke(d, [(s * x0, s * y), (s * .34, s * y)], c, s * .06)


def draw_back(d, s, c):
    stroke(d, [(s * .62, s * .2), (s * .34, s * .5), (s * .62, s * .8)], c, s * .15)


ICONS = {
    "coin": draw_coin, "home": draw_home, "paw": draw_paw, "shop": draw_shop, "pause": draw_pause,
    "undo": draw_undo, "restart": draw_restart, "settings": draw_settings, "lock": draw_lock,
    "check": draw_check, "bulb": draw_bulb, "globe": draw_globe, "close": draw_close, "heart": draw_heart, "fish": draw_fish, "play": draw_play,
    "plane": draw_plane, "airport": draw_airport, "sound": draw_sound, "music": draw_music,
    "vibrate": draw_vibrate, "motion": draw_motion, "back": draw_back,
}


# ------------------------------------------------------------------ menu icons (in colour)

CORAL = (240, 124, 108, 255)
CORAL_DARK = (206, 92, 82, 255)


def color_icon(name, draw_fn, size=128):
    """A menu icon in its own colours, like the props it stands for: drawn, then given
    the same round brown outline as every other icon. Never tinted in Unity."""
    s = size * SS
    fill = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    draw_fn(ImageDraw.Draw(fill), s)
    alpha = dilate_round(fill.split()[3], OUTLINE_PX * SS)
    img = Image.new("RGBA", (s, s), BROWN)
    img.putalpha(alpha)
    img.alpha_composite(fill)
    save(img, name, size, size)


def shaded_rect(d, s, box, r, face, shade, k=0.05):
    """A flat shape with one hard-edged shade along its bottom right, as the props have."""
    x0, y0, x1, y1 = box
    d.rounded_rectangle([s * x0, s * y0, s * x1, s * y1], radius=s * r, fill=shade)
    d.rounded_rectangle([s * x0, s * y0, s * (x1 - k), s * (y1 - k)], radius=s * r, fill=face)


def shaded_oval(d, s, box, face, shade, k=0.04):
    x0, y0, x1, y1 = box
    d.ellipse([s * x0, s * y0, s * x1, s * y1], fill=shade)
    d.ellipse([s * x0, s * y0, s * (x1 - k), s * (y1 - k)], fill=face)


def nav_airport(d, s):
    """The control tower: cream shaft, sky-blue cab, coral roof."""
    d.line([s * .5, s * .04, s * .5, s * .14], fill=BROWN, width=int(s * .04))
    d.ellipse([s * .45, s * .01, s * .55, s * .09], fill=CORAL)
    shaded_rect(d, s, (.38, .40, .62, .90), .04, CREAM, CREAM_DARK, .04)
    shaded_rect(d, s, (.26, .80, .74, .92), .04, CREAM, CREAM_DARK, .03)
    shaded_rect(d, s, (.22, .24, .78, .46), .06, SKY, SKY_DARK, .04)
    for x in (.36, .50, .64):
        d.line([s * x, s * .27, s * x, s * .42], fill=WHITE, width=int(s * .025))
    d.polygon([(s * .16, s * .27), (s * .84, s * .27), (s * .68, s * .12), (s * .32, s * .12)], fill=CORAL)
    d.polygon([(s * .60, s * .27), (s * .84, s * .27), (s * .68, s * .12), (s * .62, s * .12)], fill=CORAL_DARK)


def nav_paw(d, s):
    """A paw print in Bơ's orange with pink beans."""
    shaded_oval(d, s, (.24, .44, .76, .88), ORANGE, ORANGE_DARK)
    d.ellipse([s * .36, s * .56, s * .62, s * .78], fill=PINK)
    for bx, by in ((.10, .28), (.30, .10), (.53, .10), (.73, .28)):
        shaded_oval(d, s, (bx, by, bx + .18, by + .24), ORANGE, ORANGE_DARK, .03)
        d.ellipse([s * (bx + .05), s * (by + .07), s * (bx + .12), s * (by + .16)], fill=PINK)


def nav_shop(d, s):
    """A coral shopping bag with a cream band and handle."""
    stroke(d, arc_pts(s * .5, s * .40, s * .23, 180, 360), CREAM, s * .065)
    shaded_rect(d, s, (.16, .36, .84, .88), .10, CORAL, CORAL_DARK, .05)
    d.rectangle([s * .16, s * .44, s * .79, s * .52], fill=CREAM)
    d.ellipse([s * .30, s * .58, s * .38, s * .66], fill=BROWN)
    d.ellipse([s * .58, s * .58, s * .66, s * .66], fill=BROWN)


def nav_map(d, s):
    """A folded map in three panels with a dashed flight route ending at a coral pin."""
    panels = [((.10, .24), (.37, .14), (.37, .84), (.10, .94)),
              ((.37, .14), (.63, .24), (.63, .94), (.37, .84)),
              ((.63, .24), (.90, .14), (.90, .84), (.63, .94))]
    for i, pts in enumerate(panels):
        d.polygon([(s * x, s * y) for x, y in pts], fill=SKY if i != 1 else SKY_DARK)
    route = [(.20, .78), (.32, .62), (.48, .66), (.60, .50), (.70, .40)]
    for (x0, y0), (x1, y1) in zip(route, route[1:]):
        d.line([s * (x0 + (x1 - x0) * .2), s * (y0 + (y1 - y0) * .2), s * (x0 + (x1 - x0) * .75), s * (y0 + (y1 - y0) * .75)],
               fill=WHITE, width=int(s * .045))
    d.ellipse([s * .62, s * .14, s * .84, s * .36], fill=CORAL)
    d.polygon([(s * .64, s * .30), (s * .82, s * .30), (s * .73, s * .46)], fill=CORAL)
    d.ellipse([s * .69, s * .21, s * .77, s * .29], fill=WHITE)


def _fish(d, s, cx, cy, size, colour=(150, 196, 230, 255)):
    """A little fish lying on its side, nose left."""
    d.ellipse([s * (cx - size), s * (cy - size * .55), s * (cx + size * .7), s * (cy + size * .55)], fill=colour)
    d.polygon([(s * (cx + size * .5), s * cy), (s * (cx + size * 1.1), s * (cy - size * .55)),
               (s * (cx + size * 1.1), s * (cy + size * .55))], fill=colour)
    d.ellipse([s * (cx - size * .62), s * (cy - size * .16), s * (cx - size * .38), s * (cy + size * .08)], fill=BROWN)


def snack_one(d, s):
    """One snack: a fish on a plate."""
    shaded_oval(d, s, (.08, .52, .92, .86), CREAM, CREAM_DARK, .04)
    _fish(d, s, .46, .58, .26)


def snack_bag(d, s):
    """Five snacks: a paper bag of fish."""
    shaded_rect(d, s, (.18, .3, .82, .9), .06, (226, 196, 150, 255), (196, 160, 112, 255), .05)
    d.polygon([(s * .18, s * .3), (s * .3, s * .16), (s * .7, s * .16), (s * .82, s * .3)], fill=(240, 216, 176, 255))
    _fish(d, s, .44, .14, .14)
    d.rounded_rectangle([s * .3, s * .52, s * .7, s * .72], radius=s * .05, fill=CORAL)
    _fish(d, s, .48, .62, .1, WHITE)


def snack_crate(d, s):
    """Ten snacks: a crate brimming with fish."""
    for x, y in ((.3, .26), (.52, .2), (.7, .28), (.42, .32)):
        _fish(d, s, x, y, .14)
    shaded_rect(d, s, (.1, .34, .9, .9), .05, (214, 160, 110, 255), (180, 124, 80, 255), .05)
    for y in (.52, .7):
        d.line([(s * .12, s * y), (s * .88, s * y)], fill=(180, 124, 80, 255), width=int(s * .03))
    d.rounded_rectangle([s * .36, s * .56, s * .64, s * .7], radius=s * .03, fill=CREAM)


SHOP_ICONS = {"shop_snack_1": snack_one, "shop_snack_5": snack_bag, "shop_snack_10": snack_crate}


NAV_ICONS = {"icon_nav_airport": nav_airport, "icon_nav_paw": nav_paw, "icon_nav_shop": nav_shop, "icon_nav_map": nav_map}



def main():
    panel("panel_cream", 128, 128, 36, CREAM)
    panel("panel_card", 96, 96, 24, (255, 253, 248, 255))
    panel("panel_sky", 96, 96, 24, SKY_LIGHT, shade=SKY_LIGHT_DARK)
    block("btn_sky", 128, 128, 34, SKY, SKY_DARK)
    block("btn_orange", 128, 128, 34, ORANGE, ORANGE_DARK)
    block("btn_cream", 128, 128, 34, CREAM, CREAM_DARK)
    block("btn_pink", 128, 128, 34, PINK, PINK_DARK)
    block("btn_disabled", 128, 128, 34, (206, 196, 176, 255), (160, 150, 132, 255))
    block("btn_round_sky", 128, 128, 64, SKY, SKY_DARK)
    block("btn_round_cream", 128, 128, 64, CREAM, CREAM_DARK)
    panel("pill_cream", 96, 64, 32, CREAM)
    trough("bar_track", 64, 32, 16, (236, 228, 214, 255), (206, 194, 176, 255))
    pill("bar_fill", 64, 32, 16, (246, 140, 150, 255), (214, 98, 114, 255))
    switch()
    panel("nav_bar", 128, 128, 44, CREAM)
    # The selected tab's marker: a small sky pill with no frame, under the tab's name.
    pill("tab_marker", 22, 5, 2.5, SKY, SKY, lip_h=0)
    ticket("btn_ticket", 310, 105)
    soft_shadow("soft_shadow", 128, 64)
    for name, fn in ICONS.items():
        icon(name, fn)
    for name, fn in NAV_ICONS.items():
        color_icon(name, fn)
    for name, fn in SHOP_ICONS.items():
        color_icon(name, fn)
    print("UI kit written to", os.path.abspath(OUT))


if __name__ == "__main__":
    main()
