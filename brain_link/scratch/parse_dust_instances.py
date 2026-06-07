import re
import os

scene_path = r"i:\PG projects\BunkerDen\Assets\Assets\Scenes\BunkerScene_v2.unity"
dust_guid = "49807342fca141243b63eda5ee636c9d"

# We want to trace the 5 instances of FloatingDust
# In Unity YAML:
# A VisualEffect component starts with:
# --- !u!2083052967 &<ID>
# VisualEffect:
#   m_GameObject: {fileID: <go_id>}
#   m_Asset: {fileID: 8926484042661614526, guid: 49807342fca141243b63eda5ee636c9d, type: 3}

# First, find the GameObject IDs that hold these components
vfx_go_ids = []
current_go_ref = None
current_vfx_id = None
in_vfx = False

go_ref_re = re.compile(r"m_GameObject:.*fileID:\s*(\d+)")
asset_re = re.compile(r"m_Asset:.*guid:\s*([a-f0-9]{32})")

with open(scene_path, 'r', encoding='utf-8', errors='ignore') as f:
    for line in f:
        # VisualEffect class is 2083052967
        if line.startswith("--- !u!2083052967 &"):
            in_vfx = True
            current_vfx_id = line.split("&")[1].strip()
            current_go_ref = None
            continue
        if in_vfx:
            if line.startswith("--- !u!"):
                in_vfx = False
                continue
            go_m = go_ref_re.search(line)
            if go_m:
                current_go_ref = go_m.group(1)
            asset_m = asset_re.search(line)
            if asset_m:
                guid = asset_m.group(1)
                if guid == dust_guid:
                    vfx_go_ids.append((current_vfx_id, current_go_ref))
                in_vfx = False

# Now read GameObjects and Transforms to get names and positions
go_names = {}
go_transform_map = {} # go_id -> transform_id
transform_pos = {} # transform_id -> pos vector

go_start = re.compile(r"^--- !u!1 &(\d+)")
name_re = re.compile(r"m_Name:\s*(.*)$")
in_go = False
cur_go_id = None

with open(scene_path, 'r', encoding='utf-8', errors='ignore') as f:
    for line in f:
        m = go_start.match(line)
        if m:
            in_go = True
            cur_go_id = m.group(1)
            go_transform_map[cur_go_id] = None
            continue
        if in_go:
            if line.startswith("--- !u!"):
                in_go = False
                continue
            nm = name_re.search(line)
            if nm:
                go_names[cur_go_id] = nm.group(1).strip()
            
            # Find transform component reference in m_Component list
            # Format:
            # - component: {fileID: 12345}
            # The first component is always the Transform in Unity.
            if "component:" in line:
                comp_m = re.search(r"component:\s*\{fileID:\s*(\d+)\}", line)
                if comp_m and go_transform_map[cur_go_id] is None:
                    go_transform_map[cur_go_id] = comp_m.group(1)

# Read positions from Transform components (class 4)
trans_start = re.compile(r"^--- !u!4 &(\d+)")
pos_re = re.compile(r"m_LocalPosition:\s*\{x:\s*(.*),\s*y:\s*(.*),\s*z:\s*(.*)\}")
in_trans = False
cur_trans_id = None

with open(scene_path, 'r', encoding='utf-8', errors='ignore') as f:
    for line in f:
        m = trans_start.match(line)
        if m:
            in_trans = True
            cur_trans_id = m.group(1)
            continue
        if in_trans:
            if line.startswith("--- !u!"):
                in_trans = False
                continue
            pos_m = pos_re.search(line)
            if pos_m:
                transform_pos[cur_trans_id] = (pos_m.group(1), pos_m.group(2), pos_m.group(3))
                in_trans = False

print("--- FloatingDust VFX Instances in Scene ---")
for vfx_id, go_id in vfx_go_ids:
    name = go_names.get(go_id, f"Unknown GameObject (ID: {go_id})")
    tid = go_transform_map.get(go_id)
    pos = transform_pos.get(tid, ("0", "0", "0"))
    print(f"VFX ID: {vfx_id} | GameObject: '{name}' (ID: {go_id}) | Position: ({pos[0]}, {pos[1]}, {pos[2]})")
