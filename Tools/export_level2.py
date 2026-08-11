"""Turn the parsed 2D Level 2 into a placement list for the 3D blockout.

2D (x, y) maps to 3D (x, z) — the ground plane — so the maze keeps its exact
proportions and every corridor stays where players remember it.

The maze is a **tilemap**, not a set of props: 926 obstacle cells and a solid
1360-cell floor. Emitting one cube per cell would mean ~2 300 objects, so the
obstacle grid is merged into maximal rectangles first (greedy: extend right, then
down while the full width still matches). That is a lossless change — the same
solid shape, two orders of magnitude fewer objects for the renderer, the physics
scene and the NavMesh bake to chew on.

Emits CSV: kind,name,x,z,a,b,rotY
Usage: python export_level2.py <l2.json> <out.csv>
"""
import json
import sys

# The 2D scene's Grid sits at (-20, -17) with 1 m cells, so cell (cx, cy) centres on
# world (cx + 0.5 - 20, cy + 0.5 - 17). Verified against Greenie's own spawn.
GRID_X, GRID_Y = -20.0, -17.0

PREFAB_KIND = {
    "Keycard": "keycard",
    "SweepingLaser": "laser",
    "ManholeTrap": "manhole",
    "PollutionFlyBot": "flybot",
    "Player": "player",
    "MegaSmogBoss": "boss",
    "HUD": None,          # the 3D HUD.prefab is placed by the builder, not the layout
}

SCRIPT_KIND = {
    "BossDoor": "bossdoor",
    "RescueNPC": "rescuenpc",
    "ReturnPortal": "returnportal",
    "DialogueNPC": "npc",
}


def cell_to_world(cx, cy):
    return cx + 0.5 + GRID_X, cy + 0.5 + GRID_Y


def merge_rects(cells):
    """Greedy maximal-rectangle merge over a set of (x, y) cells."""
    todo = set(map(tuple, cells))
    rects = []
    for cx, cy in sorted(todo):
        if (cx, cy) not in todo:
            continue
        # extend right
        w = 1
        while (cx + w, cy) in todo:
            w += 1
        # extend down while the whole width is present
        h = 1
        while all((cx + i, cy + h) in todo for i in range(w)):
            h += 1
        for j in range(h):
            for i in range(w):
                todo.discard((cx + i, cy + j))
        rects.append((cx, cy, w, h))
    return rects


def main():
    data = json.load(open(sys.argv[1], encoding="utf-8"))
    out = []

    # --- floor and walls, merged ------------------------------------------------------
    for map_name, kind in (("Tilemap_Ground", "floor"), ("Tilemap_Obstacles", "wall")):
        cells = data["tilemaps"].get(map_name, [])
        for cx, cy, w, h in merge_rects(cells):
            # rectangle spans cells [cx, cx+w) x [cy, cy+h); centre is half a cell in
            x0, y0 = cell_to_world(cx, cy)
            out.append((kind, kind.capitalize(),
                        x0 - 0.5 + w / 2.0, y0 - 0.5 + h / 2.0, float(w), float(h), 0.0))

    # --- gameplay prefab instances ------------------------------------------------------
    for inst in data["instances"]:
        kind = PREFAB_KIND.get(inst["prefab"], None)
        if kind is None:
            continue
        x, y, _z = inst["pos"]
        out.append((kind, inst["name"], x, y, 0.0, 0.0, 0.0))

    # --- gameplay objects authored straight into the scene --------------------------------
    for obj in data["objects"]:
        if not obj["pos"]:
            continue
        for script in obj["scripts"]:
            kind = SCRIPT_KIND.get(script)
            if kind is None:
                continue
            out.append((kind, obj["name"], obj["pos"][0], obj["pos"][1], 0.0, 0.0, 0.0))
            break

    with open(sys.argv[2], "w", encoding="utf-8") as fh:
        for kind, name, x, y, a, b, rot in out:
            fh.write("%s,%s,%.3f,%.3f,%.3f,%.3f,%.1f\n" % (kind, name, x, y, a, b, rot))

    kinds = {}
    for k, *_ in out:
        kinds[k] = kinds.get(k, 0) + 1
    print("wrote", len(out), "placements:", kinds)


if __name__ == "__main__":
    main()
