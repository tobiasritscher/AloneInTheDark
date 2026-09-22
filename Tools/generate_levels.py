#!/usr/bin/env python3
"""
Generates the handcrafted cave levels as ASCII maps and verifies each one is solvable.

Levels are defined below as a spine of waypoints plus a corridor radius, which is far
easier to tune than placing a few hundred cubes in the editor. The output .txt files are
plain text and can be hand-edited afterwards; re-running this overwrites them.

    python3 Tools/generate_levels.py

Legend, also documented in each file's header:
    #  rock          S  start (exactly one)
    .  open space    F  finish
    B  bonus pickup  T  chapter gate
    < > ^ v          wind blowing that way

Output: "Alone in the Dark/Assets/Resources/Levels/*.txt", loaded by LevelBuilder.cs.
"""

import math
import os
from collections import deque

OUT = os.path.join(
    os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
    "Alone in the Dark", "Assets", "Resources", "Levels",
)

LEGEND = [
    "@ legend  # rock   . open   S start   F finish   B bonus   < > ^ v wind",
]


class Cave:
    def __init__(self, width, height):
        self.w = width
        self.h = height
        self.g = [["#"] * width for _ in range(height)]

    def carve(self, points, radius):
        """Opens every cell within `radius` cells of the polyline through `points`."""
        for (x0, y0), (x1, y1) in zip(points, points[1:]):
            length = math.hypot(x1 - x0, y1 - y0)
            steps = max(1, int(length * 4))
            for s in range(steps + 1):
                t = s / steps
                cx = x0 + (x1 - x0) * t
                cy = y0 + (y1 - y0) * t
                r = int(math.ceil(radius))
                for dy in range(-r - 1, r + 2):
                    for dx in range(-r - 1, r + 2):
                        px, py = int(round(cx)) + dx, int(round(cy)) + dy
                        if 0 <= px < self.w and 0 <= py < self.h:
                            if math.hypot(px - cx, py - cy) <= radius:
                                self.g[py][px] = "."

    def put(self, x, y, ch):
        self.g[y][x] = ch

    def wind(self, x0, y0, x1, y1, ch):
        """Marks a rectangle of already-open cells as a wind zone."""
        count = 0
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                if 0 <= x < self.w and 0 <= y < self.h and self.g[y][x] == ".":
                    self.g[y][x] = ch
                    count += 1
        assert count > 0, "wind zone at %d,%d covers no open cells" % (x0, y0)

    def open_at(self, x, y):
        return 0 <= x < self.w and 0 <= y < self.h and self.g[y][x] != "#"

    def find(self, ch):
        for y in range(self.h):
            for x in range(self.w):
                if self.g[y][x] == ch:
                    return x, y
        return None

    def reachable(self, start):
        seen = {start}
        q = deque([start])
        while q:
            x, y = q.popleft()
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                p = (x + dx, y + dy)
                if p not in seen and self.open_at(*p):
                    seen.add(p)
                    q.append(p)
        return seen

    def clearance(self, x, y, limit=6):
        """Distance in cells from this cell's centre to the nearest rock."""
        best = float(limit)
        r = int(limit) + 1
        for dy in range(-r, r + 1):
            for dx in range(-r, r + 1):
                px, py = x + dx, y + dy
                if not self.open_at(px, py):
                    best = min(best, math.hypot(dx, dy))
        return best

    def text(self, header):
        return "\n".join(LEGEND + header + ["".join(row) for row in self.g]) + "\n"


def check(cave, name, spine):
    """Solvable means: the finish and every pickup are reachable, and the route stays wide enough."""
    start = cave.find("S")
    finish = cave.find("F")
    assert start, "%s: no start" % name
    assert finish, "%s: no finish" % name

    reach = cave.reachable(start)
    assert finish in reach, "%s: finish is walled off from the start" % name
    for y in range(cave.h):
        for x in range(cave.w):
            if cave.g[y][x] in ("B", "T"):
                assert (x, y) in reach, "%s: pickup at %d,%d is unreachable" % (name, x, y)

    # Measure the travelled route, not every corner of the cave: a diagonal corridor has short
    # vertical runs while still being perfectly wide to fly through.
    along = [cave.clearance(int(round(x)), int(round(y))) for x, y in spine]
    tightest = min(along)
    span = max(x for x, _ in reach) - min(x for x, _ in reach)
    print("%-8s %3dx%-3d  open %4d  span %3d cells (%3d units)  tightest %.1f cells (%.1f units wide)" % (
        name, cave.w, cave.h, len(reach), span, span * 2, tightest, tightest * 2 * 2))
    assert tightest >= 1.0, "%s: route pinches to %.1f cells, the ball cannot pass comfortably" % (name, tightest)
    return True


def level4():
    """Chapter 4: the walls came closer. Descending switchbacks, wide enough to learn on."""
    c = Cave(64, 34)
    spine = [(4, 5), (16, 6), (26, 11), (38, 9), (48, 14), (54, 22), (44, 27), (30, 26), (18, 29)]
    c.carve(spine, 1.7)
    c.carve([(26, 11), (26, 11)], 2.4)
    c.carve([(48, 14), (48, 14)], 2.4)
    c.put(4, 5, "S")
    c.put(18, 29, "F")
    c.put(26, 11, "B")
    c.put(54, 22, "B")
    return c, ["@ name The walls came closer", "@ gravity 1.2"], spine


def level5():
    """Chapter 5: other lights passed this way. A climb, with updrafts that do the lifting."""
    c = Cave(52, 44)
    spine = [(5, 38), (14, 36), (20, 28), (14, 20), (22, 13), (34, 15), (40, 25), (34, 33), (42, 38)]
    c.carve(spine, 1.5)
    c.put(5, 38, "S")
    c.put(42, 38, "F")
    c.put(20, 28, "B")
    c.put(34, 15, "B")
    # Updrafts in the two vertical sections, so climbing feels assisted rather than fought.
    c.wind(17, 20, 23, 30, "^")
    c.wind(37, 25, 43, 34, "v")
    return c, ["@ name Other lights", "@ gravity 1.5"], spine


def level6():
    """Chapter 6: far above, something like a sky. Narrow, crosswinds, the exit is up and out."""
    c = Cave(72, 38)
    spine = [(5, 30), (15, 28), (24, 22), (33, 26), (42, 18), (52, 22), (60, 14), (64, 7), (58, 4)]
    c.carve(spine, 1.5)
    c.carve([(24, 22), (24, 22)], 2.0)
    c.carve([(52, 22), (52, 22)], 2.0)
    c.put(5, 30, "S")
    c.put(58, 4, "F")
    c.put(24, 22, "B")
    c.put(42, 18, "B")
    c.put(60, 14, "B")
    # Crosswinds pushing back down the corridor, so the last stretch has to be fought for.
    c.wind(12, 26, 20, 31, ">")
    c.wind(36, 16, 46, 22, "<")
    c.wind(56, 5, 66, 12, "v")
    return c, ["@ name Something like a sky", "@ gravity 1.5"], spine


def main():
    os.makedirs(OUT, exist_ok=True)
    for index, builder in enumerate((level4, level5, level6), start=4):
        cave, header, spine = builder()
        name = "level%d" % index
        check(cave, name, spine)
        with open(os.path.join(OUT, name + ".txt"), "w") as f:
            f.write(cave.text(header))


if __name__ == "__main__":
    main()
