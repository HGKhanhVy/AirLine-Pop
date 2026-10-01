"""Builds the world atlas: the CubeAnimals colour map with rows 10 and 11 (purples the
cats never use) repainted in the GDD palette, so cats and props share one material.

Keep in sync with WorldPalette.cs, which names these swatches.

    python Tools/ui_kit/generate_world_palette.py
"""
import os

from PIL import Image, ImageDraw

ROOT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets")
SOURCE = os.path.join(ROOT, "CuteMagic_CubeAnimals_Free", "CubeAnimals_Free", "ShaderTexture", "Materials", "ColorMap_Texture.png")
OUT = os.path.join(ROOT, "_Game", "Art", "World", "WorldPalette.png")

ROW_10 = ["fff8ec", "e6dccb", "a6d36e", "86bf58", "5e9a3e", "4a7d34", "c98e5a", "9a6238",
          "6b4127", "e38b3f", "c8643a", "f4b7b5", "bfe0da", "7a7570", "57524e", "fffbf0"]
ROW_11 = ["f7f5fa", "d3cfe0", "9fd35e", "6fb04a", "4c8d42", "386b35", "8a5a3a", "c9c1b4",
          "9a9186", "8ccfd0", "5fafb5", "d9624a", "a9d2e6", "ffd35c", "e7a0a0", "f4ebdc"]


def hex_rgb(h):
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4)) + (255,)


def main():
    img = Image.open(SOURCE).convert("RGBA")
    w, h = img.size
    d = ImageDraw.Draw(img)
    for row, colours in ((10, ROW_10), (11, ROW_11)):
        for col, colour in enumerate(colours):
            x0 = round(col * w / 16)
            x1 = round((col + 1) * w / 16) - 1
            y0 = round(row * h / 14)
            y1 = round((row + 1) * h / 14) - 1
            d.rectangle([x0, y0, x1, y1], fill=hex_rgb(colour))
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    img.save(OUT)
    print("World palette written to", os.path.abspath(OUT))


if __name__ == "__main__":
    main()
