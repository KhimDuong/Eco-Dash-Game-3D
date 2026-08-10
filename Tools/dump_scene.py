"""Extract a readable layout table from a 2D Unity scene YAML.

Unity's YAML uses custom !u! tags that trip up PyYAML, so this is a small
line scanner: it splits the document stream, keeps the fields we care about
per component type, then resolves transform parents into world positions.

Usage: python dump_scene.py <scene.unity> <project_root> [name_filter]
"""
import os
import re
import sys
import json
from collections import defaultdict

DOC_RE = re.compile(r"^--- !u!(\d+) &(\d+)")
KV_RE = re.compile(r"^(\s*)([A-Za-z_][\w]*):\s*(.*)$")
FILEID_RE = re.compile(r"fileID:\s*(-?\d+)")
GUID_RE = re.compile(r"guid:\s*([a-f0-9]{32})")


def build_guid_index(project_root):
    """guid -> asset path, from every .meta under Assets/."""
    index = {}
    assets = os.path.join(project_root, "Assets")
    for root, _dirs, files in os.walk(assets):
        for f in files:
            if not f.endswith(".meta"):
                continue
            p = os.path.join(root, f)
            try:
                with open(p, "r", encoding="utf-8", errors="ignore") as fh:
                    for line in fh:
                        if line.startswith("guid: "):
                            index[line.strip()[6:]] = os.path.relpath(p[: -len(".meta")], project_root).replace("\\", "/")
                            break
            except OSError:
                pass
    return index


def parse_scene(path):
    """fileID -> {'type': <class name>, <flat field dict>}"""
    objects = {}
    cur = None
    with open(path, "r", encoding="utf-8", errors="ignore") as fh:
        lines = fh.readlines()

    i = 0
    while i < len(lines):
        m = DOC_RE.match(lines[i])
        if m:
            fid = m.group(2)
            # the class name is on the next line, e.g. "GameObject:"
            cls = lines[i + 1].strip().rstrip(":") if i + 1 < len(lines) else "?"
            cur = {"type": cls, "fileID": fid, "components": []}
            objects[fid] = cur
            i += 2
            continue
        if cur is not None:
            line = lines[i]
            if "- component:" in line:
                fm = FILEID_RE.search(line)
                if fm:
                    cur["components"].append(fm.group(1))
            else:
                km = KV_RE.match(line.rstrip("\n"))
                if km and km.group(1) == "  ":          # top-level field of the doc
                    cur[km.group(2)] = km.group(3).strip()
                elif km and km.group(1) == "    ":       # one level in (x/y/z, size...)
                    cur.setdefault("_nested", {})[km.group(2)] = km.group(3).strip()
        i += 1
    return objects


def vec(raw):
    if not raw:
        return None
    out = {}
    for key in ("x", "y", "z"):
        m = re.search(r"\b%s:\s*(-?[\d.eE+]+)" % key, raw)
        if m:
            out[key] = float(m.group(1))
    return out or None


def main():
    scene, project_root = sys.argv[1], sys.argv[2]
    name_filter = sys.argv[3] if len(sys.argv) > 3 else None

    guids = build_guid_index(project_root)
    objs = parse_scene(scene)

    # transforms: fileID -> (gameObject fileID, localPos, localScale, parent)
    transforms = {}
    for fid, o in objs.items():
        if o["type"] in ("Transform", "RectTransform"):
            transforms[fid] = o

    def world_pos(tfid):
        x = y = z = 0.0
        seen = set()
        while tfid and tfid != "0" and tfid in transforms and tfid not in seen:
            seen.add(tfid)
            t = transforms[tfid]
            p = vec(t.get("m_LocalPosition")) or {}
            x += p.get("x", 0.0)
            y += p.get("y", 0.0)
            z += p.get("z", 0.0)
            fm = FILEID_RE.search(t.get("m_Father", "") or "")
            tfid = fm.group(1) if fm else None
        return (round(x, 3), round(y, 3), round(z, 3))

    rows = []
    for fid, o in objs.items():
        if o["type"] != "GameObject":
            continue
        name = o.get("m_Name", "")
        if name_filter and name_filter.lower() not in name.lower():
            continue
        row = {
            "name": name,
            "active": o.get("m_IsActive", "1") == "1",
            "tag": o.get("m_TagString", "").strip(),
            "layer": o.get("m_Layer", ""),
            "scripts": [],
            "colliders": [],
            "sprite": None,
            "pos": None,
            "scale": None,
        }
        for cfid in o["components"]:
            c = objs.get(cfid)
            if not c:
                continue
            t = c["type"]
            if t in ("Transform", "RectTransform"):
                row["pos"] = world_pos(cfid)
                row["scale"] = vec(c.get("m_LocalScale"))
            elif t == "MonoBehaviour":
                gm = GUID_RE.search(c.get("m_Script", "") or "")
                if gm:
                    p = guids.get(gm.group(1), gm.group(1))
                    row["scripts"].append(os.path.basename(p).replace(".cs", ""))
            elif t.endswith("Collider2D"):
                info = {"type": t, "trigger": c.get("m_IsTrigger") == "1"}
                if c.get("m_Size"):
                    info["size"] = vec(c["m_Size"])
                if c.get("m_Radius"):
                    info["radius"] = float(c["m_Radius"])
                if c.get("m_Offset"):
                    info["offset"] = vec(c["m_Offset"])
                row["colliders"].append(info)
            elif t == "SpriteRenderer":
                gm = GUID_RE.search(c.get("m_Sprite", "") or "")
                if gm:
                    row["sprite"] = os.path.basename(guids.get(gm.group(1), gm.group(1)))
                if c.get("m_Size"):
                    row["sprite_size"] = vec(c["m_Size"])
        rows.append(row)

    rows.sort(key=lambda r: (r["name"], r["pos"] or (0, 0, 0)))
    print(json.dumps(rows, ensure_ascii=False, indent=1))


if __name__ == "__main__":
    main()
