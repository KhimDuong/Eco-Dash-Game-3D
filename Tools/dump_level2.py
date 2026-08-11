"""Extract the 2D Level 2 (Factory Maze) layout from its scene YAML.

Level 2 is built differently from Level 1: the maze itself is a pair of
**Tilemaps** (ground + obstacles) rather than individual prop objects, and every
piece of gameplay is a **PrefabInstance** whose position lives in the instance's
modification list. `dump_scene.py` reads neither, so this is its Level 2 sibling.

Usage: python dump_level2.py <scene.unity> <project_root>
Emits JSON: {"tilemaps": {name: [[x, y], ...]}, "instances": [...], "objects": [...]}
"""
import json
import os
import re
import sys

DOC_RE = re.compile(r"^--- !u!(\d+) &(\d+)")
FILEID_RE = re.compile(r"fileID:\s*(-?\d+)")
GUID_RE = re.compile(r"guid:\s*([a-f0-9]{32})")
TILE_RE = re.compile(r"^\s*- first:\s*\{x:\s*(-?\d+),\s*y:\s*(-?\d+),\s*z:\s*(-?\d+)\}")


def guid_index(project_root):
    """guid -> asset path, from every .meta under Assets/."""
    index = {}
    for root, _dirs, files in os.walk(os.path.join(project_root, "Assets")):
        for f in files:
            if not f.endswith(".meta"):
                continue
            p = os.path.join(root, f)
            try:
                with open(p, "r", encoding="utf-8", errors="ignore") as fh:
                    for line in fh:
                        if line.startswith("guid: "):
                            index[line.strip()[6:]] = os.path.relpath(
                                p[: -len(".meta")], project_root).replace("\\", "/")
                            break
            except OSError:
                pass
    return index


def split_docs(path):
    """[(fileID, class-name, [lines])] in file order."""
    with open(path, "r", encoding="utf-8", errors="ignore") as fh:
        lines = fh.readlines()
    docs, cur = [], None
    for i, line in enumerate(lines):
        m = DOC_RE.match(line)
        if m:
            cls = lines[i + 1].strip().rstrip(":") if i + 1 < len(lines) else "?"
            cur = (m.group(2), cls, [])
            docs.append(cur)
        elif cur is not None:
            cur[2].append(line.rstrip("\n"))
    return docs


def field(body, key):
    """Top-level (2-space indented) field of a document."""
    prefix = "  %s: " % key
    for line in body:
        if line.startswith(prefix):
            return line[len(prefix):].strip()
    return None


def main():
    scene, project_root = sys.argv[1], sys.argv[2]
    guids = guid_index(project_root)
    docs = split_docs(scene)
    by_id = {fid: (cls, body) for fid, cls, body in docs}

    # --- GameObject names, and Transform world positions ----------------------------
    names = {fid: field(body, "m_Name") for fid, cls, body in docs if cls == "GameObject"}

    transforms = {}   # transform fileID -> (gameObject fileID, local pos, parent fileID)
    for fid, cls, body in docs:
        if cls not in ("Transform", "RectTransform"):
            continue
        go = FILEID_RE.search(field(body, "m_GameObject") or "")
        pos = field(body, "m_LocalPosition") or ""
        father = FILEID_RE.search(field(body, "m_Father") or "")
        p = {k: float(v) for k, v in re.findall(r"\b([xyz]):\s*(-?[\d.eE+-]+)", pos)}
        transforms[fid] = (go.group(1) if go else None,
                           (p.get("x", 0.0), p.get("y", 0.0), p.get("z", 0.0)),
                           father.group(1) if father else None)

    def world(tfid):
        x = y = z = 0.0
        seen = set()
        while tfid and tfid != "0" and tfid in transforms and tfid not in seen:
            seen.add(tfid)
            _go, (lx, ly, lz), parent = transforms[tfid]
            x, y, z = x + lx, y + ly, z + lz
            tfid = parent
        return round(x, 3), round(y, 3), round(z, 3)

    # --- tilemaps: the maze itself ---------------------------------------------------
    tilemaps = {}
    for fid, cls, body in docs:
        if cls != "Tilemap":
            continue
        go = FILEID_RE.search(field(body, "m_GameObject") or "")
        name = names.get(go.group(1), "Tilemap") if go else "Tilemap"
        tiles = []
        in_tiles = False
        for line in body:
            if line.startswith("  m_Tiles:"):
                in_tiles = True
                continue
            if in_tiles:
                m = TILE_RE.match(line)
                if m:
                    tiles.append([int(m.group(1)), int(m.group(2))])
                elif line.startswith("  ") and not line.startswith("   ") and line.strip().endswith(":"):
                    in_tiles = False   # next top-level field
        tilemaps[name] = tiles

    # --- prefab instances: every piece of gameplay -----------------------------------
    instances = []
    for fid, cls, body in docs:
        if cls != "PrefabInstance":
            continue
        src = GUID_RE.search(field(body, "m_SourcePrefab") or "")
        prefab = guids.get(src.group(1), src.group(1)) if src else "?"
        parent = FILEID_RE.search(field(body, "m_TransformParent") or "")

        # Position and name live in the modification list, one propertyPath per entry.
        mods, name = {}, None
        path = None
        for line in body:
            pm = re.match(r"\s*propertyPath:\s*(\S+)", line)
            if pm:
                path = pm.group(1)
                continue
            vm = re.match(r"\s*value:\s*(.*)$", line)
            if vm and path:
                v = vm.group(1).strip()
                if path == "m_Name":
                    name = v
                elif path in ("m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z"):
                    try:
                        mods[path[-1]] = float(v)
                    except ValueError:
                        pass
                path = None

        px, py, pz = mods.get("x", 0.0), mods.get("y", 0.0), mods.get("z", 0.0)
        if parent and parent.group(1) != "0":
            wx, wy, wz = world(parent.group(1))
            px, py, pz = px + wx, py + wy, pz + wz
        instances.append({
            "prefab": os.path.basename(prefab).replace(".prefab", ""),
            "name": name or os.path.basename(prefab).replace(".prefab", ""),
            "pos": [round(px, 3), round(py, 3), round(pz, 3)],
        })

    # --- plain scene objects carrying gameplay scripts --------------------------------
    objects = []
    for fid, cls, body in docs:
        if cls != "GameObject":
            continue
        scripts = []
        for cfid in FILEID_RE.findall("\n".join(l for l in body if "- component:" in l)):
            c = by_id.get(cfid)
            if not c or c[0] != "MonoBehaviour":
                continue
            gm = GUID_RE.search(field(c[1], "m_Script") or "")
            if gm:
                scripts.append(os.path.basename(guids.get(gm.group(1), gm.group(1))).replace(".cs", ""))
        if not scripts:
            continue
        tfid = next((t for t, (go, _p, _f) in transforms.items() if go == fid), None)
        objects.append({
            "name": field(body, "m_Name"),
            "scripts": scripts,
            "pos": list(world(tfid)) if tfid else None,
        })

    json.dump({"tilemaps": tilemaps, "instances": instances, "objects": objects},
              sys.stdout, ensure_ascii=False, indent=1)


if __name__ == "__main__":
    main()
