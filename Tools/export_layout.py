"""Turn the parsed 2D Level 1 into a placement list for the 3D blockout.

2D (x, y) maps to 3D (x, z) — the ground plane — so the map keeps its exact
proportions and every landmark stays where players remember it.

Emits CSV: kind,name,x,z,a,b,rotY
"""
import json
import sys

rows = json.load(open(sys.argv[1], encoding="utf-8"))
out = []

# --- decoration / props -----------------------------------------------------------
PROP = {
    "rock": "Rock", "bush": "Rock",
    "barrel": "Barrel", "WarningExhaustPipe": "Barrel",
    "crate": "Crate", "pot": "Crate",
}

fences = [tuple(r["pos"][:2]) for r in rows if r["name"] == "Fence"]
fence_set = set(fences)

for r in rows:
    n, p = r["name"], r["pos"]
    if p is None:
        continue
    x, y = p[0], p[1]

    if n in PROP:
        out.append(("prop", PROP[n], x, y, 0, 0, 0))
    elif n == "RusticHut":
        out.append(("prop", "Hut", x, y, 0, 0, 0))
    elif n == "DeadTree":
        out.append(("prop", "DeadTree", x, y, 0, 0, 0))
    elif n == "Fence":
        # A post whose run continues north/south lies along Z, so turn it 90°.
        vertical = (x, y + 1) in fence_set or (x, y - 1) in fence_set
        horizontal = (x + 1, y) in fence_set or (x - 1, y) in fence_set
        rot = 90 if (vertical and not horizontal) else 0
        out.append(("prop", "Fence", x, y, 0, 0, rot))

# --- gameplay ---------------------------------------------------------------------
def scripted(name):
    return [r for r in rows if name in r["scripts"]]

for r in scripted("Chest"):
    x, y = r["pos"][0], r["pos"][1]
    out.append(("chest", "Chest_%d_%d" % (x, y), x, y, 0, 0, 0))

for r in scripted("ToxicMud"):
    box = next((c for c in r["colliders"] if c.get("size")), None)
    sx = box["size"].get("x", 3.0) if box else 3.0
    sy = box["size"].get("y", 3.0) if box else 3.0
    out.append(("mud", "ToxicMud", r["pos"][0], r["pos"][1], sx, sy, 0))

for r in scripted("Litter"):
    out.append(("litter", r["name"], r["pos"][0], r["pos"][1], 0, 0, 0))

for r in scripted("ReclamationPatch"):
    out.append(("patch", r["name"], r["pos"][0], r["pos"][1], 0, 0, 0))

for r in scripted("TeleportGate"):
    out.append(("gate", "TeleportGate", r["pos"][0], r["pos"][1], 0, 0, 0))

for r in scripted("DialogueNPC"):
    out.append(("npc_batu", r["name"], r["pos"][0], r["pos"][1], 0, 0, 0))

for r in scripted("QuestGiverNPC"):
    out.append(("npc_ongsau", r["name"], r["pos"][0], r["pos"][1], 0, 0, 0))

with open(sys.argv[2], "w", encoding="utf-8") as fh:
    for kind, name, x, y, a, b, rot in out:
        fh.write("%s,%s,%.3f,%.3f,%.3f,%.3f,%.1f\n" % (kind, name, x, y, a, b, rot))

kinds = {}
for k, *_ in out:
    kinds[k] = kinds.get(k, 0) + 1
print("wrote", len(out), "placements:", kinds)
