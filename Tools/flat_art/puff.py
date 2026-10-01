"""Shared shading for the flat art, matched to the Craftpix cute cats the passengers use.

The cats are flat colour with a dark red-brown outline and one hard-edged shade: a
darker band of the same colour along the side turned away from the light, no gradients
and no gloss. Every generator calls puff() on a shape's mask right after filling it,
before the outline goes on, so all art in the game shades the same way:

- light comes from the top left, so the shade is a crescent along the bottom and right,
  cut where the shape, nudged towards the light, no longer covers it;
- the shade multiplies the colours already painted, so patterns and patches darken with
  the surface instead of being painted over.

The name and signature stay from the earlier puffy style so the generators need no
change; "gloss" is accepted and ignored. Masks and images are at supersampled
resolution; "ss" is the supersample factor so edges stay the same width in final pixels.
"""
from PIL import Image, ImageChops, ImageFilter

# Darkening applied to the far side, warm like the cats' own shade (orange ffb54d -> e8973d).
SHADE_TINT = (226, 200, 196)
# Matches the passengers' outline.
OUTLINE = (100, 56, 53, 255)


def puff(img, mask, ss, strength=1.0, gloss=True):
    """Cel-shades img in place inside mask. Works on the mask's bounding box only, for speed."""
    box = mask.getbbox()
    if box is None:
        return
    x0, y0, x1, y1 = box
    w, h = x1 - x0, y1 - y0
    if w < 4 * ss or h < 4 * ss:
        return

    pad = 2 * ss
    cx0, cy0 = max(0, x0 - pad), max(0, y0 - pad)
    cx1, cy1 = min(img.width, x1 + pad), min(img.height, y1 + pad)
    region = img.crop((cx0, cy0, cx1, cy1))
    m = mask.crop((cx0, cy0, cx1, cy1))

    # The lit part is the shape shifted towards the light; what it leaves uncovered at
    # the bottom right is in shade. The band scales with the shape but stays readable.
    dx = max(2 * ss, int(min(w * 0.12, h * 0.5)))
    dy = max(2 * ss, int(min(h * 0.14, w * 0.5)))
    lit = m.transform(m.size, Image.AFFINE, (1, 0, dx, 0, 1, dy))
    band = ImageChops.subtract(m, lit)
    # Soften by a pixel only, so the cut stays crisp but anti-aliased.
    band = band.filter(ImageFilter.GaussianBlur(ss * 0.6)).point(lambda v: 255 if v > 127 else 0)
    band = band.filter(ImageFilter.GaussianBlur(ss * 0.5))
    band = ImageChops.multiply(band, m).point(lambda v: int(v * min(1.0, strength)))

    tint = tuple(int(255 - (255 - c) * min(1.0, strength)) for c in SHADE_TINT)
    shaded = ImageChops.multiply(region, Image.new("RGBA", region.size, tint + (255,)))
    shaded.putalpha(ImageChops.multiply(band, region.getchannel("A")))
    region.alpha_composite(shaded)
    img.paste(region, (cx0, cy0))
