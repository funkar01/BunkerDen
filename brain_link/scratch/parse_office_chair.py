import re
import os

prefab_path = r"i:\PG projects\BunkerDen\Assets\Assets\Prefabs\OfficeChair.prefab"

if not os.path.exists(prefab_path):
    print("OfficeChair.prefab not found")
    sys.exit(1)

go_start = re.compile(r"^--- !u!1 &(\d+)")
name_re = re.compile(r"m_Name:\s*(.*)$")
renderer_start = re.compile(r"^--- !u!23 &(\d+)")
go_ref_re = re.compile(r"m_GameObject:\s*\{fileID:\s*(\d+)\}")
mesh_filter_start = re.compile(r"^--- !u!33 &(\d+)")
mesh_ref_re = re.compile(r"m_Mesh:\s*\{fileID:\s*(-?\d+),\s*guid:\s*([a-f0-9]{32})")

go_map = {} # go_id -> {"name": "", "components": []}
current_go_id = None
in_go = False

with open(prefab_path, 'r', encoding='utf-8', errors='ignore') as f:
    for line in f:
        go_m = go_start.match(line)
        if go_m:
            current_go_id = go_m.group(1)
            go_map[current_go_id] = {"name": "", "components": []}
            in_go = True
            continue
        if in_go:
            if line.startswith("--- !u!"):
                in_go = False
                current_go_id = None
                continue
            name_m = name_re.search(line)
            if name_m:
                go_map[current_go_id]["name"] = name_m.group(1).strip()

# Next pass, count components per GameObject
current_comp_id = None
in_comp = False
comp_class = None

with open(prefab_path, 'r', encoding='utf-8', errors='ignore') as f:
    for line in f:
        comp_m = re.match(r"^--- !u!(\d+) &(\d+)", line)
        if comp_m:
            comp_class = comp_m.group(1)
            current_comp_id = comp_m.group(2)
            in_comp = True
            continue
        if in_comp:
            if line.startswith("--- !u!"):
                in_comp = False
                continue
            go_ref = go_ref_re.search(line)
            if go_ref:
                go_id = go_ref.group(1)
                if go_id in go_map:
                    go_map[go_id]["components"].append((comp_class, current_comp_id))

print("--- OfficeChair Prefab Hierarchy & Renderers ---")
renderer_gos = []
for go_id, info in go_map.items():
    renderers = [comp for comp in info["components"] if comp[0] == '23']
    if renderers:
        renderer_gos.append((info["name"], len(renderers), go_id))
        
print(f"Total GameObjects with MeshRenderers: {len(renderer_gos)}")
for name, cnt, go_id in sorted(renderer_gos, key=lambda x: x[0]):
    print(f"GameObject: '{name}' (ID: {go_id}) has {cnt} renderer(s)")
