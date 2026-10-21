"""Synthesizes the game's short sound effects: button click, board actions, the win
fanfare and the coin sounds of the win card. Soft bell and marimba tones in the same
key (C major) as the step tune, so every effect sits under the melody instead of
fighting it.

Writes .ogg files through ffmpeg (must be on PATH). Rerun after a tweak; Unity keeps
the GUIDs.

    python Tools/sfx/generate_sfx.py
"""
import math
import os
import random
import struct
import subprocess
import tempfile
import wave

RATE = 44100
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Audio", "Sfx")

NOTES = {"C": -9, "D": -7, "E": -5, "F": -4, "G": -2, "A": 0, "B": 2}


def freq(note):
    """'C5' style note name to Hz."""
    name, octave = note[:-1], int(note[-1])
    return 440.0 * 2 ** ((NOTES[name] + 12 * (octave - 4)) / 12)


def silence(seconds):
    return [0.0] * int(RATE * seconds)


def mix(into, sound, at):
    start = int(RATE * at)
    need = start + len(sound)
    if len(into) < need:
        into.extend([0.0] * (need - len(into)))
    for i, v in enumerate(sound):
        into[start + i] += v
    return into


def bell(hz, seconds, decay=6.0, partials=((1, 1.0), (2.0, 0.35), (3.01, 0.12), (4.2, 0.05))):
    """A soft struck tone: a few partials, a 4 ms attack and an exponential fade."""
    n = int(RATE * seconds)
    out = []
    for i in range(n):
        t = i / RATE
        env = min(1.0, t / 0.004) * math.exp(-decay * t)
        out.append(env * sum(a * math.sin(2 * math.pi * hz * m * t) for m, a in partials))
    return out


def marimba(hz, seconds):
    return bell(hz, seconds, decay=11.0, partials=((1, 1.0), (4.0, 0.18), (9.9, 0.04)))


def sweep(f0, f1, seconds, decay=8.0):
    n = int(RATE * seconds)
    out, phase = [], 0.0
    for i in range(n):
        t = i / RATE
        f = f0 * (f1 / f0) ** (t / seconds)
        phase += 2 * math.pi * f / RATE
        env = min(1.0, t / 0.006) * math.exp(-decay * t)
        out.append(env * (math.sin(phase) + 0.2 * math.sin(2 * phase)))
    return out


def whoosh(seconds, rising):
    """Noise through a moving one-pole low-pass, swelling then fading."""
    rnd = random.Random(7)
    n = int(RATE * seconds)
    out, y = [], 0.0
    for i in range(n):
        p = i / n
        cutoff = 0.02 + 0.25 * (p if rising else 1 - p)
        y += cutoff * (rnd.uniform(-1, 1) - y)
        env = math.sin(math.pi * p) ** 1.5
        out.append(y * env * 2.2)
    return out


def normalize(sound, peak=0.7):
    top = max(abs(v) for v in sound) or 1.0
    fade = int(RATE * 0.01)
    out = [v * peak / top for v in sound]
    for i in range(min(fade, len(out))):
        out[-1 - i] *= i / fade
    return out


def write(name, sound):
    os.makedirs(OUT, exist_ok=True)
    data = normalize(sound)
    with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as tmp:
        path = tmp.name
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, v)) * 32767)) for v in data))
    target = os.path.join(OUT, name + ".ogg")
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", path, "-c:a", "libvorbis", "-q:a", "5", target],
                   check=True)
    os.remove(path)


def chord(notes, step, length, voice=bell):
    out = []
    for i, n in enumerate(notes):
        mix(out, voice(freq(n), length), i * step)
    return out


def main():
    write("ui_click", marimba(freq("G6"), 0.09))
    write("level_start", chord(["C6", "G6"], 0.09, 0.45))
    write("undo", sweep(700, 330, 0.18, decay=10))
    write("restart", whoosh(0.32, rising=False))
    write("hint", chord(["E6", "G6", "C7"], 0.06, 0.4))
    win = chord(["C5", "E5", "G5", "C6"], 0.085, 0.9)
    mix(win, chord(["E6", "G6", "C7"], 0.0, 1.1), 0.36)
    write("win_fanfare", win)
    write("dead_end", chord(["E4", "C4"], 0.16, 0.45, voice=marimba))
    reveal = whoosh(0.22, rising=True)
    mix(reveal, bell(freq("G6"), 0.5), 0.16)
    write("card_reveal", reveal)
    write("milestone", chord(["C6", "E6", "G6", "C7"], 0.07, 0.8))
    coin = marimba(freq("B6"), 0.07)
    mix(coin, bell(freq("E7"), 0.3, decay=9), 0.06)
    write("coin", coin)
    print("SFX written to", os.path.normpath(OUT))


if __name__ == "__main__":
    main()
