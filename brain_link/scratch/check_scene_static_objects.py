import re
import os

scene_path = r"i:\PG projects\BunkerDen\Assets\Assets\Scenes\BunkerScene_v2.unity"

# Flags bitmask:
# Lightmap Static = 1
# Occluder Static = 2
# Batching Static = 4
# Navigation Static = 8
# Occludee Static = 16
# OffMeshLink Static = 32
# Reflection Probe Static = 64

# Scan scene for GameObjects (class 1)
go_start = re.compile(r"^--- !u!1 &(\d+)")
name_re = re.compile(r"m_Name:\s*(.*)$")
static_flags_re = re.compile(r"m_StaticEditorFlags:\s*(\d+)")

go_statics = {} # go_id -> {"name": "", "static_flags": 0}
current_go_id = None
in_go = False

with open(scene_path, 'r', encoding='utf-8', errors='ignore') as f:
    for line in f:
        m = go_start.match(line)
        if m:
            in_go = True
            current_go_id = m.group(1)
            go_statics[current_go_id] = {"name": "", "static_flags": 0}
            continue
        if in_go:
            if line.startswith("--- !u!"):
                in_go = False
                current_go_id = None
                continue
            nm = name_re.search(line)
            if nm:
                go_statics[current_go_id]["name"] = nm.group(1).strip()
            sf = static_flags_re.search(line)
            if sf:
                go_statics[current_go_id]["static_flags"] = int(sf.group(1))

# Scan scene for PrefabInstances (class 1001) and find overridden static flags
prefab_start = re.compile(r"^--- !u!1001 &(\d+)")
in_prefab = False
current_prefab_id = None
modifications = []
cur_mod = {}

# We're looking for modifications:
# - target: {fileID: ..., guid: ..., type: 3}
#   propertyPath: m_StaticEditorFlags
#   value: ...
target_re = re.compile(r"target:\s*\{fileID:\s*\d+,\s*guid:\s*[a-f0-9]{32},\s*type:\s*\d+\}")
property_re = re.compile(r"propertyPath:\s*(.*)$")
value_re = re.compile(r"value:\s*(\d+)")

target_seen = False
prop_name = None
val_seen = None

prefab_statics = [] # (prefab_instance_id, static_flags)

with open(scene_path, 'r', encoding='utf-8', errors='ignore') as f:
    for line in f:
        m = prefab_start.match(line)
        if m:
            in_prefab = True
            current_prefab_id = m.group(1)
            continue
        if in_prefab:
            if line.startswith("--- !u!"):
                in_prefab = False
                current_prefab_id = None
                continue
            
            # Look for modifications list
            if target_re.search(line):
                target_seen = True
                prop_name = None
                val_seen = None
            if target_seen:
                p_m = property_re.search(line)
                if p_m:
                    prop_name = p_m.group(1).strip()
                v_m = value_re.search(line)
                if v_m:
                    val_seen = int(v_m.group(1))
                    if prop_name == "m_StaticEditorFlags":
                        prefab_statics.append((current_prefab_id, val_seen))
                        target_seen = False
                        prop_name = None
                        val_seen = None

# Let's count how many total objects are static and their flag breakdown
print("--- Scene GameObject Static Flags Analysis ---")
total_static_gos = 0
lightmap_static = 0
occluder_static = 0
batching_static = 0
navigation_static = 0
occludee_static = 0
reflection_static = 0

def analyze_flags(flags, name):
    global lightmap_static, occluder_static, batching_static, navigation_static, occludee_static, reflection_static
    is_static = False
    
    if flags & 1:
        lightmap_static += 1
        is_static = True
    if flags & 2:
        occluder_static += 1
        is_static = True
    if flags & 4:
        batching_static += 1
        is_static = True
    if flags & 8:
        navigation_static += 1
        is_static = True
    if flags & 16:
        occludee_static += 1
        is_static = True
    if flags & 64:
        reflection_static += 1
        is_static = True
    return is_static

for go_id, info in go_statics.items():
    flags = info["static_flags"]
    if flags > 0:
        total_static_gos += 1
        analyze_flags(flags, info["name"])

print(f"Total Base GameObjects: {len(go_statics)}")
print(f"Static GameObjects in Scene file: {total_static_gos}")

print("\n--- Prefab Instance Overridden Static Flags ---")
print(f"Total Prefab Instances with static overrides: {len(prefab_statics)}")
prefab_static_count = 0
for pid, flags in prefab_statics:
    if flags > 0:
        prefab_static_count += 1
        analyze_flags(flags, f"Prefab {pid}")

print(f"Static Prefab Instances: {prefab_static_count}")

print("\n--- Overall Static Flag Breakdown ---")
print(f"Lightmap Static (bit 0): {lightmap_static}")
print(f"Occluder Static (bit 1): {occluder_static}")
print(f"Batching Static (bit 2): {batching_static}")
print(f"Navigation Static (bit 3): {navigation_static}")
print(f"Occludee Static (bit 4): {occludee_static}")
print(f"Reflection Probe Static (bit 6): {reflection_static}")
