#!/usr/bin/env python3
"""
Generates the game's sound effects and the ambient cave loop.

Everything here is synthesised from sine waves and noise, so the audio in the repo
has no third-party licence attached to it. Re-run after editing:

    python3 Tools/generate_audio.py

Output: "Alone in the Dark/Assets/Resources/Audio/*.wav", loaded by name in GameAudio.cs.
"""

import math
import os
import random
import struct
import wave

RATE = 22050
OUT = os.path.join(
    os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
    "Alone in the Dark", "Assets", "Resources", "Audio",
)

# Pentatonic-ish scale in Hz. Keeps every random combination consonant.
NOTES = {
    "c3": 130.81, "e3": 164.81, "g3": 196.00, "a3": 220.00,
    "c4": 261.63, "d4": 293.66, "e4": 329.63, "g4": 392.00, "a4": 440.00,
    "c5": 523.25, "d5": 587.33, "e5": 659.25, "g5": 783.99, "c6": 1046.50,
}


def silence(seconds):
    return [0.0] * int(RATE * seconds)


def mix(base, addition, at):
    """Adds a buffer into another at an offset in seconds, growing the target as needed."""
    start = int(RATE * at)
    needed = start + len(addition) - len(base)
    if needed > 0:
        base.extend([0.0] * needed)
    for i, v in enumerate(addition):
        base[start + i] += v
    return base


def envelope(n, attack, release, sustain=1.0):
    """Linear attack, flat body, exponential-ish release. Lengths in samples."""
    out = []
    for i in range(n):
        if i < attack:
            a = i / max(1, attack)
        elif i > n - release:
            a = ((n - i) / max(1, release)) ** 1.8
        else:
            a = sustain
        out.append(a)
    return out


def tone(freq, seconds, volume=0.5, attack=0.004, release=None, harmonics=(1.0, 0.25, 0.08), vibrato=0.0):
    n = int(RATE * seconds)
    release = seconds * 0.7 if release is None else release
    env = envelope(n, int(RATE * attack), int(RATE * release))
    out = []
    for i in range(n):
        t = i / RATE
        f = freq * (1.0 + vibrato * math.sin(2 * math.pi * 5.0 * t))
        v = 0.0
        for k, amp in enumerate(harmonics, start=1):
            v += amp * math.sin(2 * math.pi * f * k * t)
        out.append(v * env[i] * volume)
    return out


def sweep(f0, f1, seconds, volume=0.5, noise=0.0):
    n = int(RATE * seconds)
    env = envelope(n, int(RATE * 0.002), int(RATE * seconds * 0.8))
    out = []
    phase = 0.0
    for i in range(n):
        frac = i / n
        f = f0 + (f1 - f0) * frac
        phase += 2 * math.pi * f / RATE
        v = math.sin(phase)
        if noise:
            v = v * (1 - noise) + random.uniform(-1, 1) * noise
        out.append(v * env[i] * volume)
    return out


def write(name, samples, peak=0.85):
    os.makedirs(OUT, exist_ok=True)
    high = max((abs(v) for v in samples), default=1.0) or 1.0
    gain = peak / high
    frames = bytearray()
    for v in samples:
        frames += struct.pack("<h", int(max(-1.0, min(1.0, v * gain)) * 32000))
    path = os.path.join(OUT, name + ".wav")
    with wave.open(path, "wb") as f:
        f.setnchannels(1)
        f.setsampwidth(2)
        f.setframerate(RATE)
        f.writeframes(bytes(frames))
    print("%-10s %5.2fs  %6.1f kB" % (name, len(samples) / RATE, os.path.getsize(path) / 1024))


def sfx_tap():
    """Menu blip. Short enough to spam."""
    return tone(NOTES["g4"], 0.07, volume=0.35, harmonics=(1.0, 0.3))


def sfx_start():
    """Two notes up. The sound of a level beginning."""
    out = silence(0.3)
    mix(out, tone(NOTES["c4"], 0.12, volume=0.4), 0.0)
    mix(out, tone(NOTES["g4"], 0.18, volume=0.4), 0.09)
    return out


def sfx_pickup():
    """Coin-adjacent triad, deliberately bright."""
    out = silence(0.36)
    for i, note in enumerate(("e5", "g5", "c6")):
        mix(out, tone(NOTES[note], 0.16, volume=0.35, harmonics=(1.0, 0.18)), i * 0.055)
    return out


def sfx_secret():
    """A wall opens. Shimmer, no melody, so it reads as "something changed"."""
    out = silence(1.0)
    for i, note in enumerate(("c5", "e5", "g5", "d5")):
        mix(out, tone(NOTES[note], 0.7, volume=0.22, attack=0.05, vibrato=0.004), i * 0.07)
    return out


def sfx_level():
    """Chapter cleared."""
    out = silence(0.8)
    for i, note in enumerate(("c4", "e4", "g4", "c5")):
        mix(out, tone(NOTES[note], 0.35, volume=0.32), i * 0.09)
    return out


def sfx_win():
    """End of the story. The only long cue in the game."""
    out = silence(2.4)
    melody = (("c4", 0.0), ("e4", 0.18), ("g4", 0.36), ("c5", 0.54), ("g4", 0.78), ("c5", 0.96), ("e5", 1.2))
    for note, at in melody:
        mix(out, tone(NOTES[note], 0.7, volume=0.3, harmonics=(1.0, 0.3, 0.12)), at)
    mix(out, tone(NOTES["c3"], 1.8, volume=0.18, attack=0.1), 0.5)
    return out


def sfx_crash():
    """Death. A thud plus a falling whistle, no harsh noise burst."""
    out = silence(0.7)
    mix(out, sweep(420, 60, 0.45, volume=0.5, noise=0.35), 0.0)
    mix(out, tone(NOTES["c3"], 0.5, volume=0.35, attack=0.001, harmonics=(1.0, 0.5, 0.2)), 0.0)
    return out


def ambient(seconds=12.0):
    """
    Cave drone that loops seamlessly: every component completes a whole number of
    cycles over the loop length, so the end lines up with the start.
    """
    n = int(RATE * seconds)
    out = [0.0] * n

    # Drone partials, each an exact multiple of the loop frequency.
    loop_f = 1.0 / seconds
    for freq, amp in ((65.41, 0.5), (98.0, 0.28), (130.81, 0.18), (196.0, 0.07)):
        cycles = round(freq / loop_f)
        f = cycles * loop_f
        for i in range(n):
            t = i / RATE
            out[i] += amp * math.sin(2 * math.pi * f * t)

    # Slow breathing of the whole drone, also an exact number of cycles.
    for i in range(n):
        t = i / RATE
        out[i] *= 0.62 + 0.38 * math.sin(2 * math.pi * (2 * loop_f) * t)

    # A few distant drips, kept away from the loop seam.
    random.seed(7)
    for _ in range(5):
        at = random.uniform(0.6, seconds - 2.0)
        note = random.choice(("c5", "e5", "g5"))
        mix(out, tone(NOTES[note], 0.9, volume=0.05, attack=0.01, vibrato=0.002), at)

    return out[:n]


def main():
    write("tap", sfx_tap(), peak=0.5)
    write("start", sfx_start(), peak=0.6)
    write("pickup", sfx_pickup(), peak=0.7)
    write("secret", sfx_secret(), peak=0.6)
    write("level", sfx_level(), peak=0.7)
    write("win", sfx_win(), peak=0.8)
    write("crash", sfx_crash(), peak=0.8)
    write("ambient", ambient(), peak=0.55)


if __name__ == "__main__":
    main()
