"""Art for the loading screen shown between scenes: the track the cats chase round and
the speech bubble Captain Bơ talks in, in the same flat sticker style as the passengers.

    python Tools/flat_art/generate_loading.py
"""
import math
import os

import generate_home

from generate_home import BROWN, CREAM, Canvas

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Art", "Loading")


def bubble():
    """The speech bubble Captain Bơ talks in: a cream rounded card with a tail at its foot."""
    w, h = 800, 260
    c = Canvas(w, h)
    tail = [(w / 2 - 44, h - 80), (w / 2 + 4, h - 14), (w / 2 + 40, h - 80)]
    card = (14, 14, w - 14, h - 66)
    # Outlines of both first, then both fills, so the tail joins the card seamlessly.
    c.poly(tail, BROWN, line=6, puffy=False)
    c.rect(card, 54, BROWN, line=6, puffy=False)
    c.poly(tail, CREAM, line=0, puffy=False)
    c.rect(card, 54, CREAM, line=0, puffy=False)
    c.save("loading_bubble")


def care_bubble():
    """
    The lounge's paw button: a round cream speech bubble whose tail points down-left at
    the cat it belongs to. The paw is laid over it in the UI, in the kit's blue.
    """
    w, h = 180, 170
    c = Canvas(w, h)
    tail = [(52, 118), (14, 160), (86, 128)]
    body = (26, 8, 172, 138)
    c.poly(tail, BROWN, line=6, puffy=False)
    c.oval(body, BROWN, line=6, puffy=False)
    c.poly(tail, CREAM, line=0, puffy=False)
    c.oval(body, CREAM, line=0, puffy=False)
    c.save("care_bubble")

    # The same bubble facing the other way, for a cat too near the right edge to fit it.
    from PIL import Image
    path = os.path.join(generate_home.OUT, "care_bubble.png")
    Image.open(path).transpose(Image.FLIP_LEFT_RIGHT).save(os.path.join(generate_home.OUT, "care_bubble_left.png"))


def track():
    """The oval the cats chase round: a soft grassy patch with a dashed white lane."""
    w, h = 760, 200
    c = Canvas(w, h)
    c.d.ellipse(c._box((6, 6, w - 6, h - 6)), fill=(150, 214, 150, 150))
    c.d.ellipse(c._box((26, 22, w - 26, h - 22)), fill=(176, 228, 168, 170))
    rx, ry, cx, cy = w / 2 - 70, h / 2 - 40, w / 2, h / 2
    for i in range(28):
        a0 = math.radians(i * 360 / 28)
        a1 = a0 + math.radians(360 / 28 * 0.55)
        pts = [(cx + rx * math.cos(a0 + (a1 - a0) * t / 6), cy + ry * math.sin(a0 + (a1 - a0) * t / 6)) for t in range(7)]
        c.line(pts, (255, 255, 255, 220), 7)
    c.save("loading_track")


def main():
    generate_home.OUT = OUT
    os.makedirs(OUT, exist_ok=True)
    bubble()
    care_bubble()
    track()
    print("loading art written")


if __name__ == "__main__":
    main()
